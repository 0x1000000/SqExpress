namespace SqExpress.Syntax
{
    public interface IExpr
    {
        TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg);
    }
}