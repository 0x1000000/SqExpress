using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using SqExpress.DbMetadata;
using SqExpress.SqlExport;
using SqExpress.SqlParser;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;
using SqExpress.SyntaxTreeOperations;

namespace SqExpress.Test.SqlParser
{
    public class TSqlParserBasicTest
    {
        [Test]
        public void ParseSimpleCrossJoinSelect_BuildsExpectedAstAndTableArtifacts()
        {

            var inputSql = "SELECT U.Id, U.Name, U2.Email, U2.Address FROM Users U CROSS JOIN Users U2";
            var outputSql = "SELECT [U].[Id],[U].[Name],[U2].[Email],[U2].[Address] FROM [dbo].[Users] [U] CROSS JOIN [dbo].[Users] [U2]";

            if (SqTSqlParser.TryParse(inputSql, out IExpr? expr, out var tables, out var errors))
            {
                Assert.That(expr, Is.TypeOf<ExprQuerySpecification>());
                var query = (ExprQuerySpecification)expr!;
                Assert.That(query.Distinct, Is.False);
                Assert.That(query.Top, Is.Null);
                Assert.That(query.Where, Is.Null);
                Assert.That(query.GroupBy, Is.Null);
                Assert.That(query.SelectList.Count, Is.EqualTo(4));

                var c0 = query.SelectList[0] as ExprColumn;
                var c1 = query.SelectList[1] as ExprColumn;
                var c2 = query.SelectList[2] as ExprColumn;
                var c3 = query.SelectList[3] as ExprColumn;
                Assert.That(c0, Is.Not.Null);
                Assert.That(c1, Is.Not.Null);
                Assert.That(c2, Is.Not.Null);
                Assert.That(c3, Is.Not.Null);
                Assert.That((c0!.Source as ExprTableAlias)?.Alias, Is.EqualTo((IExprAlias)new ExprAlias("U")));
                Assert.That(c0.ColumnName.Name, Is.EqualTo("Id"));
                Assert.That((c1!.Source as ExprTableAlias)?.Alias, Is.EqualTo((IExprAlias)new ExprAlias("U")));
                Assert.That(c1.ColumnName.Name, Is.EqualTo("Name"));
                Assert.That((c2!.Source as ExprTableAlias)?.Alias, Is.EqualTo((IExprAlias)new ExprAlias("U2")));
                Assert.That(c2.ColumnName.Name, Is.EqualTo("Email"));
                Assert.That((c3!.Source as ExprTableAlias)?.Alias, Is.EqualTo((IExprAlias)new ExprAlias("U2")));
                Assert.That(c3.ColumnName.Name, Is.EqualTo("Address"));

                Assert.That(query.From, Is.TypeOf<ExprCrossedTable>());
                var cross = (ExprCrossedTable)query.From!;
                var left = cross.Left as ExprTable;
                var right = cross.Right as ExprTable;
                Assert.That(left, Is.Not.Null);
                Assert.That(right, Is.Not.Null);
                Assert.That(left!.FullName.AsExprTableFullName().TableName.Name, Is.EqualTo("Users"));
                Assert.That(right!.FullName.AsExprTableFullName().TableName.Name, Is.EqualTo("Users"));
                Assert.That((left.Alias?.Alias as ExprAlias)?.Name, Is.EqualTo("U"));
                Assert.That((right.Alias?.Alias as ExprAlias)?.Name, Is.EqualTo("U2"));

                var refs = expr.SyntaxTree().Descendants().OfType<ExprTable>().ToList();
                Assert.That(refs.Count, Is.EqualTo(2));
                Assert.That(refs.Select(i => (i.Alias?.Alias as ExprAlias)?.Name).OrderBy(i => i).ToArray(), Is.EqualTo(new[] { "U", "U2" }));
                Assert.That(expr.SyntaxTree().Descendants().OfType<SqTable>().Any(), Is.False);

                Assert.That(tables, Is.Not.Null);
                Assert.That(tables!.Count, Is.EqualTo(1));
                var sqTable = tables.Single();
                Assert.That(sqTable.FullName.TableName, Is.EqualTo("Users"));
                Assert.That(sqTable.Columns.Count, Is.EqualTo(4));
                Assert.That(sqTable.Columns[0].ColumnName.Name, Is.EqualTo("Id"));
                Assert.That(sqTable.Columns[0].SqlType, Is.EqualTo(ExprTypeInt32.Instance));
                Assert.That(sqTable.Columns[1].ColumnName.Name, Is.EqualTo("Name"));
                Assert.That(sqTable.Columns[1].SqlType is ExprTypeString s ? s.Size : -1, Is.EqualTo(255));
                Assert.That(sqTable.Columns[2].ColumnName.Name, Is.EqualTo("Email"));
                Assert.That(sqTable.Columns[2].SqlType is ExprTypeString s1 ? s1.Size : -1, Is.EqualTo(255));
                Assert.That(sqTable.Columns[3].ColumnName.Name, Is.EqualTo("Address"));
                Assert.That(sqTable.Columns[3].SqlType is ExprTypeString s2 ? s2.Size : -1, Is.EqualTo(1000));

                var actualSql = expr.ToSql(TSqlExporter.Default);

                Assert.That(actualSql, Is.EqualTo(outputSql));
            }
            else
            {
                Assert.Fail(errors);
            }
        }

        [Test]
        public void ParseUpdateWithJoinAndLike_BuildsExpectedAstNodes()
        {

            var inputSql = "UPDATE u SET u.[Name]=[o].[Title],[u].[IsActive]=1 FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] WHERE [o].[Title] LIKE 'A%'";

            if (SqTSqlParser.TryParse(inputSql, out var exp, out var errors))
            {
                Assert.That(exp, Is.Not.Null);
                Assert.That(exp, Is.TypeOf<ExprUpdate>());

                var update = (ExprUpdate)exp;
                Assert.That(update.SetClause.Count, Is.EqualTo(2));

                var target = update.Target;
                Assert.That(target.FullName.AsExprTableFullName().TableName.Name, Is.EqualTo("Users"));
                Assert.That(target.FullName.AsExprTableFullName().DbSchema?.Schema.Name, Is.EqualTo("dbo"));
                Assert.That((target.Alias?.Alias as ExprAlias)?.Name, Is.EqualTo("u"));

                var source = update.Source as ExprJoinedTable;
                Assert.That(source, Is.Not.Null);
                Assert.That(source!.JoinType, Is.EqualTo(ExprJoinedTable.ExprJoinType.Inner));

                var sourceLeft = source.Left as ExprTable;
                Assert.That(sourceLeft, Is.Not.Null);
                Assert.That(sourceLeft!.FullName.AsExprTableFullName().TableName.Name, Is.EqualTo("Users"));
                Assert.That((sourceLeft.Alias?.Alias as ExprAlias)?.Name, Is.EqualTo("u"));

                var sourceRight = source.Right as ExprTable;
                Assert.That(sourceRight, Is.Not.Null);
                Assert.That(sourceRight!.FullName.AsExprTableFullName().TableName.Name, Is.EqualTo("Orders"));
                Assert.That((sourceRight.Alias?.Alias as ExprAlias)?.Name, Is.EqualTo("o"));

                var joinCondition = source.SearchCondition as ExprBooleanEq;
                Assert.That(joinCondition, Is.Not.Null);
                var joinLeft = joinCondition!.Left as ExprColumn;
                var joinRight = joinCondition.Right as ExprColumn;
                Assert.That(joinLeft, Is.Not.Null);
                Assert.That(joinRight, Is.Not.Null);
                Assert.That((joinLeft!.Source as ExprTableAlias)?.Alias, Is.EqualTo((IExprAlias)new ExprAlias("o")));
                Assert.That(joinLeft.ColumnName.Name, Is.EqualTo("UserId"));
                Assert.That((joinRight!.Source as ExprTableAlias)?.Alias, Is.EqualTo((IExprAlias)new ExprAlias("u")));
                Assert.That(joinRight.ColumnName.Name, Is.EqualTo("UserId"));

                var firstSet = update.SetClause[0];
                Assert.That((firstSet.Column.Source as ExprTableAlias)?.Alias, Is.EqualTo((IExprAlias)new ExprAlias("u")));
                Assert.That(firstSet.Column.ColumnName.Name, Is.EqualTo("Name"));
                var firstSetValue = firstSet.Value as ExprColumn;
                Assert.That(firstSetValue, Is.Not.Null);
                Assert.That((firstSetValue!.Source as ExprTableAlias)?.Alias, Is.EqualTo((IExprAlias)new ExprAlias("o")));
                Assert.That(firstSetValue.ColumnName.Name, Is.EqualTo("Title"));

                var secondSet = update.SetClause[1];
                Assert.That((secondSet.Column.Source as ExprTableAlias)?.Alias, Is.EqualTo((IExprAlias)new ExprAlias("u")));
                Assert.That(secondSet.Column.ColumnName.Name, Is.EqualTo("IsActive"));
                Assert.That((secondSet.Value as ExprInt32Literal)?.Value, Is.EqualTo(1));

                var filter = update.Filter as ExprLike;
                Assert.That(filter, Is.Not.Null);
                var likeTest = filter!.Test as ExprColumn;
                Assert.That(likeTest, Is.Not.Null);
                Assert.That((likeTest!.Source as ExprTableAlias)?.Alias, Is.EqualTo((IExprAlias)new ExprAlias("o")));
                Assert.That(likeTest.ColumnName.Name, Is.EqualTo("Title"));
                Assert.That((filter.Pattern as ExprStringLiteral)?.Value, Is.EqualTo("A%"));
            }
            else
            {
                Assert.Fail(errors == null ? null : string.Join("\n", errors));
            }
        }

        [Test]
        public void ParseComplexQueryWithCteAndOuterApply_BuildsExpectedAstNodes()
        {

            var inputSql = "WITH R AS(SELECT 1 UNION ALL SELECT [r].[N]+1 FROM [R] [r] WHERE [r].[N]<3)SELECT [u].[UserId],ISNULL([u].[Name],'NA') [UserName],LEN([u].[Name]) [NameLen],ROW_NUMBER()OVER(ORDER BY [u].[UserId]) [Rn],(SELECT COUNT(1) FROM (SELECT [l2].[UserId] FROM (SELECT [o3].[UserId] FROM [dbo].[Orders] [o3] WHERE [o3].[Amount]>0)[l2])[l1]) [NestedCnt],(SELECT COUNT(1) FROM [R] [rr] WHERE [rr].[N]<=2) [DepthCnt] FROM [dbo].[Users] [u] OUTER APPLY (SELECT [o].[OrderId] FROM [dbo].[Orders] [o] ORDER BY [o].[OrderId] DESC OFFSET 0 ROW FETCH NEXT 1 ROW ONLY)[oa] WHERE [u].[UserId] IN(SELECT [x].[UserId] FROM (SELECT [y].[UserId] FROM (SELECT [o2].[UserId] FROM [dbo].[Orders] [o2] WHERE [o2].[Amount]>0)[y])[x]) AND EXISTS(SELECT 1 FROM [R] [rx] WHERE [rx].[N]=1) ORDER BY [u].[UserId] OFFSET 0 ROW FETCH NEXT 20 ROW ONLY";

            if (SqTSqlParser.TryParse(inputSql, out var exp, out var errors))
            {
                Assert.That(exp, Is.Not.Null);
                Assert.That(exp, Is.TypeOf<ExprSelectOffsetFetch>());

                var query = (ExprSelectOffsetFetch)exp!;
                Assert.That(query.OrderBy.OrderList.Count, Is.EqualTo(1));
                Assert.That((query.OrderBy.OffsetFetch.Offset as ExprInt32Literal)?.Value, Is.EqualTo(0));
                Assert.That((query.OrderBy.OffsetFetch.Fetch as ExprInt32Literal)?.Value, Is.EqualTo(20));

                var specification = query.SelectQuery as ExprQuerySpecification;
                Assert.That(specification, Is.Not.Null);
                Assert.That(specification!.SelectList.Count, Is.EqualTo(6));
                Assert.That(specification.From, Is.TypeOf<ExprLateralCrossedTable>());

                var lateral = (ExprLateralCrossedTable)specification.From!;
                Assert.That(lateral.Outer, Is.True);
                var left = lateral.Left as ExprTable;
                var right = lateral.Right as ExprDerivedTableQuery;
                Assert.That(left, Is.Not.Null);
                Assert.That(right, Is.Not.Null);
                Assert.That(left!.FullName.AsExprTableFullName().TableName.Name, Is.EqualTo("Users"));
                Assert.That((left.Alias?.Alias as ExprAlias)?.Name, Is.EqualTo("u"));
                Assert.That((right!.Alias.Alias as ExprAlias)?.Name, Is.EqualTo("oa"));
                Assert.That(specification.Where, Is.Not.Null);

                var descendants = exp.SyntaxTree().DescendantsAndSelf().ToList();
                Assert.That(descendants.OfType<ExprCteQuery>().Any(i => i.Name == "R"), Is.True);
                Assert.That(descendants.OfType<ExprExists>().Any(), Is.True);
                Assert.That(descendants.OfType<ExprInSubQuery>().Any(), Is.True);
                Assert.That(descendants.OfType<ExprAnalyticFunction>().Any(i => i.Name.Name == "ROW_NUMBER"), Is.True);
                Assert.That(descendants.OfType<ExprAggregateFunction>().Any(i => i.Name.Name == "COUNT"), Is.True);
                Assert.That(descendants.OfType<ExprTable>().Any(), Is.True);

                var sql1 = exp.ToSql(TSqlExporter.Default);
                var roundTripOk = SqTSqlParser.TryParse(sql1, out IExpr? reparsed, out var roundTripError);
                Assert.That(roundTripOk, Is.True, roundTripError);
                Assert.That(reparsed, Is.Not.Null);
                var reparsedExpr = reparsed!;

                var sqlFromReparsed = reparsedExpr.ToSql(TSqlExporter.Default);
                Assert.That(sqlFromReparsed, Is.EqualTo(sql1), "Round-trip SQL should be stable.");

                var originalCounts = exp.SyntaxTree()
                    .DescendantsAndSelf()
                    .GroupBy(i => i.GetType().FullName ?? i.GetType().Name)
                    .ToDictionary(i => i.Key, i => i.Count(), StringComparer.Ordinal);

                var reparsedCounts = reparsedExpr.SyntaxTree()
                    .DescendantsAndSelf()
                    .GroupBy(i => i.GetType().FullName ?? i.GetType().Name)
                    .ToDictionary(i => i.Key, i => i.Count(), StringComparer.Ordinal);

                Assert.That(reparsedCounts.Count, Is.EqualTo(originalCounts.Count), "Node type set changed after round-trip.");
                foreach (var kv in originalCounts)
                {
                    Assert.That(reparsedCounts.TryGetValue(kv.Key, out var count), Is.True, "Missing node type after round-trip: " + kv.Key);
                    Assert.That(count, Is.EqualTo(kv.Value), "Node count mismatch for type: " + kv.Key);
                }

                var json = ToJson(exp);

                var restored = ExprDeserializer
                    .DeserializeFormJson(JsonDocument.Parse(json).RootElement);

                Assert.That(ToJson(restored), Is.EqualTo(json));

                var finalSql = restored.ToSql(TSqlExporter.Default);
                Assert.That(finalSql, Is.EqualTo(sql1), "Final SQL should come from restored expression and remain stable.");

            }
            else
            {
                Assert.Fail(errors == null ? null : string.Join("\n", errors));
            }

            string ToJson(IExpr result)
            {
                MemoryStream s = new MemoryStream();
                var utf8JsonWriter = new Utf8JsonWriter(s);
                result.SyntaxTree().ExportToJson(utf8JsonWriter);
                var json = Encoding.UTF8.GetString(s.ToArray());
                return json;
            }
        }

        [Test]
        public void ParseLargeMultiCteQuery_BuildsExpectedAstNodes()
        {

            var inputSql = "WITH Customers AS (SELECT c.CustomerID,c.CustomerName,c.Region FROM dbo.Customers c WHERE c.IsActive=1), OrdersBase AS (SELECT o.OrderID,o.CustomerID,o.OrderDate,o.TotalAmount,o.Status FROM dbo.Orders o WHERE o.OrderDate>=DATEADD(day,-90,CONVERT(date,GETDATE()))), LineAgg AS (SELECT ol.OrderID,SUM(ol.Quantity*ol.UnitPrice) AS LineTotal,COUNT(*) AS LineCount,MAX(ol.UnitPrice) AS MaxUnitPrice FROM dbo.OrderLines ol GROUP BY ol.OrderID), OrdersEnriched AS (SELECT ob.OrderID,ob.CustomerID,ob.OrderDate,ob.TotalAmount,ob.Status,la.LineTotal,la.LineCount,la.MaxUnitPrice,CASE WHEN la.LineTotal IS NULL THEN 0 ELSE la.LineTotal END AS SafeLineTotal FROM OrdersBase ob LEFT JOIN LineAgg la ON la.OrderID=ob.OrderID), Ranked AS (SELECT oe.*,ROW_NUMBER() OVER (PARTITION BY oe.CustomerID ORDER BY oe.TotalAmount DESC,oe.OrderDate DESC) AS rn,DENSE_RANK() OVER (ORDER BY oe.TotalAmount DESC) AS global_rank FROM OrdersEnriched oe), FilteredOrders AS (SELECT r.* FROM Ranked r WHERE r.Status IN ('Paid','Shipped') AND r.CustomerID IN (SELECT x.CustomerID FROM (SELECT y.CustomerID FROM (SELECT z.CustomerID FROM (SELECT ob.CustomerID,COUNT(*) AS Cnt FROM OrdersBase ob WHERE ob.TotalAmount>50 GROUP BY ob.CustomerID) z WHERE z.Cnt>=2) y WHERE EXISTS (SELECT 1 FROM Customers c WHERE c.CustomerID=y.CustomerID AND c.Region IN ('NA','EU'))) x) ), Final AS (SELECT fo.CustomerID,COUNT(*) AS OrdersCount,SUM(fo.TotalAmount) AS SumAmount,AVG(fo.TotalAmount) AS AvgAmount,MAX(fo.TotalAmount) AS MaxAmount,MIN(fo.OrderDate) AS FirstOrderDate,MAX(fo.OrderDate) AS LastOrderDate,MAX(CASE WHEN fo.rn=1 THEN fo.OrderID END) AS TopOrderId FROM FilteredOrders fo GROUP BY fo.CustomerID) SELECT c.CustomerID,c.CustomerName,c.Region,f.OrdersCount,f.SumAmount,f.AvgAmount,f.MaxAmount,f.FirstOrderDate,f.LastOrderDate,f.TopOrderId FROM Final f INNER JOIN Customers c ON c.CustomerID=f.CustomerID WHERE f.OrdersCount>=1 ORDER BY f.SumAmount DESC,c.CustomerName ASC;";

            if (SqTSqlParser.TryParse(inputSql, out var exp, out var errors))
            {
                Assert.That(exp, Is.Not.Null);
                Assert.That(exp, Is.TypeOf<ExprSelect>());

                var select = (ExprSelect)exp!;
                Assert.That(select.OrderBy.OrderList.Count, Is.EqualTo(2));
                Assert.That(select.SelectQuery, Is.TypeOf<ExprQuerySpecification>());

                var query = (ExprQuerySpecification)select.SelectQuery;
                Assert.That(query.SelectList.Count, Is.EqualTo(10));
                Assert.That(query.From, Is.TypeOf<ExprJoinedTable>());
                Assert.That(query.Where, Is.TypeOf<ExprBooleanGtEq>());

                var from = (ExprJoinedTable)query.From!;
                Assert.That(from.JoinType, Is.EqualTo(ExprJoinedTable.ExprJoinType.Inner));
                var left = from.Left as ExprCteQuery;
                var right = from.Right as ExprCteQuery;
                Assert.That(left, Is.Not.Null);
                Assert.That(right, Is.Not.Null);
                Assert.That(left!.Name, Is.EqualTo("Final"));
                Assert.That(right!.Name, Is.EqualTo("Customers"));

                var descendants = exp.SyntaxTree().DescendantsAndSelf().ToList();
                var cteNames = descendants.OfType<ExprCteQuery>().Select(i => i.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                Assert.That(cteNames, Is.SupersetOf(new[]
                {
                    "Customers",
                    "OrdersBase",
                    "LineAgg",
                    "OrdersEnriched",
                    "Ranked",
                    "FilteredOrders",
                    "Final"
                }));

                Assert.That(descendants.OfType<ExprAnalyticFunction>().Count(), Is.GreaterThanOrEqualTo(2));
                Assert.That(descendants.OfType<ExprAggregateFunction>().Count(), Is.GreaterThanOrEqualTo(6));
                Assert.That(descendants.OfType<ExprCase>().Count(), Is.GreaterThanOrEqualTo(2));
                Assert.That(descendants.OfType<ExprExists>().Any(), Is.True);
                Assert.That(descendants.OfType<ExprInSubQuery>().Any(), Is.True);
                Assert.That(descendants.OfType<ExprAllColumns>().Count(), Is.GreaterThanOrEqualTo(2));
                Assert.That(descendants.OfType<ExprTable>().Any(), Is.True);
            }
            else
            {
                Assert.Fail(errors == null ? null : string.Join("\n", errors));
            }
        }

        [Test]
        public void ExtractTableArtifacts_ForMultiJoinQuery_CollectsExpectedColumns()
        {
            var inputSql =
                "SELECT [u].[UserId],[o].[OrderId],[u2].[Name] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] LEFT JOIN [dbo].[Users] [u2] ON [u2].[UserId]=[o].[UserId] WHERE [u2].[IsActive]=1";

            if (SqTSqlParser.TryParse(inputSql, out IExpr? _, out var tables, out var errors))
            {
                Assert.That(tables, Is.Not.Null);
                Assert.That(tables!.Count, Is.EqualTo(2));

                var byTable = tables.ToDictionary(
                    i => i.FullName.AsExprTableFullName().TableName.Name,
                    i => i,
                    StringComparer.OrdinalIgnoreCase);

                Assert.That(byTable.Keys.ToArray(), Is.EquivalentTo(new[] { "Users", "Orders" }));
                Assert.That(byTable["Users"].Columns.Select(i => i.ColumnName.Name).ToArray(), Is.EquivalentTo(new[] { "UserId", "Name", "IsActive" }));
                Assert.That(byTable["Orders"].Columns.Select(i => i.ColumnName.Name).ToArray(), Is.EquivalentTo(new[] { "OrderId", "UserId" }));
            }
            else
            {
                Assert.Fail(errors);
            }
        }
    }
}


