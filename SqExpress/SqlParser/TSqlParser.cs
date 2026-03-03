using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SqExpress.DbMetadata;
using SqExpress.SqlParser.Internal.Mapping;
using SqExpress.SqlParser.Internal.Parsing;
using SqExpress.Syntax.Names;
using SqExpress.Syntax;

namespace SqExpress.SqlParser
{
    public static class TSqlParser
    {
        public static IExpr Parse(string sql, IReadOnlyList<TableBase> existingTables)
        {
            if (TryParse(sql, existingTables, out IExpr? expr, out var error))
            {
                return expr!;
            }

            throw new SqExpressTSqlParserException(error ?? "Could not parse SQL.");
        }

        public static bool TryParse(
            string sql,
            IReadOnlyList<TableBase> existingTables,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(false)] out string? error)
        {
            if (existingTables == null)
            {
                throw new ArgumentNullException(nameof(existingTables));
            }

            if (TryParseCore(sql, out result, out var tables, out var errors))
            {
                if (!TryValidateParsedTables(existingTables, tables!, out error))
                {
                    result = null;
                    return false;
                }

                error = null;
                return true;
            }

            result = null;
            error = string.Join(Environment.NewLine, errors);
            return false;
        }

        public static bool TryParse(
            string sql,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(false)] out string? error)
            => TryParse(sql, out result, out _, out error);

        public static bool TryParse(
            string sql,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(true)] out IReadOnlyList<SqTable>? tables,
            [NotNullWhen(false)] out string? error)
        {
            if (TryParseCore(sql, out result, out tables, out var errors))
            {
                error = null;
                return true;
            }

            result = null;
            error = string.Join(Environment.NewLine, errors);
            return false;
        }

        private static bool TryParseCore(
            string sql,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(true)] out IReadOnlyList<SqTable>? tables,
            [NotNullWhen(false)] out IReadOnlyList<string>? errors)
        {
            if (!SqlDomParser.TryParseSingleStatement(sql, out var statement, out errors))
            {
                result = null;
                tables = null;
                return false;
            }

            var extractedTables = SqlDomTableArtifactExtractor.ExtractTables(statement!);

            if (SqlDomToSqExprMapper.TryMap(statement!, out result, out _, out var mappingError))
            {
                tables = extractedTables;
                errors = null;
                return true;
            }

            result = null;
            tables = extractedTables;
            errors = new[] { mappingError ?? "Could not map SQL DOM to SqExpress AST." };
            return false;
        }

        private static bool TryValidateParsedTables(
            IReadOnlyList<TableBase> existingTables,
            IReadOnlyList<SqTable> parsedTables,
            [NotNullWhen(false)] out string? error)
        {
            var expectedByTable = new Dictionary<string, TableBase>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in existingTables)
            {
                expectedByTable[BuildTableKey(table.FullName)] = table;
            }

            var parsedByTable = new Dictionary<string, SqTable>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in parsedTables)
            {
                parsedByTable[BuildTableKey(table.FullName)] = table;
            }

            var missingTables = expectedByTable.Keys
                .Where(i => !parsedByTable.ContainsKey(i))
                .Select(i => FormatTableName(expectedByTable[i].FullName))
                .OrderBy(i => i, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var extraTables = parsedByTable.Keys
                .Where(i => !expectedByTable.ContainsKey(i))
                .Select(i => FormatTableName(parsedByTable[i].FullName))
                .OrderBy(i => i, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var tableDifferences = new List<string>();
            foreach (var tableKey in expectedByTable.Keys.Where(parsedByTable.ContainsKey).OrderBy(i => i, StringComparer.OrdinalIgnoreCase))
            {
                var expected = expectedByTable[tableKey];
                var parsed = parsedByTable[tableKey];
                var tableDiff = BuildTableDifferenceMessage(expected, parsed);
                if (!string.IsNullOrEmpty(tableDiff))
                {
                    tableDifferences.Add(tableDiff!);
                }
            }

            if (missingTables.Count < 1 && extraTables.Count < 1 && tableDifferences.Count < 1)
            {
                error = null;
                return true;
            }

            var parts = new List<string>
            {
                "Parsed SQL table artifacts do not match provided existing tables."
            };

            if (missingTables.Count > 0)
            {
                parts.Add("Missing tables: " + string.Join(", ", missingTables));
            }

            if (extraTables.Count > 0)
            {
                parts.Add("Unexpected tables: " + string.Join(", ", extraTables));
            }

            if (tableDifferences.Count > 0)
            {
                parts.Add("Table differences: " + string.Join("; ", tableDifferences));
            }

            error = string.Join(Environment.NewLine, parts);
            return false;
        }

        private static string? BuildTableDifferenceMessage(TableBase expected, SqTable parsed)
        {
            var expectedColumns = expected.Columns.ToDictionary(i => i.ColumnName.Name, StringComparer.OrdinalIgnoreCase);
            var parsedColumns = parsed.Columns.ToDictionary(i => i.ColumnName.Name, StringComparer.OrdinalIgnoreCase);

            var missingColumns = expectedColumns.Keys
                .Where(i => !parsedColumns.ContainsKey(i))
                .OrderBy(i => i, StringComparer.OrdinalIgnoreCase)
                .Select(i => $"[{i}]")
                .ToList();

            var extraColumns = parsedColumns.Keys
                .Where(i => !expectedColumns.ContainsKey(i))
                .OrderBy(i => i, StringComparer.OrdinalIgnoreCase)
                .Select(i => $"[{i}]")
                .ToList();

            var changedColumns = new List<string>();
            foreach (var columnKey in expectedColumns.Keys.Where(parsedColumns.ContainsKey).OrderBy(i => i, StringComparer.OrdinalIgnoreCase))
            {
                var comparison = expectedColumns[columnKey].CompareWith(parsedColumns[columnKey]);
                if (comparison != TableColumnComparison.Equal)
                {
                    changedColumns.Add($"[{columnKey}] ({comparison})");
                }
            }

            if (missingColumns.Count < 1 && extraColumns.Count < 1 && changedColumns.Count < 1)
            {
                return null;
            }

            var parts = new List<string>
            {
                FormatTableName(expected.FullName)
            };

            if (missingColumns.Count > 0)
            {
                parts.Add("missing columns: " + string.Join(", ", missingColumns));
            }

            if (extraColumns.Count > 0)
            {
                parts.Add("extra columns: " + string.Join(", ", extraColumns));
            }

            if (changedColumns.Count > 0)
            {
                parts.Add("changed columns: " + string.Join(", ", changedColumns));
            }

            return string.Join(", ", parts);
        }

        private static string BuildTableKey(IExprTableFullName fullName)
        {
            var table = fullName.AsExprTableFullName();
            var schema = table.DbSchema?.Schema.Name ?? "dbo";
            return schema + "." + table.TableName.Name;
        }

        private static string FormatTableName(IExprTableFullName fullName)
        {
            var table = fullName.AsExprTableFullName();
            var schema = table.DbSchema?.Schema.Name ?? "dbo";
            return $"[{schema}].[{table.TableName.Name}]";
        }
    }
}

