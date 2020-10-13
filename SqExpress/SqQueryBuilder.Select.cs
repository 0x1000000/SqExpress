using System.Collections.Generic;
using SqExpress.QueryBuilders.Select;
using SqExpress.QueryBuilders.Select.Internal;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress
{
    public partial class SqQueryBuilder
    {
        public static IQuerySpecificationBuilderInitial Select(IReadOnlyList<IExprSelecting> selection) 
            => new QuerySpecificationBuilder(null, false, selection);

        public static IQuerySpecificationBuilderInitial Select(IExprSelecting selection, params IExprSelecting[] selections) 
            => new QuerySpecificationBuilder(null, false, Helpers.Combine(selection, selections));

        public static IQuerySpecificationBuilderInitial SelectOne()
            => new QuerySpecificationBuilder(null, false, new[] { Literal(1) });

        public static IQuerySpecificationBuilderInitial SelectDistinct(IExprSelecting selection, params IExprSelecting[] selections)
            => new QuerySpecificationBuilder(null, true, Helpers.Combine(selection, selections));

        public static IQuerySpecificationBuilderInitial SelectTop(int top, IExprSelecting selection, params IExprSelecting[] selections)
            => new QuerySpecificationBuilder(Literal(top), false, Helpers.Combine(selection, selections));

        public static IQuerySpecificationBuilderInitial SelectTop(ExprValue top, IExprSelecting selection, params IExprSelecting[] selections)
            => new QuerySpecificationBuilder(top, false, Helpers.Combine(selection, selections));

        public static IQuerySpecificationBuilderInitial SelectTopDistinct(int top, IExprSelecting selection, params IExprSelecting[] selections)
            => new QuerySpecificationBuilder(Literal(top), true, Helpers.Combine(selection, selections));

        public static IQuerySpecificationBuilderInitial SelectTopDistinct(ExprValue top, IExprSelecting selection, params IExprSelecting[] selections)
            => new QuerySpecificationBuilder(top, true, Helpers.Combine(selection, selections));

        public static IQuerySpecificationBuilderInitial SelectTopOne()
            => new QuerySpecificationBuilder(Literal(1), false, new[] { Literal(1) });

        public static ExprOrderByItem Asc(ExprValue value)=>new ExprOrderByItem(value, false);

        public static ExprOrderByItem Desc(ExprValue value)=>new ExprOrderByItem(value, true);
    }
}