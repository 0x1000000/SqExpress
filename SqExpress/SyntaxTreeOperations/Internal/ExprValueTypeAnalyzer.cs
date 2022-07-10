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
        public static readonly ExprValueTypeAnalyzer<TRes, TCtx> Instance = new ExprValueTypeAnalyzer<TRes, TCtx>();

        private ExprValueTypeAnalyzer() { }

        public TRes VisitExprInt32Literal(ExprInt32Literal exprInt32Literal, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitInt32(ctx.Ctx, !exprInt32Literal.Value.HasValue);
        }

        public TRes VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitGuid(ctx.Ctx, !exprGuidLiteral.Value.HasValue);
        }

        public TRes VisitExprStringLiteral(ExprStringLiteral stringLiteral, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitString(ctx.Ctx, stringLiteral.Value == null, stringLiteral.Value?.Length, false);
        }

        public TRes VisitExprDateTimeLiteral(ExprDateTimeLiteral dateTimeLiteral, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitDateTime(ctx.Ctx, !dateTimeLiteral.Value.HasValue);
        }

        public TRes VisitExprDateTimeOffsetLiteral(ExprDateTimeOffsetLiteral dateTimeLiteral, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitDateTimeOffset(ctx.Ctx, !dateTimeLiteral.Value.HasValue);
        }

        public TRes VisitExprBoolLiteral(ExprBoolLiteral boolLiteral, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitBool(ctx.Ctx, !boolLiteral.Value.HasValue);
        }

        public TRes VisitExprInt64Literal(ExprInt64Literal int64Literal, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitInt64(ctx.Ctx, !int64Literal.Value.HasValue);
        }

        public TRes VisitExprByteLiteral(ExprByteLiteral byteLiteral, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitByte(ctx.Ctx, !byteLiteral.Value.HasValue);
        }

        public TRes VisitExprInt16Literal(ExprInt16Literal int16Literal, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitInt16(ctx.Ctx, !int16Literal.Value.HasValue);
        }

        public TRes VisitExprDecimalLiteral(ExprDecimalLiteral decimalLiteral, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            DecimalPrecisionScale? precisionScale = null;

            if (decimalLiteral.Value.HasValue)
            {
                SqlDecimal sd = decimalLiteral.Value.Value;
                precisionScale = new DecimalPrecisionScale(sd.Precision, sd.Scale);
            }

            return ctx.ValueVisitor.VisitDecimal(ctx.Ctx, !decimalLiteral.Value.HasValue, precisionScale);
        }

        public TRes VisitExprDoubleLiteral(ExprDoubleLiteral doubleLiteral, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitDouble(ctx.Ctx, !doubleLiteral.Value.HasValue);
        }

        public TRes VisitExprByteArrayLiteral(ExprByteArrayLiteral byteArrayLiteral, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitByteArray(ctx.Ctx, byteArrayLiteral.Value == null, byteArrayLiteral.Value?.Count, false);
        }

        public TRes VisitExprNull(ExprNull exprNull, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitAny(ctx.Ctx, true);
        }

        public TRes VisitExprUnsafeValue(ExprUnsafeValue exprUnsafeValue, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitAny(ctx.Ctx, null);
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
            return ctx.ValueVisitor.VisitString(ctx.Ctx, null ,null, false);
        }

        public TRes VisitExprScalarFunction(ExprScalarFunction exprScalarFunction, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitAny(ctx.Ctx, null);
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
            return ctx.ValueVisitor.VisitDateTime(ctx.Ctx, false);
        }

        public TRes VisitExprGetUtcDate(ExprGetUtcDate exprGetUtcDate, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitDateTime(ctx.Ctx, false);
        }

        public TRes VisitExprDateAdd(ExprDateAdd exprDateAdd, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitDateTime(ctx.Ctx, null);
        }

        public TRes VisitExprColumn(ExprColumn exprColumn, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            if (exprColumn is TableColumn tc)
            {
                return tc.SqlType.Accept(this, ctx);
            }
            return ctx.ValueVisitor.VisitAny(ctx.Ctx, null);
        }

        public TRes VisitExprCast(ExprCast exprCast, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return exprCast.SqlType.Accept(this, ctx);
        }

        //Implementation to analyze in "VisitExprCast" and "VisitExprColumn"

        TRes IExprTypeVisitor<TRes, ExprValueTypeAnalyzerCtx<TRes, TCtx>>.VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitBool(ctx.Ctx, null);
        }

        TRes IExprTypeVisitor<TRes, ExprValueTypeAnalyzerCtx<TRes, TCtx>>.VisitExprTypeByte(ExprTypeByte exprTypeByte, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitByte(ctx.Ctx, null);
        }

        TRes IExprTypeVisitor<TRes, ExprValueTypeAnalyzerCtx<TRes, TCtx>>.VisitExprTypeByteArray(ExprTypeByteArray exprTypeByte, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitByteArray(ctx.Ctx, null, exprTypeByte.Size, false);
        }

        TRes IExprTypeVisitor<TRes, ExprValueTypeAnalyzerCtx<TRes, TCtx>>.VisitExprTypeFixSizeByteArray(ExprTypeFixSizeByteArray exprTypeFixSizeByteArray, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitByteArray(ctx.Ctx, null, exprTypeFixSizeByteArray.Size, true);
        }

        TRes IExprTypeVisitor<TRes, ExprValueTypeAnalyzerCtx<TRes, TCtx>>.VisitExprTypeInt16(ExprTypeInt16 exprTypeInt16, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitInt16(ctx.Ctx, null);
        }

        TRes IExprTypeVisitor<TRes, ExprValueTypeAnalyzerCtx<TRes, TCtx>>.VisitExprTypeInt32(ExprTypeInt32 exprTypeInt32, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitInt32(ctx.Ctx, null);
        }

        TRes IExprTypeVisitor<TRes, ExprValueTypeAnalyzerCtx<TRes, TCtx>>.VisitExprTypeInt64(ExprTypeInt64 exprTypeInt64, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitInt64(ctx.Ctx, null);
        }

        TRes IExprTypeVisitor<TRes, ExprValueTypeAnalyzerCtx<TRes, TCtx>>.VisitExprTypeDecimal(ExprTypeDecimal exprTypeDecimal, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitDecimal(ctx.Ctx, null, exprTypeDecimal.PrecisionScale);
        }

        TRes IExprTypeVisitor<TRes, ExprValueTypeAnalyzerCtx<TRes, TCtx>>.VisitExprTypeDouble(ExprTypeDouble exprTypeDouble, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitDouble(ctx.Ctx, null);
        }

        TRes IExprTypeVisitor<TRes, ExprValueTypeAnalyzerCtx<TRes, TCtx>>.VisitExprTypeDateTime(ExprTypeDateTime exprTypeDateTime, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitDateTime(ctx.Ctx, null);
        }

        public TRes VisitExprTypeDateTimeOffset(ExprTypeDateTimeOffset exprTypeDateTimeOffset, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitDateTimeOffset(ctx.Ctx, null);
        }

        TRes IExprTypeVisitor<TRes, ExprValueTypeAnalyzerCtx<TRes, TCtx>>.VisitExprTypeGuid(ExprTypeGuid exprTypeGuid, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitGuid(ctx.Ctx, null);
        }

        TRes IExprTypeVisitor<TRes, ExprValueTypeAnalyzerCtx<TRes, TCtx>>.VisitExprTypeString(ExprTypeString exprTypeString, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitString(ctx.Ctx, null, exprTypeString.Size, false);
        }

        TRes IExprTypeVisitor<TRes, ExprValueTypeAnalyzerCtx<TRes, TCtx>>.VisitExprTypeFixSizeString(ExprTypeFixSizeString exprTypeFixSizeString, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitString(ctx.Ctx, null, exprTypeFixSizeString.Size, true);
        }

        TRes IExprTypeVisitor<TRes, ExprValueTypeAnalyzerCtx<TRes, TCtx>>.VisitExprTypeXml(ExprTypeXml exprTypeXml, ExprValueTypeAnalyzerCtx<TRes, TCtx> ctx)
        {
            return ctx.ValueVisitor.VisitXml(ctx.Ctx, null);
        }
    }
}