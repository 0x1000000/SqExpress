using System;

namespace SqExpress.Syntax.Value
{
    public class ExprDateTimeOffsetLiteral : ExprLiteral
    {
        public ExprDateTimeOffsetLiteral(DateTimeOffset? value)
        {
            this.Value = value;
        }

        public DateTimeOffset? Value { get; }

        public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprDateTimeOffsetLiteral(this, arg);

        public static implicit operator ExprDateTimeOffsetLiteral(DateTime value)
            => new ExprDateTimeOffsetLiteral(value);

        public static implicit operator ExprDateTimeOffsetLiteral(DateTime? value)
            => new ExprDateTimeOffsetLiteral(value);

    }
}