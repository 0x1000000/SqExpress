using NUnit.Framework;
using SqExpress.SqlParser;
using SqExpress.Syntax;

namespace SqExpress.Test.SqlParser
{
    public class TSqlParserUnicodeLiteralTest
    {
        [Test]
        public void UnicodePrefixedStringLiteralParses()
        {
            var sql = "SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name]=N'A'";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
        }
    }
}
