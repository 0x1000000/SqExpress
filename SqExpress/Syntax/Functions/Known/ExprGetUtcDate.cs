using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Functions.Known
{
    public class ExprGetUtcDate : ExprValue
    {
        public static readonly ExprGetUtcDate Instance = new ExprGetUtcDate();

        private ExprGetUtcDate() { }

        public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprGetUtcDate(this, arg);
    }
}