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

        [Test]
        public void UndefinedOrderByAliasIsRejected()
        {
            var sql = "SELECT UserId, Name AS UserName FROM Users WHERE IsActive = 2 ORDER BY u.Name DESC";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("Unknown table alias or name: u."));
        }

        [Test]
        public void UnterminatedBracketIdentifierIsRejected()
        {
            var sql = "SELECT [UserId FROM Users";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("unterminated bracket identifier"));
        }

        [Test]
        public void UnterminatedBracketIdentifierDoesNotSwallowRestOfStatement()
        {
            var sql = "SELECT UserId, Name AS UserName FROM Users CROSS JOIN Users U[ WHERE IsActive = 2 ORDER BY u.Name DESC";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("unterminated bracket identifier"));
        }

        [Test]
        public void JoinKeywordIsNotConsumedAsAlias()
        {
            var sql = "SELECT UserId FROM Users INNER JOIN Orders ON Users.Id = Orders.UserId";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("Unqualified column reference is ambiguous"));
        }

        [Test]
        public void UnqualifiedJoinPredicateColumnsAreRejectedAsAmbiguous()
        {
            var sql = "SELECT [u].[UserId] FROM [dbo].[Users] [u] INNER JOIN [dbo].[Orders] [o] ON UserId = UserId";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("Unqualified column reference is ambiguous"));
        }

        [Test]
        public void DuplicateTableAliasIsRejected()
        {
            var sql = "SELECT [u].[UserId] FROM [dbo].[Users] [u] INNER JOIN [dbo].[Orders] [u] ON [u].[UserId] = [u].[UserId]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("Duplicate table alias or name in scope: u."));
        }

        [Test]
        public void DuplicateImplicitTableNameIsRejected()
        {
            var sql = "SELECT [Users].[UserId] FROM [dbo].[Users] INNER JOIN [dbo].[Users] ON [Users].[UserId] = [Users].[UserId]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("Duplicate table alias or name in scope: Users."));
        }

        [Test]
        public void OrderByBareTableAliasIsRejected()
        {
            var sql = "SELECT [u].[UserId] FROM [dbo].[Users] [u] ORDER BY [u]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("ORDER BY item cannot reference table alias without column: u."));
        }

        [Test]
        public void DuplicateSelectAliasIsRejected()
        {
            var sql = "SELECT [u].[Name] [X],[u].[UserId] [X] FROM [dbo].[Users] [u] ORDER BY [X]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("Duplicate select alias in scope: X."));
        }

        [Test]
        public void NonApplyDerivedTableCannotReferenceOuterAlias()
        {
            var sql = "SELECT [u].[UserId] FROM [dbo].[Users] [u] CROSS JOIN (SELECT [u].[UserId] [UserId]) [x]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("Unknown table alias or name: u."));
        }

        [Test]
        public void AggregateQueryWithoutGroupByRejectsBareColumn()
        {
            var sql = "SELECT [u].[Status],COUNT(1) [Total] FROM [dbo].[Users] [u]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("neither grouped nor aggregated"));
            Assert.That(error, Does.Contain("Status"));
        }

        [Test]
        public void GroupByRejectsNonGroupedProjectedColumn()
        {
            var sql = "SELECT [u].[Status],[u].[Name],COUNT(1) [Total] FROM [dbo].[Users] [u] GROUP BY [u].[Status]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("neither grouped nor aggregated"));
            Assert.That(error, Does.Contain("Name"));
        }

        [Test]
        public void GroupByCannotReferenceSelectAlias()
        {
            var sql = "SELECT [u].[Name] [UserName],COUNT(1) [Total] FROM [dbo].[Users] [u] GROUP BY [UserName]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("GROUP BY clause cannot reference select alias: UserName."));
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

            yield return new TestCaseData(
                    "SELECT UserId FROM Users INNER JOIN Orders",
                    "Syntax error: JOIN clause must contain ON condition.")
                .SetName("InnerJoinWithoutOn");

            yield return new TestCaseData(
                    "SELECT UserId FROM Users LEFT JOIN Orders",
                    "Syntax error: JOIN clause must contain ON condition.")
                .SetName("LeftJoinWithoutOn");

            yield return new TestCaseData(
                    "SELECT UserId FROM Users RIGHT JOIN Orders",
                    "Syntax error: JOIN clause must contain ON condition.")
                .SetName("RightJoinWithoutOn");

            yield return new TestCaseData(
                    "SELECT UserId FROM Users FULL JOIN Orders",
                    "Syntax error: JOIN clause must contain ON condition.")
                .SetName("FullJoinWithoutOn");

            yield return new TestCaseData(
                    "SELECT UserId FROM Users CROSS JOIN Orders ON Users.Id = Orders.UserId",
                    "Syntax error: CROSS/ APPLY join cannot contain ON condition.")
                .SetName("CrossJoinWithOn");

            yield return new TestCaseData(
                    "SELECT UserId FROM Users ORDER BY",
                    "Syntax error: ORDER BY clause is invalid.")
                .SetName("OrderByWithoutItems");

            yield return new TestCaseData(
                    "SELECT UserId FROM Users ORDER BY UserId,",
                    "Syntax error: ORDER BY clause is invalid.")
                .SetName("OrderByTrailingComma");

            yield return new TestCaseData(
                    "SELECT UserId FROM Users GROUP BY",
                    "Syntax error: GROUP BY clause is invalid.")
                .SetName("GroupByWithoutItems");

            yield return new TestCaseData(
                    "SELECT UserId FROM Users GROUP BY UserId,",
                    "Syntax error: GROUP BY clause is invalid.")
                .SetName("GroupByTrailingComma");

            yield return new TestCaseData(
                    "SELECT UserId FROM Users OFFSET 10 ROWS",
                    "Syntax error: OFFSET requires ORDER BY clause.")
                .SetName("OffsetWithoutOrderBy");

            yield return new TestCaseData(
                    "SELECT UserId FROM Users ORDER BY UserId OFFSET 10",
                    "Syntax error: OFFSET/FETCH clause is invalid.")
                .SetName("OffsetMissingRowKeyword");

            yield return new TestCaseData(
                    "SELECT UserId FROM Users ORDER BY UserId OFFSET 10 ROWS FETCH NEXT ROWS ONLY",
                    "Syntax error: OFFSET/FETCH clause is invalid.")
                .SetName("OffsetFetchMissingFetchValue");
        }
    }
}




