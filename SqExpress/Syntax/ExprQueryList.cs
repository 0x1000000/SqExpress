using System.Collections.Generic;
using SqExpress.Syntax.Select;
using SqExpress.Utils;

namespace SqExpress.Syntax
{
    public class ExprQueryList : IExprQuery
    {
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
        }

        public IReadOnlyList<IExprComplete> Expressions { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        {
            return visitor.VisitExprQueryList(this, arg);
        }

        public IReadOnlyList<IExprSelecting> ExtractSelecting()
        {
            foreach (var expression in this.Expressions)
            {
                if (expression is IExprSelectingSource query)
                {
                    return query.ExtractSelecting();
                }
            }
            return [];
        }

        public IReadOnlyList<string?> GetOutputColumnNames()
        {
            foreach (var expression in this.Expressions)
            {
                if (expression is IExprQuery query)
                {
                    return query.GetOutputColumnNames();
                }
            }
            return [];
        }
    }
}
