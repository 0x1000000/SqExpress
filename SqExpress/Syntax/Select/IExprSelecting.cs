namespace SqExpress.Syntax.Select;

public interface IExprSelecting : IExpr
{
    TRes Accept<TRes, TArg>(IExprSelectingVisitor<TRes, TArg> visitor, TArg arg);
}

public abstract class ExprSelecting : IExprSelecting
{
    public abstract TRes Accept<TRes, TArg>(IExprSelectingVisitor<TRes, TArg> visitor, TArg arg);

    public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
    {
        return this.Accept((IExprSelectingVisitor<TRes, TArg>)visitor, arg);
    }
}

public interface IExprNamedSelecting : IExprSelecting
{
    string? OutputName { get; }
}