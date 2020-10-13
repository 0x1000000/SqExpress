using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Boolean.Predicate
{
    public abstract class ExprPredicateLeftRight: ExprPredicate
    {
        public abstract ExprValue Left { get; }
        public abstract ExprValue Right { get; }
    }
}