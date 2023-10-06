namespace SqExpress.Syntax.Names
{
    public interface IExprTableFullName : IExprColumnSource
    {
        ExprTableFullName AsExprTableFullName();

        string? SchemaName
        {
            get;
        }

        string TableName
        {
            get;
        }
    }
}