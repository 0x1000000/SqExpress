using System.Collections.Generic;
using SqExpress.Syntax.Names;

namespace SqExpress.Syntax.Select
{
    public abstract class ExprDerivedTable : IExprTableSource
    {
        protected ExprDerivedTable(ExprTableAlias alias)
        {
            this.Alias = alias;
        }

        public ExprTableAlias Alias { get; }

        ExprTableAlias? IExprTableSource.Alias => this.Alias;

        public abstract TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg);

        public TableMultiplication ToTableMultiplication() 
            => new TableMultiplication(new[] {this}, null);

        public abstract IReadOnlyList<IExprSelecting> ExtractSelecting();

        public abstract IExprSubQuery CreateSubQuery();
    }

    public class ExprDerivedTableValues : ExprDerivedTable
    {
        public ExprDerivedTableValues(ExprTableValueConstructor values, ExprTableAlias alias, IReadOnlyList<ExprColumnName> columns) : base(alias)
        {
            this.Values = values;
            this.Columns = columns;
        }

        public ExprTableValueConstructor Values { get; }

        public IReadOnlyList<ExprColumnName> Columns { get; }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprDerivedTableValues(this, arg);

        public override IReadOnlyList<IExprSelecting> ExtractSelecting()
        {
            return this.Columns;
        }

        public override IExprSubQuery CreateSubQuery()
        {
            return SqQueryBuilder.Select(this.Columns).From(this).Done();
        }
    }

    [SqCustomTraversal]
    public class ExprDerivedTableQuery : ExprDerivedTable
    {
        public ExprDerivedTableQuery(IExprSubQuery query, ExprTableAlias alias, IReadOnlyList<ExprColumnName>? columns) : base(alias)
        {
            this.Query = query;
            this.Columns = columns;
        }

        public IExprSubQuery Query { get; }

        public IReadOnlyList<ExprColumnName>? Columns { get; }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprDerivedTableQuery(this, arg);

        public override IReadOnlyList<IExprSelecting> ExtractSelecting() => this.Columns ?? this.Query.ExtractSelecting();

        public override IExprSubQuery CreateSubQuery() => this.Query;
    }

    internal class DerivedTableQueryWithOriginalRef : ExprDerivedTableQuery
    {
        public DerivedTableBase Original { get; }

        public DerivedTableQueryWithOriginalRef(IExprSubQuery query, ExprTableAlias alias, IReadOnlyList<ExprColumnName>? columns, DerivedTableBase original) : base(query, alias, columns)
        {
            this.Original = original;
        }
    }

}
