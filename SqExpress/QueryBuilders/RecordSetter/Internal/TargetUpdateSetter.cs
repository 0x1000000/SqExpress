using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Value;

namespace SqExpress.QueryBuilders.RecordSetter.Internal
{
    internal class TargetUpdateSetter<TTable> : RecordSetterBase<IExprAssignRecordSetterNext>, ITargetUpdateSetter<TTable>, IExprAssignRecordSetterNext
    {
        public TTable Target { get; }

        private readonly List<ColumnValueUpdateMap> _maps = new List<ColumnValueUpdateMap>();

        public IReadOnlyList<ColumnValueUpdateMap> Maps => this._maps;

        public TargetUpdateSetter(TTable target)
        {
            this.Target = target;
        }

        public IExprAssignRecordSetterNext Set(ExprColumnName column, ExprValue value)
        {
            this._maps.Add(new ColumnValueUpdateMap(column, value));
            return this;
        }

        public IExprAssignRecordSetterNext SetDefault(ExprColumnName column)
        {
            this._maps.Add(new ColumnValueUpdateMap(column, ExprDefault.Instance));
            return this;
        }

        protected override IExprAssignRecordSetterNext SetGeneric(ExprColumn column, ExprLiteral value) 
            => this.Set(column, value);
    }
}