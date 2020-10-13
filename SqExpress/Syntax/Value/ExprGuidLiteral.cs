using System;

namespace SqExpress.Syntax.Value
{
    public class ExprGuidLiteral : ExprLiteral
    {
        public Guid? Value { get; }

        public ExprGuidLiteral(Guid? value)
        {
            this.Value = value;
        }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprGuidLiteral(this);

        public static implicit operator ExprGuidLiteral(Guid value)
            => new ExprGuidLiteral(value);

        public static implicit operator ExprGuidLiteral(Guid? value)
            => new ExprGuidLiteral(value);
    }
}