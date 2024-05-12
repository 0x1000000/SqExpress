namespace SqExpress.DbMetadata.Internal.DbManagers.MySql.Tables.InformationSchema
{
    internal class MySqlTableConstraints : TableBase
    {
        public StringTableColumn ConstraintCatalog { get; set; }

        public StringTableColumn ConstraintSchema { get; set; }

        public StringTableColumn ConstraintName { get; set; }

        public StringTableColumn TableCatalog { get; }

        public StringTableColumn TableSchema { get; }

        public StringTableColumn TableName { get; }

        public StringTableColumn TableType { get; }

        public StringTableColumn ConstraintType { get; }

        public MySqlTableConstraints(Alias alias = default)
            : base("INFORMATION_SCHEMA", string.Empty, "TABLE_CONSTRAINTS", alias)
        {
            ConstraintCatalog = CreateStringColumn("CONSTRAINT_CATALOG", 512, true);
            ConstraintSchema = CreateStringColumn("CONSTRAINT_SCHEMA", 64, true);
            ConstraintName = CreateStringColumn("CONSTRAINT_NAME", 64, true);

            TableCatalog = CreateStringColumn("TABLE_CATALOG", 64, true);
            TableSchema = CreateStringColumn("TABLE_SCHEMA", 64, true);
            TableName = CreateStringColumn("TABLE_NAME", 64, true);
            TableType = CreateStringColumn("TABLE_TYPE", 64, true);

            ConstraintType = CreateStringColumn("CONSTRAINT_TYPE", 64);
        }
    }
}