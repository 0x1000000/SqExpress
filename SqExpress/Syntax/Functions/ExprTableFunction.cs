using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Functions;

public class ExprTableFunction : IExprTableSource
{
    public ExprTableFunction(ExprDbSchema? schema, ExprFunctionName name, IReadOnlyList<ExprValue>? arguments)
    {
        this.Schema = schema;
        this.Name = name;
        this.Arguments = arguments;
    }

    public ExprDbSchema? Schema { get; }

    public ExprFunctionName Name { get; }

    public IReadOnlyList<ExprValue>? Arguments { get; }

    public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg) 
        => visitor.VisitExprTableFunction(this, arg);

    public TableMultiplication ToTableMultiplication() => new(new[] { this }, null);
}

public class ExprAliasedTableFunction : IExprTableSource
{
    public ExprAliasedTableFunction(ExprTableFunction function, ExprTableAlias alias)
    {
        this.Function = function;
        this.Alias = alias;
    }

    public ExprTableFunction Function { get; }

    public ExprTableAlias Alias { get; }

    public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
        => visitor.VisitExprAliasedTableFunction(this, arg);

    public TableMultiplication ToTableMultiplication() => this.Function.ToTableMultiplication();
}
