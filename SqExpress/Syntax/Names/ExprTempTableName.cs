namespace SqExpress.Syntax.Names
{
    public class ExprTempTableName : IExprTableFullName, IExprName
    {
        private string? _lowerInvariantName;

        public ExprTempTableName(string name)
        {
            this.Name = name.Trim();
        }

        public string Name { get; }

        public string LowerInvariantName
        {
            get
            {
                this._lowerInvariantName ??= this.Name.ToLowerInvariant();
                return this._lowerInvariantName;
            }
        }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprTempTableName(this, arg);

        public ExprTableFullName AsExprTableFullName()
        {
            return new ExprTableFullName(null, new ExprTableName(this.Name));
        }

        string? IExprTableFullName.SchemaName => null;

        string? IExprTableFullName.LowerInvariantSchemaName => null;

        string IExprTableFullName.TableName => Name;

        string IExprTableFullName.LowerInvariantTableName => this.LowerInvariantName;
    }
}