﻿using System;
using System.Linq;
using SqExpress.DbMetadata.Internal.Model;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress.DbMetadata;

public sealed class SqTable : TableBase
{
    internal SqTable(string? schema, string name, Alias alias = default) : base(schema, name, alias)
    {
    }

    internal TableColumn AddColumn(ColumnModel columnModel, Func<ColumnRef, TableColumn> contextStorage)
    {
        return columnModel.ColumnType.Accept(new ColumnFactory(this, contextStorage), columnModel);
    }

    private class ColumnFactory : IColumnTypeVisitor<TableColumn, ColumnModel>
    {
        private readonly SqTable _table;

        private readonly Func<ColumnRef, TableColumn> _contextStorage;

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
                            defaultValue = SqQueryBuilder.Literal(columnModel.DefaultValue.Value.RawValue);
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
                                defaultValue = SqQueryBuilder.Literal(columnModel.DefaultValue.Value.RawValue);
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
            this._table = table;
            this._contextStorage = contextStorage;
        }

        public TableColumn VisitBooleanColumnType(BooleanColumnType booleanColumnType, ColumnModel arg)
        {
            return booleanColumnType.IsNullable
                ? this._table.CreateNullableBooleanColumn(arg.DbName.Name, this.CreateMeta(arg))
                : this._table.CreateBooleanColumn(arg.DbName.Name, this.CreateMeta(arg));
        }

        public TableColumn VisitByteColumnType(ByteColumnType byteColumnType, ColumnModel arg)
        {
            return byteColumnType.IsNullable
                ? this._table.CreateNullableByteColumn(arg.DbName.Name, this.CreateMeta(arg))
                : this._table.CreateByteColumn(arg.DbName.Name, this.CreateMeta(arg));
        }

        public TableColumn VisitByteArrayColumnType(ByteArrayColumnType byteArrayColumnType, ColumnModel arg)
        {
            if (!byteArrayColumnType.IsFixed)
            {
                return byteArrayColumnType.IsNullable
                    ? this._table.CreateNullableByteArrayColumn(arg.DbName.Name, byteArrayColumnType.Size, this.CreateMeta(arg))
                    : this._table.CreateByteArrayColumn(arg.DbName.Name, byteArrayColumnType.Size, this.CreateMeta(arg));
            }
            return byteArrayColumnType.IsNullable
                ? this._table.CreateNullableFixedSizeByteArrayColumn(arg.DbName.Name, byteArrayColumnType.Size ?? throw new SqExpressException("array size should be explicitly defined"), this.CreateMeta(arg))
                : this._table.CreateFixedSizeByteArrayColumn(arg.DbName.Name, byteArrayColumnType.Size ?? throw new SqExpressException("array size should be explicitly defined"), this.CreateMeta(arg));

        }

        public TableColumn VisitInt16ColumnType(Int16ColumnType int16ColumnType, ColumnModel arg)
        {
            return int16ColumnType.IsNullable
                ? this._table.CreateNullableInt16Column(arg.DbName.Name, this.CreateMeta(arg))
                : this._table.CreateInt16Column(arg.DbName.Name, this.CreateMeta(arg));
        }

        public TableColumn VisitInt32ColumnType(Int32ColumnType int32ColumnType, ColumnModel arg)
        {
            return int32ColumnType.IsNullable
                ? this._table.CreateNullableInt32Column(arg.DbName.Name, this.CreateMeta(arg))
                : this._table.CreateInt32Column(arg.DbName.Name, this.CreateMeta(arg));
        }

        public TableColumn VisitInt64ColumnType(Int64ColumnType int64ColumnType, ColumnModel arg)
        {
            return int64ColumnType.IsNullable
                ? this._table.CreateNullableInt64Column(arg.DbName.Name, this.CreateMeta(arg))
                : this._table.CreateInt64Column(arg.DbName.Name, this.CreateMeta(arg));
        }

        public TableColumn VisitDoubleColumnType(DoubleColumnType doubleColumnType, ColumnModel arg)
        {
            return doubleColumnType.IsNullable
                ? this._table.CreateNullableDoubleColumn(arg.DbName.Name, this.CreateMeta(arg))
                : this._table.CreateDoubleColumn(arg.DbName.Name, this.CreateMeta(arg));
        }

        public TableColumn VisitDecimalColumnType(DecimalColumnType decimalColumnType, ColumnModel arg)
        {
            DecimalPrecisionScale scale = new(decimalColumnType.Precision, decimalColumnType.Scale);
            return decimalColumnType.IsNullable
                ? this._table.CreateNullableDecimalColumn(arg.DbName.Name, scale, this.CreateMeta(arg))
                : this._table.CreateDecimalColumn(arg.DbName.Name, scale, this.CreateMeta(arg));
        }

        public TableColumn VisitDateTimeColumnType(DateTimeColumnType dateTimeColumnType, ColumnModel arg)
        {
            return dateTimeColumnType.IsNullable
                ? this._table.CreateNullableDateTimeColumn(arg.DbName.Name, dateTimeColumnType.IsDate,
                    this.CreateMeta(arg))
                : this._table.CreateDateTimeColumn(arg.DbName.Name, dateTimeColumnType.IsDate, this.CreateMeta(arg));
        }

        public TableColumn VisitDateTimeOffsetColumnType(DateTimeOffsetColumnType dateTimeColumnType, ColumnModel arg)
        {
            return dateTimeColumnType.IsNullable
                ? this._table.CreateNullableDateTimeOffsetColumn(arg.DbName.Name, this.CreateMeta(arg))
                : this._table.CreateDateTimeOffsetColumn(arg.DbName.Name, this.CreateMeta(arg));
        }

        public TableColumn VisitStringColumnType(StringColumnType stringColumnType, ColumnModel arg)
        {
            if (!stringColumnType.IsFixed)
            {
                return stringColumnType.IsNullable
                    ? this._table.CreateNullableStringColumn(arg.DbName.Name, stringColumnType.Size,
                        stringColumnType.IsUnicode, stringColumnType.IsText, this.CreateMeta(arg))
                    : this._table.CreateStringColumn(arg.DbName.Name, stringColumnType.Size, stringColumnType.IsUnicode,
                        stringColumnType.IsText, this.CreateMeta(arg));
            }

            return stringColumnType.IsNullable
                ? this._table.CreateNullableFixedSizeStringColumn(arg.DbName.Name,
                    stringColumnType.Size ??
                    throw new SqExpressException("string size should be explicitly defined"),
                    stringColumnType.IsUnicode, this.CreateMeta(arg))
                : this._table.CreateFixedSizeStringColumn(arg.DbName.Name,
                    stringColumnType.Size ??
                    throw new SqExpressException("string size should be explicitly defined"),
                    stringColumnType.IsUnicode, this.CreateMeta(arg));
        }

        public TableColumn VisitGuidColumnType(GuidColumnType guidColumnType, ColumnModel arg)
        {
            return guidColumnType.IsNullable
                ? this._table.CreateNullableGuidColumn(arg.DbName.Name, this.CreateMeta(arg))
                : this._table.CreateGuidColumn(arg.DbName.Name, this.CreateMeta(arg));
        }

        public TableColumn VisitXmlColumnType(XmlColumnType xmlColumnType, ColumnModel arg)
        {
            return xmlColumnType.IsNullable
                ? this._table.CreateNullableXmlColumn(arg.DbName.Name, this.CreateMeta(arg))
                : this._table.CreateXmlColumn(arg.DbName.Name, this.CreateMeta(arg));
        }
    }
}