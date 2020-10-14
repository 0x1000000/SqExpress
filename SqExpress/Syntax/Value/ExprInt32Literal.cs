namespace SqExpress.Syntax.Value
{
    public class ExprInt32Literal : ExprLiteral
    {
        public int? Value { get; }

        public ExprInt32Literal(int? value)
        {
            this.Value = value;
        }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprInt32Literal(this, arg);

        public static implicit operator ExprInt32Literal(int value)
            => new ExprInt32Literal(value);

        public static implicit operator ExprInt32Literal(int? value)
            => new ExprInt32Literal(value);
    }
}