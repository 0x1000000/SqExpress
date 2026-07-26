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
    /// <summary>Builds and navigates a graph of foreign-key relationships between table descriptors.</summary>
    /// <remarks>
    /// Table identity is the case-insensitive database/schema/table full name, not object identity. An edge points
    /// from the table containing a foreign key to the referenced table. Self-references are retained but excluded
    /// from navigation unless explicitly requested; cycles involving different tables are rejected at creation.
    /// Join discovery uses inner joins and preserves the caller-supplied endpoint objects and aliases. Connector
    /// tables use canonical descriptors copied with automatic aliases.
    /// </remarks>
    /// <example>
    /// <code>
    /// var graph = TablesGraph.Create(AllTables.BuildAllAliasedTableList());
    ///
    /// if (graph.TryToJoinTables(customer, company, out var source))
    /// {
    ///     var query = SqQueryBuilder.Select(customer.AllColumns())
    ///         .From(source)
    ///         .Done();
    /// }
    /// </code>
    /// </example>
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

        /// <summary>Creates a graph from table descriptors and their foreign-key metadata.</summary>
        /// <param name="tables">The complete set of canonical table descriptors participating in the graph.</param>
        /// <returns>The validated relationship graph.</returns>
        /// <exception cref="SqExpressException">The input contains a null table, duplicate full name, missing foreign-key target, or non-self cycle.</exception>
        public static TablesGraph Create(IReadOnlyList<TableBase> tables)
        {
            if (!TryCreate(tables, out var graph, out var error) || graph == null)
            {
                throw new SqExpressException(error ?? "TablesGraph could not be created.");
            }

            return graph;
        }

        /// <summary>Attempts to create a graph while returning structural validation failures as text.</summary>
        /// <param name="tables">The complete set of canonical table descriptors participating in the graph.</param>
        /// <param name="graph">The created graph on success; otherwise <see langword="null"/>.</param>
        /// <param name="error">The validation error on failure; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the graph is valid and was created.</returns>
        /// <remarks>A null element in <paramref name="tables"/> is a programming error and throws <see cref="SqExpressException"/>.</remarks>
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

        /// <summary>Determines whether a table with the same full name belongs to this graph.</summary>
        /// <param name="table">A table expression whose database/schema/table identity is checked; object identity and alias are ignored.</param>
        /// <returns><see langword="false"/> for null or unknown tables.</returns>
        public bool Contains(ExprTable table)
        {
            if (table == null)
            {
                return false;
            }

            return this._tablesByKey.ContainsKey(BuildTableKey(table.FullName));
        }

        /// <summary>Resolves a table expression to the canonical descriptor stored in this graph.</summary>
        /// <param name="table">A table whose full name identifies the graph entry.</param>
        /// <param name="canonicalTable">The graph-owned descriptor on success; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="false"/> for null or unknown tables.</returns>
        public bool TryGetTable(ExprTable table, [NotNullWhen(true)] out TableBase? canonicalTable)
        {
            canonicalTable = null;
            if (table == null)
            {
                return false;
            }

            return this._tablesByKey.TryGetValue(BuildTableKey(table.FullName), out canonicalTable);
        }

        /// <summary>Determines whether one table directly references another through foreign-key metadata.</summary>
        /// <param name="table">The possible foreign-key source.</param>
        /// <param name="referencedCandidateTable">The possible foreign-key target.</param>
        /// <param name="includeSelfRef">Whether a self-referencing foreign key counts as a reference.</param>
        /// <returns><see langword="false"/> when either table is null, unknown, or not directly related.</returns>
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

        /// <summary>Gets the canonical tables directly referenced by a table's foreign keys.</summary>
        /// <param name="table">The foreign-key source table.</param>
        /// <param name="includeSelfRef">Whether to include the table itself when it has a self-reference.</param>
        /// <returns>The graph-owned target descriptors in deterministic metadata-discovery order.</returns>
        /// <exception cref="ArgumentException">The table does not belong to this graph.</exception>
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

        /// <summary>Traverses all canonical tables transitively referenced by a table.</summary>
        /// <remarks>Each full table name is yielded at most once, in deterministic depth-first discovery order.</remarks>
        /// <param name="table">The foreign-key source at which traversal begins.</param>
        /// <param name="includeSelfRef">Whether a direct self-reference may be yielded; it never causes recursive looping.</param>
        /// <returns>A lazy traversal of graph-owned referenced-table descriptors.</returns>
        /// <exception cref="ArgumentException">The table does not belong to this graph.</exception>
        public IEnumerable<TableBase> GetAllReferences(ExprTable table, bool includeSelfRef = false)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var reference in this.GetAllReferencesIterator(this.ResolveTable(table), visited, includeSelfRef))
            {
                yield return reference;
            }
        }

        /// <summary>Gets canonical tables whose foreign keys directly reference the supplied table.</summary>
        /// <param name="table">The referenced target table.</param>
        /// <param name="includeSelfRef">Whether to include the table itself when it has a self-reference.</param>
        /// <returns>The graph-owned dependent descriptors in deterministic metadata-discovery order.</returns>
        /// <exception cref="ArgumentException">The table does not belong to this graph.</exception>
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

        /// <summary>Traverses all canonical tables that transitively depend on the supplied table.</summary>
        /// <remarks>Each full table name is yielded at most once, in deterministic depth-first discovery order.</remarks>
        /// <param name="table">The referenced target at which reverse traversal begins.</param>
        /// <param name="includeSelfRef">Whether a direct self-reference may be yielded; it never causes recursive looping.</param>
        /// <returns>A lazy traversal of graph-owned dependent-table descriptors.</returns>
        /// <exception cref="ArgumentException">The table does not belong to this graph.</exception>
        public IEnumerable<TableBase> GetAllReferencedBy(ExprTable table, bool includeSelfRef = false)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var referencedBy in this.GetAllReferencedByIterator(this.ResolveTable(table), visited, includeSelfRef))
            {
                yield return referencedBy;
            }
        }

        /// <summary>Builds an inner-join source along the default shortest path between two tables.</summary>
        /// <param name="table1">The first endpoint; its instance and alias are preserved.</param>
        /// <param name="table2">The second endpoint; its instance and alias are preserved.</param>
        /// <param name="join">Receives the left-deep join source on success; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="false"/> for unknown, identical, or disconnected tables.</returns>
        public bool TryToJoinTables(
            ExprTable table1,
            ExprTable table2,
            [NotNullWhen(true)] out IExprTableSource? join)
            => this.TryToJoinTables(table1, table2, intermediateTables: null, out join, new TablesGraphJoinOptions());

        /// <summary>Builds an inner-join source between two tables using explicit ambiguity options.</summary>
        /// <param name="table1">The first endpoint; its instance and alias are preserved.</param>
        /// <param name="table2">The second endpoint; its instance and alias are preserved.</param>
        /// <param name="join">Receives the left-deep join source on success; otherwise <see langword="null"/>.</param>
        /// <param name="options">The policy applied when multiple equally short relationship paths exist.</param>
        /// <returns><see langword="false"/> for unknown, identical, disconnected, or policy-rejected ambiguous endpoints.</returns>
        /// <exception cref="ArgumentException">The ambiguity options are invalid.</exception>
        public bool TryToJoinTables(
            ExprTable table1,
            ExprTable table2,
            [NotNullWhen(true)] out IExprTableSource? join,
            TablesGraphJoinOptions options)
            => this.TryToJoinTables(table1, table2, intermediateTables: null, out join, options);

        /// <summary>Builds an inner-join source through an ordered list of mandatory intermediate tables.</summary>
        /// <remarks>Each intermediate table is a checkpoint; every path segment uses the default shortest-path policy.</remarks>
        /// <param name="table1">The first endpoint; its instance and alias are preserved.</param>
        /// <param name="table2">The second endpoint; its instance and alias are preserved.</param>
        /// <param name="intermediateTables">Ordered mandatory checkpoints, or <see langword="null"/> for a direct shortest-path search.</param>
        /// <param name="join">Receives the left-deep join source on success; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="false"/> when any checkpoint is unknown/disconnected or the endpoints are invalid.</returns>
        public bool TryToJoinTables(
            ExprTable table1,
            ExprTable table2,
            IReadOnlyList<ExprTable>? intermediateTables,
            [NotNullWhen(true)] out IExprTableSource? join)
            => this.TryToJoinTables(table1, table2, intermediateTables, out join, new TablesGraphJoinOptions());

        /// <summary>Builds an inner-join source through ordered checkpoints using explicit ambiguity options.</summary>
        /// <param name="table1">The first endpoint; its instance and alias are preserved.</param>
        /// <param name="table2">The second endpoint; its instance and alias are preserved.</param>
        /// <param name="intermediateTables">Ordered mandatory checkpoints between <paramref name="table1"/> and <paramref name="table2"/>.</param>
        /// <param name="join">The joined table source on success; otherwise <see langword="null"/>.</param>
        /// <param name="options">The policy used independently for ambiguity in each path segment.</param>
        /// <returns><see langword="false"/> when an input is unknown, a segment is disconnected, or ambiguity policy selects failure.</returns>
        /// <exception cref="ArgumentException">The ambiguity options are invalid.</exception>
        public bool TryToJoinTables(
            ExprTable table1,
            ExprTable table2,
            IReadOnlyList<ExprTable>? intermediateTables,
            [NotNullWhen(true)] out IExprTableSource? join,
            TablesGraphJoinOptions options)
        {
            ValidateJoinOptions(options);
            join = null;

            if (!this.TryResolveTable(table1, out var canonicalTable1) || !this.TryResolveTable(table2, out var canonicalTable2))
            {
                return false;
            }

            if (string.Equals(BuildTableKey(canonicalTable1.FullName), BuildTableKey(canonicalTable2.FullName), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var path = this.TryBuildPath(table1, canonicalTable1, table2, canonicalTable2, intermediateTables, options);
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

        /// <summary>Builds one left-deep inner-join tree connecting multiple requested tables.</summary>
        /// <remarks>
        /// The first table is the root. Remaining tables are attached greedily by the nearest available path; this is
        /// deterministic but is not guaranteed to be a globally minimal connector tree.
        /// </remarks>
        /// <param name="tables">Requested tables in root-first order; caller instances and aliases are preserved.</param>
        /// <param name="join">Receives the connected join tree on success; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="false"/> for empty, duplicate, unknown, disconnected, or ambiguously connected input.</returns>
        public bool TryToJoinTables(
            IReadOnlyList<ExprTable> tables,
            [NotNullWhen(true)] out IExprTableSource? join)
            => this.TryToJoinTables(tables, new TablesGraphJoinOptions(), out join);

        /// <summary>Builds one left-deep inner-join tree using explicit ambiguity options.</summary>
        /// <param name="tables">Requested tables in root-first order. Their instances and aliases are preserved.</param>
        /// <param name="options">The policy used when equal shortest paths are available.</param>
        /// <param name="join">The joined table source on success; otherwise <see langword="null"/>.</param>
        /// <returns>
        /// <see langword="false"/> for null or empty input, duplicate full names, unknown or disconnected tables,
        /// or ambiguity rejected by the selected policy. A single known table succeeds unchanged.
        /// </returns>
        /// <exception cref="ArgumentException">The ambiguity options are invalid.</exception>
        public bool TryToJoinTables(
            IReadOnlyList<ExprTable> tables,
            TablesGraphJoinOptions options,
            [NotNullWhen(true)] out IExprTableSource? join)
        {
            ValidateJoinOptions(options);
            join = null;
            if (tables == null || tables.Count == 0)
            {
                return false;
            }

            var actualByKey = new Dictionary<string, ExprTable>(StringComparer.OrdinalIgnoreCase);
            var canonicalByKey = new Dictionary<string, TableBase>(StringComparer.OrdinalIgnoreCase);
            var requestedKeys = new List<string>(tables.Count);
            for (var i = 0; i < tables.Count; i++)
            {
                var actual = tables[i];
                if (!this.TryResolveTable(actual, out var canonical))
                {
                    return false;
                }

                var key = BuildTableKey(canonical.FullName);
                if (actualByKey.ContainsKey(key))
                {
                    return false;
                }

                actualByKey.Add(key, actual);
                canonicalByKey.Add(key, canonical);
                requestedKeys.Add(key);
            }

            if (tables.Count == 1)
            {
                join = tables[0];
                return true;
            }

            var treeKeys = new List<string> { requestedKeys[0] };
            var tree = new HashSet<string>(treeKeys, StringComparer.OrdinalIgnoreCase);
            var pending = new HashSet<string>(requestedKeys.Skip(1), StringComparer.OrdinalIgnoreCase);
            IExprTableSource source = tables[0];

            while (pending.Count > 0)
            {
                var sourceTables = treeKeys.Select(key => canonicalByKey[key]).ToArray();
                var candidates = this.FindShortestPaths(sourceTables, pending, options);
                var selected = SelectPath(candidates, options);
                if (selected == null)
                {
                    return false;
                }

                for (var i = 1; i < selected.Count; i++)
                {
                    var parentCanonical = selected[i - 1];
                    var currentCanonical = selected[i];
                    var parentKey = BuildTableKey(parentCanonical.FullName);
                    var currentKey = BuildTableKey(currentCanonical.FullName);
                    if (!actualByKey.TryGetValue(parentKey, out var parentActual))
                    {
                        return false;
                    }
                    if (!actualByKey.TryGetValue(currentKey, out var currentActual))
                    {
                        currentActual = currentCanonical.WithAlias(SqQueryBuilder.TableAlias());
                        actualByKey.Add(currentKey, currentActual);
                    }
                    if (!canonicalByKey.ContainsKey(currentKey))
                    {
                        canonicalByKey.Add(currentKey, currentCanonical);
                    }

                    source = new ExprJoinedTable(
                        source,
                        ExprJoinedTable.ExprJoinType.Inner,
                        currentActual,
                        BuildJoinCondition(parentCanonical, currentCanonical, parentActual, currentActual));

                    if (tree.Add(currentKey))
                    {
                        treeKeys.Add(currentKey);
                    }
                    pending.Remove(currentKey);
                }
            }

            join = source;
            return true;
        }

        private List<PathItem>? TryBuildPath(
            ExprTable sourceActual,
            TableBase sourceCanonical,
            ExprTable targetActual,
            TableBase targetCanonical,
            IReadOnlyList<ExprTable>? intermediateTables,
            TablesGraphJoinOptions options)
        {
            if (intermediateTables == null || intermediateTables.Count == 0)
            {
                var directPath = SelectPath(
                    this.FindShortestPaths(new[] { sourceCanonical }, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { BuildTableKey(targetCanonical.FullName) }, options),
                    options);
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

                var segmentPath = SelectPath(
                    this.FindShortestPaths(new[] { segmentSourceCanonical }, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { BuildTableKey(segmentTargetCanonical.FullName) }, options),
                    options);
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

        private IReadOnlyList<IReadOnlyList<TableBase>> FindShortestPaths(
            IReadOnlyList<TableBase> sources,
            HashSet<string> targetKeys,
            TablesGraphJoinOptions options)
        {
            var queue = new Queue<string>();
            var distance = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var previous = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < sources.Count; i++)
            {
                var sourceKey = BuildTableKey(sources[i].FullName);
                if (distance.ContainsKey(sourceKey))
                {
                    continue;
                }
                distance.Add(sourceKey, 0);
                previous.Add(sourceKey, new List<string>());
                queue.Enqueue(sourceKey);
            }

            int? targetDistance = null;
            var reachedTargets = new List<string>();
            while (queue.Count > 0)
            {
                var currentKey = queue.Dequeue();
                var currentDistance = distance[currentKey];
                if (targetDistance.HasValue && currentDistance > targetDistance.Value)
                {
                    break;
                }
                if (targetKeys.Contains(currentKey))
                {
                    targetDistance ??= currentDistance;
                    reachedTargets.Add(currentKey);
                    continue;
                }

                foreach (var neighbor in this.GetAdjacentTables(this._tablesByKey[currentKey]))
                {
                    var neighborKey = BuildTableKey(neighbor.FullName);
                    var neighborDistance = currentDistance + 1;
                    if (!distance.TryGetValue(neighborKey, out var knownDistance))
                    {
                        distance.Add(neighborKey, neighborDistance);
                        previous.Add(neighborKey, new List<string> { currentKey });
                        queue.Enqueue(neighborKey);
                        continue;
                    }
                    if (knownDistance == neighborDistance)
                    {
                        previous[neighborKey].Add(currentKey);
                    }
                }
            }

            if (reachedTargets.Count == 0)
            {
                return Array.Empty<IReadOnlyList<TableBase>>();
            }

            var maxPaths = options.AmbiguousPathBehavior switch
            {
                AmbiguousJoinPathBehavior.DeterministicFirst => 1,
                AmbiguousJoinPathBehavior.Fail => 2,
                AmbiguousJoinPathBehavior.Callback => int.MaxValue,
                _ => throw new ArgumentOutOfRangeException(nameof(options.AmbiguousPathBehavior))
            };
            var result = new List<IReadOnlyList<TableBase>>();
            foreach (var targetKey in reachedTargets)
            {
                ReconstructAllPaths(targetKey, previous, this._tablesByKey, new List<TableBase>(), result, maxPaths);
                if (result.Count >= maxPaths)
                {
                    break;
                }
            }
            return result;
        }

        private static void ReconstructAllPaths(
            string currentKey,
            IReadOnlyDictionary<string, List<string>> previous,
            IReadOnlyDictionary<string, TableBase> tablesByKey,
            List<TableBase> reversedPath,
            List<IReadOnlyList<TableBase>> result,
            int maxPaths)
        {
            reversedPath.Add(tablesByKey[currentKey]);
            var predecessors = previous[currentKey];
            if (predecessors.Count == 0)
            {
                var path = reversedPath.ToArray();
                Array.Reverse(path);
                result.Add(path);
            }
            else
            {
                for (var i = 0; i < predecessors.Count && result.Count < maxPaths; i++)
                {
                    ReconstructAllPaths(predecessors[i], previous, tablesByKey, reversedPath, result, maxPaths);
                }
            }
            reversedPath.RemoveAt(reversedPath.Count - 1);
        }

        private static IReadOnlyList<TableBase>? SelectPath(
            IReadOnlyList<IReadOnlyList<TableBase>> candidates,
            TablesGraphJoinOptions options)
        {
            if (candidates.Count == 0)
            {
                return null;
            }
            if (candidates.Count == 1 || options.AmbiguousPathBehavior == AmbiguousJoinPathBehavior.DeterministicFirst)
            {
                return candidates[0];
            }
            if (options.AmbiguousPathBehavior == AmbiguousJoinPathBehavior.Fail)
            {
                return null;
            }

            var selectedIndex = options.AmbiguousPathResolver!(candidates);
            if (selectedIndex < 0 || selectedIndex >= candidates.Count)
            {
                throw new ArgumentException("The ambiguous join path resolver returned an invalid candidate index.", nameof(options));
            }
            return candidates[selectedIndex];
        }

        private static void ValidateJoinOptions(TablesGraphJoinOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.AmbiguousPathBehavior == AmbiguousJoinPathBehavior.Callback
                && options.AmbiguousPathResolver == null)
            {
                throw new ArgumentException("An ambiguous join path resolver is required for Callback behavior.", nameof(options));
            }
            if (!Enum.IsDefined(typeof(AmbiguousJoinPathBehavior), options.AmbiguousPathBehavior))
            {
                throw new ArgumentOutOfRangeException(nameof(options.AmbiguousPathBehavior));
            }
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
