using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.DbMetadata.Internal.DbManagers.Sqlite
{
    internal class SqliteDbStrategy : DbStrategyBase
    {
        private readonly string _databaseName;
        private readonly DbConnection _connection;

        internal SqliteDbStrategy(ISqDatabase database, string databaseName, DbConnection connection) : base(database)
        {
            this._databaseName = databaseName;
            this._connection = connection;
        }

        public override string DefaultSchemaName => "dbo";

        public override async Task<DbRawModels> LoadRawModels()
        {
            var tables = await this.LoadTables();
            var columns = new List<ColumnRawModel>();
            var primaryKeys = new Dictionary<TableRef, PrimaryKeyModel>();
            var indexes = new Dictionary<TableRef, List<IndexModel>>();
            var foreignKeys = new Dictionary<ColumnRef, List<ColumnRef>>();

            foreach (var table in tables)
            {
                var tableRef = new TableRef(this.DefaultSchemaName, table.Name);
                var tableColumns = await this.LoadColumns(tableRef, table.CreateSql);
                columns.AddRange(tableColumns);

                var pkColumns = tableColumns
                    .Where(c => c.Extra is int pk && pk > 0)
                    .OrderBy(c => (int)c.Extra!)
                    .Select(c => new IndexColumnModel(false, c.DbName))
                    .ToList();
                if (pkColumns.Count > 0)
                {
                    primaryKeys[tableRef] = new PrimaryKeyModel(pkColumns, $"PK_{table.Name}");
                }

                indexes[tableRef] = await this.LoadIndexes(tableRef);
                await this.LoadForeignKeys(tableRef, foreignKeys);
            }

            return new DbRawModels(columns, new LoadIndexesResult(primaryKeys, indexes), foreignKeys);
        }

        public override ColumnType? TryGetColType(ColumnRawModel raw)
        {
            var type = raw.TypeName.ToUpperInvariant();

            if (type.IndexOf("BIGINT", StringComparison.Ordinal) >= 0)
            {
                return new Int64ColumnType(raw.Nullable);
            }

            if (type.IndexOf("SMALLINT", StringComparison.Ordinal) >= 0)
            {
                return new Int16ColumnType(raw.Nullable);
            }

            if (type.IndexOf("TINYINT", StringComparison.Ordinal) >= 0)
            {
                return new ByteColumnType(raw.Nullable);
            }

            if (type.IndexOf("INT", StringComparison.Ordinal) >= 0)
            {
                return new Int32ColumnType(raw.Nullable);
            }

            if (type.IndexOf("CHAR", StringComparison.Ordinal) >= 0 || type.IndexOf("CLOB", StringComparison.Ordinal) >= 0 || type.IndexOf("TEXT", StringComparison.Ordinal) >= 0)
            {
                return new StringColumnType(raw.Nullable, raw.Size, isFixed: false, isUnicode: true, isText: true);
            }

            if (type.IndexOf("BLOB", StringComparison.Ordinal) >= 0)
            {
                return new ByteArrayColumnType(raw.Nullable, raw.Size, isFixed: false);
            }

            if (type.IndexOf("REAL", StringComparison.Ordinal) >= 0 || type.IndexOf("FLOA", StringComparison.Ordinal) >= 0 || type.IndexOf("DOUB", StringComparison.Ordinal) >= 0)
            {
                return new DoubleColumnType(raw.Nullable);
            }

            if (type.IndexOf("DEC", StringComparison.Ordinal) >= 0 || type.IndexOf("NUM", StringComparison.Ordinal) >= 0)
            {
                return new DecimalColumnType(raw.Nullable, raw.Precision ?? 18, raw.Scale ?? 0);
            }

            if (type.IndexOf("BOOL", StringComparison.Ordinal) >= 0)
            {
                return new BooleanColumnType(raw.Nullable);
            }

            if (type.IndexOf("DATE", StringComparison.Ordinal) >= 0 || type.IndexOf("TIME", StringComparison.Ordinal) >= 0)
            {
                return new DateTimeColumnType(raw.Nullable, type.IndexOf("DATE", StringComparison.Ordinal) >= 0 && type.IndexOf("TIME", StringComparison.Ordinal) < 0);
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                return new StringColumnType(raw.Nullable, raw.Size, isFixed: false, isUnicode: true, isText: true);
            }

            return new StringColumnType(raw.Nullable, raw.Size, isFixed: false, isUnicode: true, isText: true);
        }

        public override DefaultValue? ParseDefaultValue(string? rawColumnDefaultValue, ColumnType columnType)
        {
            if (string.IsNullOrWhiteSpace(rawColumnDefaultValue))
            {
                return null;
            }

            var raw = rawColumnDefaultValue!;
            raw = raw.Trim();
            if (string.Equals(raw, "CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase))
            {
                return new DefaultValue(DefaultValueType.GetUtcDate, null);
            }

            if (raw.StartsWith("(") && raw.EndsWith(")"))
            {
                raw = raw.Substring(1, raw.Length - 2).Trim();
            }

            if (raw.Length >= 2 && raw[0] == '\'' && raw[raw.Length - 1] == '\'')
            {
                return new DefaultValue(DefaultValueType.String, raw.Substring(1, raw.Length - 2).Replace("''", "'"));
            }

            return columnType.Accept(DefaultValueParser.Instance, raw) ?? new DefaultValue(DefaultValueType.Raw, raw);
        }

        private async Task<List<(string Name, string CreateSql)>> LoadTables()
        {
            var result = new List<(string Name, string CreateSql)>();
            await this.WithOpenConnection(async () =>
            {
                using var cmd = this._connection.CreateCommand();
                cmd.CommandText = "SELECT name, sql FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add((reader.GetString(0), reader.IsDBNull(1) ? string.Empty : reader.GetString(1)));
                }
            });
            return result;
        }

        private async Task<List<ColumnRawModel>> LoadColumns(TableRef table, string createSql)
        {
            var result = new List<ColumnRawModel>();
            await this.WithOpenConnection(async () =>
            {
                using var cmd = this._connection.CreateCommand();
                cmd.CommandText = $"PRAGMA table_info({QuoteIdentifier(table.Name)})";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var name = reader.GetString(1);
                    var typeName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                    var notNull = reader.GetInt32(3) == 1;
                    var defaultValue = reader.IsDBNull(4) ? null : reader.GetString(4);
                    var pkPosition = reader.GetInt32(5);
                    var identity = pkPosition > 0 &&
                                   (createSql.IndexOf($"{name}\" INTEGER PRIMARY KEY", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    createSql.IndexOf($"{name} INTEGER PRIMARY KEY", StringComparison.OrdinalIgnoreCase) >= 0);
                    var (size, precision, scale) = ParseTypeShape(typeName);

                    result.Add(new ColumnRawModel(
                        new ColumnRef(table.Schema, table.Name, name),
                        ordinalPosition: reader.GetInt32(0),
                        identity: identity,
                        nullable: pkPosition > 0 ? false : !notNull,
                        typeName: typeName,
                        defaultValue: defaultValue,
                        size: size,
                        precision: precision,
                        scale: scale,
                        extra: pkPosition > 0 ? pkPosition : null
                    ));
                }
            });
            return result;
        }

        private async Task<List<IndexModel>> LoadIndexes(TableRef table)
        {
            var result = new List<IndexModel>();
            await this.WithOpenConnection(async () =>
            {
                using var cmd = this._connection.CreateCommand();
                cmd.CommandText = $"PRAGMA index_list({QuoteIdentifier(table.Name)})";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var indexName = reader.GetString(1);
                    var isUnique = reader.GetInt32(2) == 1;
                    var origin = reader.IsDBNull(3) ? null : reader.GetString(3);
                    if (string.Equals(origin, "pk", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var columns = await this.LoadIndexColumns(table, indexName);
                    result.Add(new IndexModel(columns, indexName, isUnique, isClustered: false));
                }
            });
            return result;
        }

        private async Task<List<IndexColumnModel>> LoadIndexColumns(TableRef table, string indexName)
        {
            var result = new List<IndexColumnModel>();
            string? createSql = null;
            await this.WithOpenConnection(async () =>
            {
                using (var indexCommand = this._connection.CreateCommand())
                {
                    indexCommand.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = $name";
                    AddParameter(indexCommand, "$name", indexName);
                    var scalar = await indexCommand.ExecuteScalarAsync();
                    createSql = scalar as string;
                }

                using var cmd = this._connection.CreateCommand();
                cmd.CommandText = $"PRAGMA index_info({QuoteIdentifier(indexName)})";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var columnName = reader.GetString(2);
                    result.Add(new IndexColumnModel(
                        IsDescendingIndexColumn(createSql, columnName),
                        new ColumnRef(table.Schema, table.Name, columnName)));
                }
            });
            return result;
        }

        private async Task LoadForeignKeys(TableRef table, Dictionary<ColumnRef, List<ColumnRef>> foreignKeys)
        {
            await this.WithOpenConnection(async () =>
            {
                using var cmd = this._connection.CreateCommand();
                cmd.CommandText = $"PRAGMA foreign_key_list({QuoteIdentifier(table.Name)})";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var from = new ColumnRef(table.Schema, table.Name, reader.GetString(3));
                    var to = new ColumnRef(this.DefaultSchemaName, reader.GetString(2), reader.GetString(4));

                    if (!foreignKeys.TryGetValue(from, out var refs))
                    {
                        refs = new List<ColumnRef>();
                        foreignKeys[from] = refs;
                    }

                    refs.Add(to);
                }
            });
        }

        private async Task WithOpenConnection(Func<Task> action)
        {
            var shouldClose = this._connection.State == ConnectionState.Closed;
            if (shouldClose)
            {
                await this._connection.OpenAsync();
            }

            try
            {
                await action();
            }
            finally
            {
                if (shouldClose)
                {
#if NETSTANDARD
                    this._connection.Close();
#else
                    await this._connection.CloseAsync();
#endif
                }
            }
        }

        private static string QuoteIdentifier(string name)
        {
            return $"\"{name.Replace("\"", "\"\"")}\"";
        }

        private static void AddParameter(DbCommand command, string name, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        private static bool IsDescendingIndexColumn(string? createSql, string columnName)
        {
            if (string.IsNullOrWhiteSpace(createSql))
            {
                return false;
            }

            var quotedColumnName = QuoteIdentifier(columnName);
            var marker = createSql!.IndexOf(quotedColumnName, StringComparison.OrdinalIgnoreCase);
            var markerLength = quotedColumnName.Length;
            if (marker < 0)
            {
                marker = createSql.IndexOf(columnName, StringComparison.OrdinalIgnoreCase);
                markerLength = columnName.Length;
                if (marker < 0)
                {
                    return false;
                }
            }

            var segmentStart = marker + markerLength;
            var segmentEnd = createSql.IndexOf(',', segmentStart);
            if (segmentEnd < 0)
            {
                segmentEnd = createSql.IndexOf(')', segmentStart);
            }

            if (segmentEnd < 0)
            {
                segmentEnd = createSql.Length;
            }

            var segment = createSql.Substring(segmentStart, segmentEnd - segmentStart);
            return segment.IndexOf("DESC", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static (int? Size, int? Precision, int? Scale) ParseTypeShape(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return (null, null, null);
            }

            var rawTypeName = typeName!;

            var openParen = rawTypeName.IndexOf('(');
            if (openParen < 0)
            {
                return (null, null, null);
            }

            var closeParen = rawTypeName.IndexOf(')', openParen + 1);
            if (closeParen < 0)
            {
                return (null, null, null);
            }

            var parts = rawTypeName.Substring(openParen + 1, closeParen - openParen - 1)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            if (parts.Length == 1 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var size))
            {
                return (size, size, null);
            }

            if (parts.Length == 2 &&
                int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var precision) &&
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var scale))
            {
                return (null, precision, scale);
            }

            return (null, null, null);
        }

        private class DefaultValueParser : IColumnTypeVisitor<DefaultValue?, string>
        {
            public static readonly DefaultValueParser Instance = new DefaultValueParser();

            public DefaultValue? VisitBooleanColumnType(BooleanColumnType booleanColumnType, string defaultValueRaw)
                => defaultValueRaw == "0" || defaultValueRaw == "1" || bool.TryParse(defaultValueRaw, out _)
                    ? new DefaultValue(DefaultValueType.Bool, defaultValueRaw)
                    : null;

            public DefaultValue? VisitByteColumnType(ByteColumnType byteColumnType, string defaultValueRaw)
                => int.TryParse(defaultValueRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                    ? new DefaultValue(DefaultValueType.Integer, defaultValueRaw)
                    : null;

            public DefaultValue? VisitByteArrayColumnType(ByteArrayColumnType byteArrayColumnType, string defaultValueRaw) => null;

            public DefaultValue? VisitInt16ColumnType(Int16ColumnType int16ColumnType, string defaultValueRaw)
                => int.TryParse(defaultValueRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                    ? new DefaultValue(DefaultValueType.Integer, defaultValueRaw)
                    : null;

            public DefaultValue? VisitInt32ColumnType(Int32ColumnType int32ColumnType, string defaultValueRaw)
                => int.TryParse(defaultValueRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                    ? new DefaultValue(DefaultValueType.Integer, defaultValueRaw)
                    : null;

            public DefaultValue? VisitInt64ColumnType(Int64ColumnType int64ColumnType, string defaultValueRaw)
                => long.TryParse(defaultValueRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                    ? new DefaultValue(DefaultValueType.Integer, defaultValueRaw)
                    : null;

            public DefaultValue? VisitDoubleColumnType(DoubleColumnType doubleColumnType, string defaultValueRaw)
                => double.TryParse(defaultValueRaw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _)
                    ? new DefaultValue(DefaultValueType.Raw, defaultValueRaw)
                    : null;

            public DefaultValue? VisitDecimalColumnType(DecimalColumnType decimalColumnType, string defaultValueRaw)
                => decimal.TryParse(defaultValueRaw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _)
                    ? new DefaultValue(DefaultValueType.Raw, defaultValueRaw)
                    : null;

            public DefaultValue? VisitDateTimeColumnType(DateTimeColumnType dateTimeColumnType, string defaultValueRaw) => null;
            public DefaultValue? VisitDateTimeOffsetColumnType(DateTimeOffsetColumnType dateTimeColumnType, string defaultValueRaw) => null;
            public DefaultValue? VisitStringColumnType(StringColumnType stringColumnType, string defaultValueRaw) => new DefaultValue(DefaultValueType.String, defaultValueRaw);
            public DefaultValue? VisitGuidColumnType(GuidColumnType guidColumnType, string defaultValueRaw) => null;
            public DefaultValue? VisitXmlColumnType(XmlColumnType xmlColumnType, string defaultValueRaw) => null;
        }
    }
}
