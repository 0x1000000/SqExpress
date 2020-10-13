namespace SqExpress.Syntax.Value
{
    public class ExprInt16Literal : ExprLiteral
    {
        public short? Value { get; }

        public ExprInt16Literal(short? value)
        {
            this.Value = value;
        }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprShortLiteral(this);

        public static implicit operator ExprInt16Literal(short value)
            => new ExprInt16Literal(value);

        public static implicit operator ExprInt16Literal(short? value)
            => new ExprInt16Literal(value);
    }
}