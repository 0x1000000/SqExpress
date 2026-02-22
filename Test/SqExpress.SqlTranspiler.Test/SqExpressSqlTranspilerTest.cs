using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using SqExpress.SqlExport;
using SqExpress.Syntax.Select;
using SqExpress.SqlTranspiler;

namespace SqExpress.SqlTranspiler.Test
{
    [TestFixture]
    public class SqExpressSqlTranspilerTest
    {
        [Test]
        public void TranspileSelect_BasicQuery_GeneratesQueryAndDeclarations()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var result = transpiler.Transpile(
                "SELECT u.UserId, u.Name AS UserName FROM dbo.Users u WHERE u.IsActive = 1 ORDER BY u.Name DESC");

            Assert.AreEqual("SELECT", result.StatementKind);
            Assert.That(result.QueryCSharpCode, Does.Contain("var u = new UTable(\"u\");"));
            Assert.That(result.QueryCSharpCode, Does.Contain("Select(u.UserId, u.Name.As(\"UserName\"))"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".Where(u.IsActive == Literal(1))"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public sealed class UTable : TableBase"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public Int32TableColumn UserId"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public Int32TableColumn Name"));

            AssertCompiles(result);
        }

        [Test]
        public void TranspileSelect_DistinctTop_GeneratesSelectTopDistinct()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var result = transpiler.Transpile("SELECT DISTINCT TOP (10) u.UserId FROM dbo.Users u");

            Assert.That(result.QueryCSharpCode, Does.Contain("SelectTopDistinct(Literal(10), u.UserId)"));
            AssertCompiles(result);
        }

        [Test]
        public void TranspileSelect_WithJoinAndPredicates_GeneratesJoinAndFiltersAndStringType()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var sql = "SELECT u.UserId " +
                      "FROM dbo.Users u INNER JOIN dbo.Orders o ON o.UserId = u.UserId " +
                      "WHERE u.Status IN (1,2,3) AND o.Title LIKE 'A%' AND u.Score BETWEEN 10 AND 20";

            var result = transpiler.Transpile(sql);

            Assert.That(result.QueryCSharpCode, Does.Contain(".InnerJoin(o, o.UserId == u.UserId)"));
            Assert.That(result.QueryCSharpCode, Does.Contain("u.Status.In(Literal(1), Literal(2), Literal(3))"));
            Assert.That(result.QueryCSharpCode, Does.Contain("Like(o.Title, \"A%\")"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public StringTableColumn Title"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("CreateStringColumn(\"Title\", null, true)"));

            AssertCompiles(result);
        }

        [Test]
        public void TranspileSelect_CountStar_UsesCountOne()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var result = transpiler.Transpile("SELECT COUNT(*) AS Total FROM dbo.Users");

            Assert.That(result.QueryCSharpCode, Does.Contain("CountOne().As(\"Total\")"));
            AssertCompiles(result);
        }

        [Test]
        public void TranspileSelect_QualifiedStar_UsesAllColumnsExtension()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var sql = "SELECT u.*, o.OrderId FROM dbo.Users u INNER JOIN dbo.Orders o ON o.UserId = u.UserId";
            var result = transpiler.Transpile(sql);

            Assert.That(result.QueryCSharpCode, Does.Contain("Select(u.AllColumns(), o.OrderId)"));
            AssertCompiles(result);
        }

        [Test]
        public void TranspileSelect_FunctionCall_UsesScalarFunctionSys()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var result = transpiler.Transpile("SELECT LEN(u.Name) AS NameLength FROM dbo.Users u");

            Assert.That(result.QueryCSharpCode, Does.Contain("ScalarFunctionSys(\"LEN\", u.Name).As(\"NameLength\")"));
            AssertCompiles(result);
        }

        [Test]
        public void TranspileSelect_WithOptions_ChangesNamespacesAndMethod()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var options = new SqExpressSqlTranspilerOptions
            {
                NamespaceName = "Demo.Generated",
                MethodName = "BuildReport",
                QueryVariableName = "reportQuery"
            };

            var result = transpiler.Transpile("SELECT 1", options);

            Assert.That(result.QueryCSharpCode, Does.Contain("namespace Demo.Generated"));
            Assert.That(result.QueryCSharpCode, Does.Contain("using Demo.Generated.Declarations;"));
            Assert.That(result.QueryCSharpCode, Does.Contain("public static IExprQuery BuildReport()"));
            Assert.That(result.QueryCSharpCode, Does.Contain("var reportQuery = Select(Literal(1)).Done();"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("namespace Demo.Generated.Declarations"));
            AssertCompiles(result);
        }

        [Test]
        public void Transpile_Cte_GeneratesCteDeclarationPlaceholder()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var sql = "WITH C AS (SELECT 1 AS A) SELECT C.A FROM C";

            var result = transpiler.Transpile(sql);

            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public sealed class CCte : CteBase"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("CTE query transpilation is not implemented yet."));
            Assert.That(result.QueryCSharpCode, Does.Contain("From(c)"));
            AssertCompiles(result);
        }

        [Test]
        public void Transpile_SubQuery_GeneratesDerivedTablePlaceholder()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var sql = "SELECT sq.A FROM (SELECT 1 AS A) sq";

            var result = transpiler.Transpile(sql);

            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public sealed class SqSubQuery : DerivedTableBase"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("Sub query transpilation is not implemented yet."));
            Assert.That(result.QueryCSharpCode, Does.Contain("var sq = new SqSubQuery(\"sq\");"));
            AssertCompiles(result);
        }

        [Test]
        public void Transpile_StringComparison_InfersNVarCharColumn()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var sql = "SELECT u.Name FROM dbo.Users u WHERE u.Name = 'A'";

            var result = transpiler.Transpile(sql);

            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public StringTableColumn Name"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("CreateStringColumn(\"Name\", null, true)"));
            Assert.That(result.QueryCSharpCode, Does.Contain("u.Name == Literal(\"A\")"));
            AssertCompiles(result);
        }

        [Test]
        public void TranspileSelect_CompiledCode_CanInvokeGeneratedBuildMethod()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var options = new SqExpressSqlTranspilerOptions
            {
                NamespaceName = "Runtime.Generated",
                DeclarationsNamespaceName = "Runtime.Generated.Tables",
                ClassName = "ReportQuery",
                MethodName = "Create"
            };

            var result = transpiler.Transpile(
                "SELECT u.UserId FROM dbo.Users u WHERE u.Name = 'A' ORDER BY u.UserId",
                options);

            var assembly = AssertCompiles(result, "GeneratedTranspilerRuntimeTests");
            var query = InvokeGeneratedBuildMethod(assembly, options);
            var sql = query.ToSql(TSqlExporter.Default);

            Assert.That(sql, Does.Contain("FROM [dbo].[Users] [u]"));
            Assert.That(sql, Does.Contain("WHERE [u].[Name]='A'"));
            Assert.That(sql, Does.Contain("ORDER BY [u].[UserId]"));
        }

        [Test]
        public void Transpile_RejectsNonSelectStatement()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var ex = Assert.Throws<SqExpressSqlTranspilerException>(() => transpiler.Transpile("INSERT INTO dbo.Users(UserId) VALUES (1)"));
            Assert.That(ex?.Message, Does.Contain("Only SELECT statements are supported"));
        }

        [Test]
        public void Transpile_RejectsUnion()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var ex = Assert.Throws<SqExpressSqlTranspilerException>(() => transpiler.Transpile("SELECT 1 UNION SELECT 2"));
            Assert.That(ex?.Message, Does.Contain("UNION"));
        }

        [Test]
        public void Transpile_RejectsSelectInto()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var ex = Assert.Throws<SqExpressSqlTranspilerException>(() => transpiler.Transpile("SELECT 1 INTO dbo.ResultTable"));
            Assert.That(ex?.Message, Does.Contain("SELECT INTO"));
        }

        [Test]
        public void Transpile_ReportsParseErrors()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var ex = Assert.Throws<SqExpressSqlTranspilerException>(() => transpiler.Transpile("SELECT FROM"));
            Assert.That(ex?.Message, Does.Contain("Could not parse SQL"));
        }

        [Test]
        public void TranspileSelect_MethodRejectsNonSelect()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var ex = Assert.Throws<SqExpressSqlTranspilerException>(() => transpiler.TranspileSelect("DELETE FROM dbo.Users"));

            Assert.That(ex?.Message, Does.Contain("Expected SELECT statement"));
        }

        private static readonly MetadataReference[] CompilationReferences = BuildCompilationReferences();

        private static Assembly AssertCompiles(SqExpressTranspileResult result, string assemblyName = "GeneratedTranspilerCodeTests")
        {
            var queryTree = CSharpSyntaxTree.ParseText(result.QueryCSharpCode);
            var declarationsTree = CSharpSyntaxTree.ParseText(result.DeclarationsCSharpCode);

            AssertNoSyntaxErrors(queryTree);
            AssertNoSyntaxErrors(declarationsTree);

            var compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { queryTree, declarationsTree },
                CompilationReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);
            if (!emitResult.Success)
            {
                var errors = emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
                Assert.Fail("Generated code does not compile:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
            }

            return Assembly.Load(ms.ToArray());
        }

        private static IExprQuery InvokeGeneratedBuildMethod(Assembly generatedAssembly, SqExpressSqlTranspilerOptions options)
        {
            var generatedTypeName = options.NamespaceName + "." + options.ClassName;
            var generatedType = generatedAssembly.GetType(generatedTypeName);
            if (generatedType == null)
            {
                Assert.Fail("Generated type was not found: " + generatedTypeName);
            }

            var generatedMethod = generatedType!.GetMethod(options.MethodName, BindingFlags.Public | BindingFlags.Static);
            if (generatedMethod == null)
            {
                Assert.Fail("Generated method was not found: " + options.MethodName);
            }

            var invocationResult = generatedMethod!.Invoke(null, Array.Empty<object>());
            if (invocationResult is not IExprQuery)
            {
                Assert.Fail("Generated method did not return IExprQuery.");
            }

            return (IExprQuery)invocationResult!;
        }

        private static void AssertNoSyntaxErrors(SyntaxTree syntaxTree)
        {
            var diagnostics = syntaxTree.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            if (diagnostics.Count > 0)
            {
                Assert.Fail("Generated code has syntax errors:" + Environment.NewLine + string.Join(Environment.NewLine, diagnostics));
            }
        }

        private static MetadataReference[] BuildCompilationReferences()
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
            referencePaths.Add(typeof(SqQueryBuilder).GetTypeInfo().Assembly.Location);
            referencePaths.Add(typeof(SqExpressSqlTranspiler).GetTypeInfo().Assembly.Location);
            referencePaths.Add(typeof(TSqlExporter).GetTypeInfo().Assembly.Location);

            return referencePaths.Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        }
    }
}
