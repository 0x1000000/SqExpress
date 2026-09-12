using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace SqExpress.Analyzers.Test;

[TestFixture]
public sealed class SqJsonPathAnalyzerTest
{
    [TestCase("$.store.book[-1]", 13, "nonnegative array index")]
    [TestCase("$.store.", 8, "property name")]
    [TestCase("$..name", 2, "property name")]
    [TestCase("$.items[*]", 8, "nonnegative array index")]
    public async Task InvalidConstantPath_ReportsPositionAndExpectation(string path, int position, string expectation)
    {
        var diagnostics = await Analyze($$"""
            using static SqExpress.SqQueryBuilder;
            public static class Host
            {
                public static object M() => JsonValue("{}", "{{path}}");
            }
            """);

        var diagnostic = diagnostics.Single(i => i.Id == "SQEX020");
        Assert.That(diagnostic.GetMessage(), Does.Contain($"position {position}"));
        Assert.That(diagnostic.GetMessage(), Does.Contain(expectation));
    }

    [Test]
    public async Task ConstantLocalAndExplicitWrapper_AreAnalyzed()
    {
        var diagnostics = await Analyze("""
            using SqExpress;
            using static SqExpress.SqQueryBuilder;
            public static class Host
            {
                public static object M()
                {
                    const string path = "$.a[-1]";
                    return JsonQuery("{}", (SqJsonPath)path);
                }
            }
            """);

        Assert.That(diagnostics.Select(i => i.Id), Contains.Item("SQEX020"));
    }

    [Test]
    public async Task ValidAndRuntimePaths_DoNotReport()
    {
        var diagnostics = await Analyze("""
            using static SqExpress.SqQueryBuilder;
            public static class Host
            {
                public static object M(string path)
                {
                    _ = JsonValue("{}", "$.store.book[0].title");
                    return JsonQuery("{}", path);
                }
            }
            """);

        Assert.That(diagnostics, Is.Empty);
    }

    private static async Task<ImmutableArray<Diagnostic>> Analyze(string source)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var solution = workspace.CurrentSolution.AddProject(ProjectInfo.Create(
            projectId, VersionStamp.Create(), "JsonPathAnalyzerTests", "JsonPathAnalyzerTests",
            LanguageNames.CSharp, parseOptions: new CSharpParseOptions(LanguageVersion.Preview),
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)))
            .AddMetadataReferences(projectId, GetReferences());
        var documentId = DocumentId.CreateNewId(projectId);
        var document = solution.AddDocument(documentId, "Test.cs", SourceText.From(source)).GetDocument(documentId)!;
        var compilation = await document.Project.GetCompilationAsync();
        Assert.That(compilation, Is.Not.Null);
        return await compilation!.WithAnalyzers([new SqJsonPathAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
    }

    private static IReadOnlyList<MetadataReference> GetReferences()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string assemblies)
            foreach (var path in assemblies.Split(Path.PathSeparator)) paths.Add(path);
        paths.Add(typeof(object).GetTypeInfo().Assembly.Location);
        paths.Add(typeof(SqQueryBuilder).GetTypeInfo().Assembly.Location);
        paths.Add(typeof(SqJsonPathAnalyzer).GetTypeInfo().Assembly.Location);
        return paths.Select(path => MetadataReference.CreateFromFile(path)).ToArray();
    }
}
