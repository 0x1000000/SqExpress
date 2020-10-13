using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Expressions
{
    public class ExprSum : ExprArithmetic
    {
        public ExprSum(ExprValue left, ExprValue right)
        {
            this.Left = left;
            this.Right = right;
        }

        public ExprValue Left { get; }

        public ExprValue Right { get; }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprSum(this);
    }

    public class ExprStringConcat : ExprValue
    {
        public ExprStringConcat(ExprValue left, ExprValue right)
        {
            this.Left = left;
            this.Right = right;
        }

        public ExprValue Left { get; }

        public ExprValue Right { get; }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprStringConcat(this);

        public static ExprStringConcat operator +(ExprValue a, ExprStringConcat b)
            => new ExprStringConcat(a, b);

        public static ExprStringConcat operator +(ExprStringConcat a, ExprValue b)
            => new ExprStringConcat(a, b);

        public static ExprStringConcat operator +(ExprStringConcat a, ExprStringConcat b)
            => new ExprStringConcat(a, b);
    }
}