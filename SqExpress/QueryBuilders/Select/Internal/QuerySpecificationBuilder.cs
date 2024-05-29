using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress.QueryBuilders.Select.Internal
{
    internal class QuerySpecificationBuilder : QueryExpressionBuilderBase, IQuerySpecificationBuilder
    {
        private readonly ExprValue? _top;
        private readonly bool _distinct;
        private readonly IReadOnlyList<IExprSelecting> _selectList;
        private IExprTableSource? _from;
        private ExprBoolean? _where;
        private IReadOnlyList<ExprColumn>? _groupBy;

        public QuerySpecificationBuilder(ExprValue? top, bool distinct, IReadOnlyList<IExprSelecting> selectList)
        {
            this._top = top;
            this._distinct = distinct;
            this._selectList = selectList;
        }

        public IQuerySpecificationBuilderJoin From(IExprTableSource tableSource)
        {
            this._from.AssertFatalNull(nameof(this._from));
            this._from = tableSource;
            return this;
        }

        public new ExprQuerySpecification Done()
        {
            return new ExprQuerySpecification(this._selectList,
                this._top,
                this._distinct,
                this._from,
                this._where,
                this._groupBy);
        }

        public IQuerySpecificationBuilderJoin InnerJoin(IExprTableSource join, ExprBoolean on)
        {
            this._from = new ExprJoinedTable(this._from.AssertFatalNotNull(nameof(this._from)), ExprJoinedTable.ExprJoinType.Inner, join, on);
            return this;
        }

        public IQuerySpecificationBuilderJoin LeftJoin(IExprTableSource join, ExprBoolean on)
        {
            this._from = new ExprJoinedTable(this._from.AssertFatalNotNull(nameof(this._from)), ExprJoinedTable.ExprJoinType.Left, join, on);
            return this;
        }

        public IQuerySpecificationBuilderJoin FullJoin(IExprTableSource join, ExprBoolean on)
        {
            this._from = new ExprJoinedTable(this._from.AssertFatalNotNull(nameof(this._from)), ExprJoinedTable.ExprJoinType.Full, join, on);
            return this;
        }

        public IQuerySpecificationBuilderJoin CrossJoin(IExprTableSource join)
        {
            this._from = new ExprCrossedTable(this._from.AssertFatalNotNull(nameof(this._from)), join);
            return this;
        }

        public IQuerySpecificationBuilderJoin CrossApply(IExprTableSource join)
        {
            this._from = new ExprLateralCrossedTable(this._from.AssertFatalNotNull(nameof(this._from)), join, false);
            return this;
        }

        public IQuerySpecificationBuilderJoin OuterApply(IExprTableSource join)
        {
            this._from = new ExprLateralCrossedTable(this._from.AssertFatalNotNull(nameof(this._from)), join, true);
            return this;
        }

        public IQuerySpecificationBuilderFiltered Where(ExprBoolean? where)
        {
            this._where.AssertFatalNull(nameof(this._where));
            this._where = where;
            return this;
        }

        public IQuerySpecificationBuilderFinal GroupBy(ExprColumn column, params ExprColumn[] otherColumns)
        {
            this._groupBy.AssertFatalNull(nameof(this._groupBy));
            this._groupBy = Helpers.Combine(column, otherColumns);
            return this;
        }

        public IQuerySpecificationBuilderFinal GroupBy(ExprColumn column1, ExprColumn column2, params ExprColumn[] otherColumns)
        {
            this._groupBy.AssertFatalNull(nameof(this._groupBy));
            this._groupBy = Helpers.Combine(column1, column2, otherColumns);
            return this;
        }

        public IQuerySpecificationBuilderFinal GroupBy(IReadOnlyList<ExprColumn> columns)
        {
            this._groupBy.AssertFatalNull(nameof(this._groupBy));
            this._groupBy = columns.AssertNotEmpty("Grouping column list in cannot be empty.");
            return this;
        }

        public ISelectBuilder OrderBy(ExprOrderBy orderBy)
            => this.OrderByInternal(orderBy);

        public ISelectBuilder OrderBy(ExprOrderByItem item, params ExprOrderByItem[] rest)
            => this.OrderByInternal(item, rest);

        public ISelectBuilder OrderBy(IReadOnlyList<ExprOrderByItem> orderItems)
            => this.OrderByInternal(orderItems);

        protected override IExprQueryExpression BuildQueryExpression() 
            => this.Done();
    }
}