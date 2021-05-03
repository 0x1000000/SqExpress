namespace SqExpress.Syntax.Value
{
    public class ExprUnsafeValue : ExprValue
    {
        public string UnsafeValue { get; }

        public ExprUnsafeValue(string unsafeValue)
        {
            this.UnsafeValue = unsafeValue;
        }

        public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprUnsafeValue(this, arg);
    }
}