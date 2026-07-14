using System;
using System.Collections.Generic;
using System.Linq;
using SqExpress.DbMetadata.Internal;
using SqExpress.DbMetadata.Internal.Model;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress.DbMetadata;

public sealed class SqTable : TableBase
{
    public static SqTable Create(
        string database,
        string schema,
        string name,
        Func<ITableColumnAppender, IEnumerable<TableColumn>> columnsBuilder,
        Func<ITableIndexAppender, IEnumerable<IndexMeta>>? indexBuilder = null,
        Alias alias = default)
    {
        var result = new SqTable(database, schema, name, alias);
        AddColumnsAndIndexes(result, columnsBuilder, indexBuilder);
        return result;
    }

    public static SqTable Create(
        string? schema,
        string name,
        Func<ITableColumnAppender, IEnumerable<TableColumn>> columnsBuilder,
        Func<ITableIndexAppender, IEnumerable<IndexMeta>>? indexBuilder = null,
        Alias alias = default)
    {
        var result = new SqTable(schema, name, alias);
        AddColumnsAndIndexes(result, columnsBuilder, indexBuilder);
        return result;
    }

    internal SqTable(string database, string schema, string name, Alias alias = default) : base(database, schema, name, alias) { }

    internal SqTable(string? schema, string name, Alias alias = default) : base(schema, name, alias) { }

    private SqTable(IExprTableFullName fullName, ExprTableAlias? tableAlias) : base(fullName, tableAlias) { }

    public static SqTable Create(TableBase table)
    {
        if (table == null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        return Clone(table, table.Alias);
    }

    internal static SqTable Clone(TableBase table, ExprTableAlias? alias)
    {
        var result = new SqTable(table.FullName, alias);
        var columns = table.Columns
            .Select(column => column.WithTable(result).WithSource(result.Alias))
            .ToArray();
        result.AddColumns(columns);

        var columnsByName = columns.ToDictionary(
            column => column.ColumnName.LowerInvariantName,
            StringComparer.Ordinal);
        result.AddIndexes(table.Indexes.Select(index => new IndexMeta(
            index.Columns.Select(column => new IndexMetaColumn(
                columnsByName[column.Column.ColumnName.LowerInvariantName],
                column.Descending)).ToArray(),
            index.Name,
            index.Unique,
            index.Clustered)));
        return result;
    }

    public SqTable With(IExprTableFullName? fullName = null, TableColumnsMapper? columnsMapper = null, TableIndexesMapper? tableIndexesMapper = null)
    {
        return this.With(this.Alias, fullName, columnsMapper, tableIndexesMapper);
    }

    public SqTable With(ExprTableAlias? alias, IExprTableFullName? fullName = null, TableColumnsMapper? columnsMapper = null, TableIndexesMapper? tableIndexesMapper = null)
    {
        var result = new SqTable(fullName ?? this.FullName, alias);

        result.AddColumns(columnsMapper == null ? this.Columns : columnsMapper(this.Columns, new TableColumnAppender(result, result.Alias)));
        result.AddIndexes(tableIndexesMapper == null ? this.Indexes : tableIndexesMapper(this.Indexes, new TableIndexAppender(result.Columns)));

        return result;
    }

    public TableColumn GetColumn(string name)
        => this.Columns.FirstOrDefault(c => string.Equals(
                c.ColumnName.Name,
                name,
                StringComparison.InvariantCultureIgnoreCase
            )
        ) ?? throw new SqExpressException($"Could not find column {name} in table {this.FullName.TableName}");

    internal TableColumn AddColumn(ColumnModel columnModel, Func<ColumnRef, TableColumn> contextStorage)
    {
        var result = this.CreateColumn(columnModel, contextStorage);
        this.AddColumns(new[] { result });
        return result;
    }

    internal TableColumn CreateColumn(ColumnModel columnModel, Func<ColumnRef, TableColumn> contextStorage)
    {
        return columnModel.ColumnType.Accept(new ColumnFactory(this, contextStorage), columnModel);
    }

    private static void AddColumnsAndIndexes(SqTable result, Func<ITableColumnAppender, IEnumerable<TableColumn>> columnsBuilder, Func<ITableIndexAppender, IEnumerable<IndexMeta>>? indexBuilder)
    {
        result.AddColumns(columnsBuilder(new TableColumnAppender(result, result.Alias)));
        if (indexBuilder != null)
        {
            result.AddIndexes(indexBuilder(new TableIndexAppender(result.Columns)));
        }
    }

    private class ColumnFactory : IColumnTypeVisitor<TableColumn, ColumnModel>
    {
        private readonly Func<ColumnRef, TableColumn> _contextStorage;

        private readonly ITableColumnFactory _columnFactory;

        private ColumnMeta? CreateMeta(ColumnModel columnModel)
        {
            if (!columnModel.Identity && columnModel.Pk == null && columnModel.Fk == null && columnModel.DefaultValue == null)
            {
                return null;
            }

            ExprValue? defaultValue = null;

            if (columnModel.DefaultValue.HasValue)
            {
                switch (columnModel.DefaultValue.Value.Type)
                {
                    case DefaultValueType.Raw:
                        if (columnModel.DefaultValue.Value.RawValue != null)
                        {
                            defaultValue = SqQueryBuilder.UnsafeValue(columnModel.DefaultValue.Value.RawValue);
                        }
                        break;
                    case DefaultValueType.Null:
                        defaultValue = SqQueryBuilder.Null;
                        break;
                    case DefaultValueType.Integer:
                        if (columnModel.DefaultValue.Value.RawValue != null)
                        {
                            if (int.TryParse(columnModel.DefaultValue.Value.RawValue, out var intLit))
                            {
                                defaultValue = SqQueryBuilder.Literal(intLit);
                            }
                            else
                            {
                                defaultValue = SqQueryBuilder.UnsafeValue(columnModel.DefaultValue.Value.RawValue);
                            }
                        }
                        break;
                    case DefaultValueType.Bool:
                        if (columnModel.DefaultValue.Value.RawValue != null)
                        {
                            var valueRawValue = columnModel.DefaultValue.Value.RawValue;
                            if (bool.TryParse(valueRawValue, out var boolLit))
                            {
                                defaultValue = SqQueryBuilder.Literal(boolLit);
                            }
                            else if (valueRawValue == "0" || valueRawValue == "1")
                            {
                                defaultValue = SqQueryBuilder.Literal(valueRawValue == "1");
                            }
                            else
                            {
                                defaultValue = SqQueryBuilder.UnsafeValue(columnModel.DefaultValue.Value.RawValue);
                            }
                        }
                        break;
                    case DefaultValueType.String:
                        defaultValue = SqQueryBuilder.Literal(columnModel.DefaultValue.Value.RawValue);
                        break;
                    case DefaultValueType.GetUtcDate:
                        defaultValue = SqQueryBuilder.GetUtcDate();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return new ColumnMeta(
                columnModel.Pk != null,
                columnModel.Identity,
                columnModel.Fk?.Select(this._contextStorage).ToList(),
                defaultValue);

        }

        public ColumnFactory(SqTable table, Func<ColumnRef, TableColumn> contextStorage)
        {
            this._contextStorage = contextStorage;
            this._columnFactory = new TableColumnAppender(table, table.Alias);
        }

        public TableColumn VisitBooleanColumnType(BooleanColumnType booleanColumnType, ColumnModel arg)
        {
            return booleanColumnType.IsNullable
                ? this._columnFactory.CreateNullableBooleanColumn(arg.DbName.Name, this.CreateMeta(arg))
                : this._columnFactory.CreateBooleanColumn(arg.DbName.Name, this.CreateMeta(arg));
        }

        public TableColumn VisitByteColumnType(ByteColumnType byteColumnType, ColumnModel arg)
        {
            return byteColumnType.IsNullable
                ? this._columnFactory.CreateNullableByteColumn(arg.DbName.Name, this.CreateMeta(arg))
                : this._columnFactory.CreateByteColumn(arg.DbName.Name, this.CreateMeta(arg));
        }

        public TableColumn VisitByteArrayColumnType(ByteArrayColumnType byteArrayColumnType, ColumnModel arg)
        {
            if (!byteArrayColumnType.IsFixed)
            {
                return byteArrayColumnType.IsNullable
                    ? this._columnFactory.CreateNullableByteArrayColumn(arg.DbName.Name, byteArrayColumnType.Size, this.CreateMeta(arg))
                    : this._columnFactory.CreateByteArrayColumn(arg.DbName.Name, byteArrayColumnType.Size, this.CreateMeta(arg));
            }
            return byteArrayColumnType.IsNullable
                ? this._columnFactory.CreateNullableFixedSizeByteArrayColumn(arg.DbName.Name, byteArrayColumnType.Size ?? throw new SqExpressException("array size should be explicitly defined"), this.CreateMeta(arg))
                : this._columnFactory.CreateFixedSizeByteArrayColumn(arg.DbName.Name, byteArrayColumnType.Size ?? throw new SqExpressException("array size should be explicitly defined"), this.CreateMeta(arg));

        }

        public TableColumn VisitInt16ColumnType(Int16ColumnType int16ColumnType, ColumnModel arg)
        {
            return int16ColumnType.IsNullable
                ? this._columnFactory.CreateNullableInt16Column(arg.DbName.Name, this.CreateMeta(arg))
                : this._columnFactory.CreateInt16Column(arg.DbName.Name, this.CreateMeta(arg));
        }

        public TableColumn VisitInt32ColumnType(Int32ColumnType int32ColumnType, ColumnModel arg)
        {
            return int32ColumnType.IsNullable
                ? this._columnFactory.CreateNullableInt32Column(arg.DbName.Name, this.CreateMeta(arg))
                : this._columnFactory.CreateInt32Column(arg.DbName.Name, this.CreateMeta(arg));
        }

        public TableColumn VisitInt64ColumnType(Int64ColumnType int64ColumnType, ColumnModel arg)
        {
            return int64ColumnType.IsNullable
                ? this._columnFactory.CreateNullableInt64Column(arg.DbName.Name, this.CreateMeta(arg))
                : this._columnFactory.CreateInt64Column(arg.DbName.Name, this.CreateMeta(arg));
        }

        public TableColumn VisitDoubleColumnType(DoubleColumnType doubleColumnType, ColumnModel arg)
        {
            return doubleColumnType.IsNullable
                ? this._columnFactory.CreateNullableDoubleColumn(arg.DbName.Name, this.CreateMeta(arg))
                : this._columnFactory.CreateDoubleColumn(arg.DbName.Name, this.CreateMeta(arg));
        }

        public TableColumn VisitDecimalColumnType(DecimalColumnType decimalColumnType, ColumnModel arg)
        {
            DecimalPrecisionScale scale = new(decimalColumnType.Precision, decimalColumnType.Scale);
            return decimalColumnType.IsNullable
                ? this._columnFactory.CreateNullableDecimalColumn(arg.DbName.Name, scale, this.CreateMeta(arg))
                : this._columnFactory.CreateDecimalColumn(arg.DbName.Name, scale, this.CreateMeta(arg));
        }

        public TableColumn VisitDateTimeColumnType(DateTimeColumnType dateTimeColumnType, ColumnModel arg)
        {
            return dateTimeColumnType.IsNullable
                ? this._columnFactory.CreateNullableDateTimeColumn(arg.DbName.Name, dateTimeColumnType.IsDate,
                    this.CreateMeta(arg))
                : this._columnFactory.CreateDateTimeColumn(arg.DbName.Name, dateTimeColumnType.IsDate, this.CreateMeta(arg));
        }

        public TableColumn VisitDateTimeOffsetColumnType(DateTimeOffsetColumnType dateTimeColumnType, ColumnModel arg)
        {
            return dateTimeColumnType.IsNullable
                ? this._columnFactory.CreateNullableDateTimeOffsetColumn(arg.DbName.Name, this.CreateMeta(arg))
                : this._columnFactory.CreateDateTimeOffsetColumn(arg.DbName.Name, this.CreateMeta(arg));
        }

        public TableColumn VisitStringColumnType(StringColumnType stringColumnType, ColumnModel arg)
        {
            if (!stringColumnType.IsFixed)
            {
                return stringColumnType.IsNullable
                    ? this._columnFactory.CreateNullableStringColumn(arg.DbName.Name, stringColumnType.Size,
                        stringColumnType.IsUnicode, stringColumnType.IsText, this.CreateMeta(arg))
                    : this._columnFactory.CreateStringColumn(arg.DbName.Name, stringColumnType.Size, stringColumnType.IsUnicode,
                        stringColumnType.IsText, this.CreateMeta(arg));
            }

            return stringColumnType.IsNullable
                ? this._columnFactory.CreateNullableFixedSizeStringColumn(arg.DbName.Name,
                    stringColumnType.Size ??
                    throw new SqExpressException("string size should be explicitly defined"),
                    stringColumnType.IsUnicode, this.CreateMeta(arg))
                : this._columnFactory.CreateFixedSizeStringColumn(arg.DbName.Name,
                    stringColumnType.Size ??
                    throw new SqExpressException("string size should be explicitly defined"),
                    stringColumnType.IsUnicode, this.CreateMeta(arg));
        }

        public TableColumn VisitGuidColumnType(GuidColumnType guidColumnType, ColumnModel arg)
        {
            return guidColumnType.IsNullable
                ? this._columnFactory.CreateNullableGuidColumn(arg.DbName.Name, this.CreateMeta(arg))
                : this._columnFactory.CreateGuidColumn(arg.DbName.Name, this.CreateMeta(arg));
        }

        public TableColumn VisitXmlColumnType(XmlColumnType xmlColumnType, ColumnModel arg)
        {
            return xmlColumnType.IsNullable
                ? this._columnFactory.CreateNullableXmlColumn(arg.DbName.Name, this.CreateMeta(arg))
                : this._columnFactory.CreateXmlColumn(arg.DbName.Name, this.CreateMeta(arg));
        }
    }
}

public delegate IEnumerable<TableColumn> TableColumnsMapper(IReadOnlyList<TableColumn> existingColumns, ITableColumnAppender tableColumnAppender);

public delegate IEnumerable<IndexMeta> TableIndexesMapper(IReadOnlyList<IndexMeta> existingIndexes, ITableIndexAppender tableIndexAppender);
