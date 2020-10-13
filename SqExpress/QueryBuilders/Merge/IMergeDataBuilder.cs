using System;
using System.Collections.Generic;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Update;

namespace SqExpress.QueryBuilders.Merge
{
    public delegate ExprBoolean MergeTargetSourceCondition<in TTable>(TTable target, IExprColumnSource sourceAlias);

    public delegate IExprAssignRecordSetterNext MergeUpdateMapping<in TTable>(IMergeUpdateSetter<TTable> setter);

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

    public interface IMergeDataBuilderMapDataInitial<out TTable, out TItem>
    {
        IMergeDataBuilderMapData<TTable, TItem> MapDataKeys(DataMapping<TTable, TItem> mapping);
    }

    public interface IMergeDataBuilderMapData<out TTable, out TItem> : IMergeDataBuilderAndOn<TTable>
    {
        IMergeDataBuilderMapExtraData<TTable, TItem> MapData(DataMapping<TTable, TItem> mapping);
    }

    public interface IMergeDataBuilderMapExtraData<out TTable, out TItem> : IMergeDataBuilderAndOn<TTable>
    {
        IMergeDataBuilderAndOn<TTable> MapExtraData(IndexDataMapping mapping);
    }

    public interface IMergeDataBuilderAndOn<out TTable> : IMergeDataBuilderWhenInit<TTable>
    {
        IMergeDataBuilderWhenInit<TTable> AndOn(MergeTargetSourceCondition<TTable> condition);
    }

    public interface IMergeDataBuilderWhenInit<out TTable> : IMergeDataBuilderWhenMatchedInit<TTable>, IMergeDataBuilderNotMatchTargetInit<TTable>, IMergeDataBuilderNotMatchSourceInit<TTable>
    {
    }

    public interface IMergeDataBuilderWhenMatchedInit<out TTable>
    {
        IMergeDataBuilderWhenMatchedWithMap<TTable> WhenMatchedThenUpdate(MergeTargetSourceCondition<TTable>? and = null);

        IMergeDataBuilderNotMatchTarget<TTable> WhenMatchedThenDelete(MergeTargetSourceCondition<TTable>? and = null);
    }

    public interface IMergeDataBuilderWhenMatched<out TTable> : IMergeDataBuilderWhenMatchedInit<TTable>, IMergeDataBuilderNotMatchTarget<TTable>
    {
    }

    public interface IMergeDataBuilderWhenMatchedWithMap<out TTable> : IMergeDataBuilderNotMatchTarget<TTable>
    {
        IMergeDataBuilderNotMatchTarget<TTable> AlsoSet(MergeUpdateMapping<TTable> mapping);
    }

    public interface IMergeDataBuilderNotMatchTargetInit<out TTable>
    {
        IMergeDataBuilderNotMatchTargetExclude<TTable> WhenNotMatchedByTargetThenInsert(MergeTargetSourceCondition<TTable>? and = null);

        IMergeDataBuilderNotMatchTargetWithMap<TTable> WhenNotMatchedByTargetThenInsertDefaults(MergeTargetSourceCondition<TTable>? and = null);
    }

    public interface IMergeDataBuilderNotMatchTarget<out TTable> : IMergeDataBuilderNotMatchTargetInit<TTable>, IMergeDataBuilderNotMatchSource<TTable>
    {
    }

    public interface IMergeDataBuilderNotMatchTargetExcludeSpecific<out TTable> : IMergeDataBuilderNotMatchTargetWithMap<TTable>
    {
        IMergeDataBuilderNotMatchTargetWithMap<TTable> Exclude(Func<TTable, ExprColumnName> column);

        IMergeDataBuilderNotMatchTargetWithMap<TTable> Exclude(Func<TTable, IReadOnlyList<ExprColumnName>> columns);
    }

    public interface IMergeDataBuilderNotMatchTargetExclude<out TTable> : IMergeDataBuilderNotMatchTargetExcludeSpecific<TTable>
    {
        IMergeDataBuilderNotMatchTargetExcludeSpecific<TTable> ExcludeKeys();
    }

    public interface IMergeDataBuilderNotMatchTargetWithMap<out TTable> : IMergeDataBuilderNotMatchSource<TTable>
    {
        IMergeDataBuilderNotMatchSource<TTable> AlsoInsert(MergeUpdateMapping<TTable> mapping);
    }

    public interface IMergeDataBuilderNotMatchSourceInit<out TTable>
    {
        IMergeDataBuilderNotMatchSourceWithMap<TTable> WhenNotMatchedBySourceThenUpdate(Func<TTable, ExprBoolean>? and = null);

        IMergeDataBuilderFinalOutput<TTable> WhenNotMatchedBySourceThenDelete(Func<TTable, ExprBoolean>? and = null);
    }

    public interface IMergeDataBuilderNotMatchSource<out TTable> : IMergeDataBuilderNotMatchSourceInit<TTable>, IMergeDataBuilderFinalOutput<TTable>
    {
    }

    public interface IMergeDataBuilderNotMatchSourceWithMap<out TTable>
    {
        IMergeDataBuilderFinalOutput<TTable> Set(TargetUpdateMapping<TTable> mapping);
    }

    public interface IMergeDataBuilderFinalOutput<out TTable> : IMergeDataBuilderFinal
    {
        IMergeDataBuilderOutputFinal Output(OutputMapping<TTable> mapping);
    }

    public interface IMergeDataBuilderFinal
    {
        ExprMerge Done();
    }

    public interface IMergeDataBuilderOutputFinal
    {
        ExprMergeOutput Done();
    }

}