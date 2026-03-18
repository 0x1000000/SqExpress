using SqExpress.Syntax.Select;

namespace SqExpress.Syntax.Value
{
    public class ExprSelectingValue : ExprValue
    {
        public ExprSelectingValue(IExprSelecting selecting)
        {
            this.Selecting = selecting;
        }

        public IExprSelecting Selecting { get; }

        public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprSelectingValue(this, arg);
    }
}
