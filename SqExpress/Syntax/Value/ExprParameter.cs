namespace SqExpress.Syntax.Value;

public class ExprParameter : ExprValue
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
        return visitor.VisitExprParameter(this, arg);
    }
}
