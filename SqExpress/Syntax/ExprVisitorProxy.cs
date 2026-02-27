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
    internal interface IExprVisitorNodeHandler
    {
        void OnEnterNode(IExpr expr);
        void OnLeaveNode();
    }

    internal sealed class ExprVisitorProxy : IExprVisitor<object?, object?>
    {
        private readonly IExprVisitor _visitor;
        private readonly IExprVisitorNodeHandler? _nodeHandler;

        public ExprVisitorProxy(IExprVisitor visitor)
        {
            this._visitor = visitor;
            this._nodeHandler = visitor as IExprVisitorNodeHandler;
        }

        //CodeGenStart
        public object? VisitExprAggregateFunction(ExprAggregateFunction expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprAggregateFunction(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprAggregateOverFunction(ExprAggregateOverFunction expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprAggregateOverFunction(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprAlias(ExprAlias expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprAlias(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprAliasGuid(ExprAliasGuid expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprAliasGuid(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprAliasedColumn(ExprAliasedColumn expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprAliasedColumn(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprAliasedColumnName(ExprAliasedColumnName expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprAliasedColumnName(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprAliasedSelecting(ExprAliasedSelecting expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprAliasedSelecting(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprAliasedTableFunction(ExprAliasedTableFunction expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprAliasedTableFunction(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprAllColumns(ExprAllColumns expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprAllColumns(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprAnalyticFunction(ExprAnalyticFunction expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprAnalyticFunction(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprBitwiseAnd(ExprBitwiseAnd expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprBitwiseAnd(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprBitwiseNot(ExprBitwiseNot expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprBitwiseNot(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprBitwiseOr(ExprBitwiseOr expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprBitwiseOr(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprBitwiseXor(ExprBitwiseXor expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprBitwiseXor(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprBoolLiteral(ExprBoolLiteral expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprBoolLiteral(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprBooleanAnd(ExprBooleanAnd expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprBooleanAnd(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprBooleanEq(ExprBooleanEq expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprBooleanEq(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprBooleanGt(ExprBooleanGt expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprBooleanGt(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprBooleanGtEq(ExprBooleanGtEq expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprBooleanGtEq(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprBooleanLt(ExprBooleanLt expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprBooleanLt(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprBooleanLtEq(ExprBooleanLtEq expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprBooleanLtEq(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprBooleanNot(ExprBooleanNot expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprBooleanNot(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprBooleanNotEq(ExprBooleanNotEq expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprBooleanNotEq(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprBooleanOr(ExprBooleanOr expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprBooleanOr(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprByteArrayLiteral(ExprByteArrayLiteral expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprByteArrayLiteral(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprByteLiteral(ExprByteLiteral expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprByteLiteral(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprCase(ExprCase expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprCase(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprCaseWhenThen(ExprCaseWhenThen expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprCaseWhenThen(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprCast(ExprCast expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprCast(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprColumn(ExprColumn expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprColumn(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprColumnAlias(ExprColumnAlias expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprColumnAlias(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprColumnName(ExprColumnName expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprColumnName(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprColumnSetClause(ExprColumnSetClause expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprColumnSetClause(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprCrossedTable(ExprCrossedTable expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprCrossedTable(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprCteQuery(ExprCteQuery expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprCteQuery(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprCurrentRowFrameBorder(ExprCurrentRowFrameBorder expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprCurrentRowFrameBorder(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprDatabaseName(ExprDatabaseName expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprDatabaseName(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprDateAdd(ExprDateAdd expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprDateAdd(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprDateDiff(ExprDateDiff expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprDateDiff(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprDateTimeLiteral(ExprDateTimeLiteral expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprDateTimeLiteral(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprDateTimeOffsetLiteral(ExprDateTimeOffsetLiteral expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprDateTimeOffsetLiteral(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprDbSchema(ExprDbSchema expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprDbSchema(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprDecimalLiteral(ExprDecimalLiteral expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprDecimalLiteral(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprDefault(ExprDefault expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprDefault(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprDelete(ExprDelete expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprDelete(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprDeleteOutput(ExprDeleteOutput expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprDeleteOutput(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprDerivedTableQuery(ExprDerivedTableQuery expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprDerivedTableQuery(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprDerivedTableValues(ExprDerivedTableValues expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprDerivedTableValues(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprDiv(ExprDiv expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprDiv(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprDoubleLiteral(ExprDoubleLiteral expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprDoubleLiteral(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprExists(ExprExists expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprExists(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprExprMergeNotMatchedInsert(ExprExprMergeNotMatchedInsert expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprExprMergeNotMatchedInsert(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprExprMergeNotMatchedInsertDefault(ExprExprMergeNotMatchedInsertDefault expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprExprMergeNotMatchedInsertDefault(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprFrameClause(ExprFrameClause expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprFrameClause(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprFuncCoalesce(ExprFuncCoalesce expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprFuncCoalesce(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprFuncIsNull(ExprFuncIsNull expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprFuncIsNull(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprFunctionName(ExprFunctionName expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprFunctionName(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprGetDate(ExprGetDate expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprGetDate(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprGetUtcDate(ExprGetUtcDate expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprGetUtcDate(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprGuidLiteral(ExprGuidLiteral expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprGuidLiteral(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprIdentityInsert(ExprIdentityInsert expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprIdentityInsert(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprInSubQuery(ExprInSubQuery expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprInSubQuery(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprInValues(ExprInValues expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprInValues(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprInsert(ExprInsert expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprInsert(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprInsertOutput(ExprInsertOutput expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprInsertOutput(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprInsertQuery(ExprInsertQuery expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprInsertQuery(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprInsertValueRow(ExprInsertValueRow expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprInsertValueRow(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprInsertValues(ExprInsertValues expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprInsertValues(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprInt16Literal(ExprInt16Literal expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprInt16Literal(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprInt32Literal(ExprInt32Literal expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprInt32Literal(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprInt64Literal(ExprInt64Literal expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprInt64Literal(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprIsNull(ExprIsNull expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprIsNull(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprJoinedTable(ExprJoinedTable expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprJoinedTable(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprLateralCrossedTable(ExprLateralCrossedTable expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprLateralCrossedTable(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprLike(ExprLike expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprLike(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprList(ExprList expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprList(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprMerge(ExprMerge expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprMerge(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprMergeMatchedDelete(ExprMergeMatchedDelete expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprMergeMatchedDelete(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprMergeMatchedUpdate(ExprMergeMatchedUpdate expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprMergeMatchedUpdate(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprMergeOutput(ExprMergeOutput expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprMergeOutput(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprModulo(ExprModulo expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprModulo(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprMul(ExprMul expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprMul(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprNull(ExprNull expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprNull(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprOffsetFetch(ExprOffsetFetch expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprOffsetFetch(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprOrderBy(ExprOrderBy expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprOrderBy(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprOrderByItem(ExprOrderByItem expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprOrderByItem(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprOrderByOffsetFetch(ExprOrderByOffsetFetch expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprOrderByOffsetFetch(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprOutput(ExprOutput expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprOutput(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprOutputAction(ExprOutputAction expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprOutputAction(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprOutputColumn(ExprOutputColumn expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprOutputColumn(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprOutputColumnDeleted(ExprOutputColumnDeleted expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprOutputColumnDeleted(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprOutputColumnInserted(ExprOutputColumnInserted expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprOutputColumnInserted(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprOver(ExprOver expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprOver(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprQueryExpression(ExprQueryExpression expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprQueryExpression(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprQueryList(ExprQueryList expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprQueryList(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprQuerySpecification(ExprQuerySpecification expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprQuerySpecification(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprScalarFunction(ExprScalarFunction expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprScalarFunction(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprSchemaName(ExprSchemaName expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprSchemaName(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprSelect(ExprSelect expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprSelect(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprSelectOffsetFetch(ExprSelectOffsetFetch expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprSelectOffsetFetch(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprStringConcat(ExprStringConcat expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprStringConcat(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprStringLiteral(ExprStringLiteral expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprStringLiteral(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprSub(ExprSub expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprSub(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprSum(ExprSum expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprSum(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTable(ExprTable expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTable(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTableAlias(ExprTableAlias expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTableAlias(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTableFullName(ExprTableFullName expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTableFullName(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTableFunction(ExprTableFunction expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTableFunction(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTableName(ExprTableName expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTableName(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTableValueConstructor(ExprTableValueConstructor expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTableValueConstructor(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTempTableName(ExprTempTableName expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTempTableName(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTypeBoolean(ExprTypeBoolean expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTypeBoolean(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTypeByte(ExprTypeByte expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTypeByte(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTypeByteArray(ExprTypeByteArray expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTypeByteArray(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTypeDateTime(ExprTypeDateTime expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTypeDateTime(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTypeDateTimeOffset(ExprTypeDateTimeOffset expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTypeDateTimeOffset(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTypeDecimal(ExprTypeDecimal expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTypeDecimal(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTypeDouble(ExprTypeDouble expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTypeDouble(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTypeFixSizeByteArray(ExprTypeFixSizeByteArray expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTypeFixSizeByteArray(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTypeFixSizeString(ExprTypeFixSizeString expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTypeFixSizeString(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTypeGuid(ExprTypeGuid expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTypeGuid(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTypeInt16(ExprTypeInt16 expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTypeInt16(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTypeInt32(ExprTypeInt32 expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTypeInt32(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTypeInt64(ExprTypeInt64 expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTypeInt64(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTypeString(ExprTypeString expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTypeString(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprTypeXml(ExprTypeXml expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprTypeXml(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprUnboundedFrameBorder(ExprUnboundedFrameBorder expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprUnboundedFrameBorder(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprUnsafeValue(ExprUnsafeValue expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprUnsafeValue(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprUpdate(ExprUpdate expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprUpdate(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprValueFrameBorder(ExprValueFrameBorder expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprValueFrameBorder(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprValueQuery(ExprValueQuery expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprValueQuery(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        public object? VisitExprValueRow(ExprValueRow expr, object? arg)
        {
            this._nodeHandler?.OnEnterNode(expr);
            try
            {
                this._visitor.VisitExprValueRow(expr);
                return null;
            }
            finally
            {
                this._nodeHandler?.OnLeaveNode();
            }
        }
        //CodeGenEnd
    }

    public static class ExprVisitorExtensions
    {
        public static void Accept(this IExpr expr, IExprVisitor visitor)
        {
            expr.Accept(new ExprVisitorProxy(visitor), null);
        }
    }
}
