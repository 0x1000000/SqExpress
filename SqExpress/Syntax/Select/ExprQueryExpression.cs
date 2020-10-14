using System.Collections.Generic;

namespace SqExpress.Syntax.Select
{
    public class ExprQueryExpression : IExprQueryExpression
    {
        public ExprQueryExpression(IExprSubQuery left, IExprSubQuery right, ExprQueryExpressionType queryExpressionType)
        {
            this.Left = left;
            this.Right = right;
            this.QueryExpressionType = queryExpressionType;
        }

        public IExprSubQuery Left { get; }

        public IExprSubQuery Right { get; }

        public ExprQueryExpressionType QueryExpressionType { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprQueryExpression(this, arg);

        public IReadOnlyList<string?> GetOutputColumnNames() => this.Left.GetOutputColumnNames();
    }

    public enum ExprQueryExpressionType
    {
        UnionAll,
        Union,
        Except,
        Intersect,
    }
}