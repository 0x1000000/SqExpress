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

        /// <summary>Initializes a joined delete from its target and initial source.</summary>
        public DeleteFromBuilder(ExprTable target, IExprTableSource source)
        {
            this._target = target;
            this._source = source;
        }

        /// <summary>Adds an inner join.</summary>
        public DeleteFromBuilder InnerJoin(IExprTableSource join, ExprBoolean on)
            => new DeleteFromBuilder(this._target,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Inner, join, on));

        /// <summary>Adds a left outer join.</summary>
        public DeleteFromBuilder LeftJoin(IExprTableSource join, ExprBoolean on)
            => new DeleteFromBuilder(this._target,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Left, join, on));

        /// <summary>Adds a full outer join.</summary>
        public DeleteFromBuilder FullJoin(IExprTableSource join, ExprBoolean on)
            => new DeleteFromBuilder(this._target,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Full, join, on));

        /// <summary>Adds a cross join.</summary>
        public DeleteFromBuilder CrossJoin(IExprTableSource join)
            => new DeleteFromBuilder(this._target, new ExprCrossedTable(this._source, join));

        /// <summary>Completes the joined delete without a row filter.</summary>
        public ExprDelete All()
        {
            return new ExprDelete(target: this._target, source: this._source, filter: null);
        }

        /// <summary>Completes the joined delete with the supplied row predicate.</summary>
        /// <remarks>A null predicate is equivalent to an unfiltered delete.</remarks>
        public ExprDelete Where(ExprBoolean? filter)
        {
            return new ExprDelete(target: this._target, source: this._source, filter: filter);
        }
    }
}
