using SqExpress.Syntax.Select;

namespace SqExpress.DbMetadata.Internal.Tables.PgSql;

internal interface IPgSqlTableColumns : IExprTableSource
{
    StringTableColumn TableCatalog { get; }
    StringTableColumn TableSchema { get; }
    StringTableColumn TableName { get; }
}