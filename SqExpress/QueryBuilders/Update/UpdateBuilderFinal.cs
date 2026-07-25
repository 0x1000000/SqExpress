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
        public UpdateBuilderFinal(ExprTable target, List<ExprColumnSetClause> sets, IExprTableSource source)
        {
            this._target = target;
            this._sets = sets;
            this._source = source;
        }

        /// <summary>Adds an inner join to the update source.</summary>
        public UpdateBuilderFinal InnerJoin(IExprTableSource join, ExprBoolean on)
            => new UpdateBuilderFinal(this._target, this._sets,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Inner, join, on));

        /// <summary>Adds a left join to the update source.</summary>
        public UpdateBuilderFinal LeftJoin(IExprTableSource join, ExprBoolean on)
            => new UpdateBuilderFinal(this._target, this._sets,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Left, join, on));

        /// <summary>Adds a full join to the update source.</summary>
        public UpdateBuilderFinal FullJoin(IExprTableSource join, ExprBoolean on)
            => new UpdateBuilderFinal(this._target, this._sets,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Full, join, on));

        /// <summary>Adds a cross join to the update source.</summary>
        public UpdateBuilderFinal CrossJoin(IExprTableSource join)
            => new UpdateBuilderFinal(this._target, this._sets, new ExprCrossedTable(this._source, join));

        /// <summary>Completes an update-from statement without a <c>WHERE</c> predicate.</summary>
        public ExprUpdate All() => new ExprUpdate(this._target, this._sets, this._source, null);

        /// <summary>Completes an update-from statement with the supplied row predicate.</summary>
        /// <remarks>A <see langword="null"/> condition is equivalent to an unfiltered update.</remarks>
        public ExprUpdate Where(ExprBoolean? condition) =>
            new ExprUpdate(this._target, this._sets, this._source, condition);
    }
}
