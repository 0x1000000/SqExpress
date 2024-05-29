using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Internal;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Output;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax
{
    internal interface IExprVisitorInternal<out TRes, in TArg> : IExprVisitor<TRes, TArg>
    {
        TRes VisitExprStatement(ExprStatement statement, TArg arg);
    }

    public interface IExprVisitor<out TRes,in TArg> : IExprValueVisitor<TRes, TArg>, IExprTypeVisitor<TRes, TArg>
    {
        //Boolean Expressions
        TRes VisitExprBooleanAnd(ExprBooleanAnd expr, TArg arg);

        TRes VisitExprBooleanOr(ExprBooleanOr expr, TArg arg);

        TRes VisitExprBooleanNot(ExprBooleanNot expr, TArg arg);

        //Boolean Predicates
        TRes VisitExprBooleanNotEq(ExprBooleanNotEq exprBooleanNotEq, TArg arg);

        TRes VisitExprBooleanEq(ExprBooleanEq exprBooleanEq, TArg arg);

        TRes VisitExprBooleanGt(ExprBooleanGt booleanGt, TArg arg);

        TRes VisitExprBooleanGtEq(ExprBooleanGtEq booleanGtEq, TArg arg);

        TRes VisitExprBooleanLt(ExprBooleanLt booleanLt, TArg arg);

        TRes VisitExprBooleanLtEq(ExprBooleanLtEq booleanLtEq, TArg arg);

        //Boolean Predicates - Others
        TRes VisitExprInSubQuery(ExprInSubQuery exprInSubQuery, TArg arg);

        TRes VisitExprInValues(ExprInValues exprInValues, TArg arg);

        TRes VisitExprExists(ExprExists exprExists, TArg arg);

        TRes VisitExprIsNull(ExprIsNull exprIsNull, TArg arg);

        TRes VisitExprLike(ExprLike exprLike, TArg arg);

        TRes VisitExprDefault(ExprDefault exprDefault, TArg arg);

        //Select
        TRes VisitExprQuerySpecification(ExprQuerySpecification exprQuerySpecification, TArg arg);

        TRes VisitExprJoinedTable(ExprJoinedTable joinedTable, TArg arg);

        TRes VisitExprCrossedTable(ExprCrossedTable exprCrossedTable, TArg arg);

        TRes VisitExprLateralCrossedTable(ExprLateralCrossedTable exprCrossedTable, TArg arg);

        TRes VisitExprQueryExpression(ExprQueryExpression exprQueryExpression, TArg arg);

        TRes VisitExprSelect(ExprSelect exprSelect, TArg arg);

        TRes VisitExprSelectOffsetFetch(ExprSelectOffsetFetch exprSelectOffsetFetch, TArg arg);

        TRes VisitExprOrderBy(ExprOrderBy exprOrderBy, TArg arg);

        TRes VisitExprOrderByOffsetFetch(ExprOrderByOffsetFetch exprOrderByOffsetFetch, TArg arg);

        TRes VisitExprOrderByItem(ExprOrderByItem exprOrderByItem, TArg arg);

        TRes VisitExprOffsetFetch(ExprOffsetFetch exprOffsetFetch, TArg arg);

        //Select Output
        TRes VisitExprOutputColumnInserted(ExprOutputColumnInserted exprOutputColumnInserted, TArg arg);

        TRes VisitExprOutputColumnDeleted(ExprOutputColumnDeleted exprOutputColumnDeleted, TArg arg);

        TRes VisitExprOutputColumn(ExprOutputColumn exprOutputColumn, TArg arg);

        TRes VisitExprOutputAction(ExprOutputAction exprOutputAction, TArg arg);

        TRes VisitExprOutput(ExprOutput exprOutput, TArg arg);

        //Functions
        TRes VisitExprAggregateFunction(ExprAggregateFunction exprAggregateFunction, TArg arg);

        TRes VisitExprAnalyticFunction(ExprAnalyticFunction exprAnalyticFunction, TArg arg);

        TRes VisitExprOver(ExprOver exprOver, TArg arg);

        TRes VisitExprFrameClause(ExprFrameClause exprFrameClause, TArg arg);

        TRes VisitExprValueFrameBorder(ExprValueFrameBorder exprValueFrameBorder, TArg arg);

        TRes VisitExprCurrentRowFrameBorder(ExprCurrentRowFrameBorder exprCurrentRowFrameBorder, TArg arg);

        TRes VisitExprUnboundedFrameBorder(ExprUnboundedFrameBorder exprUnboundedFrameBorder, TArg arg);

        //Meta
        TRes VisitExprTable(ExprTable exprTable, TArg arg);

        TRes VisitExprAllColumns(ExprAllColumns exprAllColumns, TArg arg);

        TRes VisitExprColumnName(ExprColumnName columnName, TArg arg);

        TRes VisitExprTableName(ExprTableName tableName, TArg arg);

        TRes VisitExprTableFullName(ExprTableFullName exprTableFullName, TArg arg);

        TRes VisitExprAlias(ExprAlias alias, TArg arg);

        TRes VisitExprAliasGuid(ExprAliasGuid aliasGuid, TArg arg);

        TRes VisitExprColumnAlias(ExprColumnAlias exprColumnAlias, TArg arg);

        TRes VisitExprAliasedColumn(ExprAliasedColumn exprAliasedColumn, TArg arg);

        TRes VisitExprAliasedColumnName(ExprAliasedColumnName exprAliasedColumnName, TArg arg);

        TRes VisitExprAliasedSelecting(ExprAliasedSelecting exprAliasedSelecting, TArg arg);

        TRes VisitExprTempTableName(ExprTempTableName tempTableName, TArg arg);

        TRes VisitExprTableAlias(ExprTableAlias tableAlias, TArg arg);

        TRes VisitExprSchemaName(ExprSchemaName schemaName, TArg arg);

        TRes VisitExprDatabaseName(ExprDatabaseName databaseName, TArg arg);

        TRes VisitExprDbSchema(ExprDbSchema exprDbSchema, TArg arg);

        TRes VisitExprFunctionName(ExprFunctionName exprFunctionName, TArg arg);

        TRes VisitExprValueRow(ExprValueRow valueRow, TArg arg);

        TRes VisitExprTableValueConstructor(ExprTableValueConstructor tableValueConstructor, TArg arg);

        TRes VisitExprDerivedTableQuery(ExprDerivedTableQuery exprDerivedTableQuery, TArg arg);

        TRes VisitExprDerivedTableValues(ExprDerivedTableValues derivedTableValues, TArg arg);

        TRes VisitExprCteQuery(ExprCteQuery exprCte, TArg arg);

        TRes VisitExprTableFunction(ExprTableFunction exprTableFunction, TArg arg);

        TRes VisitExprAliasedTableFunction(ExprAliasedTableFunction exprTableFunction, TArg arg);

        TRes VisitExprColumnSetClause(ExprColumnSetClause columnSetClause, TArg arg);

        //Merge
        TRes VisitExprMerge(ExprMerge merge, TArg arg);

        TRes VisitExprMergeOutput(ExprMergeOutput mergeOutput, TArg arg);

        TRes VisitExprMergeMatchedUpdate(ExprMergeMatchedUpdate mergeMatchedUpdate, TArg arg);

        TRes VisitExprMergeMatchedDelete(ExprMergeMatchedDelete mergeMatchedDelete, TArg arg);

        TRes VisitExprExprMergeNotMatchedInsert(ExprExprMergeNotMatchedInsert exprMergeNotMatchedInsert, TArg arg);

        TRes VisitExprExprMergeNotMatchedInsertDefault(ExprExprMergeNotMatchedInsertDefault exprExprMergeNotMatchedInsertDefault, TArg arg);

        //Insert
        TRes VisitExprInsert(ExprInsert exprInsert, TArg arg);

        TRes VisitExprInsertOutput(ExprInsertOutput exprInsertOutput, TArg arg);

        TRes VisitExprInsertValues(ExprInsertValues exprInsertValues, TArg arg);

        TRes VisitExprInsertValueRow(ExprInsertValueRow exprInsertValueRow, TArg arg);

        TRes VisitExprInsertQuery(ExprInsertQuery exprInsertQuery, TArg arg);

        TRes VisitExprIdentityInsert(ExprIdentityInsert exprIdentityInsert, TArg arg);

        //Update
        TRes VisitExprUpdate(ExprUpdate exprUpdate, TArg arg);

        //Delete
        TRes VisitExprDelete(ExprDelete exprDelete, TArg arg);

        TRes VisitExprDeleteOutput(ExprDeleteOutput exprDeleteOutput, TArg arg);

        //List of expressions
        TRes VisitExprList(ExprList exprList, TArg arg);

        TRes VisitExprQueryList(ExprQueryList exprList, TArg arg);
    }
}