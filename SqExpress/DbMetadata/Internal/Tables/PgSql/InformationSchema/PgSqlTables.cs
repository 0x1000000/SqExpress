namespace SqExpress.DbMetadata.Internal.Tables.PgSql.InformationSchema
{
    internal class PgSqlTables : TableBase, IPgSqlTableColumns
    {
        public StringTableColumn TableCatalog { get; }

        public StringTableColumn TableSchema { get; }

        public StringTableColumn TableName { get; }

        public readonly StringTableColumn TableType;

        public PgSqlTables(Alias alias = default)
            : base("INFORMATION_SCHEMA", "TABLES", alias)
        {
            TableCatalog = CreateStringColumn("TABLE_CATALOG", null, true);
            TableSchema = CreateStringColumn("TABLE_SCHEMA", null, true);
            TableName = CreateStringColumn("TABLE_NAME", null, true);
            TableType = CreateStringColumn("TABLE_TYPE", null, true);
        }
    }
}