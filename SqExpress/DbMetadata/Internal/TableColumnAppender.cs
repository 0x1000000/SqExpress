using System.Collections;
using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;

namespace SqExpress.DbMetadata.Internal;

internal class TableColumnAppender : ITableColumnAppender
{
    private readonly ExprTable _table;

    private readonly ExprTableAlias? _alias;

    private readonly List<TableColumn> _trace = new List<TableColumn>();

    public TableColumnAppender(ExprTable table, ExprTableAlias? alias)
    {
        this._table = table;
        this._alias = alias;
    }

    public BooleanTableColumn CreateBooleanColumn(string name, ColumnMeta? columnMeta = null)
    {
        return new BooleanTableColumn(this._alias, new ExprColumnName(name), this._table, columnMeta);
    }

    public NullableBooleanTableColumn CreateNullableBooleanColumn(string name, ColumnMeta? columnMeta = null)
    {
        return new NullableBooleanTableColumn(this._alias, new ExprColumnName(name), this._table, columnMeta);
    }

    public ByteTableColumn CreateByteColumn(string name, ColumnMeta? columnMeta = null)
    {
        return new ByteTableColumn(this._alias, new ExprColumnName(name), this._table, columnMeta);
    }

    public NullableByteTableColumn CreateNullableByteColumn(string name, ColumnMeta? columnMeta = null)
    {
        return new NullableByteTableColumn(this._alias, new ExprColumnName(name), this._table, columnMeta);
    }

    public ByteArrayTableColumn CreateByteArrayColumn(string name, int? size, ColumnMeta? columnMeta = null)
    {
        return new ByteArrayTableColumn(
            this._alias,
            new ExprColumnName(name),
            this._table,
            new ExprTypeByteArray(size),
            columnMeta
        );
    }

    public NullableByteArrayTableColumn CreateNullableByteArrayColumn(string name, int? size, ColumnMeta? columnMeta = null)
    {
        return new NullableByteArrayTableColumn(
            this._alias,
            new ExprColumnName(name),
            this._table,
            new ExprTypeByteArray(size),
            columnMeta
        );
    }

    public ByteArrayTableColumn CreateFixedSizeByteArrayColumn(string name, int size, ColumnMeta? columnMeta = null)
    {
        return new ByteArrayTableColumn(
            this._alias,
            new ExprColumnName(name),
            this._table,
            new ExprTypeFixSizeByteArray(size),
            columnMeta
        );
    }

    public NullableByteArrayTableColumn CreateNullableFixedSizeByteArrayColumn(
        string name,
        int size,
        ColumnMeta? columnMeta = null)
    {
        return new NullableByteArrayTableColumn(
            this._alias,
            new ExprColumnName(name),
            this._table,
            new ExprTypeFixSizeByteArray(size),
            columnMeta
        );
    }

    public Int16TableColumn CreateInt16Column(string name, ColumnMeta? columnMeta = null)
    {
        return new Int16TableColumn(this._alias, new ExprColumnName(name), this._table, columnMeta);
    }

    public NullableInt16TableColumn CreateNullableInt16Column(string name, ColumnMeta? columnMeta = null)
    {
        return new NullableInt16TableColumn(this._alias, new ExprColumnName(name), this._table, columnMeta);
    }

    public Int32TableColumn CreateInt32Column(string name, ColumnMeta? columnMeta = null)
    {
        return new Int32TableColumn(this._alias, new ExprColumnName(name), this._table, columnMeta);
    }

    public NullableInt32TableColumn CreateNullableInt32Column(string name, ColumnMeta? columnMeta = null)
    {
        return new NullableInt32TableColumn(this._alias, new ExprColumnName(name), this._table, columnMeta);
    }

    public Int64TableColumn CreateInt64Column(string name, ColumnMeta? columnMeta = null)
    {
        return new Int64TableColumn(this._alias, new ExprColumnName(name), this._table, columnMeta);
    }

    public NullableInt64TableColumn CreateNullableInt64Column(string name, ColumnMeta? columnMeta = null)
    {
        return new NullableInt64TableColumn(this._alias, new ExprColumnName(name), this._table, columnMeta);
    }

    public DecimalTableColumn CreateDecimalColumn(
        string name,
        DecimalPrecisionScale? decimalPrecisionScale = null,
        ColumnMeta? columnMeta = null)
    {
        return new DecimalTableColumn(this._alias, new ExprColumnName(name), this._table, decimalPrecisionScale, columnMeta);
    }

    public NullableDecimalTableColumn CreateNullableDecimalColumn(
        string name,
        DecimalPrecisionScale? decimalPrecisionScale = null,
        ColumnMeta? columnMeta = null)
    {
        return new NullableDecimalTableColumn(
            this._alias,
            new ExprColumnName(name),
            this._table,
            decimalPrecisionScale,
            columnMeta
        );
    }

    public DoubleTableColumn CreateDoubleColumn(string name, ColumnMeta? columnMeta = null)
    {
        return new DoubleTableColumn(this._alias, new ExprColumnName(name), this._table, columnMeta);
    }

    public NullableDoubleTableColumn CreateNullableDoubleColumn(string name, ColumnMeta? columnMeta = null)
    {
        return new NullableDoubleTableColumn(this._alias, new ExprColumnName(name), this._table, columnMeta);
    }

    public DateTimeTableColumn CreateDateTimeColumn(string name, bool isDate = false, ColumnMeta? columnMeta = null)
    {
        return new DateTimeTableColumn(this._alias, new ExprColumnName(name), this._table, isDate, columnMeta);
    }

    public NullableDateTimeTableColumn CreateNullableDateTimeColumn(
        string name,
        bool isDate = false,
        ColumnMeta? columnMeta = null)
    {
        return new NullableDateTimeTableColumn(this._alias, new ExprColumnName(name), this._table, isDate, columnMeta);
    }

    public DateTimeOffsetTableColumn CreateDateTimeOffsetColumn(string name, ColumnMeta? columnMeta = null)
    {
        return new DateTimeOffsetTableColumn(this._alias, new ExprColumnName(name), this._table, columnMeta);
    }

    public NullableDateTimeOffsetTableColumn CreateNullableDateTimeOffsetColumn(string name, ColumnMeta? columnMeta = null)
    {
        return new NullableDateTimeOffsetTableColumn(this._alias, new ExprColumnName(name), this._table, columnMeta);
    }

    public GuidTableColumn CreateGuidColumn(string name, ColumnMeta? columnMeta = null)
    {
        return new GuidTableColumn(this._alias, new ExprColumnName(name), this._table, columnMeta);
    }

    public NullableGuidTableColumn CreateNullableGuidColumn(string name, ColumnMeta? columnMeta = null)
    {
        return new NullableGuidTableColumn(this._alias, new ExprColumnName(name), this._table, columnMeta);
    }

    public StringTableColumn CreateStringColumn(
        string name,
        int? size,
        bool isUnicode = false,
        bool isText = false,
        ColumnMeta? columnMeta = null)
    {
        return new StringTableColumn(
            this._alias,
            new ExprColumnName(name),
            this._table,
            new ExprTypeString(size, isUnicode, isText),
            columnMeta
        );
    }

    public NullableStringTableColumn CreateNullableStringColumn(
        string name,
        int? size,
        bool isUnicode = false,
        bool isText = false,
        ColumnMeta? columnMeta = null)
    {
        return new NullableStringTableColumn(
            this._alias,
            new ExprColumnName(name),
            this._table,
            new ExprTypeString(size, isUnicode, isText),
            columnMeta
        );
    }

    public NullableStringTableColumn CreateNullableFixedSizeStringColumn(
        string name,
        int size,
        bool isUnicode = false,
        ColumnMeta? columnMeta = null)
    {
        return new NullableStringTableColumn(
            this._alias,
            new ExprColumnName(name),
            this._table,
            new ExprTypeFixSizeString(size, isUnicode),
            columnMeta
        );
    }

    public StringTableColumn CreateFixedSizeStringColumn(
        string name,
        int size,
        bool isUnicode = false,
        ColumnMeta? columnMeta = null)
    {
        return new StringTableColumn(
            this._alias,
            new ExprColumnName(name),
            this._table,
            new ExprTypeFixSizeString(size, isUnicode),
            columnMeta
        );
    }

    public NullableStringTableColumn CreateNullableXmlColumn(string name, ColumnMeta? columnMeta = null)
    {
        return new NullableStringTableColumn(
            this._alias,
            new ExprColumnName(name),
            this._table,
            ExprTypeXml.Instance,
            columnMeta
        );
    }

    public StringTableColumn CreateXmlColumn(string name, ColumnMeta? columnMeta = null)
    {
        return new StringTableColumn(this._alias, new ExprColumnName(name), this._table, ExprTypeXml.Instance, columnMeta);
    }

    public IEnumerator<TableColumn> GetEnumerator()
    {
        return this._trace.GetEnumerator();
    }

    public ITableColumnAppender AppendColumns(IEnumerable<TableColumn> columns)
    {
        this._trace.AddRange(columns);
        return this;
    }

    private ITableColumnAppender AppendColumn(TableColumn column)
    {
        this._trace.Add(column);
        return this;
    }

    public ITableColumnAppender AppendBooleanColumn(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateBooleanColumn(name, columnMeta));
    }

    public ITableColumnAppender AppendNullableBooleanColumn(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateNullableBooleanColumn(name, columnMeta));
    }

    public ITableColumnAppender AppendByteColumn(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateByteColumn(name, columnMeta));
    }

    public ITableColumnAppender AppendNullableByteColumn(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateNullableByteColumn(name, columnMeta));
    }

    public ITableColumnAppender AppendByteArrayColumn(string name, int? size, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateByteArrayColumn(name, size, columnMeta));
    }

    public ITableColumnAppender AppendNullableByteArrayColumn(string name, int? size, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateNullableByteArrayColumn(name, size, columnMeta));
    }

    public ITableColumnAppender AppendFixedSizeByteArrayColumn(string name, int size, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateFixedSizeByteArrayColumn(name, size, columnMeta));
    }

    public ITableColumnAppender AppendNullableFixedSizeByteArrayColumn(string name, int size, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateNullableFixedSizeByteArrayColumn(name, size, columnMeta));
    }

    public ITableColumnAppender AppendInt16Column(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateInt16Column(name, columnMeta));
    }

    public ITableColumnAppender AppendNullableInt16Column(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateNullableInt16Column(name, columnMeta));
    }

    public ITableColumnAppender AppendInt32Column(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateInt32Column(name, columnMeta));
    }

    public ITableColumnAppender AppendNullableInt32Column(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateNullableInt32Column(name, columnMeta));
    }

    public ITableColumnAppender AppendInt64Column(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateInt64Column(name, columnMeta));
    }

    public ITableColumnAppender AppendNullableInt64Column(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateNullableInt64Column(name, columnMeta));
    }

    public ITableColumnAppender AppendDecimalColumn(
        string name,
        DecimalPrecisionScale? decimalPrecisionScale = null,
        ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateDecimalColumn(name, decimalPrecisionScale, columnMeta));
    }

    public ITableColumnAppender AppendNullableDecimalColumn(
        string name,
        DecimalPrecisionScale? decimalPrecisionScale = null,
        ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateNullableDecimalColumn(name, decimalPrecisionScale, columnMeta));
    }

    public ITableColumnAppender AppendDoubleColumn(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateDoubleColumn(name, columnMeta));
    }

    public ITableColumnAppender AppendNullableDoubleColumn(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateNullableDoubleColumn(name, columnMeta));
    }

    public ITableColumnAppender AppendDateTimeColumn(string name, bool isDate = false, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateDateTimeColumn(name, isDate, columnMeta));
    }

    public ITableColumnAppender AppendNullableDateTimeColumn(string name, bool isDate = false, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateNullableDateTimeColumn(name, isDate, columnMeta));
    }

    public ITableColumnAppender AppendDateTimeOffsetColumn(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateDateTimeOffsetColumn(name, columnMeta));
    }

    public ITableColumnAppender AppendNullableDateTimeOffsetColumn(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateNullableDateTimeOffsetColumn(name, columnMeta));
    }

    public ITableColumnAppender AppendGuidColumn(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateGuidColumn(name, columnMeta));
    }

    public ITableColumnAppender AppendNullableGuidColumn(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateNullableGuidColumn(name, columnMeta));
    }

    public ITableColumnAppender AppendStringColumn(
        string name,
        int? size,
        bool isUnicode = false,
        bool isText = false,
        ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateStringColumn(name, size, isUnicode, isText, columnMeta));
    }

    public ITableColumnAppender AppendNullableStringColumn(
        string name,
        int? size,
        bool isUnicode = false,
        bool isText = false,
        ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateNullableStringColumn(name, size, isUnicode, isText, columnMeta));
    }

    public ITableColumnAppender AppendNullableFixedSizeStringColumn(
        string name,
        int size,
        bool isUnicode = false,
        ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateNullableFixedSizeStringColumn(name, size, isUnicode, columnMeta));
    }

    public ITableColumnAppender AppendFixedSizeStringColumn(
        string name,
        int size,
        bool isUnicode = false,
        ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateFixedSizeStringColumn(name, size, isUnicode, columnMeta));
    }

    public ITableColumnAppender AppendNullableXmlColumn(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateNullableXmlColumn(name, columnMeta));
    }

    public ITableColumnAppender AppendXmlColumn(string name, ColumnMeta? columnMeta = null)
    {
        return this.AppendColumn(this.CreateXmlColumn(name, columnMeta));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
