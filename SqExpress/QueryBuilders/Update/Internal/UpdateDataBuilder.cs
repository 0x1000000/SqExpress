using System.Collections.Generic;
using SqExpress.QueryBuilders.Merge;
using SqExpress.QueryBuilders.Merge.Internal;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.QueryBuilders.RecordSetter.Internal;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Update;
using SqExpress.Utils;

namespace SqExpress.QueryBuilders.Update.Internal
{
    internal class UpdateDataBuilder<TTable, TItem> : IUpdateDataBuilder<TTable, TItem>
        where TTable : ExprTable
    {
        private readonly TTable _table;

        private readonly IEnumerable<TItem> _data;

        private readonly ExprTableAlias _sourceTableAlias;

        private DataMapping<TTable, TItem>? _dataMapKeys;

        private DataMapping<TTable, TItem>? _dataMap;

        private MergeUpdateMapping<TTable>? _alsoSet;

        public UpdateDataBuilder(TTable table, IEnumerable<TItem> data, IExprAlias sourceTableAlias)
        {
            this._table = table;
            this._data = data;
            this._sourceTableAlias = new ExprTableAlias(sourceTableAlias);
        }

        public IUpdateDataBuilderMapData<TTable, TItem> MapDataKeys(DataMapping<TTable, TItem> mapping)
        {
            this._dataMapKeys = mapping.AssertArgumentNotNull(nameof(mapping));
            return this;
        }

        public IUpdateDataBuilderAlsoSet<TTable> MapData(DataMapping<TTable, TItem> mapping)
        {
            this._dataMap = mapping.AssertArgumentNotNull(nameof(mapping));
            return this;
        }

        public IUpdateDataBuilderFinal AlsoSet(MergeUpdateMapping<TTable> mapping)
        {
            this._alsoSet = mapping.AssertArgumentNotNull(nameof(mapping));
            return this;
        }

        public ExprUpdate Done()
        {
            var (keys, allColumns, exprValuesTable) = Helpers.AnalyzeUpdateData(
                data: this._data,
                targetTable: this._table,
                dataMapKeys: this._dataMapKeys.AssertNotNull("DataMapKeys should be initialized"),
                dataMap: this._dataMap.AssertNotNull("DataMap should be initialized"),
                extraDataMap: null,
                sourceTableAlias: this._sourceTableAlias);

            var source = new ExprJoinedTable(this._table,
                ExprJoinedTable.ExprJoinType.Inner,
                exprValuesTable,
                this.GetWhere(keys));

            return new ExprUpdate(
                target: this._table, 
                setClause: this.GetSets(allColumns: allColumns, keys: keys), 
                source: source, 
                filter: null);
        }

        private ExprColumnSetClause[] GetSets(IReadOnlyList<ExprColumnName> allColumns, List<ExprColumnName> keys)
        {
            IReadOnlyList<ColumnValueUpdateMap>? extraMaps = null;
            if (this._alsoSet != null)
            {
                var mergeUpdateSetter = new MergerUpdateSetter<TTable>(this._table, this._sourceTableAlias);
                this._alsoSet.Invoke(mergeUpdateSetter);
                extraMaps = mergeUpdateSetter.Maps;
            }

            var updateColNum = allColumns.Count - keys.Count;
            ExprColumnSetClause[] sets = new ExprColumnSetClause[updateColNum + (extraMaps?.Count ?? 0)];

            for (int i = keys.Count; i < allColumns.Count; i++)
            {
                sets[i - keys.Count] = new ExprColumnSetClause(allColumns[i].WithSource(this._table.Alias),
                    allColumns[i].WithSource(this._sourceTableAlias));
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
                    throw new SqExpressException(
                        $"The column name '{sets[i].Column.ColumnName.Name}' is specified more than once in the SET clause");
                }
            }

            return sets;
        }

        private ExprBoolean GetWhere(IReadOnlyList<ExprColumnName> keys)
        {
            ExprBoolean? result = null;
            foreach (var key in keys)
            {
                result = result & key.WithSource(this._table.Alias) == key.WithSource(this._sourceTableAlias);
            }
            return result.AssertNotNull("Update condition cannot be null");
        }

    }

}