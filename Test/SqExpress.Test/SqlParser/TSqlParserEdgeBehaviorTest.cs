using System.Linq;
using NUnit.Framework;
using SqExpress.SqlExport;
using SqExpress.SqlParser;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Update;

namespace SqExpress.Test.SqlParser
{
    public class TSqlParserEdgeBehaviorTest
    {
        [Test]
        public void EmptySqlReturnsError()
        {

            var ok = TSqlParser.TryParse("   ", out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Is.EqualTo("SQL text cannot be empty."));
        }

        [Test]
        public void MultipleStatementsReturnError()
        {

            var ok = TSqlParser.TryParse("SELECT 1; SELECT 2", out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Is.EqualTo("Only one SQL statement is supported."));
        }

        [Test]
        public void SingleStatementWithTrailingSemicolonParses()
        {
            var sql = "SELECT 1;";

            var ok = TSqlParser.TryParse(sql, out var expr, out var errors);

            Assert.That(ok, Is.True, errors == null ? null : string.Join("\n", errors));
            Assert.That(expr, Is.Not.Null);
        }

        [Test]
        public void TryFormatScriptNormalizesUpdateAlias()
        {
            var sql = "UPDATE u SET u.[Name]=[o].[Title] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId]";

            var ok = TSqlParser.TryParse(sql, out var expr, out var errors);

            Assert.That(ok, Is.True, errors == null ? null : string.Join("\n", errors));
            Assert.That(expr, Is.Not.Null);
            Assert.That(expr, Is.TypeOf<SqExpress.Syntax.Update.ExprUpdate>());
        }

        [Test]
        public void TryFormatScriptNormalizesCteName()
        {
            var sql = "WITH R AS(SELECT 1) SELECT [R].[A] FROM [R]";

            var ok = TSqlParser.TryParse(sql, out var expr, out var errors);

            Assert.That(ok, Is.True, errors == null ? null : string.Join("\n", errors));
            Assert.That(expr, Is.Not.Null);
            var exportedSql = expr!.ToSql(TSqlExporter.Default);
            Assert.That(exportedSql, Does.StartWith("WITH "));
            Assert.That(exportedSql, Does.Contain("FROM [R]"));
        }

        [Test]
        public void TableArtifactsIgnoreFunctionSource()
        {
            var sql = "SELECT [s].[value] FROM STRING_SPLIT('a,b',',') [s]";

            var ok = TSqlParser.TryParse(sql, out IExpr? _, out var tables, out var errors);

            Assert.That(ok, Is.True, errors);
            Assert.That(tables, Is.Not.Null);
            Assert.That(tables!, Is.Empty);
        }

        [Test]
        public void TableArtifactsIgnoreValuesSource()
        {
            var sql = "SELECT [v].[Id] FROM (VALUES (1),(2))[v]([Id])";

            var ok = TSqlParser.TryParse(sql, out IExpr? _, out var tables, out var errors);

            Assert.That(ok, Is.True, errors);
            Assert.That(tables, Is.Not.Null);
            Assert.That(tables!, Is.Empty);
        }

        [Test]
        public void TableArtifactsAreCollectedFromMergeTargetAndSource()
        {
            var sql = "MERGE [dbo].[Users] [t] USING [dbo].[UsersStaging] [s] ON [t].[UserId]=[s].[UserId] WHEN MATCHED THEN UPDATE SET [t].[Name]=[s].[Name];";

            var ok = TSqlParser.TryParse(sql, out IExpr? _, out var tables, out var errors);

            Assert.That(ok, Is.True, errors);
            Assert.That(tables, Is.Not.Null);
            Assert.That(tables!.Count, Is.EqualTo(2));
            Assert.That(tables.Select(i => i.FullName.AsExprTableFullName().TableName.Name).ToArray(), Is.EquivalentTo(new[] { "Users", "UsersStaging" }));
        }

        [Test]
        public void CommentsAreIgnoredByTokenizer()
        {
            var sql = "/*head*/ SELECT [u].[UserId] -- tail\nFROM [dbo].[Users] [u]";

            var ok = TSqlParser.TryParse(sql, out IExpr? _, out var tables, out var errors);

            Assert.That(ok, Is.True, errors);
            Assert.That(tables, Is.Not.Null);
            Assert.That(tables!.Count, Is.EqualTo(1));
            Assert.That(tables[0].FullName.AsExprTableFullName().TableName.Name, Is.EqualTo("Users"));
        }

        [Test]
        public void CrossJoinSimpleProjectionMapsToStructuredExpr()
        {
            var sql = "SELECT U.Id,U2.Name FROM Users U CROSS JOIN Users U2";

            var ok = TSqlParser.TryParse(sql, out var expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr!.ToSql(TSqlExporter.Default), Is.EqualTo("SELECT [U].[Id],[U2].[Name] FROM [dbo].[Users] [U] CROSS JOIN [dbo].[Users] [U2]"));
            Assert.That(expr!.SyntaxTree().Descendants().OfType<ExprTable>().Count(), Is.EqualTo(2));
        }

        [Test]
        public void UpdateJoinOnPredicateIsPreserved()
        {
            var sql = "UPDATE [u] SET [u].[Name]=[o].[Title] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] WHERE [o].[Title] LIKE 'A%'";

            var ok = TSqlParser.TryParse(sql, out var expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.TypeOf<ExprUpdate>());

            var update = (ExprUpdate)expr!;
            Assert.That(update.Source, Is.TypeOf<ExprJoinedTable>());
            var source = (ExprJoinedTable)update.Source!;
            Assert.That(source.JoinType, Is.EqualTo(ExprJoinedTable.ExprJoinType.Inner));

            var join = source.SearchCondition as ExprBooleanEq;
            Assert.That(join, Is.Not.Null);
            var left = join!.Left as ExprColumn;
            var right = join.Right as ExprColumn;
            Assert.That(left, Is.Not.Null);
            Assert.That(right, Is.Not.Null);
            Assert.That((left!.Source as ExprTableAlias)?.Alias, Is.EqualTo((IExprAlias)new ExprAlias("o")));
            Assert.That(left.ColumnName.Name, Is.EqualTo("UserId"));
            Assert.That((right!.Source as ExprTableAlias)?.Alias, Is.EqualTo((IExprAlias)new ExprAlias("u")));
            Assert.That(right.ColumnName.Name, Is.EqualTo("UserId"));

            Assert.That(expr!.ToSql(TSqlExporter.Default), Does.Contain("ON [o].[UserId]=[u].[UserId]"));
        }

        [Test]
        public void CaseWithoutAliasDoesNotTreatEndAsAlias()
        {
            var sql = "SELECT CASE WHEN [u].[IsActive]=1 THEN 'Y' ELSE 'N' END FROM [dbo].[Users] [u]";

            var ok = TSqlParser.TryParse(sql, out var expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.TypeOf<ExprQuerySpecification>());
            var query = (ExprQuerySpecification)expr!;
            Assert.That(query.SelectList.Count, Is.EqualTo(1));
            Assert.That(query.SelectList[0], Is.TypeOf<ExprCase>());

            var parsedExpr = expr!;
            var exported = parsedExpr.ToSql(TSqlExporter.Default);
            Assert.That(exported, Does.Contain(" END "));
            Assert.That(exported, Is.EqualTo("SELECT CASE WHEN [u].[IsActive]=1 THEN 'Y' ELSE 'N' END FROM [dbo].[Users] [u]"));
        }

        [Test]
        public void UnaliasedTableReferenceKeepsNullAlias()
        {
            var sql = "SELECT COUNT(1) [Total] FROM [dbo].[Users]";

            var parseOk = TSqlParser.TryParse(sql, out var expr, out var tables, out var parseError);

            Assert.That(parseOk, Is.True, parseError);
            Assert.That(tables, Is.Not.Null);
            Assert.That(tables!.Count, Is.EqualTo(1));
            Assert.That(tables[0].FullName.AsExprTableFullName().TableName.Name, Is.EqualTo("Users"));

            var parsedExpr = expr!;
            var userTable = parsedExpr
                .SyntaxTree()
                .Descendants()
                .OfType<ExprTable>()
                .Single(i => i.FullName.AsExprTableFullName().TableName.Name == "Users");
            Assert.That(userTable.Alias, Is.Null);
            Assert.That(parsedExpr.ToSql(TSqlExporter.Default), Is.EqualTo("SELECT COUNT(1) [Total] FROM [dbo].[Users]"));
        }
    }
}


