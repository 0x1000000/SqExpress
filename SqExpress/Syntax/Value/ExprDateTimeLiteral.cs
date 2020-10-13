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

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprDateTimeLiteral(this);

        public static implicit operator ExprDateTimeLiteral(DateTime value)
            => new ExprDateTimeLiteral(value);

        public static implicit operator ExprDateTimeLiteral(DateTime? value)
            => new ExprDateTimeLiteral(value);

    }
}