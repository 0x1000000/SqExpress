namespace SqExpress.Syntax.Names
{
    public class ExprColumnAlias : IExpr
    {
        public ExprColumnAlias(string name)
        {
            this.Name = name.Trim();
            this.LowerInvariantName = this.Name.ToLowerInvariant();
        }

        public string Name { get; }

        public string LowerInvariantName { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprColumnAlias(this, arg);

        public static implicit operator ExprColumnAlias(string name)=> new ExprColumnAlias(name);

        public static implicit operator ExprColumnAlias(ExprColumn column) => new ExprColumnAlias(column.ColumnName.Name);

        public static implicit operator ExprColumnAlias(ExprColumnName column) => new ExprColumnAlias(column.Name);
    }
}