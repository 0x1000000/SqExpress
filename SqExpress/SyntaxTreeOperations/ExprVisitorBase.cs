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

namespace SqExpress.SyntaxTreeOperations
{
    public abstract class ExprVisitorBase : IExprVisitor, IExprVisitorNodeHandler
    {
        private readonly IExprVisitor<object?, object?> _proxy;
        private readonly List<IExpr> _currentPath = new List<IExpr>();
        private HashSet<string>? _cteChecker;

        protected ExprVisitorBase()
        {
            this._proxy = new ExprVisitorProxy(this);
        }

        public IReadOnlyList<IExpr> CurrentPath => this._currentPath;

        public IExpr? CurrentNode => this._currentPath.Count > 0
            ? this._currentPath[this._currentPath.Count - 1]
            : null;

        public int Depth => this._currentPath.Count - 1;

        void IExprVisitorNodeHandler.OnEnterNode(IExpr expr)
        {
            this._currentPath.Add(expr);
        }

        void IExprVisitorNodeHandler.OnLeaveNode()
        {
            if (this._currentPath.Count > 0)
            {
                this._currentPath.RemoveAt(this._currentPath.Count - 1);
            }
        }

        protected virtual void Accept(IExpr? expr)
        {
            if (expr != null)
            {
                expr.Accept(this._proxy, null);
            }
        }

        protected virtual void Accept(IReadOnlyList<IExpr>? expressions)
        {
            if (expressions == null)
            {
                return;
            }

            for (var i = 0; i < expressions.Count; i++)
            {
                this.Accept(expressions[i]);
            }
        }

        public virtual void VisitExprCteQuery(ExprCteQuery expr)
        {
            this.Accept(expr.Alias);

            this._cteChecker ??= new HashSet<string>();
            if (!this._cteChecker.Contains(expr.Name))
            {
                this._cteChecker.Add(expr.Name);
                this.Accept(expr.Query);
                this._cteChecker.Remove(expr.Name);
            }
        }

        public virtual void VisitExprDerivedTableQuery(ExprDerivedTableQuery expr)
        {
            this.Accept(expr.Query);
            this.Accept(expr.Alias);
            this.Accept(expr.Columns);
        }

        //CodeGenStart
        public virtual void VisitExprAggregateFunction(ExprAggregateFunction expr)
        {
            this.Accept(expr.Name);
            this.Accept(expr.Expression);
        }
        public virtual void VisitExprAggregateOverFunction(ExprAggregateOverFunction expr)
        {
            this.Accept(expr.Function);
            this.Accept(expr.Over);
        }
        public virtual void VisitExprAlias(ExprAlias expr)
        {
            
        }
        public virtual void VisitExprAliasGuid(ExprAliasGuid expr)
        {
            
        }
        public virtual void VisitExprAliasedColumn(ExprAliasedColumn expr)
        {
            this.Accept(expr.Column);
            this.Accept(expr.Alias);
        }
        public virtual void VisitExprAliasedColumnName(ExprAliasedColumnName expr)
        {
            this.Accept(expr.Column);
            this.Accept(expr.Alias);
        }
        public virtual void VisitExprAliasedSelecting(ExprAliasedSelecting expr)
        {
            this.Accept(expr.Value);
            this.Accept(expr.Alias);
        }
        public virtual void VisitExprAliasedTableFunction(ExprAliasedTableFunction expr)
        {
            this.Accept(expr.Function);
            this.Accept(expr.Alias);
        }
        public virtual void VisitExprAllColumns(ExprAllColumns expr)
        {
            this.Accept(expr.Source);
        }
        public virtual void VisitExprAnalyticFunction(ExprAnalyticFunction expr)
        {
            this.Accept(expr.Name);
            this.Accept(expr.Arguments);
            this.Accept(expr.Over);
        }
        public virtual void VisitExprBitwiseAnd(ExprBitwiseAnd expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprBitwiseNot(ExprBitwiseNot expr)
        {
            this.Accept(expr.Value);
        }
        public virtual void VisitExprBitwiseOr(ExprBitwiseOr expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprBitwiseXor(ExprBitwiseXor expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprBoolLiteral(ExprBoolLiteral expr)
        {
            
        }
        public virtual void VisitExprBooleanAnd(ExprBooleanAnd expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprBooleanEq(ExprBooleanEq expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprBooleanGt(ExprBooleanGt expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprBooleanGtEq(ExprBooleanGtEq expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprBooleanLt(ExprBooleanLt expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprBooleanLtEq(ExprBooleanLtEq expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprBooleanNot(ExprBooleanNot expr)
        {
            this.Accept(expr.Expr);
        }
        public virtual void VisitExprBooleanNotEq(ExprBooleanNotEq expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprBooleanOr(ExprBooleanOr expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprByteArrayLiteral(ExprByteArrayLiteral expr)
        {
            
        }
        public virtual void VisitExprByteLiteral(ExprByteLiteral expr)
        {
            
        }
        public virtual void VisitExprCase(ExprCase expr)
        {
            this.Accept(expr.Cases);
            this.Accept(expr.DefaultValue);
        }
        public virtual void VisitExprCaseWhenThen(ExprCaseWhenThen expr)
        {
            this.Accept(expr.Condition);
            this.Accept(expr.Value);
        }
        public virtual void VisitExprCast(ExprCast expr)
        {
            this.Accept(expr.Expression);
            this.Accept(expr.SqlType);
        }
        public virtual void VisitExprColumn(ExprColumn expr)
        {
            this.Accept(expr.Source);
            this.Accept(expr.ColumnName);
        }
        public virtual void VisitExprColumnAlias(ExprColumnAlias expr)
        {
            
        }
        public virtual void VisitExprColumnName(ExprColumnName expr)
        {
            
        }
        public virtual void VisitExprColumnSetClause(ExprColumnSetClause expr)
        {
            this.Accept(expr.Column);
            this.Accept(expr.Value);
        }
        public virtual void VisitExprCrossedTable(ExprCrossedTable expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        ////Default implementation
        //public virtual void VisitExprCteQuery(ExprCteQuery expr)
        //{
            //this.Accept(expr.Alias);
            //this.Accept(expr.Query);
        //}
        public virtual void VisitExprCurrentRowFrameBorder(ExprCurrentRowFrameBorder expr)
        {
            
        }
        public virtual void VisitExprDatabaseName(ExprDatabaseName expr)
        {
            
        }
        public virtual void VisitExprDateAdd(ExprDateAdd expr)
        {
            this.Accept(expr.Date);
        }
        public virtual void VisitExprDateDiff(ExprDateDiff expr)
        {
            this.Accept(expr.StartDate);
            this.Accept(expr.EndDate);
        }
        public virtual void VisitExprDateTimeLiteral(ExprDateTimeLiteral expr)
        {
            
        }
        public virtual void VisitExprDateTimeOffsetLiteral(ExprDateTimeOffsetLiteral expr)
        {
            
        }
        public virtual void VisitExprDbSchema(ExprDbSchema expr)
        {
            this.Accept(expr.Database);
            this.Accept(expr.Schema);
        }
        public virtual void VisitExprDecimalLiteral(ExprDecimalLiteral expr)
        {
            
        }
        public virtual void VisitExprDefault(ExprDefault expr)
        {
            
        }
        public virtual void VisitExprDelete(ExprDelete expr)
        {
            this.Accept(expr.Target);
            this.Accept(expr.Source);
            this.Accept(expr.Filter);
        }
        public virtual void VisitExprDeleteOutput(ExprDeleteOutput expr)
        {
            this.Accept(expr.Delete);
            this.Accept(expr.OutputColumns);
        }
        ////Default implementation
        //public virtual void VisitExprDerivedTableQuery(ExprDerivedTableQuery expr)
        //{
            //this.Accept(expr.Query);
            //this.Accept(expr.Alias);
            //this.Accept(expr.Columns);
        //}
        public virtual void VisitExprDerivedTableValues(ExprDerivedTableValues expr)
        {
            this.Accept(expr.Values);
            this.Accept(expr.Alias);
            this.Accept(expr.Columns);
        }
        public virtual void VisitExprDiv(ExprDiv expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprDoubleLiteral(ExprDoubleLiteral expr)
        {
            
        }
        public virtual void VisitExprExists(ExprExists expr)
        {
            this.Accept(expr.SubQuery);
        }
        public virtual void VisitExprExprMergeNotMatchedInsert(ExprExprMergeNotMatchedInsert expr)
        {
            this.Accept(expr.And);
            this.Accept(expr.Columns);
            this.Accept(expr.Values);
        }
        public virtual void VisitExprExprMergeNotMatchedInsertDefault(ExprExprMergeNotMatchedInsertDefault expr)
        {
            this.Accept(expr.And);
        }
        public virtual void VisitExprFrameClause(ExprFrameClause expr)
        {
            this.Accept(expr.Start);
            this.Accept(expr.End);
        }
        public virtual void VisitExprFuncCoalesce(ExprFuncCoalesce expr)
        {
            this.Accept(expr.Test);
            this.Accept(expr.Alts);
        }
        public virtual void VisitExprFuncIsNull(ExprFuncIsNull expr)
        {
            this.Accept(expr.Test);
            this.Accept(expr.Alt);
        }
        public virtual void VisitExprFunctionName(ExprFunctionName expr)
        {
            
        }
        public virtual void VisitExprGetDate(ExprGetDate expr)
        {
            
        }
        public virtual void VisitExprGetUtcDate(ExprGetUtcDate expr)
        {
            
        }
        public virtual void VisitExprGuidLiteral(ExprGuidLiteral expr)
        {
            
        }
        public virtual void VisitExprIdentityInsert(ExprIdentityInsert expr)
        {
            this.Accept(expr.Insert);
            this.Accept(expr.IdentityColumns);
        }
        public virtual void VisitExprInSubQuery(ExprInSubQuery expr)
        {
            this.Accept(expr.TestExpression);
            this.Accept(expr.SubQuery);
        }
        public virtual void VisitExprInValues(ExprInValues expr)
        {
            this.Accept(expr.TestExpression);
            this.Accept(expr.Items);
        }
        public virtual void VisitExprInsert(ExprInsert expr)
        {
            this.Accept(expr.Target);
            this.Accept(expr.TargetColumns);
            this.Accept(expr.Source);
        }
        public virtual void VisitExprInsertOutput(ExprInsertOutput expr)
        {
            this.Accept(expr.Insert);
            this.Accept(expr.OutputColumns);
        }
        public virtual void VisitExprInsertQuery(ExprInsertQuery expr)
        {
            this.Accept(expr.Query);
        }
        public virtual void VisitExprInsertValueRow(ExprInsertValueRow expr)
        {
            this.Accept(expr.Items);
        }
        public virtual void VisitExprInsertValues(ExprInsertValues expr)
        {
            this.Accept(expr.Items);
        }
        public virtual void VisitExprInt16Literal(ExprInt16Literal expr)
        {
            
        }
        public virtual void VisitExprInt32Literal(ExprInt32Literal expr)
        {
            
        }
        public virtual void VisitExprInt64Literal(ExprInt64Literal expr)
        {
            
        }
        public virtual void VisitExprIsNull(ExprIsNull expr)
        {
            this.Accept(expr.Test);
        }
        public virtual void VisitExprJoinedTable(ExprJoinedTable expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
            this.Accept(expr.SearchCondition);
        }
        public virtual void VisitExprLateralCrossedTable(ExprLateralCrossedTable expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprLike(ExprLike expr)
        {
            this.Accept(expr.Test);
            this.Accept(expr.Pattern);
        }
        public virtual void VisitExprList(ExprList expr)
        {
            this.Accept(expr.Expressions);
        }
        public virtual void VisitExprMerge(ExprMerge expr)
        {
            this.Accept(expr.TargetTable);
            this.Accept(expr.Source);
            this.Accept(expr.On);
            this.Accept(expr.WhenMatched);
            this.Accept(expr.WhenNotMatchedByTarget);
            this.Accept(expr.WhenNotMatchedBySource);
        }
        public virtual void VisitExprMergeMatchedDelete(ExprMergeMatchedDelete expr)
        {
            this.Accept(expr.And);
        }
        public virtual void VisitExprMergeMatchedUpdate(ExprMergeMatchedUpdate expr)
        {
            this.Accept(expr.And);
            this.Accept(expr.Set);
        }
        public virtual void VisitExprMergeOutput(ExprMergeOutput expr)
        {
            this.Accept(expr.TargetTable);
            this.Accept(expr.Source);
            this.Accept(expr.On);
            this.Accept(expr.WhenMatched);
            this.Accept(expr.WhenNotMatchedByTarget);
            this.Accept(expr.WhenNotMatchedBySource);
            this.Accept(expr.Output);
        }
        public virtual void VisitExprModulo(ExprModulo expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprMul(ExprMul expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprNull(ExprNull expr)
        {
            
        }
        public virtual void VisitExprOffsetFetch(ExprOffsetFetch expr)
        {
            this.Accept(expr.Offset);
            this.Accept(expr.Fetch);
        }
        public virtual void VisitExprOrderBy(ExprOrderBy expr)
        {
            this.Accept(expr.OrderList);
        }
        public virtual void VisitExprOrderByItem(ExprOrderByItem expr)
        {
            this.Accept(expr.Value);
        }
        public virtual void VisitExprOrderByOffsetFetch(ExprOrderByOffsetFetch expr)
        {
            this.Accept(expr.OrderList);
            this.Accept(expr.OffsetFetch);
        }
        public virtual void VisitExprOutput(ExprOutput expr)
        {
            this.Accept(expr.Columns);
        }
        public virtual void VisitExprOutputAction(ExprOutputAction expr)
        {
            this.Accept(expr.Alias);
        }
        public virtual void VisitExprOutputColumn(ExprOutputColumn expr)
        {
            this.Accept(expr.Column);
        }
        public virtual void VisitExprOutputColumnDeleted(ExprOutputColumnDeleted expr)
        {
            this.Accept(expr.ColumnName);
        }
        public virtual void VisitExprOutputColumnInserted(ExprOutputColumnInserted expr)
        {
            this.Accept(expr.ColumnName);
        }
        public virtual void VisitExprOver(ExprOver expr)
        {
            this.Accept(expr.Partitions);
            this.Accept(expr.OrderBy);
            this.Accept(expr.FrameClause);
        }
        public virtual void VisitExprQueryExpression(ExprQueryExpression expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprQueryList(ExprQueryList expr)
        {
            this.Accept(expr.Expressions);
        }
        public virtual void VisitExprQuerySpecification(ExprQuerySpecification expr)
        {
            this.Accept(expr.SelectList);
            this.Accept(expr.Top);
            this.Accept(expr.From);
            this.Accept(expr.Where);
            this.Accept(expr.GroupBy);
        }
        public virtual void VisitExprScalarFunction(ExprScalarFunction expr)
        {
            this.Accept(expr.Schema);
            this.Accept(expr.Name);
            this.Accept(expr.Arguments);
        }
        public virtual void VisitExprSchemaName(ExprSchemaName expr)
        {
            
        }
        public virtual void VisitExprSelect(ExprSelect expr)
        {
            this.Accept(expr.SelectQuery);
            this.Accept(expr.OrderBy);
        }
        public virtual void VisitExprSelectOffsetFetch(ExprSelectOffsetFetch expr)
        {
            this.Accept(expr.SelectQuery);
            this.Accept(expr.OrderBy);
        }
        public virtual void VisitExprStringConcat(ExprStringConcat expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprStringLiteral(ExprStringLiteral expr)
        {
            
        }
        public virtual void VisitExprSub(ExprSub expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprSum(ExprSum expr)
        {
            this.Accept(expr.Left);
            this.Accept(expr.Right);
        }
        public virtual void VisitExprTable(ExprTable expr)
        {
            this.Accept(expr.FullName);
            this.Accept(expr.Alias);
        }
        public virtual void VisitExprTableAlias(ExprTableAlias expr)
        {
            this.Accept(expr.Alias);
        }
        public virtual void VisitExprTableFullName(ExprTableFullName expr)
        {
            this.Accept(expr.DbSchema);
            this.Accept(expr.TableName);
        }
        public virtual void VisitExprTableFunction(ExprTableFunction expr)
        {
            this.Accept(expr.Schema);
            this.Accept(expr.Name);
            this.Accept(expr.Arguments);
        }
        public virtual void VisitExprTableName(ExprTableName expr)
        {
            
        }
        public virtual void VisitExprTableValueConstructor(ExprTableValueConstructor expr)
        {
            this.Accept(expr.Items);
        }
        public virtual void VisitExprTempTableName(ExprTempTableName expr)
        {
            
        }
        public virtual void VisitExprTypeBoolean(ExprTypeBoolean expr)
        {
            
        }
        public virtual void VisitExprTypeByte(ExprTypeByte expr)
        {
            
        }
        public virtual void VisitExprTypeByteArray(ExprTypeByteArray expr)
        {
            
        }
        public virtual void VisitExprTypeDateTime(ExprTypeDateTime expr)
        {
            
        }
        public virtual void VisitExprTypeDateTimeOffset(ExprTypeDateTimeOffset expr)
        {
            
        }
        public virtual void VisitExprTypeDecimal(ExprTypeDecimal expr)
        {
            
        }
        public virtual void VisitExprTypeDouble(ExprTypeDouble expr)
        {
            
        }
        public virtual void VisitExprTypeFixSizeByteArray(ExprTypeFixSizeByteArray expr)
        {
            
        }
        public virtual void VisitExprTypeFixSizeString(ExprTypeFixSizeString expr)
        {
            
        }
        public virtual void VisitExprTypeGuid(ExprTypeGuid expr)
        {
            
        }
        public virtual void VisitExprTypeInt16(ExprTypeInt16 expr)
        {
            
        }
        public virtual void VisitExprTypeInt32(ExprTypeInt32 expr)
        {
            
        }
        public virtual void VisitExprTypeInt64(ExprTypeInt64 expr)
        {
            
        }
        public virtual void VisitExprTypeString(ExprTypeString expr)
        {
            
        }
        public virtual void VisitExprTypeXml(ExprTypeXml expr)
        {
            
        }
        public virtual void VisitExprUnboundedFrameBorder(ExprUnboundedFrameBorder expr)
        {
            
        }
        public virtual void VisitExprUnsafeValue(ExprUnsafeValue expr)
        {
            
        }
        public virtual void VisitExprUpdate(ExprUpdate expr)
        {
            this.Accept(expr.Target);
            this.Accept(expr.SetClause);
            this.Accept(expr.Source);
            this.Accept(expr.Filter);
        }
        public virtual void VisitExprValueFrameBorder(ExprValueFrameBorder expr)
        {
            this.Accept(expr.Value);
        }
        public virtual void VisitExprValueQuery(ExprValueQuery expr)
        {
            this.Accept(expr.Query);
        }
        public virtual void VisitExprValueRow(ExprValueRow expr)
        {
            this.Accept(expr.Items);
        }
        //CodeGenEnd
    }
}
