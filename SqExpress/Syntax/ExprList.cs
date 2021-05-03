using System.Collections.Generic;
using SqExpress.Utils;

namespace SqExpress.Syntax
{
    public class ExprList : IExprExec
    {
        public ExprList(IReadOnlyList<IExprExec> expressions)
        {
            this.Expressions = expressions.AssertNotEmpty("Expression list cannot be empty");
        }

        public IReadOnlyList<IExprExec> Expressions { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        {
            return visitor.VisitExprList(this, arg);
        }
    }
}