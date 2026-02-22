using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using SqExpress.SqlTranspiler;

namespace SqExpress.SqlTranspiler.Test
{
    [TestFixture]
    public class SqExpressSqlTranspilerTest
    {
        [Test]
        public void TranspileSelect_BasicQuery_GeneratesExpectedCode()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var result = transpiler.Transpile(
                "SELECT u.UserId, u.Name AS UserName FROM dbo.Users u WHERE u.IsActive = 1 ORDER BY u.Name DESC");

            Assert.AreEqual("SELECT", result.StatementKind);
            Assert.That(result.CSharpCode, Does.Contain("var u = new TableBase(\"dbo\", \"Users\", \"u\");"));
            Assert.That(result.CSharpCode, Does.Contain("Select(u.Column(\"UserId\"), u.Column(\"Name\").As(\"UserName\"))"));
            Assert.That(result.CSharpCode, Does.Contain(".Where(u.Column(\"IsActive\") == Literal(1))"));
            Assert.That(result.CSharpCode, Does.Contain(".OrderBy(Desc(u.Column(\"Name\")))"));

            AssertHasNoSyntaxErrors(result.CSharpCode);
            AssertCompiles(result.CSharpCode);
        }

        [Test]
        public void TranspileSelect_DistinctTop_GeneratesSelectTopDistinct()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var result = transpiler.Transpile("SELECT DISTINCT TOP (10) u.UserId FROM dbo.Users u");

            Assert.That(result.CSharpCode, Does.Contain("SelectTopDistinct(Literal(10), u.Column(\"UserId\"))"));
            AssertCompiles(result.CSharpCode);
        }

        [Test]
        public void TranspileSelect_WithJoinAndPredicates_GeneratesJoinAndFilters()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var sql = "SELECT u.UserId " +
                      "FROM dbo.Users u INNER JOIN dbo.Orders o ON o.UserId = u.UserId " +
                      "WHERE u.Status IN (1,2,3) AND o.Title LIKE 'A%' AND u.Score BETWEEN 10 AND 20";

            var result = transpiler.Transpile(sql);

            Assert.That(result.CSharpCode, Does.Contain(".InnerJoin(o, o.Column(\"UserId\") == u.Column(\"UserId\"))"));
            Assert.That(result.CSharpCode, Does.Contain("u.Column(\"Status\").In(Literal(1), Literal(2), Literal(3))"));
            Assert.That(result.CSharpCode, Does.Contain("Like(o.Column(\"Title\"), \"A%\")"));
            Assert.That(result.CSharpCode, Does.Contain("u.Column(\"Score\") >= Literal(10)"));
            Assert.That(result.CSharpCode, Does.Contain("u.Column(\"Score\") <= Literal(20)"));

            AssertCompiles(result.CSharpCode);
        }

        [Test]
        public void TranspileSelect_CountStar_UsesCountOne()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var result = transpiler.Transpile("SELECT COUNT(*) AS Total FROM dbo.Users");

            Assert.That(result.CSharpCode, Does.Contain("CountOne().As(\"Total\")"));
            AssertCompiles(result.CSharpCode);
        }

        [Test]
        public void TranspileSelect_QualifiedStar_UsesAllColumnsExtension()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var sql = "SELECT u.*, o.OrderId FROM dbo.Users u INNER JOIN dbo.Orders o ON o.UserId = u.UserId";
            var result = transpiler.Transpile(sql);

            Assert.That(result.CSharpCode, Does.Contain("Select(u.AllColumns(), o.Column(\"OrderId\"))"));
            AssertCompiles(result.CSharpCode);
        }

        [Test]
        public void TranspileSelect_FunctionCall_UsesScalarFunctionSys()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var result = transpiler.Transpile("SELECT LEN(u.Name) AS NameLength FROM dbo.Users u");

            Assert.That(result.CSharpCode, Does.Contain("ScalarFunctionSys(\"LEN\", u.Column(\"Name\")).As(\"NameLength\")"));
            AssertCompiles(result.CSharpCode);
        }

        [Test]
        public void TranspileSelect_WithOptions_ChangesNamespaceAndMethod()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var options = new SqExpressSqlTranspilerOptions
            {
                NamespaceName = "Demo.Generated",
                ClassName = "ReportQuery",
                MethodName = "BuildReport",
                QueryVariableName = "reportQuery"
            };

            var result = transpiler.Transpile("SELECT 1", options);

            Assert.That(result.CSharpCode, Does.Contain("namespace Demo.Generated"));
            Assert.That(result.CSharpCode, Does.Contain("public static class ReportQuery"));
            Assert.That(result.CSharpCode, Does.Contain("public static IExprQuery BuildReport()"));
            Assert.That(result.CSharpCode, Does.Contain("var reportQuery = Select(Literal(1)).Done();"));
            AssertCompiles(result.CSharpCode);
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
        public void Transpile_RejectsCteForNow()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var sql = "WITH C AS (SELECT 1 AS A) SELECT A FROM C";

            var ex = Assert.Throws<SqExpressSqlTranspilerException>(() => transpiler.Transpile(sql));
            Assert.That(ex?.Message, Does.Contain("CTE"));
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

        private static void AssertHasNoSyntaxErrors(string code)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var diagnostics = syntaxTree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            if (diagnostics.Count > 0)
            {
                Assert.Fail("Generated code has syntax errors:" + Environment.NewLine + string.Join(Environment.NewLine, diagnostics));
            }
        }

        private static void AssertCompiles(string code)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var references = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                .Select(a => a.Location)
                .Append(typeof(object).GetTypeInfo().Assembly.Location)
                .Append(typeof(SqQueryBuilder).GetTypeInfo().Assembly.Location)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(path => MetadataReference.CreateFromFile(path))
                .ToList();

            var compilation = CSharpCompilation.Create(
                "GeneratedTranspilerCodeTests",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);
            if (!emitResult.Success)
            {
                var errors = emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
                Assert.Fail("Generated code does not compile:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
            }
        }
    }
}
