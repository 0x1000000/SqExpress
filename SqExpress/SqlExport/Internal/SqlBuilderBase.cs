using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
using SqExpress.Utils;

namespace SqExpress.SqlExport.Internal
{
    internal abstract class SqlBuilderBase: IExprVisitor<bool, object?>
    {
        protected readonly SqlBuilderOptions Options;

        protected readonly StringBuilder Builder;

        protected SqlBuilderBase(SqlBuilderOptions? options, StringBuilder? externalBuilder)
        {
            this.Options = options ?? SqlBuilderOptions.Default;
            this.Builder = externalBuilder ?? new StringBuilder();
        }

        private readonly SqlAliasGenerator _aliasGenerator = new SqlAliasGenerator();

        public bool VisitExprBooleanAnd(ExprBooleanAnd expr, object? arg)
        {
            if (expr.Left is ExprBooleanOr)
            {
                this.AcceptPar('(', expr.Left, ')', arg);
                this.Builder.Append("AND");
            }
            else
            {
                expr.Left.Accept(this, arg);
                this.Builder.Append(" AND");
            }

            if (expr.Right is ExprBooleanOr)
            {
                this.AcceptPar('(', expr.Right, ')', arg);
            }
            else
            {
                this.Builder.Append(' ');
                expr.Right.Accept(this, arg);
            }

            return true;
        }

        public bool VisitExprBooleanOr(ExprBooleanOr expr, object? arg)
        {
            expr.Left.Accept(this, arg);
            this.Builder.Append(" OR ");
            expr.Right.Accept(this, arg);

            return true;
        }

        public bool VisitExprBooleanNot(ExprBooleanNot expr, object? arg)
        {
            this.Builder.Append("NOT");
            if (expr.Expr is ExprPredicate)
            {
                this.Builder.Append(' ');
                expr.Expr.Accept(this, arg);
            }
            else
            {
                this.AcceptPar('(', expr.Expr, ')', arg);
            }

            return true;
        }

        public bool VisitExprBooleanNotEq(ExprBooleanNotEq exprBooleanNotEq, object? arg)
        {
            exprBooleanNotEq.Left.Accept(this, arg);
            this.Builder.Append("!=");
            exprBooleanNotEq.Right.Accept(this, arg);

            return true;
        }

        public bool VisitExprBooleanEq(ExprBooleanEq exprBooleanEq, object? arg)
        {
            exprBooleanEq.Left.Accept(this, arg);
            this.Builder.Append('=');
            exprBooleanEq.Right.Accept(this, arg);

            return true;
        }

        public bool VisitExprBooleanGt(ExprBooleanGt booleanGt, object? arg)
        {
            booleanGt.Left.Accept(this, arg);
            this.Builder.Append('>');
            booleanGt.Right.Accept(this, arg);

            return true;
        }

        public bool VisitExprBooleanGtEq(ExprBooleanGtEq booleanGtEq, object? arg)
        {
            booleanGtEq.Left.Accept(this, arg);
            this.Builder.Append(">=");
            booleanGtEq.Right.Accept(this, arg);

            return true;
        }

        public bool VisitExprBooleanLt(ExprBooleanLt booleanLt, object? arg)
        {
            booleanLt.Left.Accept(this, arg);
            this.Builder.Append('<');
            booleanLt.Right.Accept(this, arg);

            return true;
        }

        public bool VisitExprBooleanLtEq(ExprBooleanLtEq booleanLtEq, object? arg)
        {
            booleanLtEq.Left.Accept(this, arg);
            this.Builder.Append("<=");
            booleanLtEq.Right.Accept(this, arg);

            return true;
        }

        public bool VisitExprInSubQuery(ExprInSubQuery exprInSubQuery, object? arg)
        {
            exprInSubQuery.TestExpression.Accept(this, arg);
            this.Builder.Append(" IN");
            this.AcceptPar('(', exprInSubQuery.SubQuery, ')', arg);
            return true;
        }

        public bool VisitExprInValues(ExprInValues exprInValues, object? arg)
        {
            exprInValues.TestExpression.Accept(this, arg);
            this.AssertNotEmptyList(exprInValues.Items, "'IN' Predicate cannot have an empty list of expressions");
            this.Builder.Append(" IN");
            this.AcceptListComaSeparatedPar('(', exprInValues.Items, ')', arg);
            return true;
        }

        public bool VisitExprExists(ExprExists exprExists, object? arg)
        {
            this.Builder.Append("EXISTS");
            this.AcceptPar('(', exprExists.SubQuery, ')', arg);
            return true;
        }

        public bool VisitExprIsNull(ExprIsNull exprIsNull, object? arg)
        {
            exprIsNull.Test.Accept(this, arg);
            this.Builder.Append(" IS");
            if (exprIsNull.Not)
            {
                this.Builder.Append(" NOT");
            }
            this.Builder.Append(" NULL");
            return true;
        }

        public bool VisitExprLike(ExprLike exprLike, object? arg)
        {
            exprLike.Test.Accept(this, arg);
            this.Builder.Append(" LIKE ");
            exprLike.Pattern.Accept(this, arg);
            return true;
        }

        public bool VisitExprInt32Literal(ExprInt32Literal exprInt32Literal, object? arg)
        {
            if (exprInt32Literal.Value == null)
            {
                this.AppendNull();
                return true;
            }

            this.Builder.Append(exprInt32Literal.Value.Value);

            return true;
        }

        public abstract bool VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral, object? arg);

        public bool VisitExprStringLiteral(ExprStringLiteral stringLiteral, object? arg)
        {
            if (stringLiteral.Value == null)
            {
                this.AppendNull();
                return true;
            }

            this.AppendUnicodePrefix(stringLiteral.Value);
            this.Builder.Append('\'');
            if (stringLiteral.Value != null)
            {
                SqlInjectionChecker.AppendStringEscapeSingleQuote(this.Builder, stringLiteral.Value);
            }

            this.Builder.Append('\'');
            return true;
        }

        public bool VisitExprDateTimeLiteral(ExprDateTimeLiteral dateTimeLiteral, object? arg)
        {
            if (!dateTimeLiteral.Value.HasValue)
            {
                this.AppendNull();
            }
            else
            {
                this.Builder.Append('\'');
                if (dateTimeLiteral.Value.Value.TimeOfDay != TimeSpan.Zero)
                {
                    this.Builder.Append(dateTimeLiteral.Value.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
                }
                else
                {
                    this.Builder.Append(dateTimeLiteral.Value.Value.ToString("yyyy-MM-dd"));
                }
                this.Builder.Append('\'');
            }

            return true;
        }

        public abstract bool VisitExprBoolLiteral(ExprBoolLiteral boolLiteral, object? arg);

        public bool VisitExprInt64Literal(ExprInt64Literal int64Literal, object? arg)
        {
            if (int64Literal.Value.HasValue)
            {
                this.Builder.Append(int64Literal.Value.Value);
            }
            else
            {
                this.AppendNull();
            }

            return true;
        }

        public bool VisitExprByteLiteral(ExprByteLiteral byteLiteral, object? arg)
        {
            if (byteLiteral.Value.HasValue)
            {
                this.Builder.Append(byteLiteral.Value.Value);
            }
            else
            {
                this.AppendNull();
            }

            return true;
        }

        public bool VisitExprInt16Literal(ExprInt16Literal int16Literal, object? arg)
        {
            if (int16Literal.Value.HasValue)
            {
                this.Builder.Append(int16Literal.Value.Value);
            }
            else
            {
                this.AppendNull();
            }

            return true;
        }

        public bool VisitExprDecimalLiteral(ExprDecimalLiteral decimalLiteral, object? arg)
        {
            if (decimalLiteral.Value.HasValue)
            {
                this.Builder.Append(decimalLiteral.Value.Value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                this.AppendNull();
            }

            return true;
        }

        public bool VisitExprDoubleLiteral(ExprDoubleLiteral doubleLiteral, object? arg)
        {
            if (doubleLiteral.Value.HasValue)
            {
                this.Builder.Append(doubleLiteral.Value.Value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                this.AppendNull();
            }

            return true;
        }

        public bool VisitExprByteArrayLiteral(ExprByteArrayLiteral byteArrayLiteral, object? arg)
        {
            if (byteArrayLiteral.Value == null || byteArrayLiteral.Value.Count < 1)
            {
                this.AppendNull();
            }
            else
            {
                for (int i = 0; i < byteArrayLiteral.Value.Count; i++)
                {
                    this.Builder.AppendFormat("{0:x2}", byteArrayLiteral.Value[i]);
                }
            }

            return true;
        }

        public bool VisitExprNull(ExprNull exprNull, object? arg)
        {
            this.Builder.Append("NULL");
            return true;
        }

        public bool VisitExprDefault(ExprDefault exprDefault, object? arg)
        {
            this.Builder.Append("DEFAULT");
            return true;
        }

        public bool VisitExprSum(ExprSum exprSum, object? arg)
        {
            exprSum.Left.Accept(this, arg);
            this.Builder.Append('+');
            exprSum.Right.Accept(this, arg);
            return true;
        }

        public bool VisitExprSub(ExprSub exprSub, object? arg)
        {
            exprSub.Left.Accept(this, arg);
            this.Builder.Append('-');
            this.CheckPlusMinusParenthesizes(exprSub.Right, arg);
            return true;
        }

        public bool VisitExprMul(ExprMul exprMul, object? arg)
        {
            this.CheckPlusMinusParenthesizes(exprMul.Left, arg);
            this.Builder.Append('*');
            this.CheckPlusMinusParenthesizes(exprMul.Right, arg);
            return true;
        }

        public bool VisitExprDiv(ExprDiv exprDiv, object? arg)
        {
            this.CheckPlusMinusParenthesizes(exprDiv.Left, arg);
            this.Builder.Append('/');
            this.CheckPlusMinusParenthesizes(exprDiv.Right, arg);
            return true;
        }

        public abstract bool VisitExprStringConcat(ExprStringConcat exprStringConcat, object? arg);

        private void CheckPlusMinusParenthesizes(ExprValue exp, object? arg)
        {
            if (exp is ExprSum || exp is ExprSub)
            {
                this.Builder.Append('(');
                exp.Accept(this, arg);
                this.Builder.Append(')');
            }
            else
            {
                exp.Accept(this, arg);
            }
        }

        protected abstract void AppendSelectTop(ExprValue top, object? arg);

        public bool VisitExprQuerySpecification(ExprQuerySpecification exprQuerySpecification, object? arg)
        {
            this.Builder.Append("SELECT ");
            if (exprQuerySpecification.Distinct)
            {
                this.Builder.Append("DISTINCT ");
            }
            if (!ReferenceEquals(exprQuerySpecification.Top, null))
            {
                this.AppendSelectTop(exprQuerySpecification.Top, arg);
            }

            this.AcceptListComaSeparated(exprQuerySpecification.SelectList, arg);

            if (exprQuerySpecification.From != null)
            {
                this.Builder.Append(" FROM ");
                exprQuerySpecification.From.Accept(this, arg);
            }

            if (exprQuerySpecification.Where != null)
            {
                this.Builder.Append(" WHERE ");
                exprQuerySpecification.Where.Accept(this, arg);
            }

            if (exprQuerySpecification.GroupBy != null)
            {
                this.Builder.Append(" GROUP BY ");
                this.AcceptListComaSeparated(exprQuerySpecification.GroupBy, arg);
            }

            return true;
        }

        public bool VisitExprJoinedTable(ExprJoinedTable joinedTable, object? arg)
        {
            joinedTable.Left.Accept(this, arg);
            switch (joinedTable.JoinType)
            {
                case ExprJoinedTable.ExprJoinType.Inner:
                    this.Builder.Append(" JOIN ");
                    break;
                case ExprJoinedTable.ExprJoinType.Left:
                    this.Builder.Append(" LEFT JOIN ");
                    break;
                case ExprJoinedTable.ExprJoinType.Right:
                    this.Builder.Append(" RIGHT JOIN ");
                    break;
                case ExprJoinedTable.ExprJoinType.Full:
                    this.Builder.Append(" FULL JOIN ");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            joinedTable.Right.Accept(this, arg);
            this.Builder.Append(" ON ");
            joinedTable.SearchCondition.Accept(this, arg);

            return true;
        }

        public bool VisitExprCrossedTable(ExprCrossedTable exprCrossedTable, object? arg)
        {
            exprCrossedTable.Left.Accept(this, arg);
            this.Builder.Append(" CROSS JOIN ");
            exprCrossedTable.Right.Accept(this, arg);
            return true;
        }

        public bool VisitExprQueryExpression(ExprQueryExpression exprQueryExpression, object? arg)
        {
            exprQueryExpression.Left.Accept(this, arg);

            switch (exprQueryExpression.QueryExpressionType)
            {
                case ExprQueryExpressionType.UnionAll:
                    this.Builder.Append(" UNION ALL ");
                    break;
                case ExprQueryExpressionType.Union:
                    this.Builder.Append(" UNION ");
                    break;
                case ExprQueryExpressionType.Except:
                    this.Builder.Append(" EXCEPT ");
                    break;
                case ExprQueryExpressionType.Intersect:
                    this.Builder.Append(" INTERSECT ");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (exprQueryExpression.Right is ExprQueryExpression)
            {
                this.AcceptPar('(', exprQueryExpression.Right, ')', arg);
            }
            else
            {
                exprQueryExpression.Right.Accept(this, arg);
            }

            return true;
        }

        public bool VisitExprSelect(ExprSelect exprSelect, object? arg)
        {
            exprSelect.SelectQuery.Accept(this, arg);
            this.Builder.Append(" ORDER BY ");
            exprSelect.OrderBy.Accept(this, arg);
            return true;
        }

        public bool VisitExprSelectOffsetFetch(ExprSelectOffsetFetch exprSelectOffsetFetch, object? arg)
        {
            exprSelectOffsetFetch.SelectQuery.Accept(this, arg);
            this.Builder.Append(" ORDER BY ");
            exprSelectOffsetFetch.OrderBy.Accept(this, arg);
            return true;
        }

        public bool VisitExprOrderBy(ExprOrderBy exprOrderBy, object? arg)
        {
            this.AcceptListComaSeparated(exprOrderBy.OrderList, arg);
            return true;
        }

        public bool VisitExprOrderByOffsetFetch(ExprOrderByOffsetFetch exprOrderByOffsetFetch, object? arg)
        {
            this.AcceptListComaSeparated(exprOrderByOffsetFetch.OrderList, arg);
            exprOrderByOffsetFetch.OffsetFetch.Accept(this, arg);
            return true;
        }

        public bool VisitExprOrderByItem(ExprOrderByItem exprOrderByItem, object? arg)
        {
            exprOrderByItem.Value.Accept(this, arg);
            if (exprOrderByItem.Descendant)
            {
                this.Builder.Append(" DESC");
            }
            return true;
        }

        public bool VisitExprOffsetFetch(ExprOffsetFetch exprOffsetFetch, object? arg)
        {
            this.Builder.Append(" OFFSET ");
            exprOffsetFetch.Offset.Accept(this, arg);
            this.Builder.Append(" ROW");

            if (!ReferenceEquals(exprOffsetFetch.Fetch,null))
            {
                this.Builder.Append(" FETCH NEXT ");
                exprOffsetFetch.Fetch.Accept(this, arg);
                this.Builder.Append(" ROW ONLY");
            }

            return true;
        }

        public bool VisitExprOutputColumnInserted(ExprOutputColumnInserted exprOutputColumnInserted, object? arg)
        {
            this.Builder.Append("INSERTED.");
            exprOutputColumnInserted.ColumnName.Accept(this, arg);
            return true;
        }

        public bool VisitExprOutputColumnDeleted(ExprOutputColumnDeleted exprOutputColumnDeleted, object? arg)
        {
            this.Builder.Append("DELETED.");
            exprOutputColumnDeleted.ColumnName.Accept(this, arg);
            return true;
        }

        public bool VisitExprOutputColumn(ExprOutputColumn exprOutputColumn, object? arg)
        {
            exprOutputColumn.Column.Accept(this, arg);
            return true;
        }

        public bool VisitExprOutputAction(ExprOutputAction exprOutputAction, object? arg)
        {
            this.Builder.Append("$ACTION");
            if (exprOutputAction.Alias != null)
            {
                this.Builder.Append(' ');
                exprOutputAction.Alias.Accept(this, arg);
            }
            return true;
        }

        public bool VisitExprOutput(ExprOutput exprOutput, object? arg)
        {
            this.AssertNotEmptyList(exprOutput.Columns, "Output column list cannot be empty");
            this.AcceptListComaSeparated(exprOutput.Columns, arg);
            return true;
        }

        public bool VisitExprAggregateFunction(ExprAggregateFunction exprAggregateFunction, object? arg)
        {
            exprAggregateFunction.Name.Accept(this, arg);
            this.Builder.Append('(');
            if (exprAggregateFunction.IsDistinct)
            {
                this.Builder.Append("DISTINCT ");
            }

            exprAggregateFunction.Expression.Accept(this, arg);
            this.Builder.Append(')');

            return true;
        }

        public bool VisitExprScalarFunction(ExprScalarFunction exprScalarFunction, object? arg)
        {
            exprScalarFunction.Name.Accept(this, arg);
            this.AcceptListComaSeparatedPar('(', exprScalarFunction.Arguments, ')', arg);

            return true;
        }

        public bool VisitExprAnalyticFunction(ExprAnalyticFunction exprAnalyticFunction, object? arg)
        {
            exprAnalyticFunction.Name.Accept(this, arg);
            this.Builder.Append('(');
            if (exprAnalyticFunction.Arguments != null)
            {
                this.AssertNotEmptyList(exprAnalyticFunction.Arguments, "Arguments list cannot be empty");
                this.AcceptListComaSeparated(exprAnalyticFunction.Arguments, arg);
            }
            this.Builder.Append(')');
            exprAnalyticFunction.Over.Accept(this, arg);
            return true;
        }

        public bool VisitExprOver(ExprOver exprOver, object? arg)
        {
            this.Builder.Append("OVER(");

            if (exprOver.Partitions != null)
            {
                this.AssertNotEmptyList(exprOver.Partitions, "Partition list cannot be empty");
                this.Builder.Append("PARTITION BY ");
                this.AcceptListComaSeparated(exprOver.Partitions, arg);
            }

            if (exprOver.OrderBy != null)
            {
                if (exprOver.Partitions != null)
                {
                    this.Builder.Append(' ');
                }
                this.Builder.Append("ORDER BY ");
                exprOver.OrderBy.Accept(this, arg);
            }
            this.Builder.Append(")");
            return true;
        }

        public bool VisitExprCase(ExprCase exprCase, object? arg)
        {
            this.AssertNotEmptyList(exprCase.Cases, "Cases cannot be empty");

            this.Builder.Append("CASE");
            for (int i = 0; i < exprCase.Cases.Count; i++)
            {
                this.Builder.Append(' ');
                exprCase.Cases[i].Accept(this, arg);
            }
            this.Builder.Append(" ELSE ");
            exprCase.DefaultValue.Accept(this, arg);
            this.Builder.Append(" END");
            return true;
        }

        public bool VisitExprCaseWhenThen(ExprCaseWhenThen exprCaseWhenThen, object? arg)
        {
            this.Builder.Append("WHEN ");
            exprCaseWhenThen.Condition.Accept(this, arg);
            this.Builder.Append(" THEN ");
            exprCaseWhenThen.Value.Accept(this, arg);

            return true;
        }

        public bool VisitExprColumn(ExprColumn exprColumn, object? arg)
        {
            if (exprColumn.Source != null)
            {
                exprColumn.Source.Accept(this, arg);
                this.Builder.Append('.');
            }

            exprColumn.ColumnName.Accept(this, arg);

            return true;
        }

        public bool VisitExprTable(ExprTable exprTable, object? arg)
        {
            exprTable.FullName.Accept(this, arg);
            if (exprTable.Alias != null)
            {
                this.Builder.Append(' ');
                exprTable.Alias.Accept(this, arg);
            }
            return true;
        }

        public bool VisitExprColumnName(ExprColumnName columnName, object? arg)
        {
            this.AppendName(columnName.Name);
            return true;
        }

        public bool VisitExprTableName(ExprTableName tableName, object? arg)
        {
            this.AppendName(tableName.Name);
            return true;
        }

        public bool VisitExprTableFullName(ExprTableFullName exprTableFullName, object? arg)
        {
            if (exprTableFullName.DbSchema != null)
            {
                exprTableFullName.DbSchema.Accept(this, arg);
                this.Builder.Append('.');
            }
            exprTableFullName.TableName.Accept(this, arg);
            return true;
        }

        public bool VisitExprAlias(ExprAlias alias, object? arg)
        {
            this.AppendName(alias.Name);
            return true;
        }

        public bool VisitExprAliasGuid(ExprAliasGuid aliasGuid, object? arg)
        {
            this.AppendName(this._aliasGenerator.GetAlias(aliasGuid));
            return true;
        }

        public bool VisitExprColumnAlias(ExprColumnAlias exprColumnAlias, object? arg)
        {
            this.AppendName(exprColumnAlias.Name);
            return true;
        }

        public bool VisitExprAliasedColumn(ExprAliasedColumn exprAliasedColumn, object? arg)
        {
            exprAliasedColumn.Column.Accept(this, arg);
            if (exprAliasedColumn.Alias != null)
            {
                this.Builder.Append(' ');
                exprAliasedColumn.Alias?.Accept(this, arg);
            }
            return true;
        }

        public bool VisitExprAliasedColumnName(ExprAliasedColumnName exprAliasedColumnName, object? arg)
        {
            exprAliasedColumnName.Column.Accept(this, arg);
            if (exprAliasedColumnName.Alias != null)
            {
                this.Builder.Append(' ');
                exprAliasedColumnName.Alias.Accept(this, arg);
            }
            return true;
        }

        public bool VisitExprAliasedSelecting(ExprAliasedSelecting exprAliasedSelecting, object? arg)
        {
            exprAliasedSelecting.Value.Accept(this, arg);
            this.Builder.Append(' ');
            exprAliasedSelecting.Alias.Accept(this, arg);
            return true;
        }

        public bool VisitExprTableAlias(ExprTableAlias tableAlias, object? arg)
        {
            tableAlias.Alias.Accept(this, arg);
            return true;
        }

        public bool VisitExprSchemaName(ExprSchemaName schemaName, object? arg)
        {
            this.AppendName(this.Options.MapSchema(schemaName.Name));
            return true;
        }

        public bool VisitExprDatabaseName(ExprDatabaseName databaseName, object? arg)
        {
            this.AppendName(databaseName.Name);
            return true;
        }

        public bool VisitExprDbSchema(ExprDbSchema exprDbSchema, object? arg)
        {
            if (exprDbSchema.Database != null)
            {
                exprDbSchema.Database.Accept(this, arg);
                this.Builder.Append('.');
            }

            exprDbSchema.Schema.Accept(this, arg);
            return true;
        }

        public bool VisitExprFunctionName(ExprFunctionName exprFunctionName, object? arg)
        {
            if (exprFunctionName.BuiltIn)
            {
                SqlInjectionChecker.AssertValidBuildInFunctionName(exprFunctionName.Name);
                this.Builder.Append(exprFunctionName.Name);
            }
            else
            {
                this.AppendName(exprFunctionName.Name);
            }
            return true;
        }

        public bool VisitExprRowValue(ExprRowValue rowValue, object? arg)
        {
            if (rowValue.Items == null || rowValue.Items.Count < 1)
            {
                throw new SqExpressException("Row value should have at least one column");
            }

            this.AcceptListComaSeparatedPar('(',rowValue.Items, ')', arg);

            return true;
        }

        public bool VisitExprTableValueConstructor(ExprTableValueConstructor tableValueConstructor, object? arg)
        {
            int initialLength = this.Builder.Length;
            bool first = true;

            this.Builder.Append("VALUES ");

            foreach (var rowValue in tableValueConstructor.Items)
            {
                if (!first)
                {
                    this.Builder.Append(',');
                }
                else
                {
                    first = false;
                }

                rowValue.Accept(this, arg);
            }

            if (first)
            {
                return this.Rollback(initialLength);
            }

            return true;
        }

        public bool VisitExprDerivedTableQuery(ExprDerivedTableQuery exprDerivedTableQuery, object? arg)
        {
            this.AcceptPar('(', exprDerivedTableQuery.Query, ')', arg);
            exprDerivedTableQuery.Alias.Accept(this, arg);
            if (exprDerivedTableQuery.Columns != null)
            {
                exprDerivedTableQuery.Columns.AssertNotEmpty("List of columns in a derived table with values literals cannot be empty");
                var selectedColumns = exprDerivedTableQuery.Query.GetOutputColumnNames();

                if (selectedColumns.Count != exprDerivedTableQuery.Columns.Count)
                {
                    throw new SqExpressException("Number of declared columns does not match to number of selected columns in the derived table sub query");
                }

                bool allMatch = true;
                for (int i = 0; i < selectedColumns.Count; i++)
                {
                    if (!string.Equals(selectedColumns[i],
                        ((IExprNamedSelecting) exprDerivedTableQuery.Columns[i]).OutputName))
                    {
                        allMatch = false;
                        break;
                    }
                }
                if (!allMatch)
                {
                    this.AcceptListComaSeparatedPar('(', exprDerivedTableQuery.Columns, ')', arg);
                }
            }

            return true;
        }

        public bool VisitExprDerivedTableValues(ExprDerivedTableValues derivedTableValues, object? arg)
        {
            int initialLength = this.Builder.Length;
            if (!this.AcceptPar('(', derivedTableValues.Values, ')', arg))
            {
                return this.Rollback(initialLength);
            }
            derivedTableValues.Alias.Accept(this, arg);
            derivedTableValues.Columns.AssertNotEmpty("List of columns in a derived table with values literals cannot be empty");
            this.AcceptListComaSeparatedPar('(', derivedTableValues.Columns, ')', arg);

            return true;
        }

        public bool VisitExprColumnSetClause(ExprColumnSetClause columnSetClause, object? arg)
        {
            columnSetClause.Column.Accept(this, arg);
            this.Builder.Append('=');
            columnSetClause.Value.Accept(this, arg);

            return true;
        }

        public bool VisitExprMerge(ExprMerge merge, object? arg)
        {
            int init = this.Builder.Length;

            this.Builder.Append("MERGE ");
            merge.TargetTable.Accept(this, arg);
            this.Builder.Append(" USING ");
            if (!merge.Source.Accept(this, arg))
            {
                return this.Rollback(init);
            }
            this.Builder.Append(" ON ");
            merge.On.Accept(this, arg);
            if (merge.WhenMatched != null)
            {
                this.Builder.Append(" WHEN MATCHED");
                merge.WhenMatched.Accept(this, arg);
            }
            if (merge.WhenNotMatchedByTarget != null)
            {
                this.Builder.Append(" WHEN NOT MATCHED");
                merge.WhenNotMatchedByTarget.Accept(this, arg);
            }
            if (merge.WhenNotMatchedBySource != null)
            {
                this.Builder.Append(" WHEN NOT MATCHED BY SOURCE");
                merge.WhenNotMatchedBySource.Accept(this, arg);
            }
            this.Builder.Append(';');

            return true;
        }

        public bool VisitExprMergeOutput(ExprMergeOutput mergeOutput, object? arg)
        {
            if (this.VisitExprMerge(mergeOutput, arg))
            {
                this.Builder.Length = this.Builder.Length - 1;// ; <-
                this.Builder.Append(" OUTPUT ");
                mergeOutput.Output.Accept(this, arg);
                this.Builder.Append(';');
                return true;
            }
            return false;
        }

        public bool VisitExprMergeMatchedUpdate(ExprMergeMatchedUpdate mergeMatchedUpdate, object? arg)
        {
            if (mergeMatchedUpdate.And != null)
            {
                this.Builder.Append(" AND ");
                mergeMatchedUpdate.And.Accept(this, arg);
            }

            this.AssertNotEmptyList(mergeMatchedUpdate.Set, "Set Clause cannot be empty");

            this.Builder.Append(" THEN UPDATE SET ");

            this.AcceptListComaSeparated(mergeMatchedUpdate.Set, arg);

            return true;
        }

        public bool VisitExprMergeMatchedDelete(ExprMergeMatchedDelete mergeMatchedDelete, object? arg)
        {
            if (mergeMatchedDelete.And != null)
            {
                this.Builder.Append(" AND ");
                mergeMatchedDelete.And.Accept(this, arg);
            }

            this.Builder.Append(" THEN  DELETE");

            return true;
        }

        public bool VisitExprExprMergeNotMatchedInsert(ExprExprMergeNotMatchedInsert exprMergeNotMatchedInsert, object? arg)
        {
            if (exprMergeNotMatchedInsert.And != null)
            {
                this.Builder.Append(" AND ");
                exprMergeNotMatchedInsert.And.Accept(this, arg);
            }

            this.AssertNotEmptyList(exprMergeNotMatchedInsert.Values, "Values cannot be empty");

            if (exprMergeNotMatchedInsert.Columns.Count > 0 &&
                exprMergeNotMatchedInsert.Columns.Count != exprMergeNotMatchedInsert.Values.Count)
            {
                throw new SqExpressException("Columns and values numbers do not match");
            }

            this.Builder.Append(" THEN INSERT");
            this.AcceptListComaSeparatedPar('(', exprMergeNotMatchedInsert.Columns, ')', arg);
            this.Builder.Append(" VALUES");
            this.AcceptListComaSeparatedPar('(', exprMergeNotMatchedInsert.Values, ')', arg);

            return true;
        }

        public bool VisitExprExprMergeNotMatchedInsertDefault(ExprExprMergeNotMatchedInsertDefault exprExprMergeNotMatchedInsertDefault, object? arg)
        {
            if (exprExprMergeNotMatchedInsertDefault.And != null)
            {
                this.Builder.Append(" AND ");
                exprExprMergeNotMatchedInsertDefault.And.Accept(this, arg);
            }

            this.Builder.Append(" THEN INSERT DEFAULT VALUES");

            return true;
        }

        public bool VisitExprInsert(ExprInsert exprInsert, object? arg)
        {
            this.GenericInsert(exprInsert, null, null, arg);
            return true;
        }

        protected void GenericInsert(ExprInsert exprInsert, Action? middleHandler, Action? endHandler, object? arg)
        {
            this.Builder.Append("INSERT INTO ");
            exprInsert.Target.Accept(this, arg);
            if (exprInsert.TargetColumns != null)
            {
                this.AssertNotEmptyList(exprInsert.TargetColumns, "Insert column list cannot be empty");
                this.AcceptListComaSeparatedPar('(', exprInsert.TargetColumns, ')', arg);
            }

            if (middleHandler != null)
            {
                this.Builder.Append(' ');
                middleHandler();
            }
            this.Builder.Append(' ');
            exprInsert.Source.Accept(this, arg);
            if (endHandler != null)
            {
                this.Builder.Append(' ');
                endHandler();
            }
        }

        public abstract bool VisitExprInsertOutput(ExprInsertOutput exprInsertOutput, object? arg);

        public bool VisitExprInsertValues(ExprInsertValues exprInsertValues, object? arg)
        {
            exprInsertValues.Values.Accept(this, arg);
            return true;
        }

        public bool VisitExprInsertQuery(ExprInsertQuery exprInsertQuery, object? arg)
        {
            exprInsertQuery.Query.Accept(this, arg);
            return true;
        }

        public abstract bool VisitExprUpdate(ExprUpdate exprUpdate, object? arg);

        public abstract bool VisitExprDelete(ExprDelete exprDelete, object? arg);

        public abstract bool VisitExprDeleteOutput(ExprDeleteOutput exprDeleteOutput, object? arg);

        public bool VisitExprCast(ExprCast exprCast, object? arg)
        {
            this.Builder.Append("CAST(");
            exprCast.Expression.Accept(this, arg);
            this.Builder.Append(" AS ");
            exprCast.SqlType.Accept(this, arg);
            this.Builder.Append(')');
            return true;
        }

        public abstract bool VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean, object? arg);

        public abstract bool VisitExprTypeByte(ExprTypeByte exprTypeByte, object? arg);

        public abstract bool VisitExprTypeInt16(ExprTypeInt16 exprTypeInt16, object? arg);

        public abstract bool VisitExprTypeInt32(ExprTypeInt32 exprTypeInt32, object? arg);

        public abstract bool VisitExprTypeInt64(ExprTypeInt64 exprTypeInt64, object? arg);

        public abstract bool VisitExprTypeDecimal(ExprTypeDecimal exprTypeDecimal, object? arg);

        public abstract bool VisitExprTypeDouble(ExprTypeDouble exprTypeDouble, object? arg);

        public abstract bool VisitExprTypeDateTime(ExprTypeDateTime exprTypeDateTime, object? arg);

        public abstract bool VisitExprTypeGuid(ExprTypeGuid exprTypeGuid, object? arg);

        public abstract bool VisitExprTypeString(ExprTypeString exprTypeString, object? arg);

        public abstract bool VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull, object? arg);

        public bool VisitExprFuncCoalesce(ExprFuncCoalesce exprFuncCoalesce, object? arg)
        {
            this.Builder.Append("COALESCE(");
            exprFuncCoalesce.Test.Accept(this, arg);
            this.Builder.Append(',');
            this.AssertNotEmptyList(exprFuncCoalesce.Alts, "Alt argument list cannot be empty in 'COALESCE' function call");
            this.AcceptListComaSeparated(exprFuncCoalesce.Alts, arg);
            this.Builder.Append(')');
            return true;
        }

        public abstract bool VisitExprGetDate(ExprGetDate exprGetDate, object? arg);

        public abstract bool VisitExprGetUtcDate(ExprGetUtcDate exprGetUtcDate, object? arg);

        public abstract void AppendName(string name);

        protected void AppendNull()
        {
            this.Builder.Append("NULL");
        }

        protected bool AcceptPar(char start, IExpr list, char end, object? arg)
        {
            int initial = this.Builder.Length;
            this.Builder.Append(start);
            if (!list.Accept(this, arg))
            {
                return this.Rollback(initial);
            }
            this.Builder.Append(end);
            return true;
        }

        internal bool AcceptListComaSeparatedPar(char start, IReadOnlyList<IExpr> list, char end, object? arg)
        {
            int initial = this.Builder.Length;
            this.Builder.Append(start);
            if (!this.AcceptListComaSeparated(list, arg))
            {
                return this.Rollback(initial);
            }
            this.Builder.Append(end);

            return true;
        }

        protected bool AcceptListComaSeparated(IReadOnlyList<IExpr> list, object? arg)
        {
            int initial = this.Builder.Length;

            for (int i = 0; i < list.Count; i++)
            {
                if (i != 0)
                {
                    this.Builder.Append(',');
                }

                if (!list[i].Accept(this, arg))
                {
                    return this.Rollback(initial);
                }
            }

            return true;
        }

        protected abstract void AppendUnicodePrefix(string str);

        protected void AssertNotEmptyList<T>(IReadOnlyList<T> list, string errorText)
        {
            if (list == null || list.Count < 1)
            {
                throw new SqExpressException(errorText);
            }
        }

        public override string ToString()
        {
            return this.Builder.ToString();
        }

        private bool Rollback(int initialLength)
        {
            this.Builder.Length = initialLength;
            return false;
        }
    }
}