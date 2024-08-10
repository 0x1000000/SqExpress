namespace SqExpress.Syntax.Names
{
    public interface IExprTableFullName : IExprColumnSource
    {
        ExprTableFullName AsExprTableFullName();

        IExprTableFullName WithTableName(string tableName);

        IExprTableFullName WithSchemaName(string? schemaName);

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