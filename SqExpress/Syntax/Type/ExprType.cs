namespace SqExpress.Syntax.Type
{
    public abstract class ExprType : IExpr
    {
        public abstract TRes Accept<TRes>(IExprVisitor<TRes> visitor);
    }

    public class ExprTypeBoolean : ExprType
    {
        public static readonly ExprTypeBoolean Instance = new ExprTypeBoolean();

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprTypeBoolean(this);
    }

    public class ExprTypeByte : ExprType
    {
        public static readonly ExprTypeByte Instance = new ExprTypeByte();

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprTypeByte(this);
    }

    public class ExprTypeInt16 : ExprType
    {
        public static readonly ExprTypeInt16 Instance = new ExprTypeInt16();

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprTypeInt16(this);
    }

    public class ExprTypeInt32 : ExprType
    {
        public static readonly ExprTypeInt32 Instance = new ExprTypeInt32();

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprTypeInt32(this);
    }

    public class ExprTypeInt64 : ExprType
    {
        public static readonly ExprTypeInt64 Instance = new ExprTypeInt64();

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprTypeInt64(this);
    }

    public class ExprTypeDecimal : ExprType
    {
        public ExprTypeDecimal(DecimalPrecisionScale? precisionScale)
        {
            this.PrecisionScale = precisionScale;
        }

        public DecimalPrecisionScale? PrecisionScale { get; }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprTypeDecimal(this);
    }

    public class ExprTypeDouble : ExprType
    {
        public static readonly ExprTypeDouble Instance = new ExprTypeDouble();

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprTypeDouble(this);
    }

    public class ExprTypeDateTime : ExprType
    {
        public ExprTypeDateTime(bool isDate)
        {
            this.IsDate = isDate;
        }

        public bool IsDate { get; }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprTypeDateTime(this);
    }

    public class ExprTypeGuid : ExprType
    {
        public static readonly ExprTypeGuid Instance = new ExprTypeGuid();

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprTypeGuid(this);
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

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprTypeString(this);
    }
}