using System;
using System.Collections.Generic;
using SqExpress.Syntax;
using SqExpress.Syntax.Internal;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Update;
using SqExpress.SyntaxTreeOperations.Internal;

namespace SqExpress.Utils
{
    internal readonly struct TempTableBuilderCtx
    {
        public readonly ExprColumnName ColumnName;
        public readonly TableColumn? PreviousType;
        public readonly bool PrimaryKey;

        public TempTableBuilderCtx(ExprColumnName columnName, TableColumn? previousType, bool primaryKey)
        {
            this.ColumnName = columnName;
            this.PreviousType = previousType;
            this.PrimaryKey = primaryKey;
        }
    }

    internal class TempTableData : TempTableBase, IExprValueTypeVisitor<TableColumn?, TempTableBuilderCtx>
    {
        private static string GenerateName() => $"t{Guid.NewGuid().ToString("N")}";

        private TempTableData(string name,Alias alias = default) : base(name, alias)
        {
        }

        public static ExprList FromDerivedTableValuesInsert(ExprDerivedTableValues derivedTableValues, IReadOnlyList<ExprColumnName>? keys, out TempTableBase tempTable, Alias alias = default, string? name = null)
        {
            tempTable = FromDerivedTableValues(derivedTableValues, keys, alias, name);

            var insertData = derivedTableValues.Values.Items.SelectToReadOnlyList(r => new ExprInsertValueRow(r.Items));

            var insert = SqQueryBuilder.InsertInto(tempTable, derivedTableValues.Columns).Values(new ExprInsertValues(insertData));

            return new ExprList(new IExprExec[] {new ExprStatement(tempTable.Script.Create()), insert});
        }

        public static TempTableData FromDerivedTableValues(ExprDerivedTableValues derivedTableValues, IReadOnlyList<ExprColumnName>? keys, Alias alias = default, string? name = null)
        {
            var result = new TempTableData(string.IsNullOrEmpty(name) || name == null ? GenerateName() : name, alias);

            derivedTableValues.Columns.AssertNotEmpty("Columns list cannot be empty");
            derivedTableValues.Values.Items.AssertNotEmpty("Rows list cannot be empty");

            TableColumn?[] tableColumns = new TableColumn?[derivedTableValues.Columns.Count];

            for (var rowIndex = 0; rowIndex < derivedTableValues.Values.Items.Count; rowIndex++)
            {
                var lastRow = rowIndex + 1 == derivedTableValues.Values.Items.Count;
                var row = derivedTableValues.Values.Items[rowIndex];
                if (row.Items.Count != derivedTableValues.Columns.Count)
                {
                    throw new SqExpressException("Number of values in a row does not match number of columns");
                }

                for (var valueIndex = 0; valueIndex < row.Items.Count; valueIndex++)
                {
                    var value = row.Items[valueIndex];
                    var previousColumn = tableColumns[valueIndex];
                    var currentColumnName = derivedTableValues.Columns[valueIndex];
                    var res = value.Accept(ExprValueTypeAnalyzer<TableColumn?, TempTableBuilderCtx>.Instance,
                        new ExprValueTypeAnalyzerCtx<TableColumn?, TempTableBuilderCtx>(
                            new TempTableBuilderCtx(
                                currentColumnName,
                                previousColumn,
                                CheckIsPk(currentColumnName)),
                            result));

                    tableColumns[valueIndex] = res;
                    if (lastRow)
                    {
                        if (ReferenceEquals(res, null))
                        {
                            throw new SqExpressException($"Could not evaluate column type at {valueIndex}");
                        }
                    }
                }
            }
            result.AddColumns(tableColumns!);

            return result;

            bool CheckIsPk(ExprColumnName columnName)
            {
                if (keys == null || keys.Count < 1)
                {
                    return false;
                }

                for (var index = 0; index < keys.Count; index++)
                {
                    var key = keys[index];

                    if (key.LowerInvariantName == columnName.LowerInvariantName)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public TableColumn? VisitAny(TempTableBuilderCtx arg, bool? isNull)
        {
            return arg.PreviousType;
        }

        private static T? EnsureColumnType<T>(TempTableBuilderCtx arg) where T : TableColumn
        {
            if (ReferenceEquals(arg.PreviousType, null))
            {
                return null;
            }
            if (arg.PreviousType is T result)
            {
                if (arg.PrimaryKey && (result.ColumnMeta == null || !result.ColumnMeta.IsPrimaryKey))
                {
                    throw new SqExpressException($"\"{arg.ColumnName.Name}\" should be marked as primary key in meta");
                }
                if (!arg.PrimaryKey && result.ColumnMeta != null && result.ColumnMeta.IsPrimaryKey)
                {
                    throw new SqExpressException($"\"{arg.ColumnName.Name}\" should not be marked as primary key in meta");
                }
                return result;
            }

            throw new SqExpressException($"\"{typeof(T).Name}\" was expected");
        }

        public TableColumn VisitBool(TempTableBuilderCtx arg, bool? isNull)
        {
            return EnsureColumnType<NullableBooleanTableColumn>(arg) ??
                   new NullableBooleanTableColumn(this.Alias, arg.ColumnName, this, arg.PrimaryKey ? ColumnMeta.PrimaryKey() : null);
        }

        public TableColumn VisitByte(TempTableBuilderCtx arg, bool? isNull)
        {
            return EnsureColumnType<NullableByteTableColumn>(arg) ??
                   new NullableByteTableColumn(this.Alias, arg.ColumnName, this, arg.PrimaryKey ? ColumnMeta.PrimaryKey() : null);
        }

        public TableColumn VisitInt16(TempTableBuilderCtx arg, bool? isNull)
        {
            return EnsureColumnType<NullableInt16TableColumn>(arg) ??
                   new NullableInt16TableColumn(this.Alias, arg.ColumnName, this, arg.PrimaryKey ? ColumnMeta.PrimaryKey() : null);
        }

        public TableColumn VisitInt32(TempTableBuilderCtx arg, bool? isNull)
        {
            return EnsureColumnType<NullableInt32TableColumn>(arg) ??
                   new NullableInt32TableColumn(this.Alias, arg.ColumnName, this, arg.PrimaryKey ? ColumnMeta.PrimaryKey() : null);
        }

        public TableColumn VisitInt64(TempTableBuilderCtx arg, bool? isNull)
        {
            return EnsureColumnType<NullableInt64TableColumn>(arg) ??
                   new NullableInt64TableColumn(this.Alias, arg.ColumnName, this, arg.PrimaryKey ? ColumnMeta.PrimaryKey() : null);
        }

        public TableColumn VisitDecimal(TempTableBuilderCtx arg, bool? isNull, DecimalPrecisionScale? decimalPrecisionScale)
        {
            var column = EnsureColumnType<NullableDecimalTableColumn>(arg);

            if (ReferenceEquals(column, null))
            {
                return CreateColumn(decimalPrecisionScale);
            }
            else
            {
                if (decimalPrecisionScale.HasValue)
                {
                    if (column.PrecisionScale.HasValue)
                    {
                        var old = column.PrecisionScale.Value;
                        var newPs = decimalPrecisionScale.Value;

                        if (old.Precision < newPs.Precision || old.Scale < newPs.Scale)
                        {
                            return CreateColumn(decimalPrecisionScale);
                        }
                    }
                }

                return column;
            }

            NullableDecimalTableColumn CreateColumn(DecimalPrecisionScale? precisionScale)
            {
                return new NullableDecimalTableColumn(this.Alias, arg.ColumnName, this, precisionScale, arg.PrimaryKey ? ColumnMeta.PrimaryKey() : null);
            }
        }

        public TableColumn VisitDouble(TempTableBuilderCtx arg, bool? isNull)
        {
            return EnsureColumnType<NullableDoubleTableColumn>(arg) ??
                   new NullableDoubleTableColumn(this.Alias, arg.ColumnName, this, arg.PrimaryKey ? ColumnMeta.PrimaryKey() : null);
        }

        public TableColumn VisitString(TempTableBuilderCtx arg, bool? isNull, int? size, bool fix)
        {
            var stringTableColumn = EnsureColumnType<NullableStringTableColumn>(arg);

            if (ReferenceEquals(stringTableColumn, null))
            {
                return CreateString(size);
            }
            else
            {
                if (stringTableColumn.SqlType.GetSize().HasValue && (!size.HasValue || size.Value > stringTableColumn.SqlType.GetSize()))
                {
                    return CreateString(size);
                }
                return stringTableColumn;
            }

            NullableStringTableColumn CreateString(int? len)
            {
                return new NullableStringTableColumn(this.Alias,
                    arg.ColumnName,
                    this,
                    fix
                        ? new ExprTypeFixSizeString(len.AssertNotNull("Length cannot be null for fixed size string"), true)
                        : new ExprTypeString(len, true, false),
                    arg.PrimaryKey ? ColumnMeta.PrimaryKey() : null);
            }
        }

        public TableColumn? VisitXml(TempTableBuilderCtx arg, bool? isNull)
        {
            return EnsureColumnType<NullableStringTableColumn>(arg) ??
                   new NullableStringTableColumn(this.Alias,
                       arg.ColumnName,
                       this,
                       ExprTypeXml.Instance,
                       arg.PrimaryKey ? ColumnMeta.PrimaryKey() : null);
        }

        public TableColumn? VisitDateTime(TempTableBuilderCtx arg, bool? isNull)
        {
            return EnsureColumnType<NullableDateTimeTableColumn>(arg) ??
                   new NullableDateTimeTableColumn(this.Alias, arg.ColumnName, this, false, arg.PrimaryKey ? ColumnMeta.PrimaryKey() : null);
        }

        public TableColumn? VisitDateTimeOffset(TempTableBuilderCtx arg, bool? isNull)
        {
            return EnsureColumnType<NullableDateTimeOffsetTableColumn>(arg) ??
                   new NullableDateTimeOffsetTableColumn(this.Alias, arg.ColumnName, this, arg.PrimaryKey ? ColumnMeta.PrimaryKey() : null);
        }

        public TableColumn? VisitGuid(TempTableBuilderCtx arg, bool? isNull)
        {
            return EnsureColumnType<NullableGuidTableColumn>(arg) ??
                   new NullableGuidTableColumn(this.Alias, arg.ColumnName, this, arg.PrimaryKey ? ColumnMeta.PrimaryKey() : null);
        }

        public TableColumn? VisitByteArray(TempTableBuilderCtx arg, bool? isNull, int? length, bool fix)
        {
            var arrayTableColumn = EnsureColumnType<NullableByteArrayTableColumn>(arg);

            if (ReferenceEquals(arrayTableColumn, null))
            {
                return CreateCol(length);
            }
            else
            {
                if (arrayTableColumn.SqlType.GetSize().HasValue && (!length.HasValue || length.Value > arrayTableColumn.SqlType.GetSize()))
                {
                    return CreateCol(length);
                }
                return arrayTableColumn;
            }

            NullableByteArrayTableColumn CreateCol(int? len)
            {
                return new NullableByteArrayTableColumn(this.Alias,
                    arg.ColumnName,
                    this,
                    fix
                        ? new ExprTypeFixSizeByteArray(length.AssertNotNull("Length cannot be null for fixed size array"))
                        : new ExprTypeByteArray(length),
                    arg.PrimaryKey ? ColumnMeta.PrimaryKey() : null);
            }
        }
    }
}