namespace SqExpress.DbMetadata.Internal.DbManagers.MySql.Tables.InformationSchema
{
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
            TableCatalog = CreateStringColumn("TABLE_CATALOG", 512, true);
            TableSchema = CreateStringColumn("TABLE_SCHEMA", 64, true);
            TableName = CreateStringColumn("TABLE_NAME", 64, true);
            ColumnName = CreateStringColumn("COLUMN_NAME", 64, true);
            OrdinalPosition = CreateInt64Column("ORDINAL_POSITION");
            ColumnDefault = CreateNullableStringColumn("COLUMN_DEFAULT", null);
            IsNullable = CreateStringColumn("IS_NULLABLE", 64, true);
            Extra = CreateNullableStringColumn("EXTRA", null);
            DataType = CreateNullableStringColumn("DATA_TYPE", 64);
            CharacterMaximumLength = CreateNullableInt64Column("CHARACTER_MAXIMUM_LENGTH");
            CharacterOctetLength = CreateNullableInt64Column("CHARACTER_OCTET_LENGTH");
            NumericPrecision = CreateNullableInt64Column("NUMERIC_PRECISION");
            NumericScale = CreateNullableInt64Column("NUMERIC_SCALE");
            DatetimePrecision = CreateNullableInt64Column("DATETIME_PRECISION");
            CharacterSetName = CreateNullableStringColumn("CHARACTER_SET_NAME", 32);
        }
    }
}