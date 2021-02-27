using SqExpress.Utils;

namespace SqExpress.Syntax.Select
{
    public class ExprCrossedTable : IExprTableSource
    {
        public ExprCrossedTable(IExprTableSource left, IExprTableSource right)
        {
            this.Left = left;
            this.Right = right;
        }

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
    }
}