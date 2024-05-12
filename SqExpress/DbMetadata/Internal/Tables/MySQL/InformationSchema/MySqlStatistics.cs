namespace SqExpress.DbMetadata.Internal.Tables.MySQL.InformationSchema;

public class MySqlStatistics : TableBase, IMySqlTableColumns
{
    public StringTableColumn TableCatalog { get; }

    public StringTableColumn TableSchema { get; }

    public StringTableColumn TableName { get; }

    public Int64TableColumn NonUnique { get; }

    public StringTableColumn IndexSchema { get; }

    public StringTableColumn IndexName { get; }

    public Int64TableColumn SeqInIndex { get; }

    public StringTableColumn ColumnName { get; }

    public StringTableColumn Collation { get; }

    public MySqlStatistics(Alias alias = default)
    : base("INFORMATION_SCHEMA", string.Empty, "STATISTICS", alias)
    {
        this.TableCatalog = this.CreateStringColumn("TABLE_CATALOG", 512, true);
        this.TableSchema = this.CreateStringColumn("TABLE_SCHEMA", 64, true);
        this.TableName = this.CreateStringColumn("TABLE_NAME", 64, true);
        this.NonUnique = this.CreateInt64Column("NON_UNIQUE");
        this.IndexSchema = this.CreateStringColumn("INDEX_SCHEMA", 64, true);
        this.IndexName = this.CreateStringColumn("INDEX_NAME", 64, true);
        this.SeqInIndex = this.CreateInt64Column("SEQ_IN_INDEX");
        this.ColumnName = this.CreateStringColumn("COLUMN_NAME", 64, true);
        this.Collation = this.CreateStringColumn("COLLATION", 64, true);
    }

}
