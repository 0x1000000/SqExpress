using System.Collections.Generic;

namespace SqExpress.Syntax.Select
{
    public class ExprTableValueConstructor : IExpr
    {
        public ExprTableValueConstructor(IReadOnlyList<ExprValueRow> items)
        {
            this.Items = items;
        }

        public IReadOnlyList<ExprValueRow> Items { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTableValueConstructor(this, arg);
    }
}