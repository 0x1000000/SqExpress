namespace SqExpress.DbMetadata.Internal.DbManagers.MySql.Tables.InformationSchema
{
    internal class MySqlKeyColumnUsage : TableBase, IMySqlTableColumns
    {
        public MySqlKeyColumnUsage(Alias alias = default) : base("INFORMATION_SCHEMA", string.Empty, "KEY_COLUMN_USAGE", alias)
        {
            ConstraintCatalog = CreateStringColumn("CONSTRAINT_CATALOG", 512, true);
            ConstraintSchema = CreateStringColumn("CONSTRAINT_SCHEMA", 64, true);
            ConstraintName = CreateStringColumn("CONSTRAINT_NAME", 64, true);
            TableCatalog = CreateStringColumn("TABLE_CATALOG", 512, true);
            TableSchema = CreateStringColumn("TABLE_SCHEMA", 64, true);
            TableName = CreateStringColumn("TABLE_NAME", 64, true);
            ColumnName = CreateStringColumn("COLUMN_NAME", 64, true);
            OrdinalPosition = CreateInt64Column("ORDINAL_POSITION");
            PositionInUniqueConstraint = CreateNullableInt64Column("POSITION_IN_UNIQUE_CONSTRAINT");
            ReferencedTableSchema = CreateNullableStringColumn("REFERENCED_TABLE_SCHEMA", 64, true);
            ReferencedTableName = CreateNullableStringColumn("REFERENCED_TABLE_NAME", 64, true);
            ReferencedColumnName = CreateNullableStringColumn("REFERENCED_COLUMN_NAME", 64, true);
        }

        public StringTableColumn TableCatalog { get; }

        public StringTableColumn TableSchema { get; }

        public StringTableColumn TableName { get; }

        public StringTableColumn ConstraintCatalog { get; }

        public StringTableColumn ConstraintSchema { get; }

        public StringTableColumn ConstraintName { get; }

        public StringTableColumn ColumnName { get; }

        public Int64TableColumn OrdinalPosition { get; }

        public NullableStringTableColumn ReferencedColumnName { get; set; }

        public NullableStringTableColumn ReferencedTableName { get; set; }

        public NullableStringTableColumn ReferencedTableSchema { get; set; }

        public NullableInt64TableColumn PositionInUniqueConstraint { get; }
    }
}
