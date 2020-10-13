namespace SqExpress.Syntax.Value
{
    public class ExprDecimalLiteral : ExprLiteral
    {
        public decimal? Value { get; }

        public ExprDecimalLiteral(decimal? value)
        {
            this.Value = value;
        }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprDecimalLiteral(this);

        public static implicit operator ExprDecimalLiteral(decimal value)
            => new ExprDecimalLiteral(value);

        public static implicit operator ExprDecimalLiteral(decimal? value)
            => new ExprDecimalLiteral(value);
    }
}