namespace SqExpress.Syntax
{
    public interface IExpr
    {
        TRes Accept<TRes>(IExprVisitor<TRes> visitor);
    }
}