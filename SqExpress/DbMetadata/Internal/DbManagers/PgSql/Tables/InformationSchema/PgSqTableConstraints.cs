namespace SqExpress.DbMetadata.Internal.DbManagers.PgSql.Tables.InformationSchema
{
    internal class PgSqlTableConstraints : TableBase
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

        public PgSqlTableConstraints(Alias alias = default)
            : base("INFORMATION_SCHEMA", "TABLE_CONSTRAINTS", alias)
        {
            ConstraintCatalog = CreateStringColumn("CONSTRAINT_CATALOG", 128, true);
            ConstraintSchema = CreateStringColumn("CONSTRAINT_SCHEMA", 128, true);
            ConstraintName = CreateStringColumn("CONSTRAINT_NAME", 128, true);

            TableCatalog = CreateStringColumn("TABLE_CATALOG", null, true);
            TableSchema = CreateStringColumn("TABLE_SCHEMA", null, true);
            TableName = CreateStringColumn("TABLE_NAME", null, true);
            TableType = CreateStringColumn("TABLE_TYPE", null, true);

            ConstraintType = CreateStringColumn("CONSTRAINT_TYPE", 11);
            IsDeferrable = CreateStringColumn("IS_DEFERRABLE", 2);
            InitiallyDeferred = CreateStringColumn("INITIALLY_DEFERRED", 2);
        }
    }
}