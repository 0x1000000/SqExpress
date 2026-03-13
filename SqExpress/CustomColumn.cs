using System;
using System.IO;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;

namespace SqExpress
{
    public class BooleanCustomColumn : TypedColumn
    {
        internal BooleanCustomColumn(string name, IExprColumnSource? columnSource = null)
            : base(columnSource, new ExprColumnName(name), ExprTypeBoolean.Instance, false)
        {
        }

        internal BooleanCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : base(columnSource, name, ExprTypeBoolean.Instance, false)
        {
        }

        public bool Read(ISqDataRecordReader recordReader) => recordReader.GetBoolean(this.ColumnName.Name);

        public bool Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetBoolean(customColumnName);

        public bool Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetBoolean(ordinal);
    }

    public class NullableBooleanCustomColumn : TypedColumn
    {
        internal NullableBooleanCustomColumn(string name, IExprColumnSource? columnSource = null)
            : base(columnSource, new ExprColumnName(name), ExprTypeBoolean.Instance, true)
        {
        }

        internal NullableBooleanCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : base(columnSource, name, ExprTypeBoolean.Instance, true)
        {
        }

        public bool? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableBoolean(this.ColumnName.Name);

        public bool? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableBoolean(customColumnName);

        public bool? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetBoolean(ordinal) : null;
    }

    public class ByteCustomColumn : TypedColumn
    {
        internal ByteCustomColumn(string name, IExprColumnSource? columnSource = null)
            : base(columnSource, new ExprColumnName(name), ExprTypeByte.Instance, false)
        {
        }

        internal ByteCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : base(columnSource, name, ExprTypeByte.Instance, false)
        {
        }

        public byte Read(ISqDataRecordReader recordReader) => recordReader.GetByte(this.ColumnName.Name);

        public byte Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetByte(customColumnName);

        public byte Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetByte(ordinal);
    }

    public class NullableByteCustomColumn : TypedColumn
    {
        internal NullableByteCustomColumn(string name, IExprColumnSource? columnSource = null)
            : base(columnSource, new ExprColumnName(name), ExprTypeByte.Instance, true)
        {
        }

        internal NullableByteCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : base(columnSource, name, ExprTypeByte.Instance, true)
        {
        }

        public byte? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableByte(this.ColumnName.Name);

        public byte? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableByte(customColumnName);

        public byte? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetByte(ordinal) : null;
    }

    public class ByteArrayCustomColumn : TypedColumn
    {
        internal ByteArrayCustomColumn(string name, IExprColumnSource? columnSource = null)
            : this(new ExprColumnName(name), columnSource, new ExprTypeByteArray(null))
        {
        }

        internal ByteArrayCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : this(name, columnSource, new ExprTypeByteArray(null))
        {
        }

        internal ByteArrayCustomColumn(ExprColumnName name, IExprColumnSource? columnSource, ExprTypeByteArrayBase sqlType)
            : base(columnSource, name, sqlType, false)
        {
        }

        public new ExprTypeByteArrayBase SqlType => (ExprTypeByteArrayBase)base.SqlType;

        public byte[] Read(ISqDataRecordReader recordReader) => recordReader.GetByteArray(this.ColumnName.Name);

        public byte[] Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetByteArray(customColumnName);

        public byte[] Read(ISqDataRecordReader recordReader, int ordinal) => (byte[])recordReader.GetValue(ordinal);

        public Stream GetStream(ISqDataRecordReader recordReader) => recordReader.GetStream(this.ColumnName.Name);
    }

    public class NullableByteArrayCustomColumn : TypedColumn
    {
        internal NullableByteArrayCustomColumn(string name, IExprColumnSource? columnSource = null)
            : this(new ExprColumnName(name), columnSource, new ExprTypeByteArray(null))
        {
        }

        internal NullableByteArrayCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : this(name, columnSource, new ExprTypeByteArray(null))
        {
        }

        internal NullableByteArrayCustomColumn(ExprColumnName name, IExprColumnSource? columnSource, ExprTypeByteArrayBase sqlType)
            : base(columnSource, name, sqlType, true)
        {
        }

        public new ExprTypeByteArrayBase SqlType => (ExprTypeByteArrayBase)base.SqlType;

        public byte[]? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableByteArray(this.ColumnName.Name);

        public byte[]? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableByteArray(customColumnName);

        public byte[]? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? (byte[])recordReader.GetValue(ordinal) : null;

        public Stream? GetStream(ISqDataRecordReader recordReader) => recordReader.GetNullableStream(this.ColumnName.Name);
    }

    public class Int16CustomColumn : TypedColumn
    {
        internal Int16CustomColumn(string name, IExprColumnSource? columnSource = null)
            : base(columnSource, new ExprColumnName(name), ExprTypeInt16.Instance, false)
        {
        }

        internal Int16CustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : base(columnSource, name, ExprTypeInt16.Instance, false)
        {
        }

        public short Read(ISqDataRecordReader recordReader) => recordReader.GetInt16(this.ColumnName.Name);

        public short Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetInt16(customColumnName);

        public short Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetInt16(ordinal);
    }

    public class NullableInt16CustomColumn : TypedColumn
    {
        internal NullableInt16CustomColumn(string name, IExprColumnSource? columnSource = null)
            : base(columnSource, new ExprColumnName(name), ExprTypeInt16.Instance, true)
        {
        }

        internal NullableInt16CustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : base(columnSource, name, ExprTypeInt16.Instance, true)
        {
        }

        public short? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableInt16(this.ColumnName.Name);

        public short? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableInt16(customColumnName);

        public short? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetInt16(ordinal) : null;
    }

    public class Int32CustomColumn : TypedColumn
    {
        internal Int32CustomColumn(string name, IExprColumnSource? columnSource = null)
            : base(columnSource, new ExprColumnName(name), ExprTypeInt32.Instance, false)
        {
        }

        internal Int32CustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : base(columnSource, name, ExprTypeInt32.Instance, false)
        {
        }

        public int Read(ISqDataRecordReader recordReader) => recordReader.GetInt32(this.ColumnName.Name);

        public int Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetInt32(customColumnName);

        public int Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetInt32(ordinal);
    }

    public class NullableInt32CustomColumn : TypedColumn
    {
        internal NullableInt32CustomColumn(string name, IExprColumnSource? columnSource = null)
            : base(columnSource, new ExprColumnName(name), ExprTypeInt32.Instance, true)
        {
        }

        internal NullableInt32CustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : base(columnSource, name, ExprTypeInt32.Instance, true)
        {
        }

        public int? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableInt32(this.ColumnName.Name);

        public int? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableInt32(customColumnName);

        public int? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetInt32(ordinal) : null;
    }

    public class Int64CustomColumn : TypedColumn
    {
        internal Int64CustomColumn(string name, IExprColumnSource? columnSource = null)
            : base(columnSource, new ExprColumnName(name), ExprTypeInt64.Instance, false)
        {
        }

        internal Int64CustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : base(columnSource, name, ExprTypeInt64.Instance, false)
        {
        }

        public long Read(ISqDataRecordReader recordReader) => recordReader.GetInt64(this.ColumnName.Name);

        public long Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetInt64(customColumnName);

        public long Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetInt64(ordinal);
    }

    public class NullableInt64CustomColumn : TypedColumn
    {
        internal NullableInt64CustomColumn(string name, IExprColumnSource? columnSource = null)
            : base(columnSource, new ExprColumnName(name), ExprTypeInt64.Instance, true)
        {
        }

        internal NullableInt64CustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : base(columnSource, name, ExprTypeInt64.Instance, true)
        {
        }

        public long? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableInt64(this.ColumnName.Name);

        public long? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableInt64(customColumnName);

        public long? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetInt64(ordinal) : null;
    }

    public class DecimalCustomColumn : TypedColumn
    {
        internal DecimalCustomColumn(string name, IExprColumnSource? columnSource = null)
            : this(new ExprColumnName(name), columnSource, new ExprTypeDecimal(null))
        {
        }

        internal DecimalCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : this(name, columnSource, new ExprTypeDecimal(null))
        {
        }

        internal DecimalCustomColumn(ExprColumnName name, IExprColumnSource? columnSource, ExprTypeDecimal sqlType)
            : base(columnSource, name, sqlType, false)
        {
        }

        public new ExprTypeDecimal SqlType => (ExprTypeDecimal)base.SqlType;

        public decimal Read(ISqDataRecordReader recordReader) => recordReader.GetDecimal(this.ColumnName.Name);

        public decimal Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetDecimal(customColumnName);

        public decimal Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetDecimal(ordinal);
    }

    public class NullableDecimalCustomColumn : TypedColumn
    {
        internal NullableDecimalCustomColumn(string name, IExprColumnSource? columnSource = null)
            : this(new ExprColumnName(name), columnSource, new ExprTypeDecimal(null))
        {
        }

        internal NullableDecimalCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : this(name, columnSource, new ExprTypeDecimal(null))
        {
        }

        internal NullableDecimalCustomColumn(ExprColumnName name, IExprColumnSource? columnSource, ExprTypeDecimal sqlType)
            : base(columnSource, name, sqlType, true)
        {
        }

        public new ExprTypeDecimal SqlType => (ExprTypeDecimal)base.SqlType;

        public decimal? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableDecimal(this.ColumnName.Name);

        public decimal? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableDecimal(customColumnName);

        public decimal? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetDecimal(ordinal) : null;
    }

    public class DoubleCustomColumn : TypedColumn
    {
        internal DoubleCustomColumn(string name, IExprColumnSource? columnSource = null)
            : base(columnSource, new ExprColumnName(name), ExprTypeDouble.Instance, false)
        {
        }

        internal DoubleCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : base(columnSource, name, ExprTypeDouble.Instance, false)
        {
        }

        public double Read(ISqDataRecordReader recordReader) => recordReader.GetDouble(this.ColumnName.Name);

        public double Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetDouble(customColumnName);

        public double Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetDouble(ordinal);
    }

    public class NullableDoubleCustomColumn : TypedColumn
    {
        internal NullableDoubleCustomColumn(string name, IExprColumnSource? columnSource = null)
            : base(columnSource, new ExprColumnName(name), ExprTypeDouble.Instance, true)
        {
        }

        internal NullableDoubleCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : base(columnSource, name, ExprTypeDouble.Instance, true)
        {
        }

        public double? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableDouble(this.ColumnName.Name);

        public double? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableDouble(customColumnName);

        public double? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetDouble(ordinal) : null;
    }

    public class DateTimeCustomColumn : TypedColumn
    {
        internal DateTimeCustomColumn(string name, IExprColumnSource? columnSource = null)
            : this(new ExprColumnName(name), columnSource, new ExprTypeDateTime(false))
        {
        }

        internal DateTimeCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : this(name, columnSource, new ExprTypeDateTime(false))
        {
        }

        internal DateTimeCustomColumn(ExprColumnName name, IExprColumnSource? columnSource, ExprTypeDateTime sqlType)
            : base(columnSource, name, sqlType, false)
        {
        }

        public new ExprTypeDateTime SqlType => (ExprTypeDateTime)base.SqlType;

        public DateTime Read(ISqDataRecordReader recordReader) => recordReader.GetDateTime(this.ColumnName.Name);

        public DateTime Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetDateTime(customColumnName);

        public DateTime Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetDateTime(ordinal);
    }

    public class NullableDateTimeCustomColumn : TypedColumn
    {
        internal NullableDateTimeCustomColumn(string name, IExprColumnSource? columnSource = null)
            : this(new ExprColumnName(name), columnSource, new ExprTypeDateTime(false))
        {
        }

        internal NullableDateTimeCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : this(name, columnSource, new ExprTypeDateTime(false))
        {
        }

        internal NullableDateTimeCustomColumn(ExprColumnName name, IExprColumnSource? columnSource, ExprTypeDateTime sqlType)
            : base(columnSource, name, sqlType, true)
        {
        }

        public new ExprTypeDateTime SqlType => (ExprTypeDateTime)base.SqlType;

        public DateTime? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableDateTime(this.ColumnName.Name);

        public DateTime? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableDateTime(customColumnName);

        public DateTime? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetDateTime(ordinal) : null;
    }

    public class DateTimeOffsetCustomColumn : TypedColumn
    {
        internal DateTimeOffsetCustomColumn(string name, IExprColumnSource? columnSource = null)
            : base(columnSource, new ExprColumnName(name), ExprTypeDateTimeOffset.Instance, false)
        {
        }

        internal DateTimeOffsetCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : base(columnSource, name, ExprTypeDateTimeOffset.Instance, false)
        {
        }

        public DateTimeOffset Read(ISqDataRecordReader recordReader) => recordReader.GetDateTimeOffset(this.ColumnName.Name);

        public DateTimeOffset Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetDateTimeOffset(customColumnName);

        public DateTimeOffset Read(ISqDataRecordReader recordReader, int ordinal) => (DateTimeOffset)recordReader.GetValue(ordinal);
    }

    public class NullableDateTimeOffsetCustomColumn : TypedColumn
    {
        internal NullableDateTimeOffsetCustomColumn(string name, IExprColumnSource? columnSource = null)
            : base(columnSource, new ExprColumnName(name), ExprTypeDateTimeOffset.Instance, true)
        {
        }

        internal NullableDateTimeOffsetCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : base(columnSource, name, ExprTypeDateTimeOffset.Instance, true)
        {
        }

        public DateTimeOffset? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableDateTimeOffset(this.ColumnName.Name);

        public DateTimeOffset? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableDateTimeOffset(customColumnName);

        public DateTimeOffset? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? (DateTimeOffset?)recordReader.GetValue(ordinal) : null;
    }

    public class GuidCustomColumn : TypedColumn
    {
        internal GuidCustomColumn(string name, IExprColumnSource? columnSource = null)
            : base(columnSource, new ExprColumnName(name), ExprTypeGuid.Instance, false)
        {
        }

        internal GuidCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : base(columnSource, name, ExprTypeGuid.Instance, false)
        {
        }

        public Guid Read(ISqDataRecordReader recordReader) => recordReader.GetGuid(this.ColumnName.Name);

        public Guid Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetGuid(customColumnName);

        public Guid Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetGuid(ordinal);
    }

    public class NullableGuidCustomColumn : TypedColumn
    {
        internal NullableGuidCustomColumn(string name, IExprColumnSource? columnSource = null)
            : base(columnSource, new ExprColumnName(name), ExprTypeGuid.Instance, true)
        {
        }

        internal NullableGuidCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : base(columnSource, name, ExprTypeGuid.Instance, true)
        {
        }

        public Guid? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableGuid(this.ColumnName.Name);

        public Guid? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableGuid(customColumnName);

        public Guid? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetGuid(ordinal) : null;
    }

    public class StringCustomColumn : TypedColumn
    {
        internal StringCustomColumn(string name, IExprColumnSource? columnSource = null)
            : this(new ExprColumnName(name), columnSource, new ExprTypeString(null, true, false))
        {
        }

        internal StringCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : this(name, columnSource, new ExprTypeString(null, true, false))
        {
        }

        internal StringCustomColumn(ExprColumnName name, IExprColumnSource? columnSource, ExprTypeStringBase sqlType)
            : base(columnSource, name, sqlType, false)
        {
        }

        public new ExprTypeStringBase SqlType => (ExprTypeStringBase)base.SqlType;

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

    public class NullableStringCustomColumn : TypedColumn
    {
        internal NullableStringCustomColumn(string name, IExprColumnSource? columnSource = null)
            : this(new ExprColumnName(name), columnSource, new ExprTypeString(null, true, false))
        {
        }

        internal NullableStringCustomColumn(ExprColumnName name, IExprColumnSource? columnSource)
            : this(name, columnSource, new ExprTypeString(null, true, false))
        {
        }

        internal NullableStringCustomColumn(ExprColumnName name, IExprColumnSource? columnSource, ExprTypeStringBase sqlType)
            : base(columnSource, name, sqlType, true)
        {
        }

        public new ExprTypeStringBase SqlType => (ExprTypeStringBase)base.SqlType;

        public string? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableString(this.ColumnName.Name);

        public string? Read(ISqDataRecordReader recordReader, string customColumnName) => recordReader.GetNullableString(customColumnName);

        public string? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetString(ordinal) : null;
    }
}
