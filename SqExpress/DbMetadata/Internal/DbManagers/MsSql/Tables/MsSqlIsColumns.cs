namespace SqExpress.DbMetadata.Internal.DbManagers.MsSql.Tables
{
    internal class MsSqlIsColumns : TableBase, IMsSqlTableColumns
    {
        public StringTableColumn TableCatalog { get; }

        public StringTableColumn TableSchema { get; }

        public StringTableColumn TableName { get; }

        public StringTableColumn ColumnName { get; }

        public Int32TableColumn OrdinalPosition { get; }

        public NullableStringTableColumn ColumnDefault { get; }

        public StringTableColumn IsNullable { get; }

        public NullableStringTableColumn DataType { get; }

        public NullableInt32TableColumn CharacterMaximumLength { get; }

        public NullableInt32TableColumn CharacterOctetLength { get; }

        public NullableByteTableColumn NumericPrecision { get; }

        public NullableInt32TableColumn NumericScale { get; }

        public NullableInt16TableColumn DatetimePrecision { get; }

        public NullableStringTableColumn CharacterSetName { get; }

        public MsSqlIsColumns(Alias alias = default)
            : base("INFORMATION_SCHEMA", "COLUMNS", alias)
        {
            TableCatalog = CreateStringColumn("TABLE_CATALOG", null);
            TableSchema = CreateStringColumn("TABLE_SCHEMA", null);
            TableName = CreateStringColumn("TABLE_NAME", null);
            ColumnName = CreateStringColumn("COLUMN_NAME", null);
            OrdinalPosition = CreateInt32Column("ORDINAL_POSITION");
            ColumnDefault = CreateNullableStringColumn("COLUMN_DEFAULT", null);
            IsNullable = CreateStringColumn("IS_NULLABLE", null);
            DataType = CreateNullableStringColumn("DATA_TYPE", null);
            CharacterMaximumLength = CreateNullableInt32Column("CHARACTER_MAXIMUM_LENGTH");
            CharacterOctetLength = CreateNullableInt32Column("CHARACTER_OCTET_LENGTH");
            NumericPrecision = CreateNullableByteColumn("NUMERIC_PRECISION");
            NumericScale = CreateNullableInt32Column("NUMERIC_SCALE");
            DatetimePrecision = CreateNullableInt16Column("DATETIME_PRECISION");
            CharacterSetName = CreateNullableStringColumn("CHARACTER_SET_NAME", null);
        }
    }
}