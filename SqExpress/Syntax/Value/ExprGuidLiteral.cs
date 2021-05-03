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

        public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprGuidLiteral(this, arg);

        public static implicit operator ExprGuidLiteral(Guid value)
            => new ExprGuidLiteral(value);

        public static implicit operator ExprGuidLiteral(Guid? value)
            => new ExprGuidLiteral(value);
    }
}