using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using SqExpress.SqlExport;
using SqExpress.Syntax;
using SqExpress.Syntax.Select;
using SqExpress.SqlTranspiler;

namespace SqExpress.SqlTranspiler.Test
{
    [TestFixture]
    public class SqExpressSqlTranspilerTest
    {
        private const string SqlBasic = "SELECT [u].[UserId],[u].[Name] [UserName] FROM [dbo].[Users] [u] WHERE [u].[IsActive]=1 ORDER BY [u].[Name] DESC";
        private const string SqlDistinctTop = "SELECT DISTINCT TOP 10 [u].[UserId] FROM [dbo].[Users] [u]";
        private const string SqlJoinPredicates = "SELECT [u].[UserId] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] WHERE [u].[Status] IN(1,2,3) AND [o].[Title] LIKE 'A%' AND [u].[Score]>=10 AND [u].[Score]<=20";
        private const string SqlCountStar = "SELECT COUNT(1) [Total] FROM [dbo].[Users] [A0]";
        private const string SqlQualifiedStar = "SELECT [u].*,[o].[OrderId] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId]";
        private const string SqlFunction = "SELECT LEN([u].[Name]) [NameLength] FROM [dbo].[Users] [u]";
        private const string SqlSchemaFunction = "SELECT [dbo].[NormalizeUserName]([u].[Name]) [NameLength] FROM [dbo].[Users] [u]";
        private const string SqlSelect1 = "SELECT 1";
        private const string SqlStringComparison = "SELECT [u].[Name] FROM [dbo].[Users] [u] WHERE [u].[Name]='A'";
        private const string SqlSubSimple = "SELECT [sq].[A] FROM (SELECT 1 [A])[sq]";
        private const string SqlSubWithTable = "SELECT [sq].[UserId] FROM (SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name]='A')[sq]";
        private const string SqlSubNested = "SELECT [sq].[A] FROM (SELECT [i].[Id] [A] FROM (SELECT 1 [Id])[i])[sq]";
        private const string SqlSubRenamedFromSource = "SELECT [sq].[PatronymicName],[sq].[FirstName] FROM (SELECT [u].[LastName] [PatronymicName],[u].[FirstName] FROM [dbo].[Users] [u])[sq]";
        private const string SqlHeuristicAndNullable = "SELECT [u].[CreatedAt],[u].[UpdatedUtc],[u].[IsDeleted],[u].[Description] FROM [dbo].[Users] [u] WHERE [u].[Description] IS NULL";
        private const string SqlRuntimeOptions = "SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name]='A' ORDER BY [u].[UserId]";
        private const string SqlUnqualifiedUsersWithDefaultSchema = "SELECT [u].[UserId] FROM [dbo].[Users] [u]";
        private const string SqlUnqualifiedUsersWithoutDefaultSchema = "SELECT [u].[UserId] FROM [Users] [u]";
        private const string SqlCaseWhen = "SELECT CASE WHEN [u].[IsActive]=1 THEN 'Y' ELSE 'N' END [ActiveMark] FROM [dbo].[Users] [u]";
        private const string SqlCaseSimple = "SELECT CASE WHEN [u].[Status]=1 THEN 'A' WHEN [u].[Status]=2 THEN 'B' ELSE 'C' END [StatusName] FROM [dbo].[Users] [u]";
        private const string SqlIsNull = "SELECT ISNULL([u].[Name],'NA') [Name2] FROM [dbo].[Users] [u]";
        private const string SqlCoalesce = "SELECT COALESCE([u].[Name],[u].[Login],'NA') [DisplayName] FROM [dbo].[Users] [u]";
        private const string SqlGetDate = "SELECT GETDATE() [Now]";
        private const string SqlGetUtcDate = "SELECT GETUTCDATE() [NowUtc]";
        private const string SqlDateAdd = "SELECT DATEADD(d,1,[u].[CreatedAt]) [NextDate] FROM [dbo].[Users] [u]";
        private const string SqlDateDiff = "SELECT DATEDIFF(DAY,[u].[CreatedAt],[u].[UpdatedAt]) [Days] FROM [dbo].[Users] [u]";
        private const string SqlWindowRowNumber = "SELECT ROW_NUMBER()OVER(ORDER BY [u].[UserId]) [Rn] FROM [dbo].[Users] [u]";
        private const string SqlWindowSumPartitionOrder = "SELECT SUM([u].[Score])OVER(PARTITION BY [u].[GroupId] ORDER BY [u].[UserId]) [RunningScore] FROM [dbo].[Users] [u]";
        private const string SqlWindowRowsFrame = "SELECT SUM([u].[Score])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) [RunningScore] FROM [dbo].[Users] [u]";
        private const string SqlWindowFirstLastValue = "SELECT FIRST_VALUE([u].[FirstName])OVER(ORDER BY [u].[UserId]) [FirstNameFirst],LAST_VALUE([u].[FirstName])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) [FirstNameLast] FROM [dbo].[Users] [u]";
        private const string SqlWindowMinFrame = "SELECT MIN([u].[Score])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) [MinScore] FROM [dbo].[Users] [u]";
        private const string SqlWindowMaxFrame = "SELECT MAX([u].[Score])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) [MaxScore] FROM [dbo].[Users] [u]";
        private const string SqlWindowAvgFrame = "SELECT AVG([u].[Score])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) [AvgScore] FROM [dbo].[Users] [u]";
        private const string SqlWindowCountFrame = "SELECT COUNT([u].[Score])OVER(ORDER BY [u].[UserId] ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) [CntScore] FROM [dbo].[Users] [u]";
        private const string SqlCteSimple = "WITH [C] AS(SELECT 1 [A])SELECT [c].[A] FROM [C] [c]";
        private const string SqlCteWithTable = "WITH [CU] AS(SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name]='A')SELECT [c].[UserId] FROM [CU] [c]";
        private const string SqlGroupByBasic = "SELECT [u].[Status],COUNT(1) [Total] FROM [dbo].[Users] [u] GROUP BY [u].[Status]";
        private const string SqlUnion = "SELECT 1 [A] UNION SELECT 2 [A]";
        private const string SqlUnionAll = "SELECT 1 [A] UNION ALL SELECT 2 [A]";
        private const string SqlExcept = "SELECT 1 [A] EXCEPT SELECT 2 [A]";
        private const string SqlIntersect = "SELECT 1 [A] INTERSECT SELECT 2 [A]";
        private const string SqlOffsetFetch = "SELECT [u].[UserId] FROM [dbo].[Users] [u] ORDER BY [u].[UserId] OFFSET 10 ROW FETCH NEXT 5 ROW ONLY";
        private const string SqlOffsetOnly = "SELECT [u].[UserId] FROM [dbo].[Users] [u] ORDER BY [u].[UserId] OFFSET 20 ROW";
        private const string SqlUnionAllOffsetFetch = "SELECT 1 [A] UNION ALL SELECT 2 [A] ORDER BY [A] OFFSET 1 ROW FETCH NEXT 1 ROW ONLY";
        private const string SqlUnionAllSameTableTypeConflict = "SELECT [u1].[UserId] FROM [dbo].[Users] [u1] WHERE [u1].[UserId]=1 UNION ALL SELECT [u2].[UserId] FROM [dbo].[Users] [u2] WHERE [u2].[UserId]='A'";
        private const string SqlOperatorInference = "SELECT ABS([u].[V1]) [Total],LEN([u].[V2]) [NameLen],DATEADD(d,1,[u].[V3]) [NextAt] FROM [dbo].[Users] [u]";
        private const string SqlInsertValues = "INSERT INTO [dbo].[Users]([UserId],[Name]) VALUES (1,'A'),(2,'B')";
        private const string SqlInsertFromSelect = "INSERT INTO [dbo].[Users]([UserId],[Name]) SELECT [o].[UserId],[o].[Title] FROM [dbo].[Orders] [o] WHERE [o].[IsDeleted]=0";
        private const string SqlUpdateWithJoin = "UPDATE [u] SET [u].[Name]=[o].[Title],[u].[IsActive]=1 FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] WHERE [o].[Title] LIKE 'A%'";
        private const string SqlDeleteWithJoin = "DELETE [u] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] WHERE [o].[IsDeleted]=1";
        private const string SqlMergeBasic = "MERGE [dbo].[Users] [A0] USING [dbo].[UsersStaging] [s] ON [A0].[UserId]=[s].[UserId] WHEN MATCHED THEN UPDATE SET [A0].[Name]=[s].[Name],[A0].[IsActive]=[s].[IsActive] WHEN NOT MATCHED THEN INSERT([UserId],[Name],[IsActive]) VALUES([s].[UserId],[s].[Name],[s].[IsActive]) WHEN NOT MATCHED BY SOURCE THEN  DELETE;";
        private const string SqlInSubQuery = "SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[UserId] IN(SELECT [o].[UserId] FROM [dbo].[Orders] [o])";
        private const string SqlExistsSubQuery = "SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE EXISTS(SELECT 1 FROM [dbo].[Orders] [o] WHERE [o].[UserId]=[u].[UserId])";
        private const string SqlScalarSubQuery = "SELECT [u].[UserId],(SELECT COUNT(1) FROM [dbo].[Orders] [o] WHERE [o].[UserId]=[u].[UserId]) [OrderCount] FROM [dbo].[Users] [u]";
        private const string SqlCrossApply = "SELECT [u].[UserId],[oa].[OrderId] FROM [dbo].[Users] [u] CROSS APPLY (SELECT [o].[OrderId] FROM [dbo].[Orders] [o] ORDER BY [o].[OrderId] DESC OFFSET 0 ROW FETCH NEXT 1 ROW ONLY)[oa]";
        private const string SqlOuterApply = "SELECT [u].[UserId],[oa].[OrderId] FROM [dbo].[Users] [u] OUTER APPLY (SELECT [o].[OrderId] FROM [dbo].[Orders] [o] ORDER BY [o].[OrderId] DESC OFFSET 0 ROW FETCH NEXT 1 ROW ONLY)[oa]";
        private const string SqlFromBuiltInTableFunction = "SELECT [s].[value] FROM STRING_SPLIT('a,b',',') [s]";
        private const string SqlFromSchemaTableFunction = "SELECT [f].[Value] FROM [dbo].[MyFunc](1) [f]";
        private const string SqlCteUnionAll = "WITH [C] AS(SELECT 1 [A] UNION ALL SELECT 2 [A])SELECT [c].[A] FROM [C] [c]";
        private const string SqlDerivedUnion = "SELECT [sq].[A] FROM (SELECT 1 [A] UNION ALL SELECT 2 [A])[sq]";
        private const string SqlDerivedOrderOffset = "SELECT [sq].[A] FROM (SELECT 1 [A] ORDER BY [A] OFFSET 0 ROW FETCH NEXT 1 ROW ONLY)[sq]";
        private const string SqlVariableStringCompare = "SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name]=''";
        private const string SqlVariableIntCompare = "SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[UserId]=0 AND 0>0";
        private const string SqlVariableInList = "SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name] IN('')";
        [Test]
        public void TranspileSelect_BasicQuery_GeneratesQueryAndDeclarations()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var result = transpiler.Transpile(
                "SELECT u.UserId, u.Name AS UserName FROM dbo.Users u WHERE u.IsActive = 1 ORDER BY u.Name DESC");

            Assert.AreEqual("SELECT", result.StatementKind);
            Assert.That(result.QueryCSharpCode, Does.Contain("public static IExprQuery Build(out TableUsers u)"));
            Assert.That(result.QueryCSharpCode, Does.Contain("u = new TableUsers(\"u\");"));
            Assert.That(result.QueryCSharpCode, Does.Contain("public static async Task Query(ISqDatabase database)"));
            Assert.That(result.QueryCSharpCode, Does.Contain("await foreach (var r in Build(out var u).Query(database))"));
            Assert.That(result.QueryCSharpCode, Does.Contain("var userId = u.UserId.Read(r);"));
            Assert.That(result.QueryCSharpCode, Does.Contain("var userName = u.Name.Read(r, \"UserName\");"));
            Assert.That(result.QueryCSharpCode, Does.Contain("Select(u.UserId, u.Name.As(\"UserName\"))"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".Where(u.IsActive == 1)"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public sealed class TableUsers : TableBase"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public Int32TableColumn UserId"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public StringTableColumn Name"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public BooleanTableColumn IsActive"));

            AssertCompilesAndSql(result, SqlBasic);
        }

        [Test]
        public void TranspileSelect_DistinctTop_GeneratesSelectTopDistinct()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var result = transpiler.Transpile("SELECT DISTINCT TOP (10) u.UserId FROM dbo.Users u");

            Assert.That(result.QueryCSharpCode, Does.Contain("SelectTopDistinct(10, u.UserId)"));
            AssertCompilesAndSql(result, SqlDistinctTop);
        }

        [Test]
        public void TranspileSelect_WithJoinAndPredicates_GeneratesJoinAndFiltersAndStringType()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var sql = "SELECT u.UserId " +
                      "FROM dbo.Users u INNER JOIN dbo.Orders o ON o.UserId = u.UserId " +
                      "WHERE u.Status IN (1,2,3) AND o.Title LIKE 'A%' AND u.Score BETWEEN 10 AND 20";

            var result = transpiler.Transpile(sql);

            Assert.That(result.QueryCSharpCode, Does.Contain("public static IExprQuery Build(out TableUsers u, out TableOrders o)"));
            Assert.That(result.QueryCSharpCode, Does.Contain("u = new TableUsers(\"u\");"));
            Assert.That(result.QueryCSharpCode, Does.Contain("o = new TableOrders(\"o\");"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".InnerJoin(o, o.UserId == u.UserId)"));
            Assert.That(result.QueryCSharpCode, Does.Contain("u.Status.In(1, 2, 3)"));
            Assert.That(result.QueryCSharpCode, Does.Contain("Like(o.Title, \"A%\")"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public StringTableColumn Title"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("CreateStringColumn(\"Title\", 255, true)"));

            AssertCompilesAndSql(result, SqlJoinPredicates);
        }

        [Test]
        public void TranspileSelect_SubQueriesInPredicatesAndProjection_AreSupported()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var inSubQuery = transpiler.Transpile(
                "SELECT u.UserId FROM dbo.Users u WHERE u.UserId IN (SELECT o.UserId FROM dbo.Orders o)");
            Assert.That(inSubQuery.QueryCSharpCode, Does.Contain(".In(Select(o.UserId)"));
            AssertCompilesAndSql(inSubQuery, SqlInSubQuery);

            var existsSubQuery = transpiler.Transpile(
                "SELECT u.UserId FROM dbo.Users u WHERE EXISTS (SELECT 1 FROM dbo.Orders o WHERE o.UserId = u.UserId)");
            Assert.That(existsSubQuery.QueryCSharpCode, Does.Contain("Where(Exists(Select"));
            AssertCompilesAndSql(existsSubQuery, SqlExistsSubQuery);

            var scalarSubQuery = transpiler.Transpile(
                "SELECT u.UserId, (SELECT COUNT(*) FROM dbo.Orders o WHERE o.UserId = u.UserId) AS OrderCount FROM dbo.Users u");
            Assert.That(scalarSubQuery.QueryCSharpCode, Does.Contain("ValueQuery(Select"));
            AssertCompilesAndSql(scalarSubQuery, SqlScalarSubQuery);
        }

        [Test]
        public void TranspileSelect_Apply_IsSupported()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var crossApply = transpiler.Transpile(
                "SELECT u.UserId, oa.OrderId " +
                "FROM dbo.Users u " +
                "CROSS APPLY (SELECT o.OrderId FROM dbo.Orders o ORDER BY o.OrderId DESC OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY) oa");
            Assert.That(crossApply.QueryCSharpCode, Does.Contain(".CrossApply("));
            AssertCompilesAndSql(crossApply, SqlCrossApply);

            var outerApply = transpiler.Transpile(
                "SELECT u.UserId, oa.OrderId " +
                "FROM dbo.Users u " +
                "OUTER APPLY (SELECT o.OrderId FROM dbo.Orders o ORDER BY o.OrderId DESC OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY) oa");
            Assert.That(outerApply.QueryCSharpCode, Does.Contain(".OuterApply("));
            AssertCompilesAndSql(outerApply, SqlOuterApply);
        }

        [Test]
        public void TranspileSelect_TableFunctionsInFrom_AreSupported()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var builtInFunction = transpiler.Transpile("SELECT s.value FROM STRING_SPLIT('a,b', ',') s");
            Assert.That(builtInFunction.QueryCSharpCode, Does.Contain("TableFunctionSys(\"STRING_SPLIT\", \"a,b\", \",\")"));
            Assert.That(builtInFunction.QueryCSharpCode, Does.Contain(".As(TableAlias(\"s\"))"));
            AssertCompilesAndSql(builtInFunction, SqlFromBuiltInTableFunction);

            var schemaFunction = transpiler.Transpile("SELECT f.Value FROM dbo.MyFunc(1) f");
            Assert.That(schemaFunction.QueryCSharpCode, Does.Contain("TableFunctionCustom(\"dbo\", \"MyFunc\", 1)"));
            AssertCompilesAndSql(schemaFunction, SqlFromSchemaTableFunction);
        }

        [Test]
        public void TranspileSelect_SqlVariables_AreTranspiledIntoTypedLocals()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var stringVariable = transpiler.Transpile("SELECT u.UserId FROM dbo.Users u WHERE u.Name = @name");
            Assert.That(stringVariable.QueryCSharpCode, Does.Contain("var name = \"\";"));
            Assert.That(stringVariable.QueryCSharpCode, Does.Contain("Where(u.Name == name)"));
            AssertCompilesAndSql(stringVariable, SqlVariableStringCompare);

            var intVariable = transpiler.Transpile("SELECT u.UserId FROM dbo.Users u WHERE u.UserId = @id AND @id > 0");
            Assert.That(intVariable.QueryCSharpCode, Does.Contain("var id = 0;"));
            Assert.That(intVariable.QueryCSharpCode, Does.Contain("Where((u.UserId == id) & (Literal(id) > 0))"));
            AssertCompilesAndSql(intVariable, SqlVariableIntCompare);

            var inVariable = transpiler.Transpile("SELECT u.UserId FROM dbo.Users u WHERE u.Name IN (@names)");
            Assert.That(inVariable.QueryCSharpCode, Does.Contain("var names = new[]"));
            Assert.That(inVariable.QueryCSharpCode, Does.Contain("\"\""));
            Assert.That(inVariable.QueryCSharpCode, Does.Contain("u.Name.In(names)"));
            AssertCompilesAndSql(inVariable, SqlVariableInList);
        }

        [Test]
        public void TranspileSelect_DescriptorAndVariableTyping_SupportsExtendedTypes()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var sql =
                "SELECT u.UserId FROM dbo.Users u " +
                "WHERE CAST(@createdAt AS datetime) = u.CreatedAt " +
                "AND CAST(@isActive AS bit) = u.IsActive " +
                "AND CAST(@amount AS decimal(10,2)) = u.Amount " +
                "AND CAST(@userGuid AS uniqueidentifier) = u.ExternalId " +
                "AND CAST(@payload AS varbinary(max)) = u.Payload " +
                "AND u.ExternalId IN (@guidList) " +
                "AND u.Amount IN (@amountList) " +
                "AND u.CreatedAt IN (@dateList) " +
                "AND u.IsActive IN (@flagList) " +
                "AND u.Payload IN (@payloadList)";

            var result = transpiler.Transpile(sql);

            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public DateTimeTableColumn CreatedAt"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public BooleanTableColumn IsActive"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public DecimalTableColumn Amount"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public GuidTableColumn ExternalId"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public ByteArrayTableColumn Payload"));

            Assert.That(result.QueryCSharpCode, Does.Contain("var createdAt = default(global::System.DateTime);"));
            Assert.That(result.QueryCSharpCode, Does.Contain("var isActive = false;"));
            Assert.That(result.QueryCSharpCode, Does.Contain("var amount = 0M;"));
            Assert.That(result.QueryCSharpCode, Does.Contain("var userGuid = default(global::System.Guid);"));
            Assert.That(result.QueryCSharpCode, Does.Contain("var payload = global::System.Array"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".Empty<byte>();"));
            Assert.That(result.QueryCSharpCode, Does.Contain("var guidList = new[]"));
            Assert.That(result.QueryCSharpCode, Does.Contain("Literal(default(global::System.Guid))"));
            Assert.That(result.QueryCSharpCode, Does.Contain("Literal(0m)"));
            Assert.That(result.QueryCSharpCode, Does.Contain("Literal(default(global::System.DateTime))"));
            Assert.That(result.QueryCSharpCode, Does.Contain("Literal(false)"));
            Assert.That(result.QueryCSharpCode, Does.Contain("Literal(global::System.Array.Empty<byte>())"));

            AssertCompiles(result);
        }

        [Test]
        public void TranspileSelect_CountStar_UsesCountOne()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var result = transpiler.Transpile("SELECT COUNT(*) AS Total FROM dbo.Users");

            Assert.That(result.QueryCSharpCode, Does.Contain("CountOne().As(\"Total\")"));
            AssertCompilesAndSql(result, SqlCountStar);
        }

        [Test]
        public void TranspileSelect_QualifiedStar_UsesAllColumnsExtension()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var sql = "SELECT u.*, o.OrderId FROM dbo.Users u INNER JOIN dbo.Orders o ON o.UserId = u.UserId";
            var result = transpiler.Transpile(sql);

            Assert.That(result.QueryCSharpCode, Does.Contain("Select(u.AllColumns(), o.OrderId)"));
            AssertCompilesAndSql(result, SqlQualifiedStar);
        }

        [Test]
        public void TranspileSelect_FunctionCall_UsesScalarFunctionSys()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var result = transpiler.Transpile("SELECT LEN(u.Name) AS NameLength FROM dbo.Users u");

            Assert.That(result.QueryCSharpCode, Does.Contain("ScalarFunctionSys(\"LEN\", u.Name).As(\"NameLength\")"));
            AssertCompilesAndSql(result, SqlFunction);
        }

        [Test]
        public void TranspileSelect_FunctionCall_SchemaQualified_UsesScalarFunctionCustom()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var result = transpiler.Transpile("SELECT dbo.NormalizeUserName(u.Name) AS NameLength FROM dbo.Users u");

            Assert.That(result.QueryCSharpCode, Does.Contain("ScalarFunctionCustom(\"dbo\", \"NormalizeUserName\", u.Name).As(\"NameLength\")"));
            AssertCompilesAndSql(result, SqlSchemaFunction);
        }

        [Test]
        public void TranspileSelect_FunctionCall_DatabaseQualified_IsRejected()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var ex = Assert.Throws<SqExpressSqlTranspilerException>(
                () => transpiler.Transpile("SELECT Db1.dbo.NormalizeUserName(u.Name) AS NameLength FROM dbo.Users u"));
            Assert.That(ex?.Message, Does.Contain("Database-qualified scalar function calls are not supported yet."));
        }

        [Test]
        public void TranspileSelect_FromValuesDerivedTable_IsSupported()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var sql = "SELECT RawData.Id, RawData.Val FROM (VALUES (1, 'a'), (2, 'b')) AS RawData(Id, Val)";

            var result = transpiler.Transpile(sql);

            Assert.That(result.QueryCSharpCode, Does.Contain("Values("));
            Assert.That(result.QueryCSharpCode, Does.Contain(".As(\"RawData\", \"Id\", \"Val\")"));
            Assert.That(result.QueryCSharpCode, Does.Contain("Select(rawData.Column(\"Id\"), rawData.Column(\"Val\"))"));
            AssertCompiles(result);
        }

        [Test]
        public void TranspileSelect_CaseWhen_SearchedCase_IsSupported()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var result = transpiler.Transpile(
                "SELECT CASE WHEN u.IsActive = 1 THEN 'Y' ELSE 'N' END AS ActiveMark FROM dbo.Users u");

            Assert.That(result.QueryCSharpCode, Does.Contain("Case().When(u.IsActive == 1).Then(\"Y\").Else(\"N\").As(\"ActiveMark\")"));
            AssertCompilesAndSql(result, SqlCaseWhen);
        }

        [Test]
        public void TranspileSelect_CaseWhen_SimpleCase_IsSupported()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var result = transpiler.Transpile(
                "SELECT CASE u.Status WHEN 1 THEN 'A' WHEN 2 THEN 'B' ELSE 'C' END AS StatusName FROM dbo.Users u");

            Assert.That(result.QueryCSharpCode, Does.Contain("Case().When(u.Status == 1).Then(\"A\").When(u.Status == 2).Then(\"B\").Else(\"C\").As(\"StatusName\")"));
            AssertCompilesAndSql(result, SqlCaseSimple);
        }

        [Test]
        public void TranspileSelect_Functions_KnownFunctionsUseNativeSqExpressCalls()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var isNullResult = transpiler.Transpile("SELECT ISNULL(u.Name, 'NA') AS Name2 FROM dbo.Users u");
            Assert.That(isNullResult.QueryCSharpCode, Does.Contain("IsNull(u.Name, \"NA\").As(\"Name2\")"));
            AssertCompilesAndSql(isNullResult, SqlIsNull);

            var coalesceResult = transpiler.Transpile("SELECT COALESCE(u.Name, u.Login, 'NA') AS DisplayName FROM dbo.Users u");
            Assert.That(coalesceResult.QueryCSharpCode, Does.Contain("Coalesce(u.Name, u.Login, \"NA\").As(\"DisplayName\")"));
            AssertCompilesAndSql(coalesceResult, SqlCoalesce);

            var getDateResult = transpiler.Transpile("SELECT GETDATE() AS Now");
            Assert.That(getDateResult.QueryCSharpCode, Does.Contain("GetDate().As(\"Now\")"));
            AssertCompilesAndSql(getDateResult, SqlGetDate);

            var getUtcDateResult = transpiler.Transpile("SELECT GETUTCDATE() AS NowUtc");
            Assert.That(getUtcDateResult.QueryCSharpCode, Does.Contain("GetUtcDate().As(\"NowUtc\")"));
            AssertCompilesAndSql(getUtcDateResult, SqlGetUtcDate);

            var dateAddResult = transpiler.Transpile("SELECT DATEADD(day, 1, u.CreatedAt) AS NextDate FROM dbo.Users u");
            Assert.That(dateAddResult.QueryCSharpCode, Does.Contain("DateAdd(DateAddDatePart.Day, 1, u.CreatedAt).As(\"NextDate\")"));
            AssertCompilesAndSql(dateAddResult, SqlDateAdd);

            var dateDiffResult = transpiler.Transpile("SELECT DATEDIFF(day, u.CreatedAt, u.UpdatedAt) AS Days FROM dbo.Users u");
            Assert.That(dateDiffResult.QueryCSharpCode, Does.Contain("DateDiff(DateDiffDatePart.Day, u.CreatedAt, u.UpdatedAt).As(\"Days\")"));
            AssertCompilesAndSql(dateDiffResult, SqlDateDiff);
        }

        [Test]
        public void TranspileSelect_WindowFunctions_AreSupported()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var rowNumberResult = transpiler.Transpile("SELECT ROW_NUMBER() OVER(ORDER BY u.UserId) AS Rn FROM dbo.Users u");
            Assert.That(rowNumberResult.QueryCSharpCode, Does.Contain("RowNumber().OverOrderBy(Asc(u.UserId)).As(\"Rn\")"));
            AssertCompilesAndSql(rowNumberResult, SqlWindowRowNumber);

            var sumResult = transpiler.Transpile("SELECT SUM(u.Score) OVER(PARTITION BY u.GroupId ORDER BY u.UserId) AS RunningScore FROM dbo.Users u");
            Assert.That(sumResult.QueryCSharpCode, Does.Contain("Sum(u.Score).OverPartitionBy(u.GroupId).OrderBy(Asc(u.UserId)).As(\"RunningScore\")"));
            AssertCompilesAndSql(sumResult, SqlWindowSumPartitionOrder);

            var frameResult = transpiler.Transpile("SELECT SUM(u.Score) OVER(ORDER BY u.UserId ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS RunningScore FROM dbo.Users u");
            Assert.That(frameResult.QueryCSharpCode, Does.Contain("Sum(u.Score).OverOrderBy(Asc(u.UserId)).FrameClause(FrameBorder.UnboundedPreceding, FrameBorder.CurrentRow).As(\"RunningScore\")"));
            AssertCompilesAndSql(frameResult, SqlWindowRowsFrame);

            var firstLastResult = transpiler.Transpile(
                "SELECT FIRST_VALUE(u.FirstName) OVER(ORDER BY u.UserId) AS FirstNameFirst, " +
                "LAST_VALUE(u.FirstName) OVER(ORDER BY u.UserId ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) AS FirstNameLast " +
                "FROM dbo.Users u");
            Assert.That(firstLastResult.QueryCSharpCode, Does.Contain("FirstValue(u.FirstName).OverOrderBy(Asc(u.UserId)).FrameClauseEmpty().As(\"FirstNameFirst\")"));
            Assert.That(firstLastResult.QueryCSharpCode, Does.Contain("LastValue(u.FirstName).OverOrderBy(Asc(u.UserId)).FrameClause(FrameBorder.UnboundedPreceding, FrameBorder.UnboundedFollowing).As(\"FirstNameLast\")"));
            AssertCompilesAndSql(firstLastResult, SqlWindowFirstLastValue);
        }

        [Test]
        public void TranspileSelect_WindowAggregateFrames_UseHelpersForAllKnownAggregates()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var minResult = transpiler.Transpile("SELECT MIN(u.Score) OVER(ORDER BY u.UserId ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS MinScore FROM dbo.Users u");
            Assert.That(minResult.QueryCSharpCode, Does.Contain("Min(u.Score).OverOrderBy(Asc(u.UserId)).FrameClause(FrameBorder.UnboundedPreceding, FrameBorder.CurrentRow).As(\"MinScore\")"));
            AssertCompilesAndSql(minResult, SqlWindowMinFrame);

            var maxResult = transpiler.Transpile("SELECT MAX(u.Score) OVER(ORDER BY u.UserId ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS MaxScore FROM dbo.Users u");
            Assert.That(maxResult.QueryCSharpCode, Does.Contain("Max(u.Score).OverOrderBy(Asc(u.UserId)).FrameClause(FrameBorder.UnboundedPreceding, FrameBorder.CurrentRow).As(\"MaxScore\")"));
            AssertCompilesAndSql(maxResult, SqlWindowMaxFrame);

            var avgResult = transpiler.Transpile("SELECT AVG(u.Score) OVER(ORDER BY u.UserId ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS AvgScore FROM dbo.Users u");
            Assert.That(avgResult.QueryCSharpCode, Does.Contain("Avg(u.Score).OverOrderBy(Asc(u.UserId)).FrameClause(FrameBorder.UnboundedPreceding, FrameBorder.CurrentRow).As(\"AvgScore\")"));
            AssertCompilesAndSql(avgResult, SqlWindowAvgFrame);

            var countResult = transpiler.Transpile("SELECT COUNT(u.Score) OVER(ORDER BY u.UserId ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS CntScore FROM dbo.Users u");
            Assert.That(countResult.QueryCSharpCode, Does.Contain("Count(u.Score).OverOrderBy(Asc(u.UserId)).FrameClause(FrameBorder.UnboundedPreceding, FrameBorder.CurrentRow).As(\"CntScore\")"));
            AssertCompilesAndSql(countResult, SqlWindowCountFrame);
        }

        [Test]
        public void TranspileSelect_WindowFunctions_RangeFrameIsRejected()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var ex = Assert.Throws<SqExpressSqlTranspilerException>(
                () => transpiler.Transpile("SELECT SUM(u.Score) OVER(ORDER BY u.UserId RANGE BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS RunningScore FROM dbo.Users u"));
            Assert.That(ex?.Message, Does.Contain("RANGE window frame is not supported"));
        }

        [Test]
        public void TranspileSelect_WithOptions_ChangesNamespacesAndMethod()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var options = new SqExpressSqlTranspilerOptions
            {
                NamespaceName = "Demo.Generated",
                MethodName = "BuildReport",
                QueryVariableName = "reportQuery"
            };

            var result = transpiler.Transpile("SELECT 1", options);

            Assert.That(result.QueryCSharpCode, Does.Contain("namespace Demo.Generated"));
            Assert.That(result.QueryCSharpCode, Does.Contain("using Demo.Generated.Declarations;"));
            Assert.That(result.QueryCSharpCode, Does.Contain("public static IExprQuery BuildReport()"));
            Assert.That(result.QueryCSharpCode, Does.Contain("var reportQuery = SelectOne()"));
            Assert.That(result.QueryCSharpCode, Does.Contain("\r\n                .Done();"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("namespace Demo.Generated.Declarations"));
            AssertCompilesAndSql(result, SqlSelect1, options);
        }

        [Test]
        public void TranspileSelect_WithStaticSqQueryBuilderDisabled_GeneratesQualifiedCalls()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var options = new SqExpressSqlTranspilerOptions
            {
                UseStaticSqQueryBuilderUsing = false
            };

            var result = transpiler.Transpile("SELECT 1", options);

            Assert.That(result.QueryCSharpCode, Does.Not.Contain("using static SqExpress.SqQueryBuilder;"));
            Assert.That(result.QueryCSharpCode, Does.Not.Contain("using SqQueryBuilder = SqExpress.SqQueryBuilder;"));
            Assert.That(result.QueryCSharpCode, Does.Contain("var query = SqQueryBuilder"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".SelectOne()"));
            AssertCompilesAndSql(result, SqlSelect1, options);
        }

        [Test]
        public void TranspileSelect_DefaultTableDescriptorNaming_UsesPrefixAndNoSuffix()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var result = transpiler.Transpile("SELECT u.UserId FROM dbo.Users u");

            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public sealed class TableUsers : TableBase"));
            Assert.That(result.QueryCSharpCode, Does.Contain("public static IExprQuery Build(out TableUsers u)"));
            Assert.That(result.QueryCSharpCode, Does.Contain("u = new TableUsers(\"u\");"));
            AssertCompilesAndSql(result, SqlUnqualifiedUsersWithDefaultSchema);
        }

        [Test]
        public void TranspileSelect_WithCustomTableDescriptorNaming_AppliesPrefixAndSuffix()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var options = new SqExpressSqlTranspilerOptions
            {
                TableDescriptorClassPrefix = "Tbl",
                TableDescriptorClassSuffix = "Row"
            };

            var result = transpiler.Transpile("SELECT u.UserId FROM dbo.Users u", options);

            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public sealed class TblUsersRow : TableBase"));
            Assert.That(result.QueryCSharpCode, Does.Contain("public static IExprQuery Build(out TblUsersRow u)"));
            Assert.That(result.QueryCSharpCode, Does.Contain("u = new TblUsersRow(\"u\");"));
            AssertCompilesAndSql(result, SqlUnqualifiedUsersWithDefaultSchema, options);
        }

        [Test]
        public void TranspileSelect_UnqualifiedTables_UseDefaultSchema()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var result = transpiler.Transpile("SELECT u.UserId FROM Users u");

            Assert.That(result.DeclarationsCSharpCode, Does.Contain("\"dbo\", \"Users\", alias"));
            AssertCompilesAndSql(result, SqlUnqualifiedUsersWithDefaultSchema);
        }

        [Test]
        public void TranspileSelect_UnqualifiedTables_CanDisableDefaultSchema()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var options = new SqExpressSqlTranspilerOptions
            {
                DefaultSchemaName = string.Empty
            };

            var result = transpiler.Transpile("SELECT u.UserId FROM Users u", options);

            Assert.That(result.DeclarationsCSharpCode, Does.Contain("null, \"Users\", alias"));
            AssertCompilesAndSql(result, SqlUnqualifiedUsersWithoutDefaultSchema, options);
        }

        [Test]
        public void Transpile_Cte_GeneratesCteDeclarationAndQuery()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var sql = "WITH C AS (SELECT 1 AS A) SELECT c.A FROM C c";

            var result = transpiler.Transpile(sql);

            Assert.That(result.QueryCSharpCode, Does.Contain("public sealed class CCte : CteBase"));
            Assert.That(result.QueryCSharpCode, Does.Contain("public override IExprSubQuery CreateQuery()"));
            Assert.That(result.QueryCSharpCode, Does.Contain("return Select(Literal(1).As(\"A\")).Done();"));
            Assert.That(result.QueryCSharpCode, Does.Contain("From(c)"));
            AssertCompilesAndSql(result, SqlCteSimple);
        }

        [Test]
        public void Transpile_Cte_WithSourceTable_GeneratesDependentDescriptors()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var sql = "WITH CU AS (SELECT u.UserId FROM dbo.Users u WHERE u.Name = 'A') SELECT c.UserId FROM CU c";

            var result = transpiler.Transpile(sql);

            Assert.That(result.QueryCSharpCode, Does.Contain("public sealed class CUCte : CteBase"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public sealed class TableUsers : TableBase"));
            Assert.That(result.QueryCSharpCode, Does.Contain("var u = new TableUsers(\"u\");"));
            Assert.That(result.QueryCSharpCode, Does.Contain("Where(u.Name == \"A\")"));
            AssertCompilesAndSql(result, SqlCteWithTable);
        }

        [Test]
        public void Transpile_Cte_AndDerived_SupportSetAndOrderExpressions()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var cteUnion = transpiler.Transpile("WITH C AS (SELECT 1 AS A UNION ALL SELECT 2 AS A) SELECT c.A FROM C c");
            Assert.That(cteUnion.QueryCSharpCode, Does.Contain(".UnionAll("));
            AssertCompilesAndSql(cteUnion, SqlCteUnionAll);

            var derivedSetOrder = transpiler.Transpile(
                "SELECT sq.A FROM (SELECT 1 AS A UNION ALL SELECT 2 AS A) sq");
            Assert.That(derivedSetOrder.QueryCSharpCode, Does.Contain(".UnionAll("));
            AssertCompilesAndSql(derivedSetOrder, SqlDerivedUnion);

            var derivedOrderOffset = transpiler.Transpile(
                "SELECT sq.A FROM (SELECT 1 AS A ORDER BY A OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY) sq");
            Assert.That(derivedOrderOffset.QueryCSharpCode, Does.Contain(".OffsetFetch(0, 1)"));
            AssertCompilesAndSql(derivedOrderOffset, SqlDerivedOrderOffset);
        }

        [Test]
        public void Transpile_GodScenario_RecursiveCte_Functions_AndThreeTierSubQueries_AreSupported()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var sql =
                "WITH R(N) AS (" +
                "SELECT 1 " +
                "UNION ALL " +
                "SELECT r.N + 1 FROM R r WHERE r.N < 3" +
                ") " +
                "SELECT " +
                "u.UserId, " +
                "ISNULL(u.Name, 'NA') AS UserName, " +
                "LEN(u.Name) AS NameLen, " +
                "ROW_NUMBER() OVER(ORDER BY u.UserId) AS Rn, " +
                "(SELECT COUNT(*) FROM (SELECT l2.UserId FROM (SELECT o3.UserId FROM dbo.Orders o3 WHERE o3.Amount > 0) l2) l1) AS NestedCnt, " +
                "(SELECT COUNT(*) FROM R rr WHERE rr.N <= 2) AS DepthCnt " +
                "FROM dbo.Users u " +
                "OUTER APPLY (" +
                "SELECT o.OrderId FROM dbo.Orders o ORDER BY o.OrderId DESC OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY" +
                ") oa " +
                "WHERE u.UserId IN (" +
                "SELECT x.UserId FROM (" +
                "SELECT y.UserId FROM (" +
                "SELECT o2.UserId FROM dbo.Orders o2 WHERE o2.Amount > 0" +
                ") y" +
                ") x" +
                ") " +
                "AND EXISTS (SELECT 1 FROM R rx WHERE rx.N = 1) " +
                "ORDER BY u.UserId OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY";

            var result = transpiler.Transpile(sql);

            Assert.That(result.QueryCSharpCode, Does.Contain("RowNumber().OverOrderBy(Asc(u.UserId)).As(\"Rn\")"));
            Assert.That(result.QueryCSharpCode, Does.Contain("IsNull(u.Name, \"NA\").As(\"UserName\")"));
            Assert.That(result.QueryCSharpCode, Does.Contain("ScalarFunctionSys(\"LEN\", u.Name).As(\"NameLen\")"));
            Assert.That(result.QueryCSharpCode, Does.Contain("Select(\r\n"));
            Assert.That(result.QueryCSharpCode, Does.Contain("\r\n                    u.UserId,"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".Where(\r\n"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".OuterApply("));
            Assert.That(result.QueryCSharpCode, Does.Contain("Exists(Select"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".OffsetFetch(0, 20)"));
            Assert.That(result.QueryCSharpCode, Does.Contain("public sealed class RCte : CteBase"));

            var assembly = AssertCompiles(result, "GeneratedTranspilerGodScenarioTests");
            var query = InvokeGeneratedBuildMethod(assembly, new SqExpressSqlTranspilerOptions());
            var generatedSql = query.ToSql(TSqlExporter.Default);

            Assert.That(generatedSql, Does.Contain("WITH [R]"));
            Assert.That(generatedSql, Does.Contain("UNION ALL"));
            Assert.That(generatedSql, Does.Contain("OUTER APPLY"));
            Assert.That(generatedSql, Does.Contain("ROW_NUMBER()OVER"));
            Assert.That(generatedSql, Does.Contain("EXISTS(SELECT 1 FROM [R] [rx]"));
            Assert.That(generatedSql, Does.Contain("OFFSET 0 ROW FETCH NEXT 20 ROW ONLY"));
        }

        [Test]
        public void Transpile_Declarations_Order_CteAndSubQueries_BeforeTables()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var result = transpiler.Transpile(
                "WITH C AS (SELECT u.UserId FROM dbo.Users u) " +
                "SELECT sq.UserId FROM (SELECT c.UserId FROM C c) sq");

            var idxCte = result.QueryCSharpCode.IndexOf("public sealed class CCte : CteBase", StringComparison.Ordinal);
            var idxSub = result.QueryCSharpCode.IndexOf("public sealed class SqSubQuery : DerivedTableBase", StringComparison.Ordinal);
            var idxTable = result.DeclarationsCSharpCode.IndexOf("public sealed class TableUsers : TableBase", StringComparison.Ordinal);

            Assert.That(idxCte, Is.GreaterThanOrEqualTo(0));
            Assert.That(idxSub, Is.GreaterThanOrEqualTo(0));
            Assert.That(idxTable, Is.GreaterThanOrEqualTo(0));
            Assert.That(idxCte, Is.LessThan(idxSub));
            Assert.That(result.DeclarationsCSharpCode, Does.Not.Contain("class CCte"));
            Assert.That(result.DeclarationsCSharpCode, Does.Not.Contain("class SqSubQuery"));
        }

        [Test]
        public void Transpile_SubQuery_GeneratesDerivedTableCreateQuery()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var sql = "SELECT sq.A FROM (SELECT 1 AS A) sq";

            var result = transpiler.Transpile(sql);

            Assert.That(result.QueryCSharpCode, Does.Contain("public sealed class SqSubQuery : DerivedTableBase"));
            Assert.That(result.QueryCSharpCode, Does.Contain("protected override IExprSubQuery CreateQuery()"));
            Assert.That(result.QueryCSharpCode, Does.Contain("return Select(Literal(1).As(this.A)).Done();"));
            Assert.That(result.QueryCSharpCode, Does.Contain("public static IExprQuery Build(out SqSubQuery sq)"));
            Assert.That(result.QueryCSharpCode, Does.Contain("sq = new SqSubQuery(\"sq\");"));
            AssertCompilesAndSql(result, SqlSubSimple);
        }

        [Test]
        public void Transpile_SubQuery_WithSourceTable_GeneratesDependentDescriptors()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var sql = "SELECT sq.UserId FROM (SELECT u.UserId FROM dbo.Users u WHERE u.Name = 'A') sq";

            var result = transpiler.Transpile(sql);

            Assert.That(result.QueryCSharpCode, Does.Contain("public sealed class SqSubQuery : DerivedTableBase"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public sealed class TableUsers : TableBase"));
            Assert.That(result.QueryCSharpCode, Does.Contain("private readonly TableUsers u = new TableUsers(\"u\");"));
            Assert.That(result.QueryCSharpCode, Does.Contain("Where(u.Name == \"A\")"));
            AssertCompilesAndSql(result, SqlSubWithTable);
        }

        [Test]
        public void Transpile_NestedSubQueries_GeneratesSeparateDerivedClasses()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var sql = "SELECT sq.A FROM (SELECT i.Id AS A FROM (SELECT 1 AS Id) i) sq";

            var result = transpiler.Transpile(sql);

            Assert.That(result.QueryCSharpCode, Does.Contain("public sealed class SqSubQuery : DerivedTableBase"));
            Assert.That(result.QueryCSharpCode, Does.Contain("public sealed class ISubQuery : DerivedTableBase"));
            Assert.That(result.QueryCSharpCode, Does.Contain("var i = new ISubQuery(\"i\");"));
            AssertCompilesAndSql(result, SqlSubNested);
        }

        [Test]
        public void Transpile_SubQuery_UsesSourceTableFieldsForColumns_AndHandlesRename()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var sql =
                "SELECT sq.PatronymicName, sq.FirstName " +
                "FROM (SELECT u.LastName AS PatronymicName, u.FirstName FROM dbo.Users u) sq";

            var result = transpiler.Transpile(sql);

            Assert.That(result.QueryCSharpCode, Does.Contain("private readonly TableUsers u = new TableUsers(\"u\");"));
            Assert.That(result.QueryCSharpCode, Does.Contain("this.FirstName = this.u.FirstName.AddToDerivedTable(this);"));
            Assert.That(result.QueryCSharpCode, Does.Contain("this.PatronymicName = this.u.LastName.WithColumnName(\"PatronymicName\").AddToDerivedTable(this);"));
            Assert.That(result.QueryCSharpCode, Does.Contain("Select(u.LastName.As(this.PatronymicName), u.FirstName)"));
            AssertCompilesAndSql(result, SqlSubRenamedFromSource);
        }

        [Test]
        public void Transpile_StringComparison_InfersNVarCharColumn()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var sql = "SELECT u.Name FROM dbo.Users u WHERE u.Name = 'A'";

            var result = transpiler.Transpile(sql);

            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public StringTableColumn Name"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("CreateStringColumn(\"Name\", 255, true)"));
            Assert.That(result.QueryCSharpCode, Does.Contain("u.Name == \"A\""));
            AssertCompilesAndSql(result, SqlStringComparison);
        }

        [Test]
        public void Transpile_NameHeuristics_AndIsNullNullability_AreApplied()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var sql = "SELECT u.CreatedAt, u.UpdatedUtc, u.IsDeleted, u.Description FROM dbo.Users u WHERE u.Description IS NULL";

            var result = transpiler.Transpile(sql);

            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public DateTimeTableColumn CreatedAt"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public DateTimeTableColumn UpdatedUtc"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public BooleanTableColumn IsDeleted"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public NullableStringTableColumn Description"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("CreateNullableStringColumn(\"Description\", 255, true)"));
            AssertCompilesAndSql(result, SqlHeuristicAndNullable);
        }

        [Test]
        public void Transpile_OperatorAndFunctionUsage_InfersColumnKinds()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var sql = "SELECT ABS(u.V1) AS Total, LEN(u.V2) AS NameLen, DATEADD(day, 1, u.V3) AS NextAt FROM dbo.Users u";

            var result = transpiler.Transpile(sql);

            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public DecimalTableColumn V1"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public StringTableColumn V2"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public DateTimeTableColumn V3"));
            AssertCompilesAndSql(result, SqlOperatorInference);
        }

        [Test]
        public void TranspileSelect_CompiledCode_CanInvokeGeneratedBuildMethod()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var options = new SqExpressSqlTranspilerOptions
            {
                NamespaceName = "Runtime.Generated",
                DeclarationsNamespaceName = "Runtime.Generated.Tables",
                ClassName = "ReportQuery",
                MethodName = "Create"
            };

            var result = transpiler.Transpile(
                "SELECT u.UserId FROM dbo.Users u WHERE u.Name = 'A' ORDER BY u.UserId",
                options);

            AssertCompilesAndSql(result, SqlRuntimeOptions, options, "GeneratedTranspilerRuntimeTests");
        }

        [Test]
        public void TranspileSelect_GroupBy_IsSupported()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var result = transpiler.Transpile("SELECT u.Status, COUNT(*) AS Total FROM dbo.Users u GROUP BY u.Status");

            Assert.That(result.QueryCSharpCode, Does.Contain("CountOne().As(\"Total\")"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".GroupBy(u.Status)"));
            AssertCompilesAndSql(result, SqlGroupByBasic);
        }

        [Test]
        public void TranspileSelect_SetOperations_AreSupported()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var unionResult = transpiler.Transpile("SELECT 1 AS A UNION SELECT 2 AS A");
            Assert.That(unionResult.QueryCSharpCode, Does.Contain(".Union("));
            AssertCompilesAndSql(unionResult, SqlUnion);

            var unionAllResult = transpiler.Transpile("SELECT 1 AS A UNION ALL SELECT 2 AS A");
            Assert.That(unionAllResult.QueryCSharpCode, Does.Contain(".UnionAll("));
            AssertCompilesAndSql(unionAllResult, SqlUnionAll);

            var exceptResult = transpiler.Transpile("SELECT 1 AS A EXCEPT SELECT 2 AS A");
            Assert.That(exceptResult.QueryCSharpCode, Does.Contain(".Except("));
            AssertCompilesAndSql(exceptResult, SqlExcept);

            var intersectResult = transpiler.Transpile("SELECT 1 AS A INTERSECT SELECT 2 AS A");
            Assert.That(intersectResult.QueryCSharpCode, Does.Contain(".Intersect("));
            AssertCompilesAndSql(intersectResult, SqlIntersect);
        }

        [Test]
        public void TranspileSelect_SetOperations_ReuseSingleDescriptorAndPreferStringOnTypeConflict()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var result = transpiler.Transpile(
                "SELECT u1.UserId FROM dbo.Users u1 WHERE u1.UserId = 1 " +
                "UNION ALL " +
                "SELECT u2.UserId FROM dbo.Users u2 WHERE u2.UserId = 'A'");

            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public sealed class TableUsers : TableBase"));
            Assert.That(result.DeclarationsCSharpCode, Does.Not.Contain("TableUsers1"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public StringTableColumn UserId"));
            AssertCompilesAndSql(result, SqlUnionAllSameTableTypeConflict);
        }

        [Test]
        public void TranspileSelect_SetOperations_MixedQualifiedAndUnqualified_UsesSingleQualifiedDescriptor()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var result = transpiler.Transpile(
                "SELECT u.UserId, u.Name AS UserName FROM dbo.Users u WHERE u.IsActive = 1 " +
                "UNION " +
                "SELECT u.UserId, u.Name AS UserName FROM Users u");

            Assert.That(result.DeclarationsCSharpCode, Does.Contain("public sealed class TableUsers : TableBase"));
            Assert.That(result.DeclarationsCSharpCode, Does.Not.Contain("TableUsers1"));
            Assert.That(result.DeclarationsCSharpCode, Does.Contain("\"dbo\", \"Users\", alias"));

            var assembly = AssertCompiles(result, "GeneratedTranspilerMixedSchemaSetOpsTests");
            var query = InvokeGeneratedBuildMethod(assembly, new SqExpressSqlTranspilerOptions());
            var generatedSql = query.ToSql(TSqlExporter.Default);

            Assert.That(generatedSql, Does.Not.Contain("FROM [Users]"));
            Assert.That(generatedSql.Split("FROM [dbo].[Users]", StringSplitOptions.None).Length - 1, Is.EqualTo(2));
        }

        [Test]
        public void TranspileSelect_SetOperations_MixedSchemasAndUnqualified_UsesDefaultSchema()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var result = transpiler.Transpile(
                "SELECT u.UserId FROM dbo.Users u " +
                "UNION " +
                "SELECT u.UserId FROM sales.Users u " +
                "UNION " +
                "SELECT u.UserId FROM Users u");

            var assembly = AssertCompiles(result, "GeneratedTranspilerMixedSchemaDefaultSchemaTests");
            var query = InvokeGeneratedBuildMethod(assembly, new SqExpressSqlTranspilerOptions());
            var generatedSql = query.ToSql(TSqlExporter.Default);

            Assert.That(generatedSql.Contains("FROM [dbo].[Users] [u]", StringComparison.Ordinal), Is.True);
            Assert.That(generatedSql.Contains("FROM [sales].[Users] [u]", StringComparison.Ordinal), Is.True);
        }

        [Test]
        public void TranspileSelect_SetOperations_MixedSchemasAndUnqualified_ThrowsAmbiguousError_WhenDefaultSchemaDisabled()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var options = new SqExpressSqlTranspilerOptions
            {
                DefaultSchemaName = string.Empty
            };

            var ex = Assert.Throws<SqExpressSqlTranspilerException>(() => transpiler.Transpile(
                "SELECT u.UserId FROM dbo.Users u " +
                "UNION " +
                "SELECT u.UserId FROM sales.Users u " +
                "UNION " +
                "SELECT u.UserId FROM Users u",
                options));

            Assert.That(ex?.Message, Does.Contain("Ambiguous unqualified table reference 'Users'"));
        }

        [Test]
        public void TranspileSelect_OffsetFetch_IsSupported()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var offsetFetchResult = transpiler.Transpile("SELECT u.UserId FROM dbo.Users u ORDER BY u.UserId OFFSET 10 ROWS FETCH NEXT 5 ROWS ONLY");
            Assert.That(offsetFetchResult.QueryCSharpCode, Does.Contain(".OffsetFetch(10, 5)"));
            AssertCompilesAndSql(offsetFetchResult, SqlOffsetFetch);

            var offsetOnlyResult = transpiler.Transpile("SELECT u.UserId FROM dbo.Users u ORDER BY u.UserId OFFSET 20 ROWS");
            Assert.That(offsetOnlyResult.QueryCSharpCode, Does.Contain(".Offset(20)"));
            AssertCompilesAndSql(offsetOnlyResult, SqlOffsetOnly);

            var unionOffsetFetchResult = transpiler.Transpile("SELECT 1 AS A UNION ALL SELECT 2 AS A ORDER BY A OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY");
            Assert.That(unionOffsetFetchResult.QueryCSharpCode, Does.Contain(".UnionAll("));
            Assert.That(unionOffsetFetchResult.QueryCSharpCode, Does.Contain(".OffsetFetch(1, 1)"));
            AssertCompilesAndSql(unionOffsetFetchResult, SqlUnionAllOffsetFetch);
        }

        [Test]
        public void TranspileUpdate_IsSupported()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var result = transpiler.Transpile(
                "UPDATE u " +
                "SET u.Name = o.Title, u.IsActive = 1 " +
                "FROM dbo.Users u INNER JOIN dbo.Orders o ON o.UserId = u.UserId " +
                "WHERE o.Title LIKE 'A%'");

            Assert.AreEqual("UPDATE", result.StatementKind);
            Assert.That(result.QueryCSharpCode, Does.Contain("public static IExprExec Build()"));
            Assert.That(result.QueryCSharpCode, Does.Contain("Update(u)"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".Set(u.Name, o.Title)"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".Set(u.IsActive, 1)"));
            AssertCompilesAndSql(result, SqlUpdateWithJoin);
        }

        [Test]
        public void TranspileUpdate_WhereVariableAgainstIdColumn_InfersInt32Variable()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var result = transpiler.Transpile(
                "UPDATE dbo.Users " +
                "SET IsDeleted = 0 " +
                "WHERE UserId = @userId");

            Assert.That(result.QueryCSharpCode, Does.Contain("var userId = 0;"));
            Assert.That(result.QueryCSharpCode, Does.Not.Contain("var userId = \"\";"));
            Assert.That(result.QueryCSharpCode, Does.Contain("Where(users.UserId == userId)"));
            AssertCompiles(result);
        }

        [Test]
        public void TranspileInsert_IsSupported()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var valuesResult = transpiler.Transpile(
                "INSERT INTO dbo.Users(UserId, Name) VALUES (1, 'A'), (2, 'B')");

            Assert.AreEqual("INSERT", valuesResult.StatementKind);
            Assert.That(valuesResult.QueryCSharpCode, Does.Contain("public static IExprExec Build()"));
            Assert.That(valuesResult.QueryCSharpCode, Does.Contain("InsertInto(users, users.UserId, users.Name)"));
            Assert.That(valuesResult.QueryCSharpCode, Does.Contain(".Values(1, \"A\")"));
            Assert.That(valuesResult.QueryCSharpCode, Does.Contain(".Values(2, \"B\")"));
            Assert.That(valuesResult.QueryCSharpCode, Does.Contain(".DoneWithValues()"));
            AssertCompilesAndSql(valuesResult, SqlInsertValues);

            var selectResult = transpiler.Transpile(
                "INSERT INTO dbo.Users(UserId, Name) " +
                "SELECT o.UserId, o.Title FROM dbo.Orders o WHERE o.IsDeleted = 0");

            Assert.That(selectResult.QueryCSharpCode, Does.Contain("InsertInto(users, users.UserId, users.Name)"));
            Assert.That(selectResult.QueryCSharpCode, Does.Contain(".From(Select(o.UserId, o.Title)"));
            AssertCompilesAndSql(selectResult, SqlInsertFromSelect);
        }

        [Test]
        public void TranspileDelete_IsSupported()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var result = transpiler.Transpile(
                "DELETE u " +
                "FROM dbo.Users u INNER JOIN dbo.Orders o ON o.UserId = u.UserId " +
                "WHERE o.IsDeleted = 1");

            Assert.AreEqual("DELETE", result.StatementKind);
            Assert.That(result.QueryCSharpCode, Does.Contain("public static IExprExec Build()"));
            Assert.That(result.QueryCSharpCode, Does.Contain("Delete(u)"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".From(u)"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".InnerJoin(o, o.UserId == u.UserId)"));
            AssertCompilesAndSql(result, SqlDeleteWithJoin);
        }

        [Test]
        public void TranspileMerge_IsSupported()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var result = transpiler.Transpile(
                "MERGE dbo.Users AS t " +
                "USING dbo.UsersStaging AS s " +
                "ON t.UserId = s.UserId " +
                "WHEN MATCHED THEN UPDATE SET t.Name = s.Name, t.IsActive = s.IsActive " +
                "WHEN NOT MATCHED BY TARGET THEN INSERT (UserId, Name, IsActive) VALUES (s.UserId, s.Name, s.IsActive) " +
                "WHEN NOT MATCHED BY SOURCE THEN DELETE;");

            Assert.AreEqual("MERGE", result.StatementKind);
            Assert.That(result.QueryCSharpCode, Does.Contain("public static IExprExec Build()"));
            Assert.That(result.QueryCSharpCode, Does.Contain("MergeInto(users, s)"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".WhenMatched()"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".ThenUpdate()"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".ThenInsert()"));
            Assert.That(result.QueryCSharpCode, Does.Contain("CustomColumnFactory.Any(\"UserId\")"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".WhenNotMatchedBySource()"));
            Assert.That(result.QueryCSharpCode, Does.Contain(".ThenDelete()"));
            AssertCompilesAndSql(result, SqlMergeBasic);
        }

        [Test]
        public void Transpile_RejectsUnsupportedStatement()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var ex = Assert.Throws<SqExpressSqlTranspilerException>(() => transpiler.Transpile("TRUNCATE TABLE dbo.Users"));
            Assert.That(ex?.Message, Does.Contain("Only SELECT, INSERT, UPDATE, DELETE and MERGE statements are supported"));
        }

        [Test]
        public void Transpile_RejectsHaving()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var ex = Assert.Throws<SqExpressSqlTranspilerException>(
                () => transpiler.Transpile("SELECT u.Status, COUNT(*) AS Total FROM dbo.Users u GROUP BY u.Status HAVING COUNT(*) > 1"));
            Assert.That(ex?.Message, Does.Contain("HAVING"));
        }

        [Test]
        public void Transpile_RejectsSelectInto()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var ex = Assert.Throws<SqExpressSqlTranspilerException>(() => transpiler.Transpile("SELECT 1 INTO dbo.ResultTable"));
            Assert.That(ex?.Message, Does.Contain("SELECT INTO"));
        }

        [Test]
        public void Transpile_ReportsParseErrors()
        {
            var transpiler = new SqExpressSqlTranspiler();

            var ex = Assert.Throws<SqExpressSqlTranspilerException>(() => transpiler.Transpile("SELECT FROM"));
            Assert.That(ex?.Message, Does.Contain("Could not parse SQL"));
        }

        [Test]
        public void TranspileSelect_MethodRejectsNonSelect()
        {
            var transpiler = new SqExpressSqlTranspiler();
            var ex = Assert.Throws<SqExpressSqlTranspilerException>(() => transpiler.TranspileSelect("DELETE FROM dbo.Users"));

            Assert.That(ex?.Message, Does.Contain("Expected SELECT statement"));
        }

        private static readonly MetadataReference[] CompilationReferences = BuildCompilationReferences();

        private static Assembly AssertCompiles(SqExpressTranspileResult result, string assemblyName = "GeneratedTranspilerCodeTests")
        {
            var queryTree = CSharpSyntaxTree.ParseText(result.QueryCSharpCode);
            var declarationsTree = CSharpSyntaxTree.ParseText(result.DeclarationsCSharpCode);

            AssertNoSyntaxErrors(queryTree);
            AssertNoSyntaxErrors(declarationsTree);

            var compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { queryTree, declarationsTree },
                CompilationReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);
            if (!emitResult.Success)
            {
                var errors = emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
                Assert.Fail("Generated code does not compile:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
            }

            return Assembly.Load(ms.ToArray());
        }

        private static void AssertCompilesAndSql(
            SqExpressTranspileResult result,
            string expectedSql,
            SqExpressSqlTranspilerOptions? options = null,
            string assemblyName = "GeneratedTranspilerCodeTests")
        {
            var effectiveOptions = options ?? new SqExpressSqlTranspilerOptions();
            var assembly = AssertCompiles(result, assemblyName);
            var expr = InvokeGeneratedBuildMethod(assembly, effectiveOptions);
            var sql = expr.ToSql(TSqlExporter.Default);
            Assert.AreEqual(expectedSql, sql);
        }

        private static IExpr InvokeGeneratedBuildMethod(Assembly generatedAssembly, SqExpressSqlTranspilerOptions options)
        {
            var generatedTypeName = options.NamespaceName + "." + options.ClassName;
            var generatedType = generatedAssembly.GetType(generatedTypeName);
            if (generatedType == null)
            {
                Assert.Fail("Generated type was not found: " + generatedTypeName);
            }

            var generatedMethod = generatedType!.GetMethod(options.MethodName, BindingFlags.Public | BindingFlags.Static);
            if (generatedMethod == null)
            {
                Assert.Fail("Generated method was not found: " + options.MethodName);
            }

            var methodParameters = generatedMethod!.GetParameters();
            var invocationArgs = new object?[methodParameters.Length];
            for (var i = 0; i < methodParameters.Length; i++)
            {
                invocationArgs[i] = null;
            }

            var invocationResult = generatedMethod.Invoke(null, invocationArgs);
            if (invocationResult is not IExpr)
            {
                Assert.Fail("Generated method did not return IExpr.");
            }

            return (IExpr)invocationResult!;
        }

        private static void AssertNoSyntaxErrors(SyntaxTree syntaxTree)
        {
            var diagnostics = syntaxTree.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            if (diagnostics.Count > 0)
            {
                Assert.Fail("Generated code has syntax errors:" + Environment.NewLine + string.Join(Environment.NewLine, diagnostics));
            }
        }

        private static MetadataReference[] BuildCompilationReferences()
        {
            var referencePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedAssemblies)
            {
                foreach (var path in trustedAssemblies.Split(Path.PathSeparator))
                {
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        referencePaths.Add(path);
                    }
                }
            }

            referencePaths.Add(typeof(object).GetTypeInfo().Assembly.Location);
            referencePaths.Add(typeof(SqQueryBuilder).GetTypeInfo().Assembly.Location);
            referencePaths.Add(typeof(SqExpressSqlTranspiler).GetTypeInfo().Assembly.Location);
            referencePaths.Add(typeof(TSqlExporter).GetTypeInfo().Assembly.Location);

            return referencePaths.Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        }
    }
}
