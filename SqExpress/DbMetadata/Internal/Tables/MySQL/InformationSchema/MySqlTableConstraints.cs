namespace SqExpress.DbMetadata.Internal.Tables.MySql.InformationSchema;

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
        this.ConstraintCatalog = this.CreateStringColumn("CONSTRAINT_CATALOG", 512, true);
        this.ConstraintSchema = this.CreateStringColumn("CONSTRAINT_SCHEMA", 64, true);
        this.ConstraintName = this.CreateStringColumn("CONSTRAINT_NAME", 64, true);

        this.TableCatalog = this.CreateStringColumn("TABLE_CATALOG", 64, true);
        this.TableSchema = this.CreateStringColumn("TABLE_SCHEMA", 64, true);
        this.TableName = this.CreateStringColumn("TABLE_NAME", 64, true);
        this.TableType = this.CreateStringColumn("TABLE_TYPE", 64, true);

        this.ConstraintType = this.CreateStringColumn("CONSTRAINT_TYPE", 64);
    }
}