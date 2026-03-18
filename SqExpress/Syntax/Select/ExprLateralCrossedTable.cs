using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Utils;

namespace SqExpress.Syntax.Select;

public class ExprLateralCrossedTable : IExprTableSource
{
    public ExprLateralCrossedTable(IExprTableSource left, IExprTableSource right, bool outer)
    {
        this.Left = left;
        this.Right = right;
        this.Outer = outer;
    }

    ExprTableAlias? IExprTableSource.Alias => null;

    public IExprTableSource Left { get; }

    public IExprTableSource Right { get; }

    public bool Outer { get; }

    public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprLateralCrossedTable(this, arg);

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
