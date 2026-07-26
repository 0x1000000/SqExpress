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
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    /// <typeparam name="TItem">The application input-record type.</typeparam>
    public interface IInsertDataBuilderMapData<out TTable, out TItem>
    {
        /// <summary>Maps fields from each source item to target columns.</summary>
        /// <param name="mapping">Assigns target columns from the current application item and its index.</param>
        /// <returns>The stage that can add computed values, existence checks, output, or completion.</returns>
        IInsertDataBuilderAlsoInsert<TTable> MapData(DataMapping<TTable, TItem> mapping);
    }

    /// <summary>Allows computed insert expressions to be added after source-item mapping.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    public interface IInsertDataBuilderAlsoInsert<out TTable> : IInsertDataBuilderWhere
    {
        /// <summary>Adds target columns whose expressions do not come directly from source items.</summary>
        /// <param name="targetInsertSelectMapping">Defines target columns and expressions evaluated by the generated insert-select.</param>
        /// <returns>The stage that can add an existence check, output, or completion.</returns>
        IInsertDataBuilderWhere AlsoInsert(TargetInsertSelectMapping<TTable> targetInsertSelectMapping);
    }

    /// <summary>Optionally adds an existence check used to avoid inserting matching rows.</summary>
    public interface IInsertDataBuilderWhere : IInsertDataBuilderMapOutput
    {
        /// <summary>Skips rows whose specified target columns already match an existing row.</summary>
        /// <param name="column">The first target column used for the existence match.</param>
        /// <param name="rest">Additional target columns forming the composite match.</param>
        /// <returns>The stage that can configure output or complete the insert.</returns>
        IInsertDataBuilderMapOutput CheckExistenceBy(ExprColumn column, params ExprColumn[] rest);

        /// <summary>Skips rows whose supplied target-column list already matches an existing row.</summary>
        /// <param name="columns">The non-empty set of target columns forming the existence match.</param>
        /// <returns>The stage that can configure output or complete the insert.</returns>
        IInsertDataBuilderMapOutput CheckExistenceBy(IReadOnlyList<ExprColumn> columns);
    }

    /// <summary>Selects identity-insert or output behavior, or completes a normal insert.</summary>
    public interface IInsertDataBuilderMapOutput : IInsertDataBuilderFinal
    {
        /// <summary>Enables explicit insertion into identity columns discovered from target metadata.</summary>
        /// <returns>The identity-insert completion stage.</returns>
        IIdentityInsertDataBuilderFinal IdentityInsert();

        /// <summary>Adds an output projection containing one or more aliased columns.</summary>
        /// <param name="column">The first required affected-row value and output alias.</param>
        /// <param name="rest">Additional output values.</param>
        /// <returns>The row-returning insert completion stage.</returns>
        IInsertDataBuilderFinalOutput Output(ExprAliasedColumnName column, params ExprAliasedColumnName[] rest);

        /// <summary>Adds an output projection from the supplied column list.</summary>
        /// <param name="columns">The non-empty affected-row projection.</param>
        /// <returns>The row-returning insert completion stage.</returns>
        IInsertDataBuilderFinalOutput Output(IReadOnlyList<ExprAliasedColumnName> columns);
    }

    /// <summary>Represents a mapped identity insert ready to produce an executable expression.</summary>
    public interface IIdentityInsertDataBuilderFinal : IExprExecFinal
    {
        /// <summary>Materializes the builder as an identity-insert statement without executing it.</summary>
        /// <returns>The completed identity-insert syntax tree.</returns>
        public new ExprIdentityInsert Done();
    }

    /// <summary>Represents a mapped insert ready to produce an executable expression.</summary>
    public interface IInsertDataBuilderFinal : IExprExecFinal
    {
        /// <summary>Materializes the builder as an insert statement without executing it.</summary>
        /// <returns>The completed insert syntax tree.</returns>
        public new ExprInsert Done();
    }

    /// <summary>Represents a mapped insert with output ready to produce a query expression.</summary>
    public interface IInsertDataBuilderFinalOutput : IExprQueryFinal
    {
        /// <summary>Materializes the builder as a row-returning insert query without executing it.</summary>
        /// <returns>The completed insert-output syntax tree.</returns>
        public new ExprInsertOutput Done();
    }
}
