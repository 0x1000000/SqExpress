using System.Linq;
using NUnit.Framework;
using SqExpress.SqlParser;
using SqExpress.Syntax;
using SqExpress.Syntax.Value;

namespace SqExpress.Test.SqlParser
{
    public class TSqlParserParameterTest
    {
        [Test]
        [TestCase("select UserId from Users u wHeRe [u].[UserId]=@id and [u].[Name]=@name")]
        [TestCase("select UserId from Users wHeRe userId=@id and [Name]=@name")]
        public void ExtractParameters(string sql)
        {
            if (SqTSqlParser.TryParse(sql, out var expr, out var tables, out var error))
            {
                var parameters = expr.SyntaxTree().Descendants().OfType<ExprParameter>().ToList();

                Assert.That(parameters.Count, Is.EqualTo(2));
                Assert.That(parameters[0].TagName, Is.EqualTo("id"));
                Assert.That(parameters[1].TagName, Is.EqualTo("name"));
            }
            else
            {
                Assert.Fail(error);
            }
        }

        [Test]
        public void NamedParametersArePreservedByFormatter()
        {
            const string sql = "SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[UserId]=@id AND [u].[Name]=@name";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var errors);

            Assert.That(ok, Is.True, errors == null ? null : string.Join("\n", errors));
            var parameters = expr!.SyntaxTree().Descendants().OfType<ExprParameter>().ToList();
            Assert.That(parameters.Count, Is.EqualTo(2));
            Assert.That(parameters[0].TagName, Is.EqualTo("id"));
            Assert.That(parameters[1].TagName, Is.EqualTo("name"));
        }

        [Test]
        public void NamedParametersAreMappedToSqExpr()
        {
            const string sql = "SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[UserId]=@id AND [u].[Name]=@name";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
            var parameters = expr!.SyntaxTree().Descendants().OfType<ExprParameter>().ToList();
            Assert.That(parameters.Count, Is.EqualTo(2));
            Assert.That(parameters[0].TagName, Is.EqualTo("id"));
            Assert.That(parameters[1].TagName, Is.EqualTo("name"));
        }

        [Test]
        public void ParameterMarkersInsideStringLiteralArePreservedByFormatter()
        {
            const string sql = "SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name]='@id'";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var errors);

            Assert.That(ok, Is.True, errors == null ? null : string.Join("\n", errors));
            Assert.That(expr, Is.Not.Null);
            Assert.That(expr!.SyntaxTree().Descendants().OfType<ExprParameter>().Any(), Is.False);
        }

        [Test]
        public void DoubleAtVariablesArePreservedByFormatter()
        {
            const string sql = "SELECT @@ROWCOUNT [Cnt]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var errors);

            Assert.That(ok, Is.True, errors == null ? null : string.Join("\n", errors));
            Assert.That(expr, Is.Not.Null);
            Assert.That(expr!.SyntaxTree().Descendants().OfType<ExprParameter>().Any(), Is.False);
        }
    }
}


