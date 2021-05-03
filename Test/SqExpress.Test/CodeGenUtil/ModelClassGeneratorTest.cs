#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
using NUnit.Framework;
using SqExpress.CodeGenUtil.CodeGen;

namespace SqExpress.Test.CodeGenUtil
{
    [TestFixture]
    public class ModelClassGeneratorTest
    {
        [Test]
        public void BasicTest()
        {
            TestFileSystem fileSystem = new TestFileSystem();

            fileSystem.AddFile("A\\table1.cs", TestTable1Text);

            var generated = ExistingCodeExplorer
                .EnumerateTableDescriptorsModelAttributes("A", fileSystem)
                .ParseAttribute(true)
                .CreateAnalysis()
                .Select(meta=> ModelClassGenerator.Generate(meta, "Org", "", true, fileSystem, out _).SyntaxTree)
                .ToList();

            var trees = new List<SyntaxTree>();

            foreach (var syntaxTree in generated)
            {
                trees.Add(CSharpSyntaxTree.ParseText(syntaxTree.ToString()));
            }

            trees.Add(CSharpSyntaxTree.ParseText(TestTable1Text));

            var compilation = CSharpCompilation.Create("SqModels",
                trees,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)); 

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
                Diagnostic first = emitResult.Diagnostics.First();

                var sourceCode = first.Location.SourceTree?.ToString();
                var s = sourceCode?.Substring(first.Location.SourceSpan.Start, first.Location.SourceSpan.Length);
                Console.WriteLine(sourceCode);
                Assert.Fail(first.GetMessage()+ (string.IsNullOrEmpty(s)?null:$" \"{s}\""));
            }

            var assembly = Assembly.Load(ms.ToArray());

            var allTypes = assembly.GetTypes();

            Assert.AreEqual(21, allTypes.Length);
        }

        private static readonly string TestTable1Text = @"
using SqExpress;
namespace Org{
    public class TestTable1 : TableBase
    {
        public TestTable1() : base(""dbo"", ""TestTable1"", SqExpress.Alias.Auto)
        {
            this.Id = this.CreateInt32Column(""Id"", ColumnMeta.PrimaryKey());
            this.Name = this.CreateStringColumn(""Name"", null);
            this.Name2 = this.CreateNullableStringColumn(""Name2"", null);
        }

        [SqModel(""Table1Data"")]
        [SqModel(""Table1Name"")]
        public Int32TableColumn Id { get; }

        [SqModel(""Table1Data"")]
        [SqModel(""Table1Name"")]
        public StringTableColumn Name { get; }

        [SqModel(""Table1Data"")]
        public NullableStringTableColumn Name2 { get; }
    }
    public class TestTable2 : TableBase
    {
        public TestTable2() : base(""dbo"", ""TestTable1"", SqExpress.Alias.Auto)
        {
            this.Id = this.CreateInt32Column(""Id"");
            this.Name = this.CreateStringColumn(""Name"", null);
            this.Name2 = this.CreateNullableStringColumn(""Name2"", null);
        }

        [SqModel(""Table2Data"")]
        [SqModel(""Table2Name"")]
        public Int32TableColumn Id { get; }

        [SqModel(""Table2Data"")]
        [SqModel(""Table2Name"")]
        public StringTableColumn Name { get; }

        [SqModel(""Table2Data"")]
        public NullableStringTableColumn Name2 { get; }
    }
    public class TestMergeTmpTable : TempTableBase
    {
        public TestMergeTmpTable() : base(""TargetTable"", SqExpress.Alias.Auto)
        {
            this.Id = this.CreateInt32Column(nameof(Id), ColumnMeta.PrimaryKey());
            this.Value = this.CreateInt32Column(nameof(Value));
            this.Version = this.CreateInt32Column(nameof(Version), ColumnMeta.DefaultValue(0));
        }


        [SqModel(""TestMergeData"")]
        public Int32TableColumn Id { get; }

        [SqModel(""TestMergeData"")]
        public Int32TableColumn Value { get; set; }

        [SqModel(""TestMergeData"")]
        public Int32TableColumn Version { get; set; }
    }
}
";
    }
}
#endif