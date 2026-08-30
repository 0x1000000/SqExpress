using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Functions.Known;

/// <summary>Concatenates non-null values from the current SQL group.</summary>
public class ExprStringAgg : IExprSelecting
{
    public ExprStringAgg(ExprValue expression, ExprValue separator, ExprOrderBy? orderBy)
    {
        this.Expression = expression;
        this.Separator = separator;
        this.OrderBy = orderBy;
    }

    public ExprValue Expression { get; }

    public ExprValue Separator { get; }

    public ExprOrderBy? OrderBy { get; }

    public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprStringAgg(this, arg);

    public TRes Accept<TRes, TArg>(IExprSelectingVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprStringAgg(this, arg);
}
