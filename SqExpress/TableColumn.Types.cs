using System;
using System.Globalization;
using System.IO;
using SqExpress.Meta;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress
{
    public class BooleanTableColumn : TableColumn
    {
        internal BooleanTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Boolean, false, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitBoolean(this);

        public bool Read(ISqDataRecordReader recordReader) => recordReader.GetBoolean(this.ColumnName.Name);

        public bool? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableBoolean(this.ColumnName.Name);

        public bool Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetBoolean(ordinal);

        public bool? ReadNullable(ISqDataRecordReader recordReader, int ordinal) 
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetBoolean(ordinal):null;

        public new BooleanTableColumn WithSource(IExprColumnSource? source) => new BooleanTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string ReadAsString(ISqDataRecordReader recordReader) 
            => this.ReadNullable(recordReader)?.ToString() 
               ?? throw new SqExpressException($"Null value is not expected in non nullable column '{this.ColumnName.Name}'");

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? throw new SqExpressException($"Value cannot be null for '{this.ColumnName.Name}' non nullable column")
                : bool.TryParse(value, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as boolean for column '{this.ColumnName.Name}'.");

        public BooleanCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new BooleanCustomColumn(this.ColumnName, columnSource);

        public BooleanCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new BooleanCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableBooleanTableColumn : TableColumn
    {
        internal NullableBooleanTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Boolean, true, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableBoolean(this);

        public bool? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableBoolean(this.ColumnName.Name);

        public bool? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetBoolean(ordinal) : null;

        public new NullableBooleanTableColumn WithSource(IExprColumnSource? source) => new NullableBooleanTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string? ReadAsString(ISqDataRecordReader recordReader) => this.Read(recordReader)?.ToString();

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? SqQueryBuilder.Literal((bool?)null)
                : bool.TryParse(value, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as boolean.");

        public NullableBooleanCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableBooleanCustomColumn(this.ColumnName, columnSource);

        public NullableBooleanCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableBooleanCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class ByteTableColumn : TableColumn
    {
        internal ByteTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Byte, false, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitByte(this);

        public byte Read(ISqDataRecordReader recordReader) => recordReader.GetByte(this.ColumnName.Name);

        public byte? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableByte(this.ColumnName.Name);

        public byte Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetByte(ordinal);

        public byte? ReadNullable(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetByte(ordinal) : null;

        public new ByteTableColumn WithSource(IExprColumnSource? source) => new ByteTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string ReadAsString(ISqDataRecordReader recordReader)
            => this.ReadNullable(recordReader)?.ToString()
               ?? throw new SqExpressException($"Null value is not expected in non nullable column '{this.ColumnName.Name}'");

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? throw new SqExpressException($"Value cannot be null for '{this.ColumnName.Name}' non nullable column")
                : byte.TryParse(value, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as byte for column '{this.ColumnName.Name}'.");

        public ByteCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new ByteCustomColumn(this.ColumnName, columnSource);

        public ByteCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new ByteCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableByteTableColumn : TableColumn
    {
        internal NullableByteTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Byte, true, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableByte(this);

        public byte? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableByte(this.ColumnName.Name);

        public byte? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetByte(ordinal) : null;

        public new NullableByteTableColumn WithSource(IExprColumnSource? source) => new NullableByteTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string? ReadAsString(ISqDataRecordReader recordReader) => this.Read(recordReader)?.ToString();

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? SqQueryBuilder.Literal((byte?)null)
                : byte.TryParse(value, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as byte for column '{this.ColumnName.Name}'.");

        public NullableByteCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableByteCustomColumn(this.ColumnName, columnSource);

        public NullableByteCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableByteCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class ByteArrayTableColumn : TableColumn
    {
        internal ByteArrayTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ExprTypeByteArrayBase typeByteArray, ColumnMeta? columnMeta) : base(source, columnName, table, typeByteArray, false, columnMeta)
        {
            this.SqlType = typeByteArray;
        }

        public new ExprTypeByteArrayBase SqlType { get; }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitByteArray(this);

        public byte[] Read(ISqDataRecordReader recordReader) => recordReader.GetByteArray(this.ColumnName.Name);

        public byte[]? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableByteArray(this.ColumnName.Name);

        public byte[] Read(ISqDataRecordReader recordReader, int ordinal) => (byte[])recordReader.GetValue(ordinal);

        public byte[]? ReadNullable(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? (byte[])recordReader.GetValue(ordinal) : null;

        public Stream GetStream(ISqDataRecordReader recordReader) => recordReader.GetStream(this.ColumnName.Name);

        public Stream? ReadNullableStream(ISqDataRecordReader recordReader) => recordReader.GetNullableStream(this.ColumnName.Name);

        public new ByteTableColumn WithSource(IExprColumnSource? source) => new ByteTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string? ReadAsString(ISqDataRecordReader recordReader)
        {
            var base64Str = this.ReadNullable(recordReader);
            return base64Str == null ? null : Convert.ToBase64String(base64Str);
        }

        public override ExprLiteral FromString(string? value)
        {
            if (value == null)
                throw new SqExpressException($"Value cannot be null for '{this.ColumnName.Name}' non nullable column");
            try
            {
                var result = Convert.FromBase64String(value);
                return SqQueryBuilder.Literal(result);
            }
            catch (FormatException e)
            {
                throw new SqExpressException($"Could not parse base64 string '{(value.Length > 50 ? value.Substring(0, 50) : value)}' for '{this.ColumnName.Name}' column.", e);
            }
        }

        public ByteCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new ByteCustomColumn(this.ColumnName, columnSource);

        public ByteCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new ByteCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableByteArrayTableColumn : TableColumn
    {
        internal NullableByteArrayTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ExprTypeByteArrayBase typeByteArray, ColumnMeta? columnMeta) : base(source, columnName, table, typeByteArray, true, columnMeta)
        {
            this.SqlType = typeByteArray;
        }

        public new ExprTypeByteArrayBase SqlType { get; }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableByteArray(this);

        public byte[]? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableByteArray(this.ColumnName.Name);

        public byte[]? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? (byte[])recordReader.GetValue(ordinal) : null;

        public Stream? GetStream(ISqDataRecordReader recordReader) => recordReader.GetStream(this.ColumnName.Name);

        public new ByteTableColumn WithSource(IExprColumnSource? source) => new ByteTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string? ReadAsString(ISqDataRecordReader recordReader)
        {
            var base64Str = this.Read(recordReader);
            return base64Str == null ? null : Convert.ToBase64String(base64Str);
        }

        public override ExprLiteral FromString(string? value)
        {
            if (value == null)
                return SqQueryBuilder.Literal((byte[]?)null);
            try
            {
                var result = Convert.FromBase64String(value);
                return SqQueryBuilder.Literal(result);
            }
            catch (FormatException e)
            {
                throw new SqExpressException($"Could not parse base64 string '{(value.Length > 50 ? value.Substring(0, 50) : value)}' for '{this.ColumnName.Name}' column.", e);
            }
        }

        public ByteCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new ByteCustomColumn(this.ColumnName, columnSource);

        public ByteCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new ByteCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class Int16TableColumn : TableColumn
    {
        internal Int16TableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Int16, false, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitInt16(this);

        public short Read(ISqDataRecordReader recordReader) => recordReader.GetInt16(this.ColumnName.Name);

        public short? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableInt16(this.ColumnName.Name);

        public short Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetInt16(ordinal);

        public short? ReadNullable(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetInt16(ordinal) : null;

        public new Int16TableColumn WithSource(IExprColumnSource? source) => new Int16TableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string ReadAsString(ISqDataRecordReader recordReader)
            => this.ReadNullable(recordReader)?.ToString()
               ?? throw new SqExpressException($"Null value is not expected in non nullable column '{this.ColumnName.Name}'");

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? throw new SqExpressException($"Value cannot be null for '{this.ColumnName.Name}' non nullable column")
                : short.TryParse(value, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as short for column '{this.ColumnName.Name}'.");

        public Int16CustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new Int16CustomColumn(this.ColumnName, columnSource);

        public Int16CustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new Int16CustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableInt16TableColumn : TableColumn
    {
        internal NullableInt16TableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Int16, true, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableInt16(this);

        public short? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableInt16(this.ColumnName.Name);

        public short? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetInt16(ordinal) : null;

        public new NullableInt16TableColumn WithSource(IExprColumnSource? source) => new NullableInt16TableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string? ReadAsString(ISqDataRecordReader recordReader) => this.Read(recordReader)?.ToString();

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? SqQueryBuilder.Literal((short?)null)
                : short.TryParse(value, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as short for column '{this.ColumnName.Name}'.");

        public NullableInt16CustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableInt16CustomColumn(this.ColumnName, columnSource);

        public NullableInt16CustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableInt16CustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class Int32TableColumn : TableColumn
    {
        internal Int32TableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Int32, false, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitInt32(this);

        public int Read(ISqDataRecordReader recordReader) => recordReader.GetInt32(this.ColumnName.Name);

        public int? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableInt32(this.ColumnName.Name);

        public int Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetInt32(ordinal);

        public int? ReadNullable(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetInt32(ordinal) : null;

        public new Int32TableColumn WithSource(IExprColumnSource? source) => new Int32TableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string ReadAsString(ISqDataRecordReader recordReader)
            => this.ReadNullable(recordReader)?.ToString()
               ?? throw new SqExpressException($"Null value is not expected in non nullable column '{this.ColumnName.Name}'");

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? throw new SqExpressException($"Value cannot be null for '{this.ColumnName.Name}' non nullable column")
                : int.TryParse(value, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as int for column '{this.ColumnName.Name}'.");

        public Int32CustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new Int32CustomColumn(this.ColumnName, columnSource);

        public Int32CustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new Int32CustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableInt32TableColumn : TableColumn
    {
        internal NullableInt32TableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Int32, true, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableInt32(this);

        public int? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableInt32(this.ColumnName.Name);

        public int? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetInt32(ordinal) : null;

        public new NullableInt32TableColumn WithSource(IExprColumnSource? source) => new NullableInt32TableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string? ReadAsString(ISqDataRecordReader recordReader) => this.Read(recordReader)?.ToString();

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? SqQueryBuilder.Literal((int?)null)
                : int.TryParse(value, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as int for column '{this.ColumnName.Name}'.");

        public NullableInt32CustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableInt32CustomColumn(this.ColumnName, columnSource);

        public NullableInt32CustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableInt32CustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class Int64TableColumn : TableColumn
    {
        internal Int64TableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Int64, false, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitInt64(this);

        public long Read(ISqDataRecordReader recordReader) => recordReader.GetInt64(this.ColumnName.Name);

        public long? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableInt64(this.ColumnName.Name);

        public long Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetInt64(ordinal);

        public long? ReadNullable(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetInt64(ordinal) : null;

        public new Int64TableColumn WithSource(IExprColumnSource? source) => new Int64TableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string ReadAsString(ISqDataRecordReader recordReader)
            => this.ReadNullable(recordReader)?.ToString()
               ?? throw new SqExpressException($"Null value is not expected in non nullable column '{this.ColumnName.Name}'");

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? throw new SqExpressException($"Value cannot be null for '{this.ColumnName.Name}' non nullable column")
                : long.TryParse(value, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as long for column '{this.ColumnName.Name}'.");

        public Int64CustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new Int64CustomColumn(this.ColumnName, columnSource);

        public Int64CustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new Int64CustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableInt64TableColumn : TableColumn
    {
        internal NullableInt64TableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Int64, true, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableInt64(this);

        public long? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableInt64(this.ColumnName.Name);

        public long? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetInt64(ordinal) : null;

        public new NullableInt64TableColumn WithSource(IExprColumnSource? source) => new NullableInt64TableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string? ReadAsString(ISqDataRecordReader recordReader) => this.Read(recordReader)?.ToString();

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? SqQueryBuilder.Literal((long?)null)
                : long.TryParse(value, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as long for column '{this.ColumnName.Name}'.");

        public NullableInt64CustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableInt64CustomColumn(this.ColumnName, columnSource);

        public NullableInt64CustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableInt64CustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class DecimalTableColumn : TableColumn
    {
        internal DecimalTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, DecimalPrecisionScale? precisionScale, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Decimal(precisionScale), false, columnMeta)
        {
            this.PrecisionScale = precisionScale;
        }

        public DecimalPrecisionScale? PrecisionScale { get; }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitDecimal(this);

        public decimal Read(ISqDataRecordReader recordReader) => recordReader.GetDecimal(this.ColumnName.Name);

        public decimal? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableDecimal(this.ColumnName.Name);

        public decimal Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetDecimal(ordinal);

        public decimal? ReadNullable(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetDecimal(ordinal) : null;

        public new DecimalTableColumn WithSource(IExprColumnSource? source) => new DecimalTableColumn(source, this.ColumnName, this.Table,this.PrecisionScale, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string ReadAsString(ISqDataRecordReader recordReader)
            => this.ReadNullable(recordReader)?.ToString("F", CultureInfo.InvariantCulture)
               ?? throw new SqExpressException($"Null value is not expected in non nullable column '{this.ColumnName.Name}'");

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? throw new SqExpressException($"Value cannot be null for '{this.ColumnName.Name}' non nullable column")
                : decimal.TryParse(value, NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as decimal for column '{this.ColumnName.Name}'.");

        public DecimalCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new DecimalCustomColumn(this.ColumnName, columnSource);

        public DecimalCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new DecimalCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableDecimalTableColumn : TableColumn
    {
        internal NullableDecimalTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, DecimalPrecisionScale? precisionScale, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Decimal(precisionScale), true, columnMeta)
        {
            this.PrecisionScale = precisionScale;
        }

        public DecimalPrecisionScale? PrecisionScale { get; }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableDecimal(this);

        public decimal? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableDecimal(this.ColumnName.Name);

        public decimal? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetDecimal(ordinal) : null;

        public new NullableDecimalTableColumn WithSource(IExprColumnSource? source) => new NullableDecimalTableColumn(source, this.ColumnName, this.Table, this.PrecisionScale ,this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string? ReadAsString(ISqDataRecordReader recordReader) => this.Read(recordReader)?.ToString("F", CultureInfo.InvariantCulture);

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? SqQueryBuilder.Literal((decimal?)null)
                : decimal.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as decimal for column '{this.ColumnName.Name}'.");
        public NullableDecimalCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableDecimalCustomColumn(this.ColumnName, columnSource);

        public NullableDecimalCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableDecimalCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class DoubleTableColumn : TableColumn
    {
        internal DoubleTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Double, false, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitDouble(this);

        public double Read(ISqDataRecordReader recordReader) => recordReader.GetDouble(this.ColumnName.Name);

        public double? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableDouble(this.ColumnName.Name);

        public double Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetDouble(ordinal);

        public double? ReadNullable(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetDouble(ordinal) : null;

        public new DoubleTableColumn WithSource(IExprColumnSource? source) => new DoubleTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string ReadAsString(ISqDataRecordReader recordReader)
            => this.ReadNullable(recordReader)?.ToString("F", CultureInfo.InvariantCulture)
               ?? throw new SqExpressException($"Null value is not expected in non nullable column '{this.ColumnName.Name}'");

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? throw new SqExpressException($"Value cannot be null for '{this.ColumnName.Name}' non nullable column")
                : double.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as double for column '{this.ColumnName.Name}'.");

        public DoubleCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new DoubleCustomColumn(this.ColumnName, columnSource);

        public DoubleCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new DoubleCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableDoubleTableColumn : TableColumn
    {
        internal NullableDoubleTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Double, true, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableDouble(this);

        public double? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableDouble(this.ColumnName.Name);

        public double? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetDouble(ordinal) : null;

        public new NullableDoubleTableColumn WithSource(IExprColumnSource? source) => new NullableDoubleTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string? ReadAsString(ISqDataRecordReader recordReader)
            => this.Read(recordReader)?.ToString("F", CultureInfo.InvariantCulture);

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? SqQueryBuilder.Literal((double?)null)
                : double.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as double for column '{this.ColumnName.Name}'.");

        public NullableDoubleCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableDoubleCustomColumn(this.ColumnName, columnSource);

        public NullableDoubleCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableDoubleCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class DateTimeTableColumn : TableColumn
    {
        internal DateTimeTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, bool isDate, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.DateTime(isDate), false, columnMeta)
        {
            this.IsDate = isDate;
        }

        public bool IsDate { get; }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitDateTime(this);

        public DateTime Read(ISqDataRecordReader recordReader) => recordReader.GetDateTime(this.ColumnName.Name);

        public DateTime? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableDateTime(this.ColumnName.Name);

        public DateTime Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetDateTime(ordinal);

        public DateTime? ReadNullable(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetDateTime(ordinal) : null;

        public new DateTimeTableColumn WithSource(IExprColumnSource? source) => new DateTimeTableColumn(source, this.ColumnName, this.Table, this.IsDate, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string ReadAsString(ISqDataRecordReader recordReader)
        {
            var value = this.ReadNullable(recordReader);
            if (value == null)
            {
                throw new SqExpressException($"Null value is not expected in non nullable column '{this.ColumnName.Name}'");
            }

            return this.IsDate ? value.Value.ToString("yyyy-MM-dd") : value.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff");
        }

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? throw new SqExpressException($"Value cannot be null for '{this.ColumnName.Name}' non nullable column")
                : DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as date(time) for column '{this.ColumnName.Name}'.");

        public DateTimeCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new DateTimeCustomColumn(this.ColumnName, columnSource);

        public DateTimeCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new DateTimeCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableDateTimeTableColumn : TableColumn
    {
        internal NullableDateTimeTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, bool isDate, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.DateTime(isDate), true, columnMeta)
        {
            this.IsDate = isDate;
        }

        public bool IsDate { get; }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableDateTime(this);

        public DateTime? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableDateTime(this.ColumnName.Name);

        public DateTime? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetDateTime(ordinal) : null;

        public new NullableDateTimeTableColumn WithSource(IExprColumnSource? source) => new NullableDateTimeTableColumn(source, this.ColumnName, this.Table, this.IsDate, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string? ReadAsString(ISqDataRecordReader recordReader)
        {
            var value = this.Read(recordReader);
            return value == null ? null : this.IsDate ? value.Value.ToString("yyyy-MM-dd") : value.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff");
        }

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? SqQueryBuilder.Literal((DateTime?)null)
                : DateTime.TryParse(value, null, DateTimeStyles.RoundtripKind, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as date(time) for column '{this.ColumnName.Name}'.");

        public NullableDateTimeCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableDateTimeCustomColumn(this.ColumnName, columnSource);

        public NullableDateTimeCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableDateTimeCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class GuidTableColumn : TableColumn
    {
        internal GuidTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Guid, false, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitGuid(this);

        public Guid Read(ISqDataRecordReader recordReader) => recordReader.GetGuid(this.ColumnName.Name);

        public Guid? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableGuid(this.ColumnName.Name);

        public Guid Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetGuid(ordinal);

        public Guid? ReadNullable(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetGuid(ordinal) : null;

        public new GuidTableColumn WithSource(IExprColumnSource? source) => new GuidTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string ReadAsString(ISqDataRecordReader recordReader)
            => this.ReadNullable(recordReader)?.ToString("D")
               ?? throw new SqExpressException($"Null value is not expected in non nullable column '{this.ColumnName.Name}'");

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? throw new SqExpressException($"Value cannot be null for '{this.ColumnName.Name}' non nullable column")
                : Guid.TryParse(value, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as GUID for column '{this.ColumnName.Name}'.");

        public GuidCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new GuidCustomColumn(this.ColumnName, columnSource);

        public GuidCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new GuidCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableGuidTableColumn : TableColumn
    {
        internal NullableGuidTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Guid, true, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableGuid(this);

        public Guid? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableGuid(this.ColumnName.Name);

        public Guid? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetGuid(ordinal) : null;

        public new NullableGuidTableColumn WithSource(IExprColumnSource? source) => new NullableGuidTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string? ReadAsString(ISqDataRecordReader recordReader) => this.Read(recordReader)?.ToString("D");

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? SqQueryBuilder.Literal((Guid?)null)
                : Guid.TryParse(value, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as GUID for column '{this.ColumnName.Name}'.");

        public NullableGuidCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableGuidCustomColumn(this.ColumnName, columnSource);

        public NullableGuidCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableGuidCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class StringTableColumn : TableColumn
    {
        internal StringTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ExprTypeStringBase stringType, ColumnMeta? columnMeta) : base(source, columnName, table, stringType, false, columnMeta)
        {
            this.SqlType = stringType;
        }

        public new ExprTypeStringBase SqlType { get; }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitString(this);

        public string Read(ISqDataRecordReader recordReader) => recordReader.GetString(this.ColumnName.Name);

        public string? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableString(this.ColumnName.Name);

        public string Read(ISqDataRecordReader recordReader, int ordinal) => recordReader.GetString(ordinal);

        public string? ReadNullable(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetString(ordinal) : null;

        public new StringTableColumn WithSource(IExprColumnSource? source) => new StringTableColumn(source, this.ColumnName, this.Table, this.SqlType, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string ReadAsString(ISqDataRecordReader recordReader)
            => this.ReadNullable(recordReader)
               ?? throw new SqExpressException($"Null value is not expected in non nullable column '{this.ColumnName.Name}'");

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? throw new SqExpressException($"Value cannot be null for '{this.ColumnName.Name}' non nullable column")
                : SqQueryBuilder.Literal(value);

        public StringCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new StringCustomColumn(this.ColumnName, columnSource);

        public StringCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new StringCustomColumn(this.ColumnName, derivedTable.Alias));

        public static ExprStringConcat operator +(ExprStringConcat a, StringTableColumn b)
            => new ExprStringConcat(a, b);

        public static ExprStringConcat operator +(StringTableColumn a, ExprStringConcat b)
            => new ExprStringConcat(a, b);

        public static ExprStringConcat operator +(StringTableColumn a, StringTableColumn b)
            => new ExprStringConcat(a, b);
    }

    public class NullableStringTableColumn : TableColumn
    {
        internal NullableStringTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ExprTypeStringBase stringType, ColumnMeta? columnMeta) : base(source, columnName, table, stringType, true, columnMeta)
        {
            this.SqlType = stringType;
        }

        public new ExprTypeStringBase SqlType { get; }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableString(this);

        public string? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableString(this.ColumnName.Name);

        public string? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? recordReader.GetString(ordinal) : null;

        public new NullableStringTableColumn WithSource(IExprColumnSource? source) => new NullableStringTableColumn(source, this.ColumnName, this.Table, this.SqlType, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string? ReadAsString(ISqDataRecordReader recordReader) => this.Read(recordReader);

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? SqQueryBuilder.Literal((string?)null)
                : SqQueryBuilder.Literal(value);

        public NullableStringCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableStringCustomColumn(this.ColumnName, columnSource);

        public NullableStringCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableStringCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class DateTimeOffsetTableColumn : TableColumn
    {
        internal DateTimeOffsetTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.DateTimeOffset, false, columnMeta)
        {
        }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitDateTimeOffset(this);

        public DateTimeOffset Read(ISqDataRecordReader recordReader) => recordReader.GetDateTimeOffset(this.ColumnName.Name);

        public DateTimeOffset? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableDateTimeOffset(this.ColumnName.Name);

        public DateTimeOffset Read(ISqDataRecordReader recordReader, int ordinal) => (DateTimeOffset)recordReader.GetValue(ordinal);

        public DateTimeOffset? ReadNullable(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? (DateTimeOffset)recordReader.GetValue(ordinal) : null;

        public new DateTimeOffsetTableColumn WithSource(IExprColumnSource? source) => new DateTimeOffsetTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string ReadAsString(ISqDataRecordReader recordReader)
        {
            var value = this.ReadNullable(recordReader);
            if (value == null)
            {
                throw new SqExpressException($"Null value is not expected in non nullable column '{this.ColumnName.Name}'");
            }

            return value.Value.ToString("O");
        }

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? throw new SqExpressException($"Value cannot be null for '{this.ColumnName.Name}' non nullable column")
                : DateTimeOffset.TryParse(value, null, DateTimeStyles.RoundtripKind, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as datetimeoffset for column '{this.ColumnName.Name}'.");

        public DateTimeCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new DateTimeCustomColumn(this.ColumnName, columnSource);

        public DateTimeCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new DateTimeCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableDateTimeOffsetTableColumn : TableColumn
    {
        internal NullableDateTimeOffsetTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.DateTimeOffset, true, columnMeta)
        {
        }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableDateTimeOffset(this);

        public DateTimeOffset? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableDateTimeOffset(this.ColumnName.Name);

        public DateTimeOffset? Read(ISqDataRecordReader recordReader, int ordinal)
            => !recordReader.IsDBNull(ordinal) ? (DateTimeOffset)recordReader.GetValue(ordinal) : null;

        public new NullableDateTimeOffsetTableColumn WithSource(IExprColumnSource? source) => new NullableDateTimeOffsetTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public override string? ReadAsString(ISqDataRecordReader recordReader) => this.Read(recordReader)?.ToString("O");

        public override ExprLiteral FromString(string? value) =>
            value == null
                ? SqQueryBuilder.Literal((DateTimeOffset?)null)
                : DateTimeOffset.TryParse(value, null, DateTimeStyles.RoundtripKind, out var result)
                    ? SqQueryBuilder.Literal(result)
                    : throw new SqExpressException($"Could not parse '{value}' as datetimeoffset for column '{this.ColumnName.Name}'.");

        public NullableDateTimeCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableDateTimeCustomColumn(this.ColumnName, columnSource);

        public NullableDateTimeCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableDateTimeCustomColumn(this.ColumnName, derivedTable.Alias));
    }
}