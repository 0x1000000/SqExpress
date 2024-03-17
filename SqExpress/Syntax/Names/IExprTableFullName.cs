namespace SqExpress.Syntax.Names
{
    public interface IExprTableFullName : IExprColumnSource
    {
        ExprTableFullName AsExprTableFullName();

        string? SchemaName
        {
            get;
        }

        string? LowerInvariantSchemaName
        {
            get;
        }

        string TableName
        {
            get;
        }

        string LowerInvariantTableName
        {
            get;
        }
    }
}