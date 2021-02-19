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
    internal class ExprWalker<TCtx> : IExprVisitor<bool, TCtx>
    {
        private readonly IWalkerVisitor<TCtx> _visitor;

        public ExprWalker(IWalkerVisitor<TCtx> visitor)
        {
            this._visitor = visitor;
        }

        private bool Visit(IExpr expr, string typeTag,TCtx ctx, out TCtx ctxOut)
        {
            var result = this._visitor.VisitExpr(expr, typeTag, ctx);
            ctxOut = result.Context;
            return !result.IsStop;
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

        private bool Accept(string name, IExpr? expr, TCtx context)
        {
            this._visitor.VisitProperty(name, false, expr == null, context);
            var accept = expr == null || expr.Accept(this, context);
            this._visitor.EndVisitProperty(name, false, expr == null, context);
            return accept;
        }

        private bool Accept(string name, IReadOnlyList<IExpr>? exprs, TCtx context)
        {
            this._visitor.VisitProperty(name, true, exprs == null, context);
            bool result = true;
            if (exprs != null)
            {
                for (int i = 0; i < exprs.Count; i++)
                {
                    this._visitor.VisitArrayItem(name, i, context);
                    if (!exprs[i].Accept(this, context))
                    {
                        result = false;
                    }
                    this._visitor.EndVisitArrayItem(name, i, context);
                    if (!result)
                    {
                        break;
                    }
                }
            }
            this._visitor.EndVisitProperty(name, true, exprs == null, context);
            return result;
        }
        //CodeGenStart
        public bool VisitExprAggregateFunction(ExprAggregateFunction expr, TCtx arg)
        {
            var res = this.Visit(expr, "AggregateFunction", arg, out var argOut) && this.Accept("Name",expr.Name, argOut) && this.Accept("Expression",expr.Expression, argOut);
            this.VisitPlainProperty("IsDistinct",expr.IsDistinct, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprAlias(ExprAlias expr, TCtx arg)
        {
            var res = this.Visit(expr, "Alias", arg, out var argOut);
            this.VisitPlainProperty("Name",expr.Name, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprAliasGuid(ExprAliasGuid expr, TCtx arg)
        {
            var res = this.Visit(expr, "AliasGuid", arg, out var argOut);
            this.VisitPlainProperty("Id",expr.Id, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprAliasedColumn(ExprAliasedColumn expr, TCtx arg)
        {
            var res = this.Visit(expr, "AliasedColumn", arg, out var argOut) && this.Accept("Column",expr.Column, argOut) && this.Accept("Alias",expr.Alias, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprAliasedColumnName(ExprAliasedColumnName expr, TCtx arg)
        {
            var res = this.Visit(expr, "AliasedColumnName", arg, out var argOut) && this.Accept("Column",expr.Column, argOut) && this.Accept("Alias",expr.Alias, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprAliasedSelecting(ExprAliasedSelecting expr, TCtx arg)
        {
            var res = this.Visit(expr, "AliasedSelecting", arg, out var argOut) && this.Accept("Value",expr.Value, argOut) && this.Accept("Alias",expr.Alias, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprAllColumns(ExprAllColumns expr, TCtx arg)
        {
            var res = this.Visit(expr, "AllColumns", arg, out var argOut) && this.Accept("Source",expr.Source, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprAnalyticFunction(ExprAnalyticFunction expr, TCtx arg)
        {
            var res = this.Visit(expr, "AnalyticFunction", arg, out var argOut) && this.Accept("Name",expr.Name, argOut) && this.Accept("Arguments",expr.Arguments, argOut) && this.Accept("Over",expr.Over, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprBoolLiteral(ExprBoolLiteral expr, TCtx arg)
        {
            var res = this.Visit(expr, "BoolLiteral", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprBooleanAnd(ExprBooleanAnd expr, TCtx arg)
        {
            var res = this.Visit(expr, "BooleanAnd", arg, out var argOut) && this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprBooleanEq(ExprBooleanEq expr, TCtx arg)
        {
            var res = this.Visit(expr, "BooleanEq", arg, out var argOut) && this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprBooleanGt(ExprBooleanGt expr, TCtx arg)
        {
            var res = this.Visit(expr, "BooleanGt", arg, out var argOut) && this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprBooleanGtEq(ExprBooleanGtEq expr, TCtx arg)
        {
            var res = this.Visit(expr, "BooleanGtEq", arg, out var argOut) && this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprBooleanLt(ExprBooleanLt expr, TCtx arg)
        {
            var res = this.Visit(expr, "BooleanLt", arg, out var argOut) && this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprBooleanLtEq(ExprBooleanLtEq expr, TCtx arg)
        {
            var res = this.Visit(expr, "BooleanLtEq", arg, out var argOut) && this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprBooleanNot(ExprBooleanNot expr, TCtx arg)
        {
            var res = this.Visit(expr, "BooleanNot", arg, out var argOut) && this.Accept("Expr",expr.Expr, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprBooleanNotEq(ExprBooleanNotEq expr, TCtx arg)
        {
            var res = this.Visit(expr, "BooleanNotEq", arg, out var argOut) && this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprBooleanOr(ExprBooleanOr expr, TCtx arg)
        {
            var res = this.Visit(expr, "BooleanOr", arg, out var argOut) && this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprByteArrayLiteral(ExprByteArrayLiteral expr, TCtx arg)
        {
            var res = this.Visit(expr, "ByteArrayLiteral", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprByteLiteral(ExprByteLiteral expr, TCtx arg)
        {
            var res = this.Visit(expr, "ByteLiteral", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprCase(ExprCase expr, TCtx arg)
        {
            var res = this.Visit(expr, "Case", arg, out var argOut) && this.Accept("Cases",expr.Cases, argOut) && this.Accept("DefaultValue",expr.DefaultValue, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprCaseWhenThen(ExprCaseWhenThen expr, TCtx arg)
        {
            var res = this.Visit(expr, "CaseWhenThen", arg, out var argOut) && this.Accept("Condition",expr.Condition, argOut) && this.Accept("Value",expr.Value, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprCast(ExprCast expr, TCtx arg)
        {
            var res = this.Visit(expr, "Cast", arg, out var argOut) && this.Accept("Expression",expr.Expression, argOut) && this.Accept("SqlType",expr.SqlType, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprColumn(ExprColumn expr, TCtx arg)
        {
            var res = this.Visit(expr, "Column", arg, out var argOut) && this.Accept("Source",expr.Source, argOut) && this.Accept("ColumnName",expr.ColumnName, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprColumnAlias(ExprColumnAlias expr, TCtx arg)
        {
            var res = this.Visit(expr, "ColumnAlias", arg, out var argOut);
            this.VisitPlainProperty("Name",expr.Name, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprColumnName(ExprColumnName expr, TCtx arg)
        {
            var res = this.Visit(expr, "ColumnName", arg, out var argOut);
            this.VisitPlainProperty("Name",expr.Name, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprColumnSetClause(ExprColumnSetClause expr, TCtx arg)
        {
            var res = this.Visit(expr, "ColumnSetClause", arg, out var argOut) && this.Accept("Column",expr.Column, argOut) && this.Accept("Value",expr.Value, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprCrossedTable(ExprCrossedTable expr, TCtx arg)
        {
            var res = this.Visit(expr, "CrossedTable", arg, out var argOut) && this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprCurrentRowFrameBorder(ExprCurrentRowFrameBorder expr, TCtx arg)
        {
            var res = this.Visit(expr, "CurrentRowFrameBorder", arg, out var argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprDatabaseName(ExprDatabaseName expr, TCtx arg)
        {
            var res = this.Visit(expr, "DatabaseName", arg, out var argOut);
            this.VisitPlainProperty("Name",expr.Name, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprDateAdd(ExprDateAdd expr, TCtx arg)
        {
            var res = this.Visit(expr, "DateAdd", arg, out var argOut) && this.Accept("Date",expr.Date, argOut);
            this.VisitPlainProperty("DatePart",expr.DatePart, argOut);
            this.VisitPlainProperty("Number",expr.Number, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprDateTimeLiteral(ExprDateTimeLiteral expr, TCtx arg)
        {
            var res = this.Visit(expr, "DateTimeLiteral", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprDbSchema(ExprDbSchema expr, TCtx arg)
        {
            var res = this.Visit(expr, "DbSchema", arg, out var argOut) && this.Accept("Database",expr.Database, argOut) && this.Accept("Schema",expr.Schema, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprDecimalLiteral(ExprDecimalLiteral expr, TCtx arg)
        {
            var res = this.Visit(expr, "DecimalLiteral", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprDefault(ExprDefault expr, TCtx arg)
        {
            var res = this.Visit(expr, "Default", arg, out var argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprDelete(ExprDelete expr, TCtx arg)
        {
            var res = this.Visit(expr, "Delete", arg, out var argOut) && this.Accept("Target",expr.Target, argOut) && this.Accept("Source",expr.Source, argOut) && this.Accept("Filter",expr.Filter, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprDeleteOutput(ExprDeleteOutput expr, TCtx arg)
        {
            var res = this.Visit(expr, "DeleteOutput", arg, out var argOut) && this.Accept("Delete",expr.Delete, argOut) && this.Accept("OutputColumns",expr.OutputColumns, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprDerivedTableQuery(ExprDerivedTableQuery expr, TCtx arg)
        {
            var res = this.Visit(expr, "DerivedTableQuery", arg, out var argOut) && this.Accept("Query",expr.Query, argOut) && this.Accept("Alias",expr.Alias, argOut) && this.Accept("Columns",expr.Columns, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprDerivedTableValues(ExprDerivedTableValues expr, TCtx arg)
        {
            var res = this.Visit(expr, "DerivedTableValues", arg, out var argOut) && this.Accept("Values",expr.Values, argOut) && this.Accept("Alias",expr.Alias, argOut) && this.Accept("Columns",expr.Columns, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprDiv(ExprDiv expr, TCtx arg)
        {
            var res = this.Visit(expr, "Div", arg, out var argOut) && this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprDoubleLiteral(ExprDoubleLiteral expr, TCtx arg)
        {
            var res = this.Visit(expr, "DoubleLiteral", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprExists(ExprExists expr, TCtx arg)
        {
            var res = this.Visit(expr, "Exists", arg, out var argOut) && this.Accept("SubQuery",expr.SubQuery, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprExprMergeNotMatchedInsert(ExprExprMergeNotMatchedInsert expr, TCtx arg)
        {
            var res = this.Visit(expr, "ExprMergeNotMatchedInsert", arg, out var argOut) && this.Accept("And",expr.And, argOut) && this.Accept("Columns",expr.Columns, argOut) && this.Accept("Values",expr.Values, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprExprMergeNotMatchedInsertDefault(ExprExprMergeNotMatchedInsertDefault expr, TCtx arg)
        {
            var res = this.Visit(expr, "ExprMergeNotMatchedInsertDefault", arg, out var argOut) && this.Accept("And",expr.And, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprFrameClause(ExprFrameClause expr, TCtx arg)
        {
            var res = this.Visit(expr, "FrameClause", arg, out var argOut) && this.Accept("Start",expr.Start, argOut) && this.Accept("End",expr.End, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprFuncCoalesce(ExprFuncCoalesce expr, TCtx arg)
        {
            var res = this.Visit(expr, "FuncCoalesce", arg, out var argOut) && this.Accept("Test",expr.Test, argOut) && this.Accept("Alts",expr.Alts, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprFuncIsNull(ExprFuncIsNull expr, TCtx arg)
        {
            var res = this.Visit(expr, "FuncIsNull", arg, out var argOut) && this.Accept("Test",expr.Test, argOut) && this.Accept("Alt",expr.Alt, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprFunctionName(ExprFunctionName expr, TCtx arg)
        {
            var res = this.Visit(expr, "FunctionName", arg, out var argOut);
            this.VisitPlainProperty("BuiltIn",expr.BuiltIn, argOut);
            this.VisitPlainProperty("Name",expr.Name, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprGetDate(ExprGetDate expr, TCtx arg)
        {
            var res = this.Visit(expr, "GetDate", arg, out var argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprGetUtcDate(ExprGetUtcDate expr, TCtx arg)
        {
            var res = this.Visit(expr, "GetUtcDate", arg, out var argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprGuidLiteral(ExprGuidLiteral expr, TCtx arg)
        {
            var res = this.Visit(expr, "GuidLiteral", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprInSubQuery(ExprInSubQuery expr, TCtx arg)
        {
            var res = this.Visit(expr, "InSubQuery", arg, out var argOut) && this.Accept("TestExpression",expr.TestExpression, argOut) && this.Accept("SubQuery",expr.SubQuery, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprInValues(ExprInValues expr, TCtx arg)
        {
            var res = this.Visit(expr, "InValues", arg, out var argOut) && this.Accept("TestExpression",expr.TestExpression, argOut) && this.Accept("Items",expr.Items, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprInsert(ExprInsert expr, TCtx arg)
        {
            var res = this.Visit(expr, "Insert", arg, out var argOut) && this.Accept("Target",expr.Target, argOut) && this.Accept("TargetColumns",expr.TargetColumns, argOut) && this.Accept("Source",expr.Source, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprInsertOutput(ExprInsertOutput expr, TCtx arg)
        {
            var res = this.Visit(expr, "InsertOutput", arg, out var argOut) && this.Accept("Insert",expr.Insert, argOut) && this.Accept("OutputColumns",expr.OutputColumns, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprInsertQuery(ExprInsertQuery expr, TCtx arg)
        {
            var res = this.Visit(expr, "InsertQuery", arg, out var argOut) && this.Accept("Query",expr.Query, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprInsertValueRow(ExprInsertValueRow expr, TCtx arg)
        {
            var res = this.Visit(expr, "InsertValueRow", arg, out var argOut) && this.Accept("Items",expr.Items, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprInsertValues(ExprInsertValues expr, TCtx arg)
        {
            var res = this.Visit(expr, "InsertValues", arg, out var argOut) && this.Accept("Items",expr.Items, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprInt16Literal(ExprInt16Literal expr, TCtx arg)
        {
            var res = this.Visit(expr, "Int16Literal", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprInt32Literal(ExprInt32Literal expr, TCtx arg)
        {
            var res = this.Visit(expr, "Int32Literal", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprInt64Literal(ExprInt64Literal expr, TCtx arg)
        {
            var res = this.Visit(expr, "Int64Literal", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprIsNull(ExprIsNull expr, TCtx arg)
        {
            var res = this.Visit(expr, "IsNull", arg, out var argOut) && this.Accept("Test",expr.Test, argOut);
            this.VisitPlainProperty("Not",expr.Not, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprJoinedTable(ExprJoinedTable expr, TCtx arg)
        {
            var res = this.Visit(expr, "JoinedTable", arg, out var argOut) && this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut) && this.Accept("SearchCondition",expr.SearchCondition, argOut);
            this.VisitPlainProperty("JoinType",expr.JoinType, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprLike(ExprLike expr, TCtx arg)
        {
            var res = this.Visit(expr, "Like", arg, out var argOut) && this.Accept("Test",expr.Test, argOut) && this.Accept("Pattern",expr.Pattern, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprMerge(ExprMerge expr, TCtx arg)
        {
            var res = this.Visit(expr, "Merge", arg, out var argOut) && this.Accept("TargetTable",expr.TargetTable, argOut) && this.Accept("Source",expr.Source, argOut) && this.Accept("On",expr.On, argOut) && this.Accept("WhenMatched",expr.WhenMatched, argOut) && this.Accept("WhenNotMatchedByTarget",expr.WhenNotMatchedByTarget, argOut) && this.Accept("WhenNotMatchedBySource",expr.WhenNotMatchedBySource, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprMergeMatchedDelete(ExprMergeMatchedDelete expr, TCtx arg)
        {
            var res = this.Visit(expr, "MergeMatchedDelete", arg, out var argOut) && this.Accept("And",expr.And, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprMergeMatchedUpdate(ExprMergeMatchedUpdate expr, TCtx arg)
        {
            var res = this.Visit(expr, "MergeMatchedUpdate", arg, out var argOut) && this.Accept("And",expr.And, argOut) && this.Accept("Set",expr.Set, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprMergeOutput(ExprMergeOutput expr, TCtx arg)
        {
            var res = this.Visit(expr, "MergeOutput", arg, out var argOut) && this.Accept("TargetTable",expr.TargetTable, argOut) && this.Accept("Source",expr.Source, argOut) && this.Accept("On",expr.On, argOut) && this.Accept("WhenMatched",expr.WhenMatched, argOut) && this.Accept("WhenNotMatchedByTarget",expr.WhenNotMatchedByTarget, argOut) && this.Accept("WhenNotMatchedBySource",expr.WhenNotMatchedBySource, argOut) && this.Accept("Output",expr.Output, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprModulo(ExprModulo expr, TCtx arg)
        {
            var res = this.Visit(expr, "Modulo", arg, out var argOut) && this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprMul(ExprMul expr, TCtx arg)
        {
            var res = this.Visit(expr, "Mul", arg, out var argOut) && this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprNull(ExprNull expr, TCtx arg)
        {
            var res = this.Visit(expr, "Null", arg, out var argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprOffsetFetch(ExprOffsetFetch expr, TCtx arg)
        {
            var res = this.Visit(expr, "OffsetFetch", arg, out var argOut) && this.Accept("Offset",expr.Offset, argOut) && this.Accept("Fetch",expr.Fetch, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprOrderBy(ExprOrderBy expr, TCtx arg)
        {
            var res = this.Visit(expr, "OrderBy", arg, out var argOut) && this.Accept("OrderList",expr.OrderList, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprOrderByItem(ExprOrderByItem expr, TCtx arg)
        {
            var res = this.Visit(expr, "OrderByItem", arg, out var argOut) && this.Accept("Value",expr.Value, argOut);
            this.VisitPlainProperty("Descendant",expr.Descendant, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprOrderByOffsetFetch(ExprOrderByOffsetFetch expr, TCtx arg)
        {
            var res = this.Visit(expr, "OrderByOffsetFetch", arg, out var argOut) && this.Accept("OrderList",expr.OrderList, argOut) && this.Accept("OffsetFetch",expr.OffsetFetch, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprOutput(ExprOutput expr, TCtx arg)
        {
            var res = this.Visit(expr, "Output", arg, out var argOut) && this.Accept("Columns",expr.Columns, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprOutputAction(ExprOutputAction expr, TCtx arg)
        {
            var res = this.Visit(expr, "OutputAction", arg, out var argOut) && this.Accept("Alias",expr.Alias, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprOutputColumn(ExprOutputColumn expr, TCtx arg)
        {
            var res = this.Visit(expr, "OutputColumn", arg, out var argOut) && this.Accept("Column",expr.Column, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprOutputColumnDeleted(ExprOutputColumnDeleted expr, TCtx arg)
        {
            var res = this.Visit(expr, "OutputColumnDeleted", arg, out var argOut) && this.Accept("ColumnName",expr.ColumnName, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprOutputColumnInserted(ExprOutputColumnInserted expr, TCtx arg)
        {
            var res = this.Visit(expr, "OutputColumnInserted", arg, out var argOut) && this.Accept("ColumnName",expr.ColumnName, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprOver(ExprOver expr, TCtx arg)
        {
            var res = this.Visit(expr, "Over", arg, out var argOut) && this.Accept("Partitions",expr.Partitions, argOut) && this.Accept("OrderBy",expr.OrderBy, argOut) && this.Accept("FrameClause",expr.FrameClause, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprQueryExpression(ExprQueryExpression expr, TCtx arg)
        {
            var res = this.Visit(expr, "QueryExpression", arg, out var argOut) && this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            this.VisitPlainProperty("QueryExpressionType",expr.QueryExpressionType, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprQuerySpecification(ExprQuerySpecification expr, TCtx arg)
        {
            var res = this.Visit(expr, "QuerySpecification", arg, out var argOut) && this.Accept("SelectList",expr.SelectList, argOut) && this.Accept("Top",expr.Top, argOut) && this.Accept("From",expr.From, argOut) && this.Accept("Where",expr.Where, argOut) && this.Accept("GroupBy",expr.GroupBy, argOut);
            this.VisitPlainProperty("Distinct",expr.Distinct, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprScalarFunction(ExprScalarFunction expr, TCtx arg)
        {
            var res = this.Visit(expr, "ScalarFunction", arg, out var argOut) && this.Accept("Schema",expr.Schema, argOut) && this.Accept("Name",expr.Name, argOut) && this.Accept("Arguments",expr.Arguments, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprSchemaName(ExprSchemaName expr, TCtx arg)
        {
            var res = this.Visit(expr, "SchemaName", arg, out var argOut);
            this.VisitPlainProperty("Name",expr.Name, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprSelect(ExprSelect expr, TCtx arg)
        {
            var res = this.Visit(expr, "Select", arg, out var argOut) && this.Accept("SelectQuery",expr.SelectQuery, argOut) && this.Accept("OrderBy",expr.OrderBy, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprSelectOffsetFetch(ExprSelectOffsetFetch expr, TCtx arg)
        {
            var res = this.Visit(expr, "SelectOffsetFetch", arg, out var argOut) && this.Accept("SelectQuery",expr.SelectQuery, argOut) && this.Accept("OrderBy",expr.OrderBy, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprStringConcat(ExprStringConcat expr, TCtx arg)
        {
            var res = this.Visit(expr, "StringConcat", arg, out var argOut) && this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprStringLiteral(ExprStringLiteral expr, TCtx arg)
        {
            var res = this.Visit(expr, "StringLiteral", arg, out var argOut);
            this.VisitPlainProperty("Value",expr.Value, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprSub(ExprSub expr, TCtx arg)
        {
            var res = this.Visit(expr, "Sub", arg, out var argOut) && this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprSum(ExprSum expr, TCtx arg)
        {
            var res = this.Visit(expr, "Sum", arg, out var argOut) && this.Accept("Left",expr.Left, argOut) && this.Accept("Right",expr.Right, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprTable(ExprTable expr, TCtx arg)
        {
            var res = this.Visit(expr, "Table", arg, out var argOut) && this.Accept("FullName",expr.FullName, argOut) && this.Accept("Alias",expr.Alias, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprTableAlias(ExprTableAlias expr, TCtx arg)
        {
            var res = this.Visit(expr, "TableAlias", arg, out var argOut) && this.Accept("Alias",expr.Alias, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprTableFullName(ExprTableFullName expr, TCtx arg)
        {
            var res = this.Visit(expr, "TableFullName", arg, out var argOut) && this.Accept("DbSchema",expr.DbSchema, argOut) && this.Accept("TableName",expr.TableName, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprTableName(ExprTableName expr, TCtx arg)
        {
            var res = this.Visit(expr, "TableName", arg, out var argOut);
            this.VisitPlainProperty("Name",expr.Name, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprTableValueConstructor(ExprTableValueConstructor expr, TCtx arg)
        {
            var res = this.Visit(expr, "TableValueConstructor", arg, out var argOut) && this.Accept("Items",expr.Items, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprTempTableName(ExprTempTableName expr, TCtx arg)
        {
            var res = this.Visit(expr, "TempTableName", arg, out var argOut);
            this.VisitPlainProperty("Name",expr.Name, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprTypeBoolean(ExprTypeBoolean expr, TCtx arg)
        {
            var res = this.Visit(expr, "TypeBoolean", arg, out var argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprTypeByte(ExprTypeByte expr, TCtx arg)
        {
            var res = this.Visit(expr, "TypeByte", arg, out var argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprTypeDateTime(ExprTypeDateTime expr, TCtx arg)
        {
            var res = this.Visit(expr, "TypeDateTime", arg, out var argOut);
            this.VisitPlainProperty("IsDate",expr.IsDate, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprTypeDecimal(ExprTypeDecimal expr, TCtx arg)
        {
            var res = this.Visit(expr, "TypeDecimal", arg, out var argOut);
            this.VisitPlainProperty("PrecisionScale",expr.PrecisionScale, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprTypeDouble(ExprTypeDouble expr, TCtx arg)
        {
            var res = this.Visit(expr, "TypeDouble", arg, out var argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprTypeGuid(ExprTypeGuid expr, TCtx arg)
        {
            var res = this.Visit(expr, "TypeGuid", arg, out var argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprTypeInt16(ExprTypeInt16 expr, TCtx arg)
        {
            var res = this.Visit(expr, "TypeInt16", arg, out var argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprTypeInt32(ExprTypeInt32 expr, TCtx arg)
        {
            var res = this.Visit(expr, "TypeInt32", arg, out var argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprTypeInt64(ExprTypeInt64 expr, TCtx arg)
        {
            var res = this.Visit(expr, "TypeInt64", arg, out var argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprTypeString(ExprTypeString expr, TCtx arg)
        {
            var res = this.Visit(expr, "TypeString", arg, out var argOut);
            this.VisitPlainProperty("Size",expr.Size, argOut);
            this.VisitPlainProperty("IsUnicode",expr.IsUnicode, argOut);
            this.VisitPlainProperty("IsText",expr.IsText, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprUnboundedFrameBorder(ExprUnboundedFrameBorder expr, TCtx arg)
        {
            var res = this.Visit(expr, "UnboundedFrameBorder", arg, out var argOut);
            this.VisitPlainProperty("FrameBorderDirection",expr.FrameBorderDirection, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprUnsafeValue(ExprUnsafeValue expr, TCtx arg)
        {
            var res = this.Visit(expr, "UnsafeValue", arg, out var argOut);
            this.VisitPlainProperty("UnsafeValue",expr.UnsafeValue, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprUpdate(ExprUpdate expr, TCtx arg)
        {
            var res = this.Visit(expr, "Update", arg, out var argOut) && this.Accept("Target",expr.Target, argOut) && this.Accept("SetClause",expr.SetClause, argOut) && this.Accept("Source",expr.Source, argOut) && this.Accept("Filter",expr.Filter, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprValueFrameBorder(ExprValueFrameBorder expr, TCtx arg)
        {
            var res = this.Visit(expr, "ValueFrameBorder", arg, out var argOut) && this.Accept("Value",expr.Value, argOut);
            this.VisitPlainProperty("FrameBorderDirection",expr.FrameBorderDirection, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        public bool VisitExprValueRow(ExprValueRow expr, TCtx arg)
        {
            var res = this.Visit(expr, "ValueRow", arg, out var argOut) && this.Accept("Items",expr.Items, argOut);
            this._visitor.EndVisitExpr(expr, arg);
            return res;
        }
        //CodeGenEnd
    }
}
