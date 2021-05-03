using System;
using System.Data.SqlTypes;
using SqExpress.Syntax;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress.SyntaxTreeOperations.Internal
{
    internal readonly struct ExprValueTypeAnalyzerCtx<TRes, TCtx>
    {
        public readonly TCtx Ctx;

        public readonly IExprValueTypeVisitor<TRes, TCtx> ValueVisitor;

        public ExprValueTypeAnalyzerCtx(TCtx ctx, IExprValueTypeVisitor<TRes, TCtx> valueVisitor)
        {
            this.Ctx = ctx;
            this.ValueVisitor = valueVisitor;
        }
    }

    internal class ExprValueTypeAnalyzer<TRes, TCtx> : IExprValueVisitor<TRes, ExprValueTypeAnalyzerCtx<TRes, TCtx>>, IExprTypeVisitor<TRes, ExprValueTypeAnalyzerCtx<TRes, TCtx>>
    {
        public TRes VisitExprInt32Literal(ExprInt32Literal exprInt32Literal, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitInt32(ctx.Ctx);
        }

        public TRes VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitGuid(ctx.Ctx);
        }

        public TRes VisitExprStringLiteral(ExprStringLiteral stringLiteral, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitString(ctx.Ctx, stringLiteral.Value?.Length, false);
        }

        public TRes VisitExprDateTimeLiteral(ExprDateTimeLiteral dateTimeLiteral, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitDateTime(ctx.Ctx);
        }

        public TRes VisitExprBoolLiteral(ExprBoolLiteral boolLiteral, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitBool(ctx.Ctx);
        }

        public TRes VisitExprInt64Literal(ExprInt64Literal int64Literal, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitInt64(ctx.Ctx);
        }

        public TRes VisitExprByteLiteral(ExprByteLiteral byteLiteral, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitByte(ctx.Ctx);
        }

        public TRes VisitExprInt16Literal(ExprInt16Literal int16Literal, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitInt16(ctx.Ctx);
        }

        public TRes VisitExprDecimalLiteral(ExprDecimalLiteral decimalLiteral, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            DecimalPrecisionScale? precisionScale = null;

            if (decimalLiteral.Value.HasValue)
            {
                SqlDecimal sd = decimalLiteral.Value.Value;
                precisionScale = new DecimalPrecisionScale(sd.Precision, sd.Scale);
            }

            return ctx.ValueVisitor.VisitDecimal(ctx.Ctx, precisionScale);
        }

        public TRes VisitExprDoubleLiteral(ExprDoubleLiteral doubleLiteral, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitDouble(ctx.Ctx);
        }

        public TRes VisitExprByteArrayLiteral(ExprByteArrayLiteral byteArrayLiteral, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitByteArray(ctx.Ctx, byteArrayLiteral.Value?.Count, false);
        }

        public TRes VisitExprNull(ExprNull exprNull, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitAny(ctx.Ctx);
        }

        public TRes VisitExprUnsafeValue(ExprUnsafeValue exprUnsafeValue, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitAny(ctx.Ctx);
        }

        public TRes VisitExprSum(ExprSum exprSum, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return exprSum.Left.Accept(this, ctx);
        }

        public TRes VisitExprSub(ExprSub exprSub, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return exprSub.Left.Accept(this, ctx);
        }

        public TRes VisitExprMul(ExprMul exprMul, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return exprMul.Left.Accept(this, ctx);
        }

        public TRes VisitExprDiv(ExprDiv exprDiv, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return exprDiv.Left.Accept(this, ctx);
        }

        public TRes VisitExprModulo(ExprModulo exprModulo, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return exprModulo.Left.Accept(this, ctx);
        }

        public TRes VisitExprStringConcat(ExprStringConcat exprStringConcat, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitString(ctx.Ctx, null, false);
        }

        public TRes VisitExprScalarFunction(ExprScalarFunction exprScalarFunction, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitAny(ctx.Ctx);
        }

        public TRes VisitExprCase(ExprCase exprCase, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return exprCase.DefaultValue.Accept(this, ctx);
        }

        public TRes VisitExprCaseWhenThen(ExprCaseWhenThen exprCaseWhenThen, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return exprCaseWhenThen.Value.Accept(this, ctx);
        }

        public TRes VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return exprFuncIsNull.Test.Accept(this, ctx);
        }

        public TRes VisitExprFuncCoalesce(ExprFuncCoalesce exprFuncCoalesce, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return exprFuncCoalesce.Test.Accept(this, ctx);
        }

        public TRes VisitExprGetDate(ExprGetDate exprGetDate, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitDateTime(ctx.Ctx);
        }

        public TRes VisitExprGetUtcDate(ExprGetUtcDate exprGetUtcDate, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitDateTime(ctx.Ctx);
        }

        public TRes VisitExprDateAdd(ExprDateAdd exprDateAdd, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitDateTime(ctx.Ctx);
        }

        public TRes VisitExprColumn(ExprColumn exprColumn, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            if (exprColumn is TableColumn tc)
            {
                return tc.SqlType.Accept(this, ctx);
            }
            return ctx.ValueVisitor.VisitAny(ctx.Ctx);
        }

        public TRes VisitExprCast(ExprCast exprCast, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return exprCast.SqlType.Accept(this, ctx);
        }

        public TRes VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitBool(ctx.Ctx);
        }

        public TRes VisitExprTypeByte(ExprTypeByte exprTypeByte, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitByte(ctx.Ctx);
        }

        public TRes VisitExprTypeByteArray(ExprTypeByteArray exprTypeByte, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitByteArray(ctx.Ctx, exprTypeByte.Size, false);
        }

        public TRes VisitExprTypeFixSizeByteArray(ExprTypeFixSizeByteArray exprTypeFixSizeByteArray, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitByteArray(ctx.Ctx, exprTypeFixSizeByteArray.Size, true);
        }

        public TRes VisitExprTypeInt16(ExprTypeInt16 exprTypeInt16, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitInt16(ctx.Ctx);
        }

        public TRes VisitExprTypeInt32(ExprTypeInt32 exprTypeInt32, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitInt32(ctx.Ctx);
        }

        public TRes VisitExprTypeInt64(ExprTypeInt64 exprTypeInt64, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitInt64(ctx.Ctx);
        }

        public TRes VisitExprTypeDecimal(ExprTypeDecimal exprTypeDecimal, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitDecimal(ctx.Ctx, exprTypeDecimal.PrecisionScale);
        }

        public TRes VisitExprTypeDouble(ExprTypeDouble exprTypeDouble, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitDouble(ctx.Ctx);
        }

        public TRes VisitExprTypeDateTime(ExprTypeDateTime exprTypeDateTime, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitDateTime(ctx.Ctx);
        }

        public TRes VisitExprTypeGuid(ExprTypeGuid exprTypeGuid, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitGuid(ctx.Ctx);
        }

        public TRes VisitExprTypeString(ExprTypeString exprTypeString, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitString(ctx.Ctx, exprTypeString.Size, false);
        }

        public TRes VisitExprTypeFixSizeString(ExprTypeFixSizeString exprTypeFixSizeString, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitString(ctx.Ctx, exprTypeFixSizeString.Size, true);
        }

        public TRes VisitExprTypeXml(ExprTypeXml exprTypeXml, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitXml(ctx.Ctx);
        }
    }
}