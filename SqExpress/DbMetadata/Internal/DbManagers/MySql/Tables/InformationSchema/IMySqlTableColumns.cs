using SqExpress.Syntax.Select;

namespace SqExpress.DbMetadata.Internal.DbManagers.MySql.Tables.InformationSchema
{
    internal interface IMySqlTableColumns : IExprTableSource
    {
        StringTableColumn TableCatalog { get; }
        StringTableColumn TableSchema { get; }
        StringTableColumn TableName { get; }
    }
}