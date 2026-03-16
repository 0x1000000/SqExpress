using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace SqExpress.Analyzers.Test
{
    [TestFixture]
    public class SqTSqlParserParseAnalyzerTest
    {
        private const string CommonTypes = """
            using System.Collections.Generic;
            using SqExpress;
            using SqExpress.SqlParser;
            using SqExpress.Syntax.Value;

            public sealed class UsersTable : TableBase
            {
                public Int32TableColumn UserId { get; }
                public StringTableColumn Name { get; }

                public UsersTable() : this(default)
                {
                }

                public UsersTable(Alias alias) : base("dbo", "Users", alias)
                {
                    this.UserId = this.CreateInt32Column("UserId");
                    this.Name = this.CreateStringColumn("Name", 64);
                }
            }
            """;

        [Test]
        public async Task Analyze_WhenSqTSqlParserParseUsesCompileTimeSql_ReportsDiagnostic()
        {
            var source = CommonTypes + """

                public sealed class Host
                {
                    public object M()
                    {
                        var u = new UsersTable("u");
                        return SqTSqlParser.Parse("SELECT u.UserId FROM dbo.Users u", [u]);
                    }
                }
                """;

            var diagnostics = await GetDiagnosticsAsync(source);

            Assert.That(diagnostics.Length, Is.EqualTo(1));
            Assert.That(diagnostics[0].Id, Is.EqualTo("SQEX001"));
            Assert.That(diagnostics[0].GetMessage(), Does.Contain("Parse"));
        }

        [Test]
        public async Task Analyze_WhenSqlIsNotCompileTimeConstant_DoesNotReportDiagnostic()
        {
            var source = CommonTypes + """

                public sealed class Host
                {
                    public object M(string sql)
                    {
                        var u = new UsersTable("u");
                        return SqTSqlParser.Parse(sql, [u]);
                    }
                }
                """;

            var diagnostics = await GetDiagnosticsAsync(source);

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public async Task Analyze_WhenSqlComesFromLastLocalAssignment_ReportsDiagnostic()
        {
            var source = CommonTypes + """

                public sealed class Host
                {
                    public object M()
                    {
                        var u = new UsersTable("u");
                        string sql = "SELECT u.UserId FROM dbo.Users u";

                        return SqTSqlParser.Parse(sql, [u]);
                    }
                }
                """;

            var diagnostics = await GetDiagnosticsAsync(source);

            Assert.That(diagnostics.Length, Is.EqualTo(1));
            Assert.That(diagnostics[0].Id, Is.EqualTo("SQEX001"));
        }

        [Test]
        public async Task Analyze_WhenSqlLocalHasAmbiguousControlFlow_DoesNotReportDiagnostic()
        {
            var source = CommonTypes + """

                public sealed class Host
                {
                    public object M(bool flag)
                    {
                        var u = new UsersTable("u");
                        string sql;
                        if (flag)
                        {
                            sql = "SELECT u.UserId FROM dbo.Users u";
                        }
                        else
                        {
                            sql = "SELECT u.Name FROM dbo.Users u";
                        }

                        return SqTSqlParser.Parse(sql, [u]);
                    }
                }
                """;

            var diagnostics = await GetDiagnosticsAsync(source);

            Assert.That(diagnostics, Is.Empty);
        }

        [Test]
        public async Task Analyze_WhenReferencedSqlTableHasNoMatchingSourceTableClass_StillReportsMigrationDiagnostic()
        {
            var source = """
                using SqExpress;
                using SqExpress.SqlParser;

                public sealed class OrdersTable : TableBase
                {
                    public OrdersTable() : base("dbo", "Orders")
                    {
                    }
                }

                public sealed class Host
                {
                    public object M()
                    {
                        var orders = new OrdersTable();
                        return SqTSqlParser.Parse("SELECT u.UserId FROM dbo.Users u", [orders]);
                    }
                }
                """;

            var diagnostics = await GetDiagnosticsAsync(source);

            Assert.That(diagnostics.Length, Is.EqualTo(1));
            Assert.That(diagnostics[0].Id, Is.EqualTo("SQEX001"));
        }

        [Test]
        public async Task CodeFix_WhenParseUsesKnownTableAndWithParams_RewritesToSqExpress()
        {
            var source = CommonTypes + """

                public sealed class Host
                {
                    public object M(int id)
                    {
                        var u = new UsersTable("u");
                        var parsed = SqTSqlParser.Parse("SELECT u.UserId FROM dbo.Users u WHERE u.UserId = @userId", [u]).WithParams("userId", id);
                        return parsed;
                    }
                }
                """;

            var fixedSource = await ApplyCodeFixAsync(source);

            Assert.That(fixedSource, Does.Not.Contain("SqTSqlParser.Parse"));
            Assert.That(fixedSource, Does.Contain("var userId = id;"));
            Assert.That(fixedSource, Does.Contain("var usersTable = new UsersTable(\"u\");"));
            Assert.That(fixedSource, Does.Contain("var expr = Select(usersTable.UserId)"));
            await AssertCompilesAsync(fixedSource);
        }

        [Test]
        public async Task CodeFix_WhenUpdateStatementIsParsed_RewritesToSqExpress()
        {
            var source = CommonTypes + """

                public sealed class Host
                {
                    public string M(int id, string name)
                    {
                        var u = new UsersTable("u");
                        return SqTSqlParser.Parse("UPDATE u SET u.Name = @name FROM dbo.Users u WHERE u.UserId = @userId", [u])
                            .WithParams("name", name)
                            .WithParams("userId", id)
                            .ToSql(SqExpress.SqlExport.TSqlExporter.Default);
                    }
                }
                """;

            var fixedSource = await ApplyCodeFixAsync(source);

            Assert.That(fixedSource, Does.Not.Contain("SqTSqlParser.Parse"));
            Assert.That(fixedSource, Does.Contain("var userId = id;"));
            Assert.That(fixedSource, Does.Contain("Update("));
            Assert.That(fixedSource, Does.Contain(".Set("));
            Assert.That(fixedSource, Does.Contain(".Where("));
            Assert.That(fixedSource, Does.Not.Contain("var name = name;"));
            await AssertCompilesAsync(fixedSource);
        }

        [Test]
        public async Task CodeFix_WhenDeleteStatementIsParsed_RewritesToSqExpress()
        {
            var source = CommonTypes + """

                public sealed class Host
                {
                    public string M(int id)
                    {
                        var u = new UsersTable("u");
                        return SqTSqlParser.Parse("DELETE u FROM dbo.Users u WHERE u.UserId = @userId", [u])
                            .WithParams("userId", id)
                            .ToSql(SqExpress.SqlExport.TSqlExporter.Default);
                    }
                }
                """;

            var fixedSource = await ApplyCodeFixAsync(source);

            Assert.That(fixedSource, Does.Not.Contain("SqTSqlParser.Parse"));
            Assert.That(fixedSource, Does.Contain("var userId = id;"));
            Assert.That(fixedSource, Does.Contain("Delete("));
            Assert.That(fixedSource, Does.Contain(".Where("));
            await AssertCompilesAsync(fixedSource);
        }

        [Test]
        public async Task CodeFix_WhenDeleteStatementOmitsFrom_RewritesToSqExpress()
        {
            var source = CommonTypes + """

                public sealed class Host
                {
                    public string M(int id)
                    {
                        var allTables = new TableBase[] { new UsersTable() };
                        return SqTSqlParser.Parse("DELETE [Users] WHERE UserId = @userId", allTables)
                            .WithParams("userId", id)
                            .ToSql(SqExpress.SqlExport.TSqlExporter.Default);
                    }
                }
                """;

            var fixedSource = await ApplyCodeFixAsync(source);

            Assert.That(fixedSource, Does.Not.Contain("SqTSqlParser.Parse"));
            Assert.That(fixedSource, Does.Contain("Delete("));
            Assert.That(fixedSource, Does.Contain(".Where("));
            await AssertCompilesAsync(fixedSource);
        }

        [Test]
        public async Task CodeFix_WhenInsertStatementIsParsed_RewritesToSqExpress()
        {
            var source = CommonTypes + """

                public sealed class Host
                {
                    public string M(int id, string name)
                    {
                        var u = new UsersTable("u");
                        return SqTSqlParser.Parse("INSERT INTO dbo.Users(UserId, Name) VALUES (@userId, @name)", [u])
                            .WithParams("userId", id)
                            .WithParams("name", name)
                            .ToSql(SqExpress.SqlExport.TSqlExporter.Default);
                    }
                }
                """;

            var fixedSource = await ApplyCodeFixAsync(source);

            Assert.That(fixedSource, Does.Not.Contain("SqTSqlParser.Parse"));
            Assert.That(fixedSource, Does.Contain("var userId = id;"));
            Assert.That(fixedSource, Does.Contain("InsertInto("));
            Assert.That(fixedSource, Does.Not.Contain("var name = name;"));
            await AssertCompilesAsync(fixedSource);
        }

        [Test]
        public async Task CodeFix_WhenDictionaryWithParams_IsUsed_IndexesDictionaryByParameterName()
        {
            var source = CommonTypes + """

                public sealed class Host
                {
                    public object M(IReadOnlyDictionary<string, ParamValue> parameters)
                    {
                        var u = new UsersTable("u");
                        return SqTSqlParser.Parse("SELECT u.UserId FROM dbo.Users u WHERE u.UserId = @userId", [u]).WithParams(parameters);
                    }
                }
                """;

            var fixedSource = await ApplyCodeFixAsync(source);

            Assert.That(fixedSource, Does.Contain("var userId = parameters[\"userId\"];"));
            Assert.That(fixedSource, Does.Contain("userId.AsSingle"));
            await AssertCompilesAsync(fixedSource);
        }

        [Test]
        public async Task CodeFix_WhenQueryContainsDerivedTable_AddsPrivateNestedHelperClass()
        {
            var source = CommonTypes + """

                public sealed class Host
                {
                    public object M()
                    {
                        var u = new UsersTable("u");
                        return SqTSqlParser.Parse("SELECT sq.UserId FROM (SELECT u.UserId FROM dbo.Users u) sq", [u]);
                    }
                }
                """;

            var fixedSource = await ApplyCodeFixAsync(source);

            Assert.That(fixedSource, Does.Contain("private sealed class SqSubQuery"));
            Assert.That(fixedSource, Does.Not.Contain("var usersTable = new UsersTable(\"u\");"));
            await AssertCompilesAsync(fixedSource);
        }

        [Test]
        public async Task CodeFix_WhenHostAlreadyContainsSqSubQuery_UsesNonConflictingNestedHelperName()
        {
            var source = CommonTypes + """

                public sealed class Host
                {
                    private sealed class SqSubQuery
                    {
                    }

                    public object M()
                    {
                        var u = new UsersTable("u");
                        return SqTSqlParser.Parse("SELECT sq.UserId FROM (SELECT u.UserId FROM dbo.Users u) sq", [u]);
                    }
                }
                """;

            var fixedSource = await ApplyCodeFixAsync(source);

            Assert.That(fixedSource, Does.Contain("private sealed class SqSubQuery"));
            Assert.That(fixedSource, Does.Contain("private sealed class SqSubQuery1"));
            await AssertCompilesAsync(fixedSource);
        }

        [Test]
        public async Task CodeFix_WhenTempTestStyleParseIsUsed_RewritesInsideToSqlChain()
        {
            var source = """
                using System;
                using NUnit.Framework;
                using SqExpress.SqlExport;
                using SqExpress.SqlParser;
                using SqExpress.Syntax;
                using SqExpress.SyntaxTreeOperations.Internal;
                using static SqExpress.SqQueryBuilder;

                namespace SqExpress.Test;

                public static class Ext
                {
                    public static string ToSql(this IExpr expr) => expr.ToSql(TSqlExporter.Default);
                }

                public class User : TableBase
                {
                    public User() : base("dbo", "user")
                    {
                    }
                }

                public class TempTest
                {
                    [Test]
                    public void Test()
                    {
                        var tUser = new User();

                        var sql = SqTSqlParser.Parse("SELECT 'Hi,' + @userName + '!'", [tUser]).WithParams("userName", "Alice").ToSql();

                        Console.WriteLine(sql);
                    }
                }
                """;

            var fixedSource = await ApplyCodeFixAsync(source);

            Assert.That(fixedSource, Does.Not.Contain("SqTSqlParser.Parse"));
            Assert.That(fixedSource, Does.Contain("var userName = \"Alice\";"));
            Assert.That(fixedSource, Does.Contain("var expr = Select"));
            Assert.That(fixedSource, Does.Contain("var sql = expr.ToSql();"));
            await AssertCompilesAsync(fixedSource);
        }

        [Test]
        public async Task CodeFix_WhenTablesComeFromHelperMethod_RewritesUsingMaterializedTableLocal()
        {
            var source = CommonTypes + """

                public static class AllTables
                {
                    public static TableBase[] BuildAllTableList() => [BuildUsers()];

                    public static UsersTable BuildUsers() => new UsersTable("U");
                }

                public sealed class Host
                {
                    public object M()
                    {
                        var allTables = AllTables.BuildAllTableList();

                        var sql = SqTSqlParser.Parse("SELECT U.UserId, U.Name FROM dbo.Users U WHERE U.Name = @userName", allTables)
                            .WithParams("userName", "Alice");

                        return sql;
                    }
                }
                """;

            var fixedSource = await ApplyCodeFixAsync(source);

            Assert.That(fixedSource, Does.Not.Contain("SqTSqlParser.Parse"));
            Assert.That(fixedSource, Does.Contain("var usersTable = new UsersTable(\"U\");"));
            Assert.That(fixedSource, Does.Contain("var expr = Select(usersTable.UserId, usersTable.Name)"));
            await AssertCompilesAsync(fixedSource);
        }

        [Test]
        public async Task CodeFix_WhenTableDescriptorIsInNamespace_AddsUsingAndUsesSimpleTypeName()
        {
            var source = """
                using SqExpress;
                using SqExpress.SqlParser;

                namespace Demo.Tables
                {
                    public sealed class UsersTable : TableBase
                    {
                        public Int32TableColumn UserId { get; }

                        public UsersTable() : this(default)
                        {
                        }

                        public UsersTable(Alias alias) : base("dbo", "Users", alias)
                        {
                            this.UserId = this.CreateInt32Column("UserId");
                        }
                    }
                }

                public sealed class Host
                {
                    public object M()
                    {
                        return SqTSqlParser.Parse("SELECT U.UserId FROM dbo.Users U", []);
                    }
                }
                """;

            var fixedSource = await ApplyCodeFixAsync(source);

            Assert.That(fixedSource, Does.Contain("using Demo.Tables;"));
            Assert.That(fixedSource, Does.Contain("var usersTable = new UsersTable(\"U\");"));
            Assert.That(fixedSource, Does.Not.Contain("global::Demo.Tables.UsersTable"));
            await AssertCompilesAsync(fixedSource);
        }

        [Test]
        public async Task CodeFix_WhenListParameterUsesCollectionExpression_EmitsArrayInitializer()
        {
            var source = """
                using SqExpress;
                using SqExpress.SqlExport;
                using SqExpress.SqlParser;

                public sealed class TableUser : TableBase
                {
                    public Int32TableColumn UserId { get; }
                    public StringTableColumn LastName { get; }

                    public TableUser() : this(default)
                    {
                    }

                    public TableUser(Alias alias) : base("dbo", "User", alias)
                    {
                        this.UserId = this.CreateInt32Column("UserId");
                        this.LastName = this.CreateStringColumn("LastName", 64);
                    }
                }

                public static class AllTables
                {
                    public static TableBase[] BuildAllTableList() => [new TableUser()];
                }

                public sealed class Host
                {
                    public string M()
                    {
                        var allTables = AllTables.BuildAllTableList();

                        var sql = SqTSqlParser.Parse("SELECT u.LastName FROM [User] u WHERE u.UserId IN (@ids)", allTables)
                            .WithParams("ids", [1, 2, 3])
                            .ToSql(TSqlExporter.Default);

                        return sql;
                    }
                }
                """;

            var fixedSource = await ApplyCodeFixAsync(source);

            Assert.That(fixedSource, Does.Contain("var ids = new[] { 1, 2, 3 };"));
            Assert.That(fixedSource, Does.Not.Contain("var ids = [1, 2, 3];"));
            await AssertCompilesAsync(fixedSource);
        }

        [Test]
        public async Task CodeFix_WhenConversionFails_InsertsSingleSqexErrorBeforeStatement()
        {
            var source = """
                using SqExpress;
                using SqExpress.SqlParser;

                public sealed class Host
                {
                    public object M()
                    {
                        return SqTSqlParser.Parse("SELECT u.UserId FROM dbo.Users u", []);
                    }
                }
                """;

            var fixedSource = await ApplyCodeFixAsync(source);

            Assert.That(fixedSource, Does.Contain("#error SQEX: Could not convert SQL to SqExpress: No SqExpress table class found for SQL table [dbo].[Users]."));

            var fixedAgain = await ApplyCodeFixAsync(fixedSource);
            Assert.That(CountOccurrences(fixedAgain, "#error SQEX:"), Is.EqualTo(1));
        }

        [Test]
        public async Task CodeFix_WhenConversionSucceeds_RemovesSqexErrorsOnlyFromHostingMethod()
        {
            var source = CommonTypes + """

                public sealed class Host
                {
                    public object M1()
                    {
                        #error SQEX: stale method error
                        var u = new UsersTable("u");
                        return SqTSqlParser.Parse("SELECT u.UserId FROM dbo.Users u", [u]);
                    }

                    public object M2()
                    {
                        #error SQEX: keep this one
                        return 1;
                    }
                }
                """;

            var fixedSource = await ApplyCodeFixAsync(source);

            Assert.That(fixedSource, Does.Not.Contain("#error SQEX: stale method error"));
            Assert.That(fixedSource, Does.Contain("#error SQEX: keep this one"));
            Assert.That(fixedSource, Does.Not.Contain("SqTSqlParser.Parse"));
            await AssertCompilesAsync(fixedSource.Replace("#error SQEX: keep this one", string.Empty, StringComparison.Ordinal));
        }

        private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
        {
            var document = CreateDocument(source);
            var compilation = await document.Project.GetCompilationAsync();
            Assert.That(compilation, Is.Not.Null);

            var analyzer = new SqTSqlParserParseAnalyzer();
            var compilationWithAnalyzers = compilation!.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        }

        private static async Task<string> ApplyCodeFixAsync(string source)
        {
            var document = CreateDocument(source);
            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.That(diagnostics, Is.Not.Empty);

            var provider = new SqTSqlParserParseCodeFixProvider();
            var actions = new List<CodeAction>();
            var context = new CodeFixContext(
                document,
                diagnostics[0],
                (action, _) => actions.Add(action),
                CancellationToken.None);

            await provider.RegisterCodeFixesAsync(context);
            Assert.That(actions, Is.Not.Empty);

            var operations = await actions[0].GetOperationsAsync(CancellationToken.None);
            var applyChanges = operations.OfType<ApplyChangesOperation>().Single();
            var changedDocument = applyChanges.ChangedSolution.GetDocument(document.Id);
            Assert.That(changedDocument, Is.Not.Null);

            var text = await changedDocument!.GetTextAsync();
            return text.ToString();
        }

        private static int CountOccurrences(string source, string value)
        {
            var count = 0;
            var index = 0;
            while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }

        private static async Task AssertCompilesAsync(string source)
        {
            var document = CreateDocument(source);
            var compilation = await document.Project.GetCompilationAsync();
            Assert.That(compilation, Is.Not.Null);

            var diagnostics = compilation!.GetDiagnostics()
                .Where(i => i.Severity == DiagnosticSeverity.Error)
                .ToList();

            Assert.That(
                diagnostics,
                Is.Empty,
                string.Join(Environment.NewLine, diagnostics.Select(i => i.ToString())));
        }

        private static Document CreateDocument(string source)
        {
            var workspace = new AdhocWorkspace();
            var projectId = ProjectId.CreateNewId();
            var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var solution = workspace.CurrentSolution
                .AddProject(ProjectInfo.Create(
                    projectId,
                    VersionStamp.Create(),
                    "AnalyzerTests",
                    "AnalyzerTests",
                    LanguageNames.CSharp,
                    parseOptions: parseOptions,
                    compilationOptions: compilationOptions))
                .AddMetadataReferences(projectId, GetMetadataReferences());

            var documentId = DocumentId.CreateNewId(projectId);
            solution = solution.AddDocument(documentId, "Test.cs", SourceText.From(source));

            return solution.GetDocument(documentId)!;
        }

        private static IReadOnlyList<MetadataReference> GetMetadataReferences()
        {
            var referencePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedAssemblies)
            {
                foreach (var path in trustedAssemblies.Split(Path.PathSeparator))
                {
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        referencePaths.Add(path);
                    }
                }
            }

            referencePaths.Add(typeof(object).GetTypeInfo().Assembly.Location);
            referencePaths.Add(typeof(Enumerable).GetTypeInfo().Assembly.Location);
            referencePaths.Add(typeof(TableBase).GetTypeInfo().Assembly.Location);
            referencePaths.Add(typeof(SqTSqlParserParseAnalyzer).GetTypeInfo().Assembly.Location);

            return referencePaths.Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        }
    }
}
