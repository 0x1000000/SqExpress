using SqExpress.Syntax.Select;

namespace SqExpress.DbMetadata.Internal.DbManagers.PgSql.Tables.InformationSchema
{
    internal interface IPgSqlTableColumns : IExprTableSource
    {
        StringTableColumn TableCatalog { get; }
        StringTableColumn TableSchema { get; }
        StringTableColumn TableName { get; }
    }
}