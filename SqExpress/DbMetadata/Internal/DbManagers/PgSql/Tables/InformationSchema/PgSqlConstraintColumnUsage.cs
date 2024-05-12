using SqExpress.Syntax.Names;

namespace SqExpress.DbMetadata.Internal.DbManagers.PgSql.Tables.InformationSchema
{
    internal class PgSqlConstraintColumnUsage : TableBase, IPgSqlTableColumns
    {
        public PgSqlConstraintColumnUsage(Alias alias = default) : base("INFORMATION_SCHEMA", "CONSTRAINT_COLUMN_USAGE", alias)
        {
            TableCatalog = CreateStringColumn("TABLE_CATALOG", null);
            TableSchema = CreateStringColumn("TABLE_SCHEMA", null);
            TableName = CreateStringColumn("TABLE_NAME", null);
            ColumnName = CreateStringColumn("COLUMN_NAME", null);
            ConstraintCatalog = CreateStringColumn("CONSTRAINT_CATALOG", null);
            ConstraintSchema = CreateStringColumn("CONSTRAINT_SCHEMA", null);
            ConstraintName = CreateStringColumn("CONSTRAINT_NAME", null);
        }

        public StringTableColumn TableCatalog { get; }
        public StringTableColumn TableSchema { get; }
        public StringTableColumn TableName { get; }
        public StringTableColumn ColumnName { get; }
        public StringTableColumn ConstraintCatalog { get; }
        public StringTableColumn ConstraintSchema { get; }
        public StringTableColumn ConstraintName { get; }
    }
}
