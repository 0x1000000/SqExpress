using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using SqExpress.DbMetadata;
using SqExpress.DbMetadata.Internal;
using SqExpress.DbMetadata.Internal.Model;
using SqExpress.Meta;
using SqExpress.Syntax;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress.CodeGen.Shared
{
    internal static class CodeGenSqTableProjector
    {
        internal static IReadOnlyDictionary<string, CodeGenTableModel> BuildCodeGenTables(
            IReadOnlyDictionary<TableRef, TableModel> allTables,
            string defaultNamespace,
            bool skipUnknownColumnTypes,
            IReadOnlyDictionary<TableRef, string>? tableNamespaces = null)
        {
            var propertyNamesByColumn = allTables.Values
                .SelectMany(t => t.Columns)
                .ToDictionary(static c => c.DbName, static c => c.Name);

            var sqTables = DbModelMapper.ToSqDbTables(allTables.Values.ToList(), skipUnknownColumnTypes)
                .ToDictionary(ToTableRef);

            return allTables
                .Select(pair => ToCodeGenTableModel(
                        sqTables[pair.Key],
                        pair.Value.Name,
                        tableNamespaces != null && tableNamespaces.TryGetValue(pair.Key, out var tableNamespace)
                            ? tableNamespace
                            : defaultNamespace,
                        propertyNamesByColumn
                    )
                )
                .ToDictionary(static t => t.TableKey, static t => t, System.StringComparer.OrdinalIgnoreCase);
        }

        private static CodeGenTableModel ToCodeGenTableModel(
            SqTable table,
            string className,
            string defaultNamespace,
            IReadOnlyDictionary<ColumnRef, string> propertyNamesByColumn)
        {
            var typeNamespace = string.IsNullOrEmpty(defaultNamespace) ? null : defaultNamespace;
            var fullyQualifiedTypeName = string.IsNullOrEmpty(typeNamespace)
                ? className
                : typeNamespace + "." + className;

            return new CodeGenTableModel(
                CodeGenTableKind.Table,
                databaseName: null,
                schemaName: table.FullName.SchemaName,
                tableName: table.FullName.TableName,
                className: className,
                @namespace: typeNamespace,
                fullyQualifiedTypeName: fullyQualifiedTypeName,
                columns: table.Columns
                    .Select(c => ToCodeGenColumnModel(c, propertyNamesByColumn))
                    .ToImmutableArray(),
                indexes: table.Indexes.Select(ToCodeGenIndexModel).ToImmutableArray()
            );
        }

        private static CodeGenColumnModel ToCodeGenColumnModel(
            TableColumn column,
            IReadOnlyDictionary<ColumnRef, string> propertyNamesByColumn)
        {
            var projection = column.Accept(ColumnProjectionVisitor.Instance);
            var foreignKey = column.ColumnMeta?.ForeignKeyColumns?.FirstOrDefault();
            var defaultValue =
                column.ColumnMeta?.ColumnDefaultValue?.Accept(DefaultValueProjectionVisitor.Instance, arg: null) ??
                (CodeGenDefaultValueKind.None, null);

            return new CodeGenColumnModel(
                kind: projection.Kind,
                sqlName: column.ColumnName.Name,
                propertyName: propertyNamesByColumn.TryGetValue(ToColumnRef(column), out var propertyName)
                    ? propertyName
                    : null,
                isPrimaryKey: column.ColumnMeta?.IsPrimaryKey ?? false,
                isIdentity: column.ColumnMeta?.IsIdentity ?? false,
                foreignKeyDatabase: null,
                foreignKeySchema: foreignKey?.Table.FullName.SchemaName,
                foreignKeyTable: foreignKey?.Table.FullName.TableName,
                foreignKeyColumn: foreignKey?.ColumnName.Name,
                defaultValueKind: defaultValue.Kind,
                defaultValue: defaultValue.Value,
                isUnicode: projection.IsUnicode,
                maxLength: projection.MaxLength,
                isFixedLength: projection.IsFixedLength,
                isText: projection.IsText,
                precision: projection.Precision,
                scale: projection.Scale,
                isDate: projection.IsDate
            );
        }

        private static CodeGenIndexModel ToCodeGenIndexModel(IndexMeta index)
        {
            return new CodeGenIndexModel(
                columns: index.Columns.Select(static c => c.Column.ColumnName.Name).ToImmutableArray(),
                descendingColumns: index.Columns.Where(static c => c.Descending)
                    .Select(static c => c.Column.ColumnName.Name)
                    .ToImmutableArray(),
                name: null,
                isUnique: index.Unique,
                isClustered: index.Clustered
            );
        }

        private static (CodeGenDefaultValueKind Kind, string? Value) ProjectDefaultValue(ExprValue? value)
            => value?.Accept(DefaultValueProjectionVisitor.Instance, arg: null) ?? (CodeGenDefaultValueKind.None, null);

        private static TableRef ToTableRef(SqTable table)
        {
            return new TableRef(
                schema: table.FullName.SchemaName ?? string.Empty,
                name: table.FullName.TableName
            );
        }

        private static ColumnRef ToColumnRef(TableColumn column)
        {
            return new ColumnRef(
                schema: column.Table.FullName.SchemaName ?? string.Empty,
                tableName: column.Table.FullName.TableName,
                name: column.ColumnName.Name
            );
        }

        private readonly struct ColumnProjection
        {
            public ColumnProjection(
                CodeGenColumnKind kind,
                bool isUnicode = false,
                int? maxLength = null,
                bool isFixedLength = false,
                bool isText = false,
                int precision = 0,
                int scale = 0,
                bool isDate = false)
            {
                this.Kind = kind;
                this.IsUnicode = isUnicode;
                this.MaxLength = maxLength;
                this.IsFixedLength = isFixedLength;
                this.IsText = isText;
                this.Precision = precision;
                this.Scale = scale;
                this.IsDate = isDate;
            }

            public CodeGenColumnKind Kind { get; }

            public bool IsUnicode { get; }

            public int? MaxLength { get; }

            public bool IsFixedLength { get; }

            public bool IsText { get; }

            public int Precision { get; }

            public int Scale { get; }

            public bool IsDate { get; }
        }

        private sealed class ColumnProjectionVisitor : ITableColumnVisitor<ColumnProjection>
        {
            public static readonly ColumnProjectionVisitor Instance = new ColumnProjectionVisitor();

            private ColumnProjectionVisitor()
            {
            }

            public ColumnProjection VisitBoolean(BooleanTableColumn booleanTableColumn)
                => new ColumnProjection(CodeGenColumnKind.Boolean);

            public ColumnProjection VisitNullableBoolean(NullableBooleanTableColumn nullableBooleanTableColumn)
                => new ColumnProjection(CodeGenColumnKind.NullableBoolean);

            public ColumnProjection VisitByte(ByteTableColumn byteTableColumn)
                => new ColumnProjection(CodeGenColumnKind.Byte);

            public ColumnProjection VisitNullableByte(NullableByteTableColumn nullableByteTableColumn)
                => new ColumnProjection(CodeGenColumnKind.NullableByte);

            public ColumnProjection VisitByteArray(ByteArrayTableColumn byteTableColumn)
                => ProjectByteArray(CodeGenColumnKind.ByteArray, byteTableColumn.SqlType);

            public ColumnProjection VisitNullableByteArray(NullableByteArrayTableColumn nullableByteTableColumn)
                => ProjectByteArray(CodeGenColumnKind.NullableByteArray, nullableByteTableColumn.SqlType);

            public ColumnProjection VisitInt16(Int16TableColumn int16TableColumn)
                => new ColumnProjection(CodeGenColumnKind.Int16);

            public ColumnProjection VisitNullableInt16(NullableInt16TableColumn nullableInt16TableColumn)
                => new ColumnProjection(CodeGenColumnKind.NullableInt16);

            public ColumnProjection VisitInt32(Int32TableColumn int32TableColumn)
                => new ColumnProjection(CodeGenColumnKind.Int32);

            public ColumnProjection VisitNullableInt32(NullableInt32TableColumn nullableInt32TableColumn)
                => new ColumnProjection(CodeGenColumnKind.NullableInt32);

            public ColumnProjection VisitInt64(Int64TableColumn int64TableColumn)
                => new ColumnProjection(CodeGenColumnKind.Int64);

            public ColumnProjection VisitNullableInt64(NullableInt64TableColumn nullableInt64TableColumn)
                => new ColumnProjection(CodeGenColumnKind.NullableInt64);

            public ColumnProjection VisitDecimal(DecimalTableColumn decimalTableColumn)
                => ProjectDecimal(CodeGenColumnKind.Decimal, decimalTableColumn.SqlType);

            public ColumnProjection VisitNullableDecimal(NullableDecimalTableColumn nullableDecimalTableColumn)
                => ProjectDecimal(CodeGenColumnKind.NullableDecimal, nullableDecimalTableColumn.SqlType);

            public ColumnProjection VisitDouble(DoubleTableColumn doubleTableColumn)
                => new ColumnProjection(CodeGenColumnKind.Double);

            public ColumnProjection VisitNullableDouble(NullableDoubleTableColumn nullableDoubleTableColumn)
                => new ColumnProjection(CodeGenColumnKind.NullableDouble);

            public ColumnProjection VisitDateTime(DateTimeTableColumn dateTimeTableColumn)
                => ProjectDateTime(CodeGenColumnKind.DateTime, dateTimeTableColumn.SqlType);

            public ColumnProjection VisitNullableDateTime(NullableDateTimeTableColumn nullableDateTimeTableColumn)
                => ProjectDateTime(CodeGenColumnKind.NullableDateTime, nullableDateTimeTableColumn.SqlType);

            public ColumnProjection VisitDateTimeOffset(DateTimeOffsetTableColumn dateTimeTableColumn)
                => new ColumnProjection(CodeGenColumnKind.DateTimeOffset);

            public ColumnProjection VisitNullableDateTimeOffset(
                NullableDateTimeOffsetTableColumn nullableDateTimeOffsetColumn)
                => new ColumnProjection(CodeGenColumnKind.NullableDateTimeOffset);

            public ColumnProjection VisitGuid(GuidTableColumn guidTableColumn)
                => new ColumnProjection(CodeGenColumnKind.Guid);

            public ColumnProjection VisitNullableGuid(NullableGuidTableColumn nullableGuidTableColumn)
                => new ColumnProjection(CodeGenColumnKind.NullableGuid);

            public ColumnProjection VisitString(StringTableColumn stringTableColumn)
                => ProjectString(CodeGenColumnKind.String, CodeGenColumnKind.Xml, stringTableColumn.SqlType);

            public ColumnProjection VisitNullableString(NullableStringTableColumn nullableStringTableColumn)
                => ProjectString(
                    CodeGenColumnKind.NullableString,
                    CodeGenColumnKind.NullableXml,
                    nullableStringTableColumn.SqlType
                );

            private static ColumnProjection ProjectByteArray(CodeGenColumnKind kind, ExprType sqlType)
            {
                return sqlType switch
                {
                    ExprTypeFixSizeByteArray fixedSizeByteArray => new ColumnProjection(
                        kind,
                        maxLength: fixedSizeByteArray.Size,
                        isFixedLength: true
                    ),
                    ExprTypeByteArray byteArray => new ColumnProjection(
                        kind,
                        maxLength: byteArray.Size,
                        isFixedLength: false
                    ),
                    _ => new ColumnProjection(kind)
                };
            }

            private static ColumnProjection ProjectDecimal(CodeGenColumnKind kind, ExprType sqlType)
            {
                return sqlType is ExprTypeDecimal decimalType && decimalType.PrecisionScale.HasValue
                    ? new ColumnProjection(
                        kind,
                        precision: decimalType.PrecisionScale.Value.Precision,
                        scale: decimalType.PrecisionScale.Value.Scale ?? 0
                    )
                    : new ColumnProjection(kind);
            }

            private static ColumnProjection ProjectDateTime(CodeGenColumnKind kind, ExprType sqlType)
            {
                return sqlType is ExprTypeDateTime dateTime
                    ? new ColumnProjection(kind, isDate: dateTime.IsDate)
                    : new ColumnProjection(kind);
            }

            private static ColumnProjection ProjectString(
                CodeGenColumnKind stringKind,
                CodeGenColumnKind xmlKind,
                ExprType sqlType)
            {
                switch (sqlType)
                {
                    case ExprTypeXml:
                        return new ColumnProjection(xmlKind);
                    case ExprTypeFixSizeString fixedSizeString:
                        return new ColumnProjection(
                            stringKind,
                            isUnicode: fixedSizeString.IsUnicode,
                            maxLength: fixedSizeString.Size,
                            isFixedLength: true
                        );
                    case ExprTypeString stringType:
                        return new ColumnProjection(
                            stringKind,
                            isUnicode: stringType.IsUnicode,
                            maxLength: stringType.Size,
                            isText: stringType.IsText
                        );
                    default:
                        return new ColumnProjection(stringKind);
                }
            }
        }

        private sealed class DefaultValueProjectionVisitor 
            : IExprValueVisitor<(CodeGenDefaultValueKind Kind, string? Value), object?>
        {
            public static readonly DefaultValueProjectionVisitor Instance = new DefaultValueProjectionVisitor();

            private static (CodeGenDefaultValueKind Kind, string? Value) Unsupported()
                => (CodeGenDefaultValueKind.None, null);

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprInt32Literal(
                ExprInt32Literal exprInt32Literal,
                object? arg)
                => (CodeGenDefaultValueKind.Int32, exprInt32Literal.Value?.ToString(CultureInfo.InvariantCulture));

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprGuidLiteral(
                ExprGuidLiteral exprGuidLiteral,
                object? arg)
                => (CodeGenDefaultValueKind.Guid, exprGuidLiteral.Value?.ToString("D"));

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprStringLiteral(
                ExprStringLiteral stringLiteral,
                object? arg)
                => (CodeGenDefaultValueKind.String, stringLiteral.Value);

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprDateTimeLiteral(
                ExprDateTimeLiteral dateTimeLiteral,
                object? arg)
                => (CodeGenDefaultValueKind.DateTime,
                    dateTimeLiteral.Value?.ToString("o", CultureInfo.InvariantCulture));

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprDateTimeOffsetLiteral(
                ExprDateTimeOffsetLiteral dateTimeLiteral,
                object? arg)
                => (CodeGenDefaultValueKind.DateTimeOffset,
                    dateTimeLiteral.Value?.ToString("o", CultureInfo.InvariantCulture));

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprBoolLiteral(
                ExprBoolLiteral boolLiteral,
                object? arg)
                => (CodeGenDefaultValueKind.Boolean,
                    boolLiteral.Value.HasValue ? (boolLiteral.Value.Value ? "1" : "0") : null);

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprInt64Literal(
                ExprInt64Literal int64Literal,
                object? arg)
                => (CodeGenDefaultValueKind.Int64, int64Literal.Value?.ToString(CultureInfo.InvariantCulture));

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprByteLiteral(
                ExprByteLiteral byteLiteral,
                object? arg)
                => (CodeGenDefaultValueKind.Byte, byteLiteral.Value?.ToString(CultureInfo.InvariantCulture));

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprInt16Literal(
                ExprInt16Literal int16Literal,
                object? arg)
                => (CodeGenDefaultValueKind.Int16, int16Literal.Value?.ToString(CultureInfo.InvariantCulture));

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprDecimalLiteral(
                ExprDecimalLiteral decimalLiteral,
                object? arg)
                => (CodeGenDefaultValueKind.Decimal, decimalLiteral.Value?.ToString(CultureInfo.InvariantCulture));

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprDoubleLiteral(
                ExprDoubleLiteral doubleLiteral,
                object? arg)
                => (CodeGenDefaultValueKind.Double, doubleLiteral.Value?.ToString("R", CultureInfo.InvariantCulture));

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprByteArrayLiteral(
                ExprByteArrayLiteral byteArrayLiteral,
                object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprNull(ExprNull exprNull, object? arg)
                => (CodeGenDefaultValueKind.Null, null);

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprUnsafeValue(
                ExprUnsafeValue exprUnsafeValue,
                object? arg)
                => (CodeGenDefaultValueKind.RawSql, exprUnsafeValue.UnsafeValue);

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprSelectingValue(
                ExprSelectingValue exprSelectingValue,
                object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprValueQuery(
                ExprValueQuery exprValueQuery,
                object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprSum(ExprSum exprSum, object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprSub(ExprSub exprSub, object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprMul(ExprMul exprMul, object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprDiv(ExprDiv exprDiv, object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprModulo(ExprModulo exprModulo, object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprStringConcat(
                ExprStringConcat exprStringConcat,
                object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprBitwiseNot(
                ExprBitwiseNot exprBitwiseNot,
                object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprBitwiseAnd(
                ExprBitwiseAnd exprBitwiseAnd,
                object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprBitwiseXor(
                ExprBitwiseXor exprBitwiseXor,
                object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprBitwiseOr(
                ExprBitwiseOr exprBitwiseOr,
                object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprScalarFunction(
                ExprScalarFunction exprScalarFunction,
                object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprPortableScalarFunction(
                ExprPortableScalarFunction exprPortableScalarFunction,
                object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprCase(ExprCase exprCase, object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprCaseWhenThen(
                ExprCaseWhenThen exprCaseWhenThen,
                object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprFuncIsNull(
                ExprFuncIsNull exprFuncIsNull,
                object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprFuncCoalesce(
                ExprFuncCoalesce exprFuncCoalesce,
                object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprGetDate(ExprGetDate exprGetDate, object? arg)
                => (CodeGenDefaultValueKind.Now, null);

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprGetUtcDate(
                ExprGetUtcDate exprGetUtcDate,
                object? arg)
                => (CodeGenDefaultValueKind.UtcNow, null);

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprDateAdd(ExprDateAdd exprDateAdd, object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprDateDiff(
                ExprDateDiff exprDateDiff,
                object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprColumn(ExprColumn exprColumn, object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprCast(ExprCast exprCast, object? arg)
                => Unsupported();

            public (CodeGenDefaultValueKind Kind, string? Value) VisitExprParameter(
                ExprParameter exprParameter,
                object? arg)
                => Unsupported();
        }
    }
}
