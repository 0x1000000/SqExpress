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
                [Int32Column("UserId", Pk = true, Identity = true, DefaultValue = "1")]
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
            Assert.That(generated, Does.Contain("using SqExpress;"));
            Assert.That(generated, Does.Contain("using SqExpress.Syntax.Type;"));
            Assert.That(generated, Does.Contain("partial class User : TableBase"));
            Assert.That(generated, Does.Contain("public User() : this(alias: SqExpress.Alias.Auto)"));
            Assert.That(generated, Does.Contain("public User(Alias alias) : base(\"dbo\", \"User\", alias)"));
            Assert.That(generated, Does.Contain("this.UserId = this.CreateInt32Column(\"UserId\", ColumnMeta.PrimaryKey().Identity().DefaultValue(1));"));
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
                [StringColumn("Code", MaxLength = 16, Unicode = false, FixedLength = true)]
                [NullableStringColumn("Body", Text = true)]
                [XmlColumn("Payload")]
                [NullableXmlColumn("NullablePayload")]
                public partial class EveryType
                {
                }
                """;

            var result = RunGenerator(source);
            var generated = GetGeneratedSource(result, "EveryType");

            Assert.That(result.Diagnostics, Is.Empty, FormatDiagnostics(result.Diagnostics));
            Assert.That(result.OutputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty, FormatDiagnostics(result.OutputCompilation.GetDiagnostics()));
            Assert.That(generated, Does.Contain("this.Title = this.CreateStringColumn(name: \"Title\", size: 128, isUnicode: true, isText: false, columnMeta: null);"));
            Assert.That(generated, Does.Contain("this.Code = this.CreateFixedSizeStringColumn(name: \"Code\", size: 16, isUnicode: false, columnMeta: null);"));
            Assert.That(generated, Does.Not.Contain("this.Code = this.CreateFixedSizeStringColumn(name: \"Code\", size: 16, isUnicode: false, isText: false, columnMeta: null);"));
            Assert.That(generated, Does.Contain("this.NullableBlob = this.CreateNullableFixedSizeByteArrayColumn(\"NullableBlob\", 64, null);"));
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
            Assert.That(generated, Does.Not.Contain("ColumnMeta.ForeignKey<User>(t => t.CompanyId)"));
            Assert.That(generated, Does.Contain("ColumnMeta.ForeignKey<Company>(t => t.CompanyId)"));
        }

        [Test]
        public void Generate_WhenPredefinedDefaultsAreUsed_UsesExpectedExpressions()
        {
            var source = """
                using SqExpress;

                [TableDescriptor("dbo", "Audit")]
                [NullableDateTimeColumn("CreatedUtc", DefaultValue = "$utcNow")]
                [NullableDateTimeColumn("CreatedLocal", DefaultValue = "$now")]
                [NullableStringColumn("DeletedBy", DefaultValue = "$null")]
                public partial class Audit
                {
                }
                """;

            var result = RunGenerator(source);
            var generated = GetGeneratedSource(result, "Audit");

            Assert.That(result.Diagnostics, Is.Empty, FormatDiagnostics(result.Diagnostics));
            Assert.That(generated, Does.Contain("this.CreatedUtc = this.CreateNullableDateTimeColumn(\"CreatedUtc\", false, ColumnMeta.DefaultValue(SqQueryBuilder.GetUtcDate()));"));
            Assert.That(generated, Does.Contain("this.CreatedLocal = this.CreateNullableDateTimeColumn(\"CreatedLocal\", false, ColumnMeta.DefaultValue(SqQueryBuilder.GetDate()));"));
            Assert.That(generated, Does.Contain("this.DeletedBy = this.CreateNullableStringColumn(name: \"DeletedBy\", size: null, isUnicode: false, isText: false, columnMeta: ColumnMeta.DefaultValue(SqQueryBuilder.Null));"));
        }

        [Test]
        public void Generate_WhenDefaultValueCannotBeParsed_ReportsDiagnostic()
        {
            var source = """
                using SqExpress;

                [TableDescriptor("dbo", "User")]
                [Int32Column("UserId", DefaultValue = "abc")]
                public partial class User
                {
                }
                """;

            var result = RunGenerator(source);

            Assert.That(result.Diagnostics.Select(static d => d.Id), Contains.Item("SQEX114"));
            Assert.That(FormatDiagnostics(result.Diagnostics), Does.Contain("invalid for Int32Column"));
            Assert.That(FormatDiagnostics(result.Diagnostics), Does.Contain("Supported predefined values for this column: $null"));
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
