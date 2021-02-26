using System;
using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Value;

namespace SqExpress.QueryBuilders.RecordSetter
{
    public delegate IRecordSetterNext DataMapping<in TTable, in TItem>(IDataMapSetter<TTable, TItem> setter);

    public delegate IRecordSetterNext IndexDataMapping(IIndexDataMapSetter setter);

    public delegate IExprAssignRecordSetterNext TargetUpdateMapping<in TTable>(ITargetUpdateSetter<TTable> setter);

    public delegate IExprRecordSetterNext TargetInsertSelectMapping<in TTable>(ITargetInsertSelectSetter<TTable> selectSetter);

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
        TNext Set(NullableGuidCustomColumn column, Guid? value);
        TNext Set(NullableStringCustomColumn column, string? value);


        TNext Set(ExprColumnName column, IReadOnlyList<byte> value);
    }
    public interface IRecordSetterNext : IRecordSetter<IRecordSetterNext> { }

    public interface IExprRecordSetter<out TNext> : IRecordSetter<TNext>
    {
        TNext Set(ExprColumnName column, ExprValue value);
    }
    public interface IExprRecordSetterNext : IExprRecordSetter<IExprRecordSetterNext> { }

    public interface ITargetUpdateSetter<out TTable> : IExprAssignRecordSetter<IExprAssignRecordSetterNext>
    {
        TTable Target { get; }
    }

    public interface ITargetInsertSelectSetter<out TTable> : IExprRecordSetter<IExprRecordSetterNext>
    {
        TTable Target { get; }
    }

    public interface IExprAssignRecordSetter<out TNext> : IExprRecordSetter<IExprAssignRecordSetterNext>
    {
        TNext SetDefault(ExprColumnName column);
    }

    public interface IExprAssignRecordSetterNext : IExprAssignRecordSetter<IExprAssignRecordSetterNext> { }


    //Data Setters

    public interface IIndexDataMapSetter : IRecordSetter<IRecordSetterNext>
    {
        int Index { get; }
    }

    public interface IDataMapSetter<out TTable, out TItem> : IIndexDataMapSetter
    {
        TTable Target { get; }

        TItem Source { get; }
    }
}