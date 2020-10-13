namespace SqExpress.Syntax.Select
{
    public interface IExprSelecting : IExpr
    {
        
    }

    public interface IExprNamedSelecting : IExprSelecting
    {
        string? OutputName { get; }
    }
}