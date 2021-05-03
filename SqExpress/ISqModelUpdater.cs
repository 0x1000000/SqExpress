using SqExpress.QueryBuilders.RecordSetter;

namespace SqExpress
{
    public interface ISqModelUpdater<in TEntity, in TTable>
    {
        IRecordSetterNext GetMapping(IDataMapSetter<TTable, TEntity> dataMapSetter);
    }

    public interface ISqModelUpdaterKey<in TEntity, in TTable> : ISqModelUpdater<TEntity, TTable>
    {
        IRecordSetterNext GetUpdateKeyMapping(IDataMapSetter<TTable, TEntity> dataMapSetter);

        IRecordSetterNext GetUpdateMapping(IDataMapSetter<TTable, TEntity> dataMapSetter);
    }
}