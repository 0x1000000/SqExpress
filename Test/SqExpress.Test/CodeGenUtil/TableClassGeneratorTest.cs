#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using NUnit.Framework;
using SqExpress.CodeGenUtil;
using SqExpress.CodeGenUtil.CodeGen;
using SqExpress.CodeGenUtil.DbManagers;
using SqExpress.CodeGenUtil.Model;
using SqExpress.SqlExport;

namespace SqExpress.Test.CodeGenUtil
{
    [TestFixture]
    public class TableClassGeneratorTest
    {
        [Test]
        public async Task BasicTest()
        {

            using var dbManager = new DbManager(new DbManagerTest(),
                new SqlConnection("Initial Catalog=_1_2_3tbl;"),
                new GenTablesOptions(ConnectionType.MsSql, "fake", "Tab", "", "MyTables", verbosity: Verbosity.Quiet));

            var tables = await dbManager.SelectTables();

            Assert.AreEqual(2, tables.Count);

            var tableMap = tables.ToDictionary(t => t.DbName);

            IReadOnlyDictionary<TableRef, ClassDeclarationSyntax> existingCode =
                new Dictionary<TableRef, ClassDeclarationSyntax>();

            var generator =
                new TableClassGenerator(tableMap, "MyCompany.MyProject.Tables", existingCode);


            var trees = tables.Select(t => CSharpSyntaxTree.Create(generator.Generate(t, out _))).ToList();

            var compilation = CSharpCompilation.Create("Tables",
                trees,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            compilation = compilation.AddReferences(
                MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location),
                MetadataReference.CreateFromFile(typeof(object).Assembly.GetAssemblyLocation()),
                MetadataReference.CreateFromFile(Assembly
                    .Load("System.Runtime, Version=4.2.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
                    .Location),
                MetadataReference.CreateFromFile(typeof(SqQueryBuilder).Assembly.GetAssemblyLocation()));

            MemoryStream ms = new MemoryStream();

            var emitResult = compilation.Emit(ms);
            if (!emitResult.Success)
            {
                Assert.Fail(emitResult.Diagnostics.FirstOrDefault()?.GetMessage());
            }

            var assembly = Assembly.Load(ms.ToArray());

            var allTypes = assembly.GetTypes();

            var table1 = (TableBase) Activator.CreateInstance(allTypes.Find(t => t.Name == tables[0].Name))!;
            Assert.NotNull(table1);
            var table2 = (TableBase) Activator.CreateInstance(allTypes.Find(t => t.Name == tables[1].Name))!;
            Assert.NotNull(table2);


            string table1ExpectedSql =
                "CREATE TABLE [dbo].[TableZ]([Id] int NOT NULL  IDENTITY (1, 1) DEFAULT (0),[ValueA] [nvarchar](255) NOT NULL DEFAULT (''),[Value_A] decimal(2,6),CONSTRAINT [PK_dbo_TableZ] PRIMARY KEY ([Id]));";
            Assert.AreEqual(table1ExpectedSql, TSqlExporter.Default.ToSql(table1.Script.Create()));

            string table2ExpectedSql =
                "CREATE TABLE [dbo].[TableA]([Id] int NOT NULL  IDENTITY (1, 1) DEFAULT (0),[Value] datetime NOT NULL DEFAULT (GETUTCDATE()),CONSTRAINT [PK_dbo_TableA] PRIMARY KEY ([Id]),CONSTRAINT [FK_dbo__TableA_to_dbo__TableZ] FOREIGN KEY ([Id]) REFERENCES [dbo].[TableZ]([Id]),INDEX [IX_dbo_TableA_Value_DESC] UNIQUE([Value] DESC));";
            Assert.AreEqual(table2ExpectedSql, TSqlExporter.Default.ToSql(table2.Script.Create()));
        }
    }
}
#endif