namespace SqExpress.DbMetadata.Internal.Tables.MySQL
{
    internal class MySqlIsTables : TableBase
    {
        public readonly StringTableColumn TableCatalog;

        public readonly StringTableColumn TableSchema;

        public readonly StringTableColumn TableName;

        public readonly StringTableColumn TableType;

        public MySqlIsTables(Alias alias = default)
            : base("INFORMATION_SCHEMA", "", "TABLES", alias)
        {
            TableCatalog = CreateStringColumn("TABLE_CATALOG", null);
            TableSchema = CreateStringColumn("TABLE_SCHEMA", null);
            TableName = CreateStringColumn("TABLE_NAME", null);
            TableType = CreateStringColumn("TABLE_TYPE", null);
        }
    }
}