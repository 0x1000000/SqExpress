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
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    /// <typeparam name="TItem">The application input-record type.</typeparam>
    public interface IUpdateDataBuilderMapDataInitial<out TTable, out TItem>
    {
        /// <summary>Maps the key fields that identify the target row for each source item.</summary>
        /// <param name="mapping">Assigns target key columns from the current application item.</param>
        /// <returns>The stage that requires non-key update values.</returns>
        IUpdateDataBuilderMapData<TTable, TItem> MapDataKeys(DataMapping<TTable, TItem> mapping);
    }

    /// <summary>Requires the non-key values that are updated from each source item.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    /// <typeparam name="TItem">The application input-record type.</typeparam>
    public interface IUpdateDataBuilderMapData<out TTable, out TItem>
    {
        /// <summary>Maps source item fields to the target columns that will be updated.</summary>
        /// <param name="mapping">Assigns non-key target columns from each source item.</param>
        /// <returns>The stage that can add extra assignments or complete the update.</returns>
        IUpdateDataBuilderAlsoSet<TTable> MapData(DataMapping<TTable, TItem> mapping);
    }

    /// <summary>Allows extra assignments or completion of a mapped update.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    public interface IUpdateDataBuilderAlsoSet<out TTable> : IUpdateDataBuilderFinal
    {
        /// <summary>Adds assignments whose values do not come directly from the mapped source item.</summary>
        /// <param name="mapping">Defines additional expressions or defaults assigned to target columns.</param>
        /// <returns>The completed mapped-update stage.</returns>
        IUpdateDataBuilderFinal AlsoSet(MergeUpdateMapping<TTable> mapping);
    }

    /// <summary>Represents a mapped update ready to produce an executable syntax tree.</summary>
    public interface IUpdateDataBuilderFinal
    {
        /// <summary>Materializes the set-based mapped update as an executable syntax tree without running it.</summary>
        /// <returns>The completed update expression.</returns>
        ExprUpdate Done();
    }
}
