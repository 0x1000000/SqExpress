using System.Collections.Generic;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Functions.Known
{
    public class ExprFuncCoalesce : ExprValue
    {
        public ExprFuncCoalesce(ExprValue test, IReadOnlyList<ExprValue> alts)
        {
            this.Test = test;
            this.Alts = alts;
        }

        public ExprValue Test { get; }

        public IReadOnlyList<ExprValue> Alts { get; }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprFuncCoalesce(this, arg);
    }
}