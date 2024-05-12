using SqExpress.Syntax.Select;

namespace SqExpress.DbMetadata.Internal.Tables.MySQL.InformationSchema;

internal interface IMySqlTableColumns : IExprTableSource
{
    StringTableColumn TableCatalog { get; }
    StringTableColumn TableSchema { get; }
    StringTableColumn TableName { get; }
}