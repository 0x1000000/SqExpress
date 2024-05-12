using SqExpress.DbMetadata.Internal.Tables.MySQL.InformationSchema;

namespace SqExpress.DbMetadata.Internal.Tables.MySql.InformationSchema;

internal class MySqlKeyColumnUsage : TableBase, IMySqlTableColumns
{
    public MySqlKeyColumnUsage(Alias alias = default) : base("INFORMATION_SCHEMA", string.Empty, "KEY_COLUMN_USAGE", alias)
    {
        this.ConstraintCatalog = this.CreateStringColumn("CONSTRAINT_CATALOG", 512, true);
        this.ConstraintSchema = this.CreateStringColumn("CONSTRAINT_SCHEMA", 64, true);
        this.ConstraintName = this.CreateStringColumn("CONSTRAINT_NAME", 64, true);
        this.TableCatalog = this.CreateStringColumn("TABLE_CATALOG", 512, true);
        this.TableSchema = this.CreateStringColumn("TABLE_SCHEMA", 64, true);
        this.TableName = this.CreateStringColumn("TABLE_NAME", 64, true);
        this.ColumnName = this.CreateStringColumn("COLUMN_NAME", 64, true);
        this.OrdinalPosition = this.CreateInt64Column("ORDINAL_POSITION");
        this.PositionInUniqueConstraint = this.CreateNullableInt64Column("POSITION_IN_UNIQUE_CONSTRAINT");
        this.ReferencedTableSchema = this.CreateNullableStringColumn("REFERENCED_TABLE_SCHEMA", 64, true);
        this.ReferencedTableName = this.CreateNullableStringColumn("REFERENCED_TABLE_NAME", 64, true);
        this.ReferencedColumnName = this.CreateNullableStringColumn("REFERENCED_COLUMN_NAME", 64, true);
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
