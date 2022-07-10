using SqExpress.Syntax.Type;

namespace SqExpress.SyntaxTreeOperations.Internal
{
    internal class ExprValueTypeDetailsVisitor : IExprValueTypeVisitor<ExprValueTypeDetails, object?>
    {
        public static readonly ExprValueTypeDetailsVisitor Instance = new ExprValueTypeDetailsVisitor();

        private ExprValueTypeDetailsVisitor() { }

        public ExprValueTypeDetails VisitAny(object? arg, bool? isNull)
        {
            return new ExprValueTypeDetails(isNull, null);
        }

        public ExprValueTypeDetails VisitBool(object? arg, bool? isNull)
        {
            return new ExprValueTypeDetails(isNull, SqQueryBuilder.SqlType.Boolean);
        }

        public ExprValueTypeDetails VisitByte(object? arg, bool? isNull)
        {
            return new ExprValueTypeDetails(isNull, SqQueryBuilder.SqlType.Byte);
        }

        public ExprValueTypeDetails VisitInt16(object? arg, bool? isNull)
        {
            return new ExprValueTypeDetails(isNull, SqQueryBuilder.SqlType.Int16);
        }

        public ExprValueTypeDetails VisitInt32(object? arg, bool? isNull)
        {
            return new ExprValueTypeDetails(isNull, SqQueryBuilder.SqlType.Int32);
        }

        public ExprValueTypeDetails VisitInt64(object? arg, bool? isNull)
        {
            return new ExprValueTypeDetails(isNull, SqQueryBuilder.SqlType.Int64);
        }

        public ExprValueTypeDetails VisitDecimal(object? arg, bool? isNull, DecimalPrecisionScale? decimalPrecisionScale)
        {
            return new ExprValueTypeDetails(isNull, SqQueryBuilder.SqlType.Decimal(decimalPrecisionScale));
        }

        public ExprValueTypeDetails VisitDouble(object? arg, bool? isNull)
        {
            return new ExprValueTypeDetails(isNull, SqQueryBuilder.SqlType.Double);
        }

        public ExprValueTypeDetails VisitString(object? arg, bool? isNull, int? size, bool fix)
        {
            return new ExprValueTypeDetails(isNull, SqQueryBuilder.SqlType.String(size));
        }

        public ExprValueTypeDetails VisitXml(object? arg, bool? isNull)
        {
            return new ExprValueTypeDetails(isNull, SqQueryBuilder.SqlType.String());
        }

        public ExprValueTypeDetails VisitDateTime(object? arg, bool? isNull)
        {
            return new ExprValueTypeDetails(isNull, SqQueryBuilder.SqlType.DateTime(false));
        }

        public ExprValueTypeDetails VisitDateTimeOffset(object? arg, bool? isNull)
        {
            return new ExprValueTypeDetails(isNull, SqQueryBuilder.SqlType.DateTimeOffset);
        }

        public ExprValueTypeDetails VisitGuid(object? arg, bool? isNull)
        {
            return new ExprValueTypeDetails(isNull, SqQueryBuilder.SqlType.Guid);
        }

        public ExprValueTypeDetails VisitByteArray(object? arg, bool? isNull, int? length, bool fix)
        {
            ExprTypeByteArrayBase e = fix 
                ? new ExprTypeFixSizeByteArray(length ?? throw new SqExpressException("A size has to be specified for a fixed size array"))
                : new ExprTypeByteArray(length);

            return new ExprValueTypeDetails(isNull, e);
        }
    }

    internal readonly struct ExprValueTypeDetails
    {
        public readonly bool? IsNull;

        public readonly ExprType? Type;

        public ExprValueTypeDetails(bool? isNull, ExprType? type)
        {
            this.IsNull = isNull;
            this.Type = type;
        }
    }
}