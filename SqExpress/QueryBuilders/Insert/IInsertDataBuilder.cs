using System.Collections.Generic;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Update;

namespace SqExpress.QueryBuilders.Insert
{
    internal interface IInsertDataBuilder<out TTable, out TItem> : IInsertDataBuilderMapData<TTable, TItem>, IInsertDataBuilderAlsoInsert<TTable>, IInsertDataBuilderMapOutput, IInsertDataBuilderFinalOutput
    {
    }

    public interface IInsertDataBuilderMapData<out TTable, out TItem>
    {
        IInsertDataBuilderAlsoInsert<TTable> MapData(DataMapping<TTable, TItem> mapping);
    }

    public interface IInsertDataBuilderAlsoInsert<out TTable> : IInsertDataBuilderMapOutput
    {
        IInsertDataBuilderMapOutput AlsoInsert(TargetInsertSelectMapping<TTable> targetInsertSelectMapping);
    }

    public interface IInsertDataBuilderMapOutput : IInsertDataBuilderFinal
    {
        IInsertDataBuilderFinalOutput Output(ExprAliasedColumnName column, params ExprAliasedColumnName[] rest);

        IInsertDataBuilderFinalOutput Output(IReadOnlyList<ExprAliasedColumnName> columns);
    }

    public interface IInsertDataBuilderFinal : IExprExecFinal
    {
        public new ExprInsert Done();
    }

    public interface IInsertDataBuilderFinalOutput : IExprQueryFinal
    {
        public new ExprInsertOutput Done();
    }
}