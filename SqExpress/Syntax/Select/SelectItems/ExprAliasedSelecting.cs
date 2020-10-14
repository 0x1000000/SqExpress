using SqExpress.Syntax.Names;

namespace SqExpress.Syntax.Select.SelectItems
{
    public class ExprAliasedSelecting : IExprNamedSelecting
    {
        public ExprAliasedSelecting(IExprSelecting value, ExprColumnAlias alias)
        {
            this.Value = value;
            this.Alias = alias;
        }

        public IExprSelecting Value { get; }

        public ExprColumnAlias Alias { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprAliasedSelecting(this, arg);

        string? IExprNamedSelecting.OutputName => this.Alias.Name;
    }
}