using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Functions
{
    public class ExprScalarFunction : ExprValue
    {
        public ExprScalarFunction(ExprDbSchema? schema, ExprFunctionName name, IReadOnlyList<ExprValue>? arguments)
        {
            this.Schema = schema;
            this.Name = name;
            this.Arguments = arguments;
        }

        public ExprDbSchema? Schema { get; }

        public ExprFunctionName Name { get; }

        public IReadOnlyList<ExprValue>? Arguments { get; }

        public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprScalarFunction(this, arg);
    }
}