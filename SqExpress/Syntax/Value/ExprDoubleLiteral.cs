namespace SqExpress.Syntax.Value
{
    public class ExprDoubleLiteral : ExprLiteral
    {
        public double? Value { get; }

        public ExprDoubleLiteral(double? value)
        {
            this.Value = value;
        }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprDoubleLiteral(this, arg);

        public static implicit operator ExprDoubleLiteral(double value)
            => new ExprDoubleLiteral(value);

        public static implicit operator ExprDoubleLiteral(double? value)
            => new ExprDoubleLiteral(value);
    }
}