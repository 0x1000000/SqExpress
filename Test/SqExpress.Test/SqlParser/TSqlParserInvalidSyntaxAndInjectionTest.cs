using System.Collections.Generic;
using NUnit.Framework;
using SqExpress.SqlParser;
using SqExpress.Syntax;

namespace SqExpress.Test.SqlParser
{
    public class TSqlParserInvalidSyntaxAndInjectionTest
    {
        [TestCaseSource(nameof(InvalidSyntaxCases))]
        public void InvalidSyntaxReturnsError(string sql, string expectedError)
        {

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Is.EqualTo(expectedError));
        }

        [Test]
        public void InjectionPayloadWithSecondStatementIsRejected()
        {
            var sql = "SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name]='x'; DROP TABLE [dbo].[Users]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Is.EqualTo("Only one SQL statement is supported."));
        }

        [Test]
        public void SemicolonInsideEscapedStringLiteralDoesNotTriggerMultiStatementCheck()
        {
            var sql = "SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name]='x''; DROP TABLE [dbo].[Users]'";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var errors);

            Assert.That(ok, Is.True, errors == null ? null : string.Join("\n", errors));
            Assert.That(expr, Is.Not.Null);
        }

        [Test]
        public void UnterminatedStringLiteralIsRejected()
        {
            var sql = "SELECT 'abc";
            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);
            Assert.That(ok, Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }
        [Test]
        public void UnterminatedBlockCommentIsRejected()
        {
            var sql = "SELECT 1 /* unterminated";
            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);
            Assert.That(ok, Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }
        private static IEnumerable<TestCaseData> InvalidSyntaxCases()
        {
            yield return new TestCaseData(
                    "SELCT 1",
                    "Unsupported or invalid statement start.")
                .SetName("InvalidStartKeyword");

            yield return new TestCaseData(
                    "SELECT FROM [dbo].[Users] [u]",
                    "Syntax error: SELECT list is missing.")
                .SetName("MissingSelectList");

            yield return new TestCaseData(
                    "UPDATE [dbo].[Users] [u] WHERE [u].[UserId]=1",
                    "Syntax error: UPDATE statement must contain SET clause.")
                .SetName("UpdateWithoutSet");

            yield return new TestCaseData(
                    "MERGE [dbo].[Users] [t] USING [dbo].[UsersStaging] [s] WHEN MATCHED THEN DELETE;",
                    "Syntax error: MERGE statement must contain ON clause.")
                .SetName("MergeWithoutOn");

            yield return new TestCaseData(
                    "SELECT ([u].[UserId] FROM [dbo].[Users] [u]",
                    "Syntax error: unbalanced parentheses.")
                .SetName("UnbalancedParentheses");

            yield return new TestCaseData(
                    "SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[UserId]=",
                    "Syntax error: unexpected end of statement.")
                .SetName("DanglingOperator");
        }
    }
}




