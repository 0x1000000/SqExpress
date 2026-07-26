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

        /// <summary>Initializes an insert builder whose source values must match the target-column order.</summary>
        /// <param name="table">The table receiving inserted rows.</param>
        /// <param name="columns">The non-empty target columns in source projection/value order.</param>
        public InsertBuilder(ExprTable table, IReadOnlyList<ExprColumnName> columns)
        {
            this._table = table;
            this._columns = columns;
        }

        /// <summary>Uses a fluent query's projection as the rows inserted into the target columns.</summary>
        /// <param name="query">The final query stage; its projected column count and types must be compatible with the target list.</param>
        /// <returns>A completed insert-from-query statement.</returns>
        public ExprInsert From(IExprQueryFinal query) => this.From(query.Done());

        /// <summary>Uses an existing query's projection as the rows inserted into the target columns.</summary>
        /// <param name="query">The source query whose projection order corresponds to the target columns.</param>
        /// <returns>A completed insert-from-query statement.</returns>
        public ExprInsert From(IExprQuery query)
        {
            return new ExprInsert(this._table.FullName, this._columns, new ExprInsertQuery(query));
        }

        /// <summary>Uses a prebuilt SQL values source for the insert.</summary>
        /// <param name="values">The values expression whose row width must match the target-column count.</param>
        /// <returns>A completed insert statement.</returns>
        public ExprInsert Values(ExprInsertValues values)
        {
            return new ExprInsert(this._table.FullName, this._columns, values);
        }

        /// <summary>Materializes a sequence of rows and creates a multi-row values insert.</summary>
        /// <param name="values">Non-empty rows of equal width, ordered to match the target columns.</param>
        /// <returns>A completed multi-row insert statement.</returns>
        public ExprInsert Values(IEnumerable<IReadOnlyList<ExprValue>> values)
        {
            var rows = BuildInsertValues(values: values);
            return new ExprInsert(this._table.FullName, this._columns, new ExprInsertValues(rows));
        }

        /// <summary>Starts fluent accumulation of values rows, beginning with the supplied row.</summary>
        /// <param name="values">The first row, ordered to match the target columns.</param>
        /// <returns>A values accumulator that accepts more rows or completes the insert.</returns>
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

            /// <summary>Appends a row; equal-width validation occurs when the insert is completed.</summary>
            /// <param name="values">The next row in target-column order.</param>
            /// <returns>The updated accumulator.</returns>
            public ValuesBuilder Values(params ExprValue[] values)
            {
                this._valuesAcc.Add(values);
                return this;
            }

            /// <summary>Validates equal row widths and materializes the accumulated rows as an insert statement.</summary>
            /// <returns>The completed multi-row insert syntax tree.</returns>
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

        /// <summary>Initializes an insert that explicitly enables values for identity columns declared in table metadata.</summary>
        /// <param name="table">The table receiving inserted rows.</param>
        /// <param name="columns">Target columns in source projection/value order, including explicitly supplied identity columns.</param>
        public IdentityInsertBuilder(ExprTable table, IReadOnlyList<ExprColumnName> columns)
        {
            this._table = table;
            this._columns = columns;
        }

        /// <summary>Uses a fluent query as the source of an identity-value insert.</summary>
        /// <param name="query">The source query whose projection corresponds to the target columns.</param>
        /// <returns>A completed identity-insert statement.</returns>
        public ExprIdentityInsert From(IExprQueryFinal query) => this.From(query.Done());

        /// <summary>Uses an existing query as the source of an identity-value insert.</summary>
        /// <param name="query">The source query whose projection corresponds to the target columns.</param>
        /// <returns>A completed identity-insert statement.</returns>
        public ExprIdentityInsert From(IExprQuery query)
        {
            var exprInsert = new ExprInsert(this._table.FullName, this._columns, new ExprInsertQuery(query));
            return new ExprIdentityInsert(exprInsert, this.IdentityColumns());
        }

        /// <summary>Uses a prebuilt values source while enabling explicit identity-column insertion.</summary>
        /// <param name="values">The values expression in target-column order.</param>
        /// <returns>A completed identity-insert statement.</returns>
        public ExprIdentityInsert Values(ExprInsertValues values)
        {
            var exprInsert = new ExprInsert(this._table.FullName, this._columns, values);
            return new ExprIdentityInsert(exprInsert, this.IdentityColumns());
        }

        /// <summary>Materializes equally sized rows as an identity-value insert.</summary>
        /// <param name="values">Non-empty rows ordered to match the target columns.</param>
        /// <returns>A completed multi-row identity-insert statement.</returns>
        public ExprIdentityInsert Values(IEnumerable<IReadOnlyList<ExprValue>> values)
        {
            var rows = InsertBuilder.BuildInsertValues(values: values);
            var exprInsert = new ExprInsert(this._table.FullName, this._columns, new ExprInsertValues(rows));
            return new ExprIdentityInsert(exprInsert, this.IdentityColumns());
        }

        /// <summary>Starts fluent accumulation of identity-insert value rows.</summary>
        /// <param name="values">The first row in target-column order.</param>
        /// <returns>An accumulator that accepts more rows or completes the statement.</returns>
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

            /// <summary>Appends an identity-insert row; equal-width validation is deferred until completion.</summary>
            /// <param name="values">The next row in target-column order.</param>
            /// <returns>The updated accumulator.</returns>
            public ValuesBuilder Values(params ExprValue[] values)
            {
                this._valuesAcc.Add(values);
                return this;
            }

            /// <summary>Validates equal row widths and materializes the accumulated identity-insert rows.</summary>
            /// <returns>The completed identity-insert syntax tree.</returns>
            public ExprIdentityInsert DoneWithValues()
            {
                var rows = InsertBuilder.BuildInsertValues(this._valuesAcc);
                var exprInsert = new ExprInsert(this._insertBuilder._table.FullName, this._insertBuilder._columns, new ExprInsertValues(rows));
                return new ExprIdentityInsert(exprInsert, this._insertBuilder.IdentityColumns());
            }
        }
    }
}
