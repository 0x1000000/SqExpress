using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Update;

namespace SqExpress.QueryBuilders.Update
{
    public readonly struct UpdateBuilderFinal
    {
        private readonly ExprTable _target;

        private readonly List<ExprColumnSetClause> _sets;

        private readonly IExprTableSource _source;

        public UpdateBuilderFinal(ExprTable target, List<ExprColumnSetClause> sets, IExprTableSource source)
        {
            this._target = target;
            this._sets = sets;
            this._source = source;
        }

        public UpdateBuilderFinal InnerJoin(IExprTableSource join, ExprBoolean on)
            => new UpdateBuilderFinal(this._target, this._sets,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Inner, join, on));

        public UpdateBuilderFinal LeftJoin(IExprTableSource join, ExprBoolean on)
            => new UpdateBuilderFinal(this._target, this._sets,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Left, join, on));

        public UpdateBuilderFinal FullJoin(IExprTableSource join, ExprBoolean on)
            => new UpdateBuilderFinal(this._target, this._sets,
                new ExprJoinedTable(this._source, ExprJoinedTable.ExprJoinType.Full, join, on));

        public UpdateBuilderFinal CrossJoin(IExprTableSource join)
            => new UpdateBuilderFinal(this._target, this._sets, new ExprCrossedTable(this._source, join));

        public ExprUpdate All() => new ExprUpdate(this._target, this._sets, this._source, null);

        public ExprUpdate Where(ExprBoolean condition) =>
            new ExprUpdate(this._target, this._sets, this._source, condition);
    }
}