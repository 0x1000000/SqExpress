using System.Collections.Generic;
using NUnit.Framework;
using SqExpress.SqlExport;

namespace SqExpress.TSqlParser.Test
{
    public class SqExpressTSqlParserSamplesTest
    {
        [TestCaseSource(nameof(RoundTripCases))]
        public void RoundTripSamples(string sql)
        {
            AssertRoundTrip(sql);
        }

        [TestCaseSource(nameof(EdgeRoundTripCases))]
        public void RoundTripEdgeCases(string sql)
        {
            AssertRoundTrip(sql);
        }

        private static void AssertRoundTrip(string sql)
        {
            var parser = new SqExpressTSqlParser();
            if (parser.TryParseScript(sql, out var expr, out var error))
            {
                var actual = expr.ToSql(TSqlExporter.Default);
                Assert.That(actual, Is.EqualTo(sql));
            }
            else
            {
                Assert.Fail(error);
            }
        }

        private static TestCaseData RoundTrip(string name, string sql)
            => new TestCaseData(sql).SetName(name);

        private static IEnumerable<TestCaseData> RoundTripCases()
        {
            yield return RoundTrip("Delete_Output_NotLike", @"DELETE [u] OUTPUT DELETED.[UserId] FROM [dbo].[Users] [u] WHERE NOT [u].[Name] LIKE 'A%'");
            yield return RoundTrip("Delete_WithDerivedSourceJoin", @"DELETE [u] FROM [dbo].[Users] [u] JOIN (SELECT [o].[UserId] FROM [dbo].[Orders] [o])[d] ON [d].[UserId]=[u].[UserId]");
            yield return RoundTrip("Delete_WithJoinAndPredicate", @"DELETE [u] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] WHERE [o].[IsDeleted]=1");
            yield return RoundTrip("Insert_Output_WithColumns", @"INSERT INTO [dbo].[Users]([UserId],[Name]) OUTPUT INSERTED.[UserId] VALUES (1,'A')");
            yield return RoundTrip("Insert_Output_WithoutColumns", @"INSERT INTO [dbo].[Users] OUTPUT INSERTED.[UserId] VALUES (1,'A')");
            yield return RoundTrip("Insert_WithoutTargetColumns", @"INSERT INTO [dbo].[Users] VALUES (1,'A'),(2,'B')");
            yield return RoundTrip("Insert_SelectSource", @"INSERT INTO [dbo].[Users]([UserId],[Name]) SELECT [o].[UserId],[o].[Title] FROM [dbo].[Orders] [o] WHERE [o].[IsDeleted]=0");
            yield return RoundTrip("Insert_ValuesMultiRow", @"INSERT INTO [dbo].[Users]([UserId],[Name]) VALUES (1,'A'),(2,'B')");
            yield return RoundTrip("Merge_Output_WithActionInsertedDeletedSource", @"MERGE [dbo].[Users] [A0] USING (VALUES (1,'Alice'),(2,'Bob'))[A1]([UserId],[FirstName]) ON [A0].[UserId]=[A1].[UserId] WHEN MATCHED THEN  DELETE WHEN NOT MATCHED BY SOURCE THEN  DELETE OUTPUT [A1].[UserId],INSERTED.[LastName] [LN],DELETED.[UserId],$ACTION [Act];");
            yield return RoundTrip("Merge_UpdateInsertDelete", @"MERGE [dbo].[Users] [A0] USING [dbo].[UsersStaging] [s] ON [A0].[UserId]=[s].[UserId] WHEN MATCHED THEN UPDATE SET [A0].[Name]=[s].[Name],[A0].[IsActive]=[s].[IsActive] WHEN NOT MATCHED THEN INSERT([UserId],[Name],[IsActive]) VALUES([s].[UserId],[s].[Name],[s].[IsActive]) WHEN NOT MATCHED BY SOURCE THEN  DELETE;");
            yield return RoundTrip("Select_SchemaQualifiedScalarFunction", @"SELECT [dbo].[NormalizeUserName]([u].[Name]) [NameLength] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_SchemaFunctionTableReference", @"SELECT [f].[Value] FROM [dbo].[MyFunc](1) [f]");
            yield return RoundTrip("Select_BuiltinFunctionTableReference", @"SELECT [s].[value] FROM STRING_SPLIT('a,b',',') [s]");
            yield return RoundTrip("Select_DerivedTable_TwoLevels", @"SELECT [sq].[A] FROM (SELECT [i].[Id] [A] FROM (SELECT 1 [Id])[i])[sq]");
            yield return RoundTrip("Select_DerivedTable_WithOffsetFetch", @"SELECT [sq].[A] FROM (SELECT 1 [A] ORDER BY [A] OFFSET 0 ROW FETCH NEXT 1 ROW ONLY)[sq]");
            yield return RoundTrip("Select_DerivedTable_WithUnionAll", @"SELECT [sq].[A] FROM (SELECT 1 [A] UNION ALL SELECT 2 [A])[sq]");
            yield return RoundTrip("Select_DerivedTable_Simple", @"SELECT [sq].[A] FROM (SELECT 1 [A])[sq]");
            yield return RoundTrip("Select_DerivedTable_WithRenamedColumn", @"SELECT [sq].[PatronymicName],[sq].[FirstName] FROM (SELECT [u].[LastName] [PatronymicName],[u].[FirstName] FROM [dbo].[Users] [u])[sq]");
            yield return RoundTrip("Select_DerivedTable_WithWhere", @"SELECT [sq].[UserId] FROM (SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name]='A')[sq]");
            yield return RoundTrip("Select_WildcardWithJoinedColumn", @"SELECT [u].*,[o].[OrderId] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId]");
            yield return RoundTrip("Select_IsNullPredicate", @"SELECT [u].[CreatedAt],[u].[UpdatedUtc],[u].[IsDeleted],[u].[Description] FROM [dbo].[Users] [u] WHERE [u].[Description] IS NULL");
            yield return RoundTrip("Select_StringEqualityPredicate", @"SELECT [u].[Name] FROM [dbo].[Users] [u] WHERE [u].[Name]='A'");
            yield return RoundTrip("Select_GroupByWithCount", @"SELECT [u].[Status],COUNT(1) [Total] FROM [dbo].[Users] [u] GROUP BY [u].[Status]");
            yield return RoundTrip("Select_BasicProjection", @"SELECT [u].[UserId] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_ComplexWhere_InLikeRange", @"SELECT [u].[UserId] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] WHERE [u].[Status] IN(1,2,3) AND [o].[Title] LIKE 'A%' AND [u].[Score]>=10 AND [u].[Score]<=20");
            yield return RoundTrip("Select_OrderOffsetFetch", @"SELECT [u].[UserId] FROM [dbo].[Users] [u] ORDER BY [u].[UserId] OFFSET 10 ROW FETCH NEXT 5 ROW ONLY");
            yield return RoundTrip("Select_OrderOffsetOnly", @"SELECT [u].[UserId] FROM [dbo].[Users] [u] ORDER BY [u].[UserId] OFFSET 20 ROW");
            yield return RoundTrip("Select_InWithEmptyString", @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name] IN('')");
            yield return RoundTrip("Select_EqualityWithEmptyString", @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name]=''");
            yield return RoundTrip("Select_WhereOrder", @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name]='A' ORDER BY [u].[UserId]");
            yield return RoundTrip("Select_InSubquery", @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[UserId] IN(SELECT [o].[UserId] FROM [dbo].[Orders] [o])");
            yield return RoundTrip("Select_ConstantBooleanAnd", @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[UserId]=0 AND 0>0");
            yield return RoundTrip("Select_ExistsSubquery", @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE EXISTS(SELECT 1 FROM [dbo].[Orders] [o] WHERE [o].[UserId]=[u].[UserId])");
            yield return RoundTrip("Select_UnqualifiedTableName", @"SELECT [u].[UserId] FROM [Users] [u]");
            yield return RoundTrip("Select_ScalarSubqueryInProjection", @"SELECT [u].[UserId],(SELECT COUNT(1) FROM [dbo].[Orders] [o] WHERE [o].[UserId]=[u].[UserId]) [OrderCount] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_CrossApply", @"SELECT [u].[UserId],[oa].[OrderId] FROM [dbo].[Users] [u] CROSS APPLY (SELECT [o].[OrderId] FROM [dbo].[Orders] [o] ORDER BY [o].[OrderId] DESC OFFSET 0 ROW FETCH NEXT 1 ROW ONLY)[oa]");
            yield return RoundTrip("Select_OuterApply", @"SELECT [u].[UserId],[oa].[OrderId] FROM [dbo].[Users] [u] OUTER APPLY (SELECT [o].[OrderId] FROM [dbo].[Orders] [o] ORDER BY [o].[OrderId] DESC OFFSET 0 ROW FETCH NEXT 1 ROW ONLY)[oa]");
            yield return RoundTrip("Select_OrderByDescending", @"SELECT [u].[UserId],[u].[Name] [UserName] FROM [dbo].[Users] [u] WHERE [u].[IsActive]=1 ORDER BY [u].[Name] DESC");
            yield return RoundTrip("Select_UnionAllWithMixedLiteralTypes", @"SELECT [u1].[UserId] FROM [dbo].[Users] [u1] WHERE [u1].[UserId]=1 UNION ALL SELECT [u2].[UserId] FROM [dbo].[Users] [u2] WHERE [u2].[UserId]='A'");
            yield return RoundTrip("Select_Except", @"SELECT 1 [A] EXCEPT SELECT 2 [A]");
            yield return RoundTrip("Select_Intersect", @"SELECT 1 [A] INTERSECT SELECT 2 [A]");
            yield return RoundTrip("Select_UnionAll", @"SELECT 1 [A] UNION ALL SELECT 2 [A]");
            yield return RoundTrip("Select_UnionAllWithTopLevelOrderOffsetFetch", @"SELECT 1 [A] UNION ALL SELECT 2 [A] ORDER BY [A] OFFSET 1 ROW FETCH NEXT 1 ROW ONLY");
            yield return RoundTrip("Select_UnionDistinct", @"SELECT 1 [A] UNION SELECT 2 [A]");
            yield return RoundTrip("Select_KnownFunctions", @"SELECT ABS([u].[V1]) [Total],LEN([u].[V2]) [NameLen],DATEADD(d,1,[u].[V3]) [NextAt] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_WindowAvgWithFrame", @"SELECT AVG([u].[Score])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) [AvgScore] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_CaseWhenElse", @"SELECT CASE WHEN [u].[IsActive]=1 THEN 'Y' ELSE 'N' END [ActiveMark] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_CaseWhenMultipleBranches", @"SELECT CASE WHEN [u].[Status]=1 THEN 'A' WHEN [u].[Status]=2 THEN 'B' ELSE 'C' END [StatusName] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_CoalesceDisplayName", @"SELECT COALESCE([u].[Name],[u].[Login],'NA') [DisplayName] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_CoalesceDisplayName_DuplicateCoverage", @"SELECT COALESCE([u].[Name],[u].[Login],'NA') [DisplayName] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_CountFromTable", @"SELECT COUNT(1) [Total] FROM [dbo].[Users]");
            yield return RoundTrip("Select_WindowCount", @"SELECT COUNT([u].[Score])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) [CntScore] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_CountFromAliasedTable", @"SELECT COUNT(1) [Total] FROM [dbo].[Users] [A0]");
            yield return RoundTrip("Select_DateAddAlias", @"SELECT DATEADD(d,1,[u].[CreatedAt]) [NextDate] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_DateDiff", @"SELECT DATEDIFF(DAY,[u].[CreatedAt],[u].[UpdatedAt]) [Days] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_DistinctTop", @"SELECT DISTINCT TOP 10 [u].[UserId] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_WindowFirstValueAndLastValue", @"SELECT FIRST_VALUE([u].[FirstName])OVER(ORDER BY [u].[UserId]) [FirstNameFirst],LAST_VALUE([u].[FirstName])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) [FirstNameLast] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_GetDate", @"SELECT GETDATE() [Now]");
            yield return RoundTrip("Select_GetUtcDate", @"SELECT GETUTCDATE() [NowUtc]");
            yield return RoundTrip("Select_IsNullFunction", @"SELECT ISNULL([u].[Name],'NA') [Name2] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_LenFunction", @"SELECT LEN([u].[Name]) [NameLength] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_WindowMax", @"SELECT MAX([u].[Score])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) [MaxScore] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_WindowMin", @"SELECT MIN([u].[Score])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) [MinScore] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_WindowRowNumber", @"SELECT ROW_NUMBER()OVER(ORDER BY [u].[UserId]) [Rn] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_WindowSum", @"SELECT SUM([u].[Score])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) [RunningScore] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Select_WindowSumPartitionBy", @"SELECT SUM([u].[Score])OVER(PARTITION BY [u].[GroupId] ORDER BY [u].[UserId]) [RunningScore] FROM [dbo].[Users] [u]");
            yield return RoundTrip("Update_WithCrossApplyDerivedAndNotLike", @"UPDATE [u] SET [u].[Name]=[x].[Name] FROM [dbo].[Users] [u] CROSS APPLY (SELECT [u].[Name] [Name])[x] WHERE NOT [u].[Name] LIKE 'A%'");
            yield return RoundTrip("Update_WithCrossApplyTableFunction", @"UPDATE [u] SET [u].[Name]=[s].[value] FROM [dbo].[Users] [u] CROSS APPLY STRING_SPLIT('a,b',',') [s] WHERE [u].[UserId]=1");
            yield return RoundTrip("Update_WithJoinAndLike", @"UPDATE [u] SET [u].[Name]=[o].[Title],[u].[IsActive]=1 FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] WHERE [o].[Title] LIKE 'A%'");
        }

        private static IEnumerable<TestCaseData> EdgeRoundTripCases()
        {
            yield return RoundTrip("Edge_Select_NestedBooleanParenthesesAndNot", @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE ([u].[IsActive]=1 OR [u].[IsDeleted]=0)AND NOT [u].[Name]='X'");
            yield return RoundTrip("Edge_Select_DeepNestedFromSubqueries", @"SELECT [A].[V] FROM (SELECT [B].[V] FROM (SELECT [C].[V] FROM (SELECT 1 [V])[C])[B])[A]");
            yield return RoundTrip("Edge_Select_FromValuesConstructor", @"SELECT [v].[Id],[v].[Val] FROM (VALUES (1,'a'),(2,'b'))[v]([Id],[Val])");
            yield return RoundTrip("Edge_Select_NestedValuesDerivedTable", @"SELECT [A].[Id] FROM (SELECT [v].[Id] FROM (VALUES (1,'a'))[v]([Id],[Val]))[A]");
            yield return RoundTrip("Edge_Select_InListWithNullLiteral", @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Status] IN(1,NULL,3)");
            yield return RoundTrip("Edge_Select_IsNotNull", @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name] IS NOT NULL");
            yield return RoundTrip("Edge_Select_SelfJoinDistinctAliases", @"SELECT [u1].[UserId],[u2].[UserId] [OtherUserId] FROM [dbo].[Users] [u1] JOIN [dbo].[Users] [u2] ON [u2].[ManagerId]=[u1].[UserId]");
            yield return RoundTrip("Edge_Select_ExistsWithNestedDerivedTable", @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE EXISTS(SELECT 1 FROM (SELECT [o].[UserId] FROM [dbo].[Orders] [o] WHERE [o].[Amount]>0)[x] WHERE [x].[UserId]=[u].[UserId])");
            yield return RoundTrip("Edge_Select_OrderOffsetFetchSingleRow", @"SELECT [u].[UserId],[u].[Name] FROM [dbo].[Users] [u] ORDER BY [u].[UserId] OFFSET 0 ROW FETCH NEXT 1 ROW ONLY");
        }
    }
}
