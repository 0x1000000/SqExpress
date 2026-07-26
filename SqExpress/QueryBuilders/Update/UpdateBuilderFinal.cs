using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Update;

namespace SqExpress.QueryBuilders.Update
{
    /// <summary>Builds joins and a final row filter for an <c>UPDATE ... FROM</c> statement.</summary>
    public readonly struct UpdateBuilderFinal
    {
        private readonly ExprTable _target;

        private readonly List<ExprColumnSetClause> _sets;

        private readonly IExprTableSource _source;

        /// <summary>Initializes an update-from builder from its target, assignments, and source.</summary>
        /// <remarks>Most callers obtain this stage from <see cref="UpdateBuilderSetter.From(IExprTableSource)"/>.</remarks>
        /// <param name="target">The table being updated.</param>
        /// <param name="sets">The assignments accumulated by earlier stages.</param>
        /// <param name="source">The initial source available to assignments and filtering.</param>
        public UpdateBuilderFinal(ExprTable target, List<ExprColumnSetClause> sets, IExprTableSource source)
        {
            this._target = target;
            this._sets = sets;
            this._source = source;
        }

        /// <summary>Adds an inner join, retaining only source combinations satisfying its predicate.</summary>
        /// <param name="join">The right-side table source.</param>
        /// <param name="on">The join condition.</param>
        /// <returns>A new update-from stage with the join appended.</returns>
        public UpdateBuilderFinal InnerJoin(IExprTableSource join, ExprBoolean on)
            => new UpdateBuilderFinal(this._target, this._sets,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Inner, join, on));

        /// <summary>Adds a left outer join that preserves unmatched rows from the existing source.</summary>
        /// <param name="join">The right-side table source.</param>
        /// <param name="on">The join condition.</param>
        /// <returns>A new update-from stage with the join appended.</returns>
        public UpdateBuilderFinal LeftJoin(IExprTableSource join, ExprBoolean on)
            => new UpdateBuilderFinal(this._target, this._sets,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Left, join, on));

        /// <summary>Adds a full outer join that preserves unmatched rows from both source sides.</summary>
        /// <param name="join">The right-side table source.</param>
        /// <param name="on">The join condition.</param>
        /// <returns>A new update-from stage with the join appended.</returns>
        public UpdateBuilderFinal FullJoin(IExprTableSource join, ExprBoolean on)
            => new UpdateBuilderFinal(this._target, this._sets,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Full, join, on));

        /// <summary>Adds a Cartesian cross join to the update source.</summary>
        /// <param name="join">The source combined with every row of the existing source.</param>
        /// <returns>A new update-from stage with the join appended.</returns>
        public UpdateBuilderFinal CrossJoin(IExprTableSource join)
            => new UpdateBuilderFinal(this._target, this._sets, new ExprCrossedTable(this._source, join));

        /// <summary>Explicitly completes an unfiltered update-from statement affecting all joined target rows.</summary>
        /// <returns>The completed update syntax tree.</returns>
        public ExprUpdate All() => new ExprUpdate(this._target, this._sets, this._source, null);

        /// <summary>Completes an update-from statement with the supplied row predicate.</summary>
        /// <remarks>A <see langword="null"/> condition is equivalent to an unfiltered update.</remarks>
        /// <param name="condition">The predicate selecting joined target/source rows.</param>
        /// <returns>The completed update syntax tree.</returns>
        public ExprUpdate Where(ExprBoolean? condition) =>
            new ExprUpdate(this._target, this._sets, this._source, condition);
    }
}
