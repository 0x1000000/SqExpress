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
                if (extractedTables.Count < 1 && result is ExprUpdate update)
                {
                    extractedTables = EnsureUpdateTargetTable(update);
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

        private static IReadOnlyList<SqTable> EnsureUpdateTargetTable(ExprUpdate update)
        {
            var fullName = update.Target.FullName.AsExprTableFullName();
            var schema = fullName.DbSchema?.Schema.Name ?? "dbo";
            var table = fullName.TableName.Name;
            return new[] { SqTable.Create(schema, table, a => a) };
        }

        private static bool TryValidateParsedTables(
            IReadOnlyList<TableBase> existingTables,
            IReadOnlyList<SqTable> parsedTables,
            [NotNullWhen(false)] out string? error)
        {
            var parsedAsBaseTables = parsedTables.Cast<TableBase>().ToList();
            var comparison = existingTables.CompareWith(parsedAsBaseTables, BuildTableComparisonKey);
            if (comparison == null)
            {
                error = null;
                return true;
            }

            var missingTables = comparison.MissedTables
                .Select(i => FormatTableName(i.FullName))
                .OrderBy(i => i, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var extraTables = comparison.ExtraTables
                .Select(i => FormatTableName(i.FullName))
                .OrderBy(i => i, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var tableDifferences = new List<string>();
            foreach (var tableDifference in comparison.DifferentTables.OrderBy(i => BuildTableComparisonKey(i.Table.FullName), StringComparer.Ordinal))
            {
                var tableDiff = BuildTableDifferenceMessage(tableDifference.Table, tableDifference.TableComparison);
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

        private static string? BuildTableDifferenceMessage(TableBase expected, TableComparison comparison)
        {
            var missingColumnsRaw = comparison.MissedColumns.ToList();
            var extraColumnsRaw = comparison.ExtraColumns.ToList();

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

            var usedExtraIndexes = new HashSet<int>();
            for (var m = 0; m < missingColumnsRaw.Count; m++)
            {
                var missing = missingColumnsRaw[m];
                var missingLower = missing.ColumnName.Name.ToLowerInvariant();
                for (var e = 0; e < extraColumnsRaw.Count; e++)
                {
                    if (usedExtraIndexes.Contains(e))
                    {
                        continue;
                    }

                    var extra = extraColumnsRaw[e];
                    if (!string.Equals(extra.ColumnName.Name.ToLowerInvariant(), missingLower, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!string.Equals(extra.ColumnName.Name, missing.ColumnName.Name, StringComparison.Ordinal))
                    {
                        changedColumns.Add($"[{missing.ColumnName.Name}] ({TableColumnComparison.DifferentName})");
                    }

                    usedExtraIndexes.Add(e);
                    break;
                }
            }

            var extraColumns = extraColumnsRaw
                .Where((_, i) => !usedExtraIndexes.Contains(i))
                .Select(i => $"[{i.ColumnName.Name}]")
                .OrderBy(i => i, StringComparer.OrdinalIgnoreCase)
                .ToList();

            changedColumns = changedColumns
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(i => i, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Parser table artifacts intentionally include only detected columns.
            // Unreferenced columns from provided table descriptors are ignored here.
            if (extraColumns.Count < 1 && changedColumns.Count < 1)
            {
                return null;
            }

            var parts = new List<string>
            {
                FormatTableName(expected.FullName)
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

        private static string BuildTableComparisonKey(IExprTableFullName fullName)
        {
            var table = fullName.AsExprTableFullName();
            var schema = table.DbSchema?.Schema.Name ?? "dbo";
            return (schema + "." + table.TableName.Name).ToUpperInvariant();
        }

        private static string FormatTableName(IExprTableFullName fullName)
        {
            var table = fullName.AsExprTableFullName();
            var schema = table.DbSchema?.Schema.Name ?? "dbo";
            return $"[{schema}].[{table.TableName.Name}]";
        }
    }
}
