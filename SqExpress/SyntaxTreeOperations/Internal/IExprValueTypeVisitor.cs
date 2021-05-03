using SqExpress.Syntax.Type;

namespace SqExpress.SyntaxTreeOperations.Internal
{
    internal interface IExprValueTypeVisitor<out TRes, in TArg>
    {
        TRes VisitAny(TArg arg);
        TRes VisitBool(TArg arg);
        TRes VisitByte(TArg arg);
        TRes VisitInt16(TArg arg);
        TRes VisitInt32(TArg arg);
        TRes VisitInt64(TArg arg);
        TRes VisitDecimal(TArg arg, DecimalPrecisionScale? decimalPrecisionScale);
        TRes VisitDouble(TArg arg);
        TRes VisitString(TArg arg, int? size, bool fix);
        TRes VisitXml(TArg arg);
        TRes VisitDateTime(TArg arg);
        TRes VisitGuid(TArg arg);
        TRes VisitByteArray(TArg arg, int? length, bool fix);
    }
}