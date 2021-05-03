using SqExpress.Syntax.Type;

namespace SqExpress.Syntax
{
    public interface IExprTypeVisitor<out TRes, in TArg>
    {

        //Types
        TRes VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean, TArg arg);

        TRes VisitExprTypeByte(ExprTypeByte exprTypeByte, TArg arg);

        TRes VisitExprTypeByteArray(ExprTypeByteArray exprTypeByte, TArg arg);

        TRes VisitExprTypeFixSizeByteArray(ExprTypeFixSizeByteArray exprTypeFixSizeByteArray, TArg arg);

        TRes VisitExprTypeInt16(ExprTypeInt16 exprTypeInt16, TArg arg);

        TRes VisitExprTypeInt32(ExprTypeInt32 exprTypeInt32, TArg arg);

        TRes VisitExprTypeInt64(ExprTypeInt64 exprTypeInt64, TArg arg);

        TRes VisitExprTypeDecimal(ExprTypeDecimal exprTypeDecimal, TArg arg);

        TRes VisitExprTypeDouble(ExprTypeDouble exprTypeDouble, TArg arg);

        TRes VisitExprTypeDateTime(ExprTypeDateTime exprTypeDateTime, TArg arg);

        TRes VisitExprTypeGuid(ExprTypeGuid exprTypeGuid, TArg arg);

        TRes VisitExprTypeString(ExprTypeString exprTypeString, TArg arg);

        TRes VisitExprTypeFixSizeString(ExprTypeFixSizeString exprTypeFixSizeString, TArg arg);

        TRes VisitExprTypeXml(ExprTypeXml exprTypeXml, TArg arg);
    }
}