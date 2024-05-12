using SqExpress.Syntax.Names;

namespace SqExpress.DbMetadata.Internal.Tables.PgSql.InformationSchema;

internal class PgSqlConstraintColumnUsage : TableBase, IPgSqlTableColumns
{
    public PgSqlConstraintColumnUsage(Alias alias = default) : base("INFORMATION_SCHEMA", "CONSTRAINT_COLUMN_USAGE", alias)
    {
        this.TableCatalog = this.CreateStringColumn("TABLE_CATALOG", null);
        this.TableSchema = this.CreateStringColumn("TABLE_SCHEMA", null);
        this.TableName = this.CreateStringColumn("TABLE_NAME", null);
        this.ColumnName = this.CreateStringColumn("COLUMN_NAME", null);
        this.ConstraintCatalog = this.CreateStringColumn("CONSTRAINT_CATALOG", null);
        this.ConstraintSchema = this.CreateStringColumn("CONSTRAINT_SCHEMA", null);
        this.ConstraintName = this.CreateStringColumn("CONSTRAINT_NAME", null);
    }

    public StringTableColumn TableCatalog { get; }
    public StringTableColumn TableSchema { get; }
    public StringTableColumn TableName { get; }
    public StringTableColumn ColumnName { get; }
    public StringTableColumn ConstraintCatalog { get; }
    public StringTableColumn ConstraintSchema { get; }
    public StringTableColumn ConstraintName { get; }
}
