namespace SqExpress.DbMetadata.Internal.Tables.MySQL.InformationSchema;

internal class MySqlColumns : TableBase, IMySqlTableColumns
{
    public StringTableColumn TableCatalog { get; }

    public StringTableColumn TableSchema { get; }

    public StringTableColumn TableName { get; }

    public StringTableColumn ColumnName { get; }

    public Int64TableColumn OrdinalPosition { get; }

    public NullableStringTableColumn ColumnDefault { get; }

    public StringTableColumn IsNullable { get; }

    public NullableStringTableColumn Extra { get; }

    public NullableStringTableColumn DataType { get; }

    public NullableInt64TableColumn CharacterMaximumLength { get; }

    public NullableInt64TableColumn CharacterOctetLength { get; }

    public NullableInt64TableColumn NumericPrecision { get; }

    public NullableInt64TableColumn NumericScale { get; }

    public NullableInt64TableColumn DatetimePrecision { get; }

    public NullableStringTableColumn CharacterSetName { get; }

    public MySqlColumns(Alias alias = default)
        : base("INFORMATION_SCHEMA", string.Empty, "COLUMNS", alias)
    {
        this.TableCatalog = this.CreateStringColumn("TABLE_CATALOG", 512, true);
        this.TableSchema = this.CreateStringColumn("TABLE_SCHEMA", 64, true);
        this.TableName = this.CreateStringColumn("TABLE_NAME", 64, true);
        this.ColumnName = this.CreateStringColumn("COLUMN_NAME", 64, true);
        this.OrdinalPosition = this.CreateInt64Column("ORDINAL_POSITION");
        this.ColumnDefault = this.CreateNullableStringColumn("COLUMN_DEFAULT", null);
        this.IsNullable = this.CreateStringColumn("IS_NULLABLE", 64, true);
        this.Extra = this.CreateNullableStringColumn("EXTRA", null);
        this.DataType = this.CreateNullableStringColumn("DATA_TYPE", 64);
        this.CharacterMaximumLength = this.CreateNullableInt64Column("CHARACTER_MAXIMUM_LENGTH");
        this.CharacterOctetLength = this.CreateNullableInt64Column("CHARACTER_OCTET_LENGTH");
        this.NumericPrecision = this.CreateNullableInt64Column("NUMERIC_PRECISION");
        this.NumericScale = this.CreateNullableInt64Column("NUMERIC_SCALE");
        this.DatetimePrecision = this.CreateNullableInt64Column("DATETIME_PRECISION");
        this.CharacterSetName = this.CreateNullableStringColumn("CHARACTER_SET_NAME", 32);
    }
}