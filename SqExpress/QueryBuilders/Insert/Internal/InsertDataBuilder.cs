using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.QueryBuilders.RecordSetter.Internal;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Update;
using SqExpress.Utils;

namespace SqExpress.QueryBuilders.Insert.Internal
{
    public class InsertDataBuilder<TTable, TItem> : IInsertDataBuilder<TTable, TItem> where TTable : ExprTable
    {
        private const string InputDataShouldNotBeEmpty = "Input data should not be empty";

        readonly TTable _target;

        private readonly IEnumerable<TItem> _data;

        private DataMapping<TTable, TItem>? _dataMapping;

        private TargetInsertSelectMapping<TTable>? _targetInsertSelectMapping;

        private IReadOnlyList<ExprAliasedColumnName>? _output;

        private IReadOnlyList<ExprColumn>? _checkExistenceByColumns;

        public InsertDataBuilder(TTable target, IEnumerable<TItem> data)
        {
            this._target = target;
            this._data = data;
        }

        public IInsertDataBuilderAlsoInsert<TTable> MapData(DataMapping<TTable, TItem> mapping)
        {
            this._dataMapping.AssertFatalNull(nameof(this._dataMapping));
            this._dataMapping = mapping;
            return this;
        }

        public IInsertDataBuilderWhere AlsoInsert(TargetInsertSelectMapping<TTable> targetInsertSelectMapping)
        {
            this._targetInsertSelectMapping.AssertFatalNull(nameof(this._dataMapping));
            this._targetInsertSelectMapping = targetInsertSelectMapping;
            return this;
        }

        public ExprInsert Done()
        {
            var checkExistence = this._checkExistenceByColumns != null && this._checkExistenceByColumns.Count > 0;

            var useDerivedTable = this._targetInsertSelectMapping != null || checkExistence;

            var mapping = this._dataMapping.AssertFatalNotNull(nameof(this._dataMapping));

            int? capacity = this._data.TryToCheckLength(out var c) ? c : (int?)null;

            if (capacity != null && capacity.Value < 1)
            {
                throw new SqExpressException(InputDataShouldNotBeEmpty);
            }

            List<ExprValueRow>? recordsS = null;
            List<ExprInsertValueRow>? recordsI = null;

            if (useDerivedTable)
            {
                recordsS = capacity.HasValue ? new List<ExprValueRow>(capacity.Value) : new List<ExprValueRow>();
            }
            else
            {
                recordsI = capacity.HasValue ? new List<ExprInsertValueRow>(capacity.Value) : new List<ExprInsertValueRow>();
            }

            DataMapSetter<TTable, TItem>? dataMapSetter = null;
            IReadOnlyList<ExprColumnName>? columns = null;

            foreach (var item in this._data)
            {
                dataMapSetter ??= new DataMapSetter<TTable, TItem>(this._target, item);

                dataMapSetter.NextItem(item, columns?.Count);
                mapping(dataMapSetter);

                columns ??= dataMapSetter.Columns.SelectToReadOnlyList(x => x.ColumnName);

                dataMapSetter.EnsureRecordLength();

                recordsS?.Add(new ExprValueRow(dataMapSetter.Record.AssertFatalNotNull(nameof(dataMapSetter.Record))));
                recordsI?.Add(new ExprInsertValueRow(dataMapSetter.Record.AssertFatalNotNull(nameof(dataMapSetter.Record))));
            }

            if ( (recordsS?.Count ?? 0 + recordsI?.Count ?? 0) < 1 || columns == null)
            {
                //In case of empty IEnumerable
                throw new SqExpressException(InputDataShouldNotBeEmpty);
            }

            IExprInsertSource insertSource;

            if (recordsI != null)
            {
                insertSource = new ExprInsertValues(recordsI);
            }
            else if(recordsS != null && useDerivedTable)
            {
                var valuesConstructor = new ExprTableValueConstructor(recordsS);
                var values = new ExprDerivedTableValues(
                    valuesConstructor,
                    new ExprTableAlias(Alias.Auto.BuildAliasExpression().AssertNotNull("Alias cannot be null")),
                    columns);

                IReadOnlyList<ColumnValueInsertSelectMap>? additionalMaps = null;
                if (this._targetInsertSelectMapping != null)
                {
                    var targetUpdateSetter = new TargetInsertSelectSetter<TTable>(this._target);

                    this._targetInsertSelectMapping.Invoke(targetUpdateSetter);

                    additionalMaps = targetUpdateSetter.Maps;
                    if (additionalMaps.Count < 1)
                    {
                        throw new SqExpressException("Additional insertion cannot be null");
                    }
                }

                var selectValues = new List<IExprSelecting>(columns.Count + (additionalMaps?.Count ?? 0));

                foreach (var exprColumnName in values.Columns)
                {
                    selectValues.Add(exprColumnName);
                }

                if (additionalMaps != null)
                {
                    foreach (var m in additionalMaps)
                    {
                        selectValues.Add(m.Value);
                    }
                }

                IExprQuery query;
                var queryBuilder = SqQueryBuilder.Select(selectValues).From(values);

                if (checkExistence && this._checkExistenceByColumns != null)
                {

                    var tbl = this._target.WithAlias(new ExprTableAlias(Alias.Auto.BuildAliasExpression()!));

                    var existsFilter = !SqQueryBuilder.Exists(SqQueryBuilder
                        .SelectOne()
                        .From(tbl)
                        .Where(this._checkExistenceByColumns
                            .Select(column => column.WithSource(tbl.Alias) == column.WithSource(values.Alias))
                            .JoinAsAnd()));

                    query = queryBuilder.Where(existsFilter).Done();
                }
                else
                {
                    query = queryBuilder.Done();
                }

                insertSource = new ExprInsertQuery(query);

                if (additionalMaps != null)
                {
                    var extraInsertCols = additionalMaps.SelectToReadOnlyList(m => m.Column);
                    columns = Helpers.Combine(columns, extraInsertCols);
                }

            }
            else
            {
                //Actually C# should have detected that this brunch cannot be invoked
                throw new SqExpressException("Fatal logic error!");
            }

            return new ExprInsert(this._target.FullName, columns, insertSource);
        }

        public DataTable ToDataTable()
        {
            var mapping = this._dataMapping.AssertFatalNotNull(nameof(this._dataMapping));

            int? capacity = this._data.TryToCheckLength(out var c) ? c : null;

            if (capacity != null && capacity.Value < 1)
            {
                throw new SqExpressException(InputDataShouldNotBeEmpty);
            }

            DataMapSetter<TTable, TItem>? dataMapSetter = null;
            IReadOnlyList<ExprColumn>? columns = null;

            DataTable? result = null;

            foreach (var item in this._data)
            {
                dataMapSetter ??= new DataMapSetter<TTable, TItem>(this._target, item);

                dataMapSetter.NextItem(item, columns?.Count);
                mapping(dataMapSetter);
                dataMapSetter.EnsureRecordLength();

                var record = dataMapSetter.Record.AssertFatalNotNull(nameof(dataMapSetter.Record));

                if (columns == null)
                {
                    result = new DataTable(this._target.FullName.TableName);
                    columns = dataMapSetter.Columns;
                    for (var index = 0; index < columns.Count; index++)
                    {
                        var targetColumn = columns[index];
                        var targetTableColumn = targetColumn as TableColumn;
                        if (targetTableColumn is null && this._target is TableBase tableBase)
                        {
                            targetTableColumn =
                                tableBase.Columns.FirstOrDefault(x => x.ColumnName.Equals(targetColumn.ColumnName));
                        }

                        if (targetTableColumn is null)
                        {
                            record[index].Accept()
                        }
                    }
                }


                recordsS?.Add(new ExprValueRow(dataMapSetter.Record.AssertFatalNotNull(nameof(dataMapSetter.Record))));
                recordsI?.Add(new ExprInsertValueRow(dataMapSetter.Record.AssertFatalNotNull(nameof(dataMapSetter.Record))));
            }

            if ( (recordsS?.Count ?? 0 + recordsI?.Count ?? 0) < 1 || columns == null)
            {
                //In case of empty IEnumerable
                throw new SqExpressException(InputDataShouldNotBeEmpty);
            }

        }

        ExprIdentityInsert IIdentityInsertDataBuilderFinal.Done()
        {
            var insertExpr = this.Done();
            return new ExprIdentityInsert(insertExpr, IdentityInsertBuilder.ExprColumnNames(this._target));
        }

        public IIdentityInsertDataBuilderFinal IdentityInsert()
        {
            return this;
        }

        public IInsertDataBuilderFinalOutput Output(ExprAliasedColumnName column, params ExprAliasedColumnName[] rest)
        {
            this._output.AssertFatalNull(nameof(this._output));
            this._output = Helpers.Combine(column, rest);
            return this;
        }

        public IInsertDataBuilderFinalOutput Output(IReadOnlyList<ExprAliasedColumnName> columns)
        {
            this._output.AssertFatalNull(nameof(this._output));
            this._output = columns;
            return this;
        }

        public IInsertDataBuilderMapOutput Where(Func<IExprColumnSource, ExprBoolean> dataFilter)
        {
            throw new NotImplementedException();
        }

        ExprInsertOutput IInsertDataBuilderFinalOutput.Done()
        {
            var output = this._output.AssertFatalNotNull(nameof(this._output));
            var insert = this.Done();

            return new ExprInsertOutput(insert, output);
        }

        IExprQuery IExprQueryFinal.Done()
        {
            return ((IInsertDataBuilderFinalOutput) this).Done();
        }

        IExprExec IExprExecFinal.Done()
        {
            return this.Done();
        }

        public IInsertDataBuilderMapOutput CheckExistenceBy(ExprColumn column, params ExprColumn[] rest)
        {
            return this.CheckExistenceBy(Helpers.Combine(column, rest));
        }

        public IInsertDataBuilderMapOutput CheckExistenceBy(IReadOnlyList<ExprColumn> columns)
        {
            this._checkExistenceByColumns = columns;
            return this;
        }
    }
}