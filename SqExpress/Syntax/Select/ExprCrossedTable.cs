using SqExpress.Utils;
using System.Collections.Generic;
using SqExpress.Syntax.Names;

namespace SqExpress.Syntax.Select;

public class ExprCrossedTable : IExprTableSource
{
    public ExprCrossedTable(IExprTableSource left, IExprTableSource right)
    {
        this.Left = left;
        this.Right = right;
    }

    ExprTableAlias? IExprTableSource.Alias => null;

    public IExprTableSource Left { get; }

    public IExprTableSource Right { get; }

    public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprCrossedTable(this, arg);

    public TableMultiplication ToTableMultiplication()
    {
        var left = this.Left.ToTableMultiplication();
        var right = this.Right.ToTableMultiplication();

        var condition = Helpers.CombineNotNull(left.On, right.On, (l, r) => l & r);

        return new TableMultiplication(Helpers.Combine(left.Tables, right.Tables), condition);
    }

    public IReadOnlyList<IExprSelecting> ExtractSelecting()
    {
        return [.. this.Left.ExtractSelecting(), .. this.Right.ExtractSelecting()];
    }

    public IExprSubQuery CreateSubQuery()
    {
        var left = this.Left.ExtractSelecting();
        var right = this.Right.ExtractSelecting();

        if (left.Count == 0 || right.Count == 0)
        {
            return SqQueryBuilder.Select(SqQueryBuilder.AllColumns()).From(this).Done();
        }

        return SqQueryBuilder.Select([.. left, .. right]).From(this).Done();
    }
}
