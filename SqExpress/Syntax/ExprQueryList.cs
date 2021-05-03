using System.Collections.Generic;
using SqExpress.Utils;

namespace SqExpress.Syntax
{
    public class ExprQueryList : IExprQuery
    {
        private readonly IExprQuery _query;

        public ExprQueryList(IReadOnlyList<IExprComplete> expressions)
        {
            this.Expressions = expressions.AssertNotEmpty("Expression list cannot be empty");

            IExprQuery? query = null;

            foreach (var expression in this.Expressions)
            {
                if (expression is IExprQuery q)
                {
                    if (query != null)
                    {
                        throw new SqExpressException("Expression list can contain only one selecting query");
                    }
                    query = q;
                }
            }

            this._query = query ?? throw new SqExpressException("Could not find any selecting query in the expression list");
        }

        public IReadOnlyList<IExprComplete> Expressions { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        {
            return visitor.VisitExprQueryList(this, arg);
        }

        public IReadOnlyList<string?> GetOutputColumnNames()
        {
            return this._query.GetOutputColumnNames();
        }
    }
}