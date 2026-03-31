namespace SqExpress.CodeGen.Shared
{
    internal static class ColumnPropertyTypeParser
    {
        public static TRes Parse<TRes, TArg>(string name, IColumnPropertyTypeSwitcher<TRes, TArg> switcher, TArg arg)
        {
            switch (name)
            {
                case nameof(BooleanTableColumn):
                case nameof(BooleanCustomColumn):
                    return switcher.CaseBooleanTableColumn(arg);
                case nameof(NullableBooleanTableColumn):
                case nameof(NullableBooleanCustomColumn):
                    return switcher.CaseNullableBooleanTableColumn(arg);
                case nameof(ByteTableColumn):
                case nameof(ByteCustomColumn):
                    return switcher.CaseByteTableColumn(arg);
                case nameof(NullableByteTableColumn):
                case nameof(NullableByteCustomColumn):
                    return switcher.CaseNullableByteTableColumn(arg);
                case nameof(ByteArrayTableColumn):
                case nameof(ByteArrayCustomColumn):
                    return switcher.CaseByteArrayTableColumn(arg);
                case nameof(NullableByteArrayTableColumn):
                case nameof(NullableByteArrayCustomColumn):
                    return switcher.CaseNullableByteArrayTableColumn(arg);
                case nameof(Int16TableColumn):
                case nameof(Int16CustomColumn):
                    return switcher.CaseInt16TableColumn(arg);
                case nameof(NullableInt16TableColumn):
                case nameof(NullableInt16CustomColumn):
                    return switcher.CaseNullableInt16TableColumn(arg);
                case nameof(Int32TableColumn):
                case nameof(Int32CustomColumn):
                    return switcher.CaseInt32TableColumn(arg);
                case nameof(NullableInt32TableColumn):
                case nameof(NullableInt32CustomColumn):
                    return switcher.CaseNullableInt32TableColumn(arg);
                case nameof(Int64TableColumn):
                case nameof(Int64CustomColumn):
                    return switcher.CaseInt64TableColumn(arg);
                case nameof(NullableInt64TableColumn):
                case nameof(NullableInt64CustomColumn):
                    return switcher.CaseNullableInt64TableColumn(arg);
                case nameof(DecimalTableColumn):
                case nameof(DecimalCustomColumn):
                    return switcher.CaseDecimalTableColumn(arg);
                case nameof(NullableDecimalTableColumn):
                case nameof(NullableDecimalCustomColumn):
                    return switcher.CaseNullableDecimalTableColumn(arg);
                case nameof(DoubleTableColumn):
                case nameof(DoubleCustomColumn):
                    return switcher.CaseDoubleTableColumn(arg);
                case nameof(NullableDoubleTableColumn):
                case nameof(NullableDoubleCustomColumn):
                    return switcher.CaseNullableDoubleTableColumn(arg);
                case nameof(DateTimeTableColumn):
                case nameof(DateTimeCustomColumn):
                    return switcher.CaseDateTimeTableColumn(arg);
                case nameof(NullableDateTimeTableColumn):
                case nameof(NullableDateTimeCustomColumn):
                    return switcher.CaseNullableDateTimeTableColumn(arg);
                case nameof(DateTimeOffsetTableColumn):
                case nameof(DateTimeOffsetCustomColumn):
                    return switcher.CaseDateTimeOffsetTableColumn(arg);
                case nameof(NullableDateTimeOffsetTableColumn):
                case nameof(NullableDateTimeOffsetCustomColumn):
                    return switcher.CaseNullableDateTimeOffsetTableColumn(arg);
                case nameof(GuidTableColumn):
                case nameof(GuidCustomColumn):
                    return switcher.CaseGuidTableColumn(arg);
                case nameof(NullableGuidTableColumn):
                case nameof(NullableGuidCustomColumn):
                    return switcher.CaseNullableGuidTableColumn(arg);
                case nameof(StringTableColumn):
                case nameof(StringCustomColumn):
                    return switcher.CaseStringTableColumn(arg);
                case nameof(NullableStringTableColumn):
                case nameof(NullableStringCustomColumn):
                    return switcher.CaseNullableStringTableColumn(arg);
                default:
                    return switcher.Default(name);
            }
        }
    }

    internal interface IColumnPropertyTypeSwitcher<out TRes, in TArg>
    {
        TRes CaseBooleanTableColumn(TArg arg);
        TRes CaseNullableBooleanTableColumn(TArg arg);
        TRes CaseByteTableColumn(TArg arg);
        TRes CaseNullableByteTableColumn(TArg arg);
        TRes CaseByteArrayTableColumn(TArg arg);
        TRes CaseNullableByteArrayTableColumn(TArg arg);
        TRes CaseInt16TableColumn(TArg arg);
        TRes CaseNullableInt16TableColumn(TArg arg);
        TRes CaseInt32TableColumn(TArg arg);
        TRes CaseNullableInt32TableColumn(TArg arg);
        TRes CaseInt64TableColumn(TArg arg);
        TRes CaseNullableInt64TableColumn(TArg arg);
        TRes CaseDecimalTableColumn(TArg arg);
        TRes CaseNullableDecimalTableColumn(TArg arg);
        TRes CaseDoubleTableColumn(TArg arg);
        TRes CaseNullableDoubleTableColumn(TArg arg);
        TRes CaseDateTimeTableColumn(TArg arg);
        TRes CaseNullableDateTimeTableColumn(TArg arg);
        TRes CaseDateTimeOffsetTableColumn(TArg arg);
        TRes CaseNullableDateTimeOffsetTableColumn(TArg arg);
        TRes CaseGuidTableColumn(TArg arg);
        TRes CaseNullableGuidTableColumn(TArg arg);
        TRes CaseStringTableColumn(TArg arg);
        TRes CaseNullableStringTableColumn(TArg arg);
        TRes Default(string name);
    }
}
