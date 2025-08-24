using System;
using SqExpress.Syntax;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress.Meta.Internal;

internal class ExplicitExprValueExtractor : IExprValueVisitor<object?, object?>
{
    public static readonly ExplicitExprValueExtractor Instance = new ();

    private ExplicitExprValueExtractor()
    {
    }

    public object? VisitExprInt32Literal(ExprInt32Literal exprInt32Literal, object? arg)
    {
        return exprInt32Literal.Value;
    }

    public object? VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral, object? arg)
    {
        return exprGuidLiteral.Value;
    }

    public object? VisitExprStringLiteral(ExprStringLiteral stringLiteral, object? arg)
    {
        return stringLiteral.Value;
    }

    public object? VisitExprDateTimeLiteral(ExprDateTimeLiteral dateTimeLiteral, object? arg)
    {
        return dateTimeLiteral.Value;
    }

    public object? VisitExprDateTimeOffsetLiteral(ExprDateTimeOffsetLiteral dateTimeLiteral, object? arg)
    {
        return dateTimeLiteral.Value;
    }

    public object? VisitExprBoolLiteral(ExprBoolLiteral boolLiteral, object? arg)
    {
        return boolLiteral.Value;
    }

    public object? VisitExprInt64Literal(ExprInt64Literal int64Literal, object? arg)
    {
        return int64Literal.Value;
    }

    public object? VisitExprByteLiteral(ExprByteLiteral byteLiteral, object? arg)
    {
        return byteLiteral.Value;
    }

    public object? VisitExprInt16Literal(ExprInt16Literal int16Literal, object? arg)
    {
        return int16Literal.Value;
    }

    public object? VisitExprDecimalLiteral(ExprDecimalLiteral decimalLiteral, object? arg)
    {
        return decimalLiteral.Value;
    }

    public object? VisitExprDoubleLiteral(ExprDoubleLiteral doubleLiteral, object? arg)
    {
        return doubleLiteral.Value;
    }

    public object? VisitExprByteArrayLiteral(ExprByteArrayLiteral byteArrayLiteral, object? arg)
    {
        return byteArrayLiteral.Value;
    }

    public object? VisitExprNull(ExprNull exprNull, object? arg)
    {
        return null;
    }

    public object? VisitExprUnsafeValue(ExprUnsafeValue exprUnsafeValue, object? arg)
    {
        throw new NotImplementedException();
    }

    public object? VisitExprValueQuery(ExprValueQuery exprValueQuery, object? arg)
    {
        return arg;
    }

    public object? VisitExprSum(ExprSum exprSum, object? arg)
    {
        return arg;
    }

    public object? VisitExprSub(ExprSub exprSub, object? arg)
    {
        return arg;
    }

    public object? VisitExprMul(ExprMul exprMul, object? arg)
    {
        return arg;
    }

    public object? VisitExprDiv(ExprDiv exprDiv, object? arg)
    {
        return arg;
    }

    public object? VisitExprModulo(ExprModulo exprModulo, object? arg)
    {
        return arg;
    }

    public object? VisitExprStringConcat(ExprStringConcat exprStringConcat, object? arg)
    {
        return arg;
    }

    public object? VisitExprBitwiseNot(ExprBitwiseNot exprBitwiseNot, object? arg)
    {
        return arg;
    }

    public object? VisitExprBitwiseAnd(ExprBitwiseAnd exprBitwiseAnd, object? arg)
    {
        return arg;
    }

    public object? VisitExprBitwiseXor(ExprBitwiseXor exprBitwiseXor, object? arg)
    {
        return arg;
    }

    public object? VisitExprBitwiseOr(ExprBitwiseOr exprBitwiseOr, object? arg)
    {
        return arg;
    }

    public object? VisitExprScalarFunction(ExprScalarFunction exprScalarFunction, object? arg)
    {
        return arg;
    }

    public object? VisitExprCase(ExprCase exprCase, object? arg)
    {
        return arg;
    }

    public object? VisitExprCaseWhenThen(ExprCaseWhenThen exprCaseWhenThen, object? arg)
    {
        return arg;
    }

    public object? VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull, object? arg)
    {
        return arg;
    }

    public object? VisitExprFuncCoalesce(ExprFuncCoalesce exprFuncCoalesce, object? arg)
    {
        return arg;
    }

    public object? VisitExprGetDate(ExprGetDate exprGetDate, object? arg)
    {
        return arg;
    }

    public object? VisitExprGetUtcDate(ExprGetUtcDate exprGetUtcDate, object? arg)
    {
        return arg;
    }

    public object? VisitExprDateAdd(ExprDateAdd exprDateAdd, object? arg)
    {
        return arg;
    }

    public object? VisitExprDateDiff(ExprDateDiff exprDateDiff, object? arg)
    {
        return arg;
    }

    public object? VisitExprColumn(ExprColumn exprColumn, object? arg)
    {
        return arg;
    }

    public object? VisitExprCast(ExprCast exprCast, object? arg)
    {
        return arg;
    }
}
