using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;

namespace SqExpress.Syntax.Update
{
    public class ExprUpdate : IExprExec
    {
        public ExprUpdate(ExprTable target, IReadOnlyList<ExprColumnSetClause> setClause, IExprTableSource? source, ExprBoolean? filter)
        {
            this.Target = target;
            this.SetClause = setClause;
            this.Source = source;
            this.Filter = filter;
        }

        public ExprTable Target { get; }

        public IReadOnlyList<ExprColumnSetClause> SetClause { get; }

        public IExprTableSource? Source { get; }

        public ExprBoolean? Filter { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprUpdate(this, arg);
    }
}