using System;
using System.IO;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Names;

namespace SqExpress
{
    public class BooleanCustomColumn : ExprColumn
    {
        internal BooleanCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal BooleanCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public bool Read(ISqDataRecordReader recordReader) => recordReader.GetBoolean(this.ColumnName.Name);

        public bool Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetBoolean(customColumnName);

        public bool Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetBoolean(ordinal);
    }
    public class NullableBooleanCustomColumn : ExprColumn
    {
        internal NullableBooleanCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableBooleanCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public bool? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableBoolean(this.ColumnName.Name);

        public bool? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableBoolean(customColumnName);

        public bool? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetBoolean(ordinal) : null;
    }
    public class ByteCustomColumn : ExprColumn
    {
        internal ByteCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal ByteCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public byte Read(ISqDataRecordReader recordReader) => recordReader.GetByte(this.ColumnName.Name);

        public byte Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetByte(customColumnName);

        public byte Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetByte(ordinal);
    }
    public class NullableByteCustomColumn : ExprColumn
    {
        internal NullableByteCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableByteCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public byte? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableByte(this.ColumnName.Name);

        public byte? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableByte(customColumnName);

        public byte? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetByte(ordinal) : null;
    }
    public class ByteArrayCustomColumn : ExprColumn
    {
        internal ByteArrayCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal ByteArrayCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public byte[] Read(ISqDataRecordReader recordReader) => recordReader.GetByteArray(this.ColumnName.Name);

        public byte[] Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetByteArray(customColumnName);

        public byte[] Read(ISqDataRecordReader recordReader, int ordinal) => (byte[])recordReader.GetValue(ordinal);

        public Stream GetStream(ISqDataRecordReader recordReader) => recordReader.GetStream(this.ColumnName.Name);
    }
    public class NullableByteArrayCustomColumn : ExprColumn
    {
        internal NullableByteArrayCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableByteArrayCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public byte? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableByte(this.ColumnName.Name);

        public byte? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableByte(customColumnName);

        public byte[]? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? (byte[])recordReader.GetValue(ordinal) : null;

        public Stream? GetStream(ISqDataRecordReader recordReader) => recordReader.GetNullableStream(this.ColumnName.Name);
    }
    public class Int16CustomColumn : ExprColumn
    {
        internal Int16CustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal Int16CustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public short Read(ISqDataRecordReader recordReader) => recordReader.GetInt16(this.ColumnName.Name);

        public short Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetInt16(customColumnName);

        public short Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetInt16(ordinal);
    }
    public class NullableInt16CustomColumn : ExprColumn
    {
        internal NullableInt16CustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableInt16CustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public short? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableInt16(this.ColumnName.Name);

        public short? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableInt16(customColumnName);

        public short? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetInt16(ordinal) : null;
    }
    public class Int32CustomColumn : ExprColumn
    {
        internal Int32CustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal Int32CustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public int Read(ISqDataRecordReader recordReader) => recordReader.GetInt32(this.ColumnName.Name);

        public int Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetInt32(customColumnName);

        public int Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetInt32(ordinal);
    }
    public class NullableInt32CustomColumn : ExprColumn
    {
        internal NullableInt32CustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }
        
        internal NullableInt32CustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public int? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableInt32(this.ColumnName.Name);

        public int? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableInt32(customColumnName);

        public int? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetInt32(ordinal) : null;
    }
    public class Int64CustomColumn : ExprColumn
    {
        internal Int64CustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }
        
        internal Int64CustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public long Read(ISqDataRecordReader recordReader) => recordReader.GetInt64(this.ColumnName.Name);

        public long Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetInt64(customColumnName);

        public long Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetInt64(ordinal);
    }
    public class NullableInt64CustomColumn : ExprColumn
    {
        internal NullableInt64CustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }
        
        internal NullableInt64CustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public long? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableInt64(this.ColumnName.Name);

        public long? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableInt64(customColumnName);

        public long? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetInt64(ordinal) : null;
    }
    public class DecimalCustomColumn : ExprColumn
    {
        internal DecimalCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal DecimalCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public decimal Read(ISqDataRecordReader recordReader) => recordReader.GetDecimal(this.ColumnName.Name);

        public decimal Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetDecimal(customColumnName);

        public decimal Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetDecimal(ordinal);
    }
    public class NullableDecimalCustomColumn : ExprColumn
    {
        internal NullableDecimalCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableDecimalCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public decimal? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableDecimal(this.ColumnName.Name);

        public decimal? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableDecimal(customColumnName);

        public decimal? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetDecimal(ordinal) : null;
    }
    public class DoubleCustomColumn : ExprColumn
    {
        internal DoubleCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal DoubleCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public double Read(ISqDataRecordReader recordReader) => recordReader.GetDouble(this.ColumnName.Name);

        public double Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetDouble(customColumnName);

        public double Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetDouble(ordinal);
    }
    public class NullableDoubleCustomColumn : ExprColumn
    {
        internal NullableDoubleCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableDoubleCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public double? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableDouble(this.ColumnName.Name);

        public double? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableDouble(customColumnName);

        public double? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetDouble(ordinal) : null;
    }
    public class DateTimeCustomColumn : ExprColumn
    {
        internal DateTimeCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal DateTimeCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public DateTime Read(ISqDataRecordReader recordReader) => recordReader.GetDateTime(this.ColumnName.Name);

        public DateTime Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetDateTime(customColumnName);

        public DateTime Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetDateTime(ordinal);
    }
    public class NullableDateTimeCustomColumn : ExprColumn
    {
        internal NullableDateTimeCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableDateTimeCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public DateTime? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableDateTime(this.ColumnName.Name);

        public DateTime? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableDateTime(customColumnName);

        public DateTime? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetDateTime(ordinal) : null;
    }
    public class DateTimeOffsetCustomColumn : ExprColumn
    {
        internal DateTimeOffsetCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal DateTimeOffsetCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public DateTimeOffset Read(ISqDataRecordReader recordReader) => recordReader.GetDateTimeOffset(this.ColumnName.Name);

        public DateTimeOffset Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetDateTimeOffset(customColumnName);

        public DateTimeOffset Read(ISqDataRecordReader recordReader, int ordinal) => (DateTimeOffset)recordReader.GetValue(ordinal);
    }
    public class NullableDateTimeOffsetCustomColumn : ExprColumn
    {
        internal NullableDateTimeOffsetCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableDateTimeOffsetCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public DateTimeOffset? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableDateTimeOffset(this.ColumnName.Name);

        public DateTimeOffset? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableDateTimeOffset(customColumnName);

        public DateTimeOffset? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? (DateTimeOffset?)recordReader.GetValue(ordinal) : null;
    }
    public class GuidCustomColumn : ExprColumn
    {
        internal GuidCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal GuidCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public Guid Read(ISqDataRecordReader recordReader) => recordReader.GetGuid(this.ColumnName.Name);

        public Guid Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetGuid(customColumnName);

        public Guid Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetGuid(ordinal);
    }
    public class NullableGuidCustomColumn : ExprColumn
    {
        internal NullableGuidCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableGuidCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public Guid? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableGuid(this.ColumnName.Name);

        public Guid? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableGuid(customColumnName);

        public Guid? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetGuid(ordinal) : null;
    }
    public class StringCustomColumn : ExprColumn
    {
        internal StringCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal StringCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public string Read(ISqDataRecordReader recordReader) => recordReader.GetString(this.ColumnName.Name);

        public string Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetString(customColumnName);

        public string Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetString(ordinal);

        public static ExprStringConcat operator +(ExprStringConcat a, StringCustomColumn b)
            => new ExprStringConcat(a, b);

        public static ExprStringConcat operator +(StringCustomColumn a, ExprStringConcat b)
            => new ExprStringConcat(a, b);

        public static ExprStringConcat operator +(StringCustomColumn a, StringCustomColumn b)
            => new ExprStringConcat(a, b);

    }
    public class NullableStringCustomColumn : ExprColumn
    {
        internal NullableStringCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableStringCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public string? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableString(this.ColumnName.Name);

        public string? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableString(customColumnName);

        public string? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetString(ordinal) : null;
    }
}