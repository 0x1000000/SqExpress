using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Update;

namespace SqExpress.QueryBuilders.Delete
{
    /// <summary>Builds joins and a final filter for a delete-from statement.</summary>
    public readonly struct DeleteFromBuilder
    {
        private readonly ExprTable _target;

        private readonly IExprTableSource _source;

        /// <summary>Initializes a delete whose target rows can be selected through a separate joined source.</summary>
        /// <param name="target">The table from which rows are deleted.</param>
        /// <param name="source">The initial source used for joins and filtering.</param>
        public DeleteFromBuilder(ExprTable target, IExprTableSource source)
        {
            this._target = target;
            this._source = source;
        }

        /// <summary>Adds an inner join, retaining only source combinations satisfying its predicate.</summary>
        /// <param name="join">The right-side source.</param><param name="on">The join condition.</param>
        /// <returns>A new joined-delete stage.</returns>
        public DeleteFromBuilder InnerJoin(IExprTableSource join, ExprBoolean on)
            => new DeleteFromBuilder(this._target,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Inner, join, on));

        /// <summary>Adds a left outer join that preserves unmatched rows from the existing source.</summary>
        /// <param name="join">The right-side source.</param><param name="on">The join condition.</param>
        /// <returns>A new joined-delete stage.</returns>
        public DeleteFromBuilder LeftJoin(IExprTableSource join, ExprBoolean on)
            => new DeleteFromBuilder(this._target,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Left, join, on));

        /// <summary>Adds a full outer join that preserves unmatched rows from both sides.</summary>
        /// <param name="join">The right-side source.</param><param name="on">The join condition.</param>
        /// <returns>A new joined-delete stage.</returns>
        public DeleteFromBuilder FullJoin(IExprTableSource join, ExprBoolean on)
            => new DeleteFromBuilder(this._target,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Full, join, on));

        /// <summary>Adds a Cartesian cross join to the delete source.</summary>
        /// <param name="join">The source combined with every existing source row.</param>
        /// <returns>A new joined-delete stage.</returns>
        public DeleteFromBuilder CrossJoin(IExprTableSource join)
            => new DeleteFromBuilder(this._target, new ExprCrossedTable(this._source, join));

        /// <summary>Explicitly completes the joined delete without an additional row filter.</summary>
        /// <returns>The completed delete syntax tree.</returns>
        public ExprDelete All()
        {
            return new ExprDelete(target: this._target, source: this._source, filter: null);
        }

        /// <summary>Completes the joined delete with the supplied row predicate.</summary>
        /// <remarks>A null predicate is equivalent to an unfiltered delete.</remarks>
        /// <param name="filter">The predicate selecting joined target/source rows.</param>
        /// <returns>The completed delete syntax tree.</returns>
        public ExprDelete Where(ExprBoolean? filter)
        {
            return new ExprDelete(target: this._target, source: this._source, filter: filter);
        }
    }
}
