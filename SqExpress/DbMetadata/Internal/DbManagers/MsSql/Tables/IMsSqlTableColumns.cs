using SqExpress.Syntax.Select;

namespace SqExpress.DbMetadata.Internal.DbManagers.MsSql.Tables
{
    internal interface IMsSqlTableColumns : IExprTableSource
    {
        StringTableColumn TableCatalog { get; }
        StringTableColumn TableSchema { get; }
        StringTableColumn TableName { get; }
    }
}