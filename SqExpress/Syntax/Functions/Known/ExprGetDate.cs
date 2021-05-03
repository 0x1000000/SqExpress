using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Functions.Known
{
    public class ExprGetDate : ExprValue
    {
        public static readonly ExprGetDate Instance = new ExprGetDate();

        private ExprGetDate() { }

        public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprGetDate(this, arg);
    }
}