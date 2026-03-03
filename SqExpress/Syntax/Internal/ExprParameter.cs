using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Internal;

internal class ExprParameter : ExprValue
{
    public ExprParameter(ExprValue? replacedValue, string? tagName)
    {
        this.TagName = tagName;
        this.ReplacedValue = replacedValue;
    }

    public ExprValue? ReplacedValue { get; }

    public string? TagName { get; }

    public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
    {
        if (visitor is not IExprValueVisitorInternal<TRes, TArg> vi)
        {
            throw new SqExpressException($"Only internal visitors can work with \"{nameof(ExprParameter)}\"");
        }

        return vi.VisitExprParameter(this, arg);
    }
}
