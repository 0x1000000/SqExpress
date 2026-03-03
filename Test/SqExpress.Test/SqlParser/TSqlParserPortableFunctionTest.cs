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
    }
}
