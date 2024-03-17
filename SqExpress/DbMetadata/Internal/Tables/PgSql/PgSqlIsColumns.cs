namespace SqExpress.DbMetadata.Internal.Tables.PgSql;

internal class PgSqlIsColumns : TableBase, IPgSqlTableColumns
{
    public StringTableColumn TableCatalog { get; }

    public StringTableColumn TableSchema { get; }

    public StringTableColumn TableName { get; }

    public StringTableColumn ColumnName { get; }

    public Int32TableColumn OrdinalPosition { get; }

    public NullableStringTableColumn ColumnDefault { get; }

    public StringTableColumn IsNullable { get; }

    public StringTableColumn IsIdentity { get; }

    public NullableStringTableColumn DataType { get; }

    public NullableInt32TableColumn CharacterMaximumLength { get; }

    public NullableInt32TableColumn CharacterOctetLength { get; }

    public NullableByteTableColumn NumericPrecision { get; }

    public NullableInt32TableColumn NumericScale { get; }

    public NullableInt16TableColumn DatetimePrecision { get; }

    public NullableStringTableColumn CharacterSetName { get; }

    public PgSqlIsColumns(Alias alias = default)
        : base("INFORMATION_SCHEMA", "COLUMNS", alias)
    {
        this.TableCatalog = this.CreateStringColumn("TABLE_CATALOG", null);
        this.TableSchema = this.CreateStringColumn("TABLE_SCHEMA", null);
        this.TableName = this.CreateStringColumn("TABLE_NAME", null);
        this.ColumnName = this.CreateStringColumn("COLUMN_NAME", null);
        this.OrdinalPosition = this.CreateInt32Column("ORDINAL_POSITION");
        this.ColumnDefault = this.CreateNullableStringColumn("COLUMN_DEFAULT", null);
        this.IsNullable = this.CreateStringColumn("IS_NULLABLE", null);
        this.IsIdentity = this.CreateStringColumn("IS_IDENTITY", null);
        this.DataType = this.CreateNullableStringColumn("DATA_TYPE", null);
        this.CharacterMaximumLength = this.CreateNullableInt32Column("CHARACTER_MAXIMUM_LENGTH");
        this.CharacterOctetLength = this.CreateNullableInt32Column("CHARACTER_OCTET_LENGTH");
        this.NumericPrecision = this.CreateNullableByteColumn("NUMERIC_PRECISION");
        this.NumericScale = this.CreateNullableInt32Column("NUMERIC_SCALE");
        this.DatetimePrecision = this.CreateNullableInt16Column("DATETIME_PRECISION");
        this.CharacterSetName = this.CreateNullableStringColumn("CHARACTER_SET_NAME", null);
    }
}