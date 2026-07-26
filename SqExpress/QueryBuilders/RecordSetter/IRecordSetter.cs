using System;
using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Value;

namespace SqExpress.QueryBuilders.RecordSetter
{
    /// <summary>Maps fields from an application item to generated source-data columns.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    /// <typeparam name="TItem">The application input-record type.</typeparam>
    /// <param name="setter">Provides the current target, source item, index, and typed column assignment methods.</param>
    /// <returns>The next mapping stage after at least one source column has been assigned.</returns>
    public delegate IRecordSetterNext DataMapping<in TTable, in TItem>(IDataMapSetter<TTable, TItem> setter);

    /// <summary>Maps the source-item index to generated source-data columns.</summary>
    /// <param name="setter">Provides the current zero-based source index and typed assignment methods.</param>
    /// <returns>The next mapping stage after at least one source column has been assigned.</returns>
    public delegate IRecordSetterNext IndexDataMapping(IIndexDataMapSetter setter);

    /// <summary>Defines extra assignments applied to a target row.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    /// <param name="setter">Provides the target descriptor and expression/default assignment methods.</param>
    /// <returns>The next mapping stage after at least one assignment has been specified.</returns>
    public delegate IExprAssignRecordSetterNext TargetUpdateMapping<in TTable>(ITargetUpdateSetter<TTable> setter);

    /// <summary>Defines target columns and expressions for an insert-from-select operation.</summary>
    /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
    /// <param name="selectSetter">Provides the target descriptor and expression assignment methods.</param>
    /// <returns>The next mapping stage after at least one projected value has been assigned.</returns>
    public delegate IExprRecordSetterNext TargetInsertSelectMapping<in TTable>(ITargetInsertSelectSetter<TTable> selectSetter);

    /// <summary>Builds a typed record by assigning CLR values to table or custom columns.</summary>
    /// <typeparam name="TNext">The fluent stage returned after an assignment.</typeparam>
    public interface IRecordSetter<out TNext>
    {
        TNext Set(BooleanTableColumn column, bool value);
        TNext Set(ByteTableColumn column, byte value);
        TNext Set(ByteArrayTableColumn column, IReadOnlyList<byte> value);
        TNext Set(Int16TableColumn column, short value);
        TNext Set(Int32TableColumn column, int value);
        TNext Set(Int64TableColumn column, long value);
        TNext Set(DecimalTableColumn column, decimal value);
        TNext Set(DoubleTableColumn column, double value);
        TNext Set(DateTimeTableColumn column, DateTime value);
        TNext Set(DateTimeOffsetTableColumn column, DateTimeOffset value);
        TNext Set(GuidTableColumn column, Guid value);
        TNext Set(StringTableColumn column, string value);

        TNext Set(NullableBooleanTableColumn column, bool? value);
        TNext Set(NullableByteTableColumn column, byte? value);
        TNext Set(NullableByteArrayTableColumn column, IReadOnlyList<byte>? value);
        TNext Set(NullableInt16TableColumn column, short? value);
        TNext Set(NullableInt32TableColumn column, int? value);
        TNext Set(NullableInt64TableColumn column, long? value);
        TNext Set(NullableDecimalTableColumn column, decimal? value);
        TNext Set(NullableDoubleTableColumn column, double? value);
        TNext Set(NullableDateTimeTableColumn column, DateTime? value);
        TNext Set(NullableDateTimeOffsetTableColumn column, DateTimeOffset? value);
        TNext Set(NullableGuidTableColumn column, Guid? value);
        TNext Set(NullableStringTableColumn column, string? value);

        TNext Set(BooleanCustomColumn column, bool value);
        TNext Set(ByteCustomColumn column, byte value);
        TNext Set(ByteArrayCustomColumn column, IReadOnlyList<byte> value);
        TNext Set(Int16CustomColumn column, short value);
        TNext Set(Int32CustomColumn column, int value);
        TNext Set(Int64CustomColumn column, long value);
        TNext Set(DecimalCustomColumn column, decimal value);
        TNext Set(DoubleCustomColumn column, double value);
        TNext Set(DateTimeCustomColumn column, DateTime value);
        TNext Set(DateTimeOffsetCustomColumn column, DateTimeOffset value);
        TNext Set(GuidCustomColumn column, Guid value);
        TNext Set(StringCustomColumn column, string value);

        TNext Set(NullableBooleanCustomColumn column, bool? value);
        TNext Set(NullableByteCustomColumn column, byte? value);
        TNext Set(NullableByteArrayCustomColumn column, IReadOnlyList<byte>? value);
        TNext Set(NullableInt16CustomColumn column, short? value);
        TNext Set(NullableInt32CustomColumn column, int? value);
        TNext Set(NullableInt64CustomColumn column, long? value);
        TNext Set(NullableDecimalCustomColumn column, decimal? value);
        TNext Set(NullableDoubleCustomColumn column, double? value);
        TNext Set(NullableDateTimeCustomColumn column, DateTime? value);
        TNext Set(NullableDateTimeOffsetCustomColumn column, DateTimeOffset? value);
        TNext Set(NullableGuidCustomColumn column, Guid? value);
        TNext Set(NullableStringCustomColumn column, string? value);
    }
    /// <summary>Allows additional typed values to be appended to a record mapping.</summary>
    public interface IRecordSetterNext : IRecordSetter<IRecordSetterNext> { }

    /// <summary>Extends typed record mapping with arbitrary SqExpress value expressions such as columns, functions, and parameters.</summary>
    /// <typeparam name="TNext">The fluent stage returned after an assignment.</typeparam>
    public interface IExprRecordSetter<out TNext> : IRecordSetter<TNext>
    {
        /// <summary>Assigns a SqExpress value expression to a column.</summary>
        /// <param name="column">The target/output column name.</param>
        /// <param name="value">The SQL expression used for the mapped value.</param>
        /// <returns>The next record-mapping stage.</returns>
        TNext Set(ExprColumnName column, ExprValue value);
    }
    /// <summary>Allows additional expression values to be appended to a record mapping.</summary>
    public interface IExprRecordSetterNext : IExprRecordSetter<IExprRecordSetterNext> { }

    /// <summary>Provides the target table and expression/default assignment methods for an update mapping.</summary>
    /// <typeparam name="TTable">The concrete target descriptor type.</typeparam>
    public interface ITargetUpdateSetter<out TTable> : IExprAssignRecordSetter<IExprAssignRecordSetterNext>
    {
        /// <summary>Gets the target table descriptor used by the mapping.</summary>
        TTable Target { get; }
    }

    /// <summary>Provides the target table and expression assignment methods for an insert-from-select mapping.</summary>
    /// <typeparam name="TTable">The concrete target descriptor type.</typeparam>
    public interface ITargetInsertSelectSetter<out TTable> : IExprRecordSetter<IExprRecordSetterNext>
    {
        /// <summary>Gets the target table descriptor used by the mapping.</summary>
        TTable Target { get; }
    }

    /// <summary>Adds SQL <c>DEFAULT</c> assignment support to expression record mapping.</summary>
    /// <typeparam name="TNext">The fluent stage returned after an assignment.</typeparam>
    public interface IExprAssignRecordSetter<out TNext> : IExprRecordSetter<IExprAssignRecordSetterNext>
    {
        /// <summary>Assigns the SQL <c>DEFAULT</c> expression to a column.</summary>
        /// <param name="column">The target column that should use its database default.</param>
        /// <returns>The next record-mapping stage.</returns>
        TNext SetDefault(ExprColumnName column);
    }

    /// <summary>Allows additional expression or default assignments to be appended to a record mapping.</summary>
    public interface IExprAssignRecordSetterNext : IExprAssignRecordSetter<IExprAssignRecordSetterNext> { }


    //Data Setters

    /// <summary>Provides the zero-based item index while mapping generated source data.</summary>
    public interface IIndexDataMapSetter : IRecordSetter<IRecordSetterNext>
    {
        /// <summary>Gets the zero-based index of the current source item.</summary>
        int Index { get; }
    }

    /// <summary>Provides the current source item, target descriptor, index, and typed mapping methods.</summary>
    /// <typeparam name="TTable">The concrete target descriptor type.</typeparam>
    /// <typeparam name="TItem">The application input-record type.</typeparam>
    public interface IDataMapSetter<out TTable, out TItem> : IIndexDataMapSetter
    {
        /// <summary>Gets the target table descriptor used by the mapping.</summary>
        TTable Target { get; }

        /// <summary>Gets the current application data item.</summary>
        TItem Source { get; }
    }
}
