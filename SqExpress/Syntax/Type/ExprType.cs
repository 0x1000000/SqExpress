namespace SqExpress.Syntax.Type
{
    public abstract class ExprType : IExpr
    {
        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        {
            return this.Accept((IExprTypeVisitor<TRes, TArg>)visitor, arg);
        }

        public abstract TRes Accept<TRes, TArg>(IExprTypeVisitor<TRes, TArg> visitor, TArg arg);
    }

    public class ExprTypeBoolean : ExprType
    {
        public static readonly ExprTypeBoolean Instance = new ExprTypeBoolean();

        public override TRes Accept<TRes, TArg>(IExprTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeBoolean(this, arg);
    }

    public class ExprTypeByte : ExprType
    {
        public static readonly ExprTypeByte Instance = new ExprTypeByte();

        public override TRes Accept<TRes, TArg>(IExprTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeByte(this, arg);
    }

    public interface IExprTypeByteArray
    {
        int? GetSize();
    }

    public abstract class ExprTypeByteArrayBase : ExprType, IExprTypeByteArray
    {
        public abstract int? GetSize();
    }

    public class ExprTypeFixSizeByteArray : ExprTypeByteArrayBase
    {
        public ExprTypeFixSizeByteArray(int size)
        {
            this.Size = size;
        }

        public override TRes Accept<TRes, TArg>(IExprTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeFixSizeByteArray(this, arg);

        public override int? GetSize() => this.Size;

        public int Size { get; }
    }

    public class ExprTypeByteArray : ExprTypeByteArrayBase
    {
        public ExprTypeByteArray(int? size)
        {
            this.Size = size;
        }

        public override TRes Accept<TRes, TArg>(IExprTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeByteArray(this, arg);

        public override int? GetSize() => this.Size;

        public int? Size { get; }
    }

    public class ExprTypeInt16 : ExprType
    {
        public static readonly ExprTypeInt16 Instance = new ExprTypeInt16();

        public override TRes Accept<TRes, TArg>(IExprTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeInt16(this, arg);
    }

    public class ExprTypeInt32 : ExprType
    {
        public static readonly ExprTypeInt32 Instance = new ExprTypeInt32();

        public override TRes Accept<TRes, TArg>(IExprTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeInt32(this, arg);
    }

    public class ExprTypeInt64 : ExprType
    {
        public static readonly ExprTypeInt64 Instance = new ExprTypeInt64();

        public override TRes Accept<TRes, TArg>(IExprTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeInt64(this, arg);
    }

    public class ExprTypeDecimal : ExprType
    {
        public ExprTypeDecimal(DecimalPrecisionScale? precisionScale)
        {
            this.PrecisionScale = precisionScale;
        }

        public DecimalPrecisionScale? PrecisionScale { get; }

        public override TRes Accept<TRes, TArg>(IExprTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeDecimal(this, arg);
    }

    public class ExprTypeDouble : ExprType
    {
        public static readonly ExprTypeDouble Instance = new ExprTypeDouble();

        public override TRes Accept<TRes, TArg>(IExprTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeDouble(this, arg);
    }

    public class ExprTypeDateTime : ExprType
    {
        public ExprTypeDateTime(bool isDate)
        {
            this.IsDate = isDate;
        }

        public bool IsDate { get; }

        public override TRes Accept<TRes, TArg>(IExprTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeDateTime(this, arg);
    }

    public class ExprTypeGuid : ExprType
    {
        public static readonly ExprTypeGuid Instance = new ExprTypeGuid();

        public override TRes Accept<TRes, TArg>(IExprTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeGuid(this, arg);
    }


    public abstract class ExprTypeStringBase : ExprType, IExprTypeString
    {
        public abstract int? GetSize();
    }

    public interface IExprTypeString
    {
        int? GetSize();
    }

    public class ExprTypeString : ExprTypeStringBase
    {
        public ExprTypeString(int? size, bool isUnicode, bool isText)
        {
            this.Size = size;
            this.IsUnicode = isUnicode;
            this.IsText = isText;
        }

        public bool IsUnicode { get; }

        public bool IsText { get; }

        public int? Size { get; }

        public override TRes Accept<TRes, TArg>(IExprTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeString(this, arg);

        public override int? GetSize() => this.Size;
    }

    public class ExprTypeXml : ExprTypeStringBase
    {
        private ExprTypeXml() { }

        public static readonly ExprTypeXml Instance = new ExprTypeXml();

        public override TRes Accept<TRes, TArg>(IExprTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeXml(this, arg);

        public int? Size => null;

        public override int? GetSize() => this.Size;
    }

    public class ExprTypeFixSizeString : ExprTypeStringBase
    {
        public ExprTypeFixSizeString(int size, bool isUnicode)
        {
            this.Size = size;
            this.IsUnicode = isUnicode;
        }

        public bool IsUnicode { get; }

        public int Size { get; }

        public override int? GetSize() => this.Size;

        public override TRes Accept<TRes, TArg>(IExprTypeVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeFixSizeString(this, arg);
    }
}