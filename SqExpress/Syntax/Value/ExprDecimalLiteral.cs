namespace SqExpress.Syntax.Value
{
    public class ExprDecimalLiteral : ExprLiteral
    {
        public decimal? Value { get; }

        public ExprDecimalLiteral(decimal? value)
        {
            this.Value = value;
        }

        public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprDecimalLiteral(this, arg);

        public static implicit operator ExprDecimalLiteral(decimal value)
            => new ExprDecimalLiteral(value);

        public static implicit operator ExprDecimalLiteral(decimal? value)
            => new ExprDecimalLiteral(value);
    }
}