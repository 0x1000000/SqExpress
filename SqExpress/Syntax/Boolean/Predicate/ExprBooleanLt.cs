using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Boolean.Predicate
{
    public class ExprBooleanLt : ExprPredicateLeftRight
    {
        public ExprBooleanLt(ExprValue left, ExprValue right)
        {
            this.Left = left;
            this.Right = right;
        }

        public override ExprValue Left { get; }

        public override ExprValue Right { get; }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprBooleanLt(this);
    }    
    
    public class ExprBooleanLtEq : ExprPredicateLeftRight
    {
        public ExprBooleanLtEq(ExprValue left, ExprValue right)
        {
            this.Left = left;
            this.Right = right;
        }

        public override ExprValue Left { get; }

        public override ExprValue Right { get; }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprBooleanLtEq(this);
    }
}