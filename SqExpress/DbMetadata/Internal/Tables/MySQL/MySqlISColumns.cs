namespace SqExpress.DbMetadata.Internal.Tables.MySQL
{
    internal class MySqlISColumns : TableBase
    {
        public readonly StringTableColumn TableCatalog;

        public readonly StringTableColumn TableSchema;

        public readonly StringTableColumn TableName;

        public readonly StringTableColumn ColumnName;

        public readonly Int32TableColumn OrdinalPosition;

        public readonly NullableStringTableColumn ColumnDefault;

        public readonly StringTableColumn IsNullable;

        public readonly NullableStringTableColumn DataType;

        public readonly NullableInt64TableColumn CharacterMaximumLength;

        public readonly NullableInt64TableColumn CharacterOctetLength;

        public readonly NullableInt64TableColumn NumericPrecision;

        public readonly NullableInt64TableColumn DatetimePrecision;

        public readonly NullableStringTableColumn CharacterSetName;

        public MySqlISColumns(Alias alias = default)
            : base("INFORMATION_SCHEMA", "", "COLUMNS", alias)
        {
            TableCatalog = CreateStringColumn("TABLE_CATALOG", null);
            TableSchema = CreateStringColumn("TABLE_SCHEMA", null);
            TableName = CreateStringColumn("TABLE_NAME", null);
            ColumnName = CreateStringColumn("COLUMN_NAME", null);
            OrdinalPosition = CreateInt32Column("ORDINAL_POSITION");
            ColumnDefault = CreateNullableStringColumn("COLUMN_DEFAULT", null);
            IsNullable = CreateStringColumn("IS_NULLABLE", null);
            DataType = CreateNullableStringColumn("DATA_TYPE", null);
            CharacterMaximumLength = CreateNullableInt64Column("CHARACTER_MAXIMUM_LENGTH");
            CharacterOctetLength = CreateNullableInt64Column("CHARACTER_OCTET_LENGTH");
            NumericPrecision = CreateNullableInt64Column("NUMERIC_PRECISION");
            DatetimePrecision = CreateNullableInt64Column("DATETIME_PRECISION");
            CharacterSetName = CreateNullableStringColumn("CHARACTER_SET_NAME", null);
        }
    }
}