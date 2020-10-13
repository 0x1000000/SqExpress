using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.Syntax.Names;

namespace SqExpress.QueryBuilders.Merge
{
    public interface IMergeUpdateSetter<out TTable> : ITargetUpdateSetter<TTable>
    {
        ExprTableAlias SourceDataAlias { get; }
    }
}