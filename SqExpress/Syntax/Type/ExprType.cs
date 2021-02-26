namespace SqExpress.Syntax.Type
{
    public abstract class ExprType : IExpr
    {
        public abstract TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg);
    }

    public class ExprTypeBoolean : ExprType
    {
        public static readonly ExprTypeBoolean Instance = new ExprTypeBoolean();

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeBoolean(this, arg);
    }

    public class ExprTypeByte : ExprType
    {
        public static readonly ExprTypeByte Instance = new ExprTypeByte();

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeByte(this, arg);
    }

    public class ExprTypeByteArray : ExprType
    {
        public ExprTypeByteArray(int? size)
        {
            this.Size = size;
        }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeByteArray(this, arg);

        public int? Size { get; }
    }

    public class ExprTypeInt16 : ExprType
    {
        public static readonly ExprTypeInt16 Instance = new ExprTypeInt16();

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeInt16(this, arg);
    }

    public class ExprTypeInt32 : ExprType
    {
        public static readonly ExprTypeInt32 Instance = new ExprTypeInt32();

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeInt32(this, arg);
    }

    public class ExprTypeInt64 : ExprType
    {
        public static readonly ExprTypeInt64 Instance = new ExprTypeInt64();

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeInt64(this, arg);
    }

    public class ExprTypeDecimal : ExprType
    {
        public ExprTypeDecimal(DecimalPrecisionScale? precisionScale)
        {
            this.PrecisionScale = precisionScale;
        }

        public DecimalPrecisionScale? PrecisionScale { get; }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeDecimal(this, arg);
    }

    public class ExprTypeDouble : ExprType
    {
        public static readonly ExprTypeDouble Instance = new ExprTypeDouble();

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeDouble(this, arg);
    }

    public class ExprTypeDateTime : ExprType
    {
        public ExprTypeDateTime(bool isDate)
        {
            this.IsDate = isDate;
        }

        public bool IsDate { get; }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeDateTime(this, arg);
    }

    public class ExprTypeGuid : ExprType
    {
        public static readonly ExprTypeGuid Instance = new ExprTypeGuid();

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeGuid(this, arg);
    }

    public class ExprTypeString : ExprType
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

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTypeString(this, arg);
    }
}