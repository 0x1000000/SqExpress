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

namespace SqExpress.Test.SqlParser;

public class TSqlParserEdgeBehaviorTest
{
    [Test]
    public void EmptySqlReturnsError()
    {

        var ok = SqTSqlParser.TryParse("   ", out IExpr? _, out var error);

        Assert.That(ok, Is.False);
        Assert.That(error, Is.EqualTo("SQL text cannot be empty."));
    }

    [Test]
    public void MultipleStatementsReturnError()
    {

        var ok = SqTSqlParser.TryParse("SELECT 1; SELECT 2", out IExpr? _, out var error);

        Assert.That(ok, Is.False);
        Assert.That(error, Is.EqualTo("Only one SQL statement is supported."));
    }

    [Test]
    public void SingleStatementWithTrailingSemicolonParses()
    {
        var sql = "SELECT 1;";

        var ok = SqTSqlParser.TryParse(sql, out var expr, out var errors);

        Assert.That(ok, Is.True, errors == null ? null : string.Join("\n", errors));
        Assert.That(expr, Is.Not.Null);
    }

    [Test]
    public void TryFormatScriptNormalizesUpdateAlias()
    {
        var sql = "UPDATE u SET u.[Name]=[o].[Title] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId]";

        var ok = SqTSqlParser.TryParse(sql, out var expr, out var errors);

        Assert.That(ok, Is.True, errors == null ? null : string.Join("\n", errors));
        Assert.That(expr, Is.Not.Null);
        Assert.That(expr, Is.TypeOf<SqExpress.Syntax.Update.ExprUpdate>());
    }

    [Test]
    public void TryFormatScriptNormalizesCteName()
    {
        var sql = "WITH R AS(SELECT 1) SELECT [R].[A] FROM [R]";

        var ok = SqTSqlParser.TryParse(sql, out var expr, out var errors);

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

        var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var tables, out var errors);

        Assert.That(ok, Is.True, errors);
        Assert.That(tables, Is.Not.Null);
        Assert.That(tables!, Is.Empty);
    }

    [Test]
    public void TableArtifactsIgnoreValuesSource()
    {
        var sql = "SELECT [v].[Id] FROM (VALUES (1),(2))[v]([Id])";

        var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var tables, out var errors);

        Assert.That(ok, Is.True, errors);
        Assert.That(tables, Is.Not.Null);
        Assert.That(tables!, Is.Empty);
    }

    [Test]
    public void TableArtifactsAreCollectedFromMergeTargetAndSource()
    {
        var sql = "MERGE [dbo].[Users] [t] USING [dbo].[UsersStaging] [s] ON [t].[UserId]=[s].[UserId] WHEN MATCHED THEN UPDATE SET [t].[Name]=[s].[Name];";

        var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var tables, out var errors);

        Assert.That(ok, Is.True, errors);
        Assert.That(tables, Is.Not.Null);
        Assert.That(tables!.Count, Is.EqualTo(2));
        Assert.That(tables.Select(i => i.FullName.AsExprTableFullName().TableName.Name).ToArray(), Is.EquivalentTo(new[] { "Users", "UsersStaging" }));
    }

    [Test]
    public void MergeWithAsAliases_MapsSuccessfully()
    {
        var sql = "MERGE dbo.Target AS t USING (SELECT 1 AS Id, 'A' AS Name) AS s ON t.Id = s.Id WHEN MATCHED THEN UPDATE SET t.Name = s.Name WHEN NOT MATCHED THEN INSERT (Id, Name) VALUES (s.Id, s.Name);";

        var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

        Assert.That(ok, Is.True, error);
        Assert.That(expr, Is.Not.Null);
    }

    [Test]
    public void CommentsAreIgnoredByTokenizer()
    {
        var sql = "/*head*/ SELECT [u].[UserId] -- tail\nFROM [dbo].[Users] [u]";

        var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var tables, out var errors);

        Assert.That(ok, Is.True, errors);
        Assert.That(tables, Is.Not.Null);
        Assert.That(tables!.Count, Is.EqualTo(1));
        Assert.That(tables[0].FullName.AsExprTableFullName().TableName.Name, Is.EqualTo("Users"));
    }

    [Test]
    public void CrossJoinSimpleProjectionMapsToStructuredExpr()
    {
        var sql = "SELECT U.Id,U2.Name FROM Users U CROSS JOIN Users U2";

        var ok = SqTSqlParser.TryParse(sql, out var expr, out var error);

        Assert.That(ok, Is.True, error);
        Assert.That(expr!.ToSql(TSqlExporter.Default), Is.EqualTo("SELECT [U].[Id],[U2].[Name] FROM [dbo].[Users] [U] CROSS JOIN [dbo].[Users] [U2]"));
        Assert.That(expr!.SyntaxTree().Descendants().OfType<ExprTable>().Count(), Is.EqualTo(2));
    }

    [Test]
    public void CustomDefaultSchemaIsUsedForUnqualifiedTables()
    {
        var sql = "SELECT U.Id FROM Users U";

        var ok = SqTSqlParser.TryParse(
            sql,
            new SqTSqlParserOptions { DefaultSchema = "sales" },
            out var expr,
            out var tables,
            out var error);

        Assert.That(ok, Is.True, error);
        Assert.That(expr, Is.Not.Null);
        Assert.That(tables, Is.Not.Null);
        Assert.That(tables!.Count, Is.EqualTo(1));
        Assert.That(tables[0].FullName.AsExprTableFullName().DbSchema!.Schema.Name, Is.EqualTo("sales"));
        Assert.That(expr!.ToSql(TSqlExporter.Default), Is.EqualTo("SELECT [U].[Id] FROM [sales].[Users] [U]"));
    }

    [Test]
    public void NullDefaultSchemaKeepsUnqualifiedTablesSchemaLess()
    {
        var sql = "SELECT U.Id FROM Users U";

        var ok = SqTSqlParser.TryParse(
            sql,
            new SqTSqlParserOptions { DefaultSchema = null },
            out var expr,
            out var tables,
            out var error);

        Assert.That(ok, Is.True, error);
        Assert.That(expr, Is.Not.Null);
        Assert.That(tables, Is.Not.Null);
        Assert.That(tables!.Count, Is.EqualTo(1));
        Assert.That(tables[0].FullName.AsExprTableFullName().DbSchema, Is.Null);
        Assert.That(expr!.ToSql(TSqlExporter.Default), Is.EqualTo("SELECT [U].[Id] FROM [Users] [U]"));
    }

    [Test]
    public void UpdateJoinOnPredicateIsPreserved()
    {
        var sql = "UPDATE [u] SET [u].[Name]=[o].[Title] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] WHERE [o].[Title] LIKE 'A%'";

        var ok = SqTSqlParser.TryParse(sql, out var expr, out var error);

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

        var ok = SqTSqlParser.TryParse(sql, out var expr, out var error);

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

        var parseOk = SqTSqlParser.TryParse(sql, out var expr, out var tables, out var parseError);

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

    [Test]
    public void OrderByCanUseSelectAliasInMultiTableScope()
    {
        var sql = "SELECT [u].[Name] [UserName],[o].[OrderId] FROM [dbo].[Users] [u] INNER JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] ORDER BY [UserName]";

        var ok = SqTSqlParser.TryParse(sql, out var expr, out var error);

        Assert.That(ok, Is.True, error);
        Assert.That(expr, Is.TypeOf<ExprSelect>());

        var select = (ExprSelect)expr!;
        Assert.That(select.OrderBy.OrderList.Count, Is.EqualTo(1));
        var orderBy = select.OrderBy.OrderList[0].Value as ExprColumn;
        Assert.That(orderBy, Is.Not.Null);
        Assert.That(orderBy!.Source, Is.Null);
        Assert.That(orderBy.ColumnName.Name, Is.EqualTo("UserName"));
    }

    [Test]
    public void CrossApplyDerivedTableCanReferenceLeftTable()
    {
        var sql = "SELECT [u].[UserId],[x].[UserId] FROM [dbo].[Users] [u] CROSS APPLY (SELECT [u].[UserId] [UserId]) [x]";

        var ok = SqTSqlParser.TryParse(sql, out var expr, out var error);

        Assert.That(ok, Is.True, error);
        Assert.That(expr, Is.Not.Null);
        Assert.That(expr!.ToSql(TSqlExporter.Default), Does.Contain("CROSS APPLY"));
    }

    [Test]
    public void AggregateArithmeticInSelectListRoundTrips()
    {
        var sql = "SELECT SUM([o].[TotalAmount])+1 [AdjustedRevenue] FROM [dbo].[Orders] [o]";

        var ok = SqTSqlParser.TryParse(sql, out var expr, out var error);

        Assert.That(ok, Is.True, error);
        Assert.That(expr, Is.Not.Null);
        Assert.That(expr!.ToSql(TSqlExporter.Default), Is.EqualTo("SELECT SUM([o].[TotalAmount])+1 [AdjustedRevenue] FROM [dbo].[Orders] [o]"));
    }

    [Test]
    public void WindowAggregateArithmeticInSelectListRoundTrips()
    {
        var sql = "SELECT SUM([o].[TotalAmount]) OVER()-[o].[Discount] [RemainingRevenue] FROM [dbo].[Orders] [o]";

        var ok = SqTSqlParser.TryParse(sql, out var expr, out var error);

        Assert.That(ok, Is.True, error);
        Assert.That(expr, Is.Not.Null);
        Assert.That(expr!.ToSql(TSqlExporter.Default), Is.EqualTo("SELECT SUM([o].[TotalAmount])OVER()-[o].[Discount] [RemainingRevenue] FROM [dbo].[Orders] [o]"));
    }

    [Test]
    public void ParameterArithmeticInSelectListParsesWithoutAliasTruncation()
    {
        var sql = "SELECT @a+@b FROM [dbo].[Users] [u] WHERE @a>0 AND @b>0";

        var ok = SqTSqlParser.TryParse(sql, out var expr, out var error);

        Assert.That(ok, Is.True, error);
        Assert.That(expr, Is.Not.Null);
    }

    [Test]
    public void DerivedTableGroupByQuery_WithSingleQuotedInnerAlias_Parses()
    {
        var sql = """
                  SELECT 
                  	CustomerName.CustomerName,
                  	COUNT(1) As OrdersNum
                  FROM 
                  (
                  	SELECT 
                  		C.CustomerId,
                  		CASE WHEN U.UserId IS NOT NULL
                  			THEN U.FirstName + ' ' + U.LastName
                  			ELSE Co.CompanyName
                  			END
                  			AS 'CustomerName'
                  	FROM Customer C
                  	LEFT JOIN [User] U ON U.UserId = C.UserId
                  	LEFT JOIN [Company] Co ON Co.CompanyId = C.CompanyId
                  ) AS CustomerName
                  INNER JOIN ItOrder Ord ON Ord.CustomerId = CustomerName.CustomerId
                  GROUP BY CustomerName.CustomerName
                  """;

        var ok = SqTSqlParser.TryParse(sql, out var expr, out var error);

        Assert.That(ok, Is.True, error);
        Assert.That(expr, Is.Not.Null);

        var exportedSql = expr!.ToSql(TSqlExporter.Default);
        Assert.That(exportedSql, Does.Contain("END [CustomerName]"));
        Assert.That(exportedSql, Does.Contain("GROUP BY [CustomerName].[CustomerName]"));
    }

    [Test]
    public void DeleteWithoutFromClause_Parses()
    {
        var sql = """
                  DELETE [User] WHERE UserId = @userId
                  """;

        var ok = SqTSqlParser.TryParse(sql, out var expr, out var error);

        Assert.That(ok, Is.True, error);
        Assert.That(expr, Is.TypeOf<ExprDelete>());
        var delete = (ExprDelete)expr!;
        Assert.That(delete.Source, Is.Null);
        Assert.That(delete.Target.FullName.AsExprTableFullName().TableName.Name, Is.EqualTo("User"));
        Assert.That(delete.Filter, Is.Not.Null);
    }

    [TestCase("UNION", ExprQueryExpressionType.Union)]
    [TestCase("UNION ALL", ExprQueryExpressionType.UnionAll)]
    [TestCase("EXCEPT", ExprQueryExpressionType.Except)]
    public void Intersect_BindsMoreTightlyThanUnionOrExcept(string operation, ExprQueryExpressionType expectedRoot)
    {
        var expression = SqTSqlParser.Parse($"SELECT 1 {operation} SELECT 2 INTERSECT SELECT 2");

        Assert.That(expression, Is.TypeOf<ExprQueryExpression>());
        var query = (ExprQueryExpression)expression;
        Assert.That(query.QueryExpressionType, Is.EqualTo(expectedRoot));
        Assert.That(query.Right, Is.TypeOf<ExprQueryExpression>());
        Assert.That(((ExprQueryExpression)query.Right).QueryExpressionType, Is.EqualTo(ExprQueryExpressionType.Intersect));
    }

    [TestCase("SELECT 1 WHERE 'a_b' LIKE 'a!_b' ESCAPE '!'")]
    [TestCase("SELECT 1 WHERE 'a_b' NOT LIKE 'a!_b' ESCAPE '!'")]
    public void LikeEscape_WhenEscapeAffectsPattern_IsRejected(string sql)
    {
        AssertRejected(sql);
    }

    [Test]
    public void DecimalLiteralBeyondClrPrecision_IsRejectedInsteadOfRounded()
    {
        AssertRejected("SELECT 0.12345678901234567890123456789");
    }

    [TestCase("UPDATE TOP (1) dbo.Users SET Name = 'changed'")]
    [TestCase("DELETE TOP (1) FROM dbo.Users")]
    public void DmlTopWithoutWhere_IsRejected(string sql)
    {
        AssertRejected(sql);
    }

    [Test]
    public void MergeMultipleMatchedActions_IsRejectedInsteadOfDroppingFirstAction()
    {
        AssertRejected("MERGE dbo.Target AS t USING dbo.Source AS s ON t.Id = s.Id "
            + "WHEN MATCHED AND s.Flag = 1 THEN UPDATE SET Value = s.Value "
            + "WHEN MATCHED THEN DELETE;");
    }

    [Test]
    public void SchemaQualifiedTable_IsNotReplacedByCteWithSameName()
    {
        var expression = SqTSqlParser.Parse("WITH c AS (SELECT 1 AS Id) SELECT c.Id FROM dbo.c");
        Assert.That(expression.ToSql(TSqlExporter.Default), Does.Contain("[dbo].[c]"));
    }

    [Test]
    public void CteColumnList_ReplacesInnerProjectionNames()
    {
        AssertRejected("WITH c(Renamed) AS (SELECT 1 AS Original) SELECT c.Original FROM c");
    }

    [TestCase("SELECT 1 UNION SELECT 1, 2")]
    [TestCase("SELECT (SELECT 1, 2)")]
    [TestCase("SELECT 1 WHERE 1 IN (SELECT 1, 2)")]
    public void KnownQueryArityMismatch_IsRejected(string sql)
    {
        AssertRejected(sql);
    }

    [Test]
    public void WindowRowsFrame_IsPreserved()
    {
#pragma warning disable SQEX012 // Raw SQL deliberately uses a table without a compiled descriptor.
        var expression = SqTSqlParser.Parse(
            "SELECT SUM(u.Id) OVER (ORDER BY u.Id ROWS BETWEEN 1 PRECEDING AND CURRENT ROW) FROM dbo.Users u");
#pragma warning restore SQEX012
        var over = expression.SyntaxTree().DescendantsAndSelf().OfType<ExprOver>().Single();

        Assert.That(over.FrameClause, Is.Not.Null);
        Assert.That(over.FrameClause!.Start, Is.TypeOf<ExprValueFrameBorder>());
        Assert.That(over.FrameClause.End, Is.TypeOf<ExprCurrentRowFrameBorder>());
    }

    private static void AssertRejected(string sql)
    {
        var success = SqTSqlParser.TryParse(sql, out var expression, out var error);
        Assert.That(success, Is.False, "Input must fail closed: " + sql);
        Assert.That(expression, Is.Null);
        Assert.That(error, Is.Not.Null.And.Not.Empty);
    }
}
