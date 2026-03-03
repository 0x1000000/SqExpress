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

internal class DbParameterValueVisitorExtractor: IExprValueVisitorInternal<DbParameterValue?, string?>
{
    public static readonly DbParameterValueVisitorExtractor Instance = new DbParameterValueVisitorExtractor();

    protected static object ToDbValue(object? value)
    {
        return value ?? System.DBNull.Value;
    }

    public virtual DbParameterValue? VisitExprInt32Literal(ExprInt32Literal exprInt32Literal, string? name)
    {
        return new DbParameterValue(ToDbValue(exprInt32Literal.Value), DbType.Int32, name);
    }

    public virtual DbParameterValue? VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral, string? name)
    {
        return new DbParameterValue(ToDbValue(exprGuidLiteral.Value), DbType.Guid, name);
    }

    public virtual DbParameterValue? VisitExprStringLiteral(ExprStringLiteral stringLiteral, string? name)
    {
        return new DbParameterValue(ToDbValue(stringLiteral.Value), DbType.String, name);
    }

    public virtual DbParameterValue? VisitExprDateTimeLiteral(ExprDateTimeLiteral dateTimeLiteral, string? name)
    {
        return new DbParameterValue(ToDbValue(dateTimeLiteral.Value), DbType.DateTime, name);
    }

    public virtual DbParameterValue? VisitExprDateTimeOffsetLiteral(ExprDateTimeOffsetLiteral dateTimeLiteral, string? name)
    {
        return new DbParameterValue(ToDbValue(dateTimeLiteral.Value), DbType.DateTimeOffset, name);
    }

    public virtual DbParameterValue? VisitExprBoolLiteral(ExprBoolLiteral boolLiteral, string? name)
    {
        return new DbParameterValue(ToDbValue(boolLiteral.Value), DbType.Boolean, name);
    }

    public virtual DbParameterValue? VisitExprInt64Literal(ExprInt64Literal int64Literal, string? name)
    {
        return new DbParameterValue(ToDbValue(int64Literal.Value), DbType.Int64, name);
    }

    public virtual DbParameterValue? VisitExprByteLiteral(ExprByteLiteral byteLiteral, string? name)
    {
        return new DbParameterValue(ToDbValue(byteLiteral.Value), DbType.Byte, name);
    }

    public virtual DbParameterValue? VisitExprInt16Literal(ExprInt16Literal int16Literal, string? name)
    {
        return new DbParameterValue(ToDbValue(int16Literal.Value), DbType.Int16, name);
    }

    public virtual DbParameterValue? VisitExprDecimalLiteral(ExprDecimalLiteral decimalLiteral, string? name)
    {
        return new DbParameterValue(ToDbValue(decimalLiteral.Value), DbType.Decimal, name);
    }

    public virtual DbParameterValue? VisitExprDoubleLiteral(ExprDoubleLiteral doubleLiteral, string? name)
    {
        return new DbParameterValue(ToDbValue(doubleLiteral.Value), DbType.Double, name);
    }

    public virtual DbParameterValue? VisitExprByteArrayLiteral(ExprByteArrayLiteral byteArrayLiteral, string? name)
    {
        return new DbParameterValue(ToDbValue(byteArrayLiteral.Value), DbType.Binary, name);
    }

    public virtual DbParameterValue? VisitExprNull(ExprNull exprNull, string? name)
    {
        return new DbParameterValue(System.DBNull.Value, DbType.Object, name);
    }

    public virtual DbParameterValue? VisitExprUnsafeValue(ExprUnsafeValue exprUnsafeValue, string? name)
    {
        return new DbParameterValue(ToDbValue(exprUnsafeValue.UnsafeValue), DbType.String, name);
    }

    public virtual DbParameterValue? VisitExprValueQuery(ExprValueQuery exprValueQuery, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprSum(ExprSum exprSum, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprSub(ExprSub exprSub, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprMul(ExprMul exprMul, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprDiv(ExprDiv exprDiv, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprModulo(ExprModulo exprModulo, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprStringConcat(ExprStringConcat exprStringConcat, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprBitwiseNot(ExprBitwiseNot exprBitwiseNot, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprBitwiseAnd(ExprBitwiseAnd exprBitwiseAnd, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprBitwiseXor(ExprBitwiseXor exprBitwiseXor, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprBitwiseOr(ExprBitwiseOr exprBitwiseOr, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprScalarFunction(ExprScalarFunction exprScalarFunction, string? name)
    {
        return null;
    }

    public DbParameterValue? VisitExprPortableScalarFunction(ExprPortableScalarFunction exprPortableScalarFunction, string? arg)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprCase(ExprCase exprCase, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprCaseWhenThen(ExprCaseWhenThen exprCaseWhenThen, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprFuncCoalesce(ExprFuncCoalesce exprFuncCoalesce, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprGetDate(ExprGetDate exprGetDate, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprGetUtcDate(ExprGetUtcDate exprGetUtcDate, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprDateAdd(ExprDateAdd exprDateAdd, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprDateDiff(ExprDateDiff exprDateDiff, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprColumn(ExprColumn exprColumn, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprCast(ExprCast exprCast, string? name)
    {
        return null;
    }

    public virtual DbParameterValue? VisitExprParameter(ExprParameter exprParameter, string? name)
    {
        if (!ReferenceEquals(exprParameter.ReplacedValue, null))
        {
            return exprParameter.ReplacedValue.Accept(this, name);
        }

        return null;
    }
}
