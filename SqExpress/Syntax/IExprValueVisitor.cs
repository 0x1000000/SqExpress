using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax
{
    public interface IExprValueVisitor<out TRes, in TArg>
    {
        //Value
        TRes VisitExprInt32Literal(ExprInt32Literal exprInt32Literal, TArg arg);

        TRes VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral, TArg arg);

        TRes VisitExprStringLiteral(ExprStringLiteral stringLiteral, TArg arg);

        TRes VisitExprDateTimeLiteral(ExprDateTimeLiteral dateTimeLiteral, TArg arg);

        TRes VisitExprBoolLiteral(ExprBoolLiteral boolLiteral, TArg arg);

        TRes VisitExprInt64Literal(ExprInt64Literal int64Literal, TArg arg);

        TRes VisitExprByteLiteral(ExprByteLiteral byteLiteral, TArg arg);

        TRes VisitExprInt16Literal(ExprInt16Literal int16Literal, TArg arg);

        TRes VisitExprDecimalLiteral(ExprDecimalLiteral decimalLiteral, TArg arg);

        TRes VisitExprDoubleLiteral(ExprDoubleLiteral doubleLiteral, TArg arg);

        TRes VisitExprByteArrayLiteral(ExprByteArrayLiteral byteArrayLiteral, TArg arg);

        TRes VisitExprNull(ExprNull exprNull, TArg arg);

        TRes VisitExprUnsafeValue(ExprUnsafeValue exprUnsafeValue, TArg arg);

        //Arithmetic Expressions
        TRes VisitExprSum(ExprSum exprSum, TArg arg);

        TRes VisitExprSub(ExprSub exprSub, TArg arg);

        TRes VisitExprMul(ExprMul exprMul, TArg arg);

        TRes VisitExprDiv(ExprDiv exprDiv, TArg arg);

        TRes VisitExprModulo(ExprModulo exprModulo, TArg arg);

        TRes VisitExprStringConcat(ExprStringConcat exprStringConcat, TArg arg);

        //Functions
        TRes VisitExprScalarFunction(ExprScalarFunction exprScalarFunction, TArg arg);

        TRes VisitExprCase(ExprCase exprCase, TArg arg);

        TRes VisitExprCaseWhenThen(ExprCaseWhenThen exprCaseWhenThen, TArg arg);

        //Functions - Known
        TRes VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull, TArg arg);

        TRes VisitExprFuncCoalesce(ExprFuncCoalesce exprFuncCoalesce, TArg arg);

        TRes VisitExprGetDate(ExprGetDate exprGetDate, TArg arg);

        TRes VisitExprGetUtcDate(ExprGetUtcDate exprGetUtcDate, TArg arg);

        TRes VisitExprDateAdd(ExprDateAdd exprDateAdd, TArg arg);

        //Meta
        TRes VisitExprColumn(ExprColumn exprColumn, TArg arg);

        //Types
        TRes VisitExprCast(ExprCast exprCast, TArg arg);
    }
}