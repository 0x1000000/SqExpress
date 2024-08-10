using System.Collections.Generic;
using SqExpress.Syntax.Type;

namespace SqExpress.DbMetadata;

public interface ITableColumnAppender : ITableColumnFactory, IEnumerable<TableColumn>
{
    ITableColumnAppender AppendColumns(IEnumerable<TableColumn> columns);

    ITableColumnAppender AppendBooleanColumn(string name, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendNullableBooleanColumn(string name, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendByteColumn(string name, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendNullableByteColumn(string name, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendByteArrayColumn(string name, int? size, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendNullableByteArrayColumn(string name, int? size, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendFixedSizeByteArrayColumn(string name, int size, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendNullableFixedSizeByteArrayColumn(string name, int size, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendInt16Column(string name, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendNullableInt16Column(string name, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendInt32Column(string name, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendNullableInt32Column(string name, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendInt64Column(string name, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendNullableInt64Column(string name, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendDecimalColumn(string name, DecimalPrecisionScale? decimalPrecisionScale = null, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendNullableDecimalColumn(string name, DecimalPrecisionScale? decimalPrecisionScale = null, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendDoubleColumn(string name, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendNullableDoubleColumn(string name, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendDateTimeColumn(string name, bool isDate = false, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendNullableDateTimeColumn(string name, bool isDate = false, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendDateTimeOffsetColumn(string name, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendNullableDateTimeOffsetColumn(string name, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendGuidColumn(string name, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendNullableGuidColumn(string name, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendStringColumn(string name, int? size, bool isUnicode = false, bool isText = false, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendNullableStringColumn(string name, int? size, bool isUnicode = false, bool isText = false, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendNullableFixedSizeStringColumn(string name, int size, bool isUnicode = false, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendFixedSizeStringColumn(string name, int size, bool isUnicode = false, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendNullableXmlColumn(string name, ColumnMeta? columnMeta = null);
    ITableColumnAppender AppendXmlColumn(string name, ColumnMeta? columnMeta = null);
}

public interface ITableColumnFactory
{
    BooleanTableColumn CreateBooleanColumn(string name, ColumnMeta? columnMeta = null);
    NullableBooleanTableColumn CreateNullableBooleanColumn(string name, ColumnMeta? columnMeta = null);
    ByteTableColumn CreateByteColumn(string name, ColumnMeta? columnMeta = null);
    NullableByteTableColumn CreateNullableByteColumn(string name, ColumnMeta? columnMeta = null);
    ByteArrayTableColumn CreateByteArrayColumn(string name, int? size, ColumnMeta? columnMeta = null);
    NullableByteArrayTableColumn CreateNullableByteArrayColumn(string name, int? size, ColumnMeta? columnMeta = null);
    ByteArrayTableColumn CreateFixedSizeByteArrayColumn(string name, int size, ColumnMeta? columnMeta = null);
    NullableByteArrayTableColumn CreateNullableFixedSizeByteArrayColumn(string name, int size, ColumnMeta? columnMeta = null);
    Int16TableColumn CreateInt16Column(string name, ColumnMeta? columnMeta = null);
    NullableInt16TableColumn CreateNullableInt16Column(string name, ColumnMeta? columnMeta = null);
    Int32TableColumn CreateInt32Column(string name, ColumnMeta? columnMeta = null);
    NullableInt32TableColumn CreateNullableInt32Column(string name, ColumnMeta? columnMeta = null);
    Int64TableColumn CreateInt64Column(string name, ColumnMeta? columnMeta = null);
    NullableInt64TableColumn CreateNullableInt64Column(string name, ColumnMeta? columnMeta = null);
    DecimalTableColumn CreateDecimalColumn(string name, DecimalPrecisionScale? decimalPrecisionScale = null, ColumnMeta? columnMeta = null);
    NullableDecimalTableColumn CreateNullableDecimalColumn(string name, DecimalPrecisionScale? decimalPrecisionScale = null, ColumnMeta? columnMeta = null);
    DoubleTableColumn CreateDoubleColumn(string name, ColumnMeta? columnMeta = null);
    NullableDoubleTableColumn CreateNullableDoubleColumn(string name, ColumnMeta? columnMeta = null);
    DateTimeTableColumn CreateDateTimeColumn(string name, bool isDate = false, ColumnMeta? columnMeta = null);
    NullableDateTimeTableColumn CreateNullableDateTimeColumn(string name, bool isDate = false, ColumnMeta? columnMeta = null);
    DateTimeOffsetTableColumn CreateDateTimeOffsetColumn(string name, ColumnMeta? columnMeta = null);
    NullableDateTimeOffsetTableColumn CreateNullableDateTimeOffsetColumn(string name, ColumnMeta? columnMeta = null);
    GuidTableColumn CreateGuidColumn(string name, ColumnMeta? columnMeta = null);
    NullableGuidTableColumn CreateNullableGuidColumn(string name, ColumnMeta? columnMeta = null);
    StringTableColumn CreateStringColumn(string name, int? size, bool isUnicode = false, bool isText = false, ColumnMeta? columnMeta = null);
    NullableStringTableColumn CreateNullableStringColumn(string name, int? size, bool isUnicode = false, bool isText = false, ColumnMeta? columnMeta = null);
    NullableStringTableColumn CreateNullableFixedSizeStringColumn(string name, int size, bool isUnicode = false, ColumnMeta? columnMeta = null);
    StringTableColumn CreateFixedSizeStringColumn(string name, int size, bool isUnicode = false, ColumnMeta? columnMeta = null);
    NullableStringTableColumn CreateNullableXmlColumn(string name, ColumnMeta? columnMeta = null);
    StringTableColumn CreateXmlColumn(string name, ColumnMeta? columnMeta = null);
}