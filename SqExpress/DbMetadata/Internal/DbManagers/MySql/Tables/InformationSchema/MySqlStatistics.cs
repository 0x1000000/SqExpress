namespace SqExpress.DbMetadata.Internal.DbManagers.MySql.Tables.InformationSchema
{
    public class MySqlStatistics : TableBase, IMySqlTableColumns
    {
        public StringTableColumn TableCatalog { get; }

        public StringTableColumn TableSchema { get; }

        public StringTableColumn TableName { get; }

        public Int64TableColumn NonUnique { get; }

        public StringTableColumn IndexSchema { get; }

        public StringTableColumn IndexName { get; }

        public Int64TableColumn SeqInIndex { get; }

        public StringTableColumn ColumnName { get; }

        public StringTableColumn Collation { get; }

        public MySqlStatistics(Alias alias = default)
            : base("INFORMATION_SCHEMA", string.Empty, "STATISTICS", alias)
        {
            TableCatalog = CreateStringColumn("TABLE_CATALOG", 512, true);
            TableSchema = CreateStringColumn("TABLE_SCHEMA", 64, true);
            TableName = CreateStringColumn("TABLE_NAME", 64, true);
            NonUnique = CreateInt64Column("NON_UNIQUE");
            IndexSchema = CreateStringColumn("INDEX_SCHEMA", 64, true);
            IndexName = CreateStringColumn("INDEX_NAME", 64, true);
            SeqInIndex = CreateInt64Column("SEQ_IN_INDEX");
            ColumnName = CreateStringColumn("COLUMN_NAME", 64, true);
            Collation = CreateStringColumn("COLLATION", 64, true);
        }

    }
}
