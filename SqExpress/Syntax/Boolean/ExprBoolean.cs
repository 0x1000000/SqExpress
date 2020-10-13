namespace SqExpress.Syntax.Boolean
{
    public abstract class ExprBoolean : IExpr
    {
        public abstract TRes Accept<TRes>(IExprVisitor<TRes> visitor);

        public static ExprBoolean operator |(ExprBoolean? a, ExprBoolean b) => a == null ? b : new ExprBooleanOr(a, b);

        public static ExprBoolean operator &(ExprBoolean? a, ExprBoolean b) => a == null ? b : new ExprBooleanAnd(a, b);

        public static ExprBoolean operator !(ExprBoolean a) => new ExprBooleanNot(a);
    }
}