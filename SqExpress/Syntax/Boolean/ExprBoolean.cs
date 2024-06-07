using System.Diagnostics.CodeAnalysis;

namespace SqExpress.Syntax.Boolean
{
    public abstract class ExprBoolean : IExpr
    {
        public abstract TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg);

#if NETSTANDARD
        public static ExprBoolean operator |(ExprBoolean? a, ExprBoolean? b)
            => a == null
                ? b!
                : b == null
                    ? a
                    : new ExprBooleanOr(a, b);


        public static ExprBoolean operator &(ExprBoolean? a, ExprBoolean? b)
            => a == null
                ? b!
                : b == null
                    ? a
                    : new ExprBooleanAnd(a, b);

#else

        [return: NotNullIfNotNull(nameof(a))]
        [return: NotNullIfNotNull(nameof(b))]
        public static ExprBoolean? operator |(ExprBoolean? a, ExprBoolean? b) 
            => a == null 
                ? b 
                : b == null 
                    ? a 
                    : new ExprBooleanOr(a, b);

        [return: NotNullIfNotNull(nameof(a))]
        [return: NotNullIfNotNull(nameof(b))]
        public static ExprBoolean? operator &(ExprBoolean? a, ExprBoolean? b)
            => a == null
                ? b
                : b == null
                    ? a
                    : new ExprBooleanAnd(a, b);

#endif
        public static ExprBoolean operator !(ExprBoolean a) => new ExprBooleanNot(a);
    }
}