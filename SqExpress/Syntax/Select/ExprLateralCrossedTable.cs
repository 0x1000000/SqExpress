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
}
