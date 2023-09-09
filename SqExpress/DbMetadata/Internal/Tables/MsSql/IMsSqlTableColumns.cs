using SqExpress.Syntax.Select;

namespace SqExpress.DbMetadata.Internal.Tables.MsSql
{
    internal interface IMsSqlTableColumns : IExprTableSource
    {
        StringTableColumn TableCatalog { get; }
        StringTableColumn TableSchema { get; }
        StringTableColumn TableName { get; }
    }
}