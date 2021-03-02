namespace SqExpress.CodeGenUtil.Tables.MsSql
{
    internal class MsSqlKeyColumnUsage : TableBase, IMsSqlTableColumns
    {
        public MsSqlKeyColumnUsage(Alias alias = default) : base("INFORMATION_SCHEMA", "KEY_COLUMN_USAGE", alias)
        {
            this.ConstraintCatalog = this.CreateStringColumn("CONSTRAINT_CATALOG", 128, true);
            this.ConstraintSchema = this.CreateStringColumn("CONSTRAINT_SCHEMA", 128, true);
            this.ConstraintName = this.CreateStringColumn("CONSTRAINT_NAME", 128, true);
            this.TableCatalog = this.CreateStringColumn("TABLE_CATALOG", 128, true);
            this.TableSchema = this.CreateStringColumn("TABLE_SCHEMA", 128, true);
            this.TableName = this.CreateStringColumn("TABLE_NAME", 128, true);
            this.ColumnName = this.CreateStringColumn("COLUMN_NAME", 128, true);
            this.OrdinalPosition = this.CreateInt32Column("ORDINAL_POSITION");
        }

        public StringTableColumn TableCatalog { get; }

        public StringTableColumn TableSchema { get; }

        public StringTableColumn TableName { get; }

        public StringTableColumn ConstraintCatalog { get; }

        public StringTableColumn ConstraintSchema { get; }

        public StringTableColumn ConstraintName { get; }

        public StringTableColumn ColumnName { get; }

        public Int32TableColumn OrdinalPosition { get; }
    }
}