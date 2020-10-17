namespace SqExpress.Syntax.Names
{
    public interface IExprTableFullName : IExprColumnSource
    {
        ExprTableFullName AsExprTableFullName();
    }
}