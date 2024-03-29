﻿namespace SqExpress.Syntax.Value
{
    public class ExprBoolLiteral : ExprLiteral
    {
        public bool? Value { get; }

        public ExprBoolLiteral(bool? value)
        {
            this.Value = value;
        }

        public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprBoolLiteral(this, arg);

        public static implicit operator ExprBoolLiteral(bool value)
            => new ExprBoolLiteral(value);

        public static implicit operator ExprBoolLiteral(bool? value)
            => new ExprBoolLiteral(value);
    }
}