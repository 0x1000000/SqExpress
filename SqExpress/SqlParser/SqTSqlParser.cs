using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SqExpress;
using SqExpress.DbMetadata;
using SqExpress.SqlParser.Internal.Mapping;
using SqExpress.SqlParser.Internal.Parsing;
using SqExpress.Syntax;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Update;

namespace SqExpress.SqlParser
{
    public static class SqTSqlParser
    {
        private static readonly SqTSqlParserOptions DefaultOptions = new SqTSqlParserOptions();

        public static IExpr Parse(string sql, IReadOnlyList<TableBase> existingTables)
            => Parse(sql, existingTables, options: null);

        public static IExpr Parse(string sql, IReadOnlyList<TableBase> existingTables, SqTSqlParserOptions? options)
        {
            if (TryParse(sql, existingTables, options, out IExpr? expr, out var error))
            {
                return expr;
            }

            throw new SqExpressTSqlParserException(error ?? "Could not parse SQL.");
        }

        public static bool TryParse(
            string sql,
            IReadOnlyList<TableBase> existingTables,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(false)] out string? error)
            => TryParse(sql, existingTables, options: null, out result, out error);

        public static bool TryParse(
            string sql,
            IReadOnlyList<TableBase> existingTables,
            SqTSqlParserOptions? options,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(false)] out string? error)
        {
            if (existingTables == null)
            {
                throw new ArgumentNullException(nameof(existingTables));
            }

            var effectiveOptions = NormalizeOptions(options);
            if (TryParseCore(sql, effectiveOptions, out result, out var tables, out var errors))
            {
                if (!TryValidateParsedTables(existingTables, tables!, effectiveOptions.DefaultSchema, out error))
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
            => TryParse(sql, options: null, out result, out error);

        public static bool TryParse(
            string sql,
            SqTSqlParserOptions? options,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(false)] out string? error)
            => TryParse(sql, options, out result, out _, out error);

        public static bool TryParse(
            string sql,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(true)] out IReadOnlyList<SqTable>? tables,
            [NotNullWhen(false)] out string? error)
            => TryParse(sql, options: null, out result, out tables, out error);

        public static bool TryParse(
            string sql,
            SqTSqlParserOptions? options,
            [NotNullWhen(true)] out IExpr? result,
            [NotNullWhen(true)] out IReadOnlyList<SqTable>? tables,
            [NotNullWhen(false)] out string? error)
        {
            if (TryParseCore(sql, NormalizeOptions(options), out result, out tables, out var errors))
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
            SqTSqlParserOptions options,
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

            var extractedTables = SqlDomTableArtifactExtractor.ExtractTables(statement!, options.DefaultSchema);

            if (SqlDomToSqExprMapper.TryMap(statement!, options.DefaultSchema, out result, out _, out var mappingError))
            {
                if (extractedTables.Count < 1 && result is ExprUpdate update)
                {
                    extractedTables = EnsureUpdateTargetTable(update, options.DefaultSchema);
                }

                tables = extractedTables;
                errors = null;
                return true;
            }

            result = null;
            tables = extractedTables;
            errors = new[] { mappingError ?? "Could not map SQL DOM to SqExpress AST." };
            return false;
        }

        private static SqTSqlParserOptions NormalizeOptions(SqTSqlParserOptions? options)
            => options ?? DefaultOptions;

        private static IReadOnlyList<SqTable> EnsureUpdateTargetTable(ExprUpdate update, string? defaultSchema)
        {
            var fullName = update.Target.FullName.AsExprTableFullName();
            var schema = fullName.DbSchema?.Schema.Name ?? defaultSchema;
            var table = fullName.TableName.Name;
            return new[] { SqTable.Create(schema, table, a => a) };
        }

        private static bool TryValidateParsedTables(
            IReadOnlyList<TableBase> existingTables,
            IReadOnlyList<SqTable> parsedTables,
            string? defaultSchema,
            [NotNullWhen(false)] out string? error)
        {
            if (parsedTables.Count < 1)
            {
                error = null;
                return true;
            }

            var parsedAsBaseTables = parsedTables.Cast<TableBase>().ToList();
            var comparison = parsedAsBaseTables.CompareWith(existingTables, i => BuildTableComparisonKey(i, defaultSchema));
            if (comparison == null)
            {
                error = null;
                return true;
            }

            var unexpectedTables = comparison.MissedTables
                .Select(i => FormatTableName(i.FullName, defaultSchema))
                .OrderBy(i => i, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var tableDifferences = new List<string>();
            foreach (var tableDifference in comparison.DifferentTables.OrderBy(i => BuildTableComparisonKey(i.Table.FullName, defaultSchema), StringComparer.Ordinal))
            {
                var tableDiff = BuildTableDifferenceMessage(tableDifference.Table, tableDifference.TableComparison, defaultSchema);
                if (!string.IsNullOrEmpty(tableDiff))
                {
                    tableDifferences.Add(tableDiff!);
                }
            }

            if (unexpectedTables.Count < 1 && tableDifferences.Count < 1)
            {
                error = null;
                return true;
            }

            var parts = new List<string>
            {
                "Parsed SQL table artifacts do not match provided existing tables."
            };

            if (unexpectedTables.Count > 0)
            {
                parts.Add("Unexpected tables: " + string.Join(", ", unexpectedTables));
            }

            if (tableDifferences.Count > 0)
            {
                parts.Add("Table differences: " + string.Join("; ", tableDifferences));
            }

            error = string.Join(Environment.NewLine, parts);
            return false;
        }

        private static string? BuildTableDifferenceMessage(TableBase expected, TableComparison comparison, string? defaultSchema)
        {
            var parsedOnlyColumns = comparison.MissedColumns.ToList();
            var providedOnlyColumns = comparison.ExtraColumns.ToList();

            var changedColumns = new List<string>();
            foreach (var differentColumn in comparison.DifferentColumns.OrderBy(i => i.Column.ColumnName.Name, StringComparer.OrdinalIgnoreCase))
            {
                // Parser type inference is heuristic, therefore type/nullability/meta mismatches are intentionally ignored here.
                var relevantComparison = differentColumn.ColumnComparison & TableColumnComparison.DifferentName;
                if (relevantComparison != TableColumnComparison.Equal)
                {
                    changedColumns.Add($"[{differentColumn.Column.ColumnName.Name}] ({relevantComparison})");
                }
            }

            var matchedProvidedOnlyIndexes = new HashSet<int>();
            var matchedParsedOnlyIndexes = new HashSet<int>();
            for (var m = 0; m < parsedOnlyColumns.Count; m++)
            {
                var parsedOnly = parsedOnlyColumns[m];
                var parsedOnlyLower = parsedOnly.ColumnName.Name.ToLowerInvariant();
                for (var e = 0; e < providedOnlyColumns.Count; e++)
                {
                    if (matchedProvidedOnlyIndexes.Contains(e))
                    {
                        continue;
                    }

                    var providedOnly = providedOnlyColumns[e];
                    if (!string.Equals(providedOnly.ColumnName.Name.ToLowerInvariant(), parsedOnlyLower, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!string.Equals(providedOnly.ColumnName.Name, parsedOnly.ColumnName.Name, StringComparison.Ordinal))
                    {
                        changedColumns.Add($"[{parsedOnly.ColumnName.Name}] ({TableColumnComparison.DifferentName})");
                    }

                    matchedProvidedOnlyIndexes.Add(e);
                    matchedParsedOnlyIndexes.Add(m);
                    break;
                }
            }

            var extraColumns = parsedOnlyColumns
                .Where((_, i) => !matchedParsedOnlyIndexes.Contains(i))
                .Select(i => $"[{i.ColumnName.Name}]")
                .OrderBy(i => i, StringComparer.OrdinalIgnoreCase)
                .ToList();

            changedColumns = changedColumns
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(i => i, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (extraColumns.Count < 1 && changedColumns.Count < 1)
            {
                return null;
            }

            var parts = new List<string>
            {
                FormatTableName(expected.FullName, defaultSchema)
            };

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

        private static string BuildTableComparisonKey(IExprTableFullName fullName, string? defaultSchema)
        {
            var table = fullName.AsExprTableFullName();
            var schema = table.DbSchema?.Schema.Name ?? defaultSchema;
            return string.IsNullOrWhiteSpace(schema)
                ? table.TableName.Name.ToUpperInvariant()
                : (schema + "." + table.TableName.Name).ToUpperInvariant();
        }

        private static string FormatTableName(IExprTableFullName fullName, string? defaultSchema)
        {
            var table = fullName.AsExprTableFullName();
            var schema = table.DbSchema?.Schema.Name ?? defaultSchema;
            return string.IsNullOrWhiteSpace(schema)
                ? $"[{table.TableName.Name}]"
                : $"[{schema}].[{table.TableName.Name}]";
        }
    }
}
