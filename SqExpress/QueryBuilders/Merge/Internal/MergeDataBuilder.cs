using System;
using System.Collections.Generic;
using System.Linq;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.QueryBuilders.RecordSetter.Internal;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Output;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress.QueryBuilders.Merge.Internal
{
    internal class MergeDataBuilder<TTable, TItem> : IMergeDataBuilder<TTable, TItem>
        where TTable : ExprTable
    {
        private readonly TTable _table;

        private readonly IEnumerable<TItem> _data;

        private readonly ExprTableAlias _sourceTableAlias;

        private DataMapping<TTable, TItem>? _dataMapKeys;

        private DataMapping<TTable, TItem>? _dataMap;

        private IndexDataMapping? _extraDataMap;

        private ExprBoolean? _andOn;

        private WhenMatched? _whenMatched;

        private WhenNotMatchedByTarget? _whenNotMatchedByTarget;

        private WhenNotMatchedBySource? _whenNotMatchedBySource;

        private ExprOutput? _output;

        internal MergeDataBuilder(TTable table, IEnumerable<TItem> data, IExprAlias sourceTableAlias)
        {
            this._table = table;
            this._data = data;
            this._sourceTableAlias = new ExprTableAlias(sourceTableAlias);
        }

        public IMergeDataBuilderMapData<TTable, TItem> MapDataKeys(DataMapping<TTable, TItem> mapping)
        {
            this._dataMapKeys = mapping.AssertArgumentNotNull(nameof(mapping));
            return this;
        }

        public IMergeDataBuilderMapExtraData<TTable, TItem> MapData(DataMapping<TTable, TItem> mapping)
        {
            this._dataMap = mapping.AssertArgumentNotNull(nameof(mapping));
            return this;
        }

        public IMergeDataBuilderAndOn<TTable> MapExtraData(IndexDataMapping mapping)
        {
            this._extraDataMap = mapping.AssertArgumentNotNull(nameof(mapping));
            return this;
        }

        public IMergeDataBuilderWhenInit<TTable> AndOn(MergeTargetSourceCondition<TTable> condition)
        {
            this._andOn = condition
                .AssertArgumentNotNull(nameof(condition))
                .Invoke(this._table, this._sourceTableAlias)
                .AssertNotNull("Extra join condition cannot be null");
            return this;
        }

        public IMergeDataBuilderWhenMatchedWithMap<TTable> WhenMatchedThenUpdate(
            MergeTargetSourceCondition<TTable>? and = null)
        {
            this._whenMatched = WhenMatched.Update(and?.Invoke(this._table, this._sourceTableAlias).AssertNotNull("Boolean expression cannot be null"));
            return this;
        }

        public IMergeDataBuilderNotMatchTarget<TTable> AlsoSet(MergeUpdateMapping<TTable> mapping)
        {
            this._whenMatched = this._whenMatched
                .AssertNotNull("WhenMatched is expected to be set")
                .WithMapping(mapping.AssertArgumentNotNull(nameof(mapping)));
            return this;
        }

        public IMergeDataBuilderNotMatchTarget<TTable> WhenMatchedThenDelete(MergeTargetSourceCondition<TTable>? and = null)
        {
            this._whenMatched = WhenMatched.Delete(and?.Invoke(this._table, this._sourceTableAlias).AssertNotNull("Boolean expression cannot be null"));
            return this;
        }

        public IMergeDataBuilderNotMatchTargetExclude<TTable> WhenNotMatchedByTargetThenInsert(
            MergeTargetSourceCondition<TTable>? and = null)
        {
            this._whenNotMatchedByTarget = WhenNotMatchedByTarget.Insert(and?.Invoke(this._table, this._sourceTableAlias).AssertNotNull("Boolean expression cannot be null"));
            return this;
        }

        public IMergeDataBuilderNotMatchTargetExcludeSpecific<TTable> ExcludeKeys()
        {
            this._whenNotMatchedByTarget = this.AssertNotMatchedByTargetIsSet().WithExcludeKeys(true);
            return this;
        }

        public IMergeDataBuilderNotMatchTargetWithMap<TTable> Exclude(Func<TTable, ExprColumnName> column)
        {
            var col = column.AssertArgumentNotNull(nameof(column)).Invoke(this._table).AssertNotNull("Column should not be null");
            this._whenNotMatchedByTarget = this.AssertNotMatchedByTargetIsSet().WithExclude(new[] {col});
            return this;
        }

        public IMergeDataBuilderNotMatchTargetWithMap<TTable> Exclude(Func<TTable, IReadOnlyList<ExprColumnName>> columns)
        {
            var c = columns.AssertArgumentNotNull(nameof(columns)).Invoke(this._table).AssertNotEmpty("Exclude columns should have at least one item ");
            this._whenNotMatchedByTarget = this.AssertNotMatchedByTargetIsSet().WithExclude(c);
            return this;
        }

        public IMergeDataBuilderNotMatchSource<TTable> AlsoInsert(MergeUpdateMapping<TTable> mapping)
        {
            this._whenNotMatchedByTarget = this.AssertNotMatchedByTargetIsSet()
                .WithMapping(mapping.AssertArgumentNotNull(nameof(mapping)));
            return this;
        }

        public IMergeDataBuilderNotMatchTargetWithMap<TTable> WhenNotMatchedByTargetThenInsertDefaults(
            MergeTargetSourceCondition<TTable>? and = null)
        {
            this._whenNotMatchedByTarget = WhenNotMatchedByTarget.InsertDefaults(and?.Invoke(this._table, this._sourceTableAlias).AssertNotNull("Boolean expression cannot be null"));
            return this;
        }

        public IMergeDataBuilderNotMatchSourceWithMap<TTable> WhenNotMatchedBySourceThenUpdate(Func<TTable, ExprBoolean>? and = null)
        {
            this._whenNotMatchedBySource = WhenNotMatchedBySource.Update(and?.Invoke(this._table).AssertNotNull("Boolean expression cannot be null"));
            return this;
        }

        public IMergeDataBuilderFinalOutput<TTable> Set(TargetUpdateMapping<TTable> mapping)
        {
            this._whenNotMatchedBySource = this._whenNotMatchedBySource
                .AssertNotNull("NotMatchedBySource is expected to be set")
                .WithMapping(mapping.AssertArgumentNotNull(nameof(mapping)));
            return this;
        }

        public IMergeDataBuilderFinalOutput<TTable> WhenNotMatchedBySourceThenDelete(Func<TTable, ExprBoolean>? and = null)
        {
            this._whenNotMatchedBySource = WhenNotMatchedBySource.Delete(and?.Invoke(this._table).AssertNotNull("Boolean expression cannot be null"));
            return this;
        }

        public IMergeDataBuilderOutputFinal Output(OutputMapping<TTable> mapping)
        {
            var outputSetter = new OutputSetter();
            mapping.AssertArgumentNotNull(nameof(mapping)).Invoke(this._table, this._sourceTableAlias, outputSetter);

            var cols = outputSetter.Columns.AssertNotEmpty("Output column list cannot be empty");

            this._output = new ExprOutput(cols);

            return this;
        }

        public ExprMerge Done()
        {
            var records = this._data.TryToCheckLength(out var capacity)
                ? capacity > 0 ? new List<ExprRowValue>(capacity) : null
                : new List<ExprRowValue>();

            if (records == null)
            {
                throw new SqExpressException("Input data should not be empty");
            }

            var dataMapKeys = this._dataMapKeys.AssertNotNull("DataMapKeys should be initialized");
            var dataMap = this._dataMap.AssertNotNull("DataMap should be initialized");


            DataMapSetter<TTable, TItem>? setter = null;
            List<ExprColumnName>? keys = null;
            IReadOnlyList<ExprColumnName>? allTableColumns = null;
            IReadOnlyList<ExprColumnName>? totalColumns = null;

            foreach (var item in this._data)
            {
                setter ??= new DataMapSetter<TTable, TItem>(this._table, item);

                setter.NextItem(item, totalColumns?.Count);
                dataMapKeys(setter);

                keys ??= new List<ExprColumnName>(setter.Columns);

                keys.AssertNotEmpty("There should be at least one key");

                dataMap(setter);

                totalColumns = allTableColumns ??= new List<ExprColumnName>(setter.Columns);

                if (this._extraDataMap != null)
                {
                    this._extraDataMap.Invoke(setter);
                    if (ReferenceEquals(totalColumns, allTableColumns))
                    {
                        totalColumns = new List<ExprColumnName>(setter.Columns);
                    }
                }

                setter.EnsureRecordLength();
                records.Add(new ExprRowValue(setter.Record.AssertFatalNotNull(nameof(setter.Record))));
            }

            if (records.Count < 1)
            {
                throw new SqExpressException("Input data should not be empty");
            }

            keys = keys.AssertFatalNotNull(nameof(keys));
            allTableColumns = allTableColumns.AssertFatalNotNull(nameof(allTableColumns));
            totalColumns = totalColumns.AssertFatalNotNull(nameof(allTableColumns));

            var exprValuesTable = new ExprDerivedTableValues(new ExprTableValueConstructor(records), this._sourceTableAlias, totalColumns);

            if (keys.Count >= allTableColumns.Count)
            {
                throw new SqExpressException("The number of keys exceeds the number of columns");
            }

            return new ExprMerge(
                this._table, 
                exprValuesTable, 
                this.GetOn(keys), 
                this.BuildWhenMatched(keys, allTableColumns),
                this.BuildWhenNotMatchedByTarget(keys, allTableColumns),
                this.BuildWhenNotMatchedBySource());
        }

        ExprMergeOutput IMergeDataBuilderOutputFinal.Done()
        {
            var merge = this.Done();
            return ExprMergeOutput.FromMerge(merge, this._output.AssertFatalNotNull(nameof(this._output)));
        }

        private ExprBoolean GetOn(IReadOnlyList<ExprColumnName> keys)
        {
            ExprBoolean? result = null;
            foreach (var key in keys)
            {
                result = result & key.WithSource(this._table.Alias) == key.WithSource(this._sourceTableAlias);
            }

            if (this._andOn != null)
            {
                result = result & this._andOn;
            }
            return result.AssertNotNull("Merge combination condition cannot be null");
        }

        private IExprMergeMatched? BuildWhenMatched(IReadOnlyList<ExprColumnName> keys, IReadOnlyList<ExprColumnName> allColumns)
        {
            if (!this._whenMatched.HasValue)
            {
                return null;
            }
            var when = this._whenMatched.Value;

            if (!when.IsDelete)
            {
                IReadOnlyList<ColumnValueUpdateMap>? extraMaps = null;
                if (when.Mapping != null)
                {
                    var mergeUpdateSetter = new MergerUpdateSetter<TTable>(this._table, this._sourceTableAlias);
                    when.Mapping.Invoke(mergeUpdateSetter);
                    extraMaps = mergeUpdateSetter.Maps;
                }

                var updateColNum = allColumns.Count - keys.Count;
                ExprColumnSetClause[] sets = new ExprColumnSetClause[updateColNum + (extraMaps?.Count ?? 0)];

                for (int i = keys.Count; i < allColumns.Count; i++)
                {
                    sets[i- keys.Count] = new ExprColumnSetClause(allColumns[i].WithSource(this._table.Alias), allColumns[i].WithSource(this._sourceTableAlias));
                }
                if (extraMaps != null && extraMaps.Count > 0)
                {
                    for (int i = updateColNum; i < sets.Length; i++)
                    {
                        var extraMap = extraMaps[i - updateColNum];
                        sets[i] = new ExprColumnSetClause(extraMap.Column.WithSource(this._table.Alias), extraMap.Value);
                    }
                }

                HashSet<ExprColumn> duplicateChecker = new HashSet<ExprColumn>();
                for (int i = 0; i < sets.Length; i++)
                {
                    if (!duplicateChecker.Add(sets[i].Column))
                    {
                        throw new SqExpressException($"The column name '{sets[i].Column.ColumnName.Name}' is specified more than once in the SET clause");
                    }
                }

                return new ExprMergeMatchedUpdate(when.And, sets);
            }

            return new ExprMergeMatchedDelete(when.And);
        }

        private IExprMergeNotMatched? BuildWhenNotMatchedByTarget(IReadOnlyList<ExprColumnName> keys, IReadOnlyList<ExprColumnName> allColumns)
        {
            if (!this._whenNotMatchedByTarget.HasValue)
            {
                return null;
            }
            var when = this._whenNotMatchedByTarget.Value;

            if (!when.IsDefaultValues)
            {
                IReadOnlyList<ColumnValueUpdateMap>? extraMaps = null;
                if (when.Mapping != null)
                {
                    var mergeUpdateSetter = new MergerUpdateSetter<TTable>(this._table, this._sourceTableAlias);
                    when.Mapping(mergeUpdateSetter);
                    extraMaps = mergeUpdateSetter.Maps;
                }

                var actualColumns = when.ExcludeKeys
                    ? new ReadOnlyListSegment<ExprColumnName>(allColumns, keys.Count)
                    : new ReadOnlyListSegment<ExprColumnName>(allColumns);

                int totalCount = actualColumns.Count + (extraMaps?.Count ?? 0);

                var insertColumns = new List<ExprColumnName>(totalCount);
                var insertValues = new List<IExprAssigning>(totalCount);

                for (int i = 0; i < actualColumns.Count; i++)
                {
                    var coll = actualColumns[i];

                    if (when.Exclude != null && when.Exclude.Contains(coll))
                    {
                        continue;
                    }

                    insertColumns.Add(coll);
                    insertValues.Add(coll.WithSource(this._sourceTableAlias));
                }

                if (extraMaps != null && extraMaps.Count > 0)
                {
                    for (int i = actualColumns.Count; i < totalCount; i++)
                    {
                        var m = extraMaps[i - actualColumns.Count];
                        insertColumns.Add(m.Column);
                        insertValues.Add(m.Value);
                    }
                }

                HashSet<ExprColumnName> duplicateChecker = new HashSet<ExprColumnName>();
                for (int i = 0; i < insertColumns.Count; i++)
                {
                    if (!duplicateChecker.Add(insertColumns[i]))
                    {
                        throw new SqExpressException($"The column name '{insertColumns[i].Name}' is specified more than once in the column list of an INSERT");
                    }
                }

                return new ExprExprMergeNotMatchedInsert(when.And, insertColumns, insertValues);
            }

            return new ExprExprMergeNotMatchedInsertDefault(when.And);
        }

        private IExprMergeMatched? BuildWhenNotMatchedBySource()
        {

            if (!this._whenNotMatchedBySource.HasValue)
            {
                return null;
            }
            var when = this._whenNotMatchedBySource.Value;

            if (!when.IsDelete)
            {
                var mergeTargetUpdateSetter = new TargetUpdateSetter<TTable>(this._table);
                when.Mapping.AssertFatalNotNull("WhenNotMatchedBySource Mapping").Invoke(mergeTargetUpdateSetter);

                mergeTargetUpdateSetter.Maps.AssertNotEmpty("SET Clause for 'When Not Matched By Source' cannot be empty");

                ExprColumnSetClause[] sets = new ExprColumnSetClause[mergeTargetUpdateSetter.Maps.Count];

                HashSet<ExprColumnName> duplicateChecker = new HashSet<ExprColumnName>();
                for (int i = 0; i < sets.Length; i++)
                {
                    if (!duplicateChecker.Add(mergeTargetUpdateSetter.Maps[i].Column))
                    {
                        throw new SqExpressException($"The column name '{sets[i].Column.ColumnName.Name}' is specified more than once in the SET clause");
                    }
                    sets[i] = new ExprColumnSetClause(mergeTargetUpdateSetter.Maps[i].Column.WithSource(this._table.Alias), mergeTargetUpdateSetter.Maps[i].Value);
                }
                return new ExprMergeMatchedUpdate(when.And, sets);
            }
            return new ExprMergeMatchedDelete(when.And);
        }

        private WhenNotMatchedByTarget AssertNotMatchedByTargetIsSet()
        {
            return this._whenNotMatchedByTarget
                .AssertNotNull("NotMatchedByTarget is expected to be set");
        }

        private readonly struct WhenMatched
        {
            public readonly bool IsDelete;

            public readonly ExprBoolean? And;

            public readonly MergeUpdateMapping<TTable>? Mapping;

            public WhenMatched(bool isDelete, ExprBoolean? and, MergeUpdateMapping<TTable>? mapping)
            {
                this.IsDelete = isDelete;
                this.Mapping = mapping;
                this.And = and;
            }

            public static WhenMatched Update(ExprBoolean? and) => new WhenMatched(false, and, null);

            public static WhenMatched Delete(ExprBoolean? and) => new WhenMatched(true, and, null);

            public WhenMatched WithMapping(MergeUpdateMapping<TTable> mapping)
            {
                if (this.IsDelete)
                {
                    throw new SqExpressException("Additional settings are not allowed in case of deletion");
                }
                return new WhenMatched(false, this.And, mapping);
            }
        }

        private readonly struct WhenNotMatchedByTarget
        {
            public readonly bool IsDefaultValues;

            public readonly ExprBoolean? And;

            public readonly MergeUpdateMapping<TTable>? Mapping;

            public readonly bool ExcludeKeys;

            public readonly IReadOnlyList<ExprColumnName>? Exclude;

            public WhenNotMatchedByTarget(bool isDefaultValues, ExprBoolean? and, MergeUpdateMapping<TTable>? mapping, bool excludeKeys, IReadOnlyList<ExprColumnName>? exclude)
            {
                this.IsDefaultValues = isDefaultValues;
                this.Mapping = mapping;
                this.ExcludeKeys = excludeKeys;
                this.Exclude = exclude;
                this.And = and;
            }

            public static WhenNotMatchedByTarget Insert(ExprBoolean? and) => new WhenNotMatchedByTarget(false, and, null, false, null);

            public static WhenNotMatchedByTarget InsertDefaults(ExprBoolean? and) => new WhenNotMatchedByTarget(true, and, null, false, null);

            public WhenNotMatchedByTarget WithMapping(MergeUpdateMapping<TTable> mapping)
            {
                this.AssertNotDefault();
                return new WhenNotMatchedByTarget(false, this.And, mapping, this.ExcludeKeys, this.Exclude);
            }

            public WhenNotMatchedByTarget WithExcludeKeys(bool excludeKeys)
            {
                this.AssertNotDefault();
                return new WhenNotMatchedByTarget(false, this.And, this.Mapping, excludeKeys, this.Exclude);
            }

            public WhenNotMatchedByTarget WithExclude(IReadOnlyList<ExprColumnName> exclude)
            {
                this.AssertNotDefault();
                return new WhenNotMatchedByTarget(false, this.And, this.Mapping, this.ExcludeKeys, exclude.AssertNotNull("There should be at least one column in the exclusion list"));
            }

            private void AssertNotDefault()
            {
                if (this.IsDefaultValues)
                {
                    throw new SqExpressException("Additional settings are not allowed in case of default values");
                }
            }
        }

        private readonly struct WhenNotMatchedBySource
        {
            public readonly bool IsDelete;

            public readonly ExprBoolean? And;

            public readonly TargetUpdateMapping<TTable>? Mapping;

            public WhenNotMatchedBySource(bool isDelete, ExprBoolean? and, TargetUpdateMapping<TTable>? mapping)
            {
                this.IsDelete = isDelete;
                this.Mapping = mapping;
                this.And = and;
            }

            public static WhenNotMatchedBySource Update(ExprBoolean? and) => new WhenNotMatchedBySource(false, and, null);

            public static WhenNotMatchedBySource Delete(ExprBoolean? and) => new WhenNotMatchedBySource(true, and, null);

            public WhenNotMatchedBySource WithMapping(TargetUpdateMapping<TTable> mapping)
            {
                if (this.IsDelete)
                {
                    throw new SqExpressException("Additional settings are not allowed in case of deletion");
                }
                return new WhenNotMatchedBySource(false, this.And, mapping);
            }
        }
    }
}