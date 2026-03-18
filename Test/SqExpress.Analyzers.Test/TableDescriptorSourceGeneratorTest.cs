using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace SqExpress.Analyzers.Test
{
    [TestFixture]
    public class TableDescriptorSourceGeneratorTest
    {
        [Test]
        public void Generate_WhenDescriptorIsSimple_EmitsTableBasePattern()
        {
            var source = """
                using SqExpress;

                [TableDescriptor("dbo", "User")]
                [Int32Column("UserId", Pk = true, Identity = true)]
                [StringColumn("Name", Unicode = true, MaxLength = 255)]
                [NullableStringColumn("Display Name", PropertyName = "DisplayName", Unicode = true, MaxLength = 255)]
                [Index("Name")]
                public partial class User
                {
                }
                """;

            var result = RunGenerator(source);
            var generated = GetGeneratedSource(result, "User");

            Assert.That(result.Diagnostics, Is.Empty, FormatDiagnostics(result.Diagnostics));
            Assert.That(generated, Does.Contain("partial class User : global::SqExpress.TableBase"));
            Assert.That(generated, Does.Contain("public User() : this(alias: global::SqExpress.Alias.Auto)"));
            Assert.That(generated, Does.Contain("public User(global::SqExpress.Alias alias) : base(\"dbo\", \"User\", alias)"));
            Assert.That(generated, Does.Contain("this.UserId = this.CreateInt32Column(\"UserId\", global::SqExpress.ColumnMeta.PrimaryKey().Identity());"));
            Assert.That(generated, Does.Contain("this.Name = this.CreateStringColumn(name: \"Name\", size: 255, isUnicode: true, isText: false, columnMeta: null);"));
            Assert.That(generated, Does.Contain("this.DisplayName = this.CreateNullableStringColumn(name: \"Display Name\", size: 255, isUnicode: true, isText: false, columnMeta: null);"));
            Assert.That(generated, Does.Contain("this.AddIndex(this.Name);"));
        }

        [Test]
        public void Generate_WhenAllColumnTypesAreUsed_Compiles()
        {
            var source = """
                using SqExpress;

                [TableDescriptor("dbo", "EveryType")]
                [BooleanColumn("BooleanValue")]
                [NullableBooleanColumn("NullableBooleanValue")]
                [ByteColumn("ByteValue")]
                [NullableByteColumn("NullableByteValue")]
                [ByteArrayColumn("Blob", MaxLength = 32)]
                [NullableByteArrayColumn("NullableBlob", MaxLength = 64, FixedLength = true)]
                [Int16Column("SmallIntValue")]
                [NullableInt16Column("NullableSmallIntValue")]
                [Int32Column("IntValue")]
                [NullableInt32Column("NullableIntValue")]
                [Int64Column("BigIntValue")]
                [NullableInt64Column("NullableBigIntValue")]
                [DoubleColumn("DoubleValue")]
                [NullableDoubleColumn("NullableDoubleValue")]
                [DecimalColumn("Amount", Precision = 18, Scale = 4)]
                [NullableDecimalColumn("NullableAmount", Precision = 10, Scale = 2)]
                [DateTimeColumn("CreatedOn", IsDate = true)]
                [NullableDateTimeColumn("UpdatedOn")]
                [DateTimeOffsetColumn("OffsetCreatedOn")]
                [NullableDateTimeOffsetColumn("OffsetUpdatedOn")]
                [GuidColumn("GuidValue")]
                [NullableGuidColumn("NullableGuidValue")]
                [StringColumn("Title", MaxLength = 128, Unicode = true)]
                [NullableStringColumn("Body", Text = true)]
                [XmlColumn("Payload")]
                [NullableXmlColumn("NullablePayload")]
                public partial class EveryType
                {
                }
                """;

            var result = RunGenerator(source);

            Assert.That(result.Diagnostics, Is.Empty, FormatDiagnostics(result.Diagnostics));
            Assert.That(result.OutputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty, FormatDiagnostics(result.OutputCompilation.GetDiagnostics()));
        }

        [Test]
        public void Generate_WhenForeignKeyIsDeclared_UsesResolvedTargetProperty()
        {
            var source = """
                using SqExpress;

                [TableDescriptor("dbo", "Company")]
                [Int32Column("CompanyId", Pk = true)]
                public partial class Company
                {
                }

                [TableDescriptor("dbo", "User")]
                [Int32Column("UserId", Pk = true)]
                [NullableInt32Column("CompanyId", FkTable = "Company", FkColumn = "CompanyId")]
                public partial class User
                {
                }
                """;

            var result = RunGenerator(source);
            var generated = GetGeneratedSource(result, "User");

            Assert.That(result.Diagnostics, Is.Empty, FormatDiagnostics(result.Diagnostics));
            Assert.That(generated, Does.Not.Contain("ColumnMeta.ForeignKey<global::User>(t => t.CompanyId)"));
            Assert.That(generated, Does.Contain("ColumnMeta.ForeignKey<global::Company>(t => t.CompanyId)"));
        }

        [Test]
        public void Generate_WhenDescriptorClassIsNotPartial_ReportsDiagnostic()
        {
            var source = """
                using SqExpress;

                [TableDescriptor("dbo", "User")]
                [Int32Column("UserId")]
                public class User
                {
                }
                """;

            var result = RunGenerator(source);

            Assert.That(result.Diagnostics.Select(static d => d.Id), Contains.Item("SQEX101"));
        }

        [Test]
        public void Generate_WhenIndexColumnDoesNotExist_ReportsDiagnostic()
        {
            var source = """
                using SqExpress;

                [TableDescriptor("dbo", "User")]
                [Int32Column("UserId")]
                [Index("Name")]
                public partial class User
                {
                }
                """;

            var result = RunGenerator(source);

            Assert.That(result.Diagnostics.Select(static d => d.Id), Contains.Item("SQEX110"));
        }

        private static GeneratorRunResultData RunGenerator(string source)
        {
            var compilation = CreateCompilation(source);
            var generator = new TableDescriptorSourceGenerator();
            var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
            GeneratorDriver driver = CSharpGeneratorDriver.Create([generator.AsSourceGenerator()], parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var outputDiagnostics);
            var result = driver.GetRunResult();

            return new GeneratorRunResultData(
                result.Results.SelectMany(static r => r.Diagnostics).Concat(outputDiagnostics).Where(static d => d.Severity == DiagnosticSeverity.Error).ToImmutableArray(),
                outputCompilation,
                result.GeneratedTrees.ToImmutableArray());
        }

        private static string GetGeneratedSource(GeneratorRunResultData result, string hintContains)
        {
            return result.GeneratedTrees
                .Select(static t => t.ToString())
                .First(t => t.Contains($"partial class {hintContains}", StringComparison.Ordinal));
        }

        private static CSharpCompilation CreateCompilation(string source)
        {
            return CSharpCompilation.Create(
                assemblyName: "GeneratorTests",
                syntaxTrees: [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
                references: GetMetadataReferences(),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
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
            referencePaths.Add(typeof(TableDescriptorSourceGenerator).GetTypeInfo().Assembly.Location);

            return referencePaths.Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        }

        private static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics)
            => string.Join(Environment.NewLine, diagnostics.Select(static d => d.ToString()));

        private readonly record struct GeneratorRunResultData(
            ImmutableArray<Diagnostic> Diagnostics,
            Compilation OutputCompilation,
            ImmutableArray<SyntaxTree> GeneratedTrees);
    }
}
