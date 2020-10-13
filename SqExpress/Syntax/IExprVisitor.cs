using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Output;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax
{
    public interface IExprVisitor<out TRes>
    {
        //Boolean Expressions
        TRes VisitExprBooleanAnd(ExprBooleanAnd expr);

        //Boolean Predicates
        TRes VisitExprBooleanOr(ExprBooleanOr expr);

        TRes VisitExprBooleanNot(ExprBooleanNot expr);

        TRes VisitExprBooleanNotEq(ExprBooleanNotEq exprBooleanNotEq);

        TRes VisitExprBooleanEq(ExprBooleanEq exprBooleanEq);

        TRes VisitExprBooleanGt(ExprBooleanGt booleanGt);

        TRes VisitExprBooleanGtEq(ExprBooleanGtEq booleanGtEq);

        TRes VisitExprBooleanLt(ExprBooleanLt booleanLt);

        TRes VisitExprBooleanLtEq(ExprBooleanLtEq booleanLtEq);

        //Boolean Predicates - Others
        TRes VisitExprInSubQuery(ExprInSubQuery exprInSubQuery);

        TRes VisitExprInValues(ExprInValues exprInValues);

        TRes VisitExprExists(ExprExists exprExists);

        TRes VisitExprIsNull(ExprIsNull exprIsNull);

        TRes VisitExprLike(ExprLike exprLike);

        //Value
        TRes VisitExprIntLiteral(ExprInt32Literal exprInt32Literal);

        TRes VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral);

        TRes VisitExprStringLiteral(ExprStringLiteral stringLiteral);

        TRes VisitExprDateTimeLiteral(ExprDateTimeLiteral dateTimeLiteral);

        TRes VisitExprBoolLiteral(ExprBoolLiteral boolLiteral);

        TRes VisitExprLongLiteral(ExprInt64Literal int64Literal);

        TRes VisitExprByteLiteral(ExprByteLiteral byteLiteral);

        TRes VisitExprShortLiteral(ExprInt16Literal int16Literal);

        TRes VisitExprDecimalLiteral(ExprDecimalLiteral decimalLiteral);

        TRes VisitExprDoubleLiteral(ExprDoubleLiteral doubleLiteral);

        TRes VisitExprByteArrayLiteral(ExprByteArrayLiteral byteArrayLiteral);

        TRes VisitExprNull(ExprNull exprNull);

        TRes VisitExprDefault(ExprDefault exprDefault);

        //Arithmetic Expressions
        TRes VisitExprSum(ExprSum exprSum);

        TRes VisitExprSub(ExprSub exprSub);

        TRes VisitExprMul(ExprMul exprMul);

        TRes VisitExprDiv(ExprDiv exprDiv);

        TRes VisitExprStringConcat(ExprStringConcat exprStringConcat);

        //Select
        TRes VisitExprQuerySpecification(ExprQuerySpecification exprQuerySpecification);

        TRes VisitExprJoinedTable(ExprJoinedTable joinedTable);

        TRes VisitExprCrossedTable(ExprCrossedTable exprCrossedTable);

        TRes VisitExprQueryExpression(ExprQueryExpression exprQueryExpression);

        TRes VisitExprSelect(ExprSelect exprSelect);

        TRes VisitExprSelectOffsetFetch(ExprSelectOffsetFetch exprSelectOffsetFetch);

        TRes VisitExprOrderBy(ExprOrderBy exprOrderBy);

        TRes VisitExprOrderByOffsetFetch(ExprOrderByOffsetFetch exprOrderByOffsetFetch);

        TRes VisitExprOrderByItem(ExprOrderByItem exprOrderByItem);

        TRes VisitExprOffsetFetch(ExprOffsetFetch exprOffsetFetch);

        //Select Output
        TRes VisitExprOutPutColumnInserted(ExprOutputColumnInserted exprOutputColumnInserted);

        TRes VisitExprOutPutColumnDeleted(ExprOutputColumnDeleted exprOutputColumnDeleted);

        TRes VisitExprOutPutColumn(ExprOutputColumn exprOutputColumn);

        TRes VisitExprOutPutAction(ExprOutputAction exprOutputAction);

        TRes VisitExprOutPut(ExprOutput exprOutput);

        //Functions
        TRes VisitExprAggregateFunction(ExprAggregateFunction exprAggregateFunction);

        TRes VisitExprScalarFunction(ExprScalarFunction exprScalarFunction);

        TRes VisitExprAggregateAnalyticFunction(ExprAnalyticFunction exprAnalyticFunction);

        TRes VisitExprOver(ExprOver exprOver);

        TRes VisitExprCase(ExprCase exprCase);

        TRes VisitExprCaseWhenThen(ExprCaseWhenThen exprCaseWhenThen);

        //Meta
        TRes VisitExprColumn(ExprColumn exprColumn);

        TRes VisitExprTable(ExprTable exprTable);

        TRes VisitExprColumnName(ExprColumnName columnName);

        TRes VisitExprTableName(ExprTableName tableName);

        TRes VisitExprTableFullName(ExprTableFullName exprTableFullName);

        TRes VisitExprAlias(ExprAlias alias);

        TRes VisitExprAliasGuid(ExprAliasGuid aliasGuid);

        TRes VisitExprColumnAlias(ExprColumnAlias exprColumnAlias);

        TRes VisitExprAliasedColumn(ExprAliasedColumn exprAliasedColumn);

        TRes VisitExprAliasedColumnName(ExprAliasedColumnName exprAliasedColumnName);

        TRes VisitExprAliasedSelectItem(ExprAliasedSelecting exprAliasedSelecting);

        TRes VisitExprTableAlias(ExprTableAlias tableAlias);

        TRes VisitExprSchemaName(ExprSchemaName schemaName);

        TRes VisitExprFunctionName(ExprFunctionName exprFunctionName);

        TRes VisitExprRowValue(ExprRowValue rowValue);

        TRes VisitExprTableValueConstructor(ExprTableValueConstructor tableValueConstructor);

        TRes VisitExprDerivedTableQuery(ExprDerivedTableQuery exprDerivedTableQuery);

        TRes VisitDerivedTableValues(ExprDerivedTableValues derivedTableValues);

        TRes VisitExprColumnSetClause(ExprColumnSetClause columnSetClause);

        //Merge
        TRes VisitExprMerge(ExprMerge merge);

        TRes VisitExprMergeOutput(ExprMergeOutput mergeOutput);

        TRes VisitExprMergeMatchedUpdate(ExprMergeMatchedUpdate mergeMatchedUpdate);

        TRes VisitExprMergeMatchedDelete(ExprMergeMatchedDelete mergeMatchedDelete);

        TRes VisitExprExprMergeNotMatchedInsert(ExprExprMergeNotMatchedInsert exprMergeNotMatchedInsert);

        TRes VisitExprExprMergeNotMatchedInsertDefault(ExprExprMergeNotMatchedInsertDefault exprExprMergeNotMatchedInsertDefault);

        //Insert
        TRes VisitExprInsert(ExprInsert exprInsert);

        TRes VisitExprInsertOutput(ExprInsertOutput exprInsertOutput);

        TRes VisitExprInsertValues(ExprInsertValues exprInsertValues);

        TRes VisitExprInsertQuery(ExprInsertQuery exprInsertQuery);

        //Update
        TRes VisitExprUpdate(ExprUpdate exprUpdate);

        //Delete
        TRes VisitExprDelete(ExprDelete exprDelete);

        TRes VisitExprDeleteOutput(ExprDeleteOutput exprDeleteOutput);

        //Types
        TRes VisitExprCast(ExprCast exprCast);

        TRes VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean);

        TRes VisitExprTypeByte(ExprTypeByte exprTypeByte);

        TRes VisitExprTypeInt16(ExprTypeInt16 exprTypeInt16);

        TRes VisitExprTypeInt32(ExprTypeInt32 exprTypeInt32);

        TRes VisitExprTypeInt64(ExprTypeInt64 exprTypeInt64);

        TRes VisitExprTypeDecimal(ExprTypeDecimal exprTypeDecimal);

        TRes VisitExprTypeDouble(ExprTypeDouble exprTypeDouble);

        TRes VisitExprTypeDateTime(ExprTypeDateTime exprTypeDateTime);

        TRes VisitExprTypeGuid(ExprTypeGuid exprTypeGuid);

        TRes VisitExprTypeString(ExprTypeString exprTypeString);

        TRes VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull);

        TRes VisitExprFuncCoalesce(ExprFuncCoalesce exprFuncCoalesce);

        TRes VisitExprFuncGetDate(ExprGetDate exprGetDate);

        TRes VisitExprFuncGetUtcDate(ExprGetUtcDate exprGetUtcDate);
    }
}