namespace SqExpress.CodeGenUtil.Tables.MySQL
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
            : base("INFORMATION_SCHEMA","", "COLUMNS", alias)
        {
            this.TableCatalog = this.CreateStringColumn("TABLE_CATALOG", null);
            this.TableSchema = this.CreateStringColumn("TABLE_SCHEMA", null);
            this.TableName = this.CreateStringColumn("TABLE_NAME", null);
            this.ColumnName = this.CreateStringColumn("COLUMN_NAME", null);
            this.OrdinalPosition = this.CreateInt32Column("ORDINAL_POSITION");
            this.ColumnDefault = this.CreateNullableStringColumn("COLUMN_DEFAULT", null);
            this.IsNullable = this.CreateStringColumn("IS_NULLABLE", null);
            this.DataType = this.CreateNullableStringColumn("DATA_TYPE", null);
            this.CharacterMaximumLength = this.CreateNullableInt64Column("CHARACTER_MAXIMUM_LENGTH");
            this.CharacterOctetLength = this.CreateNullableInt64Column("CHARACTER_OCTET_LENGTH");
            this.NumericPrecision = this.CreateNullableInt64Column("NUMERIC_PRECISION");
            this.DatetimePrecision = this.CreateNullableInt64Column("DATETIME_PRECISION");
            this.CharacterSetName = this.CreateNullableStringColumn("CHARACTER_SET_NAME", null);
        }
    }
}