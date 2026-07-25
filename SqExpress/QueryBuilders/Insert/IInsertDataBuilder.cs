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

    /// <summary>Requires mapping application data to target insert columns.</summary>
    public interface IInsertDataBuilderMapData<out TTable, out TItem>
    {
        /// <summary>Maps fields from each source item to target columns.</summary>
        IInsertDataBuilderAlsoInsert<TTable> MapData(DataMapping<TTable, TItem> mapping);
    }

    /// <summary>Allows computed insert expressions to be added after source-item mapping.</summary>
    public interface IInsertDataBuilderAlsoInsert<out TTable> : IInsertDataBuilderWhere
    {
        /// <summary>Adds target columns whose expressions do not come directly from source items.</summary>
        IInsertDataBuilderWhere AlsoInsert(TargetInsertSelectMapping<TTable> targetInsertSelectMapping);
    }

    /// <summary>Optionally adds an existence check used to avoid inserting matching rows.</summary>
    public interface IInsertDataBuilderWhere : IInsertDataBuilderMapOutput
    {
        /// <summary>Skips rows whose specified target columns already match an existing row.</summary>
        IInsertDataBuilderMapOutput CheckExistenceBy(ExprColumn column, params ExprColumn[] rest);

        /// <summary>Skips rows whose supplied target-column list already matches an existing row.</summary>
        IInsertDataBuilderMapOutput CheckExistenceBy(IReadOnlyList<ExprColumn> columns);
    }

    /// <summary>Selects identity-insert or output behavior, or completes a normal insert.</summary>
    public interface IInsertDataBuilderMapOutput : IInsertDataBuilderFinal
    {
        /// <summary>Enables explicit insertion into identity columns discovered from target metadata.</summary>
        IIdentityInsertDataBuilderFinal IdentityInsert();

        /// <summary>Adds an output projection containing one or more aliased columns.</summary>
        IInsertDataBuilderFinalOutput Output(ExprAliasedColumnName column, params ExprAliasedColumnName[] rest);

        /// <summary>Adds an output projection from the supplied column list.</summary>
        IInsertDataBuilderFinalOutput Output(IReadOnlyList<ExprAliasedColumnName> columns);
    }

    /// <summary>Represents a mapped identity insert ready to produce an executable expression.</summary>
    public interface IIdentityInsertDataBuilderFinal : IExprExecFinal
    {
        /// <summary>Completes the mapped identity insert.</summary>
        public new ExprIdentityInsert Done();
    }

    /// <summary>Represents a mapped insert ready to produce an executable expression.</summary>
    public interface IInsertDataBuilderFinal : IExprExecFinal
    {
        /// <summary>Completes the mapped insert.</summary>
        public new ExprInsert Done();
    }

    /// <summary>Represents a mapped insert with output ready to produce a query expression.</summary>
    public interface IInsertDataBuilderFinalOutput : IExprQueryFinal
    {
        /// <summary>Completes the row-returning mapped insert.</summary>
        public new ExprInsertOutput Done();
    }
}
