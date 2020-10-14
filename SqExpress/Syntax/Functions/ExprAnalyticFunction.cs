using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Functions
{
    public class ExprAnalyticFunction : IExprSelecting
    {
        public ExprAnalyticFunction(ExprFunctionName name, IReadOnlyList<ExprValue>? arguments, ExprOver over)
        {
            this.Name = name;
            this.Arguments = arguments;
            this.Over = over;
        }

        public ExprFunctionName Name { get; }

        public IReadOnlyList<ExprValue>? Arguments { get; }

        public ExprOver Over { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprAnalyticFunction(this, arg);
    }
}