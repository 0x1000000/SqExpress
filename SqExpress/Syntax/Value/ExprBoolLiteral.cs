namespace SqExpress.Syntax.Value
{
    public class ExprBoolLiteral : ExprLiteral
    {
        public bool? Value { get; }

        public ExprBoolLiteral(bool? value)
        {
            this.Value = value;
        }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprBoolLiteral(this);

        public static implicit operator ExprBoolLiteral(bool value)
            => new ExprBoolLiteral(value);

        public static implicit operator ExprBoolLiteral(bool? value)
            => new ExprBoolLiteral(value);
    }
}