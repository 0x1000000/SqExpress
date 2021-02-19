using System;
using System.Collections.Generic;
using SqExpress.QueryBuilders.Select;
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
using SqExpress.Utils;

namespace SqExpress
{
    public static class SyntaxModifyExtensions
    {
        private static ExprQuerySpecification JoinQuerySpecification(ExprJoinedTable.ExprJoinType exprJoinType,
            ExprQuerySpecification querySpecification, IExprTableSource tableSource, ExprBoolean @on)
        {
            if (querySpecification.From == null)
            {
                throw new SqExpressException("Query Specification \"From\" cannot be null");
            }

            var newJoin = new ExprJoinedTable(querySpecification.From, exprJoinType, tableSource, on);

            return querySpecification.WithFrom(newJoin);
        }

        public static ExprQuerySpecification WithInnerJoin(this ExprQuerySpecification querySpecification, IExprTableSource tableSource, ExprBoolean on) 
            => JoinQuerySpecification(ExprJoinedTable.ExprJoinType.Inner, querySpecification, tableSource, on);

        public static ExprSelect WithInnerJoin(this ExprSelect select, IExprTableSource tableSource, ExprBoolean on) 
            => select.SelectQuery is ExprQuerySpecification specification
                ? select.WithSelectQuery(specification.WithInnerJoin(tableSource, on))
                : throw new SqExpressException("Join can be done only with a query specification");

        public static ExprSelectOffsetFetch WithInnerJoin(this ExprSelectOffsetFetch select, IExprTableSource tableSource, ExprBoolean on) 
            => select.SelectQuery is ExprQuerySpecification specification
                ? select.WithSelectQuery(specification.WithInnerJoin(tableSource, on))
                : throw new SqExpressException("Join can be done only with a query specification");

        public static ExprQuerySpecification WithLeftJoin(this ExprQuerySpecification querySpecification, IExprTableSource tableSource, ExprBoolean on) 
            => JoinQuerySpecification(ExprJoinedTable.ExprJoinType.Left, querySpecification, tableSource, on);

        public static ExprSelect WithLeftJoin(this ExprSelect select, IExprTableSource tableSource, ExprBoolean on) 
            => select.SelectQuery is ExprQuerySpecification specification
                ? select.WithSelectQuery(specification.WithLeftJoin(tableSource, on))
                : throw new SqExpressException("Join can be done only with a query specification");

        public static ExprSelectOffsetFetch WithLeftJoin(this ExprSelectOffsetFetch select, IExprTableSource tableSource, ExprBoolean on) 
            => select.SelectQuery is ExprQuerySpecification specification
                ? select.WithSelectQuery(specification.WithLeftJoin(tableSource, on))
                : throw new SqExpressException("Join can be done only with a query specification");

        public static ExprQuerySpecification WithFullJoin(this ExprQuerySpecification querySpecification, IExprTableSource tableSource, ExprBoolean on)
            => JoinQuerySpecification(ExprJoinedTable.ExprJoinType.Full, querySpecification, tableSource, on);

        public static ExprSelect WithFullJoin(this ExprSelect select, IExprTableSource tableSource, ExprBoolean on)
            => select.SelectQuery is ExprQuerySpecification specification
                ? select.WithSelectQuery(specification.WithFullJoin(tableSource, on))
                : throw new SqExpressException("Join can be done only with a query specification");

        public static ExprSelectOffsetFetch WithFullJoin(this ExprSelectOffsetFetch select, IExprTableSource tableSource, ExprBoolean on)
            => select.SelectQuery is ExprQuerySpecification specification
                ? select.WithSelectQuery(specification.WithFullJoin(tableSource, on))
                : throw new SqExpressException("Join can be done only with a query specification");

        public static ExprQuerySpecification WithCrossJoin(this ExprQuerySpecification querySpecification, IExprTableSource tableSource)
        {
            if (querySpecification.From == null)
            {
                throw new SqExpressException("Query Specification \"From\" cannot be null");
            }

            var newJoin = new ExprCrossedTable(querySpecification.From, tableSource);

            return querySpecification.WithFrom(newJoin);
        }

        public static ExprSelect WithCrossJoin(this ExprSelect select, IExprTableSource tableSource)
            => select.SelectQuery is ExprQuerySpecification specification
                ? select.WithSelectQuery(specification.WithCrossJoin(tableSource))
                : throw new SqExpressException("Join can be done only with a query specification");

        public static ExprSelectOffsetFetch WithCrossJoin(this ExprSelectOffsetFetch select, IExprTableSource tableSource)
            => select.SelectQuery is ExprQuerySpecification specification
                ? select.WithSelectQuery(specification.WithCrossJoin(tableSource))
                : throw new SqExpressException("Join can be done only with a query specification");

        public static ExprQuerySpecification WithSelectList(this ExprQuerySpecification original, SelectingProxy selection, params SelectingProxy[] selections)
            => new ExprQuerySpecification(selectList: Helpers.Combine(selection, selections, SelectingProxy.MapSelectionProxy), top: original.Top, from: original.From, where: original.Where, groupBy: original.GroupBy, distinct: original.Distinct);

        //CodeGenStart
        public static ExprAggregateFunction WithName(this ExprAggregateFunction original, ExprFunctionName newName) 
            => new ExprAggregateFunction(name: newName, expression: original.Expression, isDistinct: original.IsDistinct);

        public static ExprAggregateFunction WithExpression(this ExprAggregateFunction original, ExprValue newExpression) 
            => new ExprAggregateFunction(name: original.Name, expression: newExpression, isDistinct: original.IsDistinct);

        public static ExprAggregateFunction WithIsDistinct(this ExprAggregateFunction original, Boolean newIsDistinct) 
            => new ExprAggregateFunction(name: original.Name, expression: original.Expression, isDistinct: newIsDistinct);

        public static ExprAlias WithName(this ExprAlias original, String newName) 
            => new ExprAlias(name: newName);

        public static ExprAliasGuid WithId(this ExprAliasGuid original, Guid newId) 
            => new ExprAliasGuid(id: newId);

        public static ExprAliasedColumn WithColumn(this ExprAliasedColumn original, ExprColumn newColumn) 
            => new ExprAliasedColumn(column: newColumn, alias: original.Alias);

        public static ExprAliasedColumn WithAlias(this ExprAliasedColumn original, ExprColumnAlias? newAlias) 
            => new ExprAliasedColumn(column: original.Column, alias: newAlias);

        public static ExprAliasedColumnName WithColumn(this ExprAliasedColumnName original, ExprColumnName newColumn) 
            => new ExprAliasedColumnName(column: newColumn, alias: original.Alias);

        public static ExprAliasedColumnName WithAlias(this ExprAliasedColumnName original, ExprColumnAlias? newAlias) 
            => new ExprAliasedColumnName(column: original.Column, alias: newAlias);

        public static ExprAliasedSelecting WithValue(this ExprAliasedSelecting original, IExprSelecting newValue) 
            => new ExprAliasedSelecting(value: newValue, alias: original.Alias);

        public static ExprAliasedSelecting WithAlias(this ExprAliasedSelecting original, ExprColumnAlias newAlias) 
            => new ExprAliasedSelecting(value: original.Value, alias: newAlias);

        public static ExprAllColumns WithSource(this ExprAllColumns original, IExprColumnSource? newSource) 
            => new ExprAllColumns(source: newSource);

        public static ExprAnalyticFunction WithName(this ExprAnalyticFunction original, ExprFunctionName newName) 
            => new ExprAnalyticFunction(name: newName, arguments: original.Arguments, over: original.Over);

        public static ExprAnalyticFunction WithArguments(this ExprAnalyticFunction original, IReadOnlyList<ExprValue>? newArguments) 
            => new ExprAnalyticFunction(name: original.Name, arguments: newArguments, over: original.Over);

        public static ExprAnalyticFunction WithOver(this ExprAnalyticFunction original, ExprOver newOver) 
            => new ExprAnalyticFunction(name: original.Name, arguments: original.Arguments, over: newOver);

        public static ExprBoolLiteral WithValue(this ExprBoolLiteral original, Boolean? newValue) 
            => new ExprBoolLiteral(value: newValue);

        public static ExprBooleanAnd WithLeft(this ExprBooleanAnd original, ExprBoolean newLeft) 
            => new ExprBooleanAnd(left: newLeft, right: original.Right);

        public static ExprBooleanAnd WithRight(this ExprBooleanAnd original, ExprBoolean newRight) 
            => new ExprBooleanAnd(left: original.Left, right: newRight);

        public static ExprBooleanEq WithLeft(this ExprBooleanEq original, ExprValue newLeft) 
            => new ExprBooleanEq(left: newLeft, right: original.Right);

        public static ExprBooleanEq WithRight(this ExprBooleanEq original, ExprValue newRight) 
            => new ExprBooleanEq(left: original.Left, right: newRight);

        public static ExprBooleanGt WithLeft(this ExprBooleanGt original, ExprValue newLeft) 
            => new ExprBooleanGt(left: newLeft, right: original.Right);

        public static ExprBooleanGt WithRight(this ExprBooleanGt original, ExprValue newRight) 
            => new ExprBooleanGt(left: original.Left, right: newRight);

        public static ExprBooleanGtEq WithLeft(this ExprBooleanGtEq original, ExprValue newLeft) 
            => new ExprBooleanGtEq(left: newLeft, right: original.Right);

        public static ExprBooleanGtEq WithRight(this ExprBooleanGtEq original, ExprValue newRight) 
            => new ExprBooleanGtEq(left: original.Left, right: newRight);

        public static ExprBooleanLt WithLeft(this ExprBooleanLt original, ExprValue newLeft) 
            => new ExprBooleanLt(left: newLeft, right: original.Right);

        public static ExprBooleanLt WithRight(this ExprBooleanLt original, ExprValue newRight) 
            => new ExprBooleanLt(left: original.Left, right: newRight);

        public static ExprBooleanLtEq WithLeft(this ExprBooleanLtEq original, ExprValue newLeft) 
            => new ExprBooleanLtEq(left: newLeft, right: original.Right);

        public static ExprBooleanLtEq WithRight(this ExprBooleanLtEq original, ExprValue newRight) 
            => new ExprBooleanLtEq(left: original.Left, right: newRight);

        public static ExprBooleanNot WithExpr(this ExprBooleanNot original, ExprBoolean newExpr) 
            => new ExprBooleanNot(expr: newExpr);

        public static ExprBooleanNotEq WithLeft(this ExprBooleanNotEq original, ExprValue newLeft) 
            => new ExprBooleanNotEq(left: newLeft, right: original.Right);

        public static ExprBooleanNotEq WithRight(this ExprBooleanNotEq original, ExprValue newRight) 
            => new ExprBooleanNotEq(left: original.Left, right: newRight);

        public static ExprBooleanOr WithLeft(this ExprBooleanOr original, ExprBoolean newLeft) 
            => new ExprBooleanOr(left: newLeft, right: original.Right);

        public static ExprBooleanOr WithRight(this ExprBooleanOr original, ExprBoolean newRight) 
            => new ExprBooleanOr(left: original.Left, right: newRight);

        public static ExprByteArrayLiteral WithValue(this ExprByteArrayLiteral original, IReadOnlyList<Byte>? newValue) 
            => new ExprByteArrayLiteral(value: newValue);

        public static ExprByteLiteral WithValue(this ExprByteLiteral original, Byte? newValue) 
            => new ExprByteLiteral(value: newValue);

        public static ExprCase WithCases(this ExprCase original, IReadOnlyList<ExprCaseWhenThen> newCases) 
            => new ExprCase(cases: newCases, defaultValue: original.DefaultValue);

        public static ExprCase WithDefaultValue(this ExprCase original, ExprValue newDefaultValue) 
            => new ExprCase(cases: original.Cases, defaultValue: newDefaultValue);

        public static ExprCaseWhenThen WithCondition(this ExprCaseWhenThen original, ExprBoolean newCondition) 
            => new ExprCaseWhenThen(condition: newCondition, value: original.Value);

        public static ExprCaseWhenThen WithValue(this ExprCaseWhenThen original, ExprValue newValue) 
            => new ExprCaseWhenThen(condition: original.Condition, value: newValue);

        public static ExprCast WithExpression(this ExprCast original, IExprSelecting newExpression) 
            => new ExprCast(expression: newExpression, sqlType: original.SqlType);

        public static ExprCast WithSqlType(this ExprCast original, ExprType newSqlType) 
            => new ExprCast(expression: original.Expression, sqlType: newSqlType);

        public static ExprColumn WithSource(this ExprColumn original, IExprColumnSource? newSource) 
            => new ExprColumn(source: newSource, columnName: original.ColumnName);

        public static ExprColumn WithColumnName(this ExprColumn original, ExprColumnName newColumnName) 
            => new ExprColumn(source: original.Source, columnName: newColumnName);

        public static ExprColumnAlias WithName(this ExprColumnAlias original, String newName) 
            => new ExprColumnAlias(name: newName);

        public static ExprColumnName WithName(this ExprColumnName original, String newName) 
            => new ExprColumnName(name: newName);

        public static ExprColumnSetClause WithColumn(this ExprColumnSetClause original, ExprColumn newColumn) 
            => new ExprColumnSetClause(column: newColumn, value: original.Value);

        public static ExprColumnSetClause WithValue(this ExprColumnSetClause original, IExprAssigning newValue) 
            => new ExprColumnSetClause(column: original.Column, value: newValue);

        public static ExprCrossedTable WithLeft(this ExprCrossedTable original, IExprTableSource newLeft) 
            => new ExprCrossedTable(left: newLeft, right: original.Right);

        public static ExprCrossedTable WithRight(this ExprCrossedTable original, IExprTableSource newRight) 
            => new ExprCrossedTable(left: original.Left, right: newRight);

        public static ExprDatabaseName WithName(this ExprDatabaseName original, String newName) 
            => new ExprDatabaseName(name: newName);

        public static ExprDateAdd WithDate(this ExprDateAdd original, ExprValue newDate) 
            => new ExprDateAdd(date: newDate, datePart: original.DatePart, number: original.Number);

        public static ExprDateAdd WithDatePart(this ExprDateAdd original, DateAddDatePart newDatePart) 
            => new ExprDateAdd(date: original.Date, datePart: newDatePart, number: original.Number);

        public static ExprDateAdd WithNumber(this ExprDateAdd original, Int32 newNumber) 
            => new ExprDateAdd(date: original.Date, datePart: original.DatePart, number: newNumber);

        public static ExprDateTimeLiteral WithValue(this ExprDateTimeLiteral original, DateTime? newValue) 
            => new ExprDateTimeLiteral(value: newValue);

        public static ExprDbSchema WithDatabase(this ExprDbSchema original, ExprDatabaseName? newDatabase) 
            => new ExprDbSchema(database: newDatabase, schema: original.Schema);

        public static ExprDbSchema WithSchema(this ExprDbSchema original, ExprSchemaName newSchema) 
            => new ExprDbSchema(database: original.Database, schema: newSchema);

        public static ExprDecimalLiteral WithValue(this ExprDecimalLiteral original, Decimal? newValue) 
            => new ExprDecimalLiteral(value: newValue);

        public static ExprDelete WithTarget(this ExprDelete original, ExprTable newTarget) 
            => new ExprDelete(target: newTarget, source: original.Source, filter: original.Filter);

        public static ExprDelete WithSource(this ExprDelete original, IExprTableSource? newSource) 
            => new ExprDelete(target: original.Target, source: newSource, filter: original.Filter);

        public static ExprDelete WithFilter(this ExprDelete original, ExprBoolean? newFilter) 
            => new ExprDelete(target: original.Target, source: original.Source, filter: newFilter);

        public static ExprDeleteOutput WithDelete(this ExprDeleteOutput original, ExprDelete newDelete) 
            => new ExprDeleteOutput(delete: newDelete, outputColumns: original.OutputColumns);

        public static ExprDeleteOutput WithOutputColumns(this ExprDeleteOutput original, IReadOnlyList<ExprAliasedColumn> newOutputColumns) 
            => new ExprDeleteOutput(delete: original.Delete, outputColumns: newOutputColumns);

        public static ExprDerivedTableQuery WithQuery(this ExprDerivedTableQuery original, IExprSubQuery newQuery) 
            => new ExprDerivedTableQuery(query: newQuery, alias: original.Alias, columns: original.Columns);

        public static ExprDerivedTableQuery WithAlias(this ExprDerivedTableQuery original, ExprTableAlias newAlias) 
            => new ExprDerivedTableQuery(query: original.Query, alias: newAlias, columns: original.Columns);

        public static ExprDerivedTableQuery WithColumns(this ExprDerivedTableQuery original, IReadOnlyList<ExprColumnName>? newColumns) 
            => new ExprDerivedTableQuery(query: original.Query, alias: original.Alias, columns: newColumns);

        public static ExprDerivedTableValues WithValues(this ExprDerivedTableValues original, ExprTableValueConstructor newValues) 
            => new ExprDerivedTableValues(values: newValues, alias: original.Alias, columns: original.Columns);

        public static ExprDerivedTableValues WithAlias(this ExprDerivedTableValues original, ExprTableAlias newAlias) 
            => new ExprDerivedTableValues(values: original.Values, alias: newAlias, columns: original.Columns);

        public static ExprDerivedTableValues WithColumns(this ExprDerivedTableValues original, IReadOnlyList<ExprColumnName> newColumns) 
            => new ExprDerivedTableValues(values: original.Values, alias: original.Alias, columns: newColumns);

        public static ExprDiv WithLeft(this ExprDiv original, ExprValue newLeft) 
            => new ExprDiv(left: newLeft, right: original.Right);

        public static ExprDiv WithRight(this ExprDiv original, ExprValue newRight) 
            => new ExprDiv(left: original.Left, right: newRight);

        public static ExprDoubleLiteral WithValue(this ExprDoubleLiteral original, Double? newValue) 
            => new ExprDoubleLiteral(value: newValue);

        public static ExprExists WithSubQuery(this ExprExists original, IExprSubQuery newSubQuery) 
            => new ExprExists(subQuery: newSubQuery);

        public static ExprExprMergeNotMatchedInsert WithAnd(this ExprExprMergeNotMatchedInsert original, ExprBoolean? newAnd) 
            => new ExprExprMergeNotMatchedInsert(and: newAnd, columns: original.Columns, values: original.Values);

        public static ExprExprMergeNotMatchedInsert WithColumns(this ExprExprMergeNotMatchedInsert original, IReadOnlyList<ExprColumnName> newColumns) 
            => new ExprExprMergeNotMatchedInsert(and: original.And, columns: newColumns, values: original.Values);

        public static ExprExprMergeNotMatchedInsert WithValues(this ExprExprMergeNotMatchedInsert original, IReadOnlyList<IExprAssigning> newValues) 
            => new ExprExprMergeNotMatchedInsert(and: original.And, columns: original.Columns, values: newValues);

        public static ExprExprMergeNotMatchedInsertDefault WithAnd(this ExprExprMergeNotMatchedInsertDefault original, ExprBoolean? newAnd) 
            => new ExprExprMergeNotMatchedInsertDefault(and: newAnd);

        public static ExprFrameClause WithStart(this ExprFrameClause original, ExprFrameBorder newStart) 
            => new ExprFrameClause(start: newStart, end: original.End);

        public static ExprFrameClause WithEnd(this ExprFrameClause original, ExprFrameBorder? newEnd) 
            => new ExprFrameClause(start: original.Start, end: newEnd);

        public static ExprFuncCoalesce WithTest(this ExprFuncCoalesce original, ExprValue newTest) 
            => new ExprFuncCoalesce(test: newTest, alts: original.Alts);

        public static ExprFuncCoalesce WithAlts(this ExprFuncCoalesce original, IReadOnlyList<ExprValue> newAlts) 
            => new ExprFuncCoalesce(test: original.Test, alts: newAlts);

        public static ExprFuncIsNull WithTest(this ExprFuncIsNull original, ExprValue newTest) 
            => new ExprFuncIsNull(test: newTest, alt: original.Alt);

        public static ExprFuncIsNull WithAlt(this ExprFuncIsNull original, ExprValue newAlt) 
            => new ExprFuncIsNull(test: original.Test, alt: newAlt);

        public static ExprFunctionName WithBuiltIn(this ExprFunctionName original, Boolean newBuiltIn) 
            => new ExprFunctionName(builtIn: newBuiltIn, name: original.Name);

        public static ExprFunctionName WithName(this ExprFunctionName original, String newName) 
            => new ExprFunctionName(builtIn: original.BuiltIn, name: newName);

        public static ExprGuidLiteral WithValue(this ExprGuidLiteral original, Guid? newValue) 
            => new ExprGuidLiteral(value: newValue);

        public static ExprInSubQuery WithTestExpression(this ExprInSubQuery original, ExprValue newTestExpression) 
            => new ExprInSubQuery(testExpression: newTestExpression, subQuery: original.SubQuery);

        public static ExprInSubQuery WithSubQuery(this ExprInSubQuery original, IExprSubQuery newSubQuery) 
            => new ExprInSubQuery(testExpression: original.TestExpression, subQuery: newSubQuery);

        public static ExprInValues WithTestExpression(this ExprInValues original, ExprValue newTestExpression) 
            => new ExprInValues(testExpression: newTestExpression, items: original.Items);

        public static ExprInValues WithItems(this ExprInValues original, IReadOnlyList<ExprValue> newItems) 
            => new ExprInValues(testExpression: original.TestExpression, items: newItems);

        public static ExprInsert WithTarget(this ExprInsert original, IExprTableFullName newTarget) 
            => new ExprInsert(target: newTarget, targetColumns: original.TargetColumns, source: original.Source);

        public static ExprInsert WithTargetColumns(this ExprInsert original, IReadOnlyList<ExprColumnName>? newTargetColumns) 
            => new ExprInsert(target: original.Target, targetColumns: newTargetColumns, source: original.Source);

        public static ExprInsert WithSource(this ExprInsert original, IExprInsertSource newSource) 
            => new ExprInsert(target: original.Target, targetColumns: original.TargetColumns, source: newSource);

        public static ExprInsertOutput WithInsert(this ExprInsertOutput original, ExprInsert newInsert) 
            => new ExprInsertOutput(insert: newInsert, outputColumns: original.OutputColumns);

        public static ExprInsertOutput WithOutputColumns(this ExprInsertOutput original, IReadOnlyList<ExprAliasedColumnName> newOutputColumns) 
            => new ExprInsertOutput(insert: original.Insert, outputColumns: newOutputColumns);

        public static ExprInsertQuery WithQuery(this ExprInsertQuery original, IExprQuery newQuery) 
            => new ExprInsertQuery(query: newQuery);

        public static ExprInsertValueRow WithItems(this ExprInsertValueRow original, IReadOnlyList<IExprAssigning> newItems) 
            => new ExprInsertValueRow(items: newItems);

        public static ExprInsertValues WithItems(this ExprInsertValues original, IReadOnlyList<ExprInsertValueRow> newItems) 
            => new ExprInsertValues(items: newItems);

        public static ExprInt16Literal WithValue(this ExprInt16Literal original, Int16? newValue) 
            => new ExprInt16Literal(value: newValue);

        public static ExprInt32Literal WithValue(this ExprInt32Literal original, Int32? newValue) 
            => new ExprInt32Literal(value: newValue);

        public static ExprInt64Literal WithValue(this ExprInt64Literal original, Int64? newValue) 
            => new ExprInt64Literal(value: newValue);

        public static ExprIsNull WithTest(this ExprIsNull original, ExprValue newTest) 
            => new ExprIsNull(test: newTest, not: original.Not);

        public static ExprIsNull WithNot(this ExprIsNull original, Boolean newNot) 
            => new ExprIsNull(test: original.Test, not: newNot);

        public static ExprJoinedTable WithLeft(this ExprJoinedTable original, IExprTableSource newLeft) 
            => new ExprJoinedTable(left: newLeft, right: original.Right, searchCondition: original.SearchCondition, joinType: original.JoinType);

        public static ExprJoinedTable WithRight(this ExprJoinedTable original, IExprTableSource newRight) 
            => new ExprJoinedTable(left: original.Left, right: newRight, searchCondition: original.SearchCondition, joinType: original.JoinType);

        public static ExprJoinedTable WithSearchCondition(this ExprJoinedTable original, ExprBoolean newSearchCondition) 
            => new ExprJoinedTable(left: original.Left, right: original.Right, searchCondition: newSearchCondition, joinType: original.JoinType);

        public static ExprJoinedTable WithJoinType(this ExprJoinedTable original, ExprJoinedTable.ExprJoinType newJoinType) 
            => new ExprJoinedTable(left: original.Left, right: original.Right, searchCondition: original.SearchCondition, joinType: newJoinType);

        public static ExprLike WithTest(this ExprLike original, ExprValue newTest) 
            => new ExprLike(test: newTest, pattern: original.Pattern);

        public static ExprLike WithPattern(this ExprLike original, ExprStringLiteral newPattern) 
            => new ExprLike(test: original.Test, pattern: newPattern);

        public static ExprMerge WithTargetTable(this ExprMerge original, ExprTable newTargetTable) 
            => new ExprMerge(targetTable: newTargetTable, source: original.Source, on: original.On, whenMatched: original.WhenMatched, whenNotMatchedByTarget: original.WhenNotMatchedByTarget, whenNotMatchedBySource: original.WhenNotMatchedBySource);

        public static ExprMerge WithSource(this ExprMerge original, IExprTableSource newSource) 
            => new ExprMerge(targetTable: original.TargetTable, source: newSource, on: original.On, whenMatched: original.WhenMatched, whenNotMatchedByTarget: original.WhenNotMatchedByTarget, whenNotMatchedBySource: original.WhenNotMatchedBySource);

        public static ExprMerge WithOn(this ExprMerge original, ExprBoolean newOn) 
            => new ExprMerge(targetTable: original.TargetTable, source: original.Source, on: newOn, whenMatched: original.WhenMatched, whenNotMatchedByTarget: original.WhenNotMatchedByTarget, whenNotMatchedBySource: original.WhenNotMatchedBySource);

        public static ExprMerge WithWhenMatched(this ExprMerge original, IExprMergeMatched? newWhenMatched) 
            => new ExprMerge(targetTable: original.TargetTable, source: original.Source, on: original.On, whenMatched: newWhenMatched, whenNotMatchedByTarget: original.WhenNotMatchedByTarget, whenNotMatchedBySource: original.WhenNotMatchedBySource);

        public static ExprMerge WithWhenNotMatchedByTarget(this ExprMerge original, IExprMergeNotMatched? newWhenNotMatchedByTarget) 
            => new ExprMerge(targetTable: original.TargetTable, source: original.Source, on: original.On, whenMatched: original.WhenMatched, whenNotMatchedByTarget: newWhenNotMatchedByTarget, whenNotMatchedBySource: original.WhenNotMatchedBySource);

        public static ExprMerge WithWhenNotMatchedBySource(this ExprMerge original, IExprMergeMatched? newWhenNotMatchedBySource) 
            => new ExprMerge(targetTable: original.TargetTable, source: original.Source, on: original.On, whenMatched: original.WhenMatched, whenNotMatchedByTarget: original.WhenNotMatchedByTarget, whenNotMatchedBySource: newWhenNotMatchedBySource);

        public static ExprMergeMatchedDelete WithAnd(this ExprMergeMatchedDelete original, ExprBoolean? newAnd) 
            => new ExprMergeMatchedDelete(and: newAnd);

        public static ExprMergeMatchedUpdate WithAnd(this ExprMergeMatchedUpdate original, ExprBoolean? newAnd) 
            => new ExprMergeMatchedUpdate(and: newAnd, set: original.Set);

        public static ExprMergeMatchedUpdate WithSet(this ExprMergeMatchedUpdate original, IReadOnlyList<ExprColumnSetClause> newSet) 
            => new ExprMergeMatchedUpdate(and: original.And, set: newSet);

        public static ExprMergeOutput WithTargetTable(this ExprMergeOutput original, ExprTable newTargetTable) 
            => new ExprMergeOutput(targetTable: newTargetTable, source: original.Source, on: original.On, whenMatched: original.WhenMatched, whenNotMatchedByTarget: original.WhenNotMatchedByTarget, whenNotMatchedBySource: original.WhenNotMatchedBySource, output: original.Output);

        public static ExprMergeOutput WithSource(this ExprMergeOutput original, IExprTableSource newSource) 
            => new ExprMergeOutput(targetTable: original.TargetTable, source: newSource, on: original.On, whenMatched: original.WhenMatched, whenNotMatchedByTarget: original.WhenNotMatchedByTarget, whenNotMatchedBySource: original.WhenNotMatchedBySource, output: original.Output);

        public static ExprMergeOutput WithOn(this ExprMergeOutput original, ExprBoolean newOn) 
            => new ExprMergeOutput(targetTable: original.TargetTable, source: original.Source, on: newOn, whenMatched: original.WhenMatched, whenNotMatchedByTarget: original.WhenNotMatchedByTarget, whenNotMatchedBySource: original.WhenNotMatchedBySource, output: original.Output);

        public static ExprMergeOutput WithWhenMatched(this ExprMergeOutput original, IExprMergeMatched? newWhenMatched) 
            => new ExprMergeOutput(targetTable: original.TargetTable, source: original.Source, on: original.On, whenMatched: newWhenMatched, whenNotMatchedByTarget: original.WhenNotMatchedByTarget, whenNotMatchedBySource: original.WhenNotMatchedBySource, output: original.Output);

        public static ExprMergeOutput WithWhenNotMatchedByTarget(this ExprMergeOutput original, IExprMergeNotMatched? newWhenNotMatchedByTarget) 
            => new ExprMergeOutput(targetTable: original.TargetTable, source: original.Source, on: original.On, whenMatched: original.WhenMatched, whenNotMatchedByTarget: newWhenNotMatchedByTarget, whenNotMatchedBySource: original.WhenNotMatchedBySource, output: original.Output);

        public static ExprMergeOutput WithWhenNotMatchedBySource(this ExprMergeOutput original, IExprMergeMatched? newWhenNotMatchedBySource) 
            => new ExprMergeOutput(targetTable: original.TargetTable, source: original.Source, on: original.On, whenMatched: original.WhenMatched, whenNotMatchedByTarget: original.WhenNotMatchedByTarget, whenNotMatchedBySource: newWhenNotMatchedBySource, output: original.Output);

        public static ExprMergeOutput WithOutput(this ExprMergeOutput original, ExprOutput newOutput) 
            => new ExprMergeOutput(targetTable: original.TargetTable, source: original.Source, on: original.On, whenMatched: original.WhenMatched, whenNotMatchedByTarget: original.WhenNotMatchedByTarget, whenNotMatchedBySource: original.WhenNotMatchedBySource, output: newOutput);

        public static ExprModulo WithLeft(this ExprModulo original, ExprValue newLeft) 
            => new ExprModulo(left: newLeft, right: original.Right);

        public static ExprModulo WithRight(this ExprModulo original, ExprValue newRight) 
            => new ExprModulo(left: original.Left, right: newRight);

        public static ExprMul WithLeft(this ExprMul original, ExprValue newLeft) 
            => new ExprMul(left: newLeft, right: original.Right);

        public static ExprMul WithRight(this ExprMul original, ExprValue newRight) 
            => new ExprMul(left: original.Left, right: newRight);

        public static ExprOffsetFetch WithOffset(this ExprOffsetFetch original, ExprInt32Literal newOffset) 
            => new ExprOffsetFetch(offset: newOffset, fetch: original.Fetch);

        public static ExprOffsetFetch WithFetch(this ExprOffsetFetch original, ExprInt32Literal? newFetch) 
            => new ExprOffsetFetch(offset: original.Offset, fetch: newFetch);

        public static ExprOrderBy WithOrderList(this ExprOrderBy original, IReadOnlyList<ExprOrderByItem> newOrderList) 
            => new ExprOrderBy(orderList: newOrderList);

        public static ExprOrderByItem WithValue(this ExprOrderByItem original, ExprValue newValue) 
            => new ExprOrderByItem(value: newValue, descendant: original.Descendant);

        public static ExprOrderByItem WithDescendant(this ExprOrderByItem original, Boolean newDescendant) 
            => new ExprOrderByItem(value: original.Value, descendant: newDescendant);

        public static ExprOrderByOffsetFetch WithOrderList(this ExprOrderByOffsetFetch original, IReadOnlyList<ExprOrderByItem> newOrderList) 
            => new ExprOrderByOffsetFetch(orderList: newOrderList, offsetFetch: original.OffsetFetch);

        public static ExprOrderByOffsetFetch WithOffsetFetch(this ExprOrderByOffsetFetch original, ExprOffsetFetch newOffsetFetch) 
            => new ExprOrderByOffsetFetch(orderList: original.OrderList, offsetFetch: newOffsetFetch);

        public static ExprOutput WithColumns(this ExprOutput original, IReadOnlyList<IExprOutputColumn> newColumns) 
            => new ExprOutput(columns: newColumns);

        public static ExprOutputAction WithAlias(this ExprOutputAction original, ExprColumnAlias? newAlias) 
            => new ExprOutputAction(alias: newAlias);

        public static ExprOutputColumn WithColumn(this ExprOutputColumn original, ExprAliasedColumn newColumn) 
            => new ExprOutputColumn(column: newColumn);

        public static ExprOutputColumnDeleted WithColumnName(this ExprOutputColumnDeleted original, ExprAliasedColumnName newColumnName) 
            => new ExprOutputColumnDeleted(columnName: newColumnName);

        public static ExprOutputColumnInserted WithColumnName(this ExprOutputColumnInserted original, ExprAliasedColumnName newColumnName) 
            => new ExprOutputColumnInserted(columnName: newColumnName);

        public static ExprOver WithPartitions(this ExprOver original, IReadOnlyList<ExprValue>? newPartitions) 
            => new ExprOver(partitions: newPartitions, orderBy: original.OrderBy, frameClause: original.FrameClause);

        public static ExprOver WithOrderBy(this ExprOver original, ExprOrderBy? newOrderBy) 
            => new ExprOver(partitions: original.Partitions, orderBy: newOrderBy, frameClause: original.FrameClause);

        public static ExprOver WithFrameClause(this ExprOver original, ExprFrameClause? newFrameClause) 
            => new ExprOver(partitions: original.Partitions, orderBy: original.OrderBy, frameClause: newFrameClause);

        public static ExprQueryExpression WithLeft(this ExprQueryExpression original, IExprSubQuery newLeft) 
            => new ExprQueryExpression(left: newLeft, right: original.Right, queryExpressionType: original.QueryExpressionType);

        public static ExprQueryExpression WithRight(this ExprQueryExpression original, IExprSubQuery newRight) 
            => new ExprQueryExpression(left: original.Left, right: newRight, queryExpressionType: original.QueryExpressionType);

        public static ExprQueryExpression WithQueryExpressionType(this ExprQueryExpression original, ExprQueryExpressionType newQueryExpressionType) 
            => new ExprQueryExpression(left: original.Left, right: original.Right, queryExpressionType: newQueryExpressionType);

        public static ExprQuerySpecification WithSelectList(this ExprQuerySpecification original, IReadOnlyList<IExprSelecting> newSelectList) 
            => new ExprQuerySpecification(selectList: newSelectList, top: original.Top, from: original.From, where: original.Where, groupBy: original.GroupBy, distinct: original.Distinct);

        public static ExprQuerySpecification WithTop(this ExprQuerySpecification original, ExprValue? newTop) 
            => new ExprQuerySpecification(selectList: original.SelectList, top: newTop, from: original.From, where: original.Where, groupBy: original.GroupBy, distinct: original.Distinct);

        public static ExprQuerySpecification WithFrom(this ExprQuerySpecification original, IExprTableSource? newFrom) 
            => new ExprQuerySpecification(selectList: original.SelectList, top: original.Top, from: newFrom, where: original.Where, groupBy: original.GroupBy, distinct: original.Distinct);

        public static ExprQuerySpecification WithWhere(this ExprQuerySpecification original, ExprBoolean? newWhere) 
            => new ExprQuerySpecification(selectList: original.SelectList, top: original.Top, from: original.From, where: newWhere, groupBy: original.GroupBy, distinct: original.Distinct);

        public static ExprQuerySpecification WithGroupBy(this ExprQuerySpecification original, IReadOnlyList<ExprColumn>? newGroupBy) 
            => new ExprQuerySpecification(selectList: original.SelectList, top: original.Top, from: original.From, where: original.Where, groupBy: newGroupBy, distinct: original.Distinct);

        public static ExprQuerySpecification WithDistinct(this ExprQuerySpecification original, Boolean newDistinct) 
            => new ExprQuerySpecification(selectList: original.SelectList, top: original.Top, from: original.From, where: original.Where, groupBy: original.GroupBy, distinct: newDistinct);

        public static ExprScalarFunction WithSchema(this ExprScalarFunction original, ExprDbSchema? newSchema) 
            => new ExprScalarFunction(schema: newSchema, name: original.Name, arguments: original.Arguments);

        public static ExprScalarFunction WithName(this ExprScalarFunction original, ExprFunctionName newName) 
            => new ExprScalarFunction(schema: original.Schema, name: newName, arguments: original.Arguments);

        public static ExprScalarFunction WithArguments(this ExprScalarFunction original, IReadOnlyList<ExprValue>? newArguments) 
            => new ExprScalarFunction(schema: original.Schema, name: original.Name, arguments: newArguments);

        public static ExprSchemaName WithName(this ExprSchemaName original, String newName) 
            => new ExprSchemaName(name: newName);

        public static ExprSelect WithSelectQuery(this ExprSelect original, IExprSubQuery newSelectQuery) 
            => new ExprSelect(selectQuery: newSelectQuery, orderBy: original.OrderBy);

        public static ExprSelect WithOrderBy(this ExprSelect original, ExprOrderBy newOrderBy) 
            => new ExprSelect(selectQuery: original.SelectQuery, orderBy: newOrderBy);

        public static ExprSelectOffsetFetch WithSelectQuery(this ExprSelectOffsetFetch original, IExprSubQuery newSelectQuery) 
            => new ExprSelectOffsetFetch(selectQuery: newSelectQuery, orderBy: original.OrderBy);

        public static ExprSelectOffsetFetch WithOrderBy(this ExprSelectOffsetFetch original, ExprOrderByOffsetFetch newOrderBy) 
            => new ExprSelectOffsetFetch(selectQuery: original.SelectQuery, orderBy: newOrderBy);

        public static ExprStringConcat WithLeft(this ExprStringConcat original, ExprValue newLeft) 
            => new ExprStringConcat(left: newLeft, right: original.Right);

        public static ExprStringConcat WithRight(this ExprStringConcat original, ExprValue newRight) 
            => new ExprStringConcat(left: original.Left, right: newRight);

        public static ExprStringLiteral WithValue(this ExprStringLiteral original, String? newValue) 
            => new ExprStringLiteral(value: newValue);

        public static ExprSub WithLeft(this ExprSub original, ExprValue newLeft) 
            => new ExprSub(left: newLeft, right: original.Right);

        public static ExprSub WithRight(this ExprSub original, ExprValue newRight) 
            => new ExprSub(left: original.Left, right: newRight);

        public static ExprSum WithLeft(this ExprSum original, ExprValue newLeft) 
            => new ExprSum(left: newLeft, right: original.Right);

        public static ExprSum WithRight(this ExprSum original, ExprValue newRight) 
            => new ExprSum(left: original.Left, right: newRight);

        public static ExprTable WithFullName(this ExprTable original, IExprTableFullName newFullName) 
            => new ExprTable(fullName: newFullName, alias: original.Alias);

        public static ExprTable WithAlias(this ExprTable original, ExprTableAlias? newAlias) 
            => new ExprTable(fullName: original.FullName, alias: newAlias);

        public static ExprTableAlias WithAlias(this ExprTableAlias original, IExprAlias newAlias) 
            => new ExprTableAlias(alias: newAlias);

        public static ExprTableFullName WithDbSchema(this ExprTableFullName original, ExprDbSchema? newDbSchema) 
            => new ExprTableFullName(dbSchema: newDbSchema, tableName: original.TableName);

        public static ExprTableFullName WithTableName(this ExprTableFullName original, ExprTableName newTableName) 
            => new ExprTableFullName(dbSchema: original.DbSchema, tableName: newTableName);

        public static ExprTableName WithName(this ExprTableName original, String newName) 
            => new ExprTableName(name: newName);

        public static ExprTableValueConstructor WithItems(this ExprTableValueConstructor original, IReadOnlyList<ExprValueRow> newItems) 
            => new ExprTableValueConstructor(items: newItems);

        public static ExprTempTableName WithName(this ExprTempTableName original, String newName) 
            => new ExprTempTableName(name: newName);

        public static ExprTypeDateTime WithIsDate(this ExprTypeDateTime original, Boolean newIsDate) 
            => new ExprTypeDateTime(isDate: newIsDate);

        public static ExprTypeDecimal WithPrecisionScale(this ExprTypeDecimal original, DecimalPrecisionScale? newPrecisionScale) 
            => new ExprTypeDecimal(precisionScale: newPrecisionScale);

        public static ExprTypeString WithSize(this ExprTypeString original, Int32? newSize) 
            => new ExprTypeString(size: newSize, isUnicode: original.IsUnicode, isText: original.IsText);

        public static ExprTypeString WithIsUnicode(this ExprTypeString original, Boolean newIsUnicode) 
            => new ExprTypeString(size: original.Size, isUnicode: newIsUnicode, isText: original.IsText);

        public static ExprTypeString WithIsText(this ExprTypeString original, Boolean newIsText) 
            => new ExprTypeString(size: original.Size, isUnicode: original.IsUnicode, isText: newIsText);

        public static ExprUnboundedFrameBorder WithFrameBorderDirection(this ExprUnboundedFrameBorder original, FrameBorderDirection newFrameBorderDirection) 
            => new ExprUnboundedFrameBorder(frameBorderDirection: newFrameBorderDirection);

        public static ExprUnsafeValue WithUnsafeValue(this ExprUnsafeValue original, String newUnsafeValue) 
            => new ExprUnsafeValue(unsafeValue: newUnsafeValue);

        public static ExprUpdate WithTarget(this ExprUpdate original, ExprTable newTarget) 
            => new ExprUpdate(target: newTarget, setClause: original.SetClause, source: original.Source, filter: original.Filter);

        public static ExprUpdate WithSetClause(this ExprUpdate original, IReadOnlyList<ExprColumnSetClause> newSetClause) 
            => new ExprUpdate(target: original.Target, setClause: newSetClause, source: original.Source, filter: original.Filter);

        public static ExprUpdate WithSource(this ExprUpdate original, IExprTableSource? newSource) 
            => new ExprUpdate(target: original.Target, setClause: original.SetClause, source: newSource, filter: original.Filter);

        public static ExprUpdate WithFilter(this ExprUpdate original, ExprBoolean? newFilter) 
            => new ExprUpdate(target: original.Target, setClause: original.SetClause, source: original.Source, filter: newFilter);

        public static ExprValueFrameBorder WithValue(this ExprValueFrameBorder original, ExprValue newValue) 
            => new ExprValueFrameBorder(value: newValue, frameBorderDirection: original.FrameBorderDirection);

        public static ExprValueFrameBorder WithFrameBorderDirection(this ExprValueFrameBorder original, FrameBorderDirection newFrameBorderDirection) 
            => new ExprValueFrameBorder(value: original.Value, frameBorderDirection: newFrameBorderDirection);

        public static ExprValueRow WithItems(this ExprValueRow original, IReadOnlyList<ExprValue> newItems) 
            => new ExprValueRow(items: newItems);

       //CodeGenEnd
    }
}
