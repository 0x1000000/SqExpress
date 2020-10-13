using System;
using SqExpress.Meta;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;

namespace SqExpress
{
    public class BooleanTableColumn : TableColumn
    {
        internal BooleanTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Boolean, false, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitBoolean(this);

        public bool Read(ISqDataRecordReader recordReader) => recordReader.GetBoolean(this.ColumnName.Name);

        public bool? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableBoolean(this.ColumnName.Name);

        public new BooleanTableColumn WithSource(IExprColumnSource? source) => new BooleanTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public BooleanCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new BooleanCustomColumn(this.ColumnName, columnSource);

        public BooleanCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new BooleanCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableBooleanTableColumn : TableColumn
    {
        internal NullableBooleanTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Boolean, true, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableBoolean(this);

        public bool? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableBoolean(this.ColumnName.Name);

        public new NullableBooleanTableColumn WithSource(IExprColumnSource? source) => new NullableBooleanTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public NullableBooleanCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableBooleanCustomColumn(this.ColumnName, columnSource);

        public NullableBooleanCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableBooleanCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class ByteTableColumn : TableColumn
    {
        internal ByteTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Byte, false, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitByte(this);

        public byte Read(ISqDataRecordReader recordReader) => recordReader.GetByte(this.ColumnName.Name);

        public byte? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableByte(this.ColumnName.Name);

        public new ByteTableColumn WithSource(IExprColumnSource? source) => new ByteTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public ByteCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new ByteCustomColumn(this.ColumnName, columnSource);

        public ByteCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new ByteCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableByteTableColumn : TableColumn
    {
        internal NullableByteTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Byte, true, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableByte(this);

        public byte? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableByte(this.ColumnName.Name);

        public new NullableByteTableColumn WithSource(IExprColumnSource? source) => new NullableByteTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public NullableByteCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableByteCustomColumn(this.ColumnName, columnSource);

        public NullableByteCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableByteCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class Int16TableColumn : TableColumn
    {
        internal Int16TableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Int16, false, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitInt16(this);

        public short Read(ISqDataRecordReader recordReader) => recordReader.GetInt16(this.ColumnName.Name);

        public short? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableInt16(this.ColumnName.Name);

        public new Int16TableColumn WithSource(IExprColumnSource? source) => new Int16TableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public Int16CustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new Int16CustomColumn(this.ColumnName, columnSource);

        public Int16CustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new Int16CustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableInt16TableColumn : TableColumn
    {
        internal NullableInt16TableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Int16, true, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableInt16(this);

        public short? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableInt16(this.ColumnName.Name);

        public new NullableInt16TableColumn WithSource(IExprColumnSource? source) => new NullableInt16TableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public NullableInt16CustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableInt16CustomColumn(this.ColumnName, columnSource);

        public NullableInt16CustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableInt16CustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class Int32TableColumn : TableColumn
    {
        internal Int32TableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Int32, false, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitInt32(this);

        public int Read(ISqDataRecordReader recordReader) => recordReader.GetInt32(this.ColumnName.Name);

        public int? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableInt32(this.ColumnName.Name);

        public new Int32TableColumn WithSource(IExprColumnSource? source) => new Int32TableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public Int32CustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new Int32CustomColumn(this.ColumnName, columnSource);

        public Int32CustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new Int32CustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableInt32TableColumn : TableColumn
    {
        internal NullableInt32TableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Int32, true, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableInt32(this);

        public int? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableInt32(this.ColumnName.Name);

        public new NullableInt32TableColumn WithSource(IExprColumnSource? source) => new NullableInt32TableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public NullableInt32CustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableInt32CustomColumn(this.ColumnName, columnSource);

        public NullableInt32CustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableInt32CustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class Int64TableColumn : TableColumn
    {
        internal Int64TableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Int64, false, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitInt64(this);

        public long Read(ISqDataRecordReader recordReader) => recordReader.GetInt64(this.ColumnName.Name);

        public long? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableInt64(this.ColumnName.Name);

        public new Int64TableColumn WithSource(IExprColumnSource? source) => new Int64TableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public Int64CustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new Int64CustomColumn(this.ColumnName, columnSource);

        public Int64CustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new Int64CustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableInt64TableColumn : TableColumn
    {
        internal NullableInt64TableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Int64, true, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableInt64(this);

        public long? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableInt64(this.ColumnName.Name);

        public new NullableInt64TableColumn WithSource(IExprColumnSource? source) => new NullableInt64TableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

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

        public new DecimalTableColumn WithSource(IExprColumnSource? source) => new DecimalTableColumn(source, this.ColumnName, this.Table,this.PrecisionScale, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

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

        public new NullableDecimalTableColumn WithSource(IExprColumnSource? source) => new NullableDecimalTableColumn(source, this.ColumnName, this.Table, this.PrecisionScale ,this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public NullableDecimalCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableDecimalCustomColumn(this.ColumnName, columnSource);

        public NullableDecimalCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableDecimalCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class DoubleTableColumn : TableColumn
    {
        internal DoubleTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Double, false, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitDouble(this);

        public double Read(ISqDataRecordReader recordReader) => recordReader.GetDouble(this.ColumnName.Name);

        public double? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableDouble(this.ColumnName.Name);

        public new DoubleTableColumn WithSource(IExprColumnSource? source) => new DoubleTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public DoubleCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new DoubleCustomColumn(this.ColumnName, columnSource);

        public DoubleCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new DoubleCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableDoubleTableColumn : TableColumn
    {
        internal NullableDoubleTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Double, true, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableDouble(this);

        public double? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableDouble(this.ColumnName.Name);

        public new NullableDoubleTableColumn WithSource(IExprColumnSource? source) => new NullableDoubleTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

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

        public new DateTimeTableColumn WithSource(IExprColumnSource? source) => new DateTimeTableColumn(source, this.ColumnName, this.Table, this.IsDate, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

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

        public new NullableDateTimeTableColumn WithSource(IExprColumnSource? source) => new NullableDateTimeTableColumn(source, this.ColumnName, this.Table, this.IsDate, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public NullableDateTimeCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableDateTimeCustomColumn(this.ColumnName, columnSource);

        public NullableDateTimeCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableDateTimeCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class GuidTableColumn : TableColumn
    {
        internal GuidTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Guid, false, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitGuid(this);

        public Guid Read(ISqDataRecordReader recordReader) => recordReader.GetGuid(this.ColumnName.Name);

        public Guid? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableGuid(this.ColumnName.Name);

        public new GuidTableColumn WithSource(IExprColumnSource? source) => new GuidTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public GuidCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new GuidCustomColumn(this.ColumnName, columnSource);

        public GuidCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new GuidCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class NullableGuidTableColumn : TableColumn
    {
        internal NullableGuidTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ColumnMeta? columnMeta) : base(source, columnName, table, SqQueryBuilder.SqlType.Guid, true, columnMeta) { }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableGuid(this);

        public Guid? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableGuid(this.ColumnName.Name);

        public new NullableGuidTableColumn WithSource(IExprColumnSource? source) => new NullableGuidTableColumn(source, this.ColumnName, this.Table, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public NullableGuidCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableGuidCustomColumn(this.ColumnName, columnSource);

        public NullableGuidCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableGuidCustomColumn(this.ColumnName, derivedTable.Alias));
    }

    public class StringTableColumn : TableColumn
    {
        internal StringTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ExprTypeString stringType, ColumnMeta? columnMeta) : base(source, columnName, table, stringType, false, columnMeta)
        {
            this.SqlType = stringType;
        }

        public new ExprTypeString SqlType { get; }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitString(this);

        public string Read(ISqDataRecordReader recordReader) => recordReader.GetString(this.ColumnName.Name);

        public string? ReadNullable(ISqDataRecordReader recordReader) => recordReader.GetNullableString(this.ColumnName.Name);

        public new StringTableColumn WithSource(IExprColumnSource? source) => new StringTableColumn(source, this.ColumnName, this.Table, this.SqlType, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

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
        internal NullableStringTableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ExprTypeString stringType, ColumnMeta? columnMeta) : base(source, columnName, table, stringType, true, columnMeta)
        {
            this.SqlType = stringType;
        }

        public new ExprTypeString SqlType { get; }

        public override TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor) => visitor.VisitNullableString(this);

        public string? Read(ISqDataRecordReader recordReader) => recordReader.GetNullableString(this.ColumnName.Name);

        public new NullableStringTableColumn WithSource(IExprColumnSource? source) => new NullableStringTableColumn(source, this.ColumnName, this.Table, this.SqlType, this.ColumnMeta);

        protected override TableColumn WithSourceInternal(IExprColumnSource? source) => this.WithSource(source);

        public NullableStringCustomColumn ToCustomColumn(IExprColumnSource? columnSource) => new NullableStringCustomColumn(this.ColumnName, columnSource);

        public NullableStringCustomColumn AddToDerivedTable(DerivedTableBase derivedTable) => derivedTable.RegisterColumn(new NullableStringCustomColumn(this.ColumnName, derivedTable.Alias));
    }
}