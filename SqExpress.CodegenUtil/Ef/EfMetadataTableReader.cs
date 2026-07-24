using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.CodeGenUtil.Ef
{
    internal static class EfMetadataTableReader
    {
        public static IReadOnlyList<TableModel> SelectTables(
            EfMetadataDocument document,
            string tableClassPrefix,
            bool skipUnknownColumnTypes)
        {
            if (document.ProviderName.IndexOf("SqlServer", StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new SqExpressCodeGenException(
                    $"Unsupported EF provider \"{document.ProviderName}\". Only Microsoft SQL Server is supported by EF mode currently.");
            }

            var tableModels = new Dictionary<TableRef, TableModel>();
            foreach (var table in document.Tables)
            {
                var tableRef = new TableRef(string.IsNullOrWhiteSpace(table.Schema) ? "dbo" : table.Schema, table.Name);
                var mappedColumns = table.Columns
                    .Select(column => (Column: column, IsSupported: TryMapColumnType(column, out var columnType), ColumnType: columnType))
                    .ToList();
                var unsupportedColumns = mappedColumns
                    .Where(c => !c.IsSupported)
                    .Select(c => c.Column.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (!skipUnknownColumnTypes && unsupportedColumns.Count > 0)
                {
                    var unsupportedColumn = mappedColumns.First(c => !c.IsSupported).Column;
                    throw new SqExpressCodeGenException(
                        $"Unsupported EF column type \"{unsupportedColumn.StoreType}\" for {tableRef.Schema}.{tableRef.Name}.{unsupportedColumn.Name}. " +
                        "Use --skip-unknown-column-types to omit unsupported columns, or add a supported type mapping.");
                }

                var primaryKeyIsComplete = !table.Columns.Any(c =>
                    c.PrimaryKeyIndex != null && unsupportedColumns.Contains(c.Name));
                var columns = new List<ColumnModel>();
                foreach (var mappedColumn in mappedColumns)
                {
                    if (!mappedColumn.IsSupported)
                    {
                        continue;
                    }

                    var column = mappedColumn.Column;
                    columns.Add(new ColumnModel(
                        StringHelper.DeSnake(column.Name),
                        new ColumnRef(tableRef.Schema, tableRef.Name, column.Name),
                        columns.Count + 1,
                        mappedColumn.ColumnType!,
                        primaryKeyIsComplete && column.PrimaryKeyIndex != null
                            ? new PkInfo(column.PrimaryKeyIndex.Value, descending: false)
                            : null,
                        column.Identity,
                        GetDefaultValue(column),
                        column.ForeignKeys.Select(f => new ColumnRef(f.Schema, f.Table, f.Column)).ToList()));
                }

                var includedColumnNames = columns
                    .Select(c => c.DbName.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var indexes = table.Indexes
                    .Where(i => i.Columns.Count > 0 && i.Columns.All(c => includedColumnNames.Contains(c.Name)))
                    .Select(i => new IndexModel(
                        i.Columns
                            .Select(c => new IndexColumnModel(c.Descending, new ColumnRef(tableRef.Schema, tableRef.Name, c.Name)))
                            .ToList(),
                        i.Name,
                        i.Unique,
                        i.Clustered))
                    .ToList();

                tableModels.Add(
                    tableRef,
                    new TableModel(tableClassPrefix + StringHelper.DeSnake(tableRef.Name), tableRef, columns, indexes));
            }

            return tableModels.Values
                .OrderBy(GetForeignKeyDepth)
                .ThenBy(t => t.DbName)
                .ToList();

            int GetForeignKeyDepth(TableModel table)
            {
                var visited = new HashSet<TableRef>();
                return Count(table.DbName);

                int Count(TableRef tableRef)
                {
                    if (!visited.Add(tableRef) || !tableModels.TryGetValue(tableRef, out var current))
                    {
                        return 0;
                    }

                    var parents = current.Columns
                        .Where(c => c.Fk != null)
                        .SelectMany(c => c.Fk!)
                        .Select(c => c.Table)
                        .Where(t => !t.Equals(tableRef))
                        .Distinct()
                        .ToArray();

                    return parents.Length == 0 ? 0 : parents.Max(Count) + 1;
                }
            }
        }

        private static DefaultValue? GetDefaultValue(EfColumnMetadata column)
        {
            return column.DefaultValueKind switch
            {
                "GetUtcDate" => new DefaultValue(DefaultValueType.GetUtcDate, null),
                "Null" => new DefaultValue(DefaultValueType.Null, null),
                "Bool" => new DefaultValue(DefaultValueType.Bool, column.DefaultValue),
                "String" => new DefaultValue(DefaultValueType.String, column.DefaultValue),
                "Raw" => new DefaultValue(DefaultValueType.Raw, column.DefaultValue),
                _ => null
            };
        }

        private static bool TryMapColumnType(EfColumnMetadata metadata, out ColumnType? columnType)
        {
            var storeType = (metadata.StoreType ?? string.Empty).Trim().ToLowerInvariant();
            var maxLength = metadata.MaxLength ?? ParseStoreTypeIntArgument(storeType, 0);
            var precision = metadata.Precision ?? ParseStoreTypeIntArgument(storeType, 0);
            var scale = metadata.Scale ?? ParseStoreTypeIntArgument(storeType, 1);
            var baseType = storeType.Split(new[] { '(', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

            switch (baseType)
            {
                case "bigint":
                    columnType = new Int64ColumnType(metadata.Nullable);
                    return true;
                case "time":
                    columnType = new Int64ColumnType(metadata.Nullable);
                    return true;
                case "int":
                    columnType = new Int32ColumnType(metadata.Nullable);
                    return true;
                case "smallint":
                    columnType = new Int16ColumnType(metadata.Nullable);
                    return true;
                case "tinyint":
                    columnType = new ByteColumnType(metadata.Nullable);
                    return true;
                case "bit":
                    columnType = new BooleanColumnType(metadata.Nullable);
                    return true;
                case "float":
                case "real":
                    columnType = new DoubleColumnType(metadata.Nullable);
                    return true;
                case "decimal":
                case "numeric":
                case "money":
                case "smallmoney":
                    columnType = new DecimalColumnType(metadata.Nullable, precision ?? 18, scale ?? 2);
                    return true;
                case "date":
                    columnType = new DateTimeColumnType(metadata.Nullable, isDate: true);
                    return true;
                case "datetime":
                case "datetime2":
                case "smalldatetime":
                case "timestamp":
                    columnType = new DateTimeColumnType(metadata.Nullable, isDate: false);
                    return true;
                case "datetimeoffset":
                    columnType = new DateTimeOffsetColumnType(metadata.Nullable);
                    return true;
                case "uniqueidentifier":
                    columnType = new GuidColumnType(metadata.Nullable);
                    return true;
                case "xml":
                    columnType = new XmlColumnType(metadata.Nullable);
                    return true;
                case "binary":
                    columnType = new ByteArrayColumnType(metadata.Nullable, maxLength, isFixed: true);
                    return true;
                case "varbinary":
                case "image":
                    columnType = new ByteArrayColumnType(metadata.Nullable, maxLength, isFixed: false);
                    return true;
                case "char":
                    columnType = new StringColumnType(metadata.Nullable, maxLength, isFixed: true, isUnicode: false, isText: false);
                    return true;
                case "nchar":
                    columnType = new StringColumnType(metadata.Nullable, maxLength, isFixed: true, isUnicode: true, isText: false);
                    return true;
                case "varchar":
                case "text":
                    columnType = new StringColumnType(metadata.Nullable, maxLength, isFixed: false, isUnicode: false, isText: baseType == "text");
                    return true;
                case "nvarchar":
                case "ntext":
                    columnType = new StringColumnType(metadata.Nullable, maxLength, isFixed: false, isUnicode: metadata.Unicode ?? true, isText: baseType == "ntext");
                    return true;
            }

            if (metadata.ClrType == "System.Int32")
            {
                columnType = new Int32ColumnType(metadata.Nullable);
                return true;
            }

            if (metadata.ClrType == "System.String")
            {
                columnType = new StringColumnType(metadata.Nullable, maxLength, isFixed: false, isUnicode: metadata.Unicode ?? true, isText: false);
                return true;
            }

            columnType = null;
            return false;
        }

        private static int? ParseStoreTypeIntArgument(string storeType, int index)
        {
            var open = storeType.IndexOf('(');
            var close = storeType.IndexOf(')', open + 1);
            if (open < 0 || close < 0)
            {
                return null;
            }

            var args = storeType.Substring(open + 1, close - open - 1)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .ToArray();
            if (index >= args.Length || string.Equals(args[index], "max", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return int.TryParse(args[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }
    }
}
