using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Utils;

namespace SqExpress.Syntax.Select
{
    public class ExprJoinedTable : IExprTableSource
    {
        public ExprJoinedTable(IExprTableSource left, ExprJoinType joinType, IExprTableSource right, ExprBoolean searchCondition)
        {
            this.Left = left;
            this.JoinType = joinType;
            this.Right = right;
            this.SearchCondition = searchCondition;
        }

        public IExprTableSource Left { get; }

        public ExprJoinType JoinType { get; }

        public IExprTableSource Right { get; }

        public ExprBoolean SearchCondition { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprJoinedTable(this, arg);

        public TableMultiplication ToTableMultiplication()
        {
            if (this.JoinType != ExprJoinType.Inner)
            {
                throw new SqExpressException($"'{this.JoinType} JOIN' does not support converting to 'USING' list");
            }

            var left = this.Left.ToTableMultiplication();
            var right = this.Right.ToTableMultiplication();

            var condition = this.SearchCondition;
            if (left.On != null)
            {
                condition = left.On & condition;
            }
            if (right.On != null)
            {
                condition = condition & right.On;
            }

            return new TableMultiplication(Helpers.Combine(left.Tables, right.Tables), condition);
        }

        public enum ExprJoinType
        {
            Inner,
            Left,
            Right,
            Full
        }
    }
}