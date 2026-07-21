#if NET
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SqExpress.CodeGenUtil;
using SqExpress.CodeGenUtil.Ef;

namespace SqExpress.Test.CodeGenUtil;

[TestFixture]
public class EfMetadataExtractorRunnerTest
{
    [Test]
    public void ExtractorProject_DisablesGenerationWithoutExcludingExistingDescriptors()
    {
        var project = EfMetadataExtractorRunner.CreateExtractorProject(
            @"C:\Project & Tests\Data.csproj",
            "net8.0");

        Assert.That(project, Does.Contain("Properties=\"SqEfTablesGenEnable=false\""));
        Assert.That(project, Does.Not.Contain("SqEfTablesGenExcludeOutputFromCompile"));
        Assert.That(project, Does.Contain(@"C:\Project &amp; Tests\Data.csproj"));
    }

    [Test]
    public void BuildExtractor_NormalBuildIncludesDescriptorsAndDoesNotRetry()
    {
        var calls = new List<string>();

        var outputExcluded = EfMetadataExtractorRunner.BuildExtractor(
            "extractor.csproj",
            "net8.0",
            "work",
            Run);

        Assert.IsFalse(outputExcluded);
        Assert.AreEqual(1, calls.Count);
        Assert.That(calls[0], Does.Contain("SqEfTablesGenEnable=false"));
        Assert.That(calls[0], Does.Not.Contain("SqEfTablesGenExcludeOutputFromCompile"));

        int Run(string fileName, string arguments, string workingDirectory, out string output)
        {
            calls.Add(arguments);
            output = "";
            return 0;
        }
    }

    [Test]
    public void BuildExtractor_RetriesWithDescriptorsExcludedAfterFailure()
    {
        var calls = new List<string>();

        var outputExcluded = EfMetadataExtractorRunner.BuildExtractor(
            "extractor.csproj",
            "net8.0",
            "work",
            Run);

        Assert.IsTrue(outputExcluded);
        Assert.AreEqual(2, calls.Count);
        Assert.That(calls[0], Does.Not.Contain("SqEfTablesGenExcludeOutputFromCompile"));
        Assert.That(calls[1], Does.Contain("SqEfTablesGenExcludeOutputFromCompile=true"));

        int Run(string fileName, string arguments, string workingDirectory, out string output)
        {
            calls.Add(arguments);
            output = calls.Count == 1 ? "normal failure" : "";
            return calls.Count == 1 ? 1 : 0;
        }
    }

    [Test]
    public void BuildExtractor_WhenBothBuildsFailReportsBothDiagnostics()
    {
        var callCount = 0;

        var exception = Assert.Throws<SqExpressCodeGenException>(() =>
            EfMetadataExtractorRunner.BuildExtractor(
                "extractor.csproj",
                "net8.0",
                "work",
                Run));

        Assert.AreEqual(2, callCount);
        Assert.That(exception!.Message, Does.Contain("Normal build (existing table descriptors included):"));
        Assert.That(exception.Message, Does.Contain("normal failure"));
        Assert.That(exception.Message, Does.Contain("Fallback build (generated table output excluded):"));
        Assert.That(exception.Message, Does.Contain("fallback failure"));

        int Run(string fileName, string arguments, string workingDirectory, out string output)
        {
            callCount++;
            output = callCount == 1 ? "normal failure" : "fallback failure";
            return 1;
        }
    }

    [Test]
    public void ExtractorProgramSource_CompilesAndReadsEfMetadata()
    {
        var targetAssembly = CompileAssembly(
            "SqExpress.EfExtractorTarget",
            TargetDbContextSource,
            OutputKind.DynamicallyLinkedLibrary,
            GetReferences(
                typeof(object),
                typeof(Enumerable),
                typeof(DbContext),
                typeof(SqlServerDbContextOptionsExtensions)
            )
        );

        var loadedTargetAssembly = Assembly.Load(targetAssembly);

        var extractorAssemblyBytes = CompileAssembly(
            "SqExpress.EfExtractorProgram",
            EfMetadataExtractorRunner.ExtractorProgramSource,
            OutputKind.ConsoleApplication,
            GetReferences(
                typeof(object),
                typeof(Console),
                typeof(Enumerable),
                typeof(JsonSerializer),
                typeof(IServiceProvider)
            )
        );

        var extractorAssembly = Assembly.Load(extractorAssemblyBytes);
        var originalOut = Console.Out;
        using var output = new StringWriter();

        try
        {
            AssemblyLoadContext.Default.Resolving += ResolveTargetAssembly;
            Console.SetOut(output);
            InvokeEntryPoint(
                extractorAssembly,
                new[]
                {
                    "--target-assembly",
                    "SqExpress.EfExtractorTarget",
                    "--db-context",
                    "TestDbContext"
                }
            );

            using var document = JsonDocument.Parse(output.ToString());
            var root = document.RootElement;
            Assert.That(root.GetProperty("ProviderName").GetString(), Does.Contain("SqlServer"));

            var table = root.GetProperty("Tables").EnumerateArray().Single();
            Assert.AreEqual("sales", table.GetProperty("Schema").GetString());
            Assert.AreEqual("Customers", table.GetProperty("Name").GetString());

            var columns = table.GetProperty("Columns")
                .EnumerateArray()
                .ToDictionary(c => c.GetProperty("Name").GetString()!);
            Assert.That(columns.Keys, Is.EquivalentTo(new[] { "CustomerId", "DisplayName", "CreatedUtc" }));
            Assert.AreEqual("int", columns["CustomerId"].GetProperty("StoreType").GetString());
            Assert.IsTrue(columns["CustomerId"].GetProperty("Identity").GetBoolean());
            Assert.AreEqual(0, columns["CustomerId"].GetProperty("PrimaryKeyIndex").GetInt32());
            Assert.AreEqual("varchar(64)", columns["DisplayName"].GetProperty("StoreType").GetString());
            Assert.AreEqual(64, columns["DisplayName"].GetProperty("MaxLength").GetInt32());
            Assert.IsFalse(columns["DisplayName"].GetProperty("Nullable").GetBoolean());
            Assert.AreEqual("String", columns["DisplayName"].GetProperty("DefaultValueKind").GetString());
            Assert.AreEqual("anonymous", columns["DisplayName"].GetProperty("DefaultValue").GetString());
            Assert.AreEqual("GetUtcDate", columns["CreatedUtc"].GetProperty("DefaultValueKind").GetString());

            var index = table.GetProperty("Indexes").EnumerateArray().Single();
            Assert.IsTrue(index.GetProperty("Unique").GetBoolean());
            Assert.AreEqual("IX_Customers_DisplayName", index.GetProperty("Name").GetString());
            Assert.AreEqual(
                "DisplayName",
                index.GetProperty("Columns").EnumerateArray().Single().GetProperty("Name").GetString()
            );
        }
        finally
        {
            Console.SetOut(originalOut);
            AssemblyLoadContext.Default.Resolving -= ResolveTargetAssembly;
        }

        Assembly? ResolveTargetAssembly(AssemblyLoadContext context, AssemblyName assemblyName)
            => string.Equals(assemblyName.Name, loadedTargetAssembly.GetName().Name, StringComparison.Ordinal)
                ? loadedTargetAssembly
                : null;
    }

    [Test]
    public void MetadataTableReader_SkippedColumnRemovesIncompletePrimaryKeyAndIndexes()
    {
        var document = new EfMetadataDocument
        {
            ProviderName = "Microsoft.EntityFrameworkCore.SqlServer",
            Tables = new List<EfTableMetadata>
            {
                new EfTableMetadata
                {
                    Schema = "dbo",
                    Name = "Items",
                    Columns = new List<EfColumnMetadata>
                    {
                        new EfColumnMetadata
                        {
                            Name = "TenantId",
                            StoreType = "int",
                            ClrType = "System.Int32",
                            PrimaryKeyIndex = 0
                        },
                        new EfColumnMetadata
                        {
                            Name = "Path",
                            StoreType = "hierarchyid",
                            ClrType = "Microsoft.EntityFrameworkCore.HierarchyId",
                            PrimaryKeyIndex = 1
                        },
                        new EfColumnMetadata
                        {
                            Name = "Name",
                            StoreType = "nvarchar(100)",
                            ClrType = "System.String",
                            MaxLength = 100
                        }
                    },
                    Indexes = new List<EfIndexMetadata>
                    {
                        new EfIndexMetadata
                        {
                            Name = "IX_Items_TenantId_Path",
                            Columns = new List<EfIndexColumnMetadata>
                            {
                                new EfIndexColumnMetadata { Name = "TenantId" },
                                new EfIndexColumnMetadata { Name = "Path" }
                            }
                        },
                        new EfIndexMetadata
                        {
                            Name = "IX_Items_Name",
                            Columns = new List<EfIndexColumnMetadata>
                            {
                                new EfIndexColumnMetadata { Name = "Name" }
                            }
                        }
                    }
                }
            }
        };

        var table = EfMetadataTableReader.SelectTables(document, "Table", skipUnknownColumnTypes: true).Single();

        Assert.That(table.Columns.Select(c => c.DbName.Name), Is.EqualTo(new[] { "TenantId", "Name" }));
        Assert.IsNull(table.Columns.Single(c => c.DbName.Name == "TenantId").Pk);
        Assert.That(table.Indexes.Select(i => i.Name), Is.EqualTo(new[] { "IX_Items_Name" }));
        Assert.That(table.Indexes.Single().Columns.Select(c => c.DbName.Name), Is.EqualTo(new[] { "Name" }));
    }

    [Test]
    public void ExtractorProgramSource_KnownErrorsAreWrittenWithoutUnhandledException()
    {
        var extractorAssembly = Assembly.Load(CompileAssembly(
            "SqExpress.EfExtractorProgram.KnownError",
            EfMetadataExtractorRunner.ExtractorProgramSource,
            OutputKind.ConsoleApplication,
            GetReferences(
                typeof(object),
                typeof(Console),
                typeof(Enumerable),
                typeof(JsonSerializer),
                typeof(IServiceProvider)
            )
        ));

        var originalError = Console.Error;
        using var error = new StringWriter();
        try
        {
            Console.SetError(error);
            var exitCode = InvokeEntryPointForExitCode(extractorAssembly, Array.Empty<string>());
            Assert.AreEqual(1, exitCode);
        }
        finally
        {
            Console.SetError(originalError);
        }

        Assert.AreEqual(
            "Usage: --target-assembly <name> [--db-context <type>]" + Environment.NewLine,
            error.ToString());
        Assert.That(error.ToString(), Does.Not.Contain("Unhandled exception"));
    }

    private static byte[] CompileAssembly(
        string assemblyName,
        string source,
        OutputKind outputKind,
        IEnumerable<MetadataReference> references)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)) },
            references,
            new CSharpCompilationOptions(outputKind, nullableContextOptions: NullableContextOptions.Enable)
        );

        using var output = new MemoryStream();
        var result = compilation.Emit(output);
        if (!result.Success)
        {
            Assert.Fail(
                string.Join(Environment.NewLine, result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            );
        }

        return output.ToArray();
    }

    private static IReadOnlyList<MetadataReference> GetReferences(params Type[] anchorTypes)
    {
        var assemblyPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddAssembly(Assembly assembly)
        {
            if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            {
                assemblyPaths.Add(assembly.Location);
            }
        }

        AddAssembly(Assembly.Load("netstandard, Version=2.0.0.0"));
        AddAssembly(Assembly.Load("System.Runtime"));
        foreach (var anchorType in anchorTypes)
        {
            AddAssembly(anchorType.Assembly);
            foreach (var referencedAssemblyName in anchorType.Assembly.GetReferencedAssemblies())
            {
                try
                {
                    AddAssembly(Assembly.Load(referencedAssemblyName));
                }
                catch
                {
                }
            }
        }

        return assemblyPaths.Select(path => MetadataReference.CreateFromFile(path)).ToList();
    }

    private static void InvokeEntryPoint(Assembly assembly, string[] args)
    {
        var exitCode = InvokeEntryPointForExitCode(assembly, args);
        if (exitCode != 0)
        {
            Assert.Fail("Extractor returned exit code " + exitCode + ".");
        }
    }

    private static int InvokeEntryPointForExitCode(Assembly assembly, string[] args)
    {
        var entryPoint = assembly.EntryPoint ??
                         throw new InvalidOperationException(
                             "Compiled extractor assembly does not have an entry point."
                         );
        var result = entryPoint.GetParameters().Length == 0
            ? entryPoint.Invoke(null, Array.Empty<object>())
            : entryPoint.Invoke(null, new object[] { args });

        switch (result)
        {
            case null:
                return 0;
            case int exitCode when exitCode == 0:
                return 0;
            case int exitCode:
                return exitCode;
            case System.Threading.Tasks.Task task:
                task.GetAwaiter().GetResult();
                return 0;
            default:
                return 0;
        }
    }

    private const string TargetDbContextSource = @"
using System;
using Microsoft.EntityFrameworkCore;

public sealed class TestDbContext : DbContext
{
    public DbSet<Customer> Customers => this.Set<Customer>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(""Server=(local);Database=SqExpressExtractorTest;Trusted_Connection=True;TrustServerCertificate=True"");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable(""Customers"", ""sales"");
            entity.HasKey(e => e.CustomerId);
            entity.Property(e => e.CustomerId).UseIdentityColumn();
            entity.Property(e => e.DisplayName)
                .HasColumnType(""varchar(64)"")
                .HasMaxLength(64)
                .IsUnicode(false)
                .IsRequired()
                .HasDefaultValue(""anonymous"");
            entity.Property(e => e.CreatedUtc)
                .HasDefaultValueSql(""sysutcdatetime()"");
            entity.HasIndex(e => e.DisplayName)
                .HasDatabaseName(""IX_Customers_DisplayName"")
                .IsUnique();
        });
    }
}

public sealed class Customer
{
    public int CustomerId { get; set; }
    public string DisplayName { get; set; } = """";
    public DateTime CreatedUtc { get; set; }
}
";
}
#endif
