namespace SqExpress.Meta
{
    public interface ITableColumnVisitor<out TRes>
    {
        TRes VisitBoolean(BooleanTableColumn booleanTableColumn);

        TRes VisitNullableBoolean(NullableBooleanTableColumn nullableBooleanTableColumn);

        TRes VisitByte(ByteTableColumn byteTableColumn);

        TRes VisitNullableByte(NullableByteTableColumn nullableByteTableColumn);

        TRes VisitInt16(Int16TableColumn int16TableColumn);

        TRes VisitNullableInt16(NullableInt16TableColumn nullableInt16TableColumn);

        TRes VisitInt32(Int32TableColumn int32TableColumn);

        TRes VisitNullableInt32(NullableInt32TableColumn nullableInt32TableColumn);

        TRes VisitInt64(Int64TableColumn int64TableColumn);

        TRes VisitNullableInt64(NullableInt64TableColumn nullableInt64TableColumn);

        TRes VisitDecimal(DecimalTableColumn decimalTableColumn);

        TRes VisitNullableDecimal(NullableDecimalTableColumn nullableDecimalTableColumn);

        TRes VisitDouble(DoubleTableColumn doubleTableColumn);

        TRes VisitNullableDouble(NullableDoubleTableColumn nullableDoubleTableColumn);

        TRes VisitDateTime(DateTimeTableColumn dateTimeTableColumn);

        TRes VisitNullableDateTime(NullableDateTimeTableColumn nullableDateTimeTableColumn);

        TRes VisitGuid(GuidTableColumn guidTableColumn);

        TRes VisitNullableGuid(NullableGuidTableColumn nullableGuidTableColumn);

        TRes VisitString(StringTableColumn stringTableColumn);

        TRes VisitNullableString(NullableStringTableColumn nullableStringTableColumn);
    }
}