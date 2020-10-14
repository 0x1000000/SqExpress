using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Functions
{
    public class ExprAggregateFunction : IExprSelecting
    {
        public ExprAggregateFunction(bool isDistinct, ExprFunctionName name, ExprValue expression)
        {
            this.IsDistinct = isDistinct;
            this.Name = name;
            this.Expression = expression;
        }

        public bool IsDistinct { get; }

        public ExprFunctionName Name { get; }

        public ExprValue Expression { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprAggregateFunction(this, arg);
    }
}