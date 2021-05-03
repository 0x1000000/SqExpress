using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress.QueryBuilders.RecordSetter.Internal
{
    internal class DataMapSetter<TTable, TItem> : RecordSetterBase<IRecordSetterNext>, IDataMapSetter<TTable, TItem>, IRecordSetterNext
    {
        private readonly List<ExprColumnName> _columns = new List<ExprColumnName>();

        private int? _capacity;

        private List<ExprLiteral>? _record;

        public int Index { get; private set; } = -1;

        public TTable Target { get; }

        public TItem Source { get; private set; }

        public IReadOnlyList<ExprLiteral>? Record => this._record;

        public IReadOnlyList<ExprColumnName> Columns => this._columns;

        public DataMapSetter(TTable target, TItem defaultItem)
        {
            this.Target = target;
            this.Source = defaultItem;
        }

        public void NextItem(TItem item, int? length)
        {
            this.Index++;
            this.Source = item;
            this._capacity = length;
            this._record = length.HasValue ? new List<ExprLiteral>(length.Value) : new List<ExprLiteral>();
        }

        public void EnsureRecordLength()
        {
            if (this._capacity.HasValue)
            {
                if (this._record == null || this._record.Count != this._capacity.Value)
                {
                    throw new SqExpressException($"Number of columns on {this.Index + 1} iteration is less than number of columns on the first one");
                }
            }
        }

        protected override IRecordSetterNext SetGeneric(ExprColumn column, ExprLiteral value)
        {
            var record = this._record.AssertFatalNotNull(nameof(this._record));
            if (this._capacity.HasValue && record.Count == this._capacity)
            {
                throw new SqExpressException($"Number of columns on {this.Index+1} iteration exceeds number of columns on the first one");
            }
            record.Add(value);
            if (!this._capacity.HasValue)
            {
                this._columns.Add(column);
            }
            return this;
        }
    }
}