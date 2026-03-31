using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax;
using SqExpress.Syntax.Select;

namespace SqExpress
{
    public sealed class TablesGraph
    {
        private readonly IReadOnlyDictionary<string, TableBase> _tablesByKey;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<TableBase>> _referencesBySourceKey;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<TableBase>> _referencedByTargetKey;

        private TablesGraph(
            IReadOnlyDictionary<string, TableBase> tablesByKey,
            IReadOnlyDictionary<string, IReadOnlyList<TableBase>> referencesBySourceKey,
            IReadOnlyDictionary<string, IReadOnlyList<TableBase>> referencedByTargetKey)
        {
            this._tablesByKey = tablesByKey;
            this._referencesBySourceKey = referencesBySourceKey;
            this._referencedByTargetKey = referencedByTargetKey;
        }

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
            [NotNullWhen(true)]out TablesGraph? graph,
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
            var orderedKeys = new List<string>(tables.Count);
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
                orderedKeys.Add(key);
            }

            var referencesBySource = new Dictionary<string, List<TableBase>>(StringComparer.OrdinalIgnoreCase);
            var referencedByTarget = new Dictionary<string, List<TableBase>>(StringComparer.OrdinalIgnoreCase);

            foreach (var table in tables)
            {
                var sourceKey = BuildTableKey(table.FullName);
                HashSet<string>? seenTargets = null;

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
                        if (!(foreignKeyColumn.Table is TableBase referencedTable))
                        {
                            continue;
                        }

                        var targetKey = BuildTableKey(referencedTable.FullName);
                        if (!tablesByKey.ContainsKey(targetKey))
                        {
                            error =
                                $"Foreign key on '{FormatTableName(table.FullName)}.{column.ColumnName.Name}' references '{FormatTableName(referencedTable.FullName)}' which is not included in the graph.";
                            return false;
                        }

                        seenTargets ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        if (!seenTargets.Add(targetKey))
                        {
                            continue;
                        }

                        AddLink(referencesBySource, sourceKey, tablesByKey[targetKey]);
                        AddLink(referencedByTarget, targetKey, table);
                    }
                }
            }

            if (TryDetectCycle(orderedKeys, referencesBySource, out error))
            {
                return false;
            }

            graph = new TablesGraph(
                tablesByKey,
                referencesBySource.ToDictionary(i => i.Key, i => (IReadOnlyList<TableBase>)i.Value, StringComparer.OrdinalIgnoreCase),
                referencedByTarget.ToDictionary(i => i.Key, i => (IReadOnlyList<TableBase>)i.Value, StringComparer.OrdinalIgnoreCase));
            return true;
        }

        public bool Contains(ExprTable table)
        {
            if (table == null)
            {
                return false;
            }

            return this._tablesByKey.ContainsKey(BuildTableKey(table.FullName));
        }

        public bool TryGetTable(ExprTable table, [NotNullWhen(true)] out TableBase? canonicalTable)
        {
            canonicalTable = null;
            if (table == null)
            {
                return false;
            }

            return this._tablesByKey.TryGetValue(BuildTableKey(table.FullName), out canonicalTable);
        }

        public bool References(ExprTable table, ExprTable referencedCandidateTable, bool includeSelfRef = false)
        {
            if (table == null || referencedCandidateTable == null)
            {
                return false;
            }

            if (!this._tablesByKey.TryGetValue(BuildTableKey(table.FullName), out var canonical))
            {
                return false;
            }

            var candidateKey = BuildTableKey(referencedCandidateTable.FullName);
            return this.GetReferences(canonical, includeSelfRef).Any(i => string.Equals(BuildTableKey(i.FullName), candidateKey, StringComparison.OrdinalIgnoreCase));
        }

        public IReadOnlyList<TableBase> GetReferences(ExprTable table, bool includeSelfRef = false)
        {
            var canonical = this.ResolveTable(table);
            var key = BuildTableKey(canonical.FullName);
            if (!this._referencesBySourceKey.TryGetValue(key, out var references))
            {
                return Array.Empty<TableBase>();
            }

            return includeSelfRef ? references : FilterSelfReference(canonical, references);
        }

        public IEnumerable<TableBase> GetAllReferences(ExprTable table, bool includeSelfRef = false)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var reference in this.GetAllReferencesIterator(this.ResolveTable(table), visited, includeSelfRef))
            {
                yield return reference;
            }
        }

        public IReadOnlyList<TableBase> GetReferencedBy(ExprTable table, bool includeSelfRef = false)
        {
            var canonical = this.ResolveTable(table);
            var key = BuildTableKey(canonical.FullName);
            if (!this._referencedByTargetKey.TryGetValue(key, out var referencedBy))
            {
                return Array.Empty<TableBase>();
            }

            return includeSelfRef ? referencedBy : FilterSelfReference(canonical, referencedBy);
        }

        public IEnumerable<TableBase> GetAllReferencedBy(ExprTable table, bool includeSelfRef = false)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var referencedBy in this.GetAllReferencedByIterator(this.ResolveTable(table), visited, includeSelfRef))
            {
                yield return referencedBy;
            }
        }

        public bool TryToJoinTables(
            ExprTable table1,
            ExprTable table2,
            [NotNullWhen(true)] out IExprTableSource? join)
            => this.TryToJoinTables(table1, table2, intermediateTables: null, out join);

        public bool TryToJoinTables(
            ExprTable table1,
            ExprTable table2,
            IReadOnlyList<ExprTable>? intermediateTables,
            [NotNullWhen(true)] out IExprTableSource? join)
        {
            join = null;

            if (!this.TryResolveTable(table1, out var canonicalTable1) || !this.TryResolveTable(table2, out var canonicalTable2))
            {
                return false;
            }

            if (string.Equals(BuildTableKey(canonicalTable1.FullName), BuildTableKey(canonicalTable2.FullName), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var path = this.TryBuildPath(table1, canonicalTable1, table2, canonicalTable2, intermediateTables);
            if (path == null || path.Count == 0)
            {
                return false;
            }

            var previous = path[0];
            IExprTableSource source = previous.ActualTable;
            for (var i = 1; i < path.Count; i++)
            {
                var current = path[i];
                source = new ExprJoinedTable(
                    source,
                    ExprJoinedTable.ExprJoinType.Inner,
                    current.ActualTable,
                    BuildJoinCondition(previous.CanonicalTable, current.CanonicalTable, previous.ActualTable, current.ActualTable));
                previous = current;
            }

            join = source;
            return true;
        }

        private List<PathItem>? TryBuildPath(
            ExprTable sourceActual,
            TableBase sourceCanonical,
            ExprTable targetActual,
            TableBase targetCanonical,
            IReadOnlyList<ExprTable>? intermediateTables)
        {
            if (intermediateTables == null || intermediateTables.Count == 0)
            {
                var directPath = this.TryFindShortestPath(sourceCanonical, targetCanonical);
                return directPath == null ? null : BuildActualPath(directPath, sourceActual, targetActual);
            }

            var fullPath = new List<PathItem>();
            var segmentSourceCanonical = sourceCanonical;
            var segmentSourceActual = sourceActual;

            for (var i = 0; i <= intermediateTables.Count; i++)
            {
                var segmentTargetActual = i < intermediateTables.Count ? intermediateTables[i] : targetActual;
                if (!this.TryResolveTable(segmentTargetActual, out var segmentTargetCanonical))
                {
                    return null;
                }

                var segmentPath = this.TryFindShortestPath(segmentSourceCanonical, segmentTargetCanonical);
                if (segmentPath == null || segmentPath.Count == 0)
                {
                    return null;
                }

                var actualSegmentPath = BuildActualPath(segmentPath, segmentSourceActual, segmentTargetActual);
                if (fullPath.Count == 0)
                {
                    fullPath.AddRange(actualSegmentPath);
                }
                else
                {
                    fullPath.AddRange(actualSegmentPath.Skip(1));
                }

                segmentSourceCanonical = segmentTargetCanonical;
                segmentSourceActual = segmentTargetActual;
            }

            return fullPath;
        }

        private static List<PathItem> BuildActualPath(
            IReadOnlyList<TableBase> canonicalPath,
            ExprTable firstActual,
            ExprTable lastActual)
        {
            var result = new List<PathItem>(canonicalPath.Count);
            for (var i = 0; i < canonicalPath.Count; i++)
            {
                var canonicalTable = canonicalPath[i];
                ExprTable actualTable;
                if (i == 0)
                {
                    actualTable = firstActual;
                }
                else if (i == canonicalPath.Count - 1)
                {
                    actualTable = lastActual;
                }
                else
                {
                    actualTable = canonicalTable.WithAlias(SqQueryBuilder.TableAlias());
                }

                result.Add(new PathItem(canonicalTable, actualTable));
            }

            return result;
        }

        private IEnumerable<TableBase> GetAllReferencesIterator(TableBase table, HashSet<string> visited, bool includeSelfRef)
        {
            foreach (var reference in this.GetReferences(table, includeSelfRef))
            {
                var key = BuildTableKey(reference.FullName);
                if (!visited.Add(key))
                {
                    continue;
                }

                yield return reference;

                foreach (var nested in this.GetAllReferencesIterator(reference, visited, includeSelfRef))
                {
                    yield return nested;
                }
            }
        }

        private IEnumerable<TableBase> GetAllReferencedByIterator(TableBase table, HashSet<string> visited, bool includeSelfRef)
        {
            foreach (var referencedBy in this.GetReferencedBy(table, includeSelfRef))
            {
                var key = BuildTableKey(referencedBy.FullName);
                if (!visited.Add(key))
                {
                    continue;
                }

                yield return referencedBy;

                foreach (var nested in this.GetAllReferencedByIterator(referencedBy, visited, includeSelfRef))
                {
                    yield return nested;
                }
            }
        }

        private List<TableBase>? TryFindShortestPath(TableBase source, TableBase target)
        {
            var sourceKey = BuildTableKey(source.FullName);
            var targetKey = BuildTableKey(target.FullName);

            var queue = new Queue<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { sourceKey };
            var previous = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                [sourceKey] = null
            };

            queue.Enqueue(sourceKey);

            while (queue.Count > 0)
            {
                var currentKey = queue.Dequeue();
                if (string.Equals(currentKey, targetKey, StringComparison.OrdinalIgnoreCase))
                {
                    return ReconstructPath(previous, targetKey, this._tablesByKey);
                }

                foreach (var neighbor in this.GetAdjacentTables(this._tablesByKey[currentKey]))
                {
                    var neighborKey = BuildTableKey(neighbor.FullName);
                    if (!visited.Add(neighborKey))
                    {
                        continue;
                    }

                    previous[neighborKey] = currentKey;
                    queue.Enqueue(neighborKey);
                }
            }

            return null;
        }

        private IEnumerable<TableBase> GetAdjacentTables(TableBase table)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var reference in this.GetReferences(table))
            {
                var key = BuildTableKey(reference.FullName);
                if (seen.Add(key))
                {
                    yield return reference;
                }
            }

            foreach (var referencedBy in this.GetReferencedBy(table))
            {
                var key = BuildTableKey(referencedBy.FullName);
                if (seen.Add(key))
                {
                    yield return referencedBy;
                }
            }
        }

        private static List<TableBase> ReconstructPath(
            IReadOnlyDictionary<string, string?> previous,
            string targetKey,
            IReadOnlyDictionary<string, TableBase> tablesByKey)
        {
            var path = new List<TableBase>();
            string? currentKey = targetKey;
            while (currentKey != null)
            {
                path.Add(tablesByKey[currentKey]);
                currentKey = previous[currentKey];
            }

            path.Reverse();
            return path;
        }

        private TableBase ResolveTable(ExprTable table)
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

        private bool TryResolveTable(ExprTable table, [NotNullWhen(true)] out TableBase? canonical)
        {
            canonical = null;
            if (table == null)
            {
                return false;
            }

            return this._tablesByKey.TryGetValue(BuildTableKey(table.FullName), out canonical);
        }

        private static ExprBoolean BuildJoinCondition(TableBase left, TableBase right, ExprTable actualLeft, ExprTable actualRight)
        {
            var condition = TryBuildJoinCondition(left, right, actualLeft, actualRight);
            if (!ReferenceEquals(condition, null))
            {
                return condition;
            }

            condition = TryBuildJoinCondition(right, left, actualRight, actualLeft);
            if (!ReferenceEquals(condition, null))
            {
                return condition;
            }

            throw new SqExpressException(
                $"No foreign key join condition was found between '{FormatTableName(left.FullName)}' and '{FormatTableName(right.FullName)}'.");
        }

        private static ExprBoolean? TryBuildJoinCondition(TableBase child, TableBase referenced, ExprTable actualChild, ExprTable actualReferenced)
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
                        || !string.Equals(BuildTableKey(referencedTable.FullName), BuildTableKey(referenced.FullName), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var referencedTableColumn = referenced.Columns.FirstOrDefault(c =>
                        string.Equals(c.ColumnName.Name, referencedColumn.ColumnName.Name, StringComparison.OrdinalIgnoreCase));

                    if (ReferenceEquals(referencedTableColumn, null))
                    {
                        throw new SqExpressException(
                            $"Referenced column '{FormatTableName(referenced.FullName)}.{referencedColumn.ColumnName.Name}' was not found.");
                    }

                    var condition =
                        RetargetColumn(childColumn, actualChild)
                        == RetargetColumn(referencedTableColumn, actualReferenced);
                    result = ReferenceEquals(result, null) ? condition : result & condition;
                }
            }

            return result;
        }

        private static TableColumn RetargetColumn(TableColumn column, ExprTable actualTable)
            => column
                .WithTable(actualTable)
                .WithSource(actualTable.Alias);

        private readonly struct PathItem
        {
            public PathItem(TableBase canonicalTable, ExprTable actualTable)
            {
                this.CanonicalTable = canonicalTable;
                this.ActualTable = actualTable;
            }

            public TableBase CanonicalTable { get; }

            public ExprTable ActualTable { get; }
        }

        private static bool TryDetectCycle(
            IReadOnlyList<string> orderedKeys,
            IReadOnlyDictionary<string, List<TableBase>> referencesBySource,
            out string? error)
        {
            var indegree = orderedKeys.ToDictionary(i => i, _ => 0, StringComparer.OrdinalIgnoreCase);
            foreach (var pair in referencesBySource)
            {
                foreach (var reference in pair.Value)
                {
                    var sourceKey = pair.Key;
                    var referenceKey = BuildTableKey(reference.FullName);
                    if (string.Equals(sourceKey, referenceKey, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    indegree[referenceKey]++;
                }
            }

            var queue = new Queue<string>(orderedKeys.Where(i => indegree[i] == 0));
            var visitedCount = 0;

            while (queue.Count > 0)
            {
                var currentKey = queue.Dequeue();
                visitedCount++;

                if (!referencesBySource.TryGetValue(currentKey, out var references))
                {
                    continue;
                }

                foreach (var reference in references)
                {
                    var referenceKey = BuildTableKey(reference.FullName);
                    if (string.Equals(currentKey, referenceKey, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    indegree[referenceKey]--;
                    if (indegree[referenceKey] == 0)
                    {
                        queue.Enqueue(referenceKey);
                    }
                }
            }

            if (visitedCount != orderedKeys.Count)
            {
                error = "Cycle detected in tables graph.";
                return true;
            }

            error = null;
            return false;
        }

        private static void AddLink(IDictionary<string, List<TableBase>> map, string key, TableBase table)
        {
            if (!map.TryGetValue(key, out var list))
            {
                list = new List<TableBase>();
                map[key] = list;
            }

            list.Add(table);
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

        private static IReadOnlyList<TableBase> FilterSelfReference(TableBase source, IReadOnlyList<TableBase> tables)
        {
            var sourceKey = BuildTableKey(source.FullName);
            List<TableBase>? filtered = null;

            for (var i = 0; i < tables.Count; i++)
            {
                var table = tables[i];
                if (string.Equals(sourceKey, BuildTableKey(table.FullName), StringComparison.OrdinalIgnoreCase))
                {
                    filtered ??= new List<TableBase>(tables.Count - 1);
                    continue;
                }

                filtered?.Add(table);
            }

            return filtered ?? tables;
        }
    }
}
