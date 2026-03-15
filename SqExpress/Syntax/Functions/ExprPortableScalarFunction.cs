using System.Collections.Generic;
using EnumVisitorGenerator;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Functions;

public class ExprPortableScalarFunction : ExprValue
{
    public ExprPortableScalarFunction(PortableScalarFunction PortableFunction, IReadOnlyList<ExprValue>? arguments)
    {
        this.PortableFunction = PortableFunction;
        this.Arguments = arguments;
    }

    public PortableScalarFunction PortableFunction { get; }

    public IReadOnlyList<ExprValue>? Arguments { get; }

    public override TRes Accept<TRes, TArg>(IExprValueVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprPortableScalarFunction(this, arg);
}

[VisitorGenerator]
public enum PortableScalarFunction
{
    NullIf,
    Abs,
    Lower,
    Upper,
    Trim,
    LTrim,
    RTrim,
    Replace,
    Substring,
    Round,
    Floor,
    Ceiling,
    Len,
    DataLen,
    Year,
    Month,
    Day,
    Hour,
    Minute,
    Second,
    IndexOf,
    Left,
    Right,
    Repeat
}
