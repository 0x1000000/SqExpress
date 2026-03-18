using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select.SelectItems;

namespace SqExpress.Syntax.Select;

public interface IExprSelectingVisitor<out TRes, in TArg> : IExprValueVisitor<TRes, TArg>
{
    TRes VisitExprAllColumns(ExprAllColumns exprAllColumns, TArg arg);

    TRes VisitExprColumnName(ExprColumnName columnName, TArg arg);

    TRes VisitExprAliasedColumn(ExprAliasedColumn exprAliasedColumn, TArg arg);

    TRes VisitExprAliasedColumnName(ExprAliasedColumnName exprAliasedColumnName, TArg arg);

    TRes VisitExprAliasedSelecting(ExprAliasedSelecting exprAliasedSelecting, TArg arg);

    TRes VisitExprAggregateFunction(ExprAggregateFunction exprAggregateFunction, TArg arg);

    TRes VisitExprAggregateOverFunction(ExprAggregateOverFunction exprAggregateFunction, TArg arg);

    TRes VisitExprAnalyticFunction(ExprAnalyticFunction exprAnalyticFunction, TArg arg);
}
