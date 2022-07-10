using SqExpress.Syntax.Type;

namespace SqExpress.SyntaxTreeOperations.Internal
{
    internal class ExprValueTypeIsNullVisitor : IExprValueTypeVisitor<bool?, object?>
    {
        public static readonly ExprValueTypeIsNullVisitor Instance = new ExprValueTypeIsNullVisitor();

        private ExprValueTypeIsNullVisitor() { }

        public bool? VisitAny(object? arg, bool? isNull)
        {
            return isNull;
        }

        public bool? VisitBool(object? arg, bool? isNull)
        {
            return isNull;
        }

        public bool? VisitByte(object? arg, bool? isNull)
        {
            return isNull;
        }

        public bool? VisitInt16(object? arg, bool? isNull)
        {
            return isNull;
        }

        public bool? VisitInt32(object? arg, bool? isNull)
        {
            return isNull;
        }

        public bool? VisitInt64(object? arg, bool? isNull)
        {
            return isNull;
        }

        public bool? VisitDecimal(object? arg, bool? isNull, DecimalPrecisionScale? decimalPrecisionScale)
        {
            return isNull;
        }

        public bool? VisitDouble(object? arg, bool? isNull)
        {
            return isNull;
        }

        public bool? VisitString(object? arg, bool? isNull, int? size, bool fix)
        {
            return isNull;
        }

        public bool? VisitXml(object? arg, bool? isNull)
        {
            return isNull;
        }

        public bool? VisitDateTime(object? arg, bool? isNull)
        {
            return isNull;
        }

        public bool? VisitDateTimeOffset(object? arg, bool? isNull)
        {
            return isNull;
        }

        public bool? VisitGuid(object? arg, bool? isNull)
        {
            return isNull;
        }

        public bool? VisitByteArray(object? arg, bool? isNull, int? length, bool fix)
        {
            return isNull;
        }
    }
}