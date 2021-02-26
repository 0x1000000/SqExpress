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
    }
    public class NullableBooleanCustomColumn : ExprColumn
    {
        internal NullableBooleanCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableBooleanCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public bool? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableBoolean(this.ColumnName.Name);
    }
    public class ByteCustomColumn : ExprColumn
    {
        internal ByteCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal ByteCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public byte Read(ISqDataRecordReader recordReader) => recordReader.GetByte(this.ColumnName.Name);
    }
    public class NullableByteCustomColumn : ExprColumn
    {
        internal NullableByteCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableByteCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public byte? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableByte(this.ColumnName.Name);
    }
    public class ByteArrayCustomColumn : ExprColumn
    {
        internal ByteArrayCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal ByteArrayCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public byte[] Read(ISqDataRecordReader recordReader) => recordReader.GetByteArray(this.ColumnName.Name);

        public Stream GetStream(ISqDataRecordReader recordReader) => recordReader.GetStream(this.ColumnName.Name);
    }
    public class NullableByteArrayCustomColumn : ExprColumn
    {
        internal NullableByteArrayCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableByteArrayCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public byte? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableByte(this.ColumnName.Name);

        public Stream? GetStream(ISqDataRecordReader recordReader) => recordReader.GetNullableStream(this.ColumnName.Name);
    }
    public class Int16CustomColumn : ExprColumn
    {
        internal Int16CustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal Int16CustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public short Read(ISqDataRecordReader recordReader) => recordReader.GetInt16(this.ColumnName.Name);
    }
    public class NullableInt16CustomColumn : ExprColumn
    {
        internal NullableInt16CustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableInt16CustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public short? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableInt16(this.ColumnName.Name);
    }
    public class Int32CustomColumn : ExprColumn
    {
        internal Int32CustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal Int32CustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public int Read(ISqDataRecordReader recordReader) => recordReader.GetInt32(this.ColumnName.Name);
    }
    public class NullableInt32CustomColumn : ExprColumn
    {
        internal NullableInt32CustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }
        
        internal NullableInt32CustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public int? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableInt32(this.ColumnName.Name);
    }
    public class Int64CustomColumn : ExprColumn
    {
        internal Int64CustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }
        
        internal Int64CustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public long Read(ISqDataRecordReader recordReader) => recordReader.GetInt64(this.ColumnName.Name);
    }
    public class NullableInt64CustomColumn : ExprColumn
    {
        internal NullableInt64CustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }
        
        internal NullableInt64CustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public long? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableInt64(this.ColumnName.Name);
    }
    public class DecimalCustomColumn : ExprColumn
    {
        internal DecimalCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal DecimalCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public decimal Read(ISqDataRecordReader recordReader) => recordReader.GetDecimal(this.ColumnName.Name);
    }
    public class NullableDecimalCustomColumn : ExprColumn
    {
        internal NullableDecimalCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableDecimalCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public decimal? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableDecimal(this.ColumnName.Name);
    }
    public class DoubleCustomColumn : ExprColumn
    {
        internal DoubleCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal DoubleCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public double Read(ISqDataRecordReader recordReader) => recordReader.GetDouble(this.ColumnName.Name);
    }
    public class NullableDoubleCustomColumn : ExprColumn
    {
        internal NullableDoubleCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableDoubleCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public double? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableDouble(this.ColumnName.Name);
    }
    public class DateTimeCustomColumn : ExprColumn
    {
        internal DateTimeCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal DateTimeCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public DateTime Read(ISqDataRecordReader recordReader) => recordReader.GetDateTime(this.ColumnName.Name);
    }
    public class NullableDateTimeCustomColumn : ExprColumn
    {
        internal NullableDateTimeCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableDateTimeCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public DateTime? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableDateTime(this.ColumnName.Name);
    }
    public class GuidCustomColumn : ExprColumn
    {
        internal GuidCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal GuidCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public Guid Read(ISqDataRecordReader recordReader) => recordReader.GetGuid(this.ColumnName.Name);
    }
    public class NullableGuidCustomColumn : ExprColumn
    {
        internal NullableGuidCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal NullableGuidCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public Guid? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableGuid(this.ColumnName.Name);
    }
    public class StringCustomColumn : ExprColumn
    {
        internal StringCustomColumn(string name, IExprColumnSource? columnSource = null) : base(columnSource, new ExprColumnName(name)) { }

        internal StringCustomColumn(ExprColumnName name, IExprColumnSource? columnSource) : base(columnSource, name) { }

        public string Read(ISqDataRecordReader recordReader) => recordReader.GetString(this.ColumnName.Name);

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
    }
}