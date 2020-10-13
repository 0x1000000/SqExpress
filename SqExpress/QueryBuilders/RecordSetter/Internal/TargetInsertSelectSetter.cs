using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Value;

namespace SqExpress.QueryBuilders.RecordSetter.Internal
{
    internal class TargetInsertSelectSetter<TTable> : RecordSetterBase<IExprRecordSetterNext>, ITargetInsertSelectSetter<TTable>, IExprRecordSetterNext
    {
        public TTable Target { get; }

        private readonly List<ColumnValueInsertSelectMap> _maps = new List<ColumnValueInsertSelectMap>();

        public IReadOnlyList<ColumnValueInsertSelectMap> Maps => this._maps;

        public TargetInsertSelectSetter(TTable target)
        {
            this.Target = target;
        }

        public IExprRecordSetterNext Set(ExprColumnName column, ExprValue value)
        {
            this._maps.Add(new ColumnValueInsertSelectMap(column, value));
            return this;
        }

        protected override IExprRecordSetterNext SetGeneric(ExprColumnName column, ExprLiteral value)
            => this.Set(column, value);
    }
}