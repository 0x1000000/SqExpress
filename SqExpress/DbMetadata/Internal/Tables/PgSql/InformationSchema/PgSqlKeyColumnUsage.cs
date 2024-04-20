namespace SqExpress.DbMetadata.Internal.Tables.PgSql.InformationSchema;

internal class PgSqlKeyColumnUsage : TableBase
{
    public PgSqlKeyColumnUsage(Alias alias = default) : base("INFORMATION_SCHEMA", "KEY_COLUMN_USAGE", alias)
    {
        ConstraintCatalog = CreateStringColumn("CONSTRAINT_CATALOG", 128, true);
        ConstraintSchema = CreateStringColumn("CONSTRAINT_SCHEMA", 128, true);
        ConstraintName = CreateStringColumn("CONSTRAINT_NAME", 128, true);
        TableCatalog = CreateStringColumn("TABLE_CATALOG", 128, true);
        TableSchema = CreateStringColumn("TABLE_SCHEMA", 128, true);
        TableName = CreateStringColumn("TABLE_NAME", 128, true);
        ColumnName = CreateStringColumn("COLUMN_NAME", 128, true);
        OrdinalPosition = CreateInt32Column("ORDINAL_POSITION");
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
