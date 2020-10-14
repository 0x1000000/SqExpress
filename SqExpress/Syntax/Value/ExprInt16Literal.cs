namespace SqExpress.Syntax.Value
{
    public class ExprInt16Literal : ExprLiteral
    {
        public short? Value { get; }

        public ExprInt16Literal(short? value)
        {
            this.Value = value;
        }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprInt16Literal(this, arg);

        public static implicit operator ExprInt16Literal(short value)
            => new ExprInt16Literal(value);

        public static implicit operator ExprInt16Literal(short? value)
            => new ExprInt16Literal(value);
    }
}