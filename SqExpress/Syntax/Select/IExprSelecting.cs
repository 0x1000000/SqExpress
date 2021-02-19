namespace SqExpress.Syntax.Select
{
    public interface IExprSelecting : IExpr
    {
        
    }

    public abstract class ExprSelecting : IExprSelecting
    {
        public abstract TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg);
    }

    public interface IExprNamedSelecting : IExprSelecting
    {
        string? OutputName { get; }
    }
}