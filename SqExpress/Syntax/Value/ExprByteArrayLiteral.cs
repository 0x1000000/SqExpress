using System.Collections.Generic;

namespace SqExpress.Syntax.Value
{
    public class ExprByteArrayLiteral : ExprLiteral
    {
        public IReadOnlyList<byte>? Value { get; }

        public ExprByteArrayLiteral(IReadOnlyList<byte>? value)
        {
            this.Value = value;
        }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprByteArrayLiteral(this, arg);
    }
}