namespace SqExpress.Syntax.Value
{
    public class ExprInt32Literal : ExprLiteral
    {
        public int? Value { get; }

        public ExprInt32Literal(int? value)
        {
            this.Value = value;
        }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprIntLiteral(this);

        public static implicit operator ExprInt32Literal(int value)
            => new ExprInt32Literal(value);

        public static implicit operator ExprInt32Literal(int? value)
            => new ExprInt32Literal(value);
    }
}