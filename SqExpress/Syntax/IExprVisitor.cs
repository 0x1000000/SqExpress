using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Internal;
using SqExpress.Syntax.Json;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Output;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax
{
    internal interface IExprVisitorInternal<out TRes, in TArg> : IExprVisitor<TRes, TArg>, IExprValueVisitor<TRes, TArg>
    {
        TRes VisitExprStatement(ExprStatement statement, TArg arg);
    }

    /// <summary>
    /// Defines a value-returning expression visitor that receives a caller-provided argument for each dispatch.
    /// </summary>
    /// <typeparam name="TRes">The value returned from each visitor callback.</typeparam>
    /// <typeparam name="TArg">The argument supplied to each visitor callback.</typeparam>
    /// <remarks>
    /// This is the generic visitor contract for algorithms that compute a result or explicitly propagate context.
    /// Implementations must handle every expression type. For a conventional recursive, read-only traversal where
    /// only selected node callbacks need customization, prefer
    /// <see cref="SyntaxTreeOperations.ExprVisitorBase"/>.
    /// </remarks>
    public interface IExprVisitor<out TRes,in TArg> : IExprSelectingVisitor<TRes, TArg>, IExprTypeVisitor<TRes, TArg>
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
        TRes VisitExprOver(ExprOver exprOver, TArg arg);

        TRes VisitExprFrameClause(ExprFrameClause exprFrameClause, TArg arg);

        TRes VisitExprValueFrameBorder(ExprValueFrameBorder exprValueFrameBorder, TArg arg);

        TRes VisitExprCurrentRowFrameBorder(ExprCurrentRowFrameBorder exprCurrentRowFrameBorder, TArg arg);

        TRes VisitExprUnboundedFrameBorder(ExprUnboundedFrameBorder exprUnboundedFrameBorder, TArg arg);

        //Meta
        TRes VisitExprTable(ExprTable exprTable, TArg arg);

        TRes VisitExprTableName(ExprTableName tableName, TArg arg);

        TRes VisitExprTableFullName(ExprTableFullName exprTableFullName, TArg arg);

        TRes VisitExprAlias(ExprAlias alias, TArg arg);

        TRes VisitExprAliasGuid(ExprAliasGuid aliasGuid, TArg arg);

        TRes VisitExprColumnAlias(ExprColumnAlias exprColumnAlias, TArg arg);

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

        // JSON
        /// <summary>Visits a JSON object member.</summary>
        /// <param name="exprJsonMember">The node to visit.</param><param name="arg">The visitor argument.</param><returns>The visitor result.</returns>
        TRes VisitExprJsonMember(ExprJsonMember exprJsonMember, TArg arg);

        /// <summary>Visits a typed JSON table scalar column.</summary>
        /// <param name="exprJsonTableValueColumn">The node to visit.</param><param name="arg">The visitor argument.</param><returns>The visitor result.</returns>
        TRes VisitExprJsonTableValueColumn(ExprJsonTableValueColumn exprJsonTableValueColumn, TArg arg);

        /// <summary>Visits a JSON table fragment column.</summary>
        /// <param name="exprJsonTableQueryColumn">The node to visit.</param><param name="arg">The visitor argument.</param><returns>The visitor result.</returns>
        TRes VisitExprJsonTableQueryColumn(ExprJsonTableQueryColumn exprJsonTableQueryColumn, TArg arg);

        /// <summary>Visits a JSON table ordinal column.</summary>
        /// <param name="exprJsonTableOrdinalColumn">The node to visit.</param><param name="arg">The visitor argument.</param><returns>The visitor result.</returns>
        TRes VisitExprJsonTableOrdinalColumn(ExprJsonTableOrdinalColumn exprJsonTableOrdinalColumn, TArg arg);

        /// <summary>Visits a JSON table source.</summary>
        /// <param name="exprJsonTable">The node to visit.</param><param name="arg">The visitor argument.</param><returns>The visitor result.</returns>
        TRes VisitExprJsonTable(ExprJsonTable exprJsonTable, TArg arg);

        /// <summary>Visits a relational query serialized as JSON.</summary>
        /// <param name="exprQueryAsJson">The node to visit.</param><param name="arg">The visitor argument.</param><returns>The visitor result.</returns>
        TRes VisitExprQueryAsJson(ExprQueryAsJson exprQueryAsJson, TArg arg);
    }
}
