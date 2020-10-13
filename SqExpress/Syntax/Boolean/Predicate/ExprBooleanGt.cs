﻿using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Boolean.Predicate
{
    public class ExprBooleanGt : ExprPredicateLeftRight
    {
        public ExprBooleanGt(ExprValue left, ExprValue right)
        {
            this.Left = left;
            this.Right = right;
        }

        public override ExprValue Left { get; }

        public override ExprValue Right { get; }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprBooleanGt(this);
    }
}