using System.Linq;
using NUnit.Framework;
using SqExpress.SqlExport;
using SqExpress.SqlParser;
using SqExpress.Syntax;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;

namespace SqExpress.Test.SqlParser
{
    public class TSqlParserPortableFunctionTest
    {
        [Test]
        public void ParsePortableStringFunctions_MapsToPortableNodes_AndExportsToPgSql()
        {
            const string sql =
                @"SELECT LEN([u].[Name]) [NameLen],DATALENGTH([u].[Name]) [NameBytes],CHARINDEX('bc',[u].[Name]) [Idx],LEFT([u].[Name],3) [L],RIGHT([u].[Name],2) [R],REPLICATE('ab',3) [Rep] FROM [dbo].[Users] [u]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);

            var portable = expr!.SyntaxTree()
                .DescendantsAndSelf()
                .OfType<ExprPortableScalarFunction>()
                .Select(i => i.PortableFunction)
                .ToArray();

            Assert.That(portable, Is.EqualTo(new[]
            {
                PortableScalarFunction.Len,
                PortableScalarFunction.DataLen,
                PortableScalarFunction.IndexOf,
                PortableScalarFunction.Left,
                PortableScalarFunction.Right,
                PortableScalarFunction.Repeat
            }));

            var pgSql = PgSqlExporter.Default.ToSql(expr!);
            Assert.That(pgSql, Is.EqualTo(
                @"SELECT CHAR_LENGTH(""u"".""Name"") ""NameLen"",OCTET_LENGTH(""u"".""Name"") ""NameBytes"",STRPOS(""u"".""Name"",'bc') ""Idx"",LEFT(""u"".""Name"",3) ""L"",RIGHT(""u"".""Name"",2) ""R"",REPEAT('ab',3) ""Rep"" FROM ""dbo"".""Users"" ""u"""));
        }

        [Test]
        public void ParsePromotedPortableScalarFunctions_MapsToPortableNodes_AndExportsToPgSql()
        {
            const string sql =
                @"SELECT NULLIF([u].[Name],'x') [NullIfV],ABS([u].[Amount]) [AbsV],LOWER([u].[Name]) [LowerV],UPPER([u].[Name]) [UpperV],TRIM([u].[Name]) [TrimV],LTRIM([u].[Name]) [LTrimV],RTRIM([u].[Name]) [RTrimV],REPLACE([u].[Name],'a','b') [ReplaceV],SUBSTRING([u].[Name],2,3) [SubstringV],ROUND([u].[Amount],2) [RoundV],FLOOR([u].[Amount]) [FloorV],CEILING([u].[Amount]) [CeilingV] FROM [dbo].[Users] [u]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);

            var portable = expr!.SyntaxTree()
                .DescendantsAndSelf()
                .OfType<ExprPortableScalarFunction>()
                .Select(i => i.PortableFunction)
                .ToArray();

            Assert.That(portable, Is.EqualTo(new[]
            {
                PortableScalarFunction.NullIf,
                PortableScalarFunction.Abs,
                PortableScalarFunction.Lower,
                PortableScalarFunction.Upper,
                PortableScalarFunction.Trim,
                PortableScalarFunction.LTrim,
                PortableScalarFunction.RTrim,
                PortableScalarFunction.Replace,
                PortableScalarFunction.Substring,
                PortableScalarFunction.Round,
                PortableScalarFunction.Floor,
                PortableScalarFunction.Ceiling
            }));

            var pgSql = PgSqlExporter.Default.ToSql(expr!);
            Assert.That(pgSql, Is.EqualTo(
                @"SELECT NULLIF(""u"".""Name"",'x') ""NullIfV"",ABS(""u"".""Amount"") ""AbsV"",LOWER(""u"".""Name"") ""LowerV"",UPPER(""u"".""Name"") ""UpperV"",TRIM(""u"".""Name"") ""TrimV"",LTRIM(""u"".""Name"") ""LTrimV"",RTRIM(""u"".""Name"") ""RTrimV"",REPLACE(""u"".""Name"",'a','b') ""ReplaceV"",SUBSTRING(""u"".""Name"",2,3) ""SubstringV"",ROUND(""u"".""Amount"",2) ""RoundV"",FLOOR(""u"".""Amount"") ""FloorV"",CEIL(""u"".""Amount"") ""CeilingV"" FROM ""dbo"".""Users"" ""u"""));
        }

        [Test]
        public void ParsePortableDatePartFunctions_MapsToPortableNodes_AndExportsToPgSql()
        {
            const string sql =
                @"SELECT YEAR([u].[CreatedAt]) [Y],MONTH([u].[CreatedAt]) [M],DAY([u].[CreatedAt]) [D] FROM [dbo].[Users] [u]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);

            var portable = expr!.SyntaxTree()
                .DescendantsAndSelf()
                .OfType<ExprPortableScalarFunction>()
                .Select(i => i.PortableFunction)
                .ToArray();

            Assert.That(portable, Is.EqualTo(new[]
            {
                PortableScalarFunction.Year,
                PortableScalarFunction.Month,
                PortableScalarFunction.Day
            }));

            var pgSql = PgSqlExporter.Default.ToSql(expr!);
            Assert.That(pgSql, Is.EqualTo(
                @"SELECT EXTRACT(YEAR FROM ""u"".""CreatedAt"") ""Y"",EXTRACT(MONTH FROM ""u"".""CreatedAt"") ""M"",EXTRACT(DAY FROM ""u"".""CreatedAt"") ""D"" FROM ""dbo"".""Users"" ""u"""));
        }

        [Test]
        public void ParseKnownDateFunctions_MapsToKnownNodes_AndExportsToPgSql()
        {
            const string sql =
                @"SELECT GETDATE() [Now],GETUTCDATE() [NowUtc],DATEADD(DAY,1,[u].[CreatedAt]) [Next],DATEDIFF(DAY,[u].[CreatedAt],[u].[UpdatedAt]) [Days] FROM [dbo].[Users] [u]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);

            var descendants = expr!.SyntaxTree().DescendantsAndSelf().ToList();
            Assert.That(descendants.OfType<ExprGetDate>().Count(), Is.EqualTo(1));
            Assert.That(descendants.OfType<ExprGetUtcDate>().Count(), Is.EqualTo(1));
            Assert.That(descendants.OfType<ExprDateAdd>().Count(), Is.EqualTo(1));
            Assert.That(descendants.OfType<ExprDateDiff>().Count(), Is.EqualTo(1));

            var pgSql = PgSqlExporter.Default.ToSql(expr!);
            Assert.That(pgSql, Is.EqualTo(
                @"SELECT now() ""Now"",now() at time zone 'utc' ""NowUtc"",""u"".""CreatedAt""+INTERVAL'1d' ""Next"",CAST(DATE_PART('DAY',DATE_TRUNC('DAY',""u"".""UpdatedAt"")-DATE_TRUNC('DAY',""u"".""CreatedAt"")) AS int4) ""Days"" FROM ""dbo"".""Users"" ""u"""));
        }

        [Test]
        public void ParseKnownDateFunctions_InMultiTableScope_MapsToKnownNodes()
        {
            const string sql =
                @"SELECT DATEADD(MONTH,-1,GETDATE()) [Cutoff],DATEDIFF(DAY,[u].[CreatedAt],[o].[CreatedAt]) [DaysBetween] FROM [dbo].[Users] [u] INNER JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] WHERE [o].[CreatedAt]>=DATEADD(DAY,-7,GETUTCDATE())";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);

            var descendants = expr!.SyntaxTree().DescendantsAndSelf().ToList();
            Assert.That(descendants.OfType<ExprDateAdd>().Count(), Is.EqualTo(2));
            Assert.That(descendants.OfType<ExprDateDiff>().Count(), Is.EqualTo(1));
            Assert.That(descendants.OfType<ExprGetDate>().Count(), Is.EqualTo(1));
            Assert.That(descendants.OfType<ExprGetUtcDate>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void ParseCurrentTimestamp_InMultiTableScope_MapsToKnownNode()
        {
            const string sql =
                @"SELECT e.EmployeeCode,e.FirstName,e.LastName,SUM(s.TotalAmount) [TotalSalesAmount] FROM [ref].[Employee] [e] INNER JOIN [ops].[SalesOrder] [s] ON [e].[EmployeeId]=[s].[SalesRepId] WHERE [s].[OrderDate]>=DATEADD(YEAR,-1,CURRENT_TIMESTAMP) GROUP BY [e].[EmployeeCode],[e].[FirstName],[e].[LastName] ORDER BY [TotalSalesAmount] DESC";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);

            var descendants = expr!.SyntaxTree().DescendantsAndSelf().ToList();
            Assert.That(descendants.OfType<ExprDateAdd>().Count(), Is.EqualTo(1));
            Assert.That(descendants.OfType<ExprGetDate>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void ParseGroupByFunctionCall_MapsSuccessfully()
        {
            const string sql =
                @"SELECT YEAR([o].[CreatedAt]) [Y],COUNT(*) [Cnt] FROM [dbo].[Orders] [o] GROUP BY YEAR([o].[CreatedAt])";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
        }

        [Test]
        public void ParseGroupByFunctionCall_WhenSelectExpressionDiffers_FailsValidation()
        {
            const string sql =
                @"SELECT [o].[CreatedAt],COUNT(*) [Cnt] FROM [dbo].[Orders] [o] GROUP BY YEAR([o].[CreatedAt])";

            var ok = SqTSqlParser.TryParse(sql, out _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("neither grouped nor aggregated"));
        }

        [Test]
        public void ParseGroupedConcatenationOfGroupedColumns_MapsSuccessfully()
        {
            const string sql =
                @"SELECT [e].[FirstName]+' '+[e].[LastName] [SalesRepName],[e].[EmployeeCode],SUM([sol].[Quantity]*[sol].[UnitPrice]) [TotalSales] FROM [ops].[SalesOrderLine] [sol] INNER JOIN [ops].[SalesOrder] [so] ON [sol].[SalesOrderId]=[so].[SalesOrderId] INNER JOIN [ref].[Employee] [e] ON [so].[SalesRepId]=[e].[EmployeeId] WHERE [so].[OrderDate]>=DATEFROMPARTS(YEAR(GETDATE()),1,1) GROUP BY [e].[FirstName],[e].[LastName],[e].[EmployeeCode] ORDER BY [TotalSales] DESC";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
        }

        [Test]
        public void ParseOrderByAggregateExpressionInGroupedQuery_MapsSuccessfully()
        {
            const string sql =
                @"SELECT TOP 5 [p].[ProductName],SUM([ol].[Quantity]) [TotalSold] FROM [ref].[Product] [p] INNER JOIN [ops].[SalesOrderLine] [ol] ON [p].[ProductId]=[ol].[ProductId] INNER JOIN [ops].[SalesOrder] [so] ON [so].[SalesOrderId]=[ol].[SalesOrderId] WHERE [so].[OrderDate] BETWEEN DATEADD(YEAR,-1,GETDATE()) AND GETDATE() GROUP BY [p].[ProductName] ORDER BY SUM([ol].[Quantity]) DESC";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
        }

        [Test]
        public void ParseGroupedFormatFunctionAndOrderByAlias_MapsSuccessfully()
        {
            const string sql =
                @"SELECT FORMAT([ops].[Invoice].[InvoiceDate],'yyyy-MM') [Month],COUNT(*) [InvoiceCount] FROM [ops].[Invoice] WHERE [InvoiceDate]>=DATEADD(YEAR,-1,CAST(GETDATE() AS date)) GROUP BY FORMAT([ops].[Invoice].[InvoiceDate],'yyyy-MM') ORDER BY [Month]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
        }

        [Test]
        public void ParseGroupedFormatFunctionAndOrderByAlias_UnbracketedMultipartColumn_MapsSuccessfully()
        {
            const string sql =
                """
                SELECT
                    FORMAT(ops.Invoice.InvoiceDate, 'yyyy-MM') AS Month,
                    COUNT(*) AS InvoiceCount
                FROM [ops].[Invoice]
                WHERE InvoiceDate >= DATEADD(YEAR, -1, CAST(GETDATE() AS date))
                GROUP BY FORMAT(ops.Invoice.InvoiceDate, 'yyyy-MM')
                ORDER BY Month
                """;

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
        }

        [Test]
        public void ParseKnownNullFunctions_MapsToKnownNodes_AndExportsToPgSql()
        {
            const string sql =
                @"SELECT ISNULL([u].[Name],'NA') [Name2],COALESCE([u].[Name],[u].[Login],'NA') [DisplayName] FROM [dbo].[Users] [u]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);

            var descendants = expr!.SyntaxTree().DescendantsAndSelf().ToList();
            Assert.That(descendants.OfType<ExprFuncIsNull>().Count(), Is.EqualTo(1));
            Assert.That(descendants.OfType<ExprFuncCoalesce>().Count(), Is.EqualTo(1));

            var pgSql = PgSqlExporter.Default.ToSql(expr!);
            Assert.That(pgSql, Is.EqualTo(
                @"SELECT COALESCE(""u"".""Name"",'NA') ""Name2"",COALESCE(""u"".""Name"",""u"".""Login"",'NA') ""DisplayName"" FROM ""dbo"".""Users"" ""u"""));
        }

        [TestCase(@"SELECT AA() [V] FROM [dbo].[Users] [u]", "AA", 0)]
        [TestCase(@"SELECT AA([u].[Id]) [V] FROM [dbo].[Users] [u]", "AA", 1)]
        [TestCase(@"SELECT AA([u].[Id],[u].[Name]) [V] FROM [dbo].[Users] [u]", "AA", 2)]
        [TestCase(@"SELECT BB([u].[Id]+1) [V] FROM [dbo].[Users] [u]", "BB", 1)]
        [TestCase(@"SELECT BB(([u].[Id]+1)%2) [V] FROM [dbo].[Users] [u]", "BB", 1)]
        [TestCase(@"SELECT BB(AA([u].[Id])) [V] FROM [dbo].[Users] [u]", "BB", 1)]
        [TestCase(@"SELECT AA([u].[Id],BB([u].[Name]),([u].[Id]+2)*3) [V] FROM [dbo].[Users] [u]", "AA", 3)]
        [TestCase(@"SELECT [dbo].[Something]([u].[Id]) [V] FROM [dbo].[Users] [u]", "Something", 1)]
        [TestCase(@"SELECT [dbo].[Something]([u].[Id],[u].[Name]+RIGHT([u].[Name],1)) [V] FROM [dbo].[Users] [u]", "Something", 2)]
        [TestCase(@"SELECT [db1].[dbo].[Something]([u].[Id],([u].[Id]%2),AA([u].[Name])) [V] FROM [dbo].[Users] [u]", "Something", 3)]
        public void ParseUnknownScalarFunctions_MapToExprScalarFunction(string sql, string expectedName, int expectedArgCount)
        {
            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);

            var functions = expr!.SyntaxTree()
                .DescendantsAndSelf()
                .OfType<ExprScalarFunction>()
                .Where(i => string.Equals(i.Name.Name, expectedName, System.StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.That(functions.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(functions[0].Arguments?.Count ?? 0, Is.EqualTo(expectedArgCount));
        }

        [Test]
        public void ParseIifFunction_MapsSuccessfully()
        {
            const string sql = @"SELECT IIF(1<2, 'A', 'B')";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
            Assert.That(TSqlExporter.Default.ToSql(expr!), Is.EqualTo("SELECT CASE WHEN 1<2 THEN 'A' ELSE 'B' END"));
        }

        [Test]
        public void ParseNotLikePredicate_MapsSuccessfully()
        {
            const string sql = @"UPDATE dbo.Users SET Score = Score + 1 WHERE Id = 1 AND Name NOT LIKE 'X%'";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
        }
    }
}
