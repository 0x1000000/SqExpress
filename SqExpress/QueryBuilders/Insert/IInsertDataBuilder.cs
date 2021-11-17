using System;
using System.Collections.Generic;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Update;

namespace SqExpress.QueryBuilders.Insert
{
    internal interface IInsertDataBuilder<out TTable, out TItem> : IInsertDataBuilderMapData<TTable, TItem>, IInsertDataBuilderAlsoInsert<TTable>,  IInsertDataBuilderWhere, IInsertDataBuilderMapOutput, IInsertDataBuilderFinalOutput, IIdentityInsertDataBuilderFinal
    {
    }

    public interface IInsertDataBuilderMapData<out TTable, out TItem>
    {
        IInsertDataBuilderAlsoInsert<TTable> MapData(DataMapping<TTable, TItem> mapping);
    }

    public interface IInsertDataBuilderAlsoInsert<out TTable> : IInsertDataBuilderWhere
    {
        IInsertDataBuilderWhere AlsoInsert(TargetInsertSelectMapping<TTable> targetInsertSelectMapping);
    }

    public interface IInsertDataBuilderWhere : IInsertDataBuilderMapOutput
    {
        IInsertDataBuilderMapOutput CheckExistenceBy(ExprColumn column, params ExprColumn[] rest);

        IInsertDataBuilderMapOutput CheckExistenceBy(IReadOnlyList<ExprColumn> columns);
    }

    public interface IInsertDataBuilderMapOutput : IInsertDataBuilderFinal
    {
        IIdentityInsertDataBuilderFinal IdentityInsert();

        IInsertDataBuilderFinalOutput Output(ExprAliasedColumnName column, params ExprAliasedColumnName[] rest);

        IInsertDataBuilderFinalOutput Output(IReadOnlyList<ExprAliasedColumnName> columns);
    }

    public interface IIdentityInsertDataBuilderFinal : IExprExecFinal
    {
        public new ExprIdentityInsert Done();
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