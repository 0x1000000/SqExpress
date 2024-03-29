﻿using System;
using System.Collections.Generic;
using System.Linq;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;

namespace SqExpress.QueryBuilders.Insert
{
    public readonly struct InsertBuilder
    {
        private readonly ExprTable _table;

        private readonly IReadOnlyList<ExprColumnName> _columns;

        public InsertBuilder(ExprTable table, IReadOnlyList<ExprColumnName> columns)
        {
            this._table = table;
            this._columns = columns;
        }

        public ExprInsert From(IExprQueryFinal query) => this.From(query.Done());

        public ExprInsert From(IExprQuery query)
        {
            return new ExprInsert(this._table.FullName, this._columns, new ExprInsertQuery(query));
        }

        public ExprInsert Values(ExprInsertValues values)
        {
            return new ExprInsert(this._table.FullName, this._columns, values);
        }

        public ExprInsert Values(IEnumerable<IReadOnlyList<ExprValue>> values)
        {
            var rows = BuildInsertValues(values: values);
            return new ExprInsert(this._table.FullName, this._columns, new ExprInsertValues(rows));
        }

        public ValuesBuilder Values(params ExprValue[] values)
        {
            return new ValuesBuilder(this, new List<ExprValue[]>()).Values(values);
        }

        internal static List<ExprInsertValueRow> BuildInsertValues(IEnumerable<IReadOnlyList<ExprValue>> values)
        {
            int? capacity = values is IReadOnlyCollection<IReadOnlyList<ExprValue>> collection ? collection.Count : null;
            if (capacity != null && capacity.Value < 1)
            {
                throw new SqExpressException("Input data should not be empty");
            }

            List<ExprInsertValueRow> rows = capacity.HasValue
                ? new List<ExprInsertValueRow>(capacity.Value)
                : new List<ExprInsertValueRow>();
            int? colCount = null;
            foreach (var row in values)
            {
                if (colCount == null)
                {
                    colCount = row.Count;
                }
                else
                {
                    if (colCount.Value != row.Count)
                    {
                        throw new SqExpressException(
                            $"All rows should have the same number of columns ({colCount.Value},{row.Count})");
                    }
                }

                rows.Add(new ExprInsertValueRow(row));
            }

            return rows;
        }

        public readonly struct ValuesBuilder
        {
            private readonly InsertBuilder _insertBuilder;
            private readonly List<ExprValue[]> _valuesAcc;

            internal ValuesBuilder(InsertBuilder insertBuilder, List<ExprValue[]> valuesAcc)
            {
                this._insertBuilder = insertBuilder;
                this._valuesAcc = valuesAcc;
            }

            public ValuesBuilder Values(params ExprValue[] values)
            {
                this._valuesAcc.Add(values);
                return this;
            }

            public ExprInsert DoneWithValues()
            {
                var rows = BuildInsertValues(this._valuesAcc);
                return new ExprInsert(this._insertBuilder._table.FullName, this._insertBuilder._columns, new ExprInsertValues(rows));
            }
        }
    }

    public readonly struct IdentityInsertBuilder
    {
        private readonly ExprTable _table;

        private readonly IReadOnlyList<ExprColumnName> _columns;

        public IdentityInsertBuilder(ExprTable table, IReadOnlyList<ExprColumnName> columns)
        {
            this._table = table;
            this._columns = columns;
        }

        public ExprIdentityInsert From(IExprQueryFinal query) => this.From(query.Done());

        public ExprIdentityInsert From(IExprQuery query)
        {
            var exprInsert = new ExprInsert(this._table.FullName, this._columns, new ExprInsertQuery(query));
            return new ExprIdentityInsert(exprInsert, this.IdentityColumns());
        }

        public ExprIdentityInsert Values(ExprInsertValues values)
        {
            var exprInsert = new ExprInsert(this._table.FullName, this._columns, values);
            return new ExprIdentityInsert(exprInsert, this.IdentityColumns());
        }

        public ExprIdentityInsert Values(IEnumerable<IReadOnlyList<ExprValue>> values)
        {
            var rows = InsertBuilder.BuildInsertValues(values: values);
            var exprInsert = new ExprInsert(this._table.FullName, this._columns, new ExprInsertValues(rows));
            return new ExprIdentityInsert(exprInsert, this.IdentityColumns());
        }

        public ValuesBuilder Values(params ExprValue[] values)
        {
            return new ValuesBuilder(this, new List<ExprValue[]>()).Values(values);
        }

        private IReadOnlyList<ExprColumnName> IdentityColumns()
        {
            var exprTable = this._table;
            return ExprColumnNames(exprTable: exprTable);
        }

        internal static IReadOnlyList<ExprColumnName> ExprColumnNames(ExprTable? exprTable)
        {
            if (exprTable is TableBase tableBase)
            {
                return tableBase.Columns.Where(c => c.ColumnMeta?.IsIdentity ?? false)
                    .Select(c => c.ColumnName)
                    .ToList();
            }

            return new ExprColumnName[0];
        }

        public readonly struct ValuesBuilder
        {
            private readonly IdentityInsertBuilder _insertBuilder;
            private readonly List<ExprValue[]> _valuesAcc;

            internal ValuesBuilder(IdentityInsertBuilder insertBuilder, List<ExprValue[]> valuesAcc)
            {
                this._insertBuilder = insertBuilder;
                this._valuesAcc = valuesAcc;
            }

            public ValuesBuilder Values(params ExprValue[] values)
            {
                this._valuesAcc.Add(values);
                return this;
            }

            public ExprIdentityInsert DoneWithValues()
            {
                var rows = InsertBuilder.BuildInsertValues(this._valuesAcc);
                var exprInsert = new ExprInsert(this._insertBuilder._table.FullName, this._insertBuilder._columns, new ExprInsertValues(rows));
                return new ExprIdentityInsert(exprInsert, this._insertBuilder.IdentityColumns());
            }
        }
    }
}