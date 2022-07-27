using System;
using SqExpress.Syntax.Names;

namespace SqExpress.Syntax.Select
{
    public abstract class ExprCte : IExprTableSource
    {
        protected ExprCte(string name, ExprTableAlias? alias)
        {
            this.Alias = alias;
            this.Name = name;
        }

        public ExprTableAlias? Alias { get; }

        public string Name { get; }

        public abstract TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg);

        public abstract IExprSubQuery CreateQuery();

        public TableMultiplication ToTableMultiplication() 
            => new TableMultiplication(new[] { this }, null);
    }

    [SqCustomTraversal]
    public class ExprCteQuery : ExprCte
    {
        public ExprCteQuery(string name, ExprTableAlias? alias, IExprSubQuery query) : base(name, alias)
        {
            this.Query = query;
        }

        //It requires the internal set to create self-reference
        public IExprSubQuery Query { get; internal set; }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprCteQuery(this, arg);

        public override IExprSubQuery CreateQuery()
            => this.Query;
    }
}