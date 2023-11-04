using System;
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

namespace SqExpress.SyntaxTreeOperations.Internal
{
    internal readonly struct WalkerContext<TCtx>
    {
        public WalkerContext(IExpr? parent, TCtx context)
        {
            this.Parent = parent;
            this.Context = context;
        }

        public readonly IExpr? Parent;
        public readonly TCtx Context;
    }

    internal class ExprWalker<TCtx> : IExprVisitor<bool, WalkerContext<TCtx>>
    {
        private readonly IWalkerVisitorBase<TCtx> _visitor;

        private HashSet<string>? _cteChecker;

        public ExprWalker(IWalkerVisitorBase<TCtx> visitor)
        {
            this._visitor = visitor;
        }

        private WalkResult Visit(IExpr expr, string typeTag, WalkerContext<TCtx> ctx, out WalkerContext<TCtx> ctxOut)
        {
            VisitorResult<TCtx> result;
            if (this._visitor is IWalkerVisitor<TCtx> walker)
            {
                result = walker.VisitExpr(expr, typeTag, ctx.Context);
            }
            else if (this._visitor is IWalkerVisitorWithParent<TCtx> walkerWithParent)
            {
                result = walkerWithParent.VisitExpr(expr, ctx.Parent, typeTag, ctx.Context);
            }
            else
            {
                throw new SqExpressException("Unknown walker type");
            }

            ctxOut = new WalkerContext<TCtx>(expr, result.Context);
            return result.WalkResult;
        }

        private void EndVisit(IExpr expr, TCtx ctx)
        {
            this._visitor.EndVisitExpr(expr, ctx);
        }

        void VisitPlainProperty(string name, string? value, TCtx ctx) => this._visitor.VisitPlainProperty(name, value, ctx);
        void VisitPlainProperty(string name, bool? value, TCtx ctx) => this._visitor.VisitPlainProperty(name, value, ctx);
        void VisitPlainProperty(string name, byte? value, TCtx ctx) => this._visitor.VisitPlainProperty(name, value, ctx);
        void VisitPlainProperty(string name, short? value, TCtx ctx) => this._visitor.VisitPlainProperty(name, value, ctx);
        void VisitPlainProperty(string name, int? value, TCtx ctx) => this._visitor.VisitPlainProperty(name, value, ctx);
        void VisitPlainProperty(string name, long? value, TCtx ctx) => this._visitor.VisitPlainProperty(name, value, ctx);
        void VisitPlainProperty(string name, decimal? value, TCtx ctx) => this._visitor.VisitPlainProperty(name, value, ctx);
        void VisitPlainProperty(string name, double? value, TCtx ctx) => this._visitor.VisitPlainProperty(name, value, ctx);
        void VisitPlainProperty(string name, DateTime? value, TCtx ctx) => this._visitor.VisitPlainProperty(name, value, ctx);
        void VisitPlainProperty(string name, DateTimeOffset? value, TCtx ctx) => this._visitor.VisitPlainProperty(name, value, ctx);
        void VisitPlainProperty(string name, Guid? value, TCtx ctx) => this._visitor.VisitPlainProperty(name, value, ctx);
        void VisitPlainProperty(string name, IReadOnlyList<byte>? value, TCtx ctx) => this._visitor.VisitPlainProperty(name, value, ctx);

        void VisitPlainProperty(string name, DecimalPrecisionScale? value, TCtx ctx)
        {
            this._visitor.VisitPlainProperty(name + '.' + nameof(DecimalPrecisionScale.Precision), value?.Precision, ctx);
            this._visitor.VisitPlainProperty(name + '.' + nameof(DecimalPrecisionScale.Scale), value?.Scale, ctx);
        }

        void VisitPlainProperty(string name, ExprJoinedTable.ExprJoinType value, TCtx ctx)
        {
            this._visitor.VisitPlainProperty(name, value.ToString(), ctx);
        }

        void VisitPlainProperty(string name, ExprQueryExpressionType value, TCtx ctx)
        {
            this._visitor.VisitPlainProperty(name, value.ToString(), ctx);
        }

        void VisitPlainProperty(string name, DateAddDatePart value, TCtx ctx)
        {
            this._visitor.VisitPlainProperty(name, value.ToString(), ctx);
        }

        void VisitPlainProperty(string name, FrameBorderDirection value, TCtx ctx)
        {
            this._visitor.VisitPlainProperty(name, value.ToString(), ctx);
        }

        private bool Accept(string name, IExpr? expr, WalkerContext<TCtx> context)
        {
            this._visitor.VisitProperty(name, false, expr == null, context.Context);
            var accept = expr == null || expr.Accept(this, context);
            this._visitor.EndVisitProperty(name, false, expr == null, context.Context);
            return accept;
        }

        private bool Accept(string name, IReadOnlyList<IExpr>? exprs, WalkerContext<TCtx> context)
        {
            this._visitor.VisitProperty(name, true, exprs == null, context.Context);
            bool result = true;
            if (exprs != null)
            {
                for (int i = 0; i < exprs.Count; i++)
                {
                    this._visitor.VisitArrayItem(name, i, context.Context);
                    if (!exprs[i].Accept(this, context))
                    {
                        result = false;
                    }

                    this._visitor.EndVisitArrayItem(name, i, context.Context);
                    if (!result)
                    {
                        break;
                    }
                }
            }

            this._visitor.EndVisitProperty(name, true, exprs == null, context.Context);
            return result;
        }

        public bool VisitExprCteQuery(ExprCteQuery expr, WalkerContext<TCtx> arg)
        {
            bool res = true;
            this._cteChecker ??= new HashSet<string>();


            ExprCte toVisit;
            if (expr is CteOriginalRef originalRef)
            {
                toVisit = originalRef.Original;
            }
            else
            {
                toVisit = expr;
            }

            var walkResult = this.Visit(toVisit, "CteQuery", arg, out var argOut);
            if (walkResult == WalkResult.Continue)
            {
                res = this.Accept("Alias", expr.Alias, argOut);
                if (!this._cteChecker.Contains(expr.Name))
                {
                    this._cteChecker.Add(expr.Name);
                    res &= this.Accept("Query", expr.Query, argOut);
                    this._cteChecker.Remove(expr.Name);
                }
            }

            this.VisitPlainProperty("Name", expr.Name, argOut.Context);
            this.EndVisit(expr, arg.Context);
            return res && walkResult != WalkResult.Stop;
        }

        public bool VisitExprDerivedTableQuery(ExprDerivedTableQuery expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "DerivedTableQuery", arg, out var argOut);
            if (walkResult == WalkResult.Continue)
            {
                res = this.Accept("Query", expr.Query, argOut) && this.Accept("Alias", expr.Alias, argOut) && this.Accept("Columns", expr.Columns, argOut);
            }

            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }

        //CodeGenStart
        public bool VisitExprAggregateFunction(ExprAggregateFunction expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "AggregateFunction", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Name",expr.Name, argOut) && this.Accept("Expression",expr.Expression, argOut);
            }
            this.VisitPlainProperty("IsDistinct",expr.IsDistinct, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprAlias(ExprAlias expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "Alias", arg, out var argOut);
            this.VisitPlainProperty("Name",expr.Name, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprAliasGuid(ExprAliasGuid expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "AliasGuid", arg, out var argOut);
            this.VisitPlainProperty("Id",expr.Id, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprAliasedColumn(ExprAliasedColumn expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "AliasedColumn", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Column",expr.Column, argOut) && this.Accept("Alias",expr.Alias, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprAliasedColumnName(ExprAliasedColumnName expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "AliasedColumnName", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Column",expr.Column, argOut) && this.Accept("Alias",expr.Alias, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprAliasedSelecting(ExprAliasedSelecting expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "AliasedSelecting", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Value",expr.Value, argOut) && this.Accept("Alias",expr.Alias, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprAllColumns(ExprAllColumns expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "AllColumns", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Source",expr.Source, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprAnalyticFunction(ExprAnalyticFunction expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "AnalyticFunction", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Name",expr.Name, argOut) && this.Accept("Arguments",expr.Arguments, argOut) && this.Accept("Over",expr.Over, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprBitwiseAnd(ExprBitwiseAnd expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "BitwiseAnd", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprBitwiseNot(ExprBitwiseNot expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "BitwiseNot", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Value",expr.Value, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprBitwiseOr(ExprBitwiseOr expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "BitwiseOr", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprBitwiseXor(ExprBitwiseXor expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "BitwiseXor", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprBoolLiteral(ExprBoolLiteral expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "BoolLiteral", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprBooleanAnd(ExprBooleanAnd expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "BooleanAnd", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprBooleanEq(ExprBooleanEq expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "BooleanEq", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprBooleanGt(ExprBooleanGt expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "BooleanGt", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprBooleanGtEq(ExprBooleanGtEq expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "BooleanGtEq", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprBooleanLt(ExprBooleanLt expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "BooleanLt", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprBooleanLtEq(ExprBooleanLtEq expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "BooleanLtEq", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprBooleanNot(ExprBooleanNot expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "BooleanNot", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Expr",expr.Expr, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprBooleanNotEq(ExprBooleanNotEq expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "BooleanNotEq", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprBooleanOr(ExprBooleanOr expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "BooleanOr", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprByteArrayLiteral(ExprByteArrayLiteral expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "ByteArrayLiteral", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprByteLiteral(ExprByteLiteral expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "ByteLiteral", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprCase(ExprCase expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Case", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Cases",expr.Cases, argOut) && this.Accept("DefaultValue",expr.DefaultValue, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprCaseWhenThen(ExprCaseWhenThen expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "CaseWhenThen", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Condition",expr.Condition, argOut) && this.Accept("Value",expr.Value, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprCast(ExprCast expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Cast", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Expression",expr.Expression, argOut) && this.Accept("SqlType",expr.SqlType, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprColumn(ExprColumn expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Column", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Source",expr.Source, argOut) && this.Accept("ColumnName",expr.ColumnName, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprColumnAlias(ExprColumnAlias expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "ColumnAlias", arg, out var argOut);
            this.VisitPlainProperty("Name",expr.Name, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprColumnName(ExprColumnName expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "ColumnName", arg, out var argOut);
            this.VisitPlainProperty("Name",expr.Name, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprColumnSetClause(ExprColumnSetClause expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "ColumnSetClause", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Column",expr.Column, argOut) && this.Accept("Value",expr.Value, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprCrossedTable(ExprCrossedTable expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "CrossedTable", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        ////Default implementation
        //public bool VisitExprCteQuery(ExprCteQuery expr, WalkerContext<TCtx> arg)
        //{
            //var res = true;
            //var walkResult = this.Visit(expr, "CteQuery", arg, out var argOut);
            //if(walkResult == WalkResult.Continue)
            //{
                //res = this.Accept("Alias",expr.Alias, argOut) && this.Accept("Query",expr.Query, argOut);
            //}
            //this.VisitPlainProperty("Name",expr.Name, argOut.Context);
            //this.EndVisit(expr, argOut.Context);
            //return res && walkResult != WalkResult.Stop;
        //}
        public bool VisitExprCurrentRowFrameBorder(ExprCurrentRowFrameBorder expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "CurrentRowFrameBorder", arg, out var argOut);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprDatabaseName(ExprDatabaseName expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "DatabaseName", arg, out var argOut);
            this.VisitPlainProperty("Name",expr.Name, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprDateAdd(ExprDateAdd expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "DateAdd", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Date",expr.Date, argOut);
            }
            this.VisitPlainProperty("DatePart",expr.DatePart, argOut.Context);
            this.VisitPlainProperty("Number",expr.Number, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprDateTimeLiteral(ExprDateTimeLiteral expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "DateTimeLiteral", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprDateTimeOffsetLiteral(ExprDateTimeOffsetLiteral expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "DateTimeOffsetLiteral", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprDbSchema(ExprDbSchema expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "DbSchema", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Database",expr.Database, argOut) && this.Accept("Schema",expr.Schema, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprDecimalLiteral(ExprDecimalLiteral expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "DecimalLiteral", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprDefault(ExprDefault expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "Default", arg, out var argOut);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprDelete(ExprDelete expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Delete", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Target",expr.Target, argOut) && this.Accept("Source",expr.Source, argOut) && this.Accept("Filter",expr.Filter, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprDeleteOutput(ExprDeleteOutput expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "DeleteOutput", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Delete",expr.Delete, argOut) && this.Accept("OutputColumns",expr.OutputColumns, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        ////Default implementation
        //public bool VisitExprDerivedTableQuery(ExprDerivedTableQuery expr, WalkerContext<TCtx> arg)
        //{
            //var res = true;
            //var walkResult = this.Visit(expr, "DerivedTableQuery", arg, out var argOut);
            //if(walkResult == WalkResult.Continue)
            //{
                //res = this.Accept("Query",expr.Query, argOut) && this.Accept("Alias",expr.Alias, argOut) && this.Accept("Columns",expr.Columns, argOut);
            //}
            //this.EndVisit(expr, argOut.Context);
            //return res && walkResult != WalkResult.Stop;
        //}
        public bool VisitExprDerivedTableValues(ExprDerivedTableValues expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "DerivedTableValues", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Values",expr.Values, argOut) && this.Accept("Alias",expr.Alias, argOut) && this.Accept("Columns",expr.Columns, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprDiv(ExprDiv expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Div", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprDoubleLiteral(ExprDoubleLiteral expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "DoubleLiteral", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprExists(ExprExists expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Exists", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("SubQuery",expr.SubQuery, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprExprMergeNotMatchedInsert(ExprExprMergeNotMatchedInsert expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "ExprMergeNotMatchedInsert", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("And",expr.And, argOut) && this.Accept("Columns",expr.Columns, argOut) && this.Accept("Values",expr.Values, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprExprMergeNotMatchedInsertDefault(ExprExprMergeNotMatchedInsertDefault expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "ExprMergeNotMatchedInsertDefault", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("And",expr.And, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprFrameClause(ExprFrameClause expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "FrameClause", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Start",expr.Start, argOut) && this.Accept("End",expr.End, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprFuncCoalesce(ExprFuncCoalesce expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "FuncCoalesce", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Test",expr.Test, argOut) && this.Accept("Alts",expr.Alts, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprFuncIsNull(ExprFuncIsNull expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "FuncIsNull", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Test",expr.Test, argOut) && this.Accept("Alt",expr.Alt, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprFunctionName(ExprFunctionName expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "FunctionName", arg, out var argOut);
            this.VisitPlainProperty("BuiltIn",expr.BuiltIn, argOut.Context);
            this.VisitPlainProperty("Name",expr.Name, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprGetDate(ExprGetDate expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "GetDate", arg, out var argOut);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprGetUtcDate(ExprGetUtcDate expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "GetUtcDate", arg, out var argOut);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprGuidLiteral(ExprGuidLiteral expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "GuidLiteral", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprIdentityInsert(ExprIdentityInsert expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "IdentityInsert", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Insert",expr.Insert, argOut) && this.Accept("IdentityColumns",expr.IdentityColumns, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprInSubQuery(ExprInSubQuery expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "InSubQuery", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("TestExpression",expr.TestExpression, argOut) && this.Accept("SubQuery",expr.SubQuery, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprInValues(ExprInValues expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "InValues", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("TestExpression",expr.TestExpression, argOut) && this.Accept("Items",expr.Items, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprInsert(ExprInsert expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Insert", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Target",expr.Target, argOut) && this.Accept("TargetColumns",expr.TargetColumns, argOut) && this.Accept("Source",expr.Source, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprInsertOutput(ExprInsertOutput expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "InsertOutput", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Insert",expr.Insert, argOut) && this.Accept("OutputColumns",expr.OutputColumns, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprInsertQuery(ExprInsertQuery expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "InsertQuery", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Query",expr.Query, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprInsertValueRow(ExprInsertValueRow expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "InsertValueRow", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Items",expr.Items, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprInsertValues(ExprInsertValues expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "InsertValues", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Items",expr.Items, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprInt16Literal(ExprInt16Literal expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "Int16Literal", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprInt32Literal(ExprInt32Literal expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "Int32Literal", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprInt64Literal(ExprInt64Literal expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "Int64Literal", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprIsNull(ExprIsNull expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "IsNull", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Test",expr.Test, argOut);
            }
            this.VisitPlainProperty("Not",expr.Not, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprJoinedTable(ExprJoinedTable expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "JoinedTable", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut) && this.Accept("SearchCondition",expr.SearchCondition, argOut);
            }
            this.VisitPlainProperty("JoinType",expr.JoinType, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprLike(ExprLike expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Like", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Test",expr.Test, argOut) && this.Accept("Pattern",expr.Pattern, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprList(ExprList expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "List", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Expressions",expr.Expressions, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprMerge(ExprMerge expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Merge", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("TargetTable",expr.TargetTable, argOut) && this.Accept("Source",expr.Source, argOut) && this.Accept("On",expr.On, argOut) && this.Accept("WhenMatched",expr.WhenMatched, argOut) && this.Accept("WhenNotMatchedByTarget",expr.WhenNotMatchedByTarget, argOut) && this.Accept("WhenNotMatchedBySource",expr.WhenNotMatchedBySource, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprMergeMatchedDelete(ExprMergeMatchedDelete expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "MergeMatchedDelete", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("And",expr.And, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprMergeMatchedUpdate(ExprMergeMatchedUpdate expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "MergeMatchedUpdate", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("And",expr.And, argOut) && this.Accept("Set",expr.Set, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprMergeOutput(ExprMergeOutput expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "MergeOutput", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("TargetTable",expr.TargetTable, argOut) && this.Accept("Source",expr.Source, argOut) && this.Accept("On",expr.On, argOut) && this.Accept("WhenMatched",expr.WhenMatched, argOut) && this.Accept("WhenNotMatchedByTarget",expr.WhenNotMatchedByTarget, argOut) && this.Accept("WhenNotMatchedBySource",expr.WhenNotMatchedBySource, argOut) && this.Accept("Output",expr.Output, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprModulo(ExprModulo expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Modulo", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprMul(ExprMul expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Mul", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprNull(ExprNull expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "Null", arg, out var argOut);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprOffsetFetch(ExprOffsetFetch expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "OffsetFetch", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Offset",expr.Offset, argOut) && this.Accept("Fetch",expr.Fetch, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprOrderBy(ExprOrderBy expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "OrderBy", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("OrderList",expr.OrderList, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprOrderByItem(ExprOrderByItem expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "OrderByItem", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Value",expr.Value, argOut);
            }
            this.VisitPlainProperty("Descendant",expr.Descendant, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprOrderByOffsetFetch(ExprOrderByOffsetFetch expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "OrderByOffsetFetch", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("OrderList",expr.OrderList, argOut) && this.Accept("OffsetFetch",expr.OffsetFetch, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprOutput(ExprOutput expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Output", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Columns",expr.Columns, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprOutputAction(ExprOutputAction expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "OutputAction", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Alias",expr.Alias, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprOutputColumn(ExprOutputColumn expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "OutputColumn", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Column",expr.Column, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprOutputColumnDeleted(ExprOutputColumnDeleted expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "OutputColumnDeleted", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("ColumnName",expr.ColumnName, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprOutputColumnInserted(ExprOutputColumnInserted expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "OutputColumnInserted", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("ColumnName",expr.ColumnName, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprOver(ExprOver expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Over", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Partitions",expr.Partitions, argOut) && this.Accept("OrderBy",expr.OrderBy, argOut) && this.Accept("FrameClause",expr.FrameClause, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprQueryExpression(ExprQueryExpression expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "QueryExpression", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.VisitPlainProperty("QueryExpressionType",expr.QueryExpressionType, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprQueryList(ExprQueryList expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "QueryList", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Expressions",expr.Expressions, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprQuerySpecification(ExprQuerySpecification expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "QuerySpecification", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("SelectList",expr.SelectList, argOut) && this.Accept("Top",expr.Top, argOut) && this.Accept("From",expr.From, argOut) && this.Accept("Where",expr.Where, argOut) && this.Accept("GroupBy",expr.GroupBy, argOut);
            }
            this.VisitPlainProperty("Distinct",expr.Distinct, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprScalarFunction(ExprScalarFunction expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "ScalarFunction", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Schema",expr.Schema, argOut) && this.Accept("Name",expr.Name, argOut) && this.Accept("Arguments",expr.Arguments, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprSchemaName(ExprSchemaName expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "SchemaName", arg, out var argOut);
            this.VisitPlainProperty("Name",expr.Name, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprSelect(ExprSelect expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Select", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("SelectQuery",expr.SelectQuery, argOut) && this.Accept("OrderBy",expr.OrderBy, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprSelectOffsetFetch(ExprSelectOffsetFetch expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "SelectOffsetFetch", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("SelectQuery",expr.SelectQuery, argOut) && this.Accept("OrderBy",expr.OrderBy, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprStringConcat(ExprStringConcat expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "StringConcat", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprStringLiteral(ExprStringLiteral expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "StringLiteral", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprSub(ExprSub expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Sub", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprSum(ExprSum expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Sum", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprTable(ExprTable expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Table", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("FullName",expr.FullName, argOut) && this.Accept("Alias",expr.Alias, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprTableAlias(ExprTableAlias expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "TableAlias", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Alias",expr.Alias, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprTableFullName(ExprTableFullName expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "TableFullName", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("DbSchema",expr.DbSchema, argOut) && this.Accept("TableName",expr.TableName, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprTableName(ExprTableName expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "TableName", arg, out var argOut);
            this.VisitPlainProperty("Name",expr.Name, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprTableValueConstructor(ExprTableValueConstructor expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "TableValueConstructor", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Items",expr.Items, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprTempTableName(ExprTempTableName expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "TempTableName", arg, out var argOut);
            this.VisitPlainProperty("Name",expr.Name, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprTypeBoolean(ExprTypeBoolean expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "TypeBoolean", arg, out var argOut);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprTypeByte(ExprTypeByte expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "TypeByte", arg, out var argOut);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprTypeByteArray(ExprTypeByteArray expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "TypeByteArray", arg, out var argOut);
            this.VisitPlainProperty("Size",expr.Size, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprTypeDateTime(ExprTypeDateTime expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "TypeDateTime", arg, out var argOut);
            this.VisitPlainProperty("IsDate",expr.IsDate, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprTypeDateTimeOffset(ExprTypeDateTimeOffset expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "TypeDateTimeOffset", arg, out var argOut);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprTypeDecimal(ExprTypeDecimal expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "TypeDecimal", arg, out var argOut);
            this.VisitPlainProperty("PrecisionScale",expr.PrecisionScale, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprTypeDouble(ExprTypeDouble expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "TypeDouble", arg, out var argOut);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprTypeFixSizeByteArray(ExprTypeFixSizeByteArray expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "TypeFixSizeByteArray", arg, out var argOut);
            this.VisitPlainProperty("Size",expr.Size, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprTypeFixSizeString(ExprTypeFixSizeString expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "TypeFixSizeString", arg, out var argOut);
            this.VisitPlainProperty("Size",expr.Size, argOut.Context);
            this.VisitPlainProperty("IsUnicode",expr.IsUnicode, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprTypeGuid(ExprTypeGuid expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "TypeGuid", arg, out var argOut);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprTypeInt16(ExprTypeInt16 expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "TypeInt16", arg, out var argOut);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprTypeInt32(ExprTypeInt32 expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "TypeInt32", arg, out var argOut);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprTypeInt64(ExprTypeInt64 expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "TypeInt64", arg, out var argOut);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprTypeString(ExprTypeString expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "TypeString", arg, out var argOut);
            this.VisitPlainProperty("Size",expr.Size, argOut.Context);
            this.VisitPlainProperty("IsUnicode",expr.IsUnicode, argOut.Context);
            this.VisitPlainProperty("IsText",expr.IsText, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprTypeXml(ExprTypeXml expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "TypeXml", arg, out var argOut);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprUnboundedFrameBorder(ExprUnboundedFrameBorder expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "UnboundedFrameBorder", arg, out var argOut);
            this.VisitPlainProperty("FrameBorderDirection",expr.FrameBorderDirection, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprUnsafeValue(ExprUnsafeValue expr, WalkerContext<TCtx> arg)
        {
            var walkResult = this.Visit(expr, "UnsafeValue", arg, out var argOut);
            this.VisitPlainProperty("UnsafeValue",expr.UnsafeValue, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return walkResult != WalkResult.Stop;
        }
        public bool VisitExprUpdate(ExprUpdate expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "Update", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Target",expr.Target, argOut) && this.Accept("SetClause",expr.SetClause, argOut) && this.Accept("Source",expr.Source, argOut) && this.Accept("Filter",expr.Filter, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprValueFrameBorder(ExprValueFrameBorder expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "ValueFrameBorder", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Value",expr.Value, argOut);
            }
            this.VisitPlainProperty("FrameBorderDirection",expr.FrameBorderDirection, argOut.Context);
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprValueQuery(ExprValueQuery expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "ValueQuery", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Query",expr.Query, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        public bool VisitExprValueRow(ExprValueRow expr, WalkerContext<TCtx> arg)
        {
            var res = true;
            var walkResult = this.Visit(expr, "ValueRow", arg, out var argOut);
            if(walkResult == WalkResult.Continue)
            {
                res = this.Accept("Items",expr.Items, argOut);
            }
            this.EndVisit(expr, argOut.Context);
            return res && walkResult != WalkResult.Stop;
        }
        //CodeGenEnd
    }
}
