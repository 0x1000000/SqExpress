using SqExpress.QueryBuilders.Merge;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.Syntax.Update;

namespace SqExpress.QueryBuilders.Update
{
    internal interface IUpdateDataBuilder<out TTable, out TItem> : 
        IUpdateDataBuilderMapDataInitial<TTable, TItem>, 
        IUpdateDataBuilderMapData<TTable, TItem>,
        IUpdateDataBuilderAlsoSet<TTable>,
        IUpdateDataBuilderFinal
    {
        
    }

    /// <summary>Requires the key mapping used to match source data with target rows.</summary>
    public interface IUpdateDataBuilderMapDataInitial<out TTable, out TItem>
    {
        /// <summary>Maps the key fields that identify the target row for each source item.</summary>
        IUpdateDataBuilderMapData<TTable, TItem> MapDataKeys(DataMapping<TTable, TItem> mapping);
    }

    /// <summary>Requires the non-key values that are updated from each source item.</summary>
    public interface IUpdateDataBuilderMapData<out TTable, out TItem>
    {
        /// <summary>Maps source item fields to the target columns that will be updated.</summary>
        IUpdateDataBuilderAlsoSet<TTable> MapData(DataMapping<TTable, TItem> mapping);
    }

    /// <summary>Allows extra assignments or completion of a mapped update.</summary>
    public interface IUpdateDataBuilderAlsoSet<out TTable> : IUpdateDataBuilderFinal
    {
        /// <summary>Adds assignments whose values do not come directly from the mapped source item.</summary>
        IUpdateDataBuilderFinal AlsoSet(MergeUpdateMapping<TTable> mapping);
    }

    /// <summary>Represents a mapped update ready to produce an executable syntax tree.</summary>
    public interface IUpdateDataBuilderFinal
    {
        /// <summary>Completes the builder and returns the update syntax tree.</summary>
        ExprUpdate Done();
    }
}
