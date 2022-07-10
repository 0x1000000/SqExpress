namespace SqExpress.CodeGenUtil.Model
{
    internal interface IColumnTypeVisitor<out TRes,in TArg>
    {
        TRes VisitBooleanColumnType(BooleanColumnType booleanColumnType, TArg arg);
        TRes VisitByteColumnType(ByteColumnType byteColumnType, TArg arg);
        TRes VisitByteArrayColumnType(ByteArrayColumnType byteArrayColumnType, TArg arg);
        TRes VisitInt16ColumnType(Int16ColumnType int16ColumnType, TArg arg);
        TRes VisitInt32ColumnType(Int32ColumnType int32ColumnType, TArg arg);
        TRes VisitInt64ColumnType(Int64ColumnType int64ColumnType, TArg arg);
        TRes VisitDoubleColumnType(DoubleColumnType doubleColumnType, TArg arg);
        TRes VisitDecimalColumnType(DecimalColumnType decimalColumnType, TArg arg);
        TRes VisitDateTimeColumnType(DateTimeColumnType dateTimeColumnType, TArg arg);
        TRes VisitDateTimeOffsetColumnType(DateTimeOffsetColumnType dateTimeColumnType, TArg arg);
        TRes VisitStringColumnType(StringColumnType stringColumnType, TArg arg);
        TRes VisitGuidColumnType(GuidColumnType guidColumnType, TArg arg);
        TRes VisitXmlColumnType(XmlColumnType xmlColumnType, TArg arg);
    }

    internal abstract class ColumnType
    {
        protected ColumnType(bool isNullable)
        {
            this.IsNullable = isNullable;
        }

        public bool IsNullable { get; }

        public abstract TRes Accept<TRes, TArg>(IColumnTypeVisitor<TRes, TArg> visitor, TArg arg);
    }

    internal class BooleanColumnType : ColumnType
    {

        public override TRes Accept<TRes, TArg>(IColumnTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitBooleanColumnType(this, arg);

        public BooleanColumnType(bool isNullable) : base(isNullable)
        {
        }
    }

    internal class ByteColumnType : ColumnType
    {

        public override TRes Accept<TRes, TArg>(IColumnTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitByteColumnType(this, arg);

        public ByteColumnType(bool isNullable) : base(isNullable)
        {
        }
    }

    internal class ByteArrayColumnType : ColumnType
    {
        public int? Size { get; }

        public bool IsFixed { get; }

        public override TRes Accept<TRes, TArg>(IColumnTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitByteArrayColumnType(this, arg);

        public ByteArrayColumnType(bool isNullable, int? size, bool isFixed) : base(isNullable)
        {
            this.Size = size;
            this.IsFixed = isFixed;
        }
    }

    internal class Int16ColumnType : ColumnType
    {
        public override TRes Accept<TRes, TArg>(IColumnTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitInt16ColumnType(this, arg);

        public Int16ColumnType(bool isNullable) : base(isNullable)
        {
        }
    }

    internal class Int32ColumnType : ColumnType
    {
        public override TRes Accept<TRes, TArg>(IColumnTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitInt32ColumnType(this, arg);

        public Int32ColumnType(bool isNullable) : base(isNullable)
        {
        }
    }

    internal class Int64ColumnType : ColumnType
    {
        public override TRes Accept<TRes, TArg>(IColumnTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitInt64ColumnType(this, arg);

        public Int64ColumnType(bool isNullable) : base(isNullable)
        {
        }
    }

    internal class GuidColumnType : ColumnType
    {
        public override TRes Accept<TRes, TArg>(IColumnTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitGuidColumnType(this, arg);

        public GuidColumnType(bool isNullable) : base(isNullable)
        {
        }
    }

    internal class DoubleColumnType : ColumnType
    {
        public override TRes Accept<TRes, TArg>(IColumnTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitDoubleColumnType(this, arg);

        public DoubleColumnType(bool isNullable) : base(isNullable)
        {
        }
    }

    internal class DecimalColumnType : ColumnType
    {
        public int Precision { get; }

        public int Scale { get; }

        public override TRes Accept<TRes, TArg>(IColumnTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitDecimalColumnType(this, arg);

        public DecimalColumnType(bool isNullable, int precision, int scale) : base(isNullable)
        {
            this.Precision = precision;
            this.Scale = scale;
        }
    }

    internal class DateTimeColumnType : ColumnType
    {
        public bool IsDate { get; }

        public override TRes Accept<TRes, TArg>(IColumnTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitDateTimeColumnType(this, arg);

        public DateTimeColumnType(bool isNullable, bool isDate) : base(isNullable)
        {
            this.IsDate = isDate;
        }
    }

    internal class DateTimeOffsetColumnType : ColumnType
    {
        public override TRes Accept<TRes, TArg>(IColumnTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitDateTimeOffsetColumnType(this, arg);

        public DateTimeOffsetColumnType(bool isNullable) : base(isNullable)
        {
        }
    }

    internal class StringColumnType : ColumnType
    {
        public int? Size { get; }

        public bool IsFixed { get; }

        public bool IsUnicode { get; }

        public bool IsText { get; }

        public override TRes Accept<TRes, TArg>(IColumnTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitStringColumnType(this, arg);

        public StringColumnType(bool isNullable, int? size, bool isFixed, bool isUnicode, bool isText) : base(isNullable)
        {
            this.Size = size;
            this.IsFixed = isFixed;
            this.IsUnicode = isUnicode;
            this.IsText = isText;
        }
    }

    internal class XmlColumnType : ColumnType
    {
        public override TRes Accept<TRes, TArg>(IColumnTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitXmlColumnType(this, arg);

        public XmlColumnType(bool isNullable) : base(isNullable)
        {
        }
    }
}