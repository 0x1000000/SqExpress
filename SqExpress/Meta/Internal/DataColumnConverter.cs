using System;
using System.Collections.Generic;
using System.Data;

namespace SqExpress.Meta.Internal;

internal class DataColumnConverter : ITableColumnVisitor<DataColumn>
{
    public static readonly DataColumnConverter Instance = new();

    private DataColumnConverter()
    {
    }

    private DataColumn Generic(TableColumn column, Type clrType)
    {
        var result = new DataColumn(column.ColumnName.Name, clrType);

        result.AllowDBNull = column.IsNullable;
        if (column.ColumnMeta?.IsIdentity ?? false)
        {
            result.AutoIncrement = true;
        }

        if(column.ColumnMeta?.ColumnDefaultValue is not null)
        {
            var explicitDefaultValue = column.ColumnMeta.ColumnDefaultValue.Accept(ExplicitExprValueExtractor.Instance, null);
            if (explicitDefaultValue != null)
            {
                result.DefaultValue = explicitDefaultValue;
            }
        }

        return result;
    }

    public DataColumn VisitBoolean(BooleanTableColumn booleanTableColumn)
    {
        return this.Generic(booleanTableColumn, typeof(bool));
    }

    public DataColumn VisitNullableBoolean(NullableBooleanTableColumn nullableBooleanTableColumn)
    {
        return this.Generic(nullableBooleanTableColumn, typeof(bool));
    }

    public DataColumn VisitByte(ByteTableColumn byteTableColumn)
    {
        return this.Generic(byteTableColumn, typeof(byte));
    }

    public DataColumn VisitNullableByte(NullableByteTableColumn nullableByteTableColumn)
    {
        return this.Generic(nullableByteTableColumn, typeof(byte));
    }

    public DataColumn VisitByteArray(ByteArrayTableColumn byteTableColumn)
    {
        var result = this.Generic(byteTableColumn, typeof(IReadOnlyList<byte>));
        var maxSize = byteTableColumn.SqlType.GetSize();
        if (maxSize != null)
        {
            result.MaxLength = maxSize.Value;
        }
        return result;
    }

    public DataColumn VisitNullableByteArray(NullableByteArrayTableColumn nullableByteTableColumn)
    {
        var result = this.Generic(nullableByteTableColumn, typeof(IReadOnlyList<byte>));
        var maxSize = nullableByteTableColumn.SqlType.GetSize();
        if (maxSize != null)
        {
            result.MaxLength = maxSize.Value;
        }
        return result;
    }

    public DataColumn VisitInt16(Int16TableColumn int16TableColumn)
    {
        return this.Generic(int16TableColumn, typeof(short));
    }

    public DataColumn VisitNullableInt16(NullableInt16TableColumn nullableInt16TableColumn)
    {
        return this.Generic(nullableInt16TableColumn, typeof(short));
    }

    public DataColumn VisitInt32(Int32TableColumn int32TableColumn)
    {
        return this.Generic(int32TableColumn, typeof(int));
    }

    public DataColumn VisitNullableInt32(NullableInt32TableColumn nullableInt32TableColumn)
    {
        return this.Generic(nullableInt32TableColumn, typeof(int));
    }

    public DataColumn VisitInt64(Int64TableColumn int64TableColumn)
    {
        return this.Generic(int64TableColumn, typeof(long));
    }

    public DataColumn VisitNullableInt64(NullableInt64TableColumn nullableInt64TableColumn)
    {
        return this.Generic(nullableInt64TableColumn, typeof(long));
    }

    public DataColumn VisitDecimal(DecimalTableColumn decimalTableColumn)
    {
        return this.Generic(decimalTableColumn, typeof(decimal));
    }

    public DataColumn VisitNullableDecimal(NullableDecimalTableColumn nullableDecimalTableColumn)
    {
        return this.Generic(nullableDecimalTableColumn, typeof(decimal));
    }

    public DataColumn VisitDouble(DoubleTableColumn doubleTableColumn)
    {
        return this.Generic(doubleTableColumn, typeof(double));
    }

    public DataColumn VisitNullableDouble(NullableDoubleTableColumn nullableDoubleTableColumn)
    {
        return this.Generic(nullableDoubleTableColumn, typeof(double));
    }

    public DataColumn VisitDateTime(DateTimeTableColumn dateTimeTableColumn)
    {
        return this.Generic(dateTimeTableColumn, typeof(DateTime));
    }

    public DataColumn VisitNullableDateTime(NullableDateTimeTableColumn nullableDateTimeTableColumn)
    {
        return this.Generic(nullableDateTimeTableColumn, typeof(DateTime));
    }

    public DataColumn VisitDateTimeOffset(DateTimeOffsetTableColumn dateTimeTableColumn)
    {
        return this.Generic(dateTimeTableColumn, typeof(DateTimeOffset));
    }

    public DataColumn VisitNullableDateTimeOffset(NullableDateTimeOffsetTableColumn nullableDateTimeTableColumn)
    {
        return this.Generic(nullableDateTimeTableColumn, typeof(DateTimeOffset));
    }

    public DataColumn VisitGuid(GuidTableColumn guidTableColumn)
    {
        return this.Generic(guidTableColumn, typeof(Guid));
    }

    public DataColumn VisitNullableGuid(NullableGuidTableColumn nullableGuidTableColumn)
    {
        return this.Generic(nullableGuidTableColumn, typeof(Guid));
    }

    public DataColumn VisitString(StringTableColumn stringTableColumn)
    {
        var result = this.Generic(stringTableColumn, typeof(Guid));
        var resultMaxLength = stringTableColumn.SqlType.GetSize();
        if (resultMaxLength.HasValue)
        {
            result.MaxLength = resultMaxLength.Value;
        }
        return result;
    }

    public DataColumn VisitNullableString(NullableStringTableColumn nullableStringTableColumn)
    {
        var result = this.Generic(nullableStringTableColumn, typeof(Guid));
        var resultMaxLength = nullableStringTableColumn.SqlType.GetSize();
        if (resultMaxLength.HasValue)
        {
            result.MaxLength = resultMaxLength.Value;
        }
        return result;
    }
}
