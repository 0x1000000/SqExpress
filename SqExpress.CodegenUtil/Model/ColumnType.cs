namespace SqExpress.CodeGenUtil.Model
{
    internal interface IColumnTypeVisitor
    {
        void VisitBooleanColumnType(BooleanColumnType booleanColumnType);
        void VisitByteColumnType(ByteColumnType byteColumnType);
        void VisitByteArrayColumnType(ByteArrayColumnType byteArrayColumnType);
        void VisitInt16ColumnType(Int16ColumnType int16ColumnType);
        void VisitInt32ColumnType(Int32ColumnType int32ColumnType);
        void VisitInt64ColumnType(Int64ColumnType int64ColumnType);
        void VisitDoubleColumnType(DoubleColumnType doubleColumnType);
        void VisitDecimalColumnType(DecimalColumnType decimalColumnType);
        void VisitDateTimeColumnType(DateTimeColumnType dateTimeColumnType);
        void VisitStringColumnType(StringColumnType stringColumnType);
        void VisitGuidColumnType(GuidColumnType guidColumnType);
        void VisitXmlColumnType(XmlColumnType xmlColumnType);
    }

    internal abstract class ColumnType
    {
        protected ColumnType(bool isNullable)
        {
            this.IsNullable = isNullable;
        }

        public bool IsNullable { get; }

        public abstract void Accept(IColumnTypeVisitor visitor);
    }

    internal class BooleanColumnType : ColumnType
    {

        public override void Accept(IColumnTypeVisitor visitor)
            => visitor.VisitBooleanColumnType(this);

        public BooleanColumnType(bool isNullable) : base(isNullable)
        {
        }
    }

    internal class ByteColumnType : ColumnType
    {

        public override void Accept(IColumnTypeVisitor visitor)
            => visitor.VisitByteColumnType(this);

        public ByteColumnType(bool isNullable) : base(isNullable)
        {
        }
    }

    internal class ByteArrayColumnType : ColumnType
    {
        public int? Size { get; }

        public bool IsFixed { get; }

        public override void Accept(IColumnTypeVisitor visitor)
            => visitor.VisitByteArrayColumnType(this);

        public ByteArrayColumnType(bool isNullable, int? size, bool isFixed) : base(isNullable)
        {
            this.Size = size;
            this.IsFixed = isFixed;
        }
    }

    internal class Int16ColumnType : ColumnType
    {
        public override void Accept(IColumnTypeVisitor visitor)
            => visitor.VisitInt16ColumnType(this);

        public Int16ColumnType(bool isNullable) : base(isNullable)
        {
        }
    }

    internal class Int32ColumnType : ColumnType
    {
        public override void Accept(IColumnTypeVisitor visitor)
            => visitor.VisitInt32ColumnType(this);

        public Int32ColumnType(bool isNullable) : base(isNullable)
        {
        }
    }

    internal class Int64ColumnType : ColumnType
    {
        public override void Accept(IColumnTypeVisitor visitor)
            => visitor.VisitInt64ColumnType(this);

        public Int64ColumnType(bool isNullable) : base(isNullable)
        {
        }
    }

    internal class GuidColumnType : ColumnType
    {
        public override void Accept(IColumnTypeVisitor visitor)
            => visitor.VisitGuidColumnType(this);

        public GuidColumnType(bool isNullable) : base(isNullable)
        {
        }
    }

    internal class DoubleColumnType : ColumnType
    {
        public override void Accept(IColumnTypeVisitor visitor)
            => visitor.VisitDoubleColumnType(this);

        public DoubleColumnType(bool isNullable) : base(isNullable)
        {
        }
    }

    internal class DecimalColumnType : ColumnType
    {
        public int Precision { get; }

        public int Scale { get; }

        public override void Accept(IColumnTypeVisitor visitor)
            => visitor.VisitDecimalColumnType(this);

        public DecimalColumnType(bool isNullable, int precision, int scale) : base(isNullable)
        {
            this.Precision = precision;
            this.Scale = scale;
        }
    }

    internal class DateTimeColumnType : ColumnType
    {
        public bool IsDate { get; }

        public override void Accept(IColumnTypeVisitor visitor)
            => visitor.VisitDateTimeColumnType(this);

        public DateTimeColumnType(bool isNullable, bool isDate) : base(isNullable)
        {
            this.IsDate = isDate;
        }
    }

    internal class StringColumnType : ColumnType
    {
        public int? Size { get; }

        public bool IsFixed { get; }

        public bool IsUnicode { get; }

        public bool IsText { get; }

        public override void Accept(IColumnTypeVisitor visitor)
            => visitor.VisitStringColumnType(this);

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
        public override void Accept(IColumnTypeVisitor visitor)
            => visitor.VisitXmlColumnType(this);

        public XmlColumnType(bool isNullable) : base(isNullable)
        {
        }
    }
}