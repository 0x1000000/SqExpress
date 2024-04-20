using SqExpress.Syntax.Select;

namespace SqExpress.DbMetadata.Internal.Tables.PgSql.InformationSchema;

internal interface IPgSqlTableColumns : IExprTableSource
{
    StringTableColumn TableCatalog { get; }
    StringTableColumn TableSchema { get; }
    StringTableColumn TableName { get; }
}