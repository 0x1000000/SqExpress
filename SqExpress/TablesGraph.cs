using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;

namespace SqExpress
{
    public sealed class TablesGraph
    {
        private readonly IReadOnlyDictionary<string, TableBase> _tablesByKey;
        private readonly IReadOnlyDictionary<string, string> _parentByChildKey;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<TableBase>> _childrenByParentKey;

        private TablesGraph(
            IReadOnlyDictionary<string, TableBase> tablesByKey,
            IReadOnlyDictionary<string, string> parentByChildKey,
            IReadOnlyDictionary<string, IReadOnlyList<TableBase>> childrenByParentKey,
            IReadOnlyList<TableBase> roots)
        {
            this._tablesByKey = tablesByKey;
            this._parentByChildKey = parentByChildKey;
            this._childrenByParentKey = childrenByParentKey;
            this.Roots = roots;
        }

        public IReadOnlyList<TableBase> Roots { get; }

        public static TablesGraph Create(IReadOnlyList<TableBase> tables)
        {
            if (!TryCreate(tables, out var graph, out var error) || graph == null)
            {
                throw new SqExpressException(error ?? "TablesGraph could not be created.");
            }

            return graph;
        }

        public static bool TryCreate(
            IReadOnlyList<TableBase> tables,
            out TablesGraph? graph,
            out string? error)
        {
            graph = null;
            error = null;

            if (tables == null)
            {
                error = "Table list cannot be null.";
                return false;
            }

            var tablesByKey = new Dictionary<string, TableBase>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < tables.Count; i++)
            {
                var table = tables[i] ?? throw new SqExpressException("Table list cannot contain null.");
                var key = BuildTableKey(table.FullName);
                if (tablesByKey.ContainsKey(key))
                {
                    error = $"Duplicate table '{FormatTableName(table.FullName)}' in graph input.";
                    return false;
                }

                tablesByKey.Add(key, table);
            }

            var parentByChildKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var table in tables)
            {
                var childKey = BuildTableKey(table.FullName);
                string? parentKey = null;

                foreach (var column in table.Columns)
                {
                    var foreignKeyColumns = column.ColumnMeta?.ForeignKeyColumns;
                    if (foreignKeyColumns == null)
                    {
                        continue;
                    }

                    for (var i = 0; i < foreignKeyColumns.Count; i++)
                    {
                        var foreignKeyColumn = foreignKeyColumns[i];
                        var referencedTable = foreignKeyColumn.Table as TableBase;
                        if (referencedTable == null)
                        {
                            error = $"Foreign key on '{FormatTableName(table.FullName)}.{column.ColumnName.Name}' does not reference a TableBase.";
                            return false;
                        }

                        var referencedKey = BuildTableKey(referencedTable.FullName);
                        if (!tablesByKey.ContainsKey(referencedKey))
                        {
                            error =
                                $"Foreign key on '{FormatTableName(table.FullName)}.{column.ColumnName.Name}' references '{FormatTableName(referencedTable.FullName)}' which is not included in the graph.";
                            return false;
                        }

                        if (string.Equals(childKey, referencedKey, StringComparison.OrdinalIgnoreCase))
                        {
                            error = $"Table '{FormatTableName(table.FullName)}' cannot reference itself as parent.";
                            return false;
                        }

                        if (parentKey == null)
                        {
                            parentKey = referencedKey;
                            continue;
                        }

                        if (!string.Equals(parentKey, referencedKey, StringComparison.OrdinalIgnoreCase))
                        {
                            error =
                                $"Table '{FormatTableName(table.FullName)}' references more than one distinct parent table.";
                            return false;
                        }
                    }
                }

                if (parentKey != null)
                {
                    parentByChildKey[childKey] = parentKey;
                }
            }

            if (TryDetectCycle(parentByChildKey, tablesByKey, out error))
            {
                return false;
            }

            var childrenMap = new Dictionary<string, List<TableBase>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in parentByChildKey)
            {
                if (!childrenMap.TryGetValue(kv.Value, out var children))
                {
                    children = new List<TableBase>();
                    childrenMap[kv.Value] = children;
                }

                children.Add(tablesByKey[kv.Key]);
            }

            var readonlyChildrenMap = childrenMap.ToDictionary(
                i => i.Key,
                i => (IReadOnlyList<TableBase>)i.Value,
                StringComparer.OrdinalIgnoreCase);

            var roots = new List<TableBase>();
            foreach (var table in tables)
            {
                if (!parentByChildKey.ContainsKey(BuildTableKey(table.FullName)))
                {
                    roots.Add(table);
                }
            }

            graph = new TablesGraph(tablesByKey, parentByChildKey, readonlyChildrenMap, roots);
            return true;
        }

        public bool Contains(TableBase table)
        {
            if (table == null)
            {
                return false;
            }

            return this._tablesByKey.ContainsKey(BuildTableKey(table.FullName));
        }

        public bool IsParent(TableBase childCandidateTable, TableBase parentCandidateTable)
        {
            if (childCandidateTable == null || parentCandidateTable == null)
            {
                return false;
            }

            var childKey = BuildTableKey(childCandidateTable.FullName);
            var parentKey = BuildTableKey(parentCandidateTable.FullName);

            return this._parentByChildKey.TryGetValue(childKey, out var actualParentKey)
                   && string.Equals(actualParentKey, parentKey, StringComparison.OrdinalIgnoreCase);
        }

        public TableBase? FindCommonAncestor(TableBase table1, TableBase table2)
        {
            if (table1 == null || table2 == null)
            {
                return null;
            }

            if (!this.TryResolveTable(table1, out var canonicalTable1) || !this.TryResolveTable(table2, out var canonicalTable2))
            {
                return null;
            }

            var ancestors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = canonicalTable1;
            while (current != null)
            {
                ancestors.Add(BuildTableKey(current.FullName));
                current = this.GetParent(current);
            }

            current = canonicalTable2;
            while (current != null)
            {
                var key = BuildTableKey(current.FullName);
                if (ancestors.Contains(key))
                {
                    return current;
                }

                current = this.GetParent(current);
            }

            return null;
        }

        public bool TryToJoinTables(
            TableBase table1,
            TableBase table2,
            [NotNullWhen(true)] out IExprTableSource? join)
        {
            join = null;

            if (!this.TryResolveTable(table1, out var canonicalTable1) || !this.TryResolveTable(table2, out var canonicalTable2))
            {
                return false;
            }

            var commonAncestor = this.FindCommonAncestor(canonicalTable1, canonicalTable2);
            if (commonAncestor == null)
            {
                return false;
            }

            IExprTableSource source = commonAncestor;

            foreach (var descendant in this.GetPathFromAncestor(commonAncestor, canonicalTable1).Skip(1))
            {
                var parent = this.GetParent(descendant);
                if (parent == null)
                {
                    throw new SqExpressException($"Table '{FormatTableName(descendant.FullName)}' does not have a parent in the graph.");
                }

                source = new ExprJoinedTable(
                    source,
                    ExprJoinedTable.ExprJoinType.Inner,
                    descendant,
                    BuildJoinCondition(descendant, parent));
            }

            foreach (var descendant in this.GetPathFromAncestor(commonAncestor, canonicalTable2).Skip(1))
            {
                var parent = this.GetParent(descendant);
                if (parent == null)
                {
                    throw new SqExpressException($"Table '{FormatTableName(descendant.FullName)}' does not have a parent in the graph.");
                }

                source = new ExprJoinedTable(
                    source,
                    ExprJoinedTable.ExprJoinType.Inner,
                    descendant,
                    BuildJoinCondition(descendant, parent));
            }

            join = source;
            return true;
        }

        public TableBase? GetParent(TableBase table)
        {
            var canonical = this.ResolveTable(table);
            var childKey = BuildTableKey(canonical.FullName);
            if (!this._parentByChildKey.TryGetValue(childKey, out var parentKey))
            {
                return null;
            }

            return this._tablesByKey[parentKey];
        }

        public IEnumerable<TableBase> GetAncestors(TableBase table)
        {
            var current = this.GetParent(table);
            while (current != null)
            {
                yield return current;
                current = this.GetParent(current);
            }
        }

        public IReadOnlyList<TableBase> GetChildren(TableBase table)
        {
            var canonical = this.ResolveTable(table);
            var key = BuildTableKey(canonical.FullName);
            return this._childrenByParentKey.TryGetValue(key, out var children)
                ? children
                : Array.Empty<TableBase>();
        }

        public IEnumerable<TableBase> GetDescendants(TableBase table)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var descendant in this.GetDescendantsIterator(this.ResolveTable(table), visited))
            {
                yield return descendant;
            }
        }

        private IEnumerable<TableBase> GetDescendantsIterator(TableBase table, HashSet<string> visited)
        {
            foreach (var child in this.GetChildren(table))
            {
                var childKey = BuildTableKey(child.FullName);
                if (!visited.Add(childKey))
                {
                    continue;
                }

                yield return child;

                foreach (var descendant in this.GetDescendantsIterator(child, visited))
                {
                    yield return descendant;
                }
            }
        }

        private TableBase ResolveTable(TableBase table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            var key = BuildTableKey(table.FullName);
            if (!this._tablesByKey.TryGetValue(key, out var canonical))
            {
                throw new ArgumentException($"Table '{FormatTableName(table.FullName)}' does not belong to this graph.", nameof(table));
            }

            return canonical;
        }

        private bool TryResolveTable(TableBase table, [NotNullWhen(true)] out TableBase? canonical)
        {
            canonical = null;

            if (table == null)
            {
                return false;
            }

            return this._tablesByKey.TryGetValue(BuildTableKey(table.FullName), out canonical);
        }

        private IReadOnlyList<TableBase> GetPathFromAncestor(TableBase ancestor, TableBase descendant)
        {
            var path = new List<TableBase>();
            var current = descendant;

            while (current != null)
            {
                path.Add(current);
                if (string.Equals(BuildTableKey(current.FullName), BuildTableKey(ancestor.FullName), StringComparison.OrdinalIgnoreCase))
                {
                    path.Reverse();
                    return path;
                }

                current = this.GetParent(current);
            }

            throw new SqExpressException(
                $"Table '{FormatTableName(descendant.FullName)}' does not descend from '{FormatTableName(ancestor.FullName)}'.");
        }

        private static ExprBoolean BuildJoinCondition(TableBase child, TableBase parent)
        {
            ExprBoolean? result = null;

            foreach (var childColumn in child.Columns)
            {
                var foreignKeyColumns = childColumn.ColumnMeta?.ForeignKeyColumns;
                if (foreignKeyColumns == null)
                {
                    continue;
                }

                for (var i = 0; i < foreignKeyColumns.Count; i++)
                {
                    var referencedColumn = foreignKeyColumns[i];
                    if (!(referencedColumn.Table is TableBase referencedTable)
                        || !string.Equals(BuildTableKey(referencedTable.FullName), BuildTableKey(parent.FullName), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var parentColumn = parent.Columns.FirstOrDefault(c =>
                        string.Equals(c.ColumnName.Name, referencedColumn.ColumnName.Name, StringComparison.OrdinalIgnoreCase));

                    if (ReferenceEquals(parentColumn, null))
                    {
                        throw new SqExpressException(
                            $"Referenced column '{FormatTableName(parent.FullName)}.{referencedColumn.ColumnName.Name}' was not found.");
                    }

                    var condition = childColumn == parentColumn;
                    result = ReferenceEquals(result, null) ? condition : result & condition;
                }
            }

            if (ReferenceEquals(result, null))
            {
                throw new SqExpressException(
                    $"Table '{FormatTableName(child.FullName)}' does not have a foreign key to '{FormatTableName(parent.FullName)}'.");
            }

            return result;
        }

        private static bool TryDetectCycle(
            IReadOnlyDictionary<string, string> parentByChildKey,
            IReadOnlyDictionary<string, TableBase> tablesByKey,
            out string? error)
        {
            foreach (var childKey in parentByChildKey.Keys)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { childKey };
                var currentKey = childKey;

                while (parentByChildKey.TryGetValue(currentKey, out var parentKey))
                {
                    if (!seen.Add(parentKey))
                    {
                        error = $"Cycle detected involving table '{FormatTableName(tablesByKey[parentKey].FullName)}'.";
                        return true;
                    }

                    currentKey = parentKey;
                }
            }

            error = null;
            return false;
        }

        private static string BuildTableKey(IExprTableFullName fullName)
        {
            var table = fullName.AsExprTableFullName();
            return string.Join(
                "|",
                table.DbSchema?.Database?.Name ?? string.Empty,
                table.DbSchema?.Schema.Name ?? string.Empty,
                table.TableName.Name).ToUpperInvariant();
        }

        private static string FormatTableName(IExprTableFullName fullName)
        {
            var table = fullName.AsExprTableFullName();
            if (table.DbSchema?.Database != null)
            {
                return $"{table.DbSchema.Database.Name}.{table.DbSchema.Schema.Name}.{table.TableName.Name}";
            }

            if (table.DbSchema?.Schema != null)
            {
                return $"{table.DbSchema.Schema.Name}.{table.TableName.Name}";
            }

            return table.TableName.Name;
        }
    }
}
