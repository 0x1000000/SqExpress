using SqExpress.Syntax.Type;

namespace SqExpress.SyntaxTreeOperations.Internal
{
    internal interface IExprValueTypeVisitor<out TRes, in TArg>
    {
        TRes VisitAny(TArg arg, bool? isNull);
        TRes VisitBool(TArg arg, bool? isNull);
        TRes VisitByte(TArg arg, bool? isNull);
        TRes VisitInt16(TArg arg, bool? isNull);
        TRes VisitInt32(TArg arg, bool? isNull);
        TRes VisitInt64(TArg arg, bool? isNull);
        TRes VisitDecimal(TArg arg, bool? isNull, DecimalPrecisionScale? decimalPrecisionScale);
        TRes VisitDouble(TArg arg, bool? isNull);
        TRes VisitString(TArg arg, bool? isNull, int? size, bool fix);
        TRes VisitXml(TArg arg, bool? isNull);
        TRes VisitDateTime(TArg arg, bool? isNull);
        TRes VisitGuid(TArg arg, bool? isNull);
        TRes VisitByteArray(TArg arg, bool? isNull, int? length, bool fix);
    }
}