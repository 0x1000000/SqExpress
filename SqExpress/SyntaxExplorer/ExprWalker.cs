using System;
using System.Collections;
using System.Collections.Generic;
using SqExpress.Syntax;
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

namespace SqExpress.SyntaxExplorer
{
    public readonly struct VisitorResult<TCtx>
    {
        public readonly TCtx Context;

        public readonly bool IsStop;

        private VisitorResult(TCtx context, bool isTop)
        {
            this.Context = context;
            this.IsStop = isTop;
        }

        public static VisitorResult<TCtx> Continue(TCtx value) => new VisitorResult<TCtx>(value, false);

        public static VisitorResult<TCtx> Stop(TCtx value) => new VisitorResult<TCtx>(value, true);
    }

    internal class ExprWalker<TCtx> : IExprVisitor<bool, TCtx>
    {
        private readonly Func<IExpr, TCtx, VisitorResult<TCtx>> _visitor;

        public ExprWalker(Func<IExpr, TCtx, VisitorResult<TCtx>> visitor)
        {
            this._visitor = visitor;
        }

        private bool Visit(IExpr expr, TCtx ctx, out TCtx ctxOut)
        {
            var result = this._visitor.Invoke(expr, ctx);
            ctxOut = result.Context;
            return !result.IsStop;
        }

        private bool Accept(IExpr? expr, TCtx context)
        {
            return expr == null || expr.Accept(this, context);
        }

        private bool Accept(IReadOnlyList<IExpr>? exprs, TCtx context)
        {
            if (exprs == null)
            {
                return true;
            }

            for (int i = 0; i < exprs.Count; i++)
            {
                if (!this.Accept(exprs[i], context))
                {
                    return false;
                }
            }
            return true;
        }

        public bool VisitExprBoolLiteral(ExprBoolLiteral expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprByteArrayLiteral(ExprByteArrayLiteral expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprByteLiteral(ExprByteLiteral expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprDateTimeLiteral(ExprDateTimeLiteral expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprDecimalLiteral(ExprDecimalLiteral expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprDefault(ExprDefault expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprDoubleLiteral(ExprDoubleLiteral expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprGuidLiteral(ExprGuidLiteral expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprInt16Literal(ExprInt16Literal expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprInt32Literal(ExprInt32Literal expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprInt64Literal(ExprInt64Literal expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprNull(ExprNull expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprStringLiteral(ExprStringLiteral expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprColumnSetClause(ExprColumnSetClause expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Column, argOut) && this.Accept(expr.Value, argOut);
        }
        public bool VisitExprDelete(ExprDelete expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Target, argOut) && this.Accept(expr.Source, argOut) && this.Accept(expr.Filter, argOut);
        }
        public bool VisitExprDeleteOutput(ExprDeleteOutput expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Delete, argOut) && this.Accept(expr.OutputColumns, argOut);
        }
        public bool VisitExprInsert(ExprInsert expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Target, argOut) && this.Accept(expr.TargetColumns, argOut) && this.Accept(expr.Source, argOut);
        }
        public bool VisitExprInsertOutput(ExprInsertOutput expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Insert, argOut) && this.Accept(expr.OutputColumns, argOut);
        }
        public bool VisitExprInsertValues(ExprInsertValues expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Values, argOut);
        }
        public bool VisitExprInsertQuery(ExprInsertQuery expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Query, argOut);
        }
        public bool VisitExprMerge(ExprMerge expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.TargetTable, argOut) && this.Accept(expr.Source, argOut) && this.Accept(expr.On, argOut) && this.Accept(expr.WhenMatched, argOut) && this.Accept(expr.WhenNotMatchedByTarget, argOut) && this.Accept(expr.WhenNotMatchedBySource, argOut);
        }
        public bool VisitExprMergeOutput(ExprMergeOutput expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.TargetTable, argOut) && this.Accept(expr.Source, argOut) && this.Accept(expr.On, argOut) && this.Accept(expr.WhenMatched, argOut) && this.Accept(expr.WhenNotMatchedByTarget, argOut) && this.Accept(expr.WhenNotMatchedBySource, argOut) && this.Accept(expr.Output, argOut);
        }
        public bool VisitExprMergeMatchedUpdate(ExprMergeMatchedUpdate expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.And, argOut) && this.Accept(expr.Set, argOut);
        }
        public bool VisitExprMergeMatchedDelete(ExprMergeMatchedDelete expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.And, argOut);
        }
        public bool VisitExprExprMergeNotMatchedInsert(ExprExprMergeNotMatchedInsert expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.And, argOut) && this.Accept(expr.Columns, argOut) && this.Accept(expr.Values, argOut);
        }
        public bool VisitExprExprMergeNotMatchedInsertDefault(ExprExprMergeNotMatchedInsertDefault expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.And, argOut);
        }
        public bool VisitExprUpdate(ExprUpdate expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Target, argOut) && this.Accept(expr.SetClause, argOut) && this.Accept(expr.Source, argOut) && this.Accept(expr.Filter, argOut);
        }
        public bool VisitExprCast(ExprCast expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Expression, argOut) && this.Accept(expr.SqlType, argOut);
        }
        public bool VisitExprTypeBoolean(ExprTypeBoolean expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprTypeByte(ExprTypeByte expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprTypeInt16(ExprTypeInt16 expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprTypeInt32(ExprTypeInt32 expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprTypeInt64(ExprTypeInt64 expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprTypeDecimal(ExprTypeDecimal expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprTypeDouble(ExprTypeDouble expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprTypeDateTime(ExprTypeDateTime expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprTypeGuid(ExprTypeGuid expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprTypeString(ExprTypeString expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprCrossedTable(ExprCrossedTable expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Left, argOut) && this.Accept(expr.Right, argOut);
        }
        public bool VisitExprDerivedTableValues(ExprDerivedTableValues expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Values, argOut) && this.Accept(expr.Alias, argOut) && this.Accept(expr.Columns, argOut);
        }
        public bool VisitExprDerivedTableQuery(ExprDerivedTableQuery expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Query, argOut) && this.Accept(expr.Alias, argOut) && this.Accept(expr.Columns, argOut);
        }
        public bool VisitExprJoinedTable(ExprJoinedTable expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Left, argOut) && this.Accept(expr.Right, argOut) && this.Accept(expr.SearchCondition, argOut);
        }
        public bool VisitExprOrderBy(ExprOrderBy expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.OrderList, argOut);
        }
        public bool VisitExprOrderByOffsetFetch(ExprOrderByOffsetFetch expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.OrderList, argOut) && this.Accept(expr.OffsetFetch, argOut);
        }
        public bool VisitExprOrderByItem(ExprOrderByItem expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Value, argOut);
        }
        public bool VisitExprOffsetFetch(ExprOffsetFetch expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Offset, argOut) && this.Accept(expr.Fetch, argOut);
        }
        public bool VisitExprQueryExpression(ExprQueryExpression expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Left, argOut) && this.Accept(expr.Right, argOut);
        }
        public bool VisitExprQuerySpecification(ExprQuerySpecification expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.SelectList, argOut) && this.Accept(expr.Top, argOut) && this.Accept(expr.From, argOut) && this.Accept(expr.Where, argOut) && this.Accept(expr.GroupBy, argOut);
        }
        public bool VisitExprRowValue(ExprRowValue expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Items, argOut);
        }
        public bool VisitExprSelect(ExprSelect expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.SelectQuery, argOut) && this.Accept(expr.OrderBy, argOut);
        }
        public bool VisitExprSelectOffsetFetch(ExprSelectOffsetFetch expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.SelectQuery, argOut) && this.Accept(expr.OrderBy, argOut);
        }
        public bool VisitExprTableValueConstructor(ExprTableValueConstructor expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Items, argOut);
        }
        public bool VisitExprAliasedColumn(ExprAliasedColumn expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Column, argOut) && this.Accept(expr.Alias, argOut);
        }
        public bool VisitExprAliasedColumnName(ExprAliasedColumnName expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Column, argOut) && this.Accept(expr.Alias, argOut);
        }
        public bool VisitExprAliasedSelecting(ExprAliasedSelecting expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Value, argOut) && this.Accept(expr.Alias, argOut);
        }
        public bool VisitExprOutput(ExprOutput expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Columns, argOut);
        }
        public bool VisitExprOutputColumnInserted(ExprOutputColumnInserted expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.ColumnName, argOut);
        }
        public bool VisitExprOutputColumnDeleted(ExprOutputColumnDeleted expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.ColumnName, argOut);
        }
        public bool VisitExprOutputColumn(ExprOutputColumn expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Column, argOut);
        }
        public bool VisitExprOutputAction(ExprOutputAction expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Alias, argOut);
        }
        public bool VisitExprAlias(ExprAlias expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprAliasGuid(ExprAliasGuid expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprColumn(ExprColumn expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Source, argOut) && this.Accept(expr.ColumnName, argOut);
        }
        public bool VisitExprColumnAlias(ExprColumnAlias expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprColumnName(ExprColumnName expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprFunctionName(ExprFunctionName expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprSchemaName(ExprSchemaName expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprTable(ExprTable expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.FullName, argOut) && this.Accept(expr.Alias, argOut);
        }
        public bool VisitExprTableAlias(ExprTableAlias expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Alias, argOut);
        }
        public bool VisitExprTableFullName(ExprTableFullName expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Schema, argOut) && this.Accept(expr.TableName, argOut);
        }
        public bool VisitExprTableName(ExprTableName expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprAggregateFunction(ExprAggregateFunction expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Name, argOut) && this.Accept(expr.Expression, argOut);
        }
        public bool VisitExprAnalyticFunction(ExprAnalyticFunction expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Name, argOut) && this.Accept(expr.Arguments, argOut) && this.Accept(expr.Over, argOut);
        }
        public bool VisitExprCase(ExprCase expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Cases, argOut) && this.Accept(expr.DefaultValue, argOut);
        }
        public bool VisitExprCaseWhenThen(ExprCaseWhenThen expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Condition, argOut) && this.Accept(expr.Value, argOut);
        }
        public bool VisitExprOver(ExprOver expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Partitions, argOut) && this.Accept(expr.OrderBy, argOut);
        }
        public bool VisitExprScalarFunction(ExprScalarFunction expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Schema, argOut) && this.Accept(expr.Name, argOut) && this.Accept(expr.Arguments, argOut);
        }
        public bool VisitExprFuncCoalesce(ExprFuncCoalesce expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Test, argOut) && this.Accept(expr.Alts, argOut);
        }
        public bool VisitExprFuncIsNull(ExprFuncIsNull expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Test, argOut) && this.Accept(expr.Alt, argOut);
        }
        public bool VisitExprGetDate(ExprGetDate expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprGetUtcDate(ExprGetUtcDate expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut);
        }
        public bool VisitExprDiv(ExprDiv expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Left, argOut) && this.Accept(expr.Right, argOut);
        }
        public bool VisitExprMul(ExprMul expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Left, argOut) && this.Accept(expr.Right, argOut);
        }
        public bool VisitExprSub(ExprSub expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Left, argOut) && this.Accept(expr.Right, argOut);
        }
        public bool VisitExprSum(ExprSum expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Left, argOut) && this.Accept(expr.Right, argOut);
        }
        public bool VisitExprStringConcat(ExprStringConcat expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Left, argOut) && this.Accept(expr.Right, argOut);
        }
        public bool VisitExprBooleanAnd(ExprBooleanAnd expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Left, argOut) && this.Accept(expr.Right, argOut);
        }
        public bool VisitExprBooleanNot(ExprBooleanNot expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Expr, argOut);
        }
        public bool VisitExprBooleanOr(ExprBooleanOr expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Left, argOut) && this.Accept(expr.Right, argOut);
        }
        public bool VisitExprBooleanEq(ExprBooleanEq expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Left, argOut) && this.Accept(expr.Right, argOut);
        }
        public bool VisitExprBooleanGt(ExprBooleanGt expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Left, argOut) && this.Accept(expr.Right, argOut);
        }
        public bool VisitExprBooleanGtEq(ExprBooleanGtEq expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Left, argOut) && this.Accept(expr.Right, argOut);
        }
        public bool VisitExprBooleanLt(ExprBooleanLt expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Left, argOut) && this.Accept(expr.Right, argOut);
        }
        public bool VisitExprBooleanLtEq(ExprBooleanLtEq expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Left, argOut) && this.Accept(expr.Right, argOut);
        }
        public bool VisitExprBooleanNotEq(ExprBooleanNotEq expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Left, argOut) && this.Accept(expr.Right, argOut);
        }
        public bool VisitExprExists(ExprExists expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.SubQuery, argOut);
        }
        public bool VisitExprInSubQuery(ExprInSubQuery expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.TestExpression, argOut) && this.Accept(expr.SubQuery, argOut);
        }
        public bool VisitExprInValues(ExprInValues expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.TestExpression, argOut) && this.Accept(expr.Items, argOut);
        }
        public bool VisitExprLike(ExprLike expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Test, argOut) && this.Accept(expr.Pattern, argOut);
        }
        public bool VisitExprIsNull(ExprIsNull expr, TCtx arg)
        {
            return this.Visit(expr, arg, out var argOut) && this.Accept(expr.Test, argOut);
        }
    }
}