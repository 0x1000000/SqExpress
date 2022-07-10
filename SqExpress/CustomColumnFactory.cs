using SqExpress.Syntax.Names;

namespace SqExpress
{
    public static class CustomColumnFactory
    {
        public static ExprColumnName Any(string columnName) => new ExprColumnName(columnName);

        public static BooleanCustomColumn Boolean(string columnName) => new BooleanCustomColumn(columnName);
        public static NullableBooleanCustomColumn NullableBoolean(string columnName) => new NullableBooleanCustomColumn(columnName);
        public static ByteCustomColumn Byte(string columnName) => new ByteCustomColumn(columnName);
        public static NullableByteCustomColumn NullableByte(string columnName) => new NullableByteCustomColumn(columnName);
        public static ByteArrayCustomColumn ByteArray(string columnName) => new ByteArrayCustomColumn(columnName);
        public static NullableByteArrayCustomColumn NullableByteArray(string columnName) => new NullableByteArrayCustomColumn(columnName);
        public static Int16CustomColumn Int16(string columnName) => new Int16CustomColumn(columnName);
        public static NullableInt16CustomColumn NullableInt16(string columnName) => new NullableInt16CustomColumn(columnName);
        public static Int32CustomColumn Int32(string columnName) => new Int32CustomColumn(columnName);
        public static NullableInt32CustomColumn NullableInt32(string columnName) => new NullableInt32CustomColumn(columnName);
        public static Int64CustomColumn Int64(string columnName) => new Int64CustomColumn(columnName);
        public static NullableInt64CustomColumn NullableInt64(string columnName) => new NullableInt64CustomColumn(columnName);
        public static DecimalCustomColumn Decimal(string columnName) => new DecimalCustomColumn(columnName);
        public static NullableDecimalCustomColumn NullableDecimal(string columnName) => new NullableDecimalCustomColumn(columnName);
        public static DoubleCustomColumn Double(string columnName) => new DoubleCustomColumn(columnName);
        public static NullableDoubleCustomColumn NullableDouble(string columnName) => new NullableDoubleCustomColumn(columnName);
        public static DateTimeCustomColumn DateTime(string columnName) => new DateTimeCustomColumn(columnName);
        public static NullableDateTimeCustomColumn NullableDateTime(string columnName) => new NullableDateTimeCustomColumn(columnName);
        public static DateTimeOffsetCustomColumn DateTimeOffset(string columnName) => new DateTimeOffsetCustomColumn(columnName);
        public static NullableDateTimeOffsetCustomColumn NullableDateTimeOffset(string columnName) => new NullableDateTimeOffsetCustomColumn(columnName);
        public static GuidCustomColumn Guid(string columnName) => new GuidCustomColumn(columnName);
        public static NullableGuidCustomColumn NullableGuid(string columnName) => new NullableGuidCustomColumn(columnName);
        public static StringCustomColumn String(string columnName) => new StringCustomColumn(columnName);
        public static NullableStringCustomColumn NullableString(string columnName) => new NullableStringCustomColumn(columnName);
    }
}