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

    public interface IUpdateDataBuilderMapDataInitial<out TTable, out TItem>
    {
        IUpdateDataBuilderMapData<TTable, TItem> MapDataKeys(DataMapping<TTable, TItem> mapping);
    }

    public interface IUpdateDataBuilderMapData<out TTable, out TItem>
    {
        IUpdateDataBuilderAlsoSet<TTable> MapData(DataMapping<TTable, TItem> mapping);
    }

    public interface IUpdateDataBuilderAlsoSet<out TTable> : IUpdateDataBuilderFinal
    {
        IUpdateDataBuilderFinal AlsoSet(MergeUpdateMapping<TTable> mapping);
    }

    public interface IUpdateDataBuilderFinal
    {
        ExprUpdate Done();
    }
}