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
        /// <summary>Creates a bare column name for APIs that infer or do not require a CLR/SQL value type.</summary>
        /// <param name="columnName">The database column name.</param>
        /// <returns>An untyped column reference.</returns>
        public static ExprColumnName Any(string columnName) => new ExprColumnName(columnName);

        /// <summary>Creates an unqualified, non-nullable Boolean reference for a dynamically shaped result source.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static BooleanCustomColumn Boolean(string columnName) => new BooleanCustomColumn(columnName);
        /// <summary>Creates an unqualified nullable Boolean reference that preserves SQL <c>NULL</c> while reading results.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableBooleanCustomColumn NullableBoolean(string columnName) => new NullableBooleanCustomColumn(columnName);
        /// <summary>Creates an unqualified byte reference using SqExpress's portable unsigned-byte semantics.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static ByteCustomColumn Byte(string columnName) => new ByteCustomColumn(columnName);
        /// <summary>Creates an unqualified nullable byte reference using portable unsigned-byte semantics.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableByteCustomColumn NullableByte(string columnName) => new NullableByteCustomColumn(columnName);
        /// <summary>Creates an unqualified binary reference with byte-array result-reading operations.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static ByteArrayCustomColumn ByteArray(string columnName) => new ByteArrayCustomColumn(columnName);
        /// <summary>Creates an unqualified nullable binary reference with byte-array/stream result-reading operations.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableByteArrayCustomColumn NullableByteArray(string columnName) => new NullableByteArrayCustomColumn(columnName);
        /// <summary>Creates an unqualified 16-bit integer reference for a derived or dynamic result column.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static Int16CustomColumn Int16(string columnName) => new Int16CustomColumn(columnName);
        /// <summary>Creates an unqualified nullable 16-bit integer reference for a derived or dynamic result column.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableInt16CustomColumn NullableInt16(string columnName) => new NullableInt16CustomColumn(columnName);
        /// <summary>Creates an unqualified 32-bit integer reference for a derived or dynamic result column.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static Int32CustomColumn Int32(string columnName) => new Int32CustomColumn(columnName);
        /// <summary>Creates an unqualified nullable 32-bit integer reference for a derived or dynamic result column.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableInt32CustomColumn NullableInt32(string columnName) => new NullableInt32CustomColumn(columnName);
        /// <summary>Creates an unqualified 64-bit integer reference for a derived or dynamic result column.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static Int64CustomColumn Int64(string columnName) => new Int64CustomColumn(columnName);
        /// <summary>Creates an unqualified nullable 64-bit integer reference for a derived or dynamic result column.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableInt64CustomColumn NullableInt64(string columnName) => new NullableInt64CustomColumn(columnName);
        /// <summary>Creates an unqualified exact-numeric reference with decimal expression and reader semantics.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static DecimalCustomColumn Decimal(string columnName) => new DecimalCustomColumn(columnName);
        /// <summary>Creates an unqualified nullable exact-numeric reference with decimal reader semantics.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableDecimalCustomColumn NullableDecimal(string columnName) => new NullableDecimalCustomColumn(columnName);
        /// <summary>Creates an unqualified approximate-numeric reference with double-precision reader semantics.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static DoubleCustomColumn Double(string columnName) => new DoubleCustomColumn(columnName);
        /// <summary>Creates an unqualified nullable approximate-numeric reference with double-precision reader semantics.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableDoubleCustomColumn NullableDouble(string columnName) => new NullableDoubleCustomColumn(columnName);
        /// <summary>Creates an unqualified date/time reference for a derived or dynamic result column.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static DateTimeCustomColumn DateTime(string columnName) => new DateTimeCustomColumn(columnName);
        /// <summary>Creates an unqualified nullable date/time reference that preserves SQL <c>NULL</c> while reading.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableDateTimeCustomColumn NullableDateTime(string columnName) => new NullableDateTimeCustomColumn(columnName);
        /// <summary>Creates an unqualified offset-aware temporal reference for a derived or dynamic result column.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static DateTimeOffsetCustomColumn DateTimeOffset(string columnName) => new DateTimeOffsetCustomColumn(columnName);
        /// <summary>Creates an unqualified nullable offset-aware temporal reference for dynamic results.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableDateTimeOffsetCustomColumn NullableDateTimeOffset(string columnName) => new NullableDateTimeOffsetCustomColumn(columnName);
        /// <summary>Creates an unqualified GUID/UUID reference using the selected dialect's compatible representation.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static GuidCustomColumn Guid(string columnName) => new GuidCustomColumn(columnName);
        /// <summary>Creates an unqualified nullable GUID/UUID reference for a derived or dynamic result column.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableGuidCustomColumn NullableGuid(string columnName) => new NullableGuidCustomColumn(columnName);
        /// <summary>Creates an unqualified string reference with SQL comparison, concatenation, and reader operations.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static StringCustomColumn String(string columnName) => new StringCustomColumn(columnName);
        /// <summary>Creates an unqualified nullable string reference that preserves SQL <c>NULL</c> while reading.</summary>
        /// <param name="columnName">The database column name.</param><returns>The typed column reference.</returns>
        public static NullableStringCustomColumn NullableString(string columnName) => new NullableStringCustomColumn(columnName);
    }
}
