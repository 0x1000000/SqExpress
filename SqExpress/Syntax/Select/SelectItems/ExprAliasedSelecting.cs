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

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprAliasedSelectItem(this);

        string? IExprNamedSelecting.OutputName => this.Alias.Name;
    }
}