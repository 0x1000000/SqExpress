using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SqExpress;
using SqExpress.SqlExport;
using SqExpress.SqlParser;
using SqExpress.Syntax;
using SqExpress.Syntax.Value;

namespace SqExpress.Test.SqlParser
{
    public class TSqlParserSamplesTest
    {
        [TestCaseSource(nameof(RoundTripCases))]
        public void RoundTripSamples(string name, string sql, string? expectedUnsupportedReason)
        {

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);
            var expectedParseFailure = !string.IsNullOrWhiteSpace(expectedUnsupportedReason)
                                       && !expectedUnsupportedReason!.StartsWith("EXPORT: ", StringComparison.Ordinal);

            if (expectedParseFailure)
            {
                Assert.That(ok, Is.False, $"Sample '{name}' should fail parsing.");
                Assert.That(error, Is.EqualTo(expectedUnsupportedReason), $"Sample '{name}' parse failure reason mismatch.");
                return;
            }

            Assert.That(ok, Is.True, $"Sample '{name}' should parse, but parser returned: {error}");
            Assert.That(expr, Is.Not.Null, $"Sample '{name}' should produce non-null expression.");
            AssertNoUnsafeValue(expr!, name);
        }

        [TestCaseSource(nameof(StructuredMappingCases))]
        public void StructuredMappingSamples(string feature, string sql)
        {

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, $"Feature '{feature}' should be mapped to structured SqExpress AST, but parser returned: {error}");
            Assert.That(expr, Is.Not.Null, $"Feature '{feature}' should produce non-null expression.");
            AssertNoUnsafeValue(expr!, feature);
        }

        [TestCaseSource(nameof(PgSqlCases))]
        public void ExportToPgSqlSamples(string name, string sql, string? expectedPgSql, string? expectedUnsupportedReason)
        {
            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var tables, out var error);

            if (!string.IsNullOrWhiteSpace(expectedUnsupportedReason))
            {
                if (!ok)
                {
                    Assert.That(error, Is.EqualTo(expectedUnsupportedReason), $"Sample '{name}' expected parser failure reason mismatch.");
                    return;
                }

                var ex = Assert.Throws<SqExpressException>(() => PgSqlExporter.Default.ToSql(expr!));
                var expectedExportReason = expectedUnsupportedReason!.StartsWith("EXPORT: ", StringComparison.Ordinal)
                    ? expectedUnsupportedReason.Substring("EXPORT: ".Length)
                    : expectedUnsupportedReason;
                Assert.That(ex!.Message, Is.EqualTo(expectedExportReason), $"Sample '{name}' expected exporter failure reason mismatch.");
                AssertNoUnsafeValue(expr!, name);
                return;
            }

            Assert.That(ok, Is.True, $"Sample '{name}' should parse for PgSql export, but parser returned: {error}");
            Assert.That(expr, Is.Not.Null, $"Sample '{name}' should produce non-null expression.");
            AssertNoUnsafeValue(expr!, name);

            var actualPgSql = PgSqlExporter.Default.ToSql(expr!.RebindParsedTables(tables!));
            Assert.That(actualPgSql, Is.EqualTo(expectedPgSql), $"Sample '{name}' PgSql mismatch.");
        }

        private static void AssertNoUnsafeValue(IExpr expr, string sampleName)
        {
            var unsafeValues = expr.SyntaxTree().DescendantsAndSelf().OfType<ExprUnsafeValue>().ToList();
            Assert.That(
                unsafeValues.Count,
                Is.EqualTo(0),
                $"Sample '{sampleName}' should not contain {nameof(ExprUnsafeValue)} nodes.");
        }

        private static IEnumerable<TestCaseData> RoundTripCases()
            => AllSamples
                .Where(i => !i.Name.StartsWith("Structured_", StringComparison.Ordinal))
                .Select(i => new TestCaseData(i.Name, i.Sql, i.UnsupportedReason).SetName(i.Name));

        private static IEnumerable<TestCaseData> StructuredMappingCases()
            => AllSamples
                .Where(i => i.Name.StartsWith("Structured_", StringComparison.Ordinal))
                .Select(i =>
                {
                    var feature = i.Name.Substring("Structured_".Length);
                    return new TestCaseData(feature, i.Sql).SetName("Structured_" + feature);
                });

        private static IEnumerable<TestCaseData> PgSqlCases()
            => AllSamples.Select(i => new TestCaseData(i.Name, i.Sql, i.PgSql, i.UnsupportedReason).SetName("Pg_" + i.Name));

        private static readonly IReadOnlyList<PgSample> AllSamples = new[]
        {
            new PgSample(
                "Delete_Output_NotLike",
                @"DELETE [u] OUTPUT DELETED.[UserId] FROM [dbo].[Users] [u] WHERE NOT [u].[Name] LIKE 'A%'",
                @"DELETE FROM ""dbo"".""Users"" ""u"" WHERE NOT ""u"".""Name"" LIKE 'A%' RETURNING ""UserId""",
                null
            ),
            new PgSample(
                "Delete_WithDerivedSourceJoin",
                @"DELETE [u] FROM [dbo].[Users] [u] JOIN (SELECT [o].[UserId] FROM [dbo].[Orders] [o])[d] ON [d].[UserId]=[u].[UserId]",
                @"DELETE FROM ""dbo"".""Users"" ""u"" USING (SELECT ""o"".""UserId"" FROM ""dbo"".""Orders"" ""o"")""d"" WHERE ""d"".""UserId""=""u"".""UserId""",
                null
            ),
            new PgSample(
                "Delete_WithJoinAndPredicate",
                @"DELETE [u] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] WHERE [o].[IsDeleted]=1",
                @"DELETE FROM ""dbo"".""Users"" ""u"" USING ""dbo"".""Orders"" ""o"" WHERE ""o"".""UserId""=""u"".""UserId"" AND ""o"".""IsDeleted""=1",
                null
            ),
            new PgSample(
                "Insert_Output_WithColumns",
                @"INSERT INTO [dbo].[Users]([UserId],[Name]) OUTPUT INSERTED.[UserId] VALUES (1,'A')",
                @"INSERT INTO ""dbo"".""Users""(""UserId"",""Name"") VALUES (1,'A')   RETURNING ""UserId""",
                null
            ),
            new PgSample(
                "Insert_Output_WithoutColumns",
                @"INSERT INTO [dbo].[Users] OUTPUT INSERTED.[UserId] VALUES (1,'A')",
                @"INSERT INTO ""dbo"".""Users"" VALUES (1,'A')   RETURNING ""UserId""",
                null
            ),
            new PgSample(
                "Insert_WithoutTargetColumns",
                @"INSERT INTO [dbo].[Users] VALUES (1,'A'),(2,'B')",
                @"INSERT INTO ""dbo"".""Users"" VALUES (1,'A'),(2,'B')",
                null
            ),
            new PgSample(
                "Insert_SelectSource",
                @"INSERT INTO [dbo].[Users]([UserId],[Name]) SELECT [o].[UserId],[o].[Title] FROM [dbo].[Orders] [o] WHERE [o].[IsDeleted]=0",
                @"INSERT INTO ""dbo"".""Users""(""UserId"",""Name"") SELECT ""o"".""UserId"",""o"".""Title"" FROM ""dbo"".""Orders"" ""o"" WHERE ""o"".""IsDeleted""=0",
                null
            ),
            new PgSample(
                "Insert_ValuesMultiRow",
                @"INSERT INTO [dbo].[Users]([UserId],[Name]) VALUES (1,'A'),(2,'B')",
                @"INSERT INTO ""dbo"".""Users""(""UserId"",""Name"") VALUES (1,'A'),(2,'B')",
                null
            ),
            new PgSample(
                "Merge_Output_WithActionInsertedDeletedSource",
                @"MERGE [dbo].[Users] [A0] USING (VALUES (1,'Alice'),(2,'Bob'))[A1]([UserId],[FirstName]) ON [A0].[UserId]=[A1].[UserId] WHEN MATCHED THEN  DELETE WHEN NOT MATCHED BY SOURCE THEN  DELETE OUTPUT [A1].[UserId],INSERTED.[LastName] [LN],DELETED.[UserId],$ACTION [Act];",
                null,
                @"Feature 'OUTPUT' is not supported by SqExpress parser for MERGE statements."
            ),
            new PgSample(
                "Merge_UpdateInsertDelete",
                @"MERGE [dbo].[Users] [A0] USING [dbo].[UsersStaging] [s] ON [A0].[UserId]=[s].[UserId] WHEN MATCHED THEN UPDATE SET [A0].[Name]=[s].[Name],[A0].[IsActive]=[s].[IsActive] WHEN NOT MATCHED THEN INSERT([UserId],[Name],[IsActive]) VALUES([s].[UserId],[s].[Name],[s].[IsActive]) WHEN NOT MATCHED BY SOURCE THEN  DELETE;",
                null,
                @"EXPORT: Could not determine MERGE source columns"
            ),
            new PgSample(
                "Select_SchemaQualifiedScalarFunction",
                @"SELECT [dbo].[NormalizeUserName]([u].[Name]) [NameLength] FROM [dbo].[Users] [u]",
                @"SELECT ""dbo"".""NormalizeUserName""(""u"".""Name"") ""NameLength"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_SchemaFunctionTableReference",
                @"SELECT [f].[Value] FROM [dbo].[MyFunc](1) [f]",
                @"SELECT ""f"".""Value"" FROM ""dbo"".""MyFunc""(1) ""f""",
                null
            ),
            new PgSample(
                "Select_BuiltinFunctionTableReference",
                @"SELECT [s].[value] FROM STRING_SPLIT('a,b',',') [s]",
                @"SELECT ""s"".""value"" FROM STRING_SPLIT('a,b',',') ""s""",
                null
            ),
            new PgSample(
                "Select_DerivedTable_TwoLevels",
                @"SELECT [sq].[A] FROM (SELECT [i].[Id] [A] FROM (SELECT 1 [Id])[i])[sq]",
                @"SELECT ""sq"".""A"" FROM (SELECT ""i"".""Id"" ""A"" FROM (SELECT 1 ""Id"")""i"")""sq""",
                null
            ),
            new PgSample(
                "Select_DerivedTable_WithOffsetFetch",
                @"SELECT [sq].[A] FROM (SELECT 1 [A] ORDER BY [A] OFFSET 0 ROW FETCH NEXT 1 ROW ONLY)[sq]",
                @"SELECT ""sq"".""A"" FROM (SELECT 1 ""A"" ORDER BY ""A"" OFFSET 0 ROW FETCH NEXT 1 ROW ONLY)""sq""",
                null
            ),
            new PgSample(
                "Select_DerivedTable_WithUnionAll",
                @"SELECT [sq].[A] FROM (SELECT 1 [A] UNION ALL SELECT 2 [A])[sq]",
                @"SELECT ""sq"".""A"" FROM (SELECT 1 ""A"" UNION ALL SELECT 2 ""A"")""sq""",
                null
            ),
            new PgSample(
                "Select_DerivedTable_Simple",
                @"SELECT [sq].[A] FROM (SELECT 1 [A])[sq]",
                @"SELECT ""sq"".""A"" FROM (SELECT 1 ""A"")""sq""",
                null
            ),
            new PgSample(
                "Select_DerivedTable_WithRenamedColumn",
                @"SELECT [sq].[PatronymicName],[sq].[FirstName] FROM (SELECT [u].[LastName] [PatronymicName],[u].[FirstName] FROM [dbo].[Users] [u])[sq]",
                @"SELECT ""sq"".""PatronymicName"",""sq"".""FirstName"" FROM (SELECT ""u"".""LastName"" ""PatronymicName"",""u"".""FirstName"" FROM ""dbo"".""Users"" ""u"")""sq""",
                null
            ),
            new PgSample(
                "Select_DerivedTable_WithWhere",
                @"SELECT [sq].[UserId] FROM (SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name]='A')[sq]",
                @"SELECT ""sq"".""UserId"" FROM (SELECT ""u"".""UserId"" FROM ""dbo"".""Users"" ""u"" WHERE ""u"".""Name""='A')""sq""",
                null
            ),
            new PgSample(
                "Select_WildcardWithJoinedColumn",
                @"SELECT [u].*,[o].[OrderId] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId]",
                @"SELECT ""u"".*,""o"".""OrderId"" FROM ""dbo"".""Users"" ""u"" JOIN ""dbo"".""Orders"" ""o"" ON ""o"".""UserId""=""u"".""UserId""",
                null
            ),
            new PgSample(
                "Select_IsNullPredicate",
                @"SELECT [u].[CreatedAt],[u].[UpdatedUtc],[u].[IsDeleted],[u].[Description] FROM [dbo].[Users] [u] WHERE [u].[Description] IS NULL",
                @"SELECT ""u"".""CreatedAt"",""u"".""UpdatedUtc"",""u"".""IsDeleted"",""u"".""Description"" FROM ""dbo"".""Users"" ""u"" WHERE ""u"".""Description"" IS NULL",
                null
            ),
            new PgSample(
                "Select_StringEqualityPredicate",
                @"SELECT [u].[Name] FROM [dbo].[Users] [u] WHERE [u].[Name]='A'",
                @"SELECT ""u"".""Name"" FROM ""dbo"".""Users"" ""u"" WHERE ""u"".""Name""='A'",
                null
            ),
            new PgSample(
                "Select_GroupByWithCount",
                @"SELECT [u].[Status],COUNT(1) [Total] FROM [dbo].[Users] [u] GROUP BY [u].[Status]",
                @"SELECT ""u"".""Status"",COUNT(1) ""Total"" FROM ""dbo"".""Users"" ""u"" GROUP BY ""u"".""Status""",
                null
            ),
            new PgSample(
                "Select_BasicProjection",
                @"SELECT [u].[UserId] FROM [dbo].[Users] [u]",
                @"SELECT ""u"".""UserId"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_ComplexWhere_InLikeRange",
                @"SELECT [u].[UserId] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] WHERE [u].[Status] IN(1,2,3) AND [o].[Title] LIKE 'A%' AND [u].[Score]>=10 AND [u].[Score]<=20",
                @"SELECT ""u"".""UserId"" FROM ""dbo"".""Users"" ""u"" JOIN ""dbo"".""Orders"" ""o"" ON ""o"".""UserId""=""u"".""UserId"" WHERE ""u"".""Status"" IN(1,2,3) AND ""o"".""Title"" LIKE 'A%' AND ""u"".""Score"">=10 AND ""u"".""Score""<=20",
                null
            ),
            new PgSample(
                "Select_OrderOffsetFetch",
                @"SELECT [u].[UserId] FROM [dbo].[Users] [u] ORDER BY [u].[UserId] OFFSET 10 ROW FETCH NEXT 5 ROW ONLY",
                @"SELECT ""u"".""UserId"" FROM ""dbo"".""Users"" ""u"" ORDER BY ""u"".""UserId"" OFFSET 10 ROW FETCH NEXT 5 ROW ONLY",
                null
            ),
            new PgSample(
                "Select_OrderOffsetOnly",
                @"SELECT [u].[UserId] FROM [dbo].[Users] [u] ORDER BY [u].[UserId] OFFSET 20 ROW",
                @"SELECT ""u"".""UserId"" FROM ""dbo"".""Users"" ""u"" ORDER BY ""u"".""UserId"" OFFSET 20 ROW",
                null
            ),
            new PgSample(
                "Select_InWithEmptyString",
                @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name] IN('')",
                @"SELECT ""u"".""UserId"" FROM ""dbo"".""Users"" ""u"" WHERE ""u"".""Name"" IN('')",
                null
            ),
            new PgSample(
                "Select_EqualityWithEmptyString",
                @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name]=''",
                @"SELECT ""u"".""UserId"" FROM ""dbo"".""Users"" ""u"" WHERE ""u"".""Name""=''",
                null
            ),
            new PgSample(
                "Select_WhereOrder",
                @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name]='A' ORDER BY [u].[UserId]",
                @"SELECT ""u"".""UserId"" FROM ""dbo"".""Users"" ""u"" WHERE ""u"".""Name""='A' ORDER BY ""u"".""UserId""",
                null
            ),
            new PgSample(
                "Select_InSubquery",
                @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[UserId] IN(SELECT [o].[UserId] FROM [dbo].[Orders] [o])",
                @"SELECT ""u"".""UserId"" FROM ""dbo"".""Users"" ""u"" WHERE ""u"".""UserId"" IN(SELECT ""o"".""UserId"" FROM ""dbo"".""Orders"" ""o"")",
                null
            ),
            new PgSample(
                "Select_ConstantBooleanAnd",
                @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[UserId]=0 AND 0>0",
                @"SELECT ""u"".""UserId"" FROM ""dbo"".""Users"" ""u"" WHERE ""u"".""UserId""=0 AND 0>0",
                null
            ),
            new PgSample(
                "Select_ExistsSubquery",
                @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE EXISTS(SELECT 1 FROM [dbo].[Orders] [o] WHERE [o].[UserId]=[u].[UserId])",
                @"SELECT ""u"".""UserId"" FROM ""dbo"".""Users"" ""u"" WHERE EXISTS(SELECT 1 FROM ""dbo"".""Orders"" ""o"" WHERE ""o"".""UserId""=""u"".""UserId"")",
                null
            ),
            new PgSample(
                "Select_UnqualifiedTableName",
                @"SELECT [u].[UserId] FROM [Users] [u]",
                @"SELECT ""u"".""UserId"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_ScalarSubqueryInProjection",
                @"SELECT [u].[UserId],(SELECT COUNT(1) FROM [dbo].[Orders] [o] WHERE [o].[UserId]=[u].[UserId]) [OrderCount] FROM [dbo].[Users] [u]",
                @"SELECT ""u"".""UserId"",(SELECT COUNT(1) FROM ""dbo"".""Orders"" ""o"" WHERE ""o"".""UserId""=""u"".""UserId"") ""OrderCount"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_CrossApply",
                @"SELECT [u].[UserId],[oa].[OrderId] FROM [dbo].[Users] [u] CROSS APPLY (SELECT [o].[OrderId] FROM [dbo].[Orders] [o] ORDER BY [o].[OrderId] DESC OFFSET 0 ROW FETCH NEXT 1 ROW ONLY)[oa]",
                @"SELECT ""u"".""UserId"",""oa"".""OrderId"" FROM ""dbo"".""Users"" ""u"" CROSS JOIN LATERAL (SELECT ""o"".""OrderId"" FROM ""dbo"".""Orders"" ""o"" ORDER BY ""o"".""OrderId"" DESC OFFSET 0 ROW FETCH NEXT 1 ROW ONLY)""oa""",
                null
            ),
            new PgSample(
                "Select_OuterApply",
                @"SELECT [u].[UserId],[oa].[OrderId] FROM [dbo].[Users] [u] OUTER APPLY (SELECT [o].[OrderId] FROM [dbo].[Orders] [o] ORDER BY [o].[OrderId] DESC OFFSET 0 ROW FETCH NEXT 1 ROW ONLY)[oa]",
                @"SELECT ""u"".""UserId"",""oa"".""OrderId"" FROM ""dbo"".""Users"" ""u"" LEFT JOIN LATERAL(SELECT ""o"".""OrderId"" FROM ""dbo"".""Orders"" ""o"" ORDER BY ""o"".""OrderId"" DESC OFFSET 0 ROW FETCH NEXT 1 ROW ONLY)""oa""",
                null
            ),
            new PgSample(
                "Select_OrderByDescending",
                @"SELECT [u].[UserId],[u].[Name] [UserName] FROM [dbo].[Users] [u] WHERE [u].[IsActive]=1 ORDER BY [u].[Name] DESC",
                @"SELECT ""u"".""UserId"",""u"".""Name"" ""UserName"" FROM ""dbo"".""Users"" ""u"" WHERE ""u"".""IsActive""=1 ORDER BY ""u"".""Name"" DESC",
                null
            ),
            new PgSample(
                "Select_UnionAllWithMixedLiteralTypes",
                @"SELECT [u1].[UserId] FROM [dbo].[Users] [u1] WHERE [u1].[UserId]=1 UNION ALL SELECT [u2].[UserId] FROM [dbo].[Users] [u2] WHERE [u2].[UserId]='A'",
                @"SELECT ""u1"".""UserId"" FROM ""dbo"".""Users"" ""u1"" WHERE ""u1"".""UserId""=1 UNION ALL SELECT ""u2"".""UserId"" FROM ""dbo"".""Users"" ""u2"" WHERE ""u2"".""UserId""='A'",
                null
            ),
            new PgSample(
                "Select_Except",
                @"SELECT 1 [A] EXCEPT SELECT 2 [A]",
                @"SELECT 1 ""A"" EXCEPT SELECT 2 ""A""",
                null
            ),
            new PgSample(
                "Select_Intersect",
                @"SELECT 1 [A] INTERSECT SELECT 2 [A]",
                @"SELECT 1 ""A"" INTERSECT SELECT 2 ""A""",
                null
            ),
            new PgSample(
                "Select_UnionAll",
                @"SELECT 1 [A] UNION ALL SELECT 2 [A]",
                @"SELECT 1 ""A"" UNION ALL SELECT 2 ""A""",
                null
            ),
            new PgSample(
                "Select_UnionAllWithTopLevelOrderOffsetFetch",
                @"SELECT 1 [A] UNION ALL SELECT 2 [A] ORDER BY [A] OFFSET 1 ROW FETCH NEXT 1 ROW ONLY",
                @"SELECT 1 ""A"" UNION ALL SELECT 2 ""A"" ORDER BY ""A"" OFFSET 1 ROW FETCH NEXT 1 ROW ONLY",
                null
            ),
            new PgSample(
                "Select_UnionDistinct",
                @"SELECT 1 [A] UNION SELECT 2 [A]",
                @"SELECT 1 ""A"" UNION SELECT 2 ""A""",
                null
            ),
            new PgSample(
                "Select_KnownFunctions",
                @"SELECT ABS([u].[V1]) [Total],LEN([u].[V2]) [NameLen],DATEADD(d,1,[u].[V3]) [NextAt] FROM [dbo].[Users] [u]",
                @"SELECT ABS(""u"".""V1"") ""Total"",CHAR_LENGTH(""u"".""V2"") ""NameLen"",""u"".""V3""+INTERVAL'1d' ""NextAt"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_WindowAvgWithFrame",
                @"SELECT AVG([u].[Score])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) [AvgScore] FROM [dbo].[Users] [u]",
                @"SELECT AVG(""u"".""Score"")OVER(ORDER BY ""u"".""UserId"") ""AvgScore"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_CaseWhenElse",
                @"SELECT CASE WHEN [u].[IsActive]=1 THEN 'Y' ELSE 'N' END [ActiveMark] FROM [dbo].[Users] [u]",
                @"SELECT CASE WHEN ""u"".""IsActive""=1 THEN 'Y' ELSE 'N' END ""ActiveMark"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_CaseWhenMultipleBranches",
                @"SELECT CASE WHEN [u].[Status]=1 THEN 'A' WHEN [u].[Status]=2 THEN 'B' ELSE 'C' END [StatusName] FROM [dbo].[Users] [u]",
                @"SELECT CASE WHEN ""u"".""Status""=1 THEN 'A' WHEN ""u"".""Status""=2 THEN 'B' ELSE 'C' END ""StatusName"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_CoalesceDisplayName",
                @"SELECT COALESCE([u].[Name],[u].[Login],'NA') [DisplayName] FROM [dbo].[Users] [u]",
                @"SELECT COALESCE(""u"".""Name"",""u"".""Login"",'NA') ""DisplayName"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_CoalesceDisplayName_DuplicateCoverage",
                @"SELECT COALESCE([u].[Name],[u].[Login],'NA') [DisplayName] FROM [dbo].[Users] [u]",
                @"SELECT COALESCE(""u"".""Name"",""u"".""Login"",'NA') ""DisplayName"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_CountFromTable",
                @"SELECT COUNT(1) [Total] FROM [dbo].[Users]",
                @"SELECT COUNT(1) ""Total"" FROM ""dbo"".""Users""",
                null
            ),
            new PgSample(
                "Select_WindowCount",
                @"SELECT COUNT([u].[Score])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) [CntScore] FROM [dbo].[Users] [u]",
                @"SELECT COUNT(""u"".""Score"")OVER(ORDER BY ""u"".""UserId"") ""CntScore"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_CountFromAliasedTable",
                @"SELECT COUNT(1) [Total] FROM [dbo].[Users] [A0]",
                @"SELECT COUNT(1) ""Total"" FROM ""dbo"".""Users"" ""A0""",
                null
            ),
            new PgSample(
                "Select_DateAddAlias",
                @"SELECT DATEADD(d,1,[u].[CreatedAt]) [NextDate] FROM [dbo].[Users] [u]",
                @"SELECT ""u"".""CreatedAt""+INTERVAL'1d' ""NextDate"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_DateDiff",
                @"SELECT DATEDIFF(DAY,[u].[CreatedAt],[u].[UpdatedAt]) [Days] FROM [dbo].[Users] [u]",
                @"SELECT CAST(DATE_PART('DAY',DATE_TRUNC('DAY',""u"".""UpdatedAt"")-DATE_TRUNC('DAY',""u"".""CreatedAt"")) AS int4) ""Days"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_DistinctTop",
                @"SELECT DISTINCT TOP 10 [u].[UserId] FROM [dbo].[Users] [u]",
                @"SELECT DISTINCT ""u"".""UserId"" FROM ""dbo"".""Users"" ""u"" LIMIT 10",
                null
            ),
            new PgSample(
                "Select_WindowFirstValueAndLastValue",
                @"SELECT FIRST_VALUE([u].[FirstName])OVER(ORDER BY [u].[UserId]) [FirstNameFirst],LAST_VALUE([u].[FirstName])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) [FirstNameLast] FROM [dbo].[Users] [u]",
                @"SELECT FIRST_VALUE(""u"".""FirstName"")OVER(ORDER BY ""u"".""UserId"") ""FirstNameFirst"",LAST_VALUE(""u"".""FirstName"")OVER(ORDER BY ""u"".""UserId"") ""FirstNameLast"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_GetDate",
                @"SELECT GETDATE() [Now]",
                @"SELECT now() ""Now""",
                null
            ),
            new PgSample(
                "Select_GetUtcDate",
                @"SELECT GETUTCDATE() [NowUtc]",
                @"SELECT now() at time zone 'utc' ""NowUtc""",
                null
            ),
            new PgSample(
                "Select_IsNullFunction",
                @"SELECT ISNULL([u].[Name],'NA') [Name2] FROM [dbo].[Users] [u]",
                @"SELECT COALESCE(""u"".""Name"",'NA') ""Name2"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_LenFunction",
                @"SELECT LEN([u].[Name]) [NameLength] FROM [dbo].[Users] [u]",
                @"SELECT CHAR_LENGTH(""u"".""Name"") ""NameLength"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_WindowMax",
                @"SELECT MAX([u].[Score])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) [MaxScore] FROM [dbo].[Users] [u]",
                @"SELECT MAX(""u"".""Score"")OVER(ORDER BY ""u"".""UserId"") ""MaxScore"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_WindowMin",
                @"SELECT MIN([u].[Score])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) [MinScore] FROM [dbo].[Users] [u]",
                @"SELECT MIN(""u"".""Score"")OVER(ORDER BY ""u"".""UserId"") ""MinScore"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_WindowRowNumber",
                @"SELECT ROW_NUMBER()OVER(ORDER BY [u].[UserId]) [Rn] FROM [dbo].[Users] [u]",
                @"SELECT ROW_NUMBER()OVER(ORDER BY ""u"".""UserId"") ""Rn"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_WindowSum",
                @"SELECT SUM([u].[Score])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) [RunningScore] FROM [dbo].[Users] [u]",
                @"SELECT SUM(""u"".""Score"")OVER(ORDER BY ""u"".""UserId"") ""RunningScore"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Select_WindowSumPartitionBy",
                @"SELECT SUM([u].[Score])OVER(PARTITION BY [u].[GroupId] ORDER BY [u].[UserId]) [RunningScore] FROM [dbo].[Users] [u]",
                @"SELECT SUM(""u"".""Score"")OVER(PARTITION BY ""u"".""GroupId"" ORDER BY ""u"".""UserId"") ""RunningScore"" FROM ""dbo"".""Users"" ""u""",
                null
            ),
            new PgSample(
                "Update_WithCrossApplyDerivedAndNotLike",
                @"UPDATE [u] SET [u].[Name]=[x].[Name] FROM [dbo].[Users] [u] CROSS APPLY (SELECT [u].[Name] [Name])[x] WHERE NOT [u].[Name] LIKE 'A%'",
                @"UPDATE ""dbo"".""Users"" ""u"" SET ""Name""=""x"".""Name"" FROM (SELECT ""u"".""Name"")""x"" WHERE NOT ""u"".""Name"" LIKE 'A%'",
                null
            ),
            new PgSample(
                "Update_WithCrossApplyTableFunction",
                @"UPDATE [u] SET [u].[Name]=[s].[value] FROM [dbo].[Users] [u] CROSS APPLY STRING_SPLIT('a,b',',') [s] WHERE [u].[UserId]=1",
                @"UPDATE ""dbo"".""Users"" ""u"" SET ""Name""=""s"".""value"" FROM STRING_SPLIT('a,b',',') WHERE ""u"".""UserId""=1",
                null
            ),
            new PgSample(
                "Update_WithJoinAndLike",
                @"UPDATE [u] SET [u].[Name]=[o].[Title],[u].[IsActive]=1 FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] WHERE [o].[Title] LIKE 'A%'",
                @"UPDATE ""dbo"".""Users"" ""u"" SET ""Name""=""o"".""Title"",""IsActive""=1 FROM ""dbo"".""Orders"" ""o"" WHERE ""o"".""UserId""=""u"".""UserId"" AND ""o"".""Title"" LIKE 'A%'",
                null
            ),
            new PgSample(
                "Edge_Select_NestedBooleanParenthesesAndNot",
                @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE ([u].[IsActive]=1 OR [u].[IsDeleted]=0)AND NOT [u].[Name]='X'",
                @"SELECT ""u"".""UserId"" FROM ""dbo"".""Users"" ""u"" WHERE (""u"".""IsActive""=1 OR ""u"".""IsDeleted""=0)AND NOT ""u"".""Name""='X'",
                null
            ),
            new PgSample(
                "Edge_Select_DeepNestedFromSubqueries",
                @"SELECT [A].[V] FROM (SELECT [B].[V] FROM (SELECT [C].[V] FROM (SELECT 1 [V])[C])[B])[A]",
                @"SELECT ""A"".""V"" FROM (SELECT ""B"".""V"" FROM (SELECT ""C"".""V"" FROM (SELECT 1 ""V"")""C"")""B"")""A""",
                null
            ),
            new PgSample(
                "Edge_Select_FromValuesConstructor",
                @"SELECT [v].[Id],[v].[Val] FROM (VALUES (1,'a'),(2,'b'))[v]([Id],[Val])",
                @"SELECT ""v"".""Id"",""v"".""Val"" FROM (VALUES (1,'a'),(2,'b'))""v""(""Id"",""Val"")",
                null
            ),
            new PgSample(
                "Edge_Select_NestedValuesDerivedTable",
                @"SELECT [A].[Id] FROM (SELECT [v].[Id] FROM (VALUES (1,'a'))[v]([Id],[Val]))[A]",
                @"SELECT ""A"".""Id"" FROM (SELECT ""v"".""Id"" FROM (VALUES (1,'a'))""v""(""Id"",""Val""))""A""",
                null
            ),
            new PgSample(
                "Edge_Select_InListWithNullLiteral",
                @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Status] IN(1,NULL,3)",
                @"SELECT ""u"".""UserId"" FROM ""dbo"".""Users"" ""u"" WHERE ""u"".""Status"" IN(1,NULL,3)",
                null
            ),
            new PgSample(
                "Edge_Select_IsNotNull",
                @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name] IS NOT NULL",
                @"SELECT ""u"".""UserId"" FROM ""dbo"".""Users"" ""u"" WHERE ""u"".""Name"" IS NOT NULL",
                null
            ),
            new PgSample(
                "Edge_Select_SelfJoinDistinctAliases",
                @"SELECT [u1].[UserId],[u2].[UserId] [OtherUserId] FROM [dbo].[Users] [u1] JOIN [dbo].[Users] [u2] ON [u2].[ManagerId]=[u1].[UserId]",
                @"SELECT ""u1"".""UserId"",""u2"".""UserId"" ""OtherUserId"" FROM ""dbo"".""Users"" ""u1"" JOIN ""dbo"".""Users"" ""u2"" ON ""u2"".""ManagerId""=""u1"".""UserId""",
                null
            ),
            new PgSample(
                "Edge_Select_ExistsWithNestedDerivedTable",
                @"SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE EXISTS(SELECT 1 FROM (SELECT [o].[UserId] FROM [dbo].[Orders] [o] WHERE [o].[Amount]>0)[x] WHERE [x].[UserId]=[u].[UserId])",
                @"SELECT ""u"".""UserId"" FROM ""dbo"".""Users"" ""u"" WHERE EXISTS(SELECT 1 FROM (SELECT ""o"".""UserId"" FROM ""dbo"".""Orders"" ""o"" WHERE ""o"".""Amount"">0)""x"" WHERE ""x"".""UserId""=""u"".""UserId"")",
                null
            ),
            new PgSample(
                "Edge_Select_OrderOffsetFetchSingleRow",
                @"SELECT [u].[UserId],[u].[Name] FROM [dbo].[Users] [u] ORDER BY [u].[UserId] OFFSET 0 ROW FETCH NEXT 1 ROW ONLY",
                @"SELECT ""u"".""UserId"",""u"".""Name"" FROM ""dbo"".""Users"" ""u"" ORDER BY ""u"".""UserId"" OFFSET 0 ROW FETCH NEXT 1 ROW ONLY",
                null
            ),
        };

        private sealed class PgSample
        {
            public PgSample(string name, string sql, string? pgSql, string? unsupportedReason)
            {
                this.Name = name;
                this.Sql = sql;
                this.PgSql = pgSql;
                this.UnsupportedReason = unsupportedReason;
            }

            public string Name { get; }

            public string Sql { get; }

            public string? PgSql { get; }

            public string? UnsupportedReason { get; }
        }
    }
}


