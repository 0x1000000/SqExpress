using System.Data;
using SqExpress.Syntax;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Internal;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress.SqlExport.Internal;

internal enum DbParameterValueVisitorExtractorMode
{
    Default,
}

internal class DbParameterValueVisitorExtractor: IExprValueVisitorInternal<DbParameterValue?, DbParameterValueVisitorExtractorMode>
{
    public static readonly DbParameterValueVisitorExtractor Instance = new DbParameterValueVisitorExtractor();

    private static object ToDbValue(object? value)
    {
        return value ?? System.DBNull.Value;
    }

    public DbParameterValue? VisitExprInt32Literal(ExprInt32Literal exprInt32Literal, DbParameterValueVisitorExtractorMode mode)
    {
        return new DbParameterValue(ToDbValue(exprInt32Literal.Value), DbType.Int32);
    }

    public DbParameterValue? VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral, DbParameterValueVisitorExtractorMode mode)
    {
        return new DbParameterValue(ToDbValue(exprGuidLiteral.Value), DbType.Guid);
    }

    public DbParameterValue? VisitExprStringLiteral(ExprStringLiteral stringLiteral, DbParameterValueVisitorExtractorMode mode)
    {
        return new DbParameterValue(ToDbValue(stringLiteral.Value), DbType.String);
    }

    public DbParameterValue? VisitExprDateTimeLiteral(ExprDateTimeLiteral dateTimeLiteral, DbParameterValueVisitorExtractorMode mode)
    {
        return new DbParameterValue(ToDbValue(dateTimeLiteral.Value), DbType.DateTime);
    }

    public DbParameterValue? VisitExprDateTimeOffsetLiteral(ExprDateTimeOffsetLiteral dateTimeLiteral, DbParameterValueVisitorExtractorMode mode)
    {
        return new DbParameterValue(ToDbValue(dateTimeLiteral.Value), DbType.DateTimeOffset);
    }

    public DbParameterValue? VisitExprBoolLiteral(ExprBoolLiteral boolLiteral, DbParameterValueVisitorExtractorMode mode)
    {
        return new DbParameterValue(ToDbValue(boolLiteral.Value), DbType.Boolean);
    }

    public DbParameterValue? VisitExprInt64Literal(ExprInt64Literal int64Literal, DbParameterValueVisitorExtractorMode mode)
    {
        return new DbParameterValue(ToDbValue(int64Literal.Value), DbType.Int64);
    }

    public DbParameterValue? VisitExprByteLiteral(ExprByteLiteral byteLiteral, DbParameterValueVisitorExtractorMode mode)
    {
        return new DbParameterValue(ToDbValue(byteLiteral.Value), DbType.Byte);
    }

    public DbParameterValue? VisitExprInt16Literal(ExprInt16Literal int16Literal, DbParameterValueVisitorExtractorMode mode)
    {
        return new DbParameterValue(ToDbValue(int16Literal.Value), DbType.Int16);
    }

    public DbParameterValue? VisitExprDecimalLiteral(ExprDecimalLiteral decimalLiteral, DbParameterValueVisitorExtractorMode mode)
    {
        return new DbParameterValue(ToDbValue(decimalLiteral.Value), DbType.Decimal);
    }

    public DbParameterValue? VisitExprDoubleLiteral(ExprDoubleLiteral doubleLiteral, DbParameterValueVisitorExtractorMode mode)
    {
        return new DbParameterValue(ToDbValue(doubleLiteral.Value), DbType.Double);
    }

    public DbParameterValue? VisitExprByteArrayLiteral(ExprByteArrayLiteral byteArrayLiteral, DbParameterValueVisitorExtractorMode mode)
    {
        return new DbParameterValue(ToDbValue(byteArrayLiteral.Value), DbType.Binary);
    }

    public DbParameterValue? VisitExprNull(ExprNull exprNull, DbParameterValueVisitorExtractorMode mode)
    {
        return new DbParameterValue(System.DBNull.Value, DbType.Object);
    }

    public DbParameterValue? VisitExprUnsafeValue(ExprUnsafeValue exprUnsafeValue, DbParameterValueVisitorExtractorMode mode)
    {
        return new DbParameterValue(ToDbValue(exprUnsafeValue.UnsafeValue), DbType.String);
    }

    public DbParameterValue? VisitExprValueQuery(ExprValueQuery exprValueQuery, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprSum(ExprSum exprSum, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprSub(ExprSub exprSub, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprMul(ExprMul exprMul, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprDiv(ExprDiv exprDiv, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprModulo(ExprModulo exprModulo, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprStringConcat(ExprStringConcat exprStringConcat, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprBitwiseNot(ExprBitwiseNot exprBitwiseNot, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprBitwiseAnd(ExprBitwiseAnd exprBitwiseAnd, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprBitwiseXor(ExprBitwiseXor exprBitwiseXor, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprBitwiseOr(ExprBitwiseOr exprBitwiseOr, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprScalarFunction(ExprScalarFunction exprScalarFunction, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprCase(ExprCase exprCase, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprCaseWhenThen(ExprCaseWhenThen exprCaseWhenThen, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprFuncCoalesce(ExprFuncCoalesce exprFuncCoalesce, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprGetDate(ExprGetDate exprGetDate, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprGetUtcDate(ExprGetUtcDate exprGetUtcDate, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprDateAdd(ExprDateAdd exprDateAdd, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprDateDiff(ExprDateDiff exprDateDiff, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprColumn(ExprColumn exprColumn, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprCast(ExprCast exprCast, DbParameterValueVisitorExtractorMode mode)
    {
        return null;
    }

    public DbParameterValue? VisitExprParameter(ExprParameter exprParameter, DbParameterValueVisitorExtractorMode arg)
    {
        if (!ReferenceEquals(exprParameter.ReplacedValue, null))
        {
            return exprParameter.ReplacedValue.Accept(this, arg);
        }

        return null;
    }
}
