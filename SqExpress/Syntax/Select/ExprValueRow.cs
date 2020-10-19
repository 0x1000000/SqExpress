using System.Collections.Generic;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Select
{
    public class ExprValueRow : IExpr
    {
        public ExprValueRow(IReadOnlyList<ExprValue> items)
        {
            this.Items = items;
        }

        public IReadOnlyList<ExprValue> Items { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprValueRow(this, arg);
    }
}