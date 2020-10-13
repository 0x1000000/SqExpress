using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Select
{
    public class ExprOrderBy : IExpr
    {
        public ExprOrderBy(IReadOnlyList<ExprOrderByItem> orderList)
        {
            this.OrderList = orderList;
        }

        public IReadOnlyList<ExprOrderByItem> OrderList { get; }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprOrderBy(this);
    }

    public class ExprOrderByOffsetFetch : IExpr
    {
        public ExprOrderByOffsetFetch(IReadOnlyList<ExprOrderByItem> orderList, ExprOffsetFetch offsetFetch)
        {
            this.OrderList = orderList;
            this.OffsetFetch = offsetFetch;
        }

        public IReadOnlyList<ExprOrderByItem> OrderList { get; }

        public ExprOffsetFetch OffsetFetch { get; }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprOrderByOffsetFetch(this);
    }

    public class ExprOrderByItem : IExpr
    {
        public ExprOrderByItem(ExprValue value, bool descendant)
        {
            this.Value = value;
            this.Descendant = descendant;
        }

        public ExprValue Value { get; }

        public bool Descendant { get; }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprOrderByItem(this);

        public static implicit operator ExprOrderByItem(ExprColumn column)=> new ExprOrderByItem(column, false);
    }

    public class ExprOffsetFetch : IExpr
    {
        public ExprOffsetFetch(ExprInt32Literal offset, ExprInt32Literal? fetch)
        {
            this.Offset = offset;
            this.Fetch = fetch;
        }

        public ExprInt32Literal Offset { get; }

        public ExprInt32Literal? Fetch { get; }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprOffsetFetch(this);
    }
}