using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Update;

namespace SqExpress.QueryBuilders.Delete
{
    public readonly struct DeleteFromBuilder
    {
        private readonly ExprTable _target;

        private readonly IExprTableSource _source;

        public DeleteFromBuilder(ExprTable target, IExprTableSource source)
        {
            this._target = target;
            this._source = source;
        }

        public DeleteFromBuilder InnerJoin(IExprTableSource join, ExprBoolean on)
            => new DeleteFromBuilder(this._target,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Inner, join, on));

        public DeleteFromBuilder LeftJoin(IExprTableSource join, ExprBoolean on)
            => new DeleteFromBuilder(this._target,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Left, join, on));

        public DeleteFromBuilder FullJoin(IExprTableSource join, ExprBoolean on)
            => new DeleteFromBuilder(this._target,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Full, join, on));

        public DeleteFromBuilder CrossJoin(IExprTableSource join)
            => new DeleteFromBuilder(this._target, new ExprCrossedTable(this._source, join));

        public ExprDelete All()
        {
            return new ExprDelete(target: this._target, source: this._source, filter: null);
        }

        public ExprDelete Where(ExprBoolean filter)
        {
            return new ExprDelete(target: this._target, source: this._source, filter: filter);
        }
    }
}