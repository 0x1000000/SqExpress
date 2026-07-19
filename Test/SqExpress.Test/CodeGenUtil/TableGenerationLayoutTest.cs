#if NET
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using SqExpress;
using SqExpress.CodeGen.Shared;
using SqExpress.CodeGenUtil;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.Test.CodeGenUtil
{
    [TestFixture]
    public class TableGenerationLayoutTest
    {
        [Test]
        public void SplitBySchema_UsesNormalizedFoldersAndNamespaces()
        {
            var sales = Table("sales-data", "Order", "TableOrder");
            var archive = Table("archive", "Order", "TableOrder");

            var layout = TableGenerationLayout.Create(
                new[] { sales, archive },
                "Tables",
                "MyApp.Tables",
                splitTablesBySchema: true);

            Assert.That(layout.Entries[sales.DbName].FilePath, Is.EqualTo(Path.Combine("Tables", "SalesData", "TableOrder.cs")));
            Assert.That(layout.Entries[sales.DbName].Namespace, Is.EqualTo("MyApp.Tables.SalesData"));
            Assert.That(layout.Entries[archive.DbName].FilePath, Is.EqualTo(Path.Combine("Tables", "Archive", "TableOrder.cs")));
            Assert.That(layout.Entries[archive.DbName].Namespace, Is.EqualTo("MyApp.Tables.Archive"));
        }

        [Test]
        public void SplitBySchema_NormalizedSchemaCollisionFails()
        {
            var exception = Assert.Throws<SqExpressCodeGenException>(() => TableGenerationLayout.Create(
                new[] { Table("sales-data", "One", "TableOne"), Table("sales_data", "Two", "TableTwo") },
                "Tables",
                "MyApp.Tables",
                splitTablesBySchema: true));

            Assert.That(exception!.Message, Does.Contain("both normalize to \"SalesData\""));
        }

        [Test]
        public void WithoutSchemaSplit_DuplicateFileFailsWithGuidance()
        {
            var exception = Assert.Throws<SqExpressCodeGenException>(() => TableGenerationLayout.Create(
                new[] { Table("sales", "Order", "TableOrder"), Table("archive", "Order", "TableOrder") },
                "Tables",
                "MyApp.Tables",
                splitTablesBySchema: false));

            Assert.That(exception!.Message, Does.Contain("--split-tables-by-schema"));
        }

        [Test]
        public void SplitBySchema_AllTablesUsesQualifiedTypesAndSchemaAccessors()
        {
            var sales = Table("sales", "Order", "TableOrder");
            var archive = Table("archive", "Order", "TableOrder");
            var tables = new[] { sales, archive };
            var layout = TableGenerationLayout.Create(tables, "Tables", "MyApp.Tables", splitTablesBySchema: true);

            var source = CodeGenAllTablesSupport.Generate(
                    "AllTables.cs",
                    tables,
                    "MyApp.Tables",
                    "Table",
                    new TestFileSystem(),
                    ToNamespaces(layout),
                    ToSchemaSegments(layout))
                .ToFullString();

            Assert.That(source, Does.Contain("GetSalesOrder"));
            Assert.That(source, Does.Contain("GetArchiveOrder"));
            Assert.That(source, Does.Contain("global::MyApp.Tables.Sales.TableOrder"));
            Assert.That(source, Does.Contain("global::MyApp.Tables.Archive.TableOrder"));
        }

        [Test]
        public void SplitBySchema_CrossSchemaForeignKeyDescriptorsAndDeclarationsCompile()
        {
            var archive = CodeGenTable("archive", "Order", "MyApp.Tables.Archive", foreignKeySchema: null);
            var sales = CodeGenTable("sales", "Order", "MyApp.Tables.Sales", foreignKeySchema: "archive");
            var allTables = new Dictionary<string, CodeGenTableModel>(System.StringComparer.OrdinalIgnoreCase)
            {
                [archive.TableKey] = archive,
                [sales.TableKey] = sales
            };

            var descriptors = new[]
            {
                CodeGenTableDescriptorSupport.GenerateTableDescriptor(archive, allTables, CodeGenTableDescriptorRenderOptions.PublicPartial),
                CodeGenTableDescriptorSupport.GenerateTableDescriptor(sales, allTables, CodeGenTableDescriptorRenderOptions.PublicPartial)
            };
            AssertCompiles(descriptors.Select(static source => CSharpSyntaxTree.Create(source)));
            Assert.That(descriptors[1].ToFullString(), Does.Contain("ForeignKey<MyApp.Tables.Archive.TableOrder>"));

            var declarations = new[]
            {
                CodeGenTableDescriptorSupport.GenerateTableDeclaration(archive, allTables),
                CodeGenTableDescriptorSupport.GenerateTableDeclaration(sales, allTables)
            };
            AssertCompiles(declarations.Select(static source => CSharpSyntaxTree.Create(source)));
        }

        private static Dictionary<TableRef, string> ToNamespaces(TableGenerationLayout layout)
        {
            var result = new Dictionary<TableRef, string>();
            foreach (var pair in layout.Entries)
            {
                result.Add(pair.Key, pair.Value.Namespace);
            }

            return result;
        }

        private static Dictionary<TableRef, string> ToSchemaSegments(TableGenerationLayout layout)
        {
            var result = new Dictionary<TableRef, string>();
            foreach (var pair in layout.Entries)
            {
                result.Add(pair.Key, pair.Value.SchemaSegment!);
            }

            return result;
        }

        private static TableModel Table(string schema, string name, string className)
            => new TableModel(className, new TableRef(schema, name), new List<ColumnModel>(), new List<IndexModel>());

        private static CodeGenTableModel CodeGenTable(string schema, string name, string @namespace, string? foreignKeySchema)
        {
            return new CodeGenTableModel(
                CodeGenTableKind.Table,
                databaseName: null,
                schemaName: schema,
                tableName: name,
                className: "TableOrder",
                @namespace: @namespace,
                fullyQualifiedTypeName: @namespace + ".TableOrder",
                columns: ImmutableArray.Create(new CodeGenColumnModel(
                    CodeGenColumnKind.Int32,
                    "Id",
                    propertyName: null,
                    isPrimaryKey: true,
                    isIdentity: false,
                    foreignKeyDatabase: null,
                    foreignKeySchema: foreignKeySchema,
                    foreignKeyTable: foreignKeySchema == null ? null : "Order",
                    foreignKeyColumn: foreignKeySchema == null ? null : "Id",
                    defaultValueKind: CodeGenDefaultValueKind.None,
                    defaultValue: null,
                    isUnicode: false,
                    maxLength: null,
                    isFixedLength: false,
                    isText: false,
                    precision: 0,
                    scale: 0,
                    isDate: false)),
                indexes: ImmutableArray<CodeGenIndexModel>.Empty);
        }

        private static void AssertCompiles(IEnumerable<SyntaxTree> syntaxTrees)
        {
            var trustedAssemblies = ((string)System.AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Select(static path => MetadataReference.CreateFromFile(path))
                .Cast<MetadataReference>()
                .ToList();
            trustedAssemblies.Add(MetadataReference.CreateFromFile(typeof(TableBase).Assembly.Location));

            var result = CSharpCompilation.Create(
                    "SchemaSplitTables",
                    syntaxTrees,
                    trustedAssemblies,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .Emit(Stream.Null);

            Assert.That(
                result.Success,
                Is.True,
                string.Join(System.Environment.NewLine, result.Diagnostics.Where(static d => d.Severity == DiagnosticSeverity.Error)));
        }
    }
}
#endif
