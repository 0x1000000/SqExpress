namespace SqExpress.CodeGenUtil.Tables.MsSql
{
    internal class MsSqlTableConstraints : TableBase, IMsSqlTableColumns
    {
        public StringTableColumn ConstraintCatalog { get; set; }

        public StringTableColumn ConstraintSchema { get; set; }

        public StringTableColumn ConstraintName { get; set; }

        public StringTableColumn TableCatalog { get; }

        public StringTableColumn TableSchema { get; }

        public StringTableColumn TableName { get; }

        public StringTableColumn TableType { get; }

        public StringTableColumn InitiallyDeferred { get; }

        public StringTableColumn IsDeferrable { get; }

        public StringTableColumn ConstraintType { get; }

        public MsSqlTableConstraints(Alias alias = default)
            : base("INFORMATION_SCHEMA", "TABLE_CONSTRAINTS", alias)
        {

            this.ConstraintCatalog = this.CreateStringColumn("CONSTRAINT_CATALOG", 128, true);
            this.ConstraintSchema = this.CreateStringColumn("CONSTRAINT_SCHEMA", 128, true);
            this.ConstraintName = this.CreateStringColumn("CONSTRAINT_NAME", 128, true);

            this.TableCatalog = this.CreateStringColumn("TABLE_CATALOG", null, true);
            this.TableSchema = this.CreateStringColumn("TABLE_SCHEMA", null, true);
            this.TableName = this.CreateStringColumn("TABLE_NAME", null, true);
            this.TableType = this.CreateStringColumn("TABLE_TYPE", null, true);

            this.ConstraintType = this.CreateStringColumn("CONSTRAINT_TYPE", 11);
            this.IsDeferrable = this.CreateStringColumn("IS_DEFERRABLE", 2);
            this.InitiallyDeferred = this.CreateStringColumn("INITIALLY_DEFERRED", 2);
        }
    }
}