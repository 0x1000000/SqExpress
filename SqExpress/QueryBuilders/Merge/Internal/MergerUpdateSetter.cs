using SqExpress.QueryBuilders.RecordSetter.Internal;
using SqExpress.Syntax.Names;

namespace SqExpress.QueryBuilders.Merge.Internal
{
    internal class MergerUpdateSetter<TTable> : TargetUpdateSetter<TTable>, IMergeUpdateSetter<TTable>
    {
        public MergerUpdateSetter(TTable target, ExprTableAlias sourceDataAlias) : base(target)
        {
            this.SourceDataAlias = sourceDataAlias;
        }

        public ExprTableAlias SourceDataAlias { get; }
    }
}