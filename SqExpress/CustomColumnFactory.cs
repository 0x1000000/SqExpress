using SqExpress.Syntax.Names;

namespace SqExpress
{
    /// <summary>
    /// Creates typed column references when no <see cref="TableBase"/> descriptor is available.
    /// </summary>
    /// <remarks>
    /// Custom columns are useful for addressing columns returned by derived SQL, database functions, or
    /// dynamically shaped queries. Choose the nullable factory when the database value may be null; the
    /// selected CLR type controls expression operators and result-reader methods available to the caller.
    /// </remarks>
    /// <example>
    /// <code>
    /// var total = CustomColumnFactory.Decimal("Total");
    /// var label = CustomColumnFactory.NullableString("Label");
    /// var query = SqQueryBuilder.Select(total, label).From(source);
    /// </code>
    /// </example>
    public static class CustomColumnFactory
    {
        /// <summary>Creates an untyped column-name expression.</summary>
        /// <param name="columnName">The database column name.</param>
        /// <returns>An untyped column reference.</returns>
        public static ExprColumnName Any(string columnName) => new ExprColumnName(columnName);

        /// <summary>Creates a non-nullable Boolean column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static BooleanCustomColumn Boolean(string columnName) => new BooleanCustomColumn(columnName);
        /// <summary>Creates a nullable Boolean column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableBooleanCustomColumn NullableBoolean(string columnName) => new NullableBooleanCustomColumn(columnName);
        /// <summary>Creates a non-nullable byte column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static ByteCustomColumn Byte(string columnName) => new ByteCustomColumn(columnName);
        /// <summary>Creates a nullable byte column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableByteCustomColumn NullableByte(string columnName) => new NullableByteCustomColumn(columnName);
        /// <summary>Creates a non-nullable binary column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static ByteArrayCustomColumn ByteArray(string columnName) => new ByteArrayCustomColumn(columnName);
        /// <summary>Creates a nullable binary column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableByteArrayCustomColumn NullableByteArray(string columnName) => new NullableByteArrayCustomColumn(columnName);
        /// <summary>Creates a non-nullable 16-bit integer column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static Int16CustomColumn Int16(string columnName) => new Int16CustomColumn(columnName);
        /// <summary>Creates a nullable 16-bit integer column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableInt16CustomColumn NullableInt16(string columnName) => new NullableInt16CustomColumn(columnName);
        /// <summary>Creates a non-nullable 32-bit integer column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static Int32CustomColumn Int32(string columnName) => new Int32CustomColumn(columnName);
        /// <summary>Creates a nullable 32-bit integer column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableInt32CustomColumn NullableInt32(string columnName) => new NullableInt32CustomColumn(columnName);
        /// <summary>Creates a non-nullable 64-bit integer column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static Int64CustomColumn Int64(string columnName) => new Int64CustomColumn(columnName);
        /// <summary>Creates a nullable 64-bit integer column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableInt64CustomColumn NullableInt64(string columnName) => new NullableInt64CustomColumn(columnName);
        /// <summary>Creates a non-nullable decimal column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static DecimalCustomColumn Decimal(string columnName) => new DecimalCustomColumn(columnName);
        /// <summary>Creates a nullable decimal column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableDecimalCustomColumn NullableDecimal(string columnName) => new NullableDecimalCustomColumn(columnName);
        /// <summary>Creates a non-nullable double-precision column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static DoubleCustomColumn Double(string columnName) => new DoubleCustomColumn(columnName);
        /// <summary>Creates a nullable double-precision column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableDoubleCustomColumn NullableDouble(string columnName) => new NullableDoubleCustomColumn(columnName);
        /// <summary>Creates a non-nullable date and time column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static DateTimeCustomColumn DateTime(string columnName) => new DateTimeCustomColumn(columnName);
        /// <summary>Creates a nullable date and time column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableDateTimeCustomColumn NullableDateTime(string columnName) => new NullableDateTimeCustomColumn(columnName);
        /// <summary>Creates a non-nullable date, time, and offset column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static DateTimeOffsetCustomColumn DateTimeOffset(string columnName) => new DateTimeOffsetCustomColumn(columnName);
        /// <summary>Creates a nullable date, time, and offset column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableDateTimeOffsetCustomColumn NullableDateTimeOffset(string columnName) => new NullableDateTimeOffsetCustomColumn(columnName);
        /// <summary>Creates a non-nullable GUID column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static GuidCustomColumn Guid(string columnName) => new GuidCustomColumn(columnName);
        /// <summary>Creates a nullable GUID column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableGuidCustomColumn NullableGuid(string columnName) => new NullableGuidCustomColumn(columnName);
        /// <summary>Creates a non-nullable string column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static StringCustomColumn String(string columnName) => new StringCustomColumn(columnName);
        /// <summary>Creates a nullable string column reference.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableStringCustomColumn NullableString(string columnName) => new NullableStringCustomColumn(columnName);
    }
}
