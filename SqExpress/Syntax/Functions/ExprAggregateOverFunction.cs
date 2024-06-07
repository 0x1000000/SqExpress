using SqExpress.Syntax.Select;

namespace SqExpress.Syntax.Functions;

public class ExprAggregateOverFunction : IExprSelecting
{
    public ExprAggregateOverFunction(ExprAggregateFunction function, ExprOver over)
    {
        this.Function = function;
        this.Over = over;
    }

    public ExprAggregateFunction Function { get; }

    public ExprOver Over { get; }

    public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprAggregateOverFunction(this, arg);
}
