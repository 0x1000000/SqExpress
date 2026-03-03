using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SqExpress.SqlParser;
using SqExpress.Syntax;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Type;

namespace SqExpress.Test.SqlParser
{
    public class TSqlParserCastTest
    {
        [Test]
        public void ParseSelectWithCastExpressions_BuildsExpectedTypeNodes()
        {
            var sql =
                "SELECT " +
                "cast(1 AS int) as A, " +
                "CAST(1 AS DECIMAL(10,2)) AS [D], " +
                "CAST('x' AS NVARCHAR(MAX)) AS [S], " +
                "CAST('x' AS CHAR(3)) AS [C], " +
                "CAST('x' AS DATETIME2(7)) AS [DT], " +
                "CAST('x' AS DATETIMEOFFSET(7)) AS [DTO], " +
                "CAST('x' AS XML) AS [X], " +
                "CAST('x' AS VARBINARY(MAX)) AS [B]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.TypeOf<ExprQuerySpecification>());

            var query = (ExprQuerySpecification)expr!;
            Assert.That(query.SelectList.Count, Is.EqualTo(8));

            var casts = query.SelectList
                .OfType<ExprAliasedSelecting>()
                .Select(i => i.Value)
                .OfType<ExprCast>()
                .ToList();

            Assert.That(casts.Count, Is.EqualTo(8));
            Assert.That(casts[0].SqlType, Is.TypeOf<ExprTypeInt32>());

            var decimalType = casts[1].SqlType as ExprTypeDecimal;
            Assert.That(decimalType, Is.Not.Null);
            Assert.That(decimalType!.PrecisionScale!.Value.Precision, Is.EqualTo(10));
            Assert.That(decimalType.PrecisionScale!.Value.Scale, Is.EqualTo(2));

            var nvarchar = casts[2].SqlType as ExprTypeString;
            Assert.That(nvarchar, Is.Not.Null);
            Assert.That(nvarchar!.IsUnicode, Is.True);
            Assert.That(nvarchar.IsText, Is.False);
            Assert.That(nvarchar.Size, Is.Null);

            var charType = casts[3].SqlType as ExprTypeFixSizeString;
            Assert.That(charType, Is.Not.Null);
            Assert.That(charType!.IsUnicode, Is.False);
            Assert.That(charType.Size, Is.EqualTo(3));

            var datetime2 = casts[4].SqlType as ExprTypeDateTime;
            Assert.That(datetime2, Is.Not.Null);
            Assert.That(datetime2!.IsDate, Is.False);

            Assert.That(casts[5].SqlType, Is.TypeOf<ExprTypeDateTimeOffset>());
            Assert.That(casts[6].SqlType, Is.TypeOf<ExprTypeXml>());

            var varBinary = casts[7].SqlType as ExprTypeByteArray;
            Assert.That(varBinary, Is.Not.Null);
            Assert.That(varBinary!.Size, Is.Null);
        }

        [Test]
        public void ParseCastWithUnsupportedType_ReturnsParserError()
        {
            var sql = "SELECT CAST(1 AS sql_variant) AS [V]";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("CAST type 'sql_variant' is not supported by SqExpress parser."));
        }

        [TestCaseSource(nameof(UnhappyCases))]
        public void ParseCastWithInvalidSyntaxOrArguments_ReturnsParserError(string sql, string expectedErrorPart)
        {
            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain(expectedErrorPart));
        }

        private static IEnumerable<TestCaseData> UnhappyCases()
        {
            yield return new TestCaseData(
                    "SELECT CAST(1) AS [A]",
                    "CAST expression is invalid.")
                .SetName("Cast_MissingAsClause");

            yield return new TestCaseData(
                    "SELECT CAST(1 AS) AS [A]",
                    "CAST expression is invalid.")
                .SetName("Cast_MissingType");

            yield return new TestCaseData(
                    "SELECT CAST 1 AS INT AS [A]",
                    "CAST expression should contain '(' after CAST.")
                .SetName("Cast_MissingParentheses");

            yield return new TestCaseData(
                    "SELECT CAST(1 AS INT(10)) AS [A]",
                    "Type 'INT' does not accept arguments.")
                .SetName("Cast_IntWithArguments");

            yield return new TestCaseData(
                    "SELECT CAST(1 AS DATETIME2(8)) AS [A]",
                    "Type 'DATETIME2' numeric argument is out of range.")
                .SetName("Cast_DateTime2ScaleOutOfRange");

            yield return new TestCaseData(
                    "SELECT CAST(1 AS DATETIMEOFFSET(9)) AS [A]",
                    "Type 'DATETIMEOFFSET' numeric argument is out of range.")
                .SetName("Cast_DateTimeOffsetScaleOutOfRange");

            yield return new TestCaseData(
                    "SELECT CAST(1 AS DECIMAL(10,11)) AS [A]",
                    "Type 'DECIMAL' scale cannot be greater than precision.")
                .SetName("Cast_DecimalScaleGreaterThanPrecision");

            yield return new TestCaseData(
                    "SELECT CAST(1 AS DECIMAL(10,2,1)) AS [A]",
                    "Type 'DECIMAL' expects one or two numeric arguments.")
                .SetName("Cast_DecimalTooManyArguments");

            yield return new TestCaseData(
                    "SELECT CAST(1 AS CHAR(MAX)) AS [A]",
                    "Type 'CHAR' cannot use MAX length.")
                .SetName("Cast_CharCannotUseMax");

            yield return new TestCaseData(
                    "SELECT CAST(1 AS NVARCHAR(10,2)) AS [A]",
                    "Type 'NVARCHAR' expects a single length argument.")
                .SetName("Cast_NVarCharTooManyLengthArguments");

            yield return new TestCaseData(
                    "SELECT CAST(1 AS VARCHAR(abc)) AS [A]",
                    "Type 'VARCHAR' length argument is invalid.")
                .SetName("Cast_VarCharInvalidLengthToken");
        }
    }
}
