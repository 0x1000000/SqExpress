using System;

namespace SqExpress.Syntax.Value
{
    public class ExprDateTimeLiteral : ExprLiteral
    {
        public ExprDateTimeLiteral(DateTime? value)
        {
            this.Value = value;
        }

        public DateTime? Value { get; }

        public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprDateTimeLiteral(this, arg);

        public static implicit operator ExprDateTimeLiteral(DateTime value)
            => new ExprDateTimeLiteral(value);

        public static implicit operator ExprDateTimeLiteral(DateTime? value)
            => new ExprDateTimeLiteral(value);

    }
}