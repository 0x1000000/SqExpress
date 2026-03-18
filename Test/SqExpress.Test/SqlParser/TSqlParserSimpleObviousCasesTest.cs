using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SqExpress.SqlExport;
using SqExpress.SqlParser;
using SqExpress.Syntax.Value;

namespace SqExpress.Test.SqlParser
{
    public class TSqlParserSimpleObviousCasesTest
    {
        [TestCaseSource(nameof(Cases))]
        public void SimpleObviousCases_ParseAndRoundTrip(string name, string sql)
        {
            var ok = SqTSqlParser.TryParse(sql, out var expr, out var error);

            Assert.That(ok, Is.True, $"Case '{name}' should parse, but parser returned: {error}");
            Assert.That(expr, Is.Not.Null, $"Case '{name}' should produce non-null expression.");
            Assert.That(expr!.SyntaxTree().DescendantsAndSelf().OfType<ExprUnsafeValue>(), Is.Empty, $"Case '{name}' should not contain unsafe values.");

            var exportedSql = expr!.ToSql(TSqlExporter.Default);

            var roundTripOk = SqTSqlParser.TryParse(exportedSql, out var reparsed, out var roundTripError);
            Assert.That(roundTripOk, Is.True, $"Case '{name}' should reparse after export, but parser returned: {roundTripError}");
            Assert.That(reparsed, Is.Not.Null, $"Case '{name}' reparsed expression should be non-null.");
            Assert.That(reparsed!.ToSql(TSqlExporter.Default), Is.EqualTo(exportedSql), $"Case '{name}' round-trip SQL should be stable.");
        }

        [TestCaseSource(nameof(UnhappyCases))]
        public void SimpleObviousCases_RejectMalformedSql(string name, string sql, string? expectedErrorFragment)
        {
            var ok = SqTSqlParser.TryParse(sql, out var expr, out var error);

            Assert.That(ok, Is.False, $"Case '{name}' should be rejected, but parser returned success.");
            Assert.That(expr, Is.Null, $"Case '{name}' should not produce expression.");
            Assert.That(error, Is.Not.Null.And.Not.Empty, $"Case '{name}' should report an error.");

            if (!string.IsNullOrWhiteSpace(expectedErrorFragment))
            {
                Assert.That(error, Does.Contain(expectedErrorFragment), $"Case '{name}' should mention '{expectedErrorFragment}'.");
            }
        }

        private static IEnumerable<TestCaseData> Cases()
        {
            foreach (var item in ScalarSelectCases())
            {
                yield return item;
            }

            foreach (var item in SingleTableSelectCases())
            {
                yield return item;
            }

            foreach (var item in AggregateAndSetCases())
            {
                yield return item;
            }

            foreach (var item in JoinApplyDerivedAndCteCases())
            {
                yield return item;
            }

            foreach (var item in InsertCases())
            {
                yield return item;
            }

            foreach (var item in UpdateCases())
            {
                yield return item;
            }

            foreach (var item in DeleteCases())
            {
                yield return item;
            }

            foreach (var item in MergeCases())
            {
                yield return item;
            }
        }

        private static IEnumerable<TestCaseData> UnhappyCases()
        {
            foreach (var item in MalformedSelectCases())
            {
                yield return item;
            }

            foreach (var item in MalformedUpdateCases())
            {
                yield return item;
            }

            foreach (var item in MalformedDeleteCases())
            {
                yield return item;
            }

            foreach (var item in MalformedMergeCases())
            {
                yield return item;
            }
        }

        private static IEnumerable<TestCaseData> ScalarSelectCases()
        {
            var cases = new (string Name, string Sql)[]
            {
                ("Select_Const_Int", "SELECT 1"),
                ("Select_Const_String", "SELECT 'A'"),
                ("Select_Const_Unicode", "SELECT N'A'"),
                ("Select_Const_Null", "SELECT NULL"),
                ("Select_Addition", "SELECT 1+2"),
                ("Select_MixedArithmetic", "SELECT (1+2)*3/2"),
                ("Select_Case", "SELECT CASE WHEN 1=1 THEN 'Y' ELSE 'N' END"),
                ("Select_Cast_IntToBigInt", "SELECT CAST(1 AS BIGINT)"),
                ("Select_Convert_Int", "SELECT CONVERT(INT, 1)"),
                ("Select_IsNull", "SELECT ISNULL(NULL, 1)"),
                ("Select_Coalesce", "SELECT COALESCE(NULL, 1, 2)"),
                ("Select_NullIf", "SELECT NULLIF(1, 2)"),
                ("Select_Abs", "SELECT ABS(-1)"),
                ("Select_Lower", "SELECT LOWER('A')"),
                ("Select_Upper", "SELECT UPPER('a')"),
                ("Select_Trim", "SELECT TRIM(' A ')"),
                ("Select_LTrim", "SELECT LTRIM(' A')"),
                ("Select_RTrim", "SELECT RTRIM('A ')"),
                ("Select_Replace", "SELECT REPLACE('abc', 'b', 'x')"),
                ("Select_Substring", "SELECT SUBSTRING('abc', 1, 2)"),
                ("Select_Round", "SELECT ROUND(12.34, 1)"),
                ("Select_Floor", "SELECT FLOOR(12.9)"),
                ("Select_Ceiling", "SELECT CEILING(12.1)"),
                ("Select_Len", "SELECT LEN('abc')"),
                ("Select_DataLength", "SELECT DATALENGTH('abc')"),
                ("Select_Year", "SELECT YEAR('2026-01-01')"),
                ("Select_Month", "SELECT MONTH('2026-01-01')"),
                ("Select_Day", "SELECT DAY('2026-01-01')"),
                ("Select_DateAdd", "SELECT DATEADD(day, 1, '2026-01-01')"),
                ("Select_DateDiff", "SELECT DATEDIFF(day, '2026-01-01', '2026-01-02')"),
                ("Select_GetDate", "SELECT GETDATE()"),
                ("Select_GetUtcDate", "SELECT GETUTCDATE()"),
            };

            return cases.Select(i => Case(i.Name, i.Sql));
        }

        private static IEnumerable<TestCaseData> SingleTableSelectCases()
        {
            var projections = new[]
            {
                ("UserId", "u.UserId", "ORDER BY u.UserId"),
                ("Name", "u.Name", "ORDER BY u.Name"),
                ("UserIdName", "u.UserId, u.Name", "ORDER BY u.UserId"),
                ("AliasedName", "u.Name AS UserName", "ORDER BY UserName")
            };

            var filters = new[]
            {
                ("All", ""),
                ("Eq", " WHERE u.UserId = 1"),
                ("In", " WHERE u.UserId IN(1,2,3)"),
                ("Like", " WHERE u.Name LIKE 'A%'"),
                ("IsNull", " WHERE u.Name IS NULL"),
                ("Bool", " WHERE u.IsActive = 1")
            };

            var wrappers = new[]
            {
                ("Plain", "SELECT {0} FROM Users u{1}{2}"),
                ("Distinct", "SELECT DISTINCT {0} FROM Users u{1}{2}"),
                ("Top", "SELECT TOP (5) {0} FROM Users u{1}{2}")
            };

            foreach (var projection in projections)
            {
                foreach (var filter in filters)
                {
                    foreach (var wrapper in wrappers)
                    {
                        var orderClause = " " + projection.Item3;
                        yield return Case(
                            "SelectSingle_" + projection.Item1 + "_" + filter.Item1 + "_" + wrapper.Item1,
                            string.Format(wrapper.Item2, projection.Item2, filter.Item2, orderClause));
                    }
                }
            }
        }

        private static IEnumerable<TestCaseData> AggregateAndSetCases()
        {
            var cases = new (string Name, string Sql)[]
            {
                ("Select_CountStar", "SELECT COUNT(1) FROM Users u"),
                ("Select_CountStar_NoAlias", "SELECT COUNT(1) FROM Users"),
                ("Select_CountColumn", "SELECT COUNT(u.UserId) FROM Users u"),
                ("Select_CountColumn_NoAlias", "SELECT COUNT(UserId) FROM Users"),
                ("Select_SumColumn", "SELECT SUM(o.Amount) FROM Orders o"),
                ("Select_SumColumn_NoAlias", "SELECT SUM(Amount) FROM Orders"),
                ("Select_MinColumn", "SELECT MIN(o.Amount) FROM Orders o"),
                ("Select_MinColumn_NoAlias", "SELECT MIN(Amount) FROM Orders"),
                ("Select_MaxColumn", "SELECT MAX(o.Amount) FROM Orders o"),
                ("Select_MaxColumn_NoAlias", "SELECT MAX(Amount) FROM Orders"),
                ("Select_AvgColumn", "SELECT AVG(o.Amount) FROM Orders o"),
                ("Select_AvgColumn_NoAlias", "SELECT AVG(Amount) FROM Orders"),
                ("Select_GroupByOneColumn", "SELECT o.UserId, COUNT(1) FROM Orders o GROUP BY o.UserId"),
                ("Select_GroupByOneColumn_NoAlias", "SELECT UserId, COUNT(1) FROM Orders GROUP BY UserId"),
                ("Select_GroupByTwoColumns", "SELECT o.UserId, o.IsDeleted, COUNT(1) FROM Orders o GROUP BY o.UserId, o.IsDeleted"),
                ("Select_GroupByTwoColumns_NoAlias", "SELECT UserId, IsDeleted, COUNT(1) FROM Orders GROUP BY UserId, IsDeleted"),
                ("Select_OrderByOffsetFetch", "SELECT u.UserId FROM Users u ORDER BY u.UserId OFFSET 0 ROW FETCH NEXT 10 ROW ONLY"),
                ("Select_OrderByOffsetFetch_NoAlias", "SELECT UserId FROM Users ORDER BY UserId OFFSET 0 ROW FETCH NEXT 10 ROW ONLY"),
                ("Select_RowNumberOverOrder", "SELECT ROW_NUMBER() OVER(ORDER BY u.UserId) AS Rn FROM Users u"),
                ("Select_RowNumberOverOrder_NoAlias", "SELECT ROW_NUMBER() OVER(ORDER BY UserId) AS Rn FROM Users"),
                ("Select_DenseRankOverPartition", "SELECT DENSE_RANK() OVER(PARTITION BY u.CompanyId ORDER BY u.UserId) AS Rn FROM Users u"),
                ("Select_DenseRankOverPartition_NoAlias", "SELECT DENSE_RANK() OVER(PARTITION BY CompanyId ORDER BY UserId) AS Rn FROM Users"),
                ("Select_SumOverOrder", "SELECT SUM(o.Amount) OVER(ORDER BY o.OrderId) AS RunningAmount FROM Orders o"),
                ("Select_SumOverOrder_NoAlias", "SELECT SUM(Amount) OVER(ORDER BY OrderId) AS RunningAmount FROM Orders"),
                ("Select_Union", "SELECT 1 AS A UNION SELECT 2 AS A"),
                ("Select_UnionAll", "SELECT 1 AS A UNION ALL SELECT 2 AS A"),
                ("Select_Intersect", "SELECT 1 AS A INTERSECT SELECT 1 AS A"),
                ("Select_Except", "SELECT 1 AS A EXCEPT SELECT 2 AS A"),
                ("Select_Exists", "SELECT 1 WHERE EXISTS(SELECT 1 FROM Users u)"),
                ("Select_Exists_NoAlias", "SELECT 1 WHERE EXISTS(SELECT 1 FROM Users)"),
                ("Select_InSubQuery", "SELECT 1 WHERE 1 IN(SELECT u.UserId FROM Users u)"),
                ("Select_InSubQuery_NoAlias", "SELECT 1 WHERE 1 IN(SELECT UserId FROM Users)"),
                ("Select_ScalarSubQuery", "SELECT (SELECT MAX(o.Amount) FROM Orders o) AS MaxAmount"),
                ("Select_ScalarSubQuery_NoAlias", "SELECT (SELECT MAX(Amount) FROM Orders) AS MaxAmount"),
                ("Select_CorrelatedScalarSubQuery", "SELECT u.UserId, (SELECT COUNT(1) FROM Orders o WHERE o.UserId = u.UserId) AS OrdersNum FROM Users u"),
                ("Select_ExistsPredicateWithFrom", "SELECT u.UserId FROM Users u WHERE EXISTS(SELECT 1 FROM Orders o WHERE o.UserId = u.UserId)"),
                ("Select_NotExistsPredicate", "SELECT u.UserId FROM Users u WHERE NOT EXISTS(SELECT 1 FROM Orders o WHERE o.UserId = u.UserId)"),
                ("Select_InPredicateWithSubQuery", "SELECT u.UserId FROM Users u WHERE u.UserId IN(SELECT o.UserId FROM Orders o)"),
                ("Select_NotEq", "SELECT u.UserId FROM Users u WHERE u.UserId <> 1"),
                ("Select_GreaterOrEqual", "SELECT u.UserId FROM Users u WHERE u.UserId >= 1"),
                ("Select_LessOrEqual", "SELECT u.UserId FROM Users u WHERE u.UserId <= 10"),
                ("Select_OrPredicate", "SELECT u.UserId FROM Users u WHERE u.Name = 'A' OR u.Name = 'B'"),
                ("Select_AndPredicate", "SELECT u.UserId FROM Users u WHERE u.UserId > 0 AND u.IsActive = 1"),
            };

            return cases.Select(i => Case(i.Name, i.Sql));
        }

        private static IEnumerable<TestCaseData> JoinApplyDerivedAndCteCases()
        {
            var cases = new (string Name, string Sql)[]
            {
                ("Join_Inner", "SELECT u.UserId, o.OrderId FROM Users u INNER JOIN Orders o ON o.UserId = u.UserId"),
                ("Join_Inner_NoAlias", "SELECT Users.UserId, Orders.OrderId FROM Users INNER JOIN Orders ON Orders.UserId = Users.UserId"),
                ("Join_Left", "SELECT u.UserId, o.OrderId FROM Users u LEFT JOIN Orders o ON o.UserId = u.UserId"),
                ("Join_Left_NoAlias", "SELECT Users.UserId, Orders.OrderId FROM Users LEFT JOIN Orders ON Orders.UserId = Users.UserId"),
                ("Join_Right", "SELECT u.UserId, o.OrderId FROM Users u RIGHT JOIN Orders o ON o.UserId = u.UserId"),
                ("Join_Right_NoAlias", "SELECT Users.UserId, Orders.OrderId FROM Users RIGHT JOIN Orders ON Orders.UserId = Users.UserId"),
                ("Join_Full", "SELECT u.UserId, o.OrderId FROM Users u FULL JOIN Orders o ON o.UserId = u.UserId"),
                ("Join_Full_NoAlias", "SELECT Users.UserId, Orders.OrderId FROM Users FULL JOIN Orders ON Orders.UserId = Users.UserId"),
                ("Join_Cross", "SELECT u.UserId, c.CompanyId FROM Users u CROSS JOIN Company c"),
                ("Join_Cross_NoAlias", "SELECT Users.UserId, Company.CompanyId FROM Users CROSS JOIN Company"),
                ("Join_InnerWhere", "SELECT u.UserId, o.OrderId FROM Users u INNER JOIN Orders o ON o.UserId = u.UserId WHERE o.IsDeleted = 0"),
                ("Join_LeftWhere", "SELECT u.UserId, o.OrderId FROM Users u LEFT JOIN Orders o ON o.UserId = u.UserId WHERE o.OrderId IS NULL"),
                ("Join_FullOrder", "SELECT u.UserId, o.OrderId FROM Users u FULL JOIN Orders o ON o.UserId = u.UserId ORDER BY u.UserId"),
                ("Join_TwoInner", "SELECT u.UserId, o.OrderId, c.CustomerId FROM Users u INNER JOIN Orders o ON o.UserId = u.UserId INNER JOIN Customer c ON c.UserId = u.UserId"),
                ("Join_TwoInner_NoAlias", "SELECT Users.UserId, Orders.OrderId, Customer.CustomerId FROM Users INNER JOIN Orders ON Orders.UserId = Users.UserId INNER JOIN Customer ON Customer.UserId = Users.UserId"),
                ("Join_JoinAndCross", "SELECT u.UserId, o.OrderId, c.CompanyId FROM Users u INNER JOIN Orders o ON o.UserId = u.UserId CROSS JOIN Company c"),
                ("Apply_Cross", "SELECT u.UserId, oa.OrderId FROM Users u CROSS APPLY (SELECT TOP (1) o.OrderId FROM Orders o WHERE o.UserId = u.UserId ORDER BY o.OrderId DESC) oa"),
                ("Apply_Outer", "SELECT u.UserId, oa.OrderId FROM Users u OUTER APPLY (SELECT TOP (1) o.OrderId FROM Orders o WHERE o.UserId = u.UserId ORDER BY o.OrderId DESC) oa"),
                ("Apply_CrossDerivedValues", "SELECT u.UserId, oa.Id FROM Users u CROSS APPLY (SELECT u.UserId AS Id) oa"),
                ("Apply_OuterDerivedValues", "SELECT u.UserId, oa.Id FROM Users u OUTER APPLY (SELECT u.UserId AS Id) oa"),
                ("Derived_Simple", "SELECT sq.UserId FROM (SELECT u.UserId FROM Users u) sq"),
                ("Derived_WithWhere", "SELECT sq.UserId FROM (SELECT u.UserId FROM Users u WHERE u.IsActive = 1) sq"),
                ("Derived_WithAliasRename", "SELECT sq.DisplayName FROM (SELECT u.Name AS DisplayName FROM Users u) sq"),
                ("Derived_TwoLevels", "SELECT sq.UserId FROM (SELECT x.UserId FROM (SELECT u.UserId FROM Users u) x) sq"),
                ("Derived_WithUnionAll", "SELECT sq.A FROM (SELECT 1 AS A UNION ALL SELECT 2 AS A) sq"),
                ("Derived_WithOffsetFetch", "SELECT sq.A FROM (SELECT 1 AS A ORDER BY A OFFSET 0 ROW FETCH NEXT 1 ROW ONLY) sq"),
                ("Cte_SelectSimple", "WITH R AS(SELECT 1 AS N) SELECT R.N FROM R"),
                ("Cte_SelectFromTable", "WITH ActiveUsers AS(SELECT u.UserId FROM Users u WHERE u.IsActive = 1) SELECT au.UserId FROM ActiveUsers au"),
                ("Cte_Multiple", "WITH A AS(SELECT 1 AS N), B AS(SELECT A.N FROM A) SELECT B.N FROM B"),
                ("Cte_Recursive", "WITH R AS(SELECT 1 AS N UNION ALL SELECT r.N + 1 FROM R r WHERE r.N < 3) SELECT R.N FROM R"),
                ("SubQuery_Exists", "SELECT u.UserId FROM Users u WHERE EXISTS(SELECT 1 FROM Orders o WHERE o.UserId = u.UserId)"),
                ("SubQuery_In", "SELECT u.UserId FROM Users u WHERE u.UserId IN(SELECT o.UserId FROM Orders o WHERE o.IsDeleted = 0)"),
                ("SubQuery_Scalar", "SELECT u.UserId, (SELECT COUNT(1) FROM Orders o WHERE o.UserId = u.UserId) AS OrdersNum FROM Users u"),
                ("SubQuery_DerivedJoin", "SELECT u.UserId, d.OrderId FROM Users u INNER JOIN (SELECT o.UserId, o.OrderId FROM Orders o) d ON d.UserId = u.UserId"),
                ("Select_WildcardSingle", "SELECT u.* FROM Users u"),
                ("Select_WildcardSingle_NoAlias", "SELECT Users.* FROM Users"),
                ("Select_WildcardJoin", "SELECT u.*, o.OrderId FROM Users u INNER JOIN Orders o ON o.UserId = u.UserId"),
                ("Select_WildcardJoin_NoAlias", "SELECT Users.*, Orders.OrderId FROM Users INNER JOIN Orders ON Orders.UserId = Users.UserId"),
                ("Select_TableFunctionLikeSource", "SELECT s.value FROM STRING_SPLIT('a,b', ',') s"),
                ("Select_DboFunctionCall", "SELECT dbo.NormalizeUserName(u.Name) AS NormalizedName FROM Users u"),
                ("Select_DboFunctionCall_NoAlias", "SELECT dbo.NormalizeUserName(Name) AS NormalizedName FROM Users"),
                ("Select_ValuesDerived", "SELECT v.Id FROM (VALUES (1), (2)) v(Id)"),
                ("Select_ValuesDerivedTwoCols", "SELECT v.Id, v.Name FROM (VALUES (1, 'A'), (2, 'B')) v(Id, Name)"),
                ("Select_CrossJoinDerived", "SELECT u.UserId, d.Id FROM Users u CROSS JOIN (SELECT 1 AS Id) d"),
                ("Select_ApplyWithValuesDerived", "SELECT u.UserId, a.Id FROM Users u CROSS APPLY (VALUES (u.UserId)) a(Id)")
            };

            return cases.Select(i => Case(i.Name, i.Sql));
        }

        private static IEnumerable<TestCaseData> InsertCases()
        {
            var cases = new (string Name, string Sql)[]
            {
                ("Insert_Values_OneCol_OneRow", "INSERT INTO Users(UserId) VALUES (1)"),
                ("Insert_Values_TwoCols_OneRow", "INSERT INTO Users(UserId, Name) VALUES (1, 'A')"),
                ("Insert_Values_ThreeCols_OneRow", "INSERT INTO Users(UserId, Name, IsActive) VALUES (1, 'A', 1)"),
                ("Insert_Values_NoColumns_OneRow", "INSERT INTO Users VALUES (1, 'A')"),
                ("Insert_Values_OneCol_MultiRow", "INSERT INTO Users(UserId) VALUES (1), (2)"),
                ("Insert_Values_TwoCols_MultiRow", "INSERT INTO Users(UserId, Name) VALUES (1, 'A'), (2, 'B')"),
                ("Insert_Values_ThreeCols_MultiRow", "INSERT INTO Users(UserId, Name, IsActive) VALUES (1, 'A', 1), (2, 'B', 0)"),
                ("Insert_Values_NoColumns_MultiRow", "INSERT INTO Users VALUES (1, 'A'), (2, 'B')"),
                ("Insert_Select_OneCol", "INSERT INTO Users(UserId) SELECT o.UserId FROM Orders o"),
                ("Insert_Select_OneCol_NoAlias", "INSERT INTO Users(UserId) SELECT UserId FROM Orders"),
                ("Insert_Select_TwoCols", "INSERT INTO Users(UserId, Name) SELECT o.UserId, o.Title FROM Orders o"),
                ("Insert_Select_TwoCols_NoAlias", "INSERT INTO Users(UserId, Name) SELECT UserId, Title FROM Orders"),
                ("Insert_Select_WithWhere", "INSERT INTO Users(UserId, Name) SELECT o.UserId, o.Title FROM Orders o WHERE o.IsDeleted = 0"),
                ("Insert_Select_WithWhere_NoAlias", "INSERT INTO Users(UserId, Name) SELECT UserId, Title FROM Orders WHERE IsDeleted = 0"),
                ("Insert_Select_WithJoin", "INSERT INTO Users(UserId, Name) SELECT o.UserId, u.Name FROM Orders o INNER JOIN Users u ON u.UserId = o.UserId"),
                ("Insert_Output_OneCol", "INSERT INTO Users(UserId) OUTPUT INSERTED.UserId VALUES (1)"),
                ("Insert_Output_TwoCols", "INSERT INTO Users(UserId, Name) OUTPUT INSERTED.UserId VALUES (1, 'A')"),
                ("Insert_Output_NoColumns", "INSERT INTO Users OUTPUT INSERTED.UserId VALUES (1, 'A')"),
                ("Insert_Output_Select", "INSERT INTO Users(UserId, Name) OUTPUT INSERTED.UserId SELECT o.UserId, o.Title FROM Orders o"),
                ("Insert_DboQualified", "INSERT INTO dbo.Users(UserId, Name) VALUES (1, 'A')"),
                ("Insert_Bracketed", "INSERT INTO [Users]([UserId], [Name]) VALUES (1, 'A')"),
                ("Insert_SelectDistinct", "INSERT INTO Users(UserId) SELECT DISTINCT o.UserId FROM Orders o"),
                ("Insert_SelectTop", "INSERT INTO Users(UserId) SELECT TOP (5) o.UserId FROM Orders o ORDER BY o.UserId"),
                ("Insert_SelectScalar", "INSERT INTO Users(UserId, Name) SELECT 1, 'A'"),
                ("Insert_SelectUnionAll", "INSERT INTO Users(UserId) SELECT 1 UNION ALL SELECT 2"),
                ("Insert_Output_MultiRow", "INSERT INTO Users(UserId, Name) OUTPUT INSERTED.UserId VALUES (1, 'A'), (2, 'B')"),
                ("Insert_Output_SelectWhere", "INSERT INTO Users(UserId, Name) OUTPUT INSERTED.UserId SELECT o.UserId, o.Title FROM Orders o WHERE o.IsDeleted = 0")
            };

            return cases.Select(i => Case(i.Name, i.Sql));
        }

        private static IEnumerable<TestCaseData> UpdateCases()
        {
            var cases = new (string Name, string Sql)[]
            {
                ("Update_Simple_NoFrom", "UPDATE Users SET Name = 'A'"),
                ("Update_Simple_NoFrom_Where", "UPDATE Users SET Name = 'A' WHERE UserId = 1"),
                ("Update_MultiSet_NoFrom", "UPDATE Users SET Name = 'A', IsActive = 1 WHERE UserId = 1"),
                ("Update_SchemaQualified_NoFrom", "UPDATE dbo.Users SET Name = 'A' WHERE UserId = 1"),
                ("Update_Bracketed_NoFrom", "UPDATE [Users] SET [Name] = 'A' WHERE [UserId] = 1"),
                ("Update_Function_NoFrom", "UPDATE Users SET ModifiedAt = GETUTCDATE() WHERE UserId = 1"),
                ("Update_Function_NoFrom_NoAliasRef", "UPDATE Users SET ModifiedAt = GETUTCDATE() WHERE ModifiedAt IS NULL"),
                ("Update_Alias_FromSameTable", "UPDATE u SET u.Name = 'A' FROM Users u WHERE u.UserId = 1"),
                ("Update_Alias_MultiSet", "UPDATE u SET u.Name = 'A', u.IsActive = 1 FROM Users u WHERE u.UserId = 1"),
                ("Update_Join", "UPDATE u SET u.Name = o.Title FROM Users u INNER JOIN Orders o ON o.UserId = u.UserId"),
                ("Update_Join_Where", "UPDATE u SET u.Name = o.Title FROM Users u INNER JOIN Orders o ON o.UserId = u.UserId WHERE o.IsDeleted = 0"),
                ("Update_LeftJoin", "UPDATE u SET u.Name = o.Title FROM Users u LEFT JOIN Orders o ON o.UserId = u.UserId WHERE o.OrderId IS NOT NULL"),
                ("Update_DerivedSource", "UPDATE u SET u.Name = d.Title FROM Users u INNER JOIN (SELECT o.UserId, o.Title FROM Orders o) d ON d.UserId = u.UserId"),
                ("Update_CrossApply", "UPDATE u SET u.Name = oa.Title FROM Users u CROSS APPLY (SELECT TOP (1) o.Title FROM Orders o WHERE o.UserId = u.UserId ORDER BY o.OrderId DESC) oa"),
                ("Update_OuterApply", "UPDATE u SET u.Name = oa.Title FROM Users u OUTER APPLY (SELECT TOP (1) o.Title FROM Orders o WHERE o.UserId = u.UserId ORDER BY o.OrderId DESC) oa WHERE oa.Title IS NOT NULL"),
                ("Update_WithCte", "WITH O AS(SELECT o.UserId, MAX(o.Title) AS Title FROM Orders o GROUP BY o.UserId) UPDATE u SET u.Name = O.Title FROM Users u INNER JOIN O ON O.UserId = u.UserId"),
                ("Update_Addition", "UPDATE Users SET Version = Version + 1 WHERE UserId = 1"),
                ("Update_Case", "UPDATE Users SET Name = CASE WHEN IsActive = 1 THEN 'A' ELSE 'B' END WHERE UserId = 1"),
                ("Update_SubQuery", "UPDATE Users SET Name = (SELECT MAX(o.Title) FROM Orders o) WHERE UserId = 1"),
                ("Update_IsNull", "UPDATE Users SET Name = ISNULL(Name, 'A') WHERE UserId = 1"),
                ("Update_SetNull", "UPDATE Users SET Name = NULL WHERE UserId = 1"),
                ("Update_WithTop", "UPDATE TOP (5) Users SET Name = 'A' WHERE UserId > 0"),
                ("Update_WithPercentTop", "UPDATE TOP (5) PERCENT Users SET Name = 'A' WHERE UserId > 0"),
                ("Update_BoolWhere", "UPDATE Users SET IsActive = 0 WHERE IsActive = 1"),
                ("Update_DateAdd", "UPDATE Users SET ModifiedAt = DATEADD(day, 1, ModifiedAt) WHERE UserId = 1")
            };

            return cases.Select(i => Case(i.Name, i.Sql));
        }

        private static IEnumerable<TestCaseData> DeleteCases()
        {
            var cases = new (string Name, string Sql)[]
            {
                ("Delete_Simple_NoFrom", "DELETE Users"),
                ("Delete_Simple_NoFrom_Where", "DELETE Users WHERE UserId = 1"),
                ("Delete_SchemaQualified_NoFrom", "DELETE dbo.Users WHERE UserId = 1"),
                ("Delete_Bracketed_NoFrom", "DELETE [Users] WHERE [UserId] = 1"),
                ("Delete_WithTop", "DELETE TOP (5) Users WHERE UserId > 0"),
                ("Delete_WithTopPercent", "DELETE TOP (5) PERCENT Users WHERE UserId > 0"),
                ("Delete_FromSameTable", "DELETE FROM Users"),
                ("Delete_FromSameTable_Where", "DELETE FROM Users WHERE UserId = 1"),
                ("Delete_FromSameTable_Where_NoAliasRef", "DELETE FROM Users WHERE IsActive = 0"),
                ("Delete_Alias_From", "DELETE u FROM Users u WHERE u.UserId = 1"),
                ("Delete_Alias_FromNoWhere", "DELETE u FROM Users u"),
                ("Delete_Join", "DELETE u FROM Users u INNER JOIN Orders o ON o.UserId = u.UserId WHERE o.IsDeleted = 1"),
                ("Delete_LeftJoin", "DELETE u FROM Users u LEFT JOIN Orders o ON o.UserId = u.UserId WHERE o.OrderId IS NULL"),
                ("Delete_DerivedJoin", "DELETE u FROM Users u INNER JOIN (SELECT o.UserId FROM Orders o) d ON d.UserId = u.UserId"),
                ("Delete_WithAndPredicate", "DELETE Users WHERE UserId > 0 AND IsActive = 0"),
                ("Delete_WithOrPredicate", "DELETE Users WHERE UserId = 1 OR UserId = 2"),
                ("Delete_WithExists", "DELETE Users WHERE EXISTS(SELECT 1 FROM Orders o WHERE o.UserId = Users.UserId)"),
                ("Delete_WithInSubQuery", "DELETE Users WHERE UserId IN(SELECT o.UserId FROM Orders o)"),
                ("Delete_WithNotExists", "DELETE Users WHERE NOT EXISTS(SELECT 1 FROM Orders o WHERE o.UserId = Users.UserId)")
            };

            return cases.Select(i => Case(i.Name, i.Sql));
        }

        private static IEnumerable<TestCaseData> MergeCases()
        {
            var cases = new (string Name, string Sql)[]
            {
                ("Merge_UpdateOnly", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN MATCHED THEN UPDATE SET t.Name = s.Name;"),
                ("Merge_DeleteOnly", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN MATCHED THEN DELETE;"),
                ("Merge_InsertOnly", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN NOT MATCHED THEN INSERT(UserId, Name) VALUES(s.UserId, s.Name);"),
                ("Merge_UpdateInsert", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN MATCHED THEN UPDATE SET t.Name = s.Name WHEN NOT MATCHED THEN INSERT(UserId, Name) VALUES(s.UserId, s.Name);"),
                ("Merge_UpdateInsertDelete", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN MATCHED THEN UPDATE SET t.Name = s.Name WHEN NOT MATCHED THEN INSERT(UserId, Name) VALUES(s.UserId, s.Name) WHEN NOT MATCHED BY SOURCE THEN DELETE;"),
                ("Merge_UsingValues", "MERGE Users t USING (VALUES (1, 'A'), (2, 'B')) s(UserId, Name) ON t.UserId = s.UserId WHEN MATCHED THEN UPDATE SET t.Name = s.Name WHEN NOT MATCHED THEN INSERT(UserId, Name) VALUES(s.UserId, s.Name);"),
                ("Merge_MatchedAnd", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN MATCHED AND s.IsDeleted = 0 THEN UPDATE SET t.Name = s.Name;"),
                ("Merge_NotMatchedByTargetAnd", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN NOT MATCHED BY TARGET AND s.IsDeleted = 0 THEN INSERT(UserId, Name) VALUES(s.UserId, s.Name);"),
                ("Merge_NotMatchedBySourceAndDelete", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN NOT MATCHED BY SOURCE AND t.IsActive = 0 THEN DELETE;"),
                ("Merge_NotMatchedBySourceAndUpdate", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN NOT MATCHED BY SOURCE AND t.IsActive = 1 THEN UPDATE SET t.IsActive = 0;"),
                ("Merge_InsertDefaultValues", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN NOT MATCHED THEN INSERT DEFAULT VALUES;"),
                ("Merge_UsingSelect", "MERGE Users t USING (SELECT o.UserId, o.Title AS Name FROM Orders o) s ON t.UserId = s.UserId WHEN MATCHED THEN UPDATE SET t.Name = s.Name WHEN NOT MATCHED THEN INSERT(UserId, Name) VALUES(s.UserId, s.Name);")
            };

            return cases.Select(i => Case(i.Name, i.Sql));
        }

        private static IEnumerable<TestCaseData> MalformedSelectCases()
        {
            var cases = new List<(string Name, string Sql, string? Error)>();

            foreach (var prefix in new[]
                     {
                         ("SelectOnly", "SELECT"),
                         ("SelectDistinctOnly", "SELECT DISTINCT"),
                         ("SelectTopParenOnly", "SELECT TOP (1)"),
                         ("SelectTopOnly", "SELECT TOP 1"),
                     })
            {
                cases.Add((prefix.Item1, prefix.Item2, "SELECT list is missing"));
                cases.Add((prefix.Item1 + "_FromUsers", prefix.Item2 + " FROM Users", "SELECT list is missing"));
                cases.Add((prefix.Item1 + "_FromUsersAlias", prefix.Item2 + " FROM Users u", "SELECT list is missing"));
                cases.Add((prefix.Item1 + "_Where", prefix.Item2 + " WHERE 1=1", "SELECT list is missing"));
                cases.Add((prefix.Item1 + "_OrderBy", prefix.Item2 + " ORDER BY UserId", "SELECT list is missing"));
            }

            foreach (var projection in new[]
                     {
                         ("Wildcard", "*"),
                         ("Column", "UserId"),
                         ("QualifiedColumn", "u.UserId"),
                         ("Const", "1"),
                     })
            {
                cases.Add(("FromMissing_" + projection.Item1, "SELECT " + projection.Item2 + " FROM", "FROM clause is invalid"));
                cases.Add(("FromWhere_" + projection.Item1, "SELECT " + projection.Item2 + " FROM WHERE UserId = 1", "FROM clause is invalid"));
                cases.Add(("FromGroup_" + projection.Item1, "SELECT " + projection.Item2 + " FROM GROUP BY UserId", "FROM clause is invalid"));
                cases.Add(("FromOrder_" + projection.Item1, "SELECT " + projection.Item2 + " FROM ORDER BY UserId", "FROM clause is invalid"));
                cases.Add(("FromOffset_" + projection.Item1, "SELECT " + projection.Item2 + " FROM OFFSET 0 ROWS", "FROM clause is invalid"));
                cases.Add(("FromUnion_" + projection.Item1, "SELECT " + projection.Item2 + " FROM UNION SELECT 1", "FROM clause is invalid"));
            }

            foreach (var invalid in new[]
                     {
                         ("WildcardAlias", "SELECT * F", "incorrect syntax near"),
                         ("QualifiedWildcardAlias", "SELECT u.* F FROM Users u", "incorrect syntax near"),
                         ("WildcardAsAlias", "SELECT * AS F", "incorrect syntax near"),
                         ("QualifiedWildcardAsAlias", "SELECT u.* AS F FROM Users u", "incorrect syntax near"),
                         ("DanglingPlus", "SELECT 1+", "unexpected end of statement"),
                         ("DanglingMinus", "SELECT 1-", "unexpected end of statement"),
                         ("DanglingMultiply", "SELECT 1*", "unexpected end of statement"),
                         ("DanglingDivide", "SELECT 1/", "unexpected end of statement"),
                         ("UnbalancedParen", "SELECT (1+2", "unbalanced parentheses"),
                         ("WhereOnly", "SELECT UserId FROM Users WHERE", "unexpected end of statement"),
                         ("WhereEq", "SELECT UserId FROM Users WHERE UserId =", "unexpected end of statement"),
                         ("WhereAnd", "SELECT UserId FROM Users WHERE UserId = 1 AND", "unexpected end of statement"),
                         ("WhereOr", "SELECT UserId FROM Users WHERE UserId = 1 OR", "unexpected end of statement"),
                         ("WhereIn", "SELECT UserId FROM Users WHERE UserId IN(", "unbalanced parentheses"),
                         ("WhereExists", "SELECT UserId FROM Users WHERE EXISTS(", "unbalanced parentheses"),
                         ("WhereOpenParen", "SELECT UserId FROM Users WHERE (UserId = 1", "unbalanced parentheses"),
                         ("OrderByOnly", "SELECT UserId FROM Users ORDER BY", "ORDER BY clause is invalid"),
                         ("OrderByComma", "SELECT UserId FROM Users ORDER BY UserId,", "ORDER BY clause is invalid"),
                         ("OrderByDoubleComma", "SELECT UserId FROM Users ORDER BY UserId,, Name", "ORDER BY clause is invalid"),
                         ("GroupByOnly", "SELECT UserId FROM Users GROUP BY", "GROUP BY clause is invalid"),
                         ("GroupByComma", "SELECT UserId FROM Users GROUP BY UserId,", "GROUP BY clause is invalid"),
                         ("GroupByDoubleComma", "SELECT UserId FROM Users GROUP BY UserId,, Name", "GROUP BY clause is invalid"),
                         ("OffsetWithoutOrder", "SELECT UserId FROM Users OFFSET 10 ROWS", "OFFSET requires ORDER BY"),
                         ("OffsetMissingRows", "SELECT UserId FROM Users ORDER BY UserId OFFSET 10", "OFFSET/FETCH clause is invalid"),
                         ("OffsetOnlyKeyword", "SELECT UserId FROM Users ORDER BY UserId OFFSET", "OFFSET/FETCH clause is invalid"),
                         ("FetchWithoutOffset", "SELECT UserId FROM Users ORDER BY UserId FETCH NEXT 10 ROWS ONLY", "OFFSET/FETCH clause is invalid"),
                         ("FetchMissingCount", "SELECT UserId FROM Users ORDER BY UserId OFFSET 0 ROWS FETCH NEXT ROWS ONLY", "OFFSET/FETCH clause is invalid"),
                         ("FetchMissingOnly", "SELECT UserId FROM Users ORDER BY UserId OFFSET 0 ROWS FETCH NEXT 10 ROWS", "OFFSET/FETCH clause is invalid"),
                         ("FetchExtraToken", "SELECT UserId FROM Users ORDER BY UserId OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY EXTRA", "OFFSET/FETCH clause is invalid"),
                         ("CrossJoinWithOn", "SELECT UserId FROM Users CROSS JOIN Orders ON Users.Id = Orders.UserId", "CROSS/ APPLY join cannot contain ON"),
                         ("CrossApplyWithOn", "SELECT UserId FROM Users CROSS APPLY Orders ON Users.Id = Orders.UserId", "CROSS/ APPLY join cannot contain ON"),
                         ("OuterApplyWithOn", "SELECT UserId FROM Users OUTER APPLY Orders ON Users.Id = Orders.UserId", "CROSS/ APPLY join cannot contain ON"),
                         ("InnerJoinNoOn", "SELECT UserId FROM Users INNER JOIN Orders", "JOIN clause must contain ON"),
                         ("LeftJoinNoOn", "SELECT UserId FROM Users LEFT JOIN Orders", "JOIN clause must contain ON"),
                         ("RightJoinNoOn", "SELECT UserId FROM Users RIGHT JOIN Orders", "JOIN clause must contain ON"),
                         ("FullJoinNoOn", "SELECT UserId FROM Users FULL JOIN Orders", "JOIN clause must contain ON"),
                         ("InnerJoinOnNoPredicate", "SELECT UserId FROM Users INNER JOIN Orders ON", null),
                         ("SelectFromComma", "SELECT UserId FROM Users,", null),
                         ("SelectDoubleFrom", "SELECT UserId FROM FROM Users", "FROM clause is invalid"),
                         ("SelectBadSetUnion", "SELECT UserId FROM Users UNION", null),
                         ("SelectBadSetIntersect", "SELECT UserId FROM Users INTERSECT", null),
                         ("SelectBadSetExcept", "SELECT UserId FROM Users EXCEPT", null),
                     })
            {
                cases.Add(invalid);
            }

            foreach (var joinType in new[] { "INNER", "LEFT", "RIGHT", "FULL" })
            {
                foreach (var left in new[] { "Users", "Users u" })
                {
                    foreach (var right in new[] { "Orders", "Orders o", "(SELECT 1 AS Id) d" })
                    {
                        cases.Add((
                            "JoinNoPredicate_" + joinType + "_" + left.Replace(" ", "_") + "_" + right.Replace(" ", "_").Replace("(", "").Replace(")", ""),
                            $"SELECT UserId FROM {left} {joinType} JOIN {right} ON",
                            null));
                    }
                }
            }

            foreach (var clause in new[]
                     {
                         ("WhereUnary", " WHERE NOT"),
                         ("WhereEqOpen", " WHERE UserId = ("),
                         ("WhereBetweenMissingEnd", " WHERE UserId BETWEEN 1 AND"),
                         ("WhereLikeMissingPattern", " WHERE Name LIKE"),
                         ("WhereInEmpty", " WHERE UserId IN()"),
                         ("WhereExistsEmpty", " WHERE EXISTS()"),
                     })
            {
                cases.Add(("SelectClause_" + clause.Item1, "SELECT UserId FROM Users" + clause.Item2, null));
                cases.Add(("SelectClauseAlias_" + clause.Item1, "SELECT u.UserId FROM Users u" + clause.Item2.Replace("UserId", "u.UserId").Replace("Name", "u.Name"), null));
            }

            return cases.Select(i => BadCase(i.Name, i.Sql, i.Error));
        }

        private static IEnumerable<TestCaseData> MalformedUpdateCases()
        {
            var cases = new List<(string Name, string Sql, string? Error)>();

            foreach (var target in new[] { "Users", "dbo.Users", "[Users]", "u" })
            {
                cases.Add(("UpdateMissingSet_" + target.Replace(".", "_").Replace("[", "").Replace("]", ""), $"UPDATE {target}", "SET clause"));
                cases.Add(("UpdateMissingSetWhere_" + target.Replace(".", "_").Replace("[", "").Replace("]", ""), $"UPDATE {target} WHERE UserId = 1", "SET clause"));
            }

            foreach (var invalid in new[]
                     {
                         ("UpdateSetOnly", "UPDATE Users SET", null),
                         ("UpdateSetWhere", "UPDATE Users SET WHERE UserId = 1", null),
                         ("UpdateNameEq", "UPDATE Users SET Name =", "unexpected end of statement"),
                         ("UpdateNameEqComma", "UPDATE Users SET Name = , IsActive = 1", null),
                         ("UpdateCommaOnly", "UPDATE Users SET Name = 'A',", null),
                         ("UpdateDoubleComma", "UPDATE Users SET Name = 'A',, IsActive = 1", null),
                         ("UpdateFromMissing", "UPDATE Users SET Name = 'A' FROM", null),
                         ("UpdateFromWhere", "UPDATE Users SET Name = 'A' FROM WHERE UserId = 1", null),
                         ("UpdateJoinNoOn", "UPDATE u SET u.Name = o.Title FROM Users u INNER JOIN Orders o", "JOIN clause must contain ON"),
                         ("UpdateJoinOnOnly", "UPDATE u SET u.Name = o.Title FROM Users u INNER JOIN Orders o ON", null),
                         ("UpdateCrossJoinOn", "UPDATE u SET u.Name = o.Title FROM Users u CROSS JOIN Orders o ON u.UserId = o.UserId", "CROSS/ APPLY join cannot contain ON"),
                         ("UpdateCrossApplyOn", "UPDATE u SET u.Name = oa.Title FROM Users u CROSS APPLY Orders oa ON u.UserId = oa.UserId", "CROSS/ APPLY join cannot contain ON"),
                         ("UpdateWhereOnly", "UPDATE Users SET Name = 'A' WHERE", "unexpected end of statement"),
                         ("UpdateWhereEq", "UPDATE Users SET Name = 'A' WHERE UserId =", "unexpected end of statement"),
                         ("UpdateWhereAnd", "UPDATE Users SET Name = 'A' WHERE UserId = 1 AND", "unexpected end of statement"),
                         ("UpdateWhereOr", "UPDATE Users SET Name = 'A' WHERE UserId = 1 OR", "unexpected end of statement"),
                         ("UpdateSetOpenParen", "UPDATE Users SET Name = (SELECT 1", "unbalanced parentheses"),
                         ("UpdateTopOnly", "UPDATE TOP (5) Users", "SET clause"),
                         ("UpdateTopPercentOnly", "UPDATE TOP (5) PERCENT Users", "SET clause"),
                         ("UpdateSetFromComma", "UPDATE Users SET Name = 'A' FROM Users u,", null),
                         ("UpdateSetBadFromSource", "UPDATE Users SET Name = 'A' FROM GROUP BY UserId", null),
                         ("UpdateSetOutputInto", "UPDATE Users SET Name = 'A' OUTPUT INSERTED.Name INTO @T WHERE UserId = 1", "OUTPUT ... INTO"),
                     })
            {
                cases.Add(invalid);
            }

            foreach (var assignment in new[]
                     {
                         "Name =",
                         "Name = 'A',",
                         "Name = 'A',, IsActive = 1",
                         "Name = CASE WHEN IsActive = 1 THEN 'A'",
                         "ModifiedAt = DATEADD(day, 1,",
                         "Version = Version +",
                     })
            {
                foreach (var where in new[] { "", " WHERE UserId = 1" })
                {
                    cases.Add((
                        "UpdateAssignment_" + assignment.GetHashCode().ToString("X") + "_" + (where.Length == 0 ? "NoWhere" : "Where"),
                        "UPDATE Users SET " + assignment + where,
                        null));
                }
            }

            return cases.Select(i => BadCase(i.Name, i.Sql, i.Error));
        }

        private static IEnumerable<TestCaseData> MalformedDeleteCases()
        {
            var cases = new List<(string Name, string Sql, string? Error)>
            {
                ("DeleteOnly", "DELETE", null),
                ("DeleteFromOnly", "DELETE FROM", null),
                ("DeleteTopOnly", "DELETE TOP (5)", null),
                ("DeleteTopPercentOnly", "DELETE TOP (5) PERCENT", "DELETE statement must contain target"),
                ("DeleteWhereOnly", "DELETE Users WHERE", "unexpected end of statement"),
                ("DeleteWhereEq", "DELETE Users WHERE UserId =", "unexpected end of statement"),
                ("DeleteWhereAnd", "DELETE Users WHERE UserId = 1 AND", "unexpected end of statement"),
                ("DeleteWhereOr", "DELETE Users WHERE UserId = 1 OR", "unexpected end of statement"),
                ("DeleteFromWhereOnly", "DELETE FROM Users WHERE", "unexpected end of statement"),
                ("DeleteAliasFromMissing", "DELETE u FROM", null),
                ("DeleteJoinNoOn", "DELETE u FROM Users u INNER JOIN Orders o", "JOIN clause must contain ON"),
                ("DeleteJoinOnOnly", "DELETE u FROM Users u INNER JOIN Orders o ON", null),
                ("DeleteCrossJoinOn", "DELETE u FROM Users u CROSS JOIN Orders o ON u.UserId = o.UserId", "CROSS/ APPLY join cannot contain ON"),
                ("DeleteCrossApplyOn", "DELETE u FROM Users u CROSS APPLY Orders oa ON u.UserId = oa.UserId", "CROSS/ APPLY join cannot contain ON"),
                ("DeleteBadFromSource", "DELETE u FROM GROUP BY UserId", null),
                ("DeleteFromComma", "DELETE FROM Users,", null),
                ("DeleteAliasComma", "DELETE u FROM Users u,", null),
                ("DeleteExistsEmpty", "DELETE Users WHERE EXISTS()", null),
                ("DeleteInEmpty", "DELETE Users WHERE UserId IN()", null),
                ("DeleteOpenParen", "DELETE Users WHERE (UserId = 1", "unbalanced parentheses"),
            };

            foreach (var target in new[] { "Users", "dbo.Users", "[Users]", "u" })
            {
                cases.Add(("DeleteTargetWhereOnly_" + target.Replace(".", "_").Replace("[", "").Replace("]", ""), "DELETE " + target + " WHERE", "unexpected end of statement"));
            }

            foreach (var clause in new[]
                     {
                         "WHERE UserId BETWEEN 1 AND",
                         "WHERE Name LIKE",
                         "WHERE NOT",
                         "WHERE UserId = (",
                     })
            {
                cases.Add(("DeleteClause_" + clause.GetHashCode().ToString("X"), "DELETE Users " + clause, null));
                cases.Add(("DeleteFromClause_" + clause.GetHashCode().ToString("X"), "DELETE FROM Users " + clause, null));
            }

            return cases.Select(i => BadCase(i.Name, i.Sql, i.Error));
        }

        private static IEnumerable<TestCaseData> MalformedMergeCases()
        {
            var cases = new List<(string Name, string Sql, string? Error)>
            {
                ("MergeOnly", "MERGE Users", "MERGE statement must contain ON clause"),
                ("MergeNoUsing", "MERGE Users t ON t.UserId = 1 WHEN MATCHED THEN DELETE;", null),
                ("MergeNoOn", "MERGE Users t USING UsersStaging s WHEN MATCHED THEN DELETE;", "MERGE statement must contain ON clause"),
                ("MergeUsingOnly", "MERGE Users t USING UsersStaging s", "MERGE statement must contain ON clause"),
                ("MergeOnOnly", "MERGE Users t USING UsersStaging s ON", null),
                ("MergeMatchedNoAction", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN MATCHED THEN;", null),
                ("MergeNotMatchedNoAction", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN NOT MATCHED THEN;", null),
                ("MergeNotMatchedBySourceNoAction", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN NOT MATCHED BY SOURCE THEN;", null),
                ("MergeInsertMissingValues", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN NOT MATCHED THEN INSERT(UserId, Name);", null),
                ("MergeInsertOpenValues", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN NOT MATCHED THEN INSERT(UserId, Name) VALUES(", "unbalanced parentheses"),
                ("MergeUpdateMissingSet", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN MATCHED THEN UPDATE;", null),
                ("MergeUpdateEmptySet", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN MATCHED THEN UPDATE SET;", null),
                ("MergeDeleteGarbage", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN MATCHED THEN DELETE EXTRA;", null),
                ("MergeUsingMissingSource", "MERGE Users t USING ON t.UserId = 1 WHEN MATCHED THEN DELETE;", null),
                ("MergeUsingBadSource", "MERGE Users t USING WHERE t.UserId = 1 ON t.UserId = 1 WHEN MATCHED THEN DELETE;", null),
                ("MergeUsingJoinNoOn", "MERGE Users t USING UsersStaging s INNER JOIN Orders o ON t.UserId = s.UserId WHEN MATCHED THEN DELETE;", null),
                ("MergeConditionAndOnly", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN MATCHED AND THEN DELETE;", null),
                ("MergeInsertDefaultValuesGarbage", "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId WHEN NOT MATCHED THEN INSERT DEFAULT VALUES EXTRA;", null),
                ("MergeUnbalancedUsingSelect", "MERGE Users t USING (SELECT s.UserId FROM UsersStaging s ON t.UserId = s.UserId WHEN MATCHED THEN DELETE;", "unbalanced parentheses"),
            };

            foreach (var action in new[]
                     {
                         "WHEN MATCHED THEN UPDATE SET Name =",
                         "WHEN MATCHED THEN UPDATE SET Name = s.Name,",
                         "WHEN MATCHED THEN UPDATE SET Name = CASE WHEN s.IsDeleted = 1 THEN 'X'",
                         "WHEN NOT MATCHED THEN INSERT(UserId, Name) VALUES(s.UserId,",
                         "WHEN NOT MATCHED THEN INSERT(UserId, Name) VALUES(",
                         "WHEN NOT MATCHED BY SOURCE THEN UPDATE SET IsActive =",
                     })
            {
                cases.Add((
                    "MergeAction_" + action.GetHashCode().ToString("X"),
                    "MERGE Users t USING UsersStaging s ON t.UserId = s.UserId " + action + ";",
                    null));
            }

            foreach (var usingSource in new[]
                     {
                         "UsersStaging",
                         "UsersStaging s",
                         "(SELECT UserId FROM UsersStaging)",
                         "(VALUES (1)) s(UserId)",
                     })
            {
                cases.Add((
                    "MergeMissingAction_" + usingSource.GetHashCode().ToString("X"),
                    "MERGE Users t USING " + usingSource + " ON t.UserId = s.UserId;",
                    null));
                cases.Add((
                    "MergeBadSemicolon_" + usingSource.GetHashCode().ToString("X"),
                    "MERGE Users t USING " + usingSource + " ON t.UserId = s.UserId WHEN MATCHED THEN DELETE",
                    null));
            }

            return cases.Select(i => BadCase(i.Name, i.Sql, i.Error));
        }

        private static TestCaseData Case(string name, string sql)
            => new TestCaseData(name, sql).SetName(name);

        private static TestCaseData BadCase(string name, string sql, string? expectedErrorFragment)
            => new TestCaseData(name, sql, expectedErrorFragment).SetName(name);
    }
}
