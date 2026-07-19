#if NET
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SqExpress.CodeGenUtil;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.Test.CodeGenUtil
{
    [TestFixture]
    public class TableOutputCleanerTest
    {
        [Test]
        public void Clean_RemovesObsoleteDescriptorsAndPreservesOtherCode()
        {
            var root = Path.Combine(Path.GetTempPath(), "SqExpressTableOutputCleaner", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                var currentRef = new TableRef("sales", "Order");
                var currentPath = Path.Combine(root, "Sales", "TableOrder.cs");
                Write(currentPath, DirectDescriptor("sales", "Order", "TableOrder"));

                var staleDirectory = Path.Combine(root, "Old");
                var stalePath = Path.Combine(staleDirectory, "TableOrder.cs");
                Write(stalePath, DirectDescriptor("sales", "Order", "TableOrder"));

                var missingDirectPath = Path.Combine(root, "TableMissing.cs");
                Write(missingDirectPath, DirectDescriptor("sales", "Missing", "TableMissing"));

                var mixedPath = Path.Combine(root, "Mixed.cs");
                Write(mixedPath,
                    "using SqExpress.TableDeclarationAttributes;\n" +
                    "[TableDescriptor(\"archive\", \"Log\")] public partial class TableLog { }\n" +
                    "public class KeepMe { }\n");

                var obsoleteOnlyPath = Path.Combine(root, "TableAudit.cs");
                Write(obsoleteOnlyPath,
                    "using SqExpress.TableDeclarationAttributes;\n" +
                    "[TableDescriptorAttribute(\"archive\", \"Audit\")] public partial class TableAudit { }\n");

                var unrelatedPath = Path.Combine(root, "Custom.cs");
                Write(unrelatedPath, "public class Custom { }\n");
                var unrecognizedPath = Path.Combine(root, "ManualTable.cs");
                Write(unrecognizedPath, "public class ManualTable : TableBase { }\n");
                var allTablesPath = Path.Combine(root, "AllTables.cs");
                Write(allTablesPath, "public static class AllTables { }\n");

                var layout = new Dictionary<TableRef, TableGenerationLayoutEntry>
                {
                    [currentRef] = new TableGenerationLayoutEntry(currentPath, "MyApp.Tables.Sales", "Sales")
                };

                TableOutputCleaner.Clean(root, layout);

                Assert.That(File.Exists(currentPath), Is.True);
                Assert.That(File.Exists(stalePath), Is.False);
                Assert.That(Directory.Exists(staleDirectory), Is.False);
                Assert.That(File.Exists(missingDirectPath), Is.False);
                Assert.That(File.Exists(obsoleteOnlyPath), Is.False);
                Assert.That(File.ReadAllText(mixedPath), Does.Not.Contain("TableLog"));
                Assert.That(File.ReadAllText(mixedPath), Does.Contain("class KeepMe"));
                Assert.That(File.Exists(unrelatedPath), Is.True);
                Assert.That(File.Exists(unrecognizedPath), Is.True);
                Assert.That(File.Exists(allTablesPath), Is.True);
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
        }

        private static string DirectDescriptor(string schema, string table, string className)
            => $"public class {className} : TableBase {{ public {className}(Alias alias) : base(\"{schema}\", \"{table}\", alias) {{ }} }}\n";

        private static void Write(string path, string contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents);
        }
    }
}
#endif
