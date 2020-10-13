using System.Collections.Generic;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Functions
{
    public class ExprOver : IExpr
    {
        public ExprOver(IReadOnlyList<ExprValue>? partitions, ExprOrderBy? orderBy)
        {
            this.Partitions = partitions;
            this.OrderBy = orderBy;
        }

        public IReadOnlyList<ExprValue>? Partitions { get; }

        public ExprOrderBy? OrderBy { get; }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprOver(this);
    }
}