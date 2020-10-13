using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;

namespace SqExpress.Syntax.Select
{
    public abstract class ExprDerivedTable : IExprTableSource
    {
        protected ExprDerivedTable(ExprTableAlias @alias)
        {
            this.Alias = alias;
        }

        public ExprTableAlias Alias { get; }

        public abstract TRes Accept<TRes>(IExprVisitor<TRes> visitor);

        public (IReadOnlyList<IExprTableSource> Tables, ExprBoolean? On) ToTableMultiplication()
        {
            return (new[] {this}, null);
        }
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

        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitDerivedTableValues(this);
    }

    public class ExprDerivedTableQuery : ExprDerivedTable
    {
        public ExprDerivedTableQuery(IExprSubQuery query, ExprTableAlias alias, IReadOnlyList<ExprColumnName>? columns) : base(alias)
        {
            this.Query = query;
            this.Columns = columns;
        }

        public IExprSubQuery Query { get; }

        public IReadOnlyList<ExprColumnName>? Columns { get; }


        public override TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprDerivedTableQuery(this);
    }
}