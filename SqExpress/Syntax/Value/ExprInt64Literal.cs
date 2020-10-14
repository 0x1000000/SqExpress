namespace SqExpress.Syntax.Value
{
    public class ExprInt64Literal : ExprLiteral
    {
        public long? Value { get; }

        public ExprInt64Literal(long? value)
        {
            this.Value = value;
        }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprInt64Literal(this, arg);

        public static implicit operator ExprInt64Literal(long value)
            => new ExprInt64Literal(value);

        public static implicit operator ExprInt64Literal(long? value)
            => new ExprInt64Literal(value);
    }
}