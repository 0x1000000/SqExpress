using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.CodeGenUtil
{
    internal static class TableFilter
    {
        public static IReadOnlyList<TableModel> Apply(
            IReadOnlyList<TableModel> tables,
            IEnumerable<string> includes,
            IEnumerable<string> excludes)
        {
            var includePatterns = CompilePatterns(includes, "include");
            var excludePatterns = CompilePatterns(excludes, "exclude");

            var selected = tables.Where(table =>
                    (includePatterns.Count == 0 || includePatterns.Any(pattern => pattern.IsMatch(table))) &&
                    !excludePatterns.Any(pattern => pattern.IsMatch(table)))
                .ToList();

            var selectedTables = selected
                .Select(static table => QualifiedName(table.DbName))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return selected.Select(table => WithoutExternalForeignKeys(table, selectedTables)).ToList();
        }

        private static IReadOnlyList<TablePattern> CompilePatterns(IEnumerable<string> patterns, string optionName)
        {
            var result = new List<TablePattern>();
            foreach (var pattern in patterns)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    throw new SqExpressCodeGenException($"Table {optionName} pattern cannot be empty.");
                }

                result.Add(new TablePattern(pattern));
            }

            return result;
        }

        private static TableModel WithoutExternalForeignKeys(TableModel table, ISet<string> selectedTables)
        {
            var columns = table.Columns.Select(column =>
            {
                var foreignKeys = column.Fk?
                    .Where(foreignKey => selectedTables.Contains(QualifiedName(foreignKey.Table)))
                    .ToList();

                return new ColumnModel(
                    column.Name,
                    column.DbName,
                    column.OrdinalPosition,
                    column.ColumnType,
                    column.Pk,
                    column.Identity,
                    column.DefaultValue,
                    foreignKeys?.Count > 0 ? foreignKeys : null);
            }).ToList();

            return new TableModel(table.Name, table.DbName, columns, table.Indexes);
        }

        private static string QualifiedName(TableRef table) => $"{table.Schema}.{table.Name}";

        private sealed class TablePattern
        {
            private readonly Regex regex;
            private readonly bool qualified;

            public TablePattern(string pattern)
            {
                this.qualified = pattern.Contains('.');
                var regexPattern = Regex.Escape(pattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".");
                this.regex = new Regex(
                    "^" + regexPattern + "$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            public bool IsMatch(TableModel table) =>
                this.regex.IsMatch(this.qualified ? QualifiedName(table.DbName) : table.DbName.Name);
        }
    }
}
