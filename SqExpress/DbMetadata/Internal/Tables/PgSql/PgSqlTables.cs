namespace SqExpress.DbMetadata.Internal.Tables.PgSql
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
            this.TableCatalog = this.CreateStringColumn("TABLE_CATALOG", null, true);
            this.TableSchema = this.CreateStringColumn("TABLE_SCHEMA", null, true);
            this.TableName = this.CreateStringColumn("TABLE_NAME", null, true);
            this.TableType = this.CreateStringColumn("TABLE_TYPE", null, true);
        }
    }
}