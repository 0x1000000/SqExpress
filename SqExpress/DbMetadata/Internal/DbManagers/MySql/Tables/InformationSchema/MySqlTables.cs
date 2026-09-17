namespace SqExpress.DbMetadata.Internal.DbManagers.MySql.Tables.InformationSchema;

internal class MySqlTables : TableBase, IMySqlTableColumns
{
    public StringTableColumn TableCatalog { get; }
    public StringTableColumn TableSchema { get; }
    public StringTableColumn TableName { get; }
    public StringTableColumn TableType { get; }

    public MySqlTables(Alias alias = default) : base("INFORMATION_SCHEMA", string.Empty, "TABLES", alias)
    {
        TableCatalog = CreateStringColumn("TABLE_CATALOG", 512, true);
        TableSchema = CreateStringColumn("TABLE_SCHEMA", 64, true);
        TableName = CreateStringColumn("TABLE_NAME", 64, true);
        TableType = CreateStringColumn("TABLE_TYPE", 64, true);
    }
}
