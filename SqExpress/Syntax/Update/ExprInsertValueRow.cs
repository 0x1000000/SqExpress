using System.Collections.Generic;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Update
{
    public class ExprInsertValueRow : IExpr
    {
        public ExprInsertValueRow(IReadOnlyList<IExprAssigning> items)
        {
            this.Items = items;
        }

        public IReadOnlyList<IExprAssigning> Items { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprInsertValueRow(this, arg);
    }
}