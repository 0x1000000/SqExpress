namespace SqExpress.Syntax.Value
{
    public class ExprByteLiteral : ExprLiteral
    {
        public byte? Value { get; }

        public ExprByteLiteral(byte? value)
        {
            this.Value = value;
        }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprByteLiteral(this);

        public static implicit operator ExprByteLiteral(byte value)
            => new ExprByteLiteral(value);

        public static implicit operator ExprByteLiteral(byte? value)
            => new ExprByteLiteral(value);
    }
}