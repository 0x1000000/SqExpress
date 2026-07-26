using System;
using System.Collections.Generic;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Update;

namespace SqExpress.QueryBuilders.Merge
{
    /// <summary>Defines the predicate used to match a target row with a generated source-data row.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    /// <param name="target">The target descriptor whose columns can be referenced.</param>
    /// <param name="sourceAlias">The generated value-table source used to reference mapped input fields.</param>
    /// <returns>The additional SQL match condition.</returns>
    public delegate ExprBoolean MergeTargetSourceCondition<in TTable>(TTable target, IExprColumnSource sourceAlias);

    /// <summary>Defines additional assignments for a mapped merge update or insert action.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    /// <param name="setter">The stage used to append target-column assignments.</param>
    /// <returns>The setter's next stage after at least one assignment.</returns>
    public delegate IExprAssignRecordSetterNext MergeUpdateMapping<in TTable>(IMergeUpdateSetter<TTable> setter);

    /// <summary>Defines the columns returned by a mapped merge <c>OUTPUT</c> clause.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    /// <param name="target">The target descriptor used to select affected-row columns.</param>
    /// <param name="sourceAlias">The generated source used to select mapped input values.</param>
    /// <param name="setter">The output projection builder.</param>
    /// <returns>The output builder's next stage after at least one selected value.</returns>
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
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    /// <typeparam name="TItem">The application input-record type.</typeparam>
    public interface IMergeDataBuilderMapDataInitial<out TTable, out TItem>
    {
        /// <summary>Maps the key fields that form the target-to-source match condition.</summary>
        /// <param name="mapping">Assigns target key columns from each source item.</param>
        /// <returns>The stage that accepts non-key mapping or advances to merge actions.</returns>
        IMergeDataBuilderMapData<TTable, TItem> MapDataKeys(DataMapping<TTable, TItem> mapping);
    }

    /// <summary>Allows non-key data mapping or advancement to merge actions.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    /// <typeparam name="TItem">The application input-record type.</typeparam>
    public interface IMergeDataBuilderMapData<out TTable, out TItem> : IMergeDataBuilderAndOn<TTable>
    {
        /// <summary>Maps source item fields to target columns available to the merge actions.</summary>
        /// <param name="mapping">Assigns non-key target columns from each source item.</param>
        /// <returns>The stage that can add index-derived data or advance to merge actions.</returns>
        IMergeDataBuilderMapExtraData<TTable, TItem> MapData(DataMapping<TTable, TItem> mapping);
    }

    /// <summary>Allows index-derived source columns to be added after item data has been mapped.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    /// <typeparam name="TItem">The application input-record type.</typeparam>
    public interface IMergeDataBuilderMapExtraData<out TTable, out TItem> : IMergeDataBuilderAndOn<TTable>
    {
        /// <summary>Adds source columns whose values are derived from the item index.</summary>
        /// <param name="mapping">Maps the zero-based item index to one or more generated source columns.</param>
        /// <returns>The stage that accepts an additional match predicate or merge actions.</returns>
        IMergeDataBuilderAndOn<TTable> MapExtraData(IndexDataMapping mapping);
    }

    /// <summary>Allows an additional predicate to be appended to the key-based match condition.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    public interface IMergeDataBuilderAndOn<out TTable> : IMergeDataBuilderWhenInit<TTable>
    {
        /// <summary>Combines an additional predicate with the match condition created by the key mapping.</summary>
        /// <param name="condition">Builds a target/source predicate combined with the generated key equality using <c>AND</c>.</param>
        /// <returns>The stage offering matched and unmatched actions.</returns>
        IMergeDataBuilderWhenInit<TTable> AndOn(MergeTargetSourceCondition<TTable> condition);
    }

    /// <summary>Offers the three mapped merge action categories.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    public interface IMergeDataBuilderWhenInit<out TTable> : IMergeDataBuilderWhenMatchedInit<TTable>, IMergeDataBuilderNotMatchTargetInit<TTable>, IMergeDataBuilderNotMatchSourceInit<TTable>
    {
    }

    /// <summary>Offers actions for generated source rows that match target rows.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    public interface IMergeDataBuilderWhenMatchedInit<out TTable>
    {
        /// <summary>Updates mapped target columns for matching rows, optionally restricted by a predicate.</summary>
        /// <param name="and">An optional additional target/source condition for this action.</param>
        /// <returns>The stage for extra assignments or later merge actions.</returns>
        IMergeDataBuilderWhenMatchedWithMap<TTable> WhenMatchedThenUpdate(MergeTargetSourceCondition<TTable>? and = null);

        /// <summary>Deletes matching target rows, optionally restricted by a predicate.</summary>
        /// <param name="and">An optional additional target/source condition for this action.</param>
        /// <returns>The stage offering target-missing and source-missing actions.</returns>
        IMergeDataBuilderNotMatchTarget<TTable> WhenMatchedThenDelete(MergeTargetSourceCondition<TTable>? and = null);
    }

    /// <summary>Allows another matched action or advancement to later merge actions.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    public interface IMergeDataBuilderWhenMatched<out TTable> : IMergeDataBuilderWhenMatchedInit<TTable>, IMergeDataBuilderNotMatchTarget<TTable>
    {
    }

    /// <summary>Allows extra assignments after a mapped matched-row update.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    public interface IMergeDataBuilderWhenMatchedWithMap<out TTable> : IMergeDataBuilderNotMatchTarget<TTable>
    {
        /// <summary>Adds assignments that are not supplied by the mapped source data.</summary>
        /// <param name="mapping">Defines additional target assignments, including expressions or defaults.</param>
        /// <returns>The stage offering later merge actions.</returns>
        IMergeDataBuilderNotMatchTarget<TTable> AlsoSet(MergeUpdateMapping<TTable> mapping);
    }

    /// <summary>Offers insert actions for generated source rows absent from the target.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    public interface IMergeDataBuilderNotMatchTargetInit<out TTable>
    {
        /// <summary>Inserts mapped source data, optionally restricted by a predicate.</summary>
        /// <param name="and">An optional condition for source rows absent from the target.</param>
        /// <returns>The stage that can exclude mapped columns or add explicit assignments.</returns>
        IMergeDataBuilderNotMatchTargetExclude<TTable> WhenNotMatchedByTargetThenInsert(MergeTargetSourceCondition<TTable>? and = null);

        /// <summary>Starts an insert containing only explicitly added assignments.</summary>
        /// <param name="and">An optional condition for source rows absent from the target.</param>
        /// <returns>The stage that requires explicit insert assignments.</returns>
        IMergeDataBuilderNotMatchTargetWithMap<TTable> WhenNotMatchedByTargetThenInsertDefaults(MergeTargetSourceCondition<TTable>? and = null);
    }

    /// <summary>Allows another target-missing action or advancement to source-missing actions.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    public interface IMergeDataBuilderNotMatchTarget<out TTable> : IMergeDataBuilderNotMatchTargetInit<TTable>, IMergeDataBuilderNotMatchSource<TTable>
    {
    }

    /// <summary>Allows selected mapped columns to be omitted from an insert.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    public interface IMergeDataBuilderNotMatchTargetExcludeSpecific<out TTable> : IMergeDataBuilderNotMatchTargetWithMap<TTable>
    {
        /// <summary>Excludes one mapped target column from the insert.</summary>
        /// <param name="column">Selects the mapped target column to omit.</param>
        /// <returns>The stage that can exclude more columns or add explicit assignments.</returns>
        IMergeDataBuilderNotMatchTargetWithMap<TTable> Exclude(Func<TTable, ExprColumnName> column);

        /// <summary>Excludes multiple mapped target columns from the insert.</summary>
        /// <param name="columns">Selects mapped target columns to omit.</param>
        /// <returns>The stage that can exclude more columns or add explicit assignments.</returns>
        IMergeDataBuilderNotMatchTargetWithMap<TTable> Exclude(Func<TTable, IReadOnlyList<ExprColumnName>> columns);
    }

    /// <summary>Allows all key columns, or selected columns, to be omitted from an insert.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    public interface IMergeDataBuilderNotMatchTargetExclude<out TTable> : IMergeDataBuilderNotMatchTargetExcludeSpecific<TTable>
    {
        /// <summary>Excludes the columns configured by <c>MapDataKeys</c> from the insert.</summary>
        /// <returns>The stage that can exclude additional columns or add explicit assignments.</returns>
        IMergeDataBuilderNotMatchTargetExcludeSpecific<TTable> ExcludeKeys();
    }

    /// <summary>Allows extra assignments after a mapped insert.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    public interface IMergeDataBuilderNotMatchTargetWithMap<out TTable> : IMergeDataBuilderNotMatchSource<TTable>
    {
        /// <summary>Adds insert assignments that are not supplied by the mapped source data.</summary>
        /// <param name="mapping">Defines explicit target-column values appended to the mapped insert.</param>
        /// <returns>The stage offering source-missing actions or completion.</returns>
        IMergeDataBuilderNotMatchSource<TTable> AlsoInsert(MergeUpdateMapping<TTable> mapping);
    }

    /// <summary>Offers actions for target rows absent from the generated source data.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    public interface IMergeDataBuilderNotMatchSourceInit<out TTable>
    {
        /// <summary>Starts an update of source-missing target rows, optionally restricted by a target predicate.</summary>
        /// <param name="and">An optional target-only condition for this action.</param>
        /// <returns>The stage that requires update assignments.</returns>
        IMergeDataBuilderNotMatchSourceWithMap<TTable> WhenNotMatchedBySourceThenUpdate(Func<TTable, ExprBoolean>? and = null);

        /// <summary>Deletes source-missing target rows, optionally restricted by a target predicate.</summary>
        /// <param name="and">An optional target-only condition for this action.</param>
        /// <returns>The stage offering output or completion.</returns>
        IMergeDataBuilderFinalOutput<TTable> WhenNotMatchedBySourceThenDelete(Func<TTable, ExprBoolean>? and = null);
    }

    /// <summary>Allows a source-missing action or completion of the mapped merge.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    public interface IMergeDataBuilderNotMatchSource<out TTable> : IMergeDataBuilderNotMatchSourceInit<TTable>, IMergeDataBuilderFinalOutput<TTable>
    {
    }

    /// <summary>Requires assignments for an update of source-missing target rows.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    public interface IMergeDataBuilderNotMatchSourceWithMap<out TTable>
    {
        /// <summary>Defines the assignments applied to source-missing target rows.</summary>
        /// <param name="mapping">Builds one or more target-column assignments.</param>
        /// <returns>The stage offering output or completion.</returns>
        IMergeDataBuilderFinalOutput<TTable> Set(TargetUpdateMapping<TTable> mapping);
    }

    /// <summary>Allows completion of the mapped merge or creation of a row-returning output clause.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    public interface IMergeDataBuilderFinalOutput<out TTable> : IMergeDataBuilderFinal
    {
        /// <summary>Defines the columns returned by the merge output clause.</summary>
        /// <param name="mapping">Selects target and generated-source values for each affected row.</param>
        /// <returns>The row-returning merge completion stage.</returns>
        IMergeDataBuilderOutputFinal Output(OutputMapping<TTable> mapping);
    }

    /// <summary>Represents a mapped merge ready to produce an executable syntax tree.</summary>
    public interface IMergeDataBuilderFinal
    {
        /// <summary>Materializes the mapped merge as an executable syntax tree without running it.</summary>
        /// <returns>The completed merge expression.</returns>
        ExprMerge Done();
    }

    /// <summary>Represents a mapped merge output ready to produce a query syntax tree.</summary>
    public interface IMergeDataBuilderOutputFinal
    {
        /// <summary>Materializes the mapped merge and output projection as a query syntax tree.</summary>
        /// <returns>The completed row-returning merge expression.</returns>
        ExprMergeOutput Done();
    }

}
