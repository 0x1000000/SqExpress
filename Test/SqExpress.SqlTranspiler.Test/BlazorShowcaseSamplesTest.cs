using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using SqExpress.SqlExport;
using SqExpress.SqlTranspiler;
using SqExpress.Syntax;

namespace SqExpress.SqlTranspiler.Test
{
    [TestFixture]
    public class BlazorShowcaseSamplesTest
    {
        private static readonly IReadOnlyDictionary<string, string> ExpectedSqlById =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ranked-revenue"] = "WITH [revenue_by_customer] AS(SELECT [o].[CustomerId],SUM([o].[TotalAmount]) [Revenue] FROM [dbo].[Orders] [o] WHERE [o].[OrderDate]>='0001-01-01' GROUP BY [o].[CustomerId]),[ranked_customers] AS(SELECT [c].[CustomerId],[c].[CustomerName],[r].[Revenue],ROW_NUMBER()OVER(ORDER BY [r].[Revenue] DESC) [RevenueRank] FROM [revenue_by_customer] [r] JOIN [dbo].[Customers] [c] ON [c].[CustomerId]=[r].[CustomerId])SELECT [rc].[CustomerId],[rc].[CustomerName],[rc].[Revenue],[rc].[RevenueRank] FROM [ranked_customers] [rc] WHERE [rc].[RevenueRank]<=10 ORDER BY [rc].[RevenueRank]",
                ["list-filter-cte"] = "WITH [base_users] AS(SELECT [u].[UserId],[u].[Name],[u].[TeamId] FROM [dbo].[Users] [u] WHERE [u].[UserId] IN(0))SELECT [bu].[UserId],[bu].[Name],[t].[TeamName] FROM [base_users] [bu] LEFT JOIN [dbo].[Teams] [t] ON [t].[TeamId]=[bu].[TeamId] ORDER BY [bu].[Name]",
                ["regional-window"] = "SELECT [q].[RegionId],[q].[RegionName],[q].[ActiveUsers],[q].[RegionalTotal] FROM (SELECT [r].[RegionId],[r].[RegionName],COUNT([u].[UserId]) [ActiveUsers],SUM(COUNT([u].[UserId]))OVER() [RegionalTotal] FROM [dbo].[Regions] [r] LEFT JOIN [dbo].[Users] [u] ON [u].[RegionId]=[r].[RegionId] WHERE [u].[IsActive]=1 GROUP BY [r].[RegionId],[r].[RegionName])[q] WHERE [q].[ActiveUsers]>0 ORDER BY [q].[ActiveUsers] DESC",
                ["cross-apply-latest-order"] = "SELECT [c].[CustomerId],[c].[CustomerName],[lastOrder].[OrderId],[lastOrder].[OrderDate],[lastOrder].[TotalAmount] FROM [dbo].[Customers] [c] CROSS APPLY (SELECT [o].[OrderId],[o].[OrderDate],[o].[TotalAmount] FROM [dbo].[Orders] [o] WHERE [o].[CustomerId]=[c].[CustomerId] ORDER BY [o].[OrderDate] DESC OFFSET 0 ROW FETCH NEXT 1 ROW ONLY)[lastOrder]",
                ["outer-apply-open-ticket"] = "SELECT [u].[UserId],[u].[Name],[nextTicket].[TicketId],[nextTicket].[Priority] FROM [dbo].[Users] [u] OUTER APPLY (SELECT [t].[TicketId],[t].[Priority] FROM [dbo].[Tickets] [t] WHERE [t].[AssignedUserId]=[u].[UserId] AND [t].[Status]='OPEN' ORDER BY [t].[Priority] DESC,[t].[CreatedAt] OFFSET 0 ROW FETCH NEXT 1 ROW ONLY)[nextTicket] WHERE [u].[IsActive]=1",
                ["paged-count-over"] = "SELECT [o].[OrderId],[o].[CustomerId],[o].[TotalAmount],COUNT(1)OVER() [TotalRows] FROM [dbo].[Orders] [o] WHERE [o].[OrderDate]>='0001-01-01' ORDER BY [o].[OrderDate] DESC OFFSET 0 ROW FETCH NEXT 0 ROW ONLY",
                ["update-from-derived"] = "UPDATE [u] SET [u].[LastOrderDate]=[src].[LastOrderDate],[u].[IsVip]=[src].[IsVip] FROM [dbo].[Users] [u] JOIN (SELECT [o].[UserId],MAX([o].[OrderDate]) [LastOrderDate],CAST(1 AS bit) [IsVip] FROM [dbo].[Orders] [o] WHERE [o].[TotalAmount]>=0 GROUP BY [o].[UserId])[src] ON [src].[UserId]=[u].[UserId]",
                ["delete-from-join"] = "DELETE [u] FROM [dbo].[Users] [u] JOIN (SELECT [lf].[UserId] FROM [dbo].[LoginFailures] [lf] WHERE [lf].[AttemptedAt]<'0001-01-01' GROUP BY [lf].[UserId])[stale] ON [stale].[UserId]=[u].[UserId] WHERE [u].[IsLocked]=1",
                ["insert-select-audit"] = "INSERT INTO [dbo].[UserAudit]([UserId],[AuditType],[CreatedAt]) SELECT [u].[UserId],'USER_EXPORT',GETUTCDATE() FROM [dbo].[Users] [u] WHERE [u].[IsActive]=1 AND [u].[UserId] IN(0)",
                ["insert-snapshot-cte"] = "WITH [backlog] AS(SELECT [t].[TicketId],[t].[AssignedUserId],ROW_NUMBER()OVER(PARTITION BY [t].[AssignedUserId] ORDER BY [t].[CreatedAt]) [QueuePosition] FROM [dbo].[Tickets] [t] WHERE [t].[Status]='OPEN')INSERT INTO [dbo].[TicketSnapshots]([TicketId],[AssignedUserId],[QueuePosition],[SnapshotAt]) SELECT [b].[TicketId],[b].[AssignedUserId],[b].[QueuePosition],GETUTCDATE() FROM [backlog] [b] WHERE [b].[QueuePosition]<=10",
                ["merge-select-source"] = "MERGE [dbo].[UserScores] [A0] USING (SELECT [u].[UserId],SUM([s].[Points]) [TotalPoints],MAX([s].[UpdatedAt]) [LastUpdatedAt] FROM [dbo].[Users] [u] JOIN [dbo].[ScoreEvents] [s] ON [s].[UserId]=[u].[UserId] WHERE [u].[UserId] IN(0) GROUP BY [u].[UserId])[src] ON [A0].[UserId]=[src].[UserId] WHEN MATCHED THEN UPDATE SET [A0].[TotalPoints]=[src].[TotalPoints],[A0].[LastUpdatedAt]=[src].[LastUpdatedAt] WHEN NOT MATCHED THEN INSERT([UserId],[TotalPoints],[LastUpdatedAt]) VALUES([src].[UserId],[src].[TotalPoints],[src].[LastUpdatedAt]) WHEN NOT MATCHED BY SOURCE THEN  DELETE;",
                ["merge-values-source"] = "MERGE [dbo].[FeatureFlags] [A0] USING (VALUES (1,'BetaDashboard',1),(2,'SmartSearch',0),(3,'OpsMode',1))[src]([FlagId],[FlagName],[IsEnabled]) ON [A0].[FlagId]=[src].[FlagId] WHEN MATCHED THEN UPDATE SET [A0].[FlagName]=[src].[FlagName],[A0].[IsEnabled]=[src].[IsEnabled] WHEN NOT MATCHED THEN INSERT([FlagId],[FlagName],[IsEnabled]) VALUES([src].[FlagId],[src].[FlagName],[src].[IsEnabled]);",
                ["exists-recent-orders"] = "SELECT [u].[UserId],[u].[Name] FROM [dbo].[Users] [u] WHERE EXISTS(SELECT 1 FROM [dbo].[Orders] [o] WHERE [o].[UserId]=[u].[UserId] AND [o].[OrderDate]>='0001-01-01') ORDER BY [u].[Name]",
                ["delete-derived-users"] = "DELETE [t] FROM [dbo].[Users] [t] JOIN (SELECT [uoSource].[UserId] FROM [dbo].[UserOrders] [uoSource] GROUP BY [uoSource].[UserId])[uo] ON [t].[UserId]=[uo].[UserId]",
                ["having-gross-sales"] = "SELECT [gross].[CustomerId],[gross].[CustomerName],[gross].[OrderCount],[gross].[GrossAmount] FROM (SELECT [c].[CustomerId],[c].[CustomerName],COUNT([o].[OrderId]) [OrderCount],SUM([o].[TotalAmount]) [GrossAmount] FROM [dbo].[Customers] [c] JOIN [dbo].[Orders] [o] ON [o].[CustomerId]=[c].[CustomerId] LEFT JOIN [dbo].[Payments] [p] ON [p].[OrderId]=[o].[OrderId] WHERE [o].[OrderDate]>='0001-01-01' AND [o].[OrderDate]<='0001-01-01' GROUP BY [c].[CustomerId],[c].[CustomerName])[gross] WHERE [gross].[GrossAmount]>0 ORDER BY [gross].[GrossAmount] DESC",
                ["string-search-functions"] = "SELECT [u].[UserId],[u].[Name],LEN([u].[Name]) [NameLength],UPPER([u].[Email]) [NormalizedEmail] FROM [dbo].[Users] [u] WHERE [u].[Name] LIKE '' AND CHARINDEX('',[u].[Email])>0",
                ["datepart-rollup"] = "WITH [order_calendar] AS(SELECT YEAR([o].[OrderDate]) [OrderYear],MONTH([o].[OrderDate]) [OrderMonth] FROM [dbo].[Orders] [o])SELECT [oc].[OrderYear],[oc].[OrderMonth],COUNT(1) [OrdersCount] FROM [order_calendar] [oc] GROUP BY [oc].[OrderYear],[oc].[OrderMonth] ORDER BY [oc].[OrderYear] DESC,[oc].[OrderMonth] DESC",
                ["dense-rank-team"] = "SELECT [u].[UserId],[u].[TeamId],[u].[Name],[u].[Score],DENSE_RANK()OVER(PARTITION BY [u].[TeamId] ORDER BY [u].[Score] DESC) [TeamRank] FROM [dbo].[Users] [u] WHERE [u].[TeamId] IN(0) ORDER BY [u].[TeamId],[u].[Score] DESC,[u].[Name]",
                ["multi-cte-balance"] = "WITH [tx] AS(SELECT [t].[AccountId],[t].[Amount],[t].[PostedAt] FROM [dbo].[AccountTransactions] [t] WHERE [t].[PostedAt]>='0001-01-01'),[balance] AS(SELECT [balanceSource].[AccountId],SUM([balanceSource].[Amount]) [Balance] FROM [tx] [balanceSource] GROUP BY [balanceSource].[AccountId])SELECT [a].[AccountId],[a].[AccountName],[b].[Balance] FROM [dbo].[Accounts] [a] JOIN [balance] [b] ON [b].[AccountId]=[a].[AccountId] WHERE [b].[Balance]!=0 ORDER BY [b].[Balance] DESC",
                ["windowed-order-share"] = "WITH [revenue_by_customer] AS(SELECT [c].[CustomerId],[c].[CustomerName],SUM([o].[TotalAmount]) [Revenue] FROM [dbo].[Customers] [c] JOIN [dbo].[Orders] [o] ON [o].[CustomerId]=[c].[CustomerId] WHERE [o].[OrderDate]>='0001-01-01' GROUP BY [c].[CustomerId],[c].[CustomerName])SELECT [r].[CustomerId],[r].[CustomerName],[r].[Revenue],SUM([r].[Revenue])OVER() [TotalRevenue],SUM([r].[Revenue])OVER()-[r].[Revenue] [RemainingRevenue] FROM [revenue_by_customer] [r] ORDER BY [r].[Revenue] DESC"
            };

        [Test]
        public void ShowcaseCatalog_ContainsTwentySamples()
        {
            Assert.That(GetShowcaseSamples().Count, Is.EqualTo(20));
        }

        [Test]
        public void ShowcaseCatalog_AllSamplesHaveExpectedSqlCoverage()
        {
            var sampleIds = GetShowcaseSamples().Select(s => s.Id).OrderBy(i => i).ToArray();
            var expectedIds = ExpectedSqlById.Keys.OrderBy(i => i).ToArray();

            Assert.That(expectedIds, Is.EqualTo(sampleIds));
        }

        [TestCaseSource(nameof(GetShowcaseSampleCases))]
        public void ShowcaseSample_Transpiles_Compiles_AndExportsExpectedSql(ShowcaseExpectation expectation)
        {
            var transpiler = new SqExpressSqlTranspiler();

            var result = transpiler.Transpile(expectation.SqlText);

            Assert.That(result.QueryCSharpCode, Is.Not.Empty, expectation.Title);
            Assert.That(result.DeclarationsCSharpCode, Is.Not.Empty, expectation.Title);
            Assert.That(result.CanonicalSql, Is.EqualTo(expectation.ExpectedSql), expectation.Title);

            var generatedSql = CompileAndBuild(result, "GeneratedShowcase_" + SanitizeName(expectation.Id))
                .ToSql(TSqlExporter.Default);

            Assert.That(generatedSql, Is.EqualTo(expectation.ExpectedSql), expectation.Title);
        }

        private static IReadOnlyList<ShowcaseSampleProxy> GetShowcaseSamples()
        {
            var assembly = Assembly.Load("SqExpress.SqlTranspiler.Blazor");
            var catalogType = assembly.GetType("SqExpress.SqlTranspiler.Blazor.Pages.ShowcaseCatalog", throwOnError: true)!;
            var allField = catalogType.GetField("All", BindingFlags.Public | BindingFlags.Static)!;
            var samples = (IEnumerable)allField.GetValue(null)!;

            return samples
                .Cast<object>()
                .Select(sample =>
                {
                    var sampleType = sample.GetType();
                    return new ShowcaseSampleProxy(
                        (string)sampleType.GetProperty("Id")!.GetValue(sample)!,
                        (string)sampleType.GetProperty("Title")!.GetValue(sample)!,
                        (string)sampleType.GetProperty("SqlText")!.GetValue(sample)!);
                })
                .ToList();
        }

        private static IEnumerable<TestCaseData> GetShowcaseSampleCases()
        {
            return GetShowcaseSamples()
                .Select(sample => new ShowcaseExpectation(
                    sample.Id,
                    sample.Title,
                    sample.SqlText,
                    ExpectedSqlById[sample.Id]))
                .Select(i => new TestCaseData(i).SetName(i.Title));
        }

        private static IExpr CompileAndBuild(SqExpressTranspileResult result, string assemblyName)
        {
            var options = new SqExpressSqlTranspilerOptions();
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

            var assembly = Assembly.Load(ms.ToArray());
            var generatedTypeName = options.NamespaceName + "." + options.ClassName;
            var generatedType = assembly.GetType(generatedTypeName);
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
            if (invocationResult is IExpr expr)
            {
                return expr;
            }

            Assert.Fail("Generated method did not return IExpr.");
            return null!;
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

        private static string SanitizeName(string value)
            => value.Replace("-", "_", StringComparison.Ordinal);

        private static readonly MetadataReference[] CompilationReferences = BuildCompilationReferences();

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

        public sealed class ShowcaseSampleProxy
        {
            public ShowcaseSampleProxy(string id, string title, string sqlText)
            {
                this.Id = id;
                this.Title = title;
                this.SqlText = sqlText;
            }

            public string Id { get; }

            public string Title { get; }

            public string SqlText { get; }
        }

        public sealed class ShowcaseExpectation
        {
            public ShowcaseExpectation(string id, string title, string sqlText, string expectedSql)
            {
                this.Id = id;
                this.Title = title;
                this.SqlText = sqlText;
                this.ExpectedSql = expectedSql;
            }

            public string Id { get; }

            public string Title { get; }

            public string SqlText { get; }

            public string ExpectedSql { get; }
        }
    }
}
