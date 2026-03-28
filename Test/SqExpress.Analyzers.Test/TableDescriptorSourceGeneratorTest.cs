using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
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
                using SqExpress.TableDecalationAttributes;

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
        public void Generate_WhenTempTableDescriptorIsSimple_EmitsTempTableBasePattern()
        {
            var source = """
                using SqExpress.TableDecalationAttributes;

                [TempTableDescriptor("#UserTemp")]
                [Int32Column("UserId", Pk = true, Identity = true)]
                [StringColumn("Name", Unicode = true, MaxLength = 255)]
                [Index("Name")]
                public partial class UserTemp
                {
                }
                """;

            var result = RunGenerator(source);
            var generated = GetGeneratedSource(result, "UserTemp");

            Assert.That(result.Diagnostics, Is.Empty, FormatDiagnostics(result.Diagnostics));
            Assert.That(generated, Does.Contain("partial class UserTemp : TempTableBase"));
            Assert.That(generated, Does.Contain("public UserTemp() : this(alias: SqExpress.Alias.Auto)"));
            Assert.That(generated, Does.Contain("public UserTemp(Alias alias) : base(\"#UserTemp\", alias)"));
            Assert.That(generated, Does.Contain("this.UserId = this.CreateInt32Column(\"UserId\", ColumnMeta.PrimaryKey().Identity());"));
            Assert.That(generated, Does.Contain("this.AddIndex(this.Name);"));
        }

        [Test]
        public void Generate_WhenAllColumnTypesAreUsed_Compiles()
        {
            var source = """
                using SqExpress.TableDecalationAttributes;

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
                using SqExpress.TableDecalationAttributes;

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
                using SqExpress.TableDecalationAttributes;

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
        public void Generate_WhenRawDefaultsAreUsed_UsesUnsafeValueExpression()
        {
            var source = """
                using SqExpress.TableDecalationAttributes;

                [TableDescriptor("dbo", "Audit")]
                [NullableDateTimeColumn("CreatedUtc", DefaultValue = "$raw((sysutcdatetime()))")]
                [GuidColumn("Token", DefaultValue = "$RAW(newid())")]
                [Int32Column("Version", DefaultValue = "$raw(1+2)")]
                public partial class Audit
                {
                }
                """;

            var result = RunGenerator(source);
            var generated = GetGeneratedSource(result, "Audit");

            Assert.That(result.Diagnostics, Is.Empty, FormatDiagnostics(result.Diagnostics));
            Assert.That(generated, Does.Contain("this.CreatedUtc = this.CreateNullableDateTimeColumn(\"CreatedUtc\", false, ColumnMeta.DefaultValue(SqQueryBuilder.UnsafeValue(\"(sysutcdatetime())\")));"));
            Assert.That(generated, Does.Contain("this.Token = this.CreateGuidColumn(\"Token\", ColumnMeta.DefaultValue(SqQueryBuilder.UnsafeValue(\"newid()\")));"));
            Assert.That(generated, Does.Contain("this.Version = this.CreateInt32Column(\"Version\", ColumnMeta.DefaultValue(SqQueryBuilder.UnsafeValue(\"1+2\")));"));
        }

        [Test]
        public void Generate_WhenDefaultValueCannotBeParsed_ReportsDiagnostic()
        {
            var source = """
                using SqExpress.TableDecalationAttributes;

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
            var diagnostic = result.Diagnostics.First(static d => d.Id == "SQEX114");
            Assert.That(diagnostic.Location.GetLineSpan().StartLinePosition.Line, Is.EqualTo(3));
        }

        [TestCase("$raw(")]
        [TestCase("$raw")]
        [TestCase("$raw(value")]
        public void Generate_WhenRawDefaultValueIsMalformed_ReportsDiagnostic(string defaultValue)
        {
            var source = $$"""
                using SqExpress.TableDecalationAttributes;

                [TableDescriptor("dbo", "User")]
                [StringColumn("Name", Unicode = true, MaxLength = 50, DefaultValue = "{{defaultValue}}")]
                public partial class User
                {
                }
                """;

            var result = RunGenerator(source);

            Assert.That(result.Diagnostics.Select(static d => d.Id), Contains.Item("SQEX114"));
            Assert.That(FormatDiagnostics(result.Diagnostics), Does.Contain("Supported predefined values for this column: $null, $raw(...)"));
        }

        [Test]
        public void Generate_WhenDescriptorClassIsNotPartial_ReportsDiagnostic()
        {
            var source = """
                using SqExpress.TableDecalationAttributes;

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
                using SqExpress.TableDecalationAttributes;

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

        [Test]
        public void Generate_WhenBothTableDescriptorKindsAreDeclared_ReportsDiagnostic()
        {
            var source = """
                using SqExpress.TableDecalationAttributes;

                [TableDescriptor("dbo", "User")]
                [TempTableDescriptor("#UserTemp")]
                [Int32Column("UserId")]
                public partial class User
                {
                }
                """;

            var result = RunGenerator(source);

            Assert.That(result.Diagnostics.Select(static d => d.Id), Contains.Item("SQEX115"));
        }

        [Test]
        public void Generate_WhenDescriptorHasSqModel_GeneratesRecordWithoutWithMethods()
        {
            var source = """
                using SqExpress.TableDecalationAttributes;

                [TableDescriptor("dbo", "User", SqModel = "UserDto")]
                [Int32Column("UserId", Pk = true, Identity = true)]
                [StringColumn("FirstName", Unicode = true, MaxLength = 255)]
                [NullableStringColumn("LastName", Unicode = true, MaxLength = 255)]
                public partial class TableUser
                {
                }
                """;

            var result = RunGenerator(source);
            var generated = GetGeneratedSource(result, "UserDto");

            Assert.That(result.Diagnostics, Is.Empty, FormatDiagnostics(result.Diagnostics));
            Assert.That(generated, Does.Contain("partial record UserDto"));
            Assert.That(generated, Does.Contain("public static UserDto Read("));
            Assert.That(generated, Does.Contain("public static TableColumn[] GetColumns("));
            Assert.That(generated, Does.Contain("public static ISqModelReader<UserDto, TableUser> GetReader()"));
            Assert.That(generated, Does.Not.Contain("WithUserId("));
            Assert.That(generated, Does.Not.Contain("WithFirstName("));
        }

        [Test]
        public void Generate_WhenTempTableDescriptorHasSqModel_GeneratesModel()
        {
            var source = """
                using SqExpress.TableDecalationAttributes;

                [TempTableDescriptor("#TmpUser", SqModel = "TmpUserDto")]
                [Int32Column("UserId")]
                [StringColumn("FirstName", Unicode = true, MaxLength = 255)]
                public partial class TmpUser
                {
                }
                """;

            var result = RunGenerator(source);
            var generated = GetGeneratedSource(result, "TmpUserDto");

            Assert.That(result.Diagnostics, Is.Empty, FormatDiagnostics(result.Diagnostics));
            Assert.That(generated, Does.Contain("partial record TmpUserDto"));
            Assert.That(generated, Does.Contain("public static TmpUserDto Read("));
            Assert.That(generated, Does.Contain("GetReader()"));
        }

        [Test]
        public void Generate_WhenColumnSqModelsAddsAndRenamesMembership_DedupesCleanly()
        {
            var source = """
                using SqExpress.TableDecalationAttributes;

                [TableDescriptor("dbo", "User", SqModel = "UserDto")]
                [Int32Column("UserId", SqModels = "UserDto.Id,UserIdentity")]
                [StringColumn("FirstName", Unicode = true, MaxLength = 255, SqModels = "UserIdentity")]
                public partial class TableUser
                {
                }
                """;

            var result = RunGenerator(source);
            var dtoGenerated = GetGeneratedSource(result, "UserDto");
            var identityGenerated = GetGeneratedSource(result, "UserIdentity");

            Assert.That(result.Diagnostics, Is.Empty, FormatDiagnostics(result.Diagnostics));
            Assert.That(dtoGenerated, Does.Contain("public int Id { get; }"));
            Assert.That(dtoGenerated, Does.Not.Contain("public int UserId { get; }"));
            Assert.That(identityGenerated, Does.Contain("partial record UserIdentity"));
            Assert.That(identityGenerated, Does.Contain("public int UserId { get; }"));
            Assert.That(identityGenerated, Does.Contain("public string FirstName { get; }"));
        }

        [Test]
        public void Generate_WhenSqModelsEntryIsMalformed_ReportsDiagnostic()
        {
            var source = """
                using SqExpress.TableDecalationAttributes;

                [TableDescriptor("dbo", "User")]
                [Int32Column("UserId", SqModels = "UserDto.")]
                public partial class TableUser
                {
                }
                """;

            var result = RunGenerator(source);

            Assert.That(result.Diagnostics.Select(static d => d.Id), Contains.Item("SQEX117"));
        }

        [Test]
        public void Generate_WhenSameModelPropertyHasConflictingTypes_ReportsDiagnostic()
        {
            var source = """
                using SqExpress.TableDecalationAttributes;

                [TableDescriptor("dbo", "User")]
                [Int32Column("UserId", SqModels = "UserDto.Id")]
                [StringColumn("UserCode", Unicode = true, MaxLength = 50, SqModels = "UserDto.Id")]
                public partial class TableUser
                {
                }
                """;

            var result = RunGenerator(source);

            Assert.That(result.Diagnostics.Select(static d => d.Id), Contains.Item("SQEX119"));
        }

        [Test]
        public void Generate_WhenSameTableMapsTwoColumnsToSameModelProperty_ReportsDiagnostic()
        {
            var source = """
                using SqExpress.TableDecalationAttributes;

                [TableDescriptor("dbo", "User")]
                [Int32Column("UserId", SqModels = "UserDto.Id")]
                [Int32Column("OtherId", SqModels = "UserDto.Id")]
                public partial class TableUser
                {
                }
                """;

            var result = RunGenerator(source);

            Assert.That(result.Diagnostics.Select(static d => d.Id), Contains.Item("SQEX120"));
        }

        [Test]
        public void Generate_WhenSqModelOptionsAreProvided_HonorsNamespaceAndImmutableClassMode()
        {
            var source = """
                using SqExpress.TableDecalationAttributes;

                [TableDescriptor("dbo", "User", SqModel = "UserDto")]
                [Int32Column("UserId")]
                [StringColumn("FirstName", Unicode = true, MaxLength = 255)]
                public partial class TableUser
                {
                }
                """;

            var result = RunGenerator(
                source,
                ImmutableDictionary<string, string>.Empty
                    .Add("build_property.SqModelGenNamespace", "MyApp.GeneratedModels")
                    .Add("build_property.SqModelGenType", "ImmutableClass"));
            var generated = GetGeneratedSource(result, "UserDto");

            Assert.That(result.Diagnostics, Is.Empty, FormatDiagnostics(result.Diagnostics));
            Assert.That(generated, Does.Contain("namespace MyApp.GeneratedModels"));
            Assert.That(generated, Does.Contain("partial class UserDto"));
            Assert.That(generated, Does.Contain("public UserDto WithUserId("));
            Assert.That(generated, Does.Contain("public UserDto WithFirstName("));
        }

        private static GeneratorRunResultData RunGenerator(string source, ImmutableDictionary<string, string>? globalOptions = null)
        {
            var compilation = CreateCompilation(source);
            var generator = new TableDescriptorSourceGenerator();
            var parseOptions = (CSharpParseOptions)compilation.SyntaxTrees[0].Options;
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: [generator.AsSourceGenerator()],
                parseOptions: parseOptions,
                optionsProvider: new TestAnalyzerConfigOptionsProvider(globalOptions ?? ImmutableDictionary<string, string>.Empty));
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
                .First(t =>
                    t.Contains($"partial class {hintContains}", StringComparison.Ordinal) ||
                    t.Contains($"class {hintContains}", StringComparison.Ordinal) ||
                    t.Contains($"record {hintContains}", StringComparison.Ordinal));
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

        private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
        {
            private readonly AnalyzerConfigOptions _globalOptions;

            public TestAnalyzerConfigOptionsProvider(ImmutableDictionary<string, string> values)
            {
                this._globalOptions = new TestAnalyzerConfigOptions(values);
            }

            public override AnalyzerConfigOptions GlobalOptions => this._globalOptions;

            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
            {
                return Empty;
            }

            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
            {
                return Empty;
            }

            private static AnalyzerConfigOptions Empty { get; } = new TestAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty);
        }

        private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            private readonly ImmutableDictionary<string, string> _values;

            public TestAnalyzerConfigOptions(ImmutableDictionary<string, string> values)
            {
                this._values = values;
            }

            public override bool TryGetValue(string key, out string value)
            {
                return this._values.TryGetValue(key, out value!);
            }
        }
    }
}
