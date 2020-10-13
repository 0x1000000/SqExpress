using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Functions.Known
{
    public class ExprFuncIsNull : ExprValue
    {
        public ExprFuncIsNull(ExprValue test, ExprValue alt)
        {
            this.Test = test;
            this.Alt = alt;
        }

        public ExprValue Test { get; }

        public ExprValue Alt { get; }

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprFuncIsNull(this);
    }
}