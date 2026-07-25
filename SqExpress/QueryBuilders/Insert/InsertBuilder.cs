using System;
using System.Collections.Generic;
using System.Linq;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;

namespace SqExpress.QueryBuilders.Insert
{
    /// <summary>Builds a regular insert from a query, value rows, or an existing values expression.</summary>
    public readonly struct InsertBuilder
    {
        private readonly ExprTable _table;

        private readonly IReadOnlyList<ExprColumnName> _columns;

        /// <summary>Initializes an insert builder for a target table and ordered target-column list.</summary>
        public InsertBuilder(ExprTable table, IReadOnlyList<ExprColumnName> columns)
        {
            this._table = table;
            this._columns = columns;
        }

        /// <summary>Completes an insert-from-query statement from a fluent query builder.</summary>
        public ExprInsert From(IExprQueryFinal query) => this.From(query.Done());

        /// <summary>Completes an insert-from-query statement from an existing query expression.</summary>
        public ExprInsert From(IExprQuery query)
        {
            return new ExprInsert(this._table.FullName, this._columns, new ExprInsertQuery(query));
        }

        /// <summary>Completes an insert using an existing values expression.</summary>
        public ExprInsert Values(ExprInsertValues values)
        {
            return new ExprInsert(this._table.FullName, this._columns, values);
        }

        /// <summary>Completes a multi-row insert from a sequence of equally sized value rows.</summary>
        public ExprInsert Values(IEnumerable<IReadOnlyList<ExprValue>> values)
        {
            var rows = BuildInsertValues(values: values);
            return new ExprInsert(this._table.FullName, this._columns, new ExprInsertValues(rows));
        }

        /// <summary>Starts a fluent multi-row values list with its first row.</summary>
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

        /// <summary>Accumulates value rows for a regular insert.</summary>
        public readonly struct ValuesBuilder
        {
            private readonly InsertBuilder _insertBuilder;
            private readonly List<ExprValue[]> _valuesAcc;

            internal ValuesBuilder(InsertBuilder insertBuilder, List<ExprValue[]> valuesAcc)
            {
                this._insertBuilder = insertBuilder;
                this._valuesAcc = valuesAcc;
            }

            /// <summary>Appends another value row.</summary>
            public ValuesBuilder Values(params ExprValue[] values)
            {
                this._valuesAcc.Add(values);
                return this;
            }

            /// <summary>Completes the insert after validating that all rows have equal width.</summary>
            public ExprInsert DoneWithValues()
            {
                var rows = BuildInsertValues(this._valuesAcc);
                return new ExprInsert(this._insertBuilder._table.FullName, this._insertBuilder._columns, new ExprInsertValues(rows));
            }
        }
    }

    /// <summary>Builds an insert that explicitly supplies identity-column values.</summary>
    public readonly struct IdentityInsertBuilder
    {
        private readonly ExprTable _table;

        private readonly IReadOnlyList<ExprColumnName> _columns;

        /// <summary>Initializes an identity insert for a target table and ordered target-column list.</summary>
        public IdentityInsertBuilder(ExprTable table, IReadOnlyList<ExprColumnName> columns)
        {
            this._table = table;
            this._columns = columns;
        }

        /// <summary>Completes an identity insert from a fluent query builder.</summary>
        public ExprIdentityInsert From(IExprQueryFinal query) => this.From(query.Done());

        /// <summary>Completes an identity insert from an existing query expression.</summary>
        public ExprIdentityInsert From(IExprQuery query)
        {
            var exprInsert = new ExprInsert(this._table.FullName, this._columns, new ExprInsertQuery(query));
            return new ExprIdentityInsert(exprInsert, this.IdentityColumns());
        }

        /// <summary>Completes an identity insert using an existing values expression.</summary>
        public ExprIdentityInsert Values(ExprInsertValues values)
        {
            var exprInsert = new ExprInsert(this._table.FullName, this._columns, values);
            return new ExprIdentityInsert(exprInsert, this.IdentityColumns());
        }

        /// <summary>Completes a multi-row identity insert from equally sized value rows.</summary>
        public ExprIdentityInsert Values(IEnumerable<IReadOnlyList<ExprValue>> values)
        {
            var rows = InsertBuilder.BuildInsertValues(values: values);
            var exprInsert = new ExprInsert(this._table.FullName, this._columns, new ExprInsertValues(rows));
            return new ExprIdentityInsert(exprInsert, this.IdentityColumns());
        }

        /// <summary>Starts a fluent identity-insert values list with its first row.</summary>
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

        /// <summary>Accumulates value rows for an identity insert.</summary>
        public readonly struct ValuesBuilder
        {
            private readonly IdentityInsertBuilder _insertBuilder;
            private readonly List<ExprValue[]> _valuesAcc;

            internal ValuesBuilder(IdentityInsertBuilder insertBuilder, List<ExprValue[]> valuesAcc)
            {
                this._insertBuilder = insertBuilder;
                this._valuesAcc = valuesAcc;
            }

            /// <summary>Appends another value row.</summary>
            public ValuesBuilder Values(params ExprValue[] values)
            {
                this._valuesAcc.Add(values);
                return this;
            }

            /// <summary>Completes the identity insert after validating equal row width.</summary>
            public ExprIdentityInsert DoneWithValues()
            {
                var rows = InsertBuilder.BuildInsertValues(this._valuesAcc);
                var exprInsert = new ExprInsert(this._insertBuilder._table.FullName, this._insertBuilder._columns, new ExprInsertValues(rows));
                return new ExprIdentityInsert(exprInsert, this._insertBuilder.IdentityColumns());
            }
        }
    }
}
