using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
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

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprCrossedTable(this);

        public (IReadOnlyList<IExprTableSource> Tables, ExprBoolean? On) ToTableMultiplication()
        {
            var left = this.Left.ToTableMultiplication();
            var right = this.Right.ToTableMultiplication();

            var condition = Helpers.CombineNotNull(left.On, right.On, (l, r) => l & r);

            return (Helpers.Combine(left.Tables, right.Tables), condition);

        }
    }
}