using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SqExpress.SqlExport.Internal;
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

namespace SqExpress.SqlExport
{
    public abstract class SqlBuilderBase: IExprVisitor<bool>
    {
        protected readonly SqlBuilderOptions Options;

        protected readonly StringBuilder Builder;

        protected SqlBuilderBase(SqlBuilderOptions? options, StringBuilder? externalBuilder)
        {
            this.Options = options ?? SqlBuilderOptions.Default;
            this.Builder = externalBuilder ?? new StringBuilder();
        }

        private readonly SqlAliasGenerator _aliasGenerator = new SqlAliasGenerator();

        public bool VisitExprBooleanAnd(ExprBooleanAnd expr)
        {
            if (expr.Left is ExprBooleanOr)
            {
                this.AcceptPar('(', expr.Left, ')');
                this.Builder.Append("AND");
            }
            else
            {
                expr.Left.Accept(this);
                this.Builder.Append(" AND");
            }

            if (expr.Right is ExprBooleanOr)
            {
                this.AcceptPar('(', expr.Right, ')');
            }
            else
            {
                this.Builder.Append(' ');
                expr.Right.Accept(this);
            }

            return true;
        }

        public bool VisitExprBooleanOr(ExprBooleanOr expr)
        {
            expr.Left.Accept(this);
            this.Builder.Append(" OR ");
            expr.Right.Accept(this);

            return true;
        }

        public bool VisitExprBooleanNot(ExprBooleanNot expr)
        {
            this.Builder.Append("NOT");
            if (expr.Expr is ExprPredicate)
            {
                this.Builder.Append(' ');
                expr.Expr.Accept(this);
            }
            else
            {
                this.AcceptPar('(', expr.Expr, ')');
            }

            return true;
        }

        public bool VisitExprBooleanNotEq(ExprBooleanNotEq exprBooleanNotEq)
        {
            exprBooleanNotEq.Left.Accept(this);
            this.Builder.Append("!=");
            exprBooleanNotEq.Right.Accept(this);

            return true;
        }

        public bool VisitExprBooleanEq(ExprBooleanEq exprBooleanEq)
        {
            exprBooleanEq.Left.Accept(this);
            this.Builder.Append('=');
            exprBooleanEq.Right.Accept(this);

            return true;
        }

        public bool VisitExprBooleanGt(ExprBooleanGt booleanGt)
        {
            booleanGt.Left.Accept(this);
            this.Builder.Append('>');
            booleanGt.Right.Accept(this);

            return true;
        }

        public bool VisitExprBooleanGtEq(ExprBooleanGtEq booleanGtEq)
        {
            booleanGtEq.Left.Accept(this);
            this.Builder.Append(">=");
            booleanGtEq.Right.Accept(this);

            return true;
        }

        public bool VisitExprBooleanLt(ExprBooleanLt booleanLt)
        {
            booleanLt.Left.Accept(this);
            this.Builder.Append('<');
            booleanLt.Right.Accept(this);

            return true;
        }

        public bool VisitExprBooleanLtEq(ExprBooleanLtEq booleanLtEq)
        {
            booleanLtEq.Left.Accept(this);
            this.Builder.Append("<=");
            booleanLtEq.Right.Accept(this);

            return true;
        }

        public bool VisitExprInSubQuery(ExprInSubQuery exprInSubQuery)
        {
            exprInSubQuery.TestExpression.Accept(this);
            this.Builder.Append(" IN");
            this.AcceptPar('(', exprInSubQuery.SubQuery, ')');
            return true;
        }

        public bool VisitExprInValues(ExprInValues exprInValues)
        {
            exprInValues.TestExpression.Accept(this);
            this.AssertNotEmptyList(exprInValues.Items, "'IN' Predicate cannot have an empty list of expressions");
            this.Builder.Append(" IN");
            this.AcceptListComaSeparatedPar('(', exprInValues.Items, ')');
            return true;
        }

        public bool VisitExprExists(ExprExists exprExists)
        {
            this.Builder.Append("EXISTS");
            this.AcceptPar('(', exprExists.SubQuery, ')');
            return true;
        }

        public bool VisitExprIsNull(ExprIsNull exprIsNull)
        {
            exprIsNull.Test.Accept(this);
            this.Builder.Append(" IS");
            if (exprIsNull.Not)
            {
                this.Builder.Append(" NOT");
            }
            this.Builder.Append(" NULL");
            return true;
        }

        public bool VisitExprLike(ExprLike exprLike)
        {
            exprLike.Test.Accept(this);
            this.Builder.Append(" LIKE ");
            exprLike.Pattern.Accept(this);
            return true;
        }

        public bool VisitExprIntLiteral(ExprInt32Literal exprInt32Literal)
        {
            if (exprInt32Literal.Value == null)
            {
                this.AppendNull();
                return true;
            }

            this.Builder.Append(exprInt32Literal.Value.Value);

            return true;
        }

        public abstract bool VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral);

        public bool VisitExprStringLiteral(ExprStringLiteral stringLiteral)
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

        public bool VisitExprDateTimeLiteral(ExprDateTimeLiteral dateTimeLiteral)
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

        public abstract bool VisitExprBoolLiteral(ExprBoolLiteral boolLiteral);

        public bool VisitExprLongLiteral(ExprInt64Literal int64Literal)
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

        public bool VisitExprByteLiteral(ExprByteLiteral byteLiteral)
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

        public bool VisitExprShortLiteral(ExprInt16Literal int16Literal)
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

        public bool VisitExprDecimalLiteral(ExprDecimalLiteral decimalLiteral)
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

        public bool VisitExprDoubleLiteral(ExprDoubleLiteral doubleLiteral)
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

        public bool VisitExprByteArrayLiteral(ExprByteArrayLiteral byteArrayLiteral)
        {
            if (byteArrayLiteral.Value.Count < 1)
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

        public bool VisitExprNull(ExprNull exprNull)
        {
            this.Builder.Append("NULL");
            return true;
        }

        public bool VisitExprDefault(ExprDefault exprDefault)
        {
            this.Builder.Append("DEFAULT");
            return true;
        }

        public bool VisitExprSum(ExprSum exprSum)
        {
            exprSum.Left.Accept(this);
            this.Builder.Append('+');
            exprSum.Right.Accept(this);
            return true;
        }

        public bool VisitExprSub(ExprSub exprSub)
        {
            exprSub.Left.Accept(this);
            this.Builder.Append('-');
            this.CheckPlusMinusParenthesizes(exprSub.Right);
            return true;
        }

        public bool VisitExprMul(ExprMul exprMul)
        {
            this.CheckPlusMinusParenthesizes(exprMul.Left);
            this.Builder.Append('*');
            this.CheckPlusMinusParenthesizes(exprMul.Right);
            return true;
        }

        public bool VisitExprDiv(ExprDiv exprDiv)
        {
            this.CheckPlusMinusParenthesizes(exprDiv.Left);
            this.Builder.Append('/');
            this.CheckPlusMinusParenthesizes(exprDiv.Right);
            return true;
        }

        public abstract bool VisitExprStringConcat(ExprStringConcat exprStringConcat);

        private void CheckPlusMinusParenthesizes(ExprValue exp)
        {
            if (exp is ExprSum || exp is ExprSub)
            {
                this.Builder.Append('(');
                exp.Accept(this);
                this.Builder.Append(')');
            }
            else
            {
                exp.Accept(this);
            }
        }

        protected abstract void AppendSelectTop(ExprValue top);

        public bool VisitExprQuerySpecification(ExprQuerySpecification exprQuerySpecification)
        {
            this.Builder.Append("SELECT ");
            if (exprQuerySpecification.Distinct)
            {
                this.Builder.Append("DISTINCT ");
            }
            if (!ReferenceEquals(exprQuerySpecification.Top, null))
            {
                this.AppendSelectTop(exprQuerySpecification.Top);
            }

            this.AcceptListComaSeparated(exprQuerySpecification.SelectList);

            if (exprQuerySpecification.From != null)
            {
                this.Builder.Append(" FROM ");
                exprQuerySpecification.From.Accept(this);
            }

            if (exprQuerySpecification.Where != null)
            {
                this.Builder.Append(" WHERE ");
                exprQuerySpecification.Where.Accept(this);
            }

            if (exprQuerySpecification.GroupBy != null)
            {
                this.Builder.Append(" GROUP BY ");
                this.AcceptListComaSeparated(exprQuerySpecification.GroupBy);
            }

            return true;
        }

        public bool VisitExprJoinedTable(ExprJoinedTable joinedTable)
        {
            joinedTable.Left.Accept(this);
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
            joinedTable.Right.Accept(this);
            this.Builder.Append(" ON ");
            joinedTable.SearchCondition.Accept(this);

            return true;
        }

        public bool VisitExprCrossedTable(ExprCrossedTable exprCrossedTable)
        {
            exprCrossedTable.Left.Accept(this);
            this.Builder.Append(" CROSS JOIN ");
            exprCrossedTable.Right.Accept(this);
            return true;
        }

        public bool VisitExprQueryExpression(ExprQueryExpression exprQueryExpression)
        {
            exprQueryExpression.Left.Accept(this);

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
                this.AcceptPar('(', exprQueryExpression.Right, ')');
            }
            else
            {
                exprQueryExpression.Right.Accept(this);
            }

            return true;
        }

        public bool VisitExprSelect(ExprSelect exprSelect)
        {
            exprSelect.SelectQuery.Accept(this);
            this.Builder.Append(" ORDER BY ");
            exprSelect.OrderBy.Accept(this);
            return true;
        }

        public bool VisitExprSelectOffsetFetch(ExprSelectOffsetFetch exprSelectOffsetFetch)
        {
            exprSelectOffsetFetch.SelectQuery.Accept(this);
            this.Builder.Append(" ORDER BY ");
            exprSelectOffsetFetch.OrderBy.Accept(this);
            return true;
        }

        public bool VisitExprOrderBy(ExprOrderBy exprOrderBy)
        {
            this.AcceptListComaSeparated(exprOrderBy.OrderList);
            return true;
        }

        public bool VisitExprOrderByOffsetFetch(ExprOrderByOffsetFetch exprOrderByOffsetFetch)
        {
            this.AcceptListComaSeparated(exprOrderByOffsetFetch.OrderList);
            exprOrderByOffsetFetch.OffsetFetch.Accept(this);
            return true;
        }

        public bool VisitExprOrderByItem(ExprOrderByItem exprOrderByItem)
        {
            exprOrderByItem.Value.Accept(this);
            if (exprOrderByItem.Descendant)
            {
                this.Builder.Append(" DESC");
            }
            return true;
        }

        public bool VisitExprOffsetFetch(ExprOffsetFetch exprOffsetFetch)
        {
            this.Builder.Append(" OFFSET ");
            exprOffsetFetch.Offset.Accept(this);
            this.Builder.Append(" ROW");

            if (!ReferenceEquals(exprOffsetFetch.Fetch,null))
            {
                this.Builder.Append(" FETCH NEXT ");
                exprOffsetFetch.Fetch.Accept(this);
                this.Builder.Append(" ROW ONLY");
            }

            return true;
        }

        public bool VisitExprOutPutColumnInserted(ExprOutputColumnInserted exprOutputColumnInserted)
        {
            this.Builder.Append("INSERTED.");
            exprOutputColumnInserted.ColumnName.Accept(this);
            return true;
        }

        public bool VisitExprOutPutColumnDeleted(ExprOutputColumnDeleted exprOutputColumnDeleted)
        {
            this.Builder.Append("DELETED.");
            exprOutputColumnDeleted.ColumnName.Accept(this);
            return true;
        }

        public bool VisitExprOutPutColumn(ExprOutputColumn exprOutputColumn)
        {
            exprOutputColumn.Column.Accept(this);
            return true;
        }

        public bool VisitExprOutPutAction(ExprOutputAction exprOutputAction)
        {
            this.Builder.Append("$ACTION");
            if (exprOutputAction.Alias != null)
            {
                this.Builder.Append(' ');
                exprOutputAction.Alias.Accept(this);
            }
            return true;
        }

        public bool VisitExprOutPut(ExprOutput exprOutput)
        {
            this.AssertNotEmptyList(exprOutput.Columns, "Output column list cannot be empty");
            this.AcceptListComaSeparated(exprOutput.Columns);
            return true;
        }

        public bool VisitExprAggregateFunction(ExprAggregateFunction exprAggregateFunction)
        {
            exprAggregateFunction.Name.Accept(this);
            this.Builder.Append('(');
            if (exprAggregateFunction.IsDistinct)
            {
                this.Builder.Append("DISTINCT ");
            }

            exprAggregateFunction.Expression.Accept(this);
            this.Builder.Append(')');

            return true;
        }

        public bool VisitExprScalarFunction(ExprScalarFunction exprScalarFunction)
        {
            exprScalarFunction.Name.Accept(this);
            this.AcceptListComaSeparatedPar('(', exprScalarFunction.Arguments, ')');

            return true;
        }

        public bool VisitExprAggregateAnalyticFunction(ExprAnalyticFunction exprAnalyticFunction)
        {
            exprAnalyticFunction.Name.Accept(this);
            this.Builder.Append('(');
            if (exprAnalyticFunction.Arguments != null)
            {
                this.AssertNotEmptyList(exprAnalyticFunction.Arguments, "Arguments list cannot be empty");
                this.AcceptListComaSeparated(exprAnalyticFunction.Arguments);
            }
            this.Builder.Append(')');
            exprAnalyticFunction.Over.Accept(this);
            return true;
        }

        public bool VisitExprOver(ExprOver exprOver)
        {
            this.Builder.Append("OVER(");

            if (exprOver.Partitions != null)
            {
                this.AssertNotEmptyList(exprOver.Partitions, "Partition list cannot be empty");
                this.Builder.Append("PARTITION BY ");
                this.AcceptListComaSeparated(exprOver.Partitions);
            }

            if (exprOver.OrderBy != null)
            {
                if (exprOver.Partitions != null)
                {
                    this.Builder.Append(' ');
                }
                this.Builder.Append("ORDER BY ");
                exprOver.OrderBy.Accept(this);
            }
            this.Builder.Append(")");
            return true;
        }

        public bool VisitExprCase(ExprCase exprCase)
        {
            this.AssertNotEmptyList(exprCase.Cases, "Cases cannot be empty");

            this.Builder.Append("CASE");
            for (int i = 0; i < exprCase.Cases.Count; i++)
            {
                this.Builder.Append(' ');
                exprCase.Cases[i].Accept(this);
            }
            this.Builder.Append(" ELSE ");
            exprCase.DefaultValue.Accept(this);
            this.Builder.Append(" END");
            return true;
        }

        public bool VisitExprCaseWhenThen(ExprCaseWhenThen exprCaseWhenThen)
        {
            this.Builder.Append("WHEN ");
            exprCaseWhenThen.Condition.Accept(this);
            this.Builder.Append(" THEN ");
            exprCaseWhenThen.Value.Accept(this);

            return true;
        }

        public bool VisitExprColumn(ExprColumn exprColumn)
        {
            if (exprColumn.Source != null)
            {
                exprColumn.Source.Accept(this);
                this.Builder.Append('.');
            }

            exprColumn.ColumnName.Accept(this);

            return true;
        }

        public bool VisitExprTable(ExprTable exprTable)
        {
            exprTable.FullName.Accept(this);
            if (exprTable.Alias != null)
            {
                this.Builder.Append(' ');
                exprTable.Alias.Accept(this);
            }
            return true;
        }

        public bool VisitExprColumnName(ExprColumnName columnName)
        {
            this.AppendName(columnName.Name);
            return true;
        }

        public bool VisitExprTableName(ExprTableName tableName)
        {
            this.AppendName(tableName.Name);
            return true;
        }

        public bool VisitExprTableFullName(ExprTableFullName exprTableFullName)
        {
            exprTableFullName.Schema.Accept(this);
            this.Builder.Append('.');
            exprTableFullName.TableName.Accept(this);
            return true;
        }

        public bool VisitExprAlias(ExprAlias alias)
        {
            this.AppendName(alias.Name);
            return true;
        }

        public bool VisitExprAliasGuid(ExprAliasGuid aliasGuid)
        {
            this.AppendName(this._aliasGenerator.GetAlias(aliasGuid));
            return true;
        }

        public bool VisitExprColumnAlias(ExprColumnAlias exprColumnAlias)
        {
            this.AppendName(exprColumnAlias.Name);
            return true;
        }

        public bool VisitExprAliasedColumn(ExprAliasedColumn exprAliasedColumn)
        {
            exprAliasedColumn.Column.Accept(this);
            if (exprAliasedColumn.Alias != null)
            {
                this.Builder.Append(' ');
                exprAliasedColumn.Alias?.Accept(this);
            }
            return true;
        }

        public bool VisitExprAliasedColumnName(ExprAliasedColumnName exprAliasedColumnName)
        {
            exprAliasedColumnName.Column.Accept(this);
            if (exprAliasedColumnName.Alias != null)
            {
                this.Builder.Append(' ');
                exprAliasedColumnName.Alias.Accept(this);
            }
            return true;
        }

        public bool VisitExprAliasedSelectItem(ExprAliasedSelecting exprAliasedSelecting)
        {
            exprAliasedSelecting.Value.Accept(this);
            this.Builder.Append(' ');
            exprAliasedSelecting.Alias.Accept(this);
            return true;
        }

        public bool VisitExprTableAlias(ExprTableAlias tableAlias)
        {
            tableAlias.Alias.Accept(this);
            return true;
        }

        public bool VisitExprSchemaName(ExprSchemaName schemaName)
        {
            this.AppendName(this.Options.MapSchema(schemaName.Name));
            return true;
        }

        public bool VisitExprFunctionName(ExprFunctionName exprFunctionName)
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

        public bool VisitExprRowValue(ExprRowValue rowValue)
        {
            if (rowValue.Items == null || rowValue.Items.Count < 1)
            {
                throw new SqExpressException("Row value should have at least one column");
            }

            this.AcceptListComaSeparatedPar('(',rowValue.Items, ')');

            return true;
        }

        public bool VisitExprTableValueConstructor(ExprTableValueConstructor tableValueConstructor)
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

                rowValue.Accept(this);
            }

            if (first)
            {
                return this.Rollback(initialLength);
            }

            return true;
        }

        public bool VisitExprDerivedTableQuery(ExprDerivedTableQuery exprDerivedTableQuery)
        {
            this.AcceptPar('(', exprDerivedTableQuery.Query, ')');
            exprDerivedTableQuery.Alias.Accept(this);
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
                    this.AcceptListComaSeparatedPar('(', exprDerivedTableQuery.Columns, ')');
                }
            }

            return true;
        }

        public bool VisitDerivedTableValues(ExprDerivedTableValues derivedTableValues)
        {
            int initialLength = this.Builder.Length;
            if (!this.AcceptPar('(', derivedTableValues.Values, ')'))
            {
                return this.Rollback(initialLength);
            }
            derivedTableValues.Alias.Accept(this);
            derivedTableValues.Columns.AssertNotEmpty("List of columns in a derived table with values literals cannot be empty");
            this.AcceptListComaSeparatedPar('(', derivedTableValues.Columns, ')');

            return true;
        }

        public bool VisitExprColumnSetClause(ExprColumnSetClause columnSetClause)
        {
            columnSetClause.Column.Accept(this);
            this.Builder.Append('=');
            columnSetClause.Value.Accept(this);

            return true;
        }

        public bool VisitExprMerge(ExprMerge merge)
        {
            int init = this.Builder.Length;

            this.Builder.Append("MERGE ");
            merge.TargetTable.Accept(this);
            this.Builder.Append(" USING ");
            if (!merge.Source.Accept(this))
            {
                return this.Rollback(init);
            }
            this.Builder.Append(" ON ");
            merge.On.Accept(this);
            if (merge.WhenMatched != null)
            {
                this.Builder.Append(" WHEN MATCHED");
                merge.WhenMatched.Accept(this);
            }
            if (merge.WhenNotMatchedByTarget != null)
            {
                this.Builder.Append(" WHEN NOT MATCHED");
                merge.WhenNotMatchedByTarget.Accept(this);
            }
            if (merge.WhenNotMatchedBySource != null)
            {
                this.Builder.Append(" WHEN NOT MATCHED BY SOURCE");
                merge.WhenNotMatchedBySource.Accept(this);
            }
            this.Builder.Append(';');

            return true;
        }

        public bool VisitExprMergeOutput(ExprMergeOutput mergeOutput)
        {
            if (this.VisitExprMerge(mergeOutput))
            {
                this.Builder.Length = this.Builder.Length - 1;// ; <-
                this.Builder.Append(" OUTPUT ");
                mergeOutput.Output.Accept(this);
                this.Builder.Append(';');
                return true;
            }
            return false;
        }

        public bool VisitExprMergeMatchedUpdate(ExprMergeMatchedUpdate mergeMatchedUpdate)
        {
            if (mergeMatchedUpdate.And != null)
            {
                this.Builder.Append(" AND ");
                mergeMatchedUpdate.And.Accept(this);
            }

            this.AssertNotEmptyList(mergeMatchedUpdate.Set, "Set Clause cannot be empty");

            this.Builder.Append(" THEN UPDATE SET ");

            this.AcceptListComaSeparated(mergeMatchedUpdate.Set);

            return true;
        }

        public bool VisitExprMergeMatchedDelete(ExprMergeMatchedDelete mergeMatchedDelete)
        {
            if (mergeMatchedDelete.And != null)
            {
                this.Builder.Append(" AND ");
                mergeMatchedDelete.And.Accept(this);
            }

            this.Builder.Append(" THEN  DELETE");

            return true;
        }

        public bool VisitExprExprMergeNotMatchedInsert(ExprExprMergeNotMatchedInsert exprMergeNotMatchedInsert)
        {
            if (exprMergeNotMatchedInsert.And != null)
            {
                this.Builder.Append(" AND ");
                exprMergeNotMatchedInsert.And.Accept(this);
            }

            this.AssertNotEmptyList(exprMergeNotMatchedInsert.Values, "Values cannot be empty");

            if (exprMergeNotMatchedInsert.Columns.Count > 0 &&
                exprMergeNotMatchedInsert.Columns.Count != exprMergeNotMatchedInsert.Values.Count)
            {
                throw new SqExpressException("Columns and values numbers do not match");
            }

            this.Builder.Append(" THEN INSERT");
            this.AcceptListComaSeparatedPar('(', exprMergeNotMatchedInsert.Columns, ')');
            this.Builder.Append(" VALUES");
            this.AcceptListComaSeparatedPar('(', exprMergeNotMatchedInsert.Values, ')');

            return true;
        }

        public bool VisitExprExprMergeNotMatchedInsertDefault(
            ExprExprMergeNotMatchedInsertDefault exprExprMergeNotMatchedInsertDefault)
        {
            if (exprExprMergeNotMatchedInsertDefault.And != null)
            {
                this.Builder.Append(" AND ");
                exprExprMergeNotMatchedInsertDefault.And.Accept(this);
            }

            this.Builder.Append(" THEN INSERT DEFAULT VALUES");

            return true;
        }

        public bool VisitExprInsert(ExprInsert exprInsert)
        {
            this.GenericInsert(exprInsert, null, null);
            return true;
        }

        protected void GenericInsert(ExprInsert exprInsert, Action? middleHandler, Action? endHandler)
        {
            this.Builder.Append("INSERT INTO ");
            exprInsert.Target.Accept(this);
            if (exprInsert.TargetColumns != null)
            {
                this.AssertNotEmptyList(exprInsert.TargetColumns, "Insert column list cannot be empty");
                this.AcceptListComaSeparatedPar('(', exprInsert.TargetColumns, ')');
            }

            if (middleHandler != null)
            {
                this.Builder.Append(' ');
                middleHandler();
            }
            this.Builder.Append(' ');
            exprInsert.Source.Accept(this);
            if (endHandler != null)
            {
                this.Builder.Append(' ');
                endHandler();
            }
        }

        public abstract bool VisitExprInsertOutput(ExprInsertOutput exprInsertOutput);

        public bool VisitExprInsertValues(ExprInsertValues exprInsertValues)
        {
            exprInsertValues.Values.Accept(this);
            return true;
        }

        public bool VisitExprInsertQuery(ExprInsertQuery exprInsertQuery)
        {
            exprInsertQuery.Query.Accept(this);
            return true;
        }

        public abstract bool VisitExprUpdate(ExprUpdate exprUpdate);

        public abstract bool VisitExprDelete(ExprDelete exprDelete);

        public abstract bool VisitExprDeleteOutput(ExprDeleteOutput exprDeleteOutput);

        public bool VisitExprCast(ExprCast exprCast)
        {
            this.Builder.Append("CAST(");
            exprCast.Expression.Accept(this);
            this.Builder.Append(" AS ");
            exprCast.SqlType.Accept(this);
            this.Builder.Append(')');
            return true;
        }

        public abstract bool VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean);

        public abstract bool VisitExprTypeByte(ExprTypeByte exprTypeByte);

        public abstract bool VisitExprTypeInt16(ExprTypeInt16 exprTypeInt16);

        public abstract bool VisitExprTypeInt32(ExprTypeInt32 exprTypeInt32);

        public abstract bool VisitExprTypeInt64(ExprTypeInt64 exprTypeInt64);

        public abstract bool VisitExprTypeDecimal(ExprTypeDecimal exprTypeDecimal);

        public abstract bool VisitExprTypeDouble(ExprTypeDouble exprTypeDouble);

        public abstract bool VisitExprTypeDateTime(ExprTypeDateTime exprTypeDateTime);

        public abstract bool VisitExprTypeGuid(ExprTypeGuid exprTypeGuid);

        public abstract bool VisitExprTypeString(ExprTypeString exprTypeString);

        public abstract bool VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull);

        public bool VisitExprFuncCoalesce(ExprFuncCoalesce exprFuncCoalesce)
        {
            this.Builder.Append("COALESCE(");
            exprFuncCoalesce.Test.Accept(this);
            this.Builder.Append(',');
            this.AssertNotEmptyList(exprFuncCoalesce.Alts, "Alt argument list cannot be empty in 'COALESCE' function call");
            this.AcceptListComaSeparated(exprFuncCoalesce.Alts);
            this.Builder.Append(')');
            return true;
        }

        public abstract bool VisitExprFuncGetDate(ExprGetDate exprGetDate);

        public abstract bool VisitExprFuncGetUtcDate(ExprGetUtcDate exprGetUtcDate);

        public abstract void AppendName(string name);

        protected void AppendNull()
        {
            this.Builder.Append("NULL");
        }

        protected bool AcceptPar(char start, IExpr list, char end)
        {
            int initial = this.Builder.Length;
            this.Builder.Append(start);
            if (!list.Accept(this))
            {
                return this.Rollback(initial);
            }
            this.Builder.Append(end);
            return true;
        }

        internal bool AcceptListComaSeparatedPar(char start, IReadOnlyList<IExpr> list, char end)
        {
            int initial = this.Builder.Length;
            this.Builder.Append(start);
            if (!this.AcceptListComaSeparated(list))
            {
                return this.Rollback(initial);
            }
            this.Builder.Append(end);

            return true;
        }

        protected bool AcceptListComaSeparated(IReadOnlyList<IExpr> list)
        {
            int initial = this.Builder.Length;

            for (int i = 0; i < list.Count; i++)
            {
                if (i != 0)
                {
                    this.Builder.Append(',');
                }

                if (!list[i].Accept(this))
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