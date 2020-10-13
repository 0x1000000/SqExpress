namespace SqExpress.Syntax.Value
{
    public class ExprDoubleLiteral : ExprLiteral
    {
        public double? Value { get; }

        public ExprDoubleLiteral(double? value)
        {
            this.Value = value;
        }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprDoubleLiteral(this);

        public static implicit operator ExprDoubleLiteral(double value)
            => new ExprDoubleLiteral(value);

        public static implicit operator ExprDoubleLiteral(double? value)
            => new ExprDoubleLiteral(value);
    }
}