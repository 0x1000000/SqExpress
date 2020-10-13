using System.Collections.Generic;

namespace SqExpress.Syntax.Select
{
    public class ExprTableValueConstructor : IExpr
    {
        public ExprTableValueConstructor(IEnumerable<ExprRowValue> items)
        {
            this.Items = items;
        }

        public IEnumerable<ExprRowValue> Items { get; }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprTableValueConstructor(this);
    }
}