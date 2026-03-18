using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
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

        ExprTableAlias? IExprTableSource.Alias => null;

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

        public IReadOnlyList<IExprSelecting> ExtractSelecting()
        {
            return [..this.Left.ExtractSelecting(), ..this.Right.ExtractSelecting()];
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

        public enum ExprJoinType
        {
            Inner,
            Left,
            Right,
            Full
        }
    }
}
