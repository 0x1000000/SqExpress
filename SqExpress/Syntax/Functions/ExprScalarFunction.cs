using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Functions
{
    public class ExprScalarFunction : ExprValue
    {
        public ExprScalarFunction(ExprSchemaName? schema, ExprFunctionName name, IReadOnlyList<ExprValue> arguments)
        {
            this.Schema = schema;
            this.Name = name;
            this.Arguments = arguments;
        }

        public ExprSchemaName? Schema { get; }

        public ExprFunctionName Name { get; }

        public IReadOnlyList<ExprValue> Arguments { get; }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprScalarFunction(this);
    }
}