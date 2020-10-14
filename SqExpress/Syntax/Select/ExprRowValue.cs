using System.Collections.Generic;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Select
{
    public class ExprRowValue : IExpr
    {
        public ExprRowValue(IReadOnlyList<ExprLiteral> items)
        {
            this.Items = items;
        }

        public IReadOnlyList<ExprLiteral> Items { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprRowValue(this, arg);
    }
}