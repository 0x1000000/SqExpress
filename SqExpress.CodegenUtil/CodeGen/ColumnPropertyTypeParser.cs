using System.ComponentModel;
using SqExpress.CodeGenUtil.Model;

namespace SqExpress.CodeGenUtil.CodeGen
{
    internal class ColumnPropertyTypeParser
    {
        public static TRes Parse<TRes, TArg>(string name, IColumnPropertyTypeSwitcher<TRes, TArg> switcher, TArg arg)
        {
            switch (name)
            {
                case nameof(BooleanTableColumn): return switcher.CaseBooleanTableColumn(arg);
                case nameof(BooleanCustomColumn): return switcher.CaseBooleanTableColumn(arg);
                case nameof(NullableBooleanTableColumn): return switcher.CaseNullableBooleanTableColumn(arg);
                case nameof(NullableBooleanCustomColumn): return switcher.CaseNullableBooleanTableColumn(arg);
                case nameof(ByteTableColumn): return switcher.CaseByteTableColumn(arg);
                case nameof(ByteCustomColumn): return switcher.CaseByteTableColumn(arg);
                case nameof(NullableByteTableColumn): return switcher.CaseNullableByteTableColumn(arg);
                case nameof(NullableByteCustomColumn): return switcher.CaseNullableByteTableColumn(arg);
                case nameof(ByteArrayTableColumn): return switcher.CaseByteArrayTableColumn(arg);
                case nameof(ByteArrayCustomColumn): return switcher.CaseByteArrayTableColumn(arg);
                case nameof(NullableByteArrayTableColumn): return switcher.CaseNullableByteArrayTableColumn(arg);
                case nameof(NullableByteArrayCustomColumn): return switcher.CaseNullableByteArrayTableColumn(arg);
                case nameof(Int16TableColumn): return switcher.CaseInt16TableColumn(arg);
                case nameof(Int16CustomColumn): return switcher.CaseInt16TableColumn(arg);
                case nameof(NullableInt16TableColumn): return switcher.CaseNullableInt16TableColumn(arg);
                case nameof(NullableInt16CustomColumn): return switcher.CaseNullableInt16TableColumn(arg);
                case nameof(Int32TableColumn): return switcher.CaseInt32TableColumn(arg);
                case nameof(Int32CustomColumn): return switcher.CaseInt32TableColumn(arg);
                case nameof(NullableInt32TableColumn): return switcher.CaseNullableInt32TableColumn(arg);
                case nameof(NullableInt32CustomColumn): return switcher.CaseNullableInt32TableColumn(arg);
                case nameof(Int64TableColumn): return switcher.CaseInt64TableColumn(arg);
                case nameof(Int64CustomColumn): return switcher.CaseInt64TableColumn(arg);
                case nameof(NullableInt64TableColumn): return switcher.CaseNullableInt64TableColumn(arg);
                case nameof(NullableInt64CustomColumn): return switcher.CaseNullableInt64TableColumn(arg);
                case nameof(DecimalTableColumn): return switcher.CaseDecimalTableColumn(arg);
                case nameof(DecimalCustomColumn): return switcher.CaseDecimalTableColumn(arg);
                case nameof(NullableDecimalTableColumn): return switcher.CaseNullableDecimalTableColumn(arg);
                case nameof(NullableDecimalCustomColumn): return switcher.CaseNullableDecimalTableColumn(arg);
                case nameof(DoubleTableColumn): return switcher.CaseDoubleTableColumn(arg);
                case nameof(DoubleCustomColumn): return switcher.CaseDoubleTableColumn(arg);
                case nameof(NullableDoubleTableColumn): return switcher.CaseNullableDoubleTableColumn(arg);
                case nameof(NullableDoubleCustomColumn): return switcher.CaseNullableDoubleTableColumn(arg);
                case nameof(DateTimeTableColumn): return switcher.CaseDateTimeTableColumn(arg);
                case nameof(DateTimeCustomColumn): return switcher.CaseDateTimeTableColumn(arg);
                case nameof(NullableDateTimeTableColumn): return switcher.CaseNullableDateTimeTableColumn(arg);
                case nameof(NullableDateTimeCustomColumn): return switcher.CaseNullableDateTimeTableColumn(arg);
                case nameof(DateTimeOffsetTableColumn): return switcher.CaseDateTimeOffsetTableColumn(arg);
                case nameof(DateTimeOffsetCustomColumn): return switcher.CaseDateTimeOffsetTableColumn(arg);
                case nameof(NullableDateTimeOffsetTableColumn): return switcher.CaseNullableDateTimeOffsetTableColumn(arg);
                case nameof(NullableDateTimeOffsetCustomColumn): return switcher.CaseNullableDateTimeOffsetTableColumn(arg);
                case nameof(GuidTableColumn): return switcher.CaseGuidTableColumn(arg);
                case nameof(GuidCustomColumn): return switcher.CaseGuidTableColumn(arg);
                case nameof(NullableGuidTableColumn): return switcher.CaseNullableGuidTableColumn(arg);
                case nameof(NullableGuidCustomColumn): return switcher.CaseNullableGuidTableColumn(arg);
                case nameof(StringTableColumn): return switcher.CaseStringTableColumn(arg);
                case nameof(StringCustomColumn): return switcher.CaseStringTableColumn(arg);
                case nameof(NullableStringTableColumn): return switcher.CaseNullableStringTableColumn(arg);
                case nameof(NullableStringCustomColumn): return switcher.CaseNullableStringTableColumn(arg);
            }

            return switcher.Default(name);
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


    public class ModelColumnClrTypeGenerator : IColumnPropertyTypeSwitcher<string, bool>
    {
        public static readonly ModelColumnClrTypeGenerator Instance = new ModelColumnClrTypeGenerator();

        private ModelColumnClrTypeGenerator()
        {
        }

        public string CaseBooleanTableColumn(bool nullRefTypes)
        {
            return "bool";
        }

        public string CaseNullableBooleanTableColumn(bool nullRefTypes)
        {
            return "bool?";
        }

        public string CaseByteTableColumn(bool nullRefTypes)
        {
            return "byte";
        }

        public string CaseNullableByteTableColumn(bool nullRefTypes)
        {
            return "byte?";
        }

        public string CaseByteArrayTableColumn(bool nullRefTypes)
        {
            return "byte[]";
        }

        public string CaseNullableByteArrayTableColumn(bool nullRefTypes)
        {
            return "byte[]" + (nullRefTypes ? "?" : null);
        }

        public string CaseInt16TableColumn(bool nullRefTypes)
        {
            return "short";
        }

        public string CaseNullableInt16TableColumn(bool nullRefTypes)
        {
            return "short?";
        }

        public string CaseInt32TableColumn(bool nullRefTypes)
        {
            return "int";
        }

        public string CaseNullableInt32TableColumn(bool nullRefTypes)
        {
            return "int?";
        }

        public string CaseInt64TableColumn(bool nullRefTypes)
        {
            return "long";
        }

        public string CaseNullableInt64TableColumn(bool nullRefTypes)
        {
            return "long?";
        }

        public string CaseDecimalTableColumn(bool nullRefTypes)
        {
            return "decimal";
        }

        public string CaseNullableDecimalTableColumn(bool nullRefTypes)
        {
            return "decimal?";
        }

        public string CaseDoubleTableColumn(bool nullRefTypes)
        {
            return "double";
        }

        public string CaseNullableDoubleTableColumn(bool nullRefTypes)
        {
            return "double?";
        }

        public string CaseDateTimeTableColumn(bool nullRefTypes)
        {
            return "DateTime";
        }

        public string CaseNullableDateTimeTableColumn(bool nullRefTypes)
        {
            return "DateTime?";
        }

        public string CaseDateTimeOffsetTableColumn(bool arg)
        {
            return "DateTimeOffset";
        }

        public string CaseNullableDateTimeOffsetTableColumn(bool arg)
        {
            return "DateTimeOffset?";
        }

        public string CaseGuidTableColumn(bool nullRefTypes)
        {
            return "Guid";
        }

        public string CaseNullableGuidTableColumn(bool nullRefTypes)
        {
            return "Guid?";
        }

        public string CaseStringTableColumn(bool nullRefTypes)
        {
            return "string";
        }

        public string CaseNullableStringTableColumn(bool nullRefTypes)
        {
            return "string" + (nullRefTypes ? "?" : null);
        }

        public string Default(string name)
        {
            throw new SqExpressCodeGenException($"Unknown column type: \"{name}\"");
        }
    }
}