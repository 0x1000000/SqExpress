using System;
using System.Collections.Generic;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Update;

namespace SqExpress.QueryBuilders.Merge
{
    /// <summary>Defines the predicate used to match a target row with a generated source-data row.</summary>
    public delegate ExprBoolean MergeTargetSourceCondition<in TTable>(TTable target, IExprColumnSource sourceAlias);

    /// <summary>Defines additional assignments for a mapped merge update or insert action.</summary>
    public delegate IExprAssignRecordSetterNext MergeUpdateMapping<in TTable>(IMergeUpdateSetter<TTable> setter);

    /// <summary>Defines the columns returned by a mapped merge <c>OUTPUT</c> clause.</summary>
    public delegate IOutputSetterNext OutputMapping<in TTable>(TTable target, IExprColumnSource sourceAlias, IOutputSetter<IOutputSetterNext> setter);

    internal interface IMergeDataBuilder<out TTable, out TItem> :
        IMergeDataBuilderMapDataInitial<TTable, TItem>,
        IMergeDataBuilderMapData<TTable, TItem>,
        IMergeDataBuilderMapExtraData<TTable, TItem>,
        IMergeDataBuilderAndOn<TTable>,
        IMergeDataBuilderWhenMatchedInit<TTable>,
        IMergeDataBuilderWhenMatched<TTable>,
        IMergeDataBuilderWhenMatchedWithMap<TTable>,
        IMergeDataBuilderNotMatchTargetInit<TTable>,
        IMergeDataBuilderNotMatchTarget<TTable>,
        IMergeDataBuilderNotMatchTargetExcludeSpecific<TTable>,
        IMergeDataBuilderNotMatchTargetExclude<TTable>,
        IMergeDataBuilderNotMatchTargetWithMap<TTable>,
        IMergeDataBuilderNotMatchSourceInit<TTable>,
        IMergeDataBuilderNotMatchSource<TTable>,
        IMergeDataBuilderNotMatchSourceWithMap<TTable>,
        IMergeDataBuilderFinalOutput<TTable>,
        IMergeDataBuilderFinal,
        IMergeDataBuilderOutputFinal
    {
    }

    /// <summary>Requires the key mapping used to match source data with target rows.</summary>
    public interface IMergeDataBuilderMapDataInitial<out TTable, out TItem>
    {
        /// <summary>Maps the key fields that form the target-to-source match condition.</summary>
        IMergeDataBuilderMapData<TTable, TItem> MapDataKeys(DataMapping<TTable, TItem> mapping);
    }

    /// <summary>Allows non-key data mapping or advancement to merge actions.</summary>
    public interface IMergeDataBuilderMapData<out TTable, out TItem> : IMergeDataBuilderAndOn<TTable>
    {
        /// <summary>Maps source item fields to target columns available to the merge actions.</summary>
        IMergeDataBuilderMapExtraData<TTable, TItem> MapData(DataMapping<TTable, TItem> mapping);
    }

    /// <summary>Allows index-derived source columns to be added after item data has been mapped.</summary>
    public interface IMergeDataBuilderMapExtraData<out TTable, out TItem> : IMergeDataBuilderAndOn<TTable>
    {
        /// <summary>Adds source columns whose values are derived from the item index.</summary>
        IMergeDataBuilderAndOn<TTable> MapExtraData(IndexDataMapping mapping);
    }

    /// <summary>Allows an additional predicate to be appended to the key-based match condition.</summary>
    public interface IMergeDataBuilderAndOn<out TTable> : IMergeDataBuilderWhenInit<TTable>
    {
        /// <summary>Combines an additional predicate with the match condition created by the key mapping.</summary>
        IMergeDataBuilderWhenInit<TTable> AndOn(MergeTargetSourceCondition<TTable> condition);
    }

    /// <summary>Offers the three mapped merge action categories.</summary>
    public interface IMergeDataBuilderWhenInit<out TTable> : IMergeDataBuilderWhenMatchedInit<TTable>, IMergeDataBuilderNotMatchTargetInit<TTable>, IMergeDataBuilderNotMatchSourceInit<TTable>
    {
    }

    /// <summary>Offers actions for generated source rows that match target rows.</summary>
    public interface IMergeDataBuilderWhenMatchedInit<out TTable>
    {
        /// <summary>Updates mapped target columns for matching rows, optionally restricted by a predicate.</summary>
        IMergeDataBuilderWhenMatchedWithMap<TTable> WhenMatchedThenUpdate(MergeTargetSourceCondition<TTable>? and = null);

        /// <summary>Deletes matching target rows, optionally restricted by a predicate.</summary>
        IMergeDataBuilderNotMatchTarget<TTable> WhenMatchedThenDelete(MergeTargetSourceCondition<TTable>? and = null);
    }

    /// <summary>Allows another matched action or advancement to later merge actions.</summary>
    public interface IMergeDataBuilderWhenMatched<out TTable> : IMergeDataBuilderWhenMatchedInit<TTable>, IMergeDataBuilderNotMatchTarget<TTable>
    {
    }

    /// <summary>Allows extra assignments after a mapped matched-row update.</summary>
    public interface IMergeDataBuilderWhenMatchedWithMap<out TTable> : IMergeDataBuilderNotMatchTarget<TTable>
    {
        /// <summary>Adds assignments that are not supplied by the mapped source data.</summary>
        IMergeDataBuilderNotMatchTarget<TTable> AlsoSet(MergeUpdateMapping<TTable> mapping);
    }

    /// <summary>Offers insert actions for generated source rows absent from the target.</summary>
    public interface IMergeDataBuilderNotMatchTargetInit<out TTable>
    {
        /// <summary>Inserts mapped source data, optionally restricted by a predicate.</summary>
        IMergeDataBuilderNotMatchTargetExclude<TTable> WhenNotMatchedByTargetThenInsert(MergeTargetSourceCondition<TTable>? and = null);

        /// <summary>Starts an insert containing only explicitly added assignments.</summary>
        IMergeDataBuilderNotMatchTargetWithMap<TTable> WhenNotMatchedByTargetThenInsertDefaults(MergeTargetSourceCondition<TTable>? and = null);
    }

    /// <summary>Allows another target-missing action or advancement to source-missing actions.</summary>
    public interface IMergeDataBuilderNotMatchTarget<out TTable> : IMergeDataBuilderNotMatchTargetInit<TTable>, IMergeDataBuilderNotMatchSource<TTable>
    {
    }

    /// <summary>Allows selected mapped columns to be omitted from an insert.</summary>
    public interface IMergeDataBuilderNotMatchTargetExcludeSpecific<out TTable> : IMergeDataBuilderNotMatchTargetWithMap<TTable>
    {
        /// <summary>Excludes one mapped target column from the insert.</summary>
        IMergeDataBuilderNotMatchTargetWithMap<TTable> Exclude(Func<TTable, ExprColumnName> column);

        /// <summary>Excludes multiple mapped target columns from the insert.</summary>
        IMergeDataBuilderNotMatchTargetWithMap<TTable> Exclude(Func<TTable, IReadOnlyList<ExprColumnName>> columns);
    }

    /// <summary>Allows all key columns, or selected columns, to be omitted from an insert.</summary>
    public interface IMergeDataBuilderNotMatchTargetExclude<out TTable> : IMergeDataBuilderNotMatchTargetExcludeSpecific<TTable>
    {
        /// <summary>Excludes the columns configured by <c>MapDataKeys</c> from the insert.</summary>
        IMergeDataBuilderNotMatchTargetExcludeSpecific<TTable> ExcludeKeys();
    }

    /// <summary>Allows extra assignments after a mapped insert.</summary>
    public interface IMergeDataBuilderNotMatchTargetWithMap<out TTable> : IMergeDataBuilderNotMatchSource<TTable>
    {
        /// <summary>Adds insert assignments that are not supplied by the mapped source data.</summary>
        IMergeDataBuilderNotMatchSource<TTable> AlsoInsert(MergeUpdateMapping<TTable> mapping);
    }

    /// <summary>Offers actions for target rows absent from the generated source data.</summary>
    public interface IMergeDataBuilderNotMatchSourceInit<out TTable>
    {
        /// <summary>Starts an update of source-missing target rows, optionally restricted by a target predicate.</summary>
        IMergeDataBuilderNotMatchSourceWithMap<TTable> WhenNotMatchedBySourceThenUpdate(Func<TTable, ExprBoolean>? and = null);

        /// <summary>Deletes source-missing target rows, optionally restricted by a target predicate.</summary>
        IMergeDataBuilderFinalOutput<TTable> WhenNotMatchedBySourceThenDelete(Func<TTable, ExprBoolean>? and = null);
    }

    /// <summary>Allows a source-missing action or completion of the mapped merge.</summary>
    public interface IMergeDataBuilderNotMatchSource<out TTable> : IMergeDataBuilderNotMatchSourceInit<TTable>, IMergeDataBuilderFinalOutput<TTable>
    {
    }

    /// <summary>Requires assignments for an update of source-missing target rows.</summary>
    public interface IMergeDataBuilderNotMatchSourceWithMap<out TTable>
    {
        /// <summary>Defines the assignments applied to source-missing target rows.</summary>
        IMergeDataBuilderFinalOutput<TTable> Set(TargetUpdateMapping<TTable> mapping);
    }

    /// <summary>Allows completion of the mapped merge or creation of a row-returning output clause.</summary>
    public interface IMergeDataBuilderFinalOutput<out TTable> : IMergeDataBuilderFinal
    {
        /// <summary>Defines the columns returned by the merge output clause.</summary>
        IMergeDataBuilderOutputFinal Output(OutputMapping<TTable> mapping);
    }

    /// <summary>Represents a mapped merge ready to produce an executable syntax tree.</summary>
    public interface IMergeDataBuilderFinal
    {
        /// <summary>Completes the builder and returns the merge syntax tree.</summary>
        ExprMerge Done();
    }

    /// <summary>Represents a mapped merge output ready to produce a query syntax tree.</summary>
    public interface IMergeDataBuilderOutputFinal
    {
        /// <summary>Completes the builder and returns the row-returning merge syntax tree.</summary>
        ExprMergeOutput Done();
    }

}
