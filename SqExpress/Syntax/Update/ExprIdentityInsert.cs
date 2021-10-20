using System.Collections.Generic;
using SqExpress.Syntax.Names;

namespace SqExpress.Syntax.Update
{
    public class ExprIdentityInsert : IExprExec
    {
        public ExprIdentityInsert(ExprInsert insert, IReadOnlyList<ExprColumnName> identityColumns)
        {
            this.Insert = insert;
            this.IdentityColumns = identityColumns;
        }

        public ExprInsert Insert { get; }

        public IReadOnlyList<ExprColumnName> IdentityColumns { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprIdentityInsert(this, arg);
    }
}