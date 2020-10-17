namespace SqExpress.Syntax.Names
{
    public interface IExprName : IExpr
    {
        string Name { get; }

        string LowerInvariantName { get; }
    }
}