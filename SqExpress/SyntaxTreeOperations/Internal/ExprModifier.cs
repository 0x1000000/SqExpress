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
    internal class ExprModifier : IExprVisitor<IExpr?, Func<IExpr, IExpr?>>
    {
        public static readonly ExprModifier Instance = new ExprModifier();

        private ExprModifier() { }

        private T AcceptItem<T>(T item, Func<IExpr, IExpr?> arg) where T : IExpr
        {
            var result = item.Accept(this, arg);
            return result switch
            {
                null => throw new SqExpressException(
                    $"Syntax tree modification - tree node of type {typeof(T).Name} cannot be null"),
                T t => t,
                _ => throw new SqExpressException(
                    $"Syntax tree modification - could not cast a new tree node to type {typeof(T).Name}")
            };
        }

        private T? AcceptNullableItem<T>(T? item, Func<IExpr, IExpr?> arg) where T : class, IExpr
        {
            var result = item?.Accept(this, arg);
            return result switch
            {
                null => null,
                T t => t,
                _ => throw new SqExpressException(
                    $"Syntax tree modification - could not cast a new tree node to type {typeof(T).Name}")
            };
        }

        private IReadOnlyList<T>? AcceptNullCollection<T>(IReadOnlyList<T>? collection, Func<IExpr, IExpr?> arg) where T : IExpr
        {
            return this.AcceptCollection(collection, true, arg);
        }

        private IReadOnlyList<T> AcceptNotNullCollection<T>(IReadOnlyList<T> collection, Func<IExpr, IExpr?> arg) where T : IExpr
        {
            return this.AcceptCollection(collection, false, arg)!;
        }

        private IReadOnlyList<T>? AcceptCollection<T>(IReadOnlyList<T>? collection, bool allowNull, Func<IExpr, IExpr?> arg) where T : IExpr
        {
            if (collection == null)
            {
                if (!allowNull)
                {
                    throw new SqExpressException(
                        $"Syntax tree modification - parameter of type {typeof(T).Name} cannot be null");
                }
                return collection;
            }

            bool detect = false;
            List<T> buffer = new List<T>(collection.Count);
            foreach (var expr in collection)
            {
                var newItem = expr.Accept(this, arg);
                if (ReferenceEquals(expr, newItem))
                {
                    buffer.Add(expr);
                }
                else
                {
                    detect = true;
                    if (newItem != null)
                    {
                        if (newItem is T t)
                        {
                            buffer.Add(t);
                        }
                        else
                        {
                            throw new SqExpressException(
                                $"Syntax tree modification - could not cast a new tree node to type {typeof(T).Name}");
                        }
                    }
                }
            }

            if (detect)
            {
                if (buffer.Count > 0)
                {
                    return buffer;
                }

                if (allowNull)
                {
                    return null;
                }
                throw new SqExpressException(
                    $"Syntax tree modification - collection of {typeof(T).Name} items cannot be empty");
            }

            return collection;
        }


        //CodeGenStart
        public IExpr? VisitExprAggregateFunction(ExprAggregateFunction exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newName = this.AcceptItem(exprIn.Name, modifier);
            var newExpression = this.AcceptItem(exprIn.Expression, modifier);
            if(!ReferenceEquals(exprIn.Name, newName) || !ReferenceEquals(exprIn.Expression, newExpression))
            {
                exprIn = new ExprAggregateFunction(name: newName, expression: newExpression, isDistinct: exprIn.IsDistinct);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprAlias(ExprAlias exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprAliasGuid(ExprAliasGuid exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprAliasedColumn(ExprAliasedColumn exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newColumn = this.AcceptItem(exprIn.Column, modifier);
            var newAlias = this.AcceptNullableItem(exprIn.Alias, modifier);
            if(!ReferenceEquals(exprIn.Column, newColumn) || !ReferenceEquals(exprIn.Alias, newAlias))
            {
                exprIn = new ExprAliasedColumn(column: newColumn, alias: newAlias);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprAliasedColumnName(ExprAliasedColumnName exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newColumn = this.AcceptItem(exprIn.Column, modifier);
            var newAlias = this.AcceptNullableItem(exprIn.Alias, modifier);
            if(!ReferenceEquals(exprIn.Column, newColumn) || !ReferenceEquals(exprIn.Alias, newAlias))
            {
                exprIn = new ExprAliasedColumnName(column: newColumn, alias: newAlias);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprAliasedSelecting(ExprAliasedSelecting exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newValue = this.AcceptItem(exprIn.Value, modifier);
            var newAlias = this.AcceptItem(exprIn.Alias, modifier);
            if(!ReferenceEquals(exprIn.Value, newValue) || !ReferenceEquals(exprIn.Alias, newAlias))
            {
                exprIn = new ExprAliasedSelecting(value: newValue, alias: newAlias);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprAllColumns(ExprAllColumns exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newSource = this.AcceptNullableItem(exprIn.Source, modifier);
            if(!ReferenceEquals(exprIn.Source, newSource))
            {
                exprIn = new ExprAllColumns(source: newSource);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprAnalyticFunction(ExprAnalyticFunction exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newName = this.AcceptItem(exprIn.Name, modifier);
            var newArguments = this.AcceptNullCollection(exprIn.Arguments, modifier);
            var newOver = this.AcceptItem(exprIn.Over, modifier);
            if(!ReferenceEquals(exprIn.Name, newName) || !ReferenceEquals(exprIn.Arguments, newArguments) || !ReferenceEquals(exprIn.Over, newOver))
            {
                exprIn = new ExprAnalyticFunction(name: newName, arguments: newArguments, over: newOver);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprBoolLiteral(ExprBoolLiteral exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprBooleanAnd(ExprBooleanAnd exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newLeft = this.AcceptItem(exprIn.Left, modifier);
            var newRight = this.AcceptItem(exprIn.Right, modifier);
            if(!ReferenceEquals(exprIn.Left, newLeft) || !ReferenceEquals(exprIn.Right, newRight))
            {
                exprIn = new ExprBooleanAnd(left: newLeft, right: newRight);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprBooleanEq(ExprBooleanEq exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newLeft = this.AcceptItem(exprIn.Left, modifier);
            var newRight = this.AcceptItem(exprIn.Right, modifier);
            if(!ReferenceEquals(exprIn.Left, newLeft) || !ReferenceEquals(exprIn.Right, newRight))
            {
                exprIn = new ExprBooleanEq(left: newLeft, right: newRight);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprBooleanGt(ExprBooleanGt exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newLeft = this.AcceptItem(exprIn.Left, modifier);
            var newRight = this.AcceptItem(exprIn.Right, modifier);
            if(!ReferenceEquals(exprIn.Left, newLeft) || !ReferenceEquals(exprIn.Right, newRight))
            {
                exprIn = new ExprBooleanGt(left: newLeft, right: newRight);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprBooleanGtEq(ExprBooleanGtEq exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newLeft = this.AcceptItem(exprIn.Left, modifier);
            var newRight = this.AcceptItem(exprIn.Right, modifier);
            if(!ReferenceEquals(exprIn.Left, newLeft) || !ReferenceEquals(exprIn.Right, newRight))
            {
                exprIn = new ExprBooleanGtEq(left: newLeft, right: newRight);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprBooleanLt(ExprBooleanLt exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newLeft = this.AcceptItem(exprIn.Left, modifier);
            var newRight = this.AcceptItem(exprIn.Right, modifier);
            if(!ReferenceEquals(exprIn.Left, newLeft) || !ReferenceEquals(exprIn.Right, newRight))
            {
                exprIn = new ExprBooleanLt(left: newLeft, right: newRight);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprBooleanLtEq(ExprBooleanLtEq exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newLeft = this.AcceptItem(exprIn.Left, modifier);
            var newRight = this.AcceptItem(exprIn.Right, modifier);
            if(!ReferenceEquals(exprIn.Left, newLeft) || !ReferenceEquals(exprIn.Right, newRight))
            {
                exprIn = new ExprBooleanLtEq(left: newLeft, right: newRight);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprBooleanNot(ExprBooleanNot exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newExpr = this.AcceptItem(exprIn.Expr, modifier);
            if(!ReferenceEquals(exprIn.Expr, newExpr))
            {
                exprIn = new ExprBooleanNot(expr: newExpr);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprBooleanNotEq(ExprBooleanNotEq exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newLeft = this.AcceptItem(exprIn.Left, modifier);
            var newRight = this.AcceptItem(exprIn.Right, modifier);
            if(!ReferenceEquals(exprIn.Left, newLeft) || !ReferenceEquals(exprIn.Right, newRight))
            {
                exprIn = new ExprBooleanNotEq(left: newLeft, right: newRight);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprBooleanOr(ExprBooleanOr exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newLeft = this.AcceptItem(exprIn.Left, modifier);
            var newRight = this.AcceptItem(exprIn.Right, modifier);
            if(!ReferenceEquals(exprIn.Left, newLeft) || !ReferenceEquals(exprIn.Right, newRight))
            {
                exprIn = new ExprBooleanOr(left: newLeft, right: newRight);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprByteArrayLiteral(ExprByteArrayLiteral exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprByteLiteral(ExprByteLiteral exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprCase(ExprCase exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newCases = this.AcceptNotNullCollection(exprIn.Cases, modifier);
            var newDefaultValue = this.AcceptItem(exprIn.DefaultValue, modifier);
            if(!ReferenceEquals(exprIn.Cases, newCases) || !ReferenceEquals(exprIn.DefaultValue, newDefaultValue))
            {
                exprIn = new ExprCase(cases: newCases, defaultValue: newDefaultValue);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprCaseWhenThen(ExprCaseWhenThen exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newCondition = this.AcceptItem(exprIn.Condition, modifier);
            var newValue = this.AcceptItem(exprIn.Value, modifier);
            if(!ReferenceEquals(exprIn.Condition, newCondition) || !ReferenceEquals(exprIn.Value, newValue))
            {
                exprIn = new ExprCaseWhenThen(condition: newCondition, value: newValue);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprCast(ExprCast exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newExpression = this.AcceptItem(exprIn.Expression, modifier);
            var newSqlType = this.AcceptItem(exprIn.SqlType, modifier);
            if(!ReferenceEquals(exprIn.Expression, newExpression) || !ReferenceEquals(exprIn.SqlType, newSqlType))
            {
                exprIn = new ExprCast(expression: newExpression, sqlType: newSqlType);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprColumn(ExprColumn exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newSource = this.AcceptNullableItem(exprIn.Source, modifier);
            var newColumnName = this.AcceptItem(exprIn.ColumnName, modifier);
            if(!ReferenceEquals(exprIn.Source, newSource) || !ReferenceEquals(exprIn.ColumnName, newColumnName))
            {
                exprIn = new ExprColumn(source: newSource, columnName: newColumnName);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprColumnAlias(ExprColumnAlias exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprColumnName(ExprColumnName exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprColumnSetClause(ExprColumnSetClause exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newColumn = this.AcceptItem(exprIn.Column, modifier);
            var newValue = this.AcceptItem(exprIn.Value, modifier);
            if(!ReferenceEquals(exprIn.Column, newColumn) || !ReferenceEquals(exprIn.Value, newValue))
            {
                exprIn = new ExprColumnSetClause(column: newColumn, value: newValue);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprCrossedTable(ExprCrossedTable exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newLeft = this.AcceptItem(exprIn.Left, modifier);
            var newRight = this.AcceptItem(exprIn.Right, modifier);
            if(!ReferenceEquals(exprIn.Left, newLeft) || !ReferenceEquals(exprIn.Right, newRight))
            {
                exprIn = new ExprCrossedTable(left: newLeft, right: newRight);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprCurrentRowFrameBorder(ExprCurrentRowFrameBorder exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprDatabaseName(ExprDatabaseName exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprDateAdd(ExprDateAdd exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newDate = this.AcceptItem(exprIn.Date, modifier);
            if(!ReferenceEquals(exprIn.Date, newDate))
            {
                exprIn = new ExprDateAdd(date: newDate, datePart: exprIn.DatePart, number: exprIn.Number);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprDateTimeLiteral(ExprDateTimeLiteral exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprDbSchema(ExprDbSchema exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newDatabase = this.AcceptNullableItem(exprIn.Database, modifier);
            var newSchema = this.AcceptItem(exprIn.Schema, modifier);
            if(!ReferenceEquals(exprIn.Database, newDatabase) || !ReferenceEquals(exprIn.Schema, newSchema))
            {
                exprIn = new ExprDbSchema(database: newDatabase, schema: newSchema);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprDecimalLiteral(ExprDecimalLiteral exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprDefault(ExprDefault exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprDelete(ExprDelete exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newTarget = this.AcceptItem(exprIn.Target, modifier);
            var newSource = this.AcceptNullableItem(exprIn.Source, modifier);
            var newFilter = this.AcceptNullableItem(exprIn.Filter, modifier);
            if(!ReferenceEquals(exprIn.Target, newTarget) || !ReferenceEquals(exprIn.Source, newSource) || !ReferenceEquals(exprIn.Filter, newFilter))
            {
                exprIn = new ExprDelete(target: newTarget, source: newSource, filter: newFilter);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprDeleteOutput(ExprDeleteOutput exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newDelete = this.AcceptItem(exprIn.Delete, modifier);
            var newOutputColumns = this.AcceptNotNullCollection(exprIn.OutputColumns, modifier);
            if(!ReferenceEquals(exprIn.Delete, newDelete) || !ReferenceEquals(exprIn.OutputColumns, newOutputColumns))
            {
                exprIn = new ExprDeleteOutput(delete: newDelete, outputColumns: newOutputColumns);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprDerivedTableQuery(ExprDerivedTableQuery exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newQuery = this.AcceptItem(exprIn.Query, modifier);
            var newAlias = this.AcceptItem(exprIn.Alias, modifier);
            var newColumns = this.AcceptNullCollection(exprIn.Columns, modifier);
            if(!ReferenceEquals(exprIn.Query, newQuery) || !ReferenceEquals(exprIn.Alias, newAlias) || !ReferenceEquals(exprIn.Columns, newColumns))
            {
                exprIn = new ExprDerivedTableQuery(query: newQuery, alias: newAlias, columns: newColumns);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprDerivedTableValues(ExprDerivedTableValues exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newValues = this.AcceptItem(exprIn.Values, modifier);
            var newAlias = this.AcceptItem(exprIn.Alias, modifier);
            var newColumns = this.AcceptNotNullCollection(exprIn.Columns, modifier);
            if(!ReferenceEquals(exprIn.Values, newValues) || !ReferenceEquals(exprIn.Alias, newAlias) || !ReferenceEquals(exprIn.Columns, newColumns))
            {
                exprIn = new ExprDerivedTableValues(values: newValues, alias: newAlias, columns: newColumns);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprDiv(ExprDiv exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newLeft = this.AcceptItem(exprIn.Left, modifier);
            var newRight = this.AcceptItem(exprIn.Right, modifier);
            if(!ReferenceEquals(exprIn.Left, newLeft) || !ReferenceEquals(exprIn.Right, newRight))
            {
                exprIn = new ExprDiv(left: newLeft, right: newRight);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprDoubleLiteral(ExprDoubleLiteral exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprExists(ExprExists exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newSubQuery = this.AcceptItem(exprIn.SubQuery, modifier);
            if(!ReferenceEquals(exprIn.SubQuery, newSubQuery))
            {
                exprIn = new ExprExists(subQuery: newSubQuery);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprExprMergeNotMatchedInsert(ExprExprMergeNotMatchedInsert exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newAnd = this.AcceptNullableItem(exprIn.And, modifier);
            var newColumns = this.AcceptNotNullCollection(exprIn.Columns, modifier);
            var newValues = this.AcceptNotNullCollection(exprIn.Values, modifier);
            if(!ReferenceEquals(exprIn.And, newAnd) || !ReferenceEquals(exprIn.Columns, newColumns) || !ReferenceEquals(exprIn.Values, newValues))
            {
                exprIn = new ExprExprMergeNotMatchedInsert(and: newAnd, columns: newColumns, values: newValues);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprExprMergeNotMatchedInsertDefault(ExprExprMergeNotMatchedInsertDefault exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newAnd = this.AcceptNullableItem(exprIn.And, modifier);
            if(!ReferenceEquals(exprIn.And, newAnd))
            {
                exprIn = new ExprExprMergeNotMatchedInsertDefault(and: newAnd);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprFrameClause(ExprFrameClause exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newStart = this.AcceptItem(exprIn.Start, modifier);
            var newEnd = this.AcceptNullableItem(exprIn.End, modifier);
            if(!ReferenceEquals(exprIn.Start, newStart) || !ReferenceEquals(exprIn.End, newEnd))
            {
                exprIn = new ExprFrameClause(start: newStart, end: newEnd);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprFuncCoalesce(ExprFuncCoalesce exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newTest = this.AcceptItem(exprIn.Test, modifier);
            var newAlts = this.AcceptNotNullCollection(exprIn.Alts, modifier);
            if(!ReferenceEquals(exprIn.Test, newTest) || !ReferenceEquals(exprIn.Alts, newAlts))
            {
                exprIn = new ExprFuncCoalesce(test: newTest, alts: newAlts);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprFuncIsNull(ExprFuncIsNull exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newTest = this.AcceptItem(exprIn.Test, modifier);
            var newAlt = this.AcceptItem(exprIn.Alt, modifier);
            if(!ReferenceEquals(exprIn.Test, newTest) || !ReferenceEquals(exprIn.Alt, newAlt))
            {
                exprIn = new ExprFuncIsNull(test: newTest, alt: newAlt);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprFunctionName(ExprFunctionName exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprGetDate(ExprGetDate exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprGetUtcDate(ExprGetUtcDate exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprGuidLiteral(ExprGuidLiteral exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprInSubQuery(ExprInSubQuery exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newTestExpression = this.AcceptItem(exprIn.TestExpression, modifier);
            var newSubQuery = this.AcceptItem(exprIn.SubQuery, modifier);
            if(!ReferenceEquals(exprIn.TestExpression, newTestExpression) || !ReferenceEquals(exprIn.SubQuery, newSubQuery))
            {
                exprIn = new ExprInSubQuery(testExpression: newTestExpression, subQuery: newSubQuery);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprInValues(ExprInValues exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newTestExpression = this.AcceptItem(exprIn.TestExpression, modifier);
            var newItems = this.AcceptNotNullCollection(exprIn.Items, modifier);
            if(!ReferenceEquals(exprIn.TestExpression, newTestExpression) || !ReferenceEquals(exprIn.Items, newItems))
            {
                exprIn = new ExprInValues(testExpression: newTestExpression, items: newItems);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprInsert(ExprInsert exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newTarget = this.AcceptItem(exprIn.Target, modifier);
            var newTargetColumns = this.AcceptNullCollection(exprIn.TargetColumns, modifier);
            var newSource = this.AcceptItem(exprIn.Source, modifier);
            if(!ReferenceEquals(exprIn.Target, newTarget) || !ReferenceEquals(exprIn.TargetColumns, newTargetColumns) || !ReferenceEquals(exprIn.Source, newSource))
            {
                exprIn = new ExprInsert(target: newTarget, targetColumns: newTargetColumns, source: newSource);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprInsertOutput(ExprInsertOutput exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newInsert = this.AcceptItem(exprIn.Insert, modifier);
            var newOutputColumns = this.AcceptNotNullCollection(exprIn.OutputColumns, modifier);
            if(!ReferenceEquals(exprIn.Insert, newInsert) || !ReferenceEquals(exprIn.OutputColumns, newOutputColumns))
            {
                exprIn = new ExprInsertOutput(insert: newInsert, outputColumns: newOutputColumns);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprInsertQuery(ExprInsertQuery exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newQuery = this.AcceptItem(exprIn.Query, modifier);
            if(!ReferenceEquals(exprIn.Query, newQuery))
            {
                exprIn = new ExprInsertQuery(query: newQuery);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprInsertValueRow(ExprInsertValueRow exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newItems = this.AcceptNotNullCollection(exprIn.Items, modifier);
            if(!ReferenceEquals(exprIn.Items, newItems))
            {
                exprIn = new ExprInsertValueRow(items: newItems);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprInsertValues(ExprInsertValues exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newItems = this.AcceptNotNullCollection(exprIn.Items, modifier);
            if(!ReferenceEquals(exprIn.Items, newItems))
            {
                exprIn = new ExprInsertValues(items: newItems);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprInt16Literal(ExprInt16Literal exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprInt32Literal(ExprInt32Literal exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprInt64Literal(ExprInt64Literal exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprIsNull(ExprIsNull exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newTest = this.AcceptItem(exprIn.Test, modifier);
            if(!ReferenceEquals(exprIn.Test, newTest))
            {
                exprIn = new ExprIsNull(test: newTest, not: exprIn.Not);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprJoinedTable(ExprJoinedTable exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newLeft = this.AcceptItem(exprIn.Left, modifier);
            var newRight = this.AcceptItem(exprIn.Right, modifier);
            var newSearchCondition = this.AcceptItem(exprIn.SearchCondition, modifier);
            if(!ReferenceEquals(exprIn.Left, newLeft) || !ReferenceEquals(exprIn.Right, newRight) || !ReferenceEquals(exprIn.SearchCondition, newSearchCondition))
            {
                exprIn = new ExprJoinedTable(left: newLeft, right: newRight, searchCondition: newSearchCondition, joinType: exprIn.JoinType);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprLike(ExprLike exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newTest = this.AcceptItem(exprIn.Test, modifier);
            var newPattern = this.AcceptItem(exprIn.Pattern, modifier);
            if(!ReferenceEquals(exprIn.Test, newTest) || !ReferenceEquals(exprIn.Pattern, newPattern))
            {
                exprIn = new ExprLike(test: newTest, pattern: newPattern);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprMerge(ExprMerge exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newTargetTable = this.AcceptItem(exprIn.TargetTable, modifier);
            var newSource = this.AcceptItem(exprIn.Source, modifier);
            var newOn = this.AcceptItem(exprIn.On, modifier);
            var newWhenMatched = this.AcceptNullableItem(exprIn.WhenMatched, modifier);
            var newWhenNotMatchedByTarget = this.AcceptNullableItem(exprIn.WhenNotMatchedByTarget, modifier);
            var newWhenNotMatchedBySource = this.AcceptNullableItem(exprIn.WhenNotMatchedBySource, modifier);
            if(!ReferenceEquals(exprIn.TargetTable, newTargetTable) || !ReferenceEquals(exprIn.Source, newSource) || !ReferenceEquals(exprIn.On, newOn) || !ReferenceEquals(exprIn.WhenMatched, newWhenMatched) || !ReferenceEquals(exprIn.WhenNotMatchedByTarget, newWhenNotMatchedByTarget) || !ReferenceEquals(exprIn.WhenNotMatchedBySource, newWhenNotMatchedBySource))
            {
                exprIn = new ExprMerge(targetTable: newTargetTable, source: newSource, on: newOn, whenMatched: newWhenMatched, whenNotMatchedByTarget: newWhenNotMatchedByTarget, whenNotMatchedBySource: newWhenNotMatchedBySource);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprMergeMatchedDelete(ExprMergeMatchedDelete exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newAnd = this.AcceptNullableItem(exprIn.And, modifier);
            if(!ReferenceEquals(exprIn.And, newAnd))
            {
                exprIn = new ExprMergeMatchedDelete(and: newAnd);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprMergeMatchedUpdate(ExprMergeMatchedUpdate exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newAnd = this.AcceptNullableItem(exprIn.And, modifier);
            var newSet = this.AcceptNotNullCollection(exprIn.Set, modifier);
            if(!ReferenceEquals(exprIn.And, newAnd) || !ReferenceEquals(exprIn.Set, newSet))
            {
                exprIn = new ExprMergeMatchedUpdate(and: newAnd, set: newSet);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprMergeOutput(ExprMergeOutput exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newTargetTable = this.AcceptItem(exprIn.TargetTable, modifier);
            var newSource = this.AcceptItem(exprIn.Source, modifier);
            var newOn = this.AcceptItem(exprIn.On, modifier);
            var newWhenMatched = this.AcceptNullableItem(exprIn.WhenMatched, modifier);
            var newWhenNotMatchedByTarget = this.AcceptNullableItem(exprIn.WhenNotMatchedByTarget, modifier);
            var newWhenNotMatchedBySource = this.AcceptNullableItem(exprIn.WhenNotMatchedBySource, modifier);
            var newOutput = this.AcceptItem(exprIn.Output, modifier);
            if(!ReferenceEquals(exprIn.TargetTable, newTargetTable) || !ReferenceEquals(exprIn.Source, newSource) || !ReferenceEquals(exprIn.On, newOn) || !ReferenceEquals(exprIn.WhenMatched, newWhenMatched) || !ReferenceEquals(exprIn.WhenNotMatchedByTarget, newWhenNotMatchedByTarget) || !ReferenceEquals(exprIn.WhenNotMatchedBySource, newWhenNotMatchedBySource) || !ReferenceEquals(exprIn.Output, newOutput))
            {
                exprIn = new ExprMergeOutput(targetTable: newTargetTable, source: newSource, on: newOn, whenMatched: newWhenMatched, whenNotMatchedByTarget: newWhenNotMatchedByTarget, whenNotMatchedBySource: newWhenNotMatchedBySource, output: newOutput);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprModulo(ExprModulo exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newLeft = this.AcceptItem(exprIn.Left, modifier);
            var newRight = this.AcceptItem(exprIn.Right, modifier);
            if(!ReferenceEquals(exprIn.Left, newLeft) || !ReferenceEquals(exprIn.Right, newRight))
            {
                exprIn = new ExprModulo(left: newLeft, right: newRight);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprMul(ExprMul exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newLeft = this.AcceptItem(exprIn.Left, modifier);
            var newRight = this.AcceptItem(exprIn.Right, modifier);
            if(!ReferenceEquals(exprIn.Left, newLeft) || !ReferenceEquals(exprIn.Right, newRight))
            {
                exprIn = new ExprMul(left: newLeft, right: newRight);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprNull(ExprNull exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprOffsetFetch(ExprOffsetFetch exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newOffset = this.AcceptItem(exprIn.Offset, modifier);
            var newFetch = this.AcceptNullableItem(exprIn.Fetch, modifier);
            if(!ReferenceEquals(exprIn.Offset, newOffset) || !ReferenceEquals(exprIn.Fetch, newFetch))
            {
                exprIn = new ExprOffsetFetch(offset: newOffset, fetch: newFetch);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprOrderBy(ExprOrderBy exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newOrderList = this.AcceptNotNullCollection(exprIn.OrderList, modifier);
            if(!ReferenceEquals(exprIn.OrderList, newOrderList))
            {
                exprIn = new ExprOrderBy(orderList: newOrderList);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprOrderByItem(ExprOrderByItem exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newValue = this.AcceptItem(exprIn.Value, modifier);
            if(!ReferenceEquals(exprIn.Value, newValue))
            {
                exprIn = new ExprOrderByItem(value: newValue, descendant: exprIn.Descendant);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprOrderByOffsetFetch(ExprOrderByOffsetFetch exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newOrderList = this.AcceptNotNullCollection(exprIn.OrderList, modifier);
            var newOffsetFetch = this.AcceptItem(exprIn.OffsetFetch, modifier);
            if(!ReferenceEquals(exprIn.OrderList, newOrderList) || !ReferenceEquals(exprIn.OffsetFetch, newOffsetFetch))
            {
                exprIn = new ExprOrderByOffsetFetch(orderList: newOrderList, offsetFetch: newOffsetFetch);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprOutput(ExprOutput exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newColumns = this.AcceptNotNullCollection(exprIn.Columns, modifier);
            if(!ReferenceEquals(exprIn.Columns, newColumns))
            {
                exprIn = new ExprOutput(columns: newColumns);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprOutputAction(ExprOutputAction exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newAlias = this.AcceptNullableItem(exprIn.Alias, modifier);
            if(!ReferenceEquals(exprIn.Alias, newAlias))
            {
                exprIn = new ExprOutputAction(alias: newAlias);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprOutputColumn(ExprOutputColumn exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newColumn = this.AcceptItem(exprIn.Column, modifier);
            if(!ReferenceEquals(exprIn.Column, newColumn))
            {
                exprIn = new ExprOutputColumn(column: newColumn);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprOutputColumnDeleted(ExprOutputColumnDeleted exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newColumnName = this.AcceptItem(exprIn.ColumnName, modifier);
            if(!ReferenceEquals(exprIn.ColumnName, newColumnName))
            {
                exprIn = new ExprOutputColumnDeleted(columnName: newColumnName);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprOutputColumnInserted(ExprOutputColumnInserted exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newColumnName = this.AcceptItem(exprIn.ColumnName, modifier);
            if(!ReferenceEquals(exprIn.ColumnName, newColumnName))
            {
                exprIn = new ExprOutputColumnInserted(columnName: newColumnName);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprOver(ExprOver exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newPartitions = this.AcceptNullCollection(exprIn.Partitions, modifier);
            var newOrderBy = this.AcceptNullableItem(exprIn.OrderBy, modifier);
            var newFrameClause = this.AcceptNullableItem(exprIn.FrameClause, modifier);
            if(!ReferenceEquals(exprIn.Partitions, newPartitions) || !ReferenceEquals(exprIn.OrderBy, newOrderBy) || !ReferenceEquals(exprIn.FrameClause, newFrameClause))
            {
                exprIn = new ExprOver(partitions: newPartitions, orderBy: newOrderBy, frameClause: newFrameClause);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprQueryExpression(ExprQueryExpression exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newLeft = this.AcceptItem(exprIn.Left, modifier);
            var newRight = this.AcceptItem(exprIn.Right, modifier);
            if(!ReferenceEquals(exprIn.Left, newLeft) || !ReferenceEquals(exprIn.Right, newRight))
            {
                exprIn = new ExprQueryExpression(left: newLeft, right: newRight, queryExpressionType: exprIn.QueryExpressionType);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprQuerySpecification(ExprQuerySpecification exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newSelectList = this.AcceptNotNullCollection(exprIn.SelectList, modifier);
            var newTop = this.AcceptNullableItem(exprIn.Top, modifier);
            var newFrom = this.AcceptNullableItem(exprIn.From, modifier);
            var newWhere = this.AcceptNullableItem(exprIn.Where, modifier);
            var newGroupBy = this.AcceptNullCollection(exprIn.GroupBy, modifier);
            if(!ReferenceEquals(exprIn.SelectList, newSelectList) || !ReferenceEquals(exprIn.Top, newTop) || !ReferenceEquals(exprIn.From, newFrom) || !ReferenceEquals(exprIn.Where, newWhere) || !ReferenceEquals(exprIn.GroupBy, newGroupBy))
            {
                exprIn = new ExprQuerySpecification(selectList: newSelectList, top: newTop, from: newFrom, where: newWhere, groupBy: newGroupBy, distinct: exprIn.Distinct);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprScalarFunction(ExprScalarFunction exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newSchema = this.AcceptNullableItem(exprIn.Schema, modifier);
            var newName = this.AcceptItem(exprIn.Name, modifier);
            var newArguments = this.AcceptNullCollection(exprIn.Arguments, modifier);
            if(!ReferenceEquals(exprIn.Schema, newSchema) || !ReferenceEquals(exprIn.Name, newName) || !ReferenceEquals(exprIn.Arguments, newArguments))
            {
                exprIn = new ExprScalarFunction(schema: newSchema, name: newName, arguments: newArguments);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprSchemaName(ExprSchemaName exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprSelect(ExprSelect exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newSelectQuery = this.AcceptItem(exprIn.SelectQuery, modifier);
            var newOrderBy = this.AcceptItem(exprIn.OrderBy, modifier);
            if(!ReferenceEquals(exprIn.SelectQuery, newSelectQuery) || !ReferenceEquals(exprIn.OrderBy, newOrderBy))
            {
                exprIn = new ExprSelect(selectQuery: newSelectQuery, orderBy: newOrderBy);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprSelectOffsetFetch(ExprSelectOffsetFetch exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newSelectQuery = this.AcceptItem(exprIn.SelectQuery, modifier);
            var newOrderBy = this.AcceptItem(exprIn.OrderBy, modifier);
            if(!ReferenceEquals(exprIn.SelectQuery, newSelectQuery) || !ReferenceEquals(exprIn.OrderBy, newOrderBy))
            {
                exprIn = new ExprSelectOffsetFetch(selectQuery: newSelectQuery, orderBy: newOrderBy);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprStringConcat(ExprStringConcat exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newLeft = this.AcceptItem(exprIn.Left, modifier);
            var newRight = this.AcceptItem(exprIn.Right, modifier);
            if(!ReferenceEquals(exprIn.Left, newLeft) || !ReferenceEquals(exprIn.Right, newRight))
            {
                exprIn = new ExprStringConcat(left: newLeft, right: newRight);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprStringLiteral(ExprStringLiteral exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprSub(ExprSub exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newLeft = this.AcceptItem(exprIn.Left, modifier);
            var newRight = this.AcceptItem(exprIn.Right, modifier);
            if(!ReferenceEquals(exprIn.Left, newLeft) || !ReferenceEquals(exprIn.Right, newRight))
            {
                exprIn = new ExprSub(left: newLeft, right: newRight);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprSum(ExprSum exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newLeft = this.AcceptItem(exprIn.Left, modifier);
            var newRight = this.AcceptItem(exprIn.Right, modifier);
            if(!ReferenceEquals(exprIn.Left, newLeft) || !ReferenceEquals(exprIn.Right, newRight))
            {
                exprIn = new ExprSum(left: newLeft, right: newRight);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprTable(ExprTable exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newFullName = this.AcceptItem(exprIn.FullName, modifier);
            var newAlias = this.AcceptNullableItem(exprIn.Alias, modifier);
            if(!ReferenceEquals(exprIn.FullName, newFullName) || !ReferenceEquals(exprIn.Alias, newAlias))
            {
                exprIn = new ExprTable(fullName: newFullName, alias: newAlias);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprTableAlias(ExprTableAlias exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newAlias = this.AcceptItem(exprIn.Alias, modifier);
            if(!ReferenceEquals(exprIn.Alias, newAlias))
            {
                exprIn = new ExprTableAlias(alias: newAlias);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprTableFullName(ExprTableFullName exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newDbSchema = this.AcceptNullableItem(exprIn.DbSchema, modifier);
            var newTableName = this.AcceptItem(exprIn.TableName, modifier);
            if(!ReferenceEquals(exprIn.DbSchema, newDbSchema) || !ReferenceEquals(exprIn.TableName, newTableName))
            {
                exprIn = new ExprTableFullName(dbSchema: newDbSchema, tableName: newTableName);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprTableName(ExprTableName exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprTableValueConstructor(ExprTableValueConstructor exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newItems = this.AcceptNotNullCollection(exprIn.Items, modifier);
            if(!ReferenceEquals(exprIn.Items, newItems))
            {
                exprIn = new ExprTableValueConstructor(items: newItems);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprTempTableName(ExprTempTableName exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprTypeBoolean(ExprTypeBoolean exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprTypeByte(ExprTypeByte exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprTypeDateTime(ExprTypeDateTime exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprTypeDecimal(ExprTypeDecimal exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprTypeDouble(ExprTypeDouble exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprTypeGuid(ExprTypeGuid exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprTypeInt16(ExprTypeInt16 exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprTypeInt32(ExprTypeInt32 exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprTypeInt64(ExprTypeInt64 exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprTypeString(ExprTypeString exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprUnboundedFrameBorder(ExprUnboundedFrameBorder exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprUnsafeValue(ExprUnsafeValue exprIn, Func<IExpr, IExpr?> modifier)
        {
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprUpdate(ExprUpdate exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newTarget = this.AcceptItem(exprIn.Target, modifier);
            var newSetClause = this.AcceptNotNullCollection(exprIn.SetClause, modifier);
            var newSource = this.AcceptNullableItem(exprIn.Source, modifier);
            var newFilter = this.AcceptNullableItem(exprIn.Filter, modifier);
            if(!ReferenceEquals(exprIn.Target, newTarget) || !ReferenceEquals(exprIn.SetClause, newSetClause) || !ReferenceEquals(exprIn.Source, newSource) || !ReferenceEquals(exprIn.Filter, newFilter))
            {
                exprIn = new ExprUpdate(target: newTarget, setClause: newSetClause, source: newSource, filter: newFilter);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprValueFrameBorder(ExprValueFrameBorder exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newValue = this.AcceptItem(exprIn.Value, modifier);
            if(!ReferenceEquals(exprIn.Value, newValue))
            {
                exprIn = new ExprValueFrameBorder(value: newValue, frameBorderDirection: exprIn.FrameBorderDirection);
            }
            return modifier.Invoke(exprIn);
        }
        public IExpr? VisitExprValueRow(ExprValueRow exprIn, Func<IExpr, IExpr?> modifier)
        {
            var newItems = this.AcceptNotNullCollection(exprIn.Items, modifier);
            if(!ReferenceEquals(exprIn.Items, newItems))
            {
                exprIn = new ExprValueRow(items: newItems);
            }
            return modifier.Invoke(exprIn);
        }
        //CodeGenEnd
    }
}
