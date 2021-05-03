using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Expressions
{
    public class ExprStringConcat : ExprValue
    {
        public ExprStringConcat(ExprValue left, ExprValue right)
        {
            this.Left = left;
            this.Right = right;
        }

        public ExprValue Left { get; }

        public ExprValue Right { get; }

        public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprStringConcat(this, arg);

        public static ExprStringConcat operator +(ExprValue a, ExprStringConcat b)
            => new ExprStringConcat(a, b);

        public static ExprStringConcat operator +(ExprStringConcat a, ExprValue b)
            => new ExprStringConcat(a, b);

        public static ExprStringConcat operator +(ExprStringConcat a, ExprStringConcat b)
            => new ExprStringConcat(a, b);
    }
}