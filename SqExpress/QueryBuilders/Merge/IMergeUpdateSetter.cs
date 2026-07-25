using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.Syntax.Names;

namespace SqExpress.QueryBuilders.Merge
{
    /// <summary>Provides target and generated source-data columns to a mapped merge assignment.</summary>
    public interface IMergeUpdateSetter<out TTable> : ITargetUpdateSetter<TTable>
    {
        /// <summary>Gets the alias of the generated source-data table.</summary>
        ExprTableAlias SourceDataAlias { get; }
    }
}
