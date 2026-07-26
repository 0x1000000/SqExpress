using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.Syntax.Names;

namespace SqExpress.QueryBuilders.Merge
{
    /// <summary>Provides target and generated source-data columns to additional assignments in a mapped merge action.</summary>
    /// <typeparam name="TTable">The concrete target descriptor type exposed by the inherited setter.</typeparam>
    public interface IMergeUpdateSetter<out TTable> : ITargetUpdateSetter<TTable>
    {
        /// <summary>Gets the alias of the generated source-data table.</summary>
        ExprTableAlias SourceDataAlias { get; }
    }
}
