﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Internal;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Output;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;
using SqExpress.SyntaxTreeOperations;
using SqExpress.SyntaxTreeOperations.Internal;
using SqExpress.Utils;

namespace SqExpress.SqlExport.Internal
{
    internal abstract class SqlBuilderBase: IExprVisitorInternal<bool, IExpr?>
    {
        protected readonly SqlBuilderOptions Options;

        protected readonly StringBuilder Builder;

        private readonly SqlAliasGenerator _aliasGenerator;

        private readonly bool _dismissCteInject;

        private IStatementVisitor? _statementBuilder;

        private int? _cteSlot;

        private Dictionary<string, ExprCte>? _uniqueCteCheck;

        protected SqlBuilderBase(SqlBuilderOptions? options, StringBuilder? externalBuilder, SqlAliasGenerator aliasGenerator, bool dismissCteInject)
        {
            this.Options = options ?? SqlBuilderOptions.Default;
            this.Builder = externalBuilder ?? new StringBuilder();
            this._dismissCteInject = dismissCteInject;
            this._aliasGenerator = aliasGenerator;
        }

        protected abstract SqlBuilderBase CreateInstance(SqlAliasGenerator aliasGenerator, bool dismissCteInject);

        //Boolean Expressions

        public bool VisitExprBooleanAnd(ExprBooleanAnd expr, IExpr? parent)
        {
            if (expr.Left is ExprBooleanOr)
            {
                this.AcceptPar('(', expr.Left, ')', expr);
                this.Builder.Append("AND");
            }
            else
            {
                expr.Left.Accept(this, expr);
                this.Builder.Append(" AND");
            }

            if (expr.Right is ExprBooleanOr)
            {
                this.AcceptPar('(', expr.Right, ')', expr);
            }
            else
            {
                this.Builder.Append(' ');
                expr.Right.Accept(this, expr);
            }

            return true;
        }

        public bool VisitExprBooleanOr(ExprBooleanOr expr, IExpr? parent)
        {
            expr.Left.Accept(this, expr);
            this.Builder.Append(" OR ");
            expr.Right.Accept(this, expr);

            return true;
        }

        public bool VisitExprBooleanNot(ExprBooleanNot expr, IExpr? parent)
        {
            this.Builder.Append("NOT");
            if (expr.Expr is ExprPredicate)
            {
                this.Builder.Append(' ');
                expr.Expr.Accept(this, expr);
            }
            else
            {
                this.AcceptPar('(', expr.Expr, ')', expr);
            }

            return true;
        }

        //Boolean Predicates

        public bool VisitExprBooleanNotEq(ExprBooleanNotEq exprBooleanNotEq, IExpr? parent)
        {
            exprBooleanNotEq.Left.Accept(this, exprBooleanNotEq);
            this.Builder.Append("!=");
            exprBooleanNotEq.Right.Accept(this, exprBooleanNotEq);

            return true;
        }

        public bool VisitExprBooleanEq(ExprBooleanEq exprBooleanEq, IExpr? parent)
        {
            exprBooleanEq.Left.Accept(this, exprBooleanEq);
            this.Builder.Append('=');
            exprBooleanEq.Right.Accept(this, exprBooleanEq);

            return true;
        }

        public bool VisitExprBooleanGt(ExprBooleanGt booleanGt, IExpr? parent)
        {
            booleanGt.Left.Accept(this, booleanGt);
            this.Builder.Append('>');
            booleanGt.Right.Accept(this, booleanGt);

            return true;
        }

        public bool VisitExprBooleanGtEq(ExprBooleanGtEq booleanGtEq, IExpr? parent)
        {
            booleanGtEq.Left.Accept(this, booleanGtEq);
            this.Builder.Append(">=");
            booleanGtEq.Right.Accept(this, booleanGtEq);

            return true;
        }

        public bool VisitExprBooleanLt(ExprBooleanLt booleanLt, IExpr? parent)
        {
            booleanLt.Left.Accept(this, booleanLt);
            this.Builder.Append('<');
            booleanLt.Right.Accept(this, booleanLt);

            return true;
        }

        public bool VisitExprBooleanLtEq(ExprBooleanLtEq booleanLtEq, IExpr? parent)
        {
            booleanLtEq.Left.Accept(this, booleanLtEq);
            this.Builder.Append("<=");
            booleanLtEq.Right.Accept(this, booleanLtEq);

            return true;
        }

        //Boolean Predicates - Others

        public bool VisitExprInSubQuery(ExprInSubQuery exprInSubQuery, IExpr? parent)
        {
            exprInSubQuery.TestExpression.Accept(this, exprInSubQuery);
            this.Builder.Append(" IN");
            this.AcceptPar('(', exprInSubQuery.SubQuery, ')', exprInSubQuery);
            return true;
        }

        public bool VisitExprInValues(ExprInValues exprInValues, IExpr? parent)
        {
            exprInValues.TestExpression.Accept(this, exprInValues);
            this.AssertNotEmptyList(exprInValues.Items, "'IN' Predicate cannot have an empty list of expressions");
            this.Builder.Append(" IN");
            this.AcceptListComaSeparatedPar('(', exprInValues.Items, ')', exprInValues);
            return true;
        }

        public bool VisitExprExists(ExprExists exprExists, IExpr? parent)
        {
            this.Builder.Append("EXISTS");
            this.AcceptPar('(', exprExists.SubQuery, ')', exprExists);
            return true;
        }

        public bool VisitExprIsNull(ExprIsNull exprIsNull, IExpr? parent)
        {
            exprIsNull.Test.Accept(this, exprIsNull);
            this.Builder.Append(" IS");
            if (exprIsNull.Not)
            {
                this.Builder.Append(" NOT");
            }
            this.Builder.Append(" NULL");
            return true;
        }

        public bool VisitExprLike(ExprLike exprLike, IExpr? parent)
        {
            exprLike.Test.Accept(this, exprLike);
            this.Builder.Append(" LIKE ");
            exprLike.Pattern.Accept(this, exprLike);
            return true;
        }

        //Value

        public bool VisitExprInt32Literal(ExprInt32Literal exprInt32Literal, IExpr? parent)
        {
            if (exprInt32Literal.Value == null)
            {
                this.AppendNull();
                return true;
            }

            this.Builder.Append(exprInt32Literal.Value.Value);

            return true;
        }

        public abstract bool VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral, IExpr? parent);

        public bool VisitExprStringLiteral(ExprStringLiteral stringLiteral, IExpr? parent)
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
                this.EscapeStringLiteral(this.Builder, stringLiteral.Value);
            }

            this.Builder.Append('\'');
            return true;
        }

        protected abstract void EscapeStringLiteral(StringBuilder builder, string literal);

        public bool VisitExprDateTimeLiteral(ExprDateTimeLiteral dateTimeLiteral, IExpr? parent)
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

        public abstract bool VisitExprDateTimeOffsetLiteral(ExprDateTimeOffsetLiteral dateTimeLiteral, IExpr? arg);

        protected bool VisitExprDateTimeOffsetLiteralCommon(ExprDateTimeOffsetLiteral dateTimeLiteral, IExpr? arg)
        {
            if (!dateTimeLiteral.Value.HasValue)
            {
                this.AppendNull();
            }
            else
            {
                this.Builder.Append('\'');
                this.Builder.Append(dateTimeLiteral.Value.Value.ToString("O"));
                this.Builder.Append('\'');
            }
            return true;
        }

        public abstract bool VisitExprBoolLiteral(ExprBoolLiteral boolLiteral, IExpr? parent);

        public bool VisitExprInt64Literal(ExprInt64Literal int64Literal, IExpr? parent)
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

        public bool VisitExprByteLiteral(ExprByteLiteral byteLiteral, IExpr? parent)
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

        public bool VisitExprInt16Literal(ExprInt16Literal int16Literal, IExpr? parent)
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

        public bool VisitExprDecimalLiteral(ExprDecimalLiteral decimalLiteral, IExpr? parent)
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

        public bool VisitExprDoubleLiteral(ExprDoubleLiteral doubleLiteral, IExpr? parent)
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


        public bool VisitExprByteArrayLiteral(ExprByteArrayLiteral byteArrayLiteral, IExpr? parent)
        {
            if (byteArrayLiteral.Value == null || byteArrayLiteral.Value.Count < 1)
            {
                this.AppendNull();
            }
            else
            {
                this.AppendByteArrayLiteralPrefix();
                for (int i = 0; i < byteArrayLiteral.Value.Count; i++)
                {
                    this.Builder.AppendFormat("{0:x2}", byteArrayLiteral.Value[i]);
                }
                this.AppendByteArrayLiteralSuffix();
            }

            return true;
        }

        protected abstract void AppendByteArrayLiteralPrefix();

        protected abstract void AppendByteArrayLiteralSuffix();

        public bool VisitExprNull(ExprNull exprNull, IExpr? parent)
        {
            this.Builder.Append("NULL");
            return true;
        }

        public bool VisitExprDefault(ExprDefault exprDefault, IExpr? parent)
        {
            this.Builder.Append("DEFAULT");
            return true;
        }

        public bool VisitExprUnsafeValue(ExprUnsafeValue exprUnsafeValue, IExpr? parent)
        {
            this.Builder.Append(exprUnsafeValue.UnsafeValue);
            return true;
        }

        public bool VisitExprValueQuery(ExprValueQuery exprValueQuery, IExpr? parent)
        {
            this.AcceptPar('(', exprValueQuery.Query, ')', exprValueQuery);
            return true;
        }

        //Arithmetic Expressions

        public bool VisitExprSum(ExprSum exprSum, IExpr? parent)
        {
            exprSum.Left.Accept(this, exprSum);
            this.Builder.Append('+');
            exprSum.Right.Accept(this, exprSum);
            return true;
        }

        public bool VisitExprSub(ExprSub exprSub, IExpr? parent)
        {
            exprSub.Left.Accept(this, exprSub);
            this.Builder.Append('-');
            this.CheckPlusMinusParenthesizes(exprSub.Right, exprSub);
            return true;
        }

        public bool VisitExprMul(ExprMul exprMul, IExpr? parent)
        {
            this.CheckPlusMinusParenthesizes(exprMul.Left, exprMul);
            this.Builder.Append('*');
            this.CheckPlusMinusParenthesizes(exprMul.Right, exprMul);
            return true;
        }

        public bool VisitExprDiv(ExprDiv exprDiv, IExpr? parent)
        {
            this.CheckPlusMinusParenthesizes(exprDiv.Left, exprDiv);
            this.Builder.Append('/');
            this.CheckPlusMinusParenthesizes(exprDiv.Right, exprDiv);
            return true;
        }

        public bool VisitExprModulo(ExprModulo exprModulo, IExpr? arg)
        {
            this.CheckPlusMinusParenthesizes(exprModulo.Left, exprModulo);
            this.Builder.Append('%');
            this.CheckPlusMinusParenthesizes(exprModulo.Right, exprModulo);
            return true;
        }

        public abstract bool VisitExprStringConcat(ExprStringConcat exprStringConcat, IExpr? parent);

        private void CheckPlusMinusParenthesizes(ExprValue exp, IExpr? parent)
        {
            if (exp is ExprSum || exp is ExprSub)
            {
                this.Builder.Append('(');
                exp.Accept(this, parent);
                this.Builder.Append(')');
            }
            else
            {
                exp.Accept(this, parent);
            }
        }

        public bool VisitExprBitwiseNot(ExprBitwiseNot exprBitwiseNot, IExpr? arg)
        {
            this.Builder.Append('~');
            this.CheckBitParenthesizes(exprBitwiseNot.Value, exprBitwiseNot);
            return true;

        }

        public bool VisitExprBitwiseAnd(ExprBitwiseAnd exprBitwiseAnd, IExpr? arg)
        {
            this.CheckBitParenthesizes(exprBitwiseAnd.Left, exprBitwiseAnd);
            this.Builder.Append('&');
            this.CheckBitParenthesizes(exprBitwiseAnd.Right, exprBitwiseAnd);
            return true;
        }

        public bool VisitExprBitwiseXor(ExprBitwiseXor exprBitwiseXor, IExpr? arg)
        {
            this.CheckBitParenthesizes(exprBitwiseXor.Left, exprBitwiseXor);
            this.Builder.Append('^');
            this.CheckBitParenthesizes(exprBitwiseXor.Right, exprBitwiseXor);
            return true;
        }

        public bool VisitExprBitwiseOr(ExprBitwiseOr exprBitwiseOr, IExpr? arg)
        {
            CheckBitParenthesizes(exprBitwiseOr.Left, exprBitwiseOr);
            this.Builder.Append('|');
            CheckBitParenthesizes(exprBitwiseOr.Right, exprBitwiseOr);
            return true;
        }

        private void CheckBitParenthesizes(ExprValue exp, ExprValue parent)
        {
            if ((exp is ExprBitwise || exp is ExprArithmetic) && !(exp is ExprBitwiseNot)  && parent.GetType() != exp.GetType())
            {
                this.Builder.Append('(');
                exp.Accept(this, parent);
                this.Builder.Append(')');
            }
            else
            {
                exp.Accept(this, parent);
            }
        }

        //Select

        protected abstract void AppendSelectTop(ExprValue top, IExpr? parent);

        protected abstract void AppendSelectLimit(ExprValue top, IExpr? parent);

        public bool VisitExprQuerySpecification(ExprQuerySpecification exprQuerySpecification, IExpr? parent)
        {
            this.AddCteSlot(parent);

            this.AcceptInlineCte(exprQuerySpecification);

            this.Builder.Append("SELECT ");
            if (exprQuerySpecification.Distinct)
            {
                this.Builder.Append("DISTINCT ");
            }
            if (!ReferenceEquals(exprQuerySpecification.Top, null))
            {
                this.AppendSelectTop(exprQuerySpecification.Top, exprQuerySpecification);
            }

            this.AcceptListComaSeparated(exprQuerySpecification.SelectList, exprQuerySpecification);

            if (exprQuerySpecification.From != null)
            {
                this.Builder.Append(" FROM ");
                exprQuerySpecification.From.Accept(this, exprQuerySpecification);
            }

            if (exprQuerySpecification.Where != null)
            {
                this.Builder.Append(" WHERE ");
                exprQuerySpecification.Where.Accept(this, exprQuerySpecification);
            }

            if (exprQuerySpecification.GroupBy != null)
            {
                this.Builder.Append(" GROUP BY ");
                this.AcceptListComaSeparated(exprQuerySpecification.GroupBy, exprQuerySpecification);
            }

            if (!ReferenceEquals(exprQuerySpecification.Top, null) && !(parent is ExprSelect) && !(parent is ExprSelectOffsetFetch))
            {
                //For non T-SQL (PostgresSQL, My SQL)
                this.AppendSelectLimit(exprQuerySpecification.Top, exprQuerySpecification);
            }

            return true;
        }

        public bool VisitExprJoinedTable(ExprJoinedTable joinedTable, IExpr? parent)
        {
            joinedTable.Left.Accept(this, joinedTable);
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
            joinedTable.Right.Accept(this, joinedTable);
            this.Builder.Append(" ON ");
            joinedTable.SearchCondition.Accept(this, joinedTable);

            return true;
        }

        public bool VisitExprCrossedTable(ExprCrossedTable exprCrossedTable, IExpr? parent)
        {
            exprCrossedTable.Left.Accept(this, exprCrossedTable);
            this.Builder.Append(" CROSS JOIN ");
            exprCrossedTable.Right.Accept(this, exprCrossedTable);
            return true;
        }

        public abstract bool VisitExprLateralCrossedTable(ExprLateralCrossedTable exprCrossedTable, IExpr? parent);

        public bool VisitExprQueryExpression(ExprQueryExpression exprQueryExpression, IExpr? parent)
        {
            this.AddCteSlot(parent);

            this.AcceptInlineCte(exprQueryExpression);

            if (ForceParenthesesForQueryExpressionPart(exprQueryExpression.Left))
            {
                this.AcceptPar('(', exprQueryExpression.Left, ')', exprQueryExpression);
            }
            else
            {
                exprQueryExpression.Left.Accept(this, exprQueryExpression);
            }

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

            if (exprQueryExpression.Right is ExprQueryExpression || ForceParenthesesForQueryExpressionPart(exprQueryExpression.Right))
            {
                this.AcceptPar('(', exprQueryExpression.Right, ')', exprQueryExpression);
            }
            else
            {
                exprQueryExpression.Right.Accept(this, exprQueryExpression);
            }

            return true;
        }

        protected abstract bool ForceParenthesesForQueryExpressionPart(IExprSubQuery subQuery);

        public bool VisitExprSelect(ExprSelect exprSelect, IExpr? parent)
        {
            this.AddCteSlot(parent);

            exprSelect.SelectQuery.Accept(this, exprSelect);
            this.Builder.Append(" ORDER BY ");
            exprSelect.OrderBy.Accept(this, exprSelect);

            if (exprSelect.SelectQuery is ExprQuerySpecification specification)
            {
                if (!ReferenceEquals(specification.Top, null))
                {
                    this.AppendSelectLimit(specification.Top, exprSelect);
                }
            }

            return true;
        }

        public bool VisitExprSelectOffsetFetch(ExprSelectOffsetFetch exprSelectOffsetFetch, IExpr? parent)
        {
            this.AddCteSlot(parent);

            exprSelectOffsetFetch.SelectQuery.Accept(this, exprSelectOffsetFetch);
            this.Builder.Append(" ORDER BY ");
            exprSelectOffsetFetch.OrderBy.Accept(this, exprSelectOffsetFetch);
            return true;
        }

        public bool VisitExprOrderBy(ExprOrderBy exprOrderBy, IExpr? parent)
        {
            this.AcceptListComaSeparated(exprOrderBy.OrderList, exprOrderBy);
            return true;
        }

        public bool VisitExprOrderByOffsetFetch(ExprOrderByOffsetFetch exprOrderByOffsetFetch, IExpr? parent)
        {
            this.AcceptListComaSeparated(exprOrderByOffsetFetch.OrderList, exprOrderByOffsetFetch);

            if (parent is ExprSelectOffsetFetch exprSelectOffsetFetch && exprSelectOffsetFetch.SelectQuery is ExprQuerySpecification specification)
            {
                if (!ReferenceEquals(specification.Top, null))
                {
                    if (!ReferenceEquals(exprSelectOffsetFetch.OrderBy.OffsetFetch.Fetch, null) && exprSelectOffsetFetch.OrderBy.OffsetFetch.Fetch.Value.HasValue)
                    {
                        throw new SqExpressException("Query with \"FETCH\" cannot be limited");
                    }

                    this.AppendSelectLimit(specification.Top, exprSelectOffsetFetch);
                }
            }

            exprOrderByOffsetFetch.OffsetFetch.Accept(this, exprOrderByOffsetFetch);
            return true;
        }

        public bool VisitExprOrderByItem(ExprOrderByItem exprOrderByItem, IExpr? parent)
        {
            exprOrderByItem.Value.Accept(this, exprOrderByItem);
            if (exprOrderByItem.Descendant)
            {
                this.Builder.Append(" DESC");
            }
            return true;
        }

        public abstract bool VisitExprOffsetFetch(ExprOffsetFetch exprOffsetFetch, IExpr? parent);

        protected bool VisitExprOffsetFetchCommon(ExprOffsetFetch exprOffsetFetch, IExpr? parent)
        {
            this.Builder.Append(" OFFSET ");
            exprOffsetFetch.Offset.Accept(this, exprOffsetFetch);
            this.Builder.Append(" ROW");

            if (!ReferenceEquals(exprOffsetFetch.Fetch,null))
            {
                this.Builder.Append(" FETCH NEXT ");
                exprOffsetFetch.Fetch.Accept(this, exprOffsetFetch);
                this.Builder.Append(" ROW ONLY");
            }

            return true;
        }

        //Select Output

        public bool VisitExprOutputColumnInserted(ExprOutputColumnInserted exprOutputColumnInserted, IExpr? parent)
        {
            this.Builder.Append("INSERTED.");
            exprOutputColumnInserted.ColumnName.Accept(this, exprOutputColumnInserted);
            return true;
        }

        public bool VisitExprOutputColumnDeleted(ExprOutputColumnDeleted exprOutputColumnDeleted, IExpr? parent)
        {
            this.Builder.Append("DELETED.");
            exprOutputColumnDeleted.ColumnName.Accept(this, exprOutputColumnDeleted);
            return true;
        }

        public bool VisitExprOutputColumn(ExprOutputColumn exprOutputColumn, IExpr? parent)
        {
            exprOutputColumn.Column.Accept(this, exprOutputColumn);
            return true;
        }

        public bool VisitExprOutputAction(ExprOutputAction exprOutputAction, IExpr? parent)
        {
            this.Builder.Append("$ACTION");
            if (exprOutputAction.Alias != null)
            {
                this.Builder.Append(' ');
                exprOutputAction.Alias.Accept(this, exprOutputAction);
            }
            return true;
        }

        public bool VisitExprOutput(ExprOutput exprOutput, IExpr? parent)
        {
            this.AssertNotEmptyList(exprOutput.Columns, "Output column list cannot be empty");
            this.AcceptListComaSeparated(exprOutput.Columns, exprOutput);
            return true;
        }

        //Functions

        public bool VisitExprAggregateFunction(ExprAggregateFunction exprAggregateFunction, IExpr? parent)
        {
            exprAggregateFunction.Name.Accept(this, exprAggregateFunction);
            this.Builder.Append('(');
            if (exprAggregateFunction.IsDistinct)
            {
                this.Builder.Append("DISTINCT ");
            }

            exprAggregateFunction.Expression.Accept(this, exprAggregateFunction);
            this.Builder.Append(')');

            return true;
        }

        public bool VisitExprAggregateOverFunction(ExprAggregateOverFunction exprAggregateFunction, IExpr? arg)
        {
            exprAggregateFunction.Function.Accept(this, exprAggregateFunction);
            exprAggregateFunction.Over.Accept(this, exprAggregateFunction);

            return true;
        }

        public bool VisitExprScalarFunction(ExprScalarFunction exprScalarFunction, IExpr? parent)
        {
            if (exprScalarFunction.Schema != null)
            {
                if (exprScalarFunction.Schema.Accept(this, exprScalarFunction))
                {
                    this.Builder.Append('.');
                }
            }

            exprScalarFunction.Name.Accept(this, exprScalarFunction);

            if (exprScalarFunction.Arguments != null)
            {
                this.AssertNotEmptyList(exprScalarFunction.Arguments, "Argument list cannot be empty");
                this.AcceptListComaSeparatedPar('(', exprScalarFunction.Arguments, ')', exprScalarFunction);
            }
            else
            {
                this.Builder.Append('(');
                this.Builder.Append(')');
            }
            
            return true;
        }


        public bool VisitExprTableFunction(ExprTableFunction exprTableFunction, IExpr? arg)
        {
            if (exprTableFunction.Schema != null)
            {
                if (exprTableFunction.Schema.Accept(this, exprTableFunction))
                {
                    this.Builder.Append('.');
                }
            }

            exprTableFunction.Name.Accept(this, exprTableFunction);

            if (exprTableFunction.Arguments != null)
            {
                this.AssertNotEmptyList(exprTableFunction.Arguments, "Argument list cannot be empty");
                this.AcceptListComaSeparatedPar('(', exprTableFunction.Arguments, ')', exprTableFunction);
            }
            else
            {
                this.Builder.Append('(');
                this.Builder.Append(')');
            }

            return true;
        }

        public bool VisitExprAliasedTableFunction(ExprAliasedTableFunction exprTableFunction, IExpr? arg)
        {
            exprTableFunction.Function.Accept(this, exprTableFunction);
            this.Builder.Append(' ');
            exprTableFunction.Alias.Accept(this, exprTableFunction);

            return true;
        }

        public bool VisitExprAnalyticFunction(ExprAnalyticFunction exprAnalyticFunction, IExpr? parent)
        {
            exprAnalyticFunction.Name.Accept(this, exprAnalyticFunction);
            this.Builder.Append('(');
            if (exprAnalyticFunction.Arguments != null)
            {
                this.AssertNotEmptyList(exprAnalyticFunction.Arguments, "Arguments list cannot be empty");
                this.AcceptListComaSeparated(exprAnalyticFunction.Arguments, exprAnalyticFunction);
            }
            this.Builder.Append(')');
            exprAnalyticFunction.Over.Accept(this, exprAnalyticFunction);
            return true;
        }

        public bool VisitExprOver(ExprOver exprOver, IExpr? parent)
        {
            this.Builder.Append("OVER(");

            if (exprOver.Partitions != null)
            {
                this.AssertNotEmptyList(exprOver.Partitions, "Partition list cannot be empty");
                this.Builder.Append("PARTITION BY ");
                this.AcceptListComaSeparated(exprOver.Partitions, exprOver);
            }

            if (exprOver.OrderBy != null)
            {
                if (exprOver.Partitions != null)
                {
                    this.Builder.Append(' ');
                }
                this.Builder.Append("ORDER BY ");
                exprOver.OrderBy.Accept(this, exprOver);
            }

            if (exprOver.FrameClause != null)
            {
                this.Builder.Append(' ');
                exprOver.FrameClause.Accept(this, exprOver);
            }

            this.Builder.Append(")");
            return true;
        }

        public bool VisitExprFrameClause(ExprFrameClause exprFrameClause, IExpr? arg)
        {
            this.Builder.Append("ROWS ");
            
            if (exprFrameClause.End != null)
            {
                this.Builder.Append("BETWEEN ");
                exprFrameClause.Start.Accept(this, exprFrameClause);
                this.Builder.Append(" AND ");
                exprFrameClause.End.Accept(this, exprFrameClause);
            }
            else
            {
                exprFrameClause.Start.Accept(this, exprFrameClause);
            }

            return true;
        }

        public bool VisitExprValueFrameBorder(ExprValueFrameBorder exprValueFrameBorder, IExpr? arg)
        {
            exprValueFrameBorder.Value.Accept(this, exprValueFrameBorder);
            switch (exprValueFrameBorder.FrameBorderDirection)
            {
                case FrameBorderDirection.Preceding:
                    this.Builder.Append(" PRECEDING");
                    break;
                case FrameBorderDirection.Following:
                    this.Builder.Append(" FOLLOWING");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return true;
        }

        public bool VisitExprCurrentRowFrameBorder(ExprCurrentRowFrameBorder exprCurrentRowFrameBorder, IExpr? arg)
        {
            this.Builder.Append("CURRENT ROW");
            return true;
        }

        public bool VisitExprUnboundedFrameBorder(ExprUnboundedFrameBorder exprUnboundedFrameBorder, IExpr? arg)
        {
            switch (exprUnboundedFrameBorder.FrameBorderDirection)
            {
                case FrameBorderDirection.Preceding:
                    this.Builder.Append("UNBOUNDED PRECEDING");
                    break;
                case FrameBorderDirection.Following:
                    this.Builder.Append("UNBOUNDED FOLLOWING");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        public bool VisitExprCase(ExprCase exprCase, IExpr? parent)
        {
            this.AssertNotEmptyList(exprCase.Cases, "Cases cannot be empty");

            this.Builder.Append("CASE");
            for (int i = 0; i < exprCase.Cases.Count; i++)
            {
                this.Builder.Append(' ');
                exprCase.Cases[i].Accept(this, exprCase);
            }
            this.Builder.Append(" ELSE ");
            exprCase.DefaultValue.Accept(this, exprCase);
            this.Builder.Append(" END");
            return true;
        }

        public bool VisitExprCaseWhenThen(ExprCaseWhenThen exprCaseWhenThen, IExpr? parent)
        {
            this.Builder.Append("WHEN ");
            exprCaseWhenThen.Condition.Accept(this, exprCaseWhenThen);
            this.Builder.Append(" THEN ");
            exprCaseWhenThen.Value.Accept(this, exprCaseWhenThen);

            return true;
        }

        //Functions - Known
        public bool VisitExprFuncCoalesce(ExprFuncCoalesce exprFuncCoalesce, IExpr? parent)
        {
            this.Builder.Append("COALESCE(");
            exprFuncCoalesce.Test.Accept(this, exprFuncCoalesce);
            this.Builder.Append(',');
            this.AssertNotEmptyList(exprFuncCoalesce.Alts, "Alt argument list cannot be empty in 'COALESCE' function call");
            this.AcceptListComaSeparated(exprFuncCoalesce.Alts, exprFuncCoalesce);
            this.Builder.Append(')');
            return true;
        }

        public abstract bool VisitExprGetDate(ExprGetDate exprGetDate, IExpr? parent);

        public abstract bool VisitExprGetUtcDate(ExprGetUtcDate exprGetUtcDate, IExpr? parent);

        public abstract bool VisitExprDateAdd(ExprDateAdd exprDateAdd, IExpr? arg);

        public abstract bool VisitExprDateDiff(ExprDateDiff exprDateDiff, IExpr? arg);

        //Meta

        public bool VisitExprColumn(ExprColumn exprColumn, IExpr? parent)
        {
            if (exprColumn.Source != null)
            {
                exprColumn.Source.Accept(this, exprColumn);
                this.Builder.Append('.');
            }

            exprColumn.ColumnName.Accept(this, exprColumn);

            return true;
        }

        public bool VisitExprTable(ExprTable exprTable, IExpr? parent)
        {
            exprTable.FullName.Accept(this, exprTable);
            if (exprTable.Alias != null)
            {
                this.Builder.Append(' ');
                exprTable.Alias.Accept(this, exprTable);
            }
            return true;
        }

        public bool VisitExprAllColumns(ExprAllColumns exprAllColumns, IExpr? parent)
        {
            if (exprAllColumns.Source != null)
            {
                exprAllColumns.Source.Accept(this, exprAllColumns);
                this.Builder.Append('.');
            }

            this.Builder.Append('*');

            return true;
        }

        public abstract bool VisitExprColumnName(ExprColumnName columnName, IExpr? parent);

        protected bool VisitExprColumnNameCommon(ExprColumnName columnName)
        {
            this.AppendName(columnName.Name);
            return true;
        }

        public bool VisitExprTableName(ExprTableName tableName, IExpr? parent)
        {
            this.AppendName(tableName.Name);
            return true;
        }

        public abstract bool VisitExprTableFullName(ExprTableFullName exprTableFullName, IExpr? parent);

        protected bool VisitExprTableFullNameCommon(ExprTableFullName exprTableFullName, IExpr? parent)
        {
            if (exprTableFullName.DbSchema != null)
            {
                if (exprTableFullName.DbSchema.Accept(this, exprTableFullName))
                {
                    this.Builder.Append('.');
                }
            }
            exprTableFullName.TableName.Accept(this, exprTableFullName);
            return true;
        }

        public bool VisitExprAlias(ExprAlias alias, IExpr? parent)
        {
            this.AppendName(alias.Name);
            return true;
        }

        public bool VisitExprAliasGuid(ExprAliasGuid aliasGuid, IExpr? parent)
        {
            this.AppendName(this._aliasGenerator.GetAlias(aliasGuid));
            return true;
        }

        public bool VisitExprColumnAlias(ExprColumnAlias exprColumnAlias, IExpr? parent)
        {
            this.AppendName(exprColumnAlias.Name);
            return true;
        }

        public bool VisitExprAliasedColumn(ExprAliasedColumn exprAliasedColumn, IExpr? parent)
        {
            exprAliasedColumn.Column.Accept(this, exprAliasedColumn);
            if (exprAliasedColumn.Alias != null)
            {
                this.Builder.Append(' ');
                exprAliasedColumn.Alias?.Accept(this, exprAliasedColumn);
            }
            return true;
        }

        public bool VisitExprAliasedColumnName(ExprAliasedColumnName exprAliasedColumnName, IExpr? parent)
        {
            exprAliasedColumnName.Column.Accept(this, exprAliasedColumnName);
            if (exprAliasedColumnName.Alias != null)
            {
                this.Builder.Append(' ');
                exprAliasedColumnName.Alias.Accept(this, exprAliasedColumnName);
            }
            return true;
        }

        public bool VisitExprAliasedSelecting(ExprAliasedSelecting exprAliasedSelecting, IExpr? parent)
        {
            exprAliasedSelecting.Value.Accept(this, exprAliasedSelecting);
            this.Builder.Append(' ');
            exprAliasedSelecting.Alias.Accept(this, exprAliasedSelecting);
            return true;
        }

        public abstract bool VisitExprTempTableName(ExprTempTableName tempTableName, IExpr? parent);

        public bool VisitExprTableAlias(ExprTableAlias tableAlias, IExpr? parent)
        {
            tableAlias.Alias.Accept(this, tableAlias);
            return true;
        }

        public bool VisitExprSchemaName(ExprSchemaName schemaName, IExpr? parent)
        {
            this.AppendName(this.Options.MapSchema(schemaName.Name));
            return true;
        }

        public bool VisitExprDatabaseName(ExprDatabaseName databaseName, IExpr? parent)
        {
            this.AppendName(databaseName.Name);
            return true;
        }

        public abstract bool VisitExprDbSchema(ExprDbSchema exprDbSchema, IExpr? parent);

        public bool VisitExprDbSchemaCommon(ExprDbSchema exprDbSchema, IExpr? parent)
        {
            if (exprDbSchema.Database != null)
            {
                exprDbSchema.Database.Accept(this, exprDbSchema);
                this.Builder.Append('.');
            }

            exprDbSchema.Schema.Accept(this, exprDbSchema);
            return true;
        }

        public bool VisitExprFunctionName(ExprFunctionName exprFunctionName, IExpr? parent)
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

        public bool VisitExprValueRow(ExprValueRow valueRow, IExpr? parent)
        {
            if (valueRow.Items == null || valueRow.Items.Count < 1)
            {
                throw new SqExpressException("Row value should have at least one column");
            }

            this.AcceptListComaSeparatedPar('(',valueRow.Items, ')', valueRow);

            return true;
        }

        public bool VisitExprTableValueConstructor(ExprTableValueConstructor tableValueConstructor, IExpr? parent)
        {
            this.Builder.Append("VALUES ");

            var nullCast = CheckForNullColCast(tableValueConstructor.Items);

            for (var rowIndex = 0; rowIndex < tableValueConstructor.Items.Count; rowIndex++)
            {
                var rowValue = tableValueConstructor.Items[rowIndex];

                if (rowIndex>0)
                {
                    this.Builder.Append(',');
                }
                else
                {
                    if (nullCast != null)
                    {
                        var newItems = rowValue
                            .Items
                            .Select((item, index) =>
                            {
                                var exprType = nullCast[index];
                                return exprType == null ? item : SqQueryBuilder.Cast(item, exprType);
                            })
                            .ToList();
                        rowValue = rowValue.WithItems(newItems);
                    }
                }

                rowValue.Accept(this, tableValueConstructor);
            }

            return true;


            //If some column contains only null then an explicit cast will be required
            static ExprType?[]? CheckForNullColCast(IReadOnlyList<ExprValueRow> rows)
            {
                ExprType?[]? result = null;
                int resultFirstRow = 0;
                for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    var row = rows[rowIndex];

                    if (result != null && result.Length != row.Items.Count)
                    {
                        throw new SqExpressException("All rows have to have the same number of columns");
                    }

                    bool rowAllNotNull = true;

                    for (int colIndex = 0; colIndex < row.Items.Count; colIndex++)
                    {
                        var value = row.Items[colIndex];
                        if (value.IsNullValue() == true)
                        {
                            rowAllNotNull = false;

                            if (result == null)
                            {
                                result = new ExprType?[row.Items.Count];
                                resultFirstRow = rowIndex;
                            }
                            if (resultFirstRow == rowIndex)
                            {
                                var varTypeDetails = value.GetTypeDetails();
                                result[colIndex] = varTypeDetails.Type;
                            }
                        }
                        else if(result != null && result[colIndex] != null)
                        {
                            result[colIndex] = null;
                        }
                    }

                    if (rowAllNotNull)
                    {
                        result = null;
                        break;
                    }
                }

                return result;
            }

        }

        public bool VisitExprDerivedTableQuery(ExprDerivedTableQuery exprDerivedTableQuery, IExpr? parent)
        {
            this.AcceptPar('(', exprDerivedTableQuery.Query, ')', exprDerivedTableQuery);
            exprDerivedTableQuery.Alias.Accept(this, exprDerivedTableQuery);
            if (exprDerivedTableQuery.Columns != null && exprDerivedTableQuery.Columns.Count > 0)
            {
                var selectedColumns = exprDerivedTableQuery.Query.GetOutputColumnNames();

                if (selectedColumns.Count != exprDerivedTableQuery.Columns.Count)
                {
                    throw new SqExpressException("Number of declared columns does not match to number of selected columns in the derived table sub query");
                }

                var derivedTableColumns = new HashSet<string>(exprDerivedTableQuery.Columns.Select(i => i.Name), StringComparer.InvariantCultureIgnoreCase);

                bool allMatch = true;
                foreach (var colName in selectedColumns)
                {
                    if (colName == null || !derivedTableColumns.Remove(colName))
                    {
                        allMatch = false;
                        break;
                    }
                }
                if (!allMatch)
                {
                    this.AcceptListComaSeparatedPar('(', exprDerivedTableQuery.Columns, ')', exprDerivedTableQuery);
                }
            }

            return true;
        }

        public abstract bool VisitExprDerivedTableValues(ExprDerivedTableValues derivedTableValues, IExpr? parent);

        public bool VisitExprCteQuery(ExprCteQuery exprCte, IExpr? parent)
        {
            this.AddCteSlot(parent);
            this.AddCteExpressionToSlot(exprCte);

            if (parent != null)
            {
                this.AppendName(exprCte.Name);
                if (exprCte.Alias != null)
                {
                    this.Builder.Append(' ');
                    exprCte.Alias.Accept(this, exprCte);
                }
            }

            return true;
        }

        protected bool VisitExprDerivedTableValuesCommon(ExprDerivedTableValues derivedTableValues, IExpr? parent)
        {
            this.AcceptPar('(', derivedTableValues.Values, ')', derivedTableValues);
            derivedTableValues.Alias.Accept(this, derivedTableValues);
            derivedTableValues.Columns.AssertNotEmpty("List of columns in a derived table with values literals cannot be empty");
            this.AcceptListComaSeparatedPar('(', derivedTableValues.Columns, ')', derivedTableValues);

            return true;
        }

        public bool VisitExprColumnSetClause(ExprColumnSetClause columnSetClause, IExpr? parent)
        {
            columnSetClause.Column.Accept(this, columnSetClause);
            this.Builder.Append('=');
            columnSetClause.Value.Accept(this, columnSetClause);

            return true;
        }

        //Merge

        public abstract bool VisitExprMerge(ExprMerge merge, IExpr? parent);

        public abstract bool VisitExprMergeOutput(ExprMergeOutput mergeOutput, IExpr? parent);

        public abstract bool VisitExprMergeMatchedUpdate(ExprMergeMatchedUpdate mergeMatchedUpdate, IExpr? parent);

        public abstract bool VisitExprMergeMatchedDelete(ExprMergeMatchedDelete mergeMatchedDelete, IExpr? parent);

        public abstract bool VisitExprExprMergeNotMatchedInsert(ExprExprMergeNotMatchedInsert exprMergeNotMatchedInsert, IExpr? parent);

        public abstract bool VisitExprExprMergeNotMatchedInsertDefault(ExprExprMergeNotMatchedInsertDefault exprExprMergeNotMatchedInsertDefault, IExpr? parent);

        //Insert
        public abstract bool VisitExprInsert(ExprInsert exprInsert, IExpr? parent);

        protected void GenericInsert(ExprInsert exprInsert, Action? middleHandler, Action? endHandler)
        {
            this.Builder.Append("INSERT INTO ");
            exprInsert.Target.Accept(this, exprInsert);
            if (exprInsert.TargetColumns != null)
            {
                this.AssertNotEmptyList(exprInsert.TargetColumns, "Insert column list cannot be empty");
                this.AcceptListComaSeparatedPar('(', exprInsert.TargetColumns, ')', exprInsert);
            }

            middleHandler?.Invoke();

            this.Builder.Append(' ');
            exprInsert.Source.Accept(this, exprInsert);
            if (endHandler != null)
            {
                this.Builder.Append(' ');
                endHandler();
            }
        }

        public abstract bool VisitExprInsertOutput(ExprInsertOutput exprInsertOutput, IExpr? parent);

        public bool VisitExprInsertValues(ExprInsertValues exprInsertValues, IExpr? parent)
        {
            if (exprInsertValues.Items.Count < 1)
            {
                throw new SqExpressException("Insert values should have at least one record");
            }

            this.Builder.Append("VALUES ");

            for (var i = 0; i < exprInsertValues.Items.Count; i++)
            {
                var rowValue = exprInsertValues.Items[i];
                if (i > 0)
                {
                    this.Builder.Append(',');
                }
                rowValue.Accept(this, exprInsertValues);
            }

            return true;
        }

        public bool VisitExprInsertValueRow(ExprInsertValueRow exprInsertValueRow, IExpr? arg)
        {
            if (exprInsertValueRow.Items == null || exprInsertValueRow.Items.Count < 1)
            {
                throw new SqExpressException("Row value should have at least one column");
            }

            this.AcceptListComaSeparatedPar('(', exprInsertValueRow.Items, ')', exprInsertValueRow);

            return true;
        }

        public abstract bool VisitExprInsertQuery(ExprInsertQuery exprInsertQuery, IExpr? parent);
        public abstract bool VisitExprIdentityInsert(ExprIdentityInsert exprIdentityInsert, IExpr? arg);

        protected bool VisitExprInsertQueryCommon(ExprInsertQuery exprInsertQuery, IExpr? parent)
        {
            exprInsertQuery.Query.Accept(this, exprInsertQuery);
            return true;
        }

        //Update

        public abstract bool VisitExprUpdate(ExprUpdate exprUpdate, IExpr? parent);

        //Delete

        public abstract bool VisitExprDelete(ExprDelete exprDelete, IExpr? parent);

        public abstract bool VisitExprDeleteOutput(ExprDeleteOutput exprDeleteOutput, IExpr? parent);

        public bool VisitExprList(ExprList exprList, IExpr? parent)
        {
            for (var index = 0; index < exprList.Expressions.Count; index++)
            {
                var expr = exprList.Expressions[index];

                if (index != 0 && exprList.Expressions[index-1] is not ExprStatement)
                {
                    this.Builder.Append(';');
                }
                expr.Accept(this, exprList);
            }

            return true;
        }

        public bool VisitExprQueryList(ExprQueryList exprList, IExpr? parent)
        {
            for (var index = 0; index < exprList.Expressions.Count; index++)
            {
                var expr = exprList.Expressions[index];

                if (index != 0 && exprList.Expressions[index - 1] is not ExprStatement)
                {
                    this.Builder.Append(';');
                }

                expr.Accept(this, exprList);
            }
            return true;
        }

        public abstract bool VisitExprCast(ExprCast exprCast, IExpr? parent);

        protected bool VisitExprCastCommon(ExprCast exprCast, IExpr? parent)
        {
            this.Builder.Append("CAST(");
            exprCast.Expression.Accept(this, exprCast);
            this.Builder.Append(" AS ");
            exprCast.SqlType.Accept(this, exprCast);
            this.Builder.Append(')');
            return true;
        }

        //Types
        
        public abstract bool VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean, IExpr? parent);

        public abstract bool VisitExprTypeByte(ExprTypeByte exprTypeByte, IExpr? parent);

        public abstract bool VisitExprTypeByteArray(ExprTypeByteArray exprTypeByte, IExpr? arg);

        public abstract bool VisitExprTypeFixSizeByteArray(ExprTypeFixSizeByteArray exprTypeFixSizeByteArray, IExpr? arg);

        public abstract bool VisitExprTypeInt16(ExprTypeInt16 exprTypeInt16, IExpr? parent);

        public abstract bool VisitExprTypeInt32(ExprTypeInt32 exprTypeInt32, IExpr? parent);

        public abstract bool VisitExprTypeInt64(ExprTypeInt64 exprTypeInt64, IExpr? parent);

        public abstract bool VisitExprTypeDecimal(ExprTypeDecimal exprTypeDecimal, IExpr? parent);

        public abstract bool VisitExprTypeDouble(ExprTypeDouble exprTypeDouble, IExpr? parent);

        public abstract bool VisitExprTypeDateTime(ExprTypeDateTime exprTypeDateTime, IExpr? parent);

        public abstract bool VisitExprTypeDateTimeOffset(ExprTypeDateTimeOffset exprTypeDateTimeOffset, IExpr? arg);

        public abstract bool VisitExprTypeGuid(ExprTypeGuid exprTypeGuid, IExpr? parent);

        public abstract bool VisitExprTypeString(ExprTypeString exprTypeString, IExpr? parent);

        public abstract bool VisitExprTypeFixSizeString(ExprTypeFixSizeString exprTypeFixSizeString, IExpr? arg);

        public abstract bool VisitExprTypeXml(ExprTypeXml exprTypeXml, IExpr? arg);

        public abstract bool VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull, IExpr? parent);

        public bool VisitExprStatement(ExprStatement statement, IExpr? parent)
        {
            statement.Statement.Accept(this.GetStatementSqlBuilder());
            return true;
        }

        public abstract void AppendName(string name, char? prefix = null);

        protected void AppendNull()
        {
            this.Builder.Append("NULL");
        }

        protected bool AcceptPar(char start, IExpr list, char end, IExpr? parent)
        {
            this.Builder.Append(start);
            list.Accept(this, parent);
            this.Builder.Append(end);

            return true;
        }

        internal bool AcceptListComaSeparatedPar(char start, IReadOnlyList<IExpr> list, char end, IExpr? parent)
        {
            this.Builder.Append(start);
            this.AcceptListComaSeparated(list, parent);
            this.Builder.Append(end);

            return true;
        }

        protected bool AcceptListComaSeparated(IReadOnlyList<IExpr> list, IExpr? parent)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (i != 0)
                {
                    this.Builder.Append(',');
                }

                list[i].Accept(this, parent);
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

        protected abstract IStatementVisitor CreateStatementSqlBuilder();

        protected IStatementVisitor GetStatementSqlBuilder()
        {
            this._statementBuilder ??= this.CreateStatementSqlBuilder();
            return this._statementBuilder;
        }

        protected void AddCteSlot(IExpr? parent)
        {
            if (!this.SupportsInlineCte() && parent == null && this._cteSlot == null)
            {
                this._cteSlot = this.Builder.Length;
            }
        }

        private void AddCteExpressionToSlot(ExprCte cte)
        {
            if (this._dismissCteInject || this.SupportsInlineCte())
            {
                return;
            }
            if (this._cteSlot == null)
            {
                throw new SqExpressException("Could not add CTE expression without a proper context");
            }

            this._uniqueCteCheck ??= new Dictionary<string, ExprCte>();

            if (this._uniqueCteCheck.TryGetValue(cte.Name, out var existingCte))
            {
                if (existingCte.GetType() != cte.GetType())
                {
                    throw new SqExpressException($"Different CTE with name \"{cte.Name}\" has already been added");
                }
            }
            else
            {
                this._uniqueCteCheck.Add(cte.Name, cte);
            }
        }

        protected void AcceptCteExpressions(IReadOnlyList<ExprCte> expressions)
        {
            this._uniqueCteCheck ??= new Dictionary<string, ExprCte>();
            bool recursive = false;
            var result = new List<ExprCte>();
            var parents = expressions.Where(e=> !this._uniqueCteCheck.ContainsKey(e.Name)).ToList();
            foreach (var parent in parents)
            {
                this._uniqueCteCheck.Add(parent.Name, parent);
            }

            result.AddRange(parents);
            while (parents.Count > 0)
            {
                var nextChunk = new List<ExprCte>();
                foreach (var cte in parents)
                {
                    foreach (var subCte in cte.SyntaxTree().Descendants().OfType<ExprCte>())
                    {
                        if (subCte.Name == cte.Name)
                        {
                            recursive = true;
                        }
                        else
                        {
                            if (!this._uniqueCteCheck.TryGetValue(subCte.Name, out var existingSubCte))
                            {
                                this._uniqueCteCheck.Add(subCte.Name, subCte);
                                nextChunk.Add(subCte);
                            }
                            else
                            {
                                var type1 = subCte.GetType();
                                var type2 = existingSubCte.GetType();

                                if (type1 != typeof(ExprCteQuery) && type2 != typeof(ExprCteQuery) && type1 != type2)
                                {
                                    throw new SqExpressException($"Different CTE with name \"{cte.Name}\" has already been added");
                                }
                            }
                        }
                    }
                }
                result.InsertRange(0, nextChunk);
                parents = nextChunk;
            }

            this.Builder.Append("WITH ");
            if (recursive)
            {
                this.AppendRecursiveCteKeyword();
            }

            for (var index = 0; index < result.Count; index++)
            {
                var exprCte = result[index];
                if (index != 0)
                {
                    this.Builder.Append(',');
                }

                this.AppendName(exprCte.Name);
                this.Builder.Append(" AS");
                this.AcceptPar('(', exprCte.CreateQuery(), ')', exprCte);
            }
        }

        protected abstract void AppendRecursiveCteKeyword();

        protected abstract bool SupportsInlineCte();

        protected void AcceptInlineCte(IExprSubQuery querySpecification)
        {
            if (!this.SupportsInlineCte())
            {
                return;
            }

            //It founds a first mention of first CTE node and does not go deeper
            var cteList = querySpecification.SyntaxTree()
                .WalkThrough((e, c) =>
                    {
                        if (e is ExprCte cte && !(this._uniqueCteCheck?.ContainsKey(cte.Name) ?? false))
                        {
                            if (!c.ContainsKey(cte.Name))
                            {
                                c.Add(cte.Name, cte);
                            }
                            return VisitorResult<Dictionary<string, ExprCte>>.StopNode(c);
                        }
                        return VisitorResult<Dictionary<string, ExprCte>>.Continue(c);
                    },
                    new Dictionary<string, ExprCte>());

            if (cteList.Count < 1)
            {
                return;
            }

            this.AcceptCteExpressions(cteList.Values.ToList());
            this.Builder.Append(' ');
        }

        private string? InjectCteToSlot(out int position)
        {
            position = 0;
            if (this._cteSlot == null || this._uniqueCteCheck == null)
            {
                return null;
            }

            position = this._cteSlot.Value;

            var cteBuilder = this.CreateInstance(this._aliasGenerator, true);

            var list = this._uniqueCteCheck.Values.ToList();

            cteBuilder.AcceptCteExpressions(list);

            return cteBuilder.ToString();
        }

        public override string ToString()
        {
            var result = this.Builder.ToString();
            if (!this._dismissCteInject)
            {
                var cteResult = this.InjectCteToSlot(out var ctePosition);
                if (cteResult != null)
                {
                    result = result.Insert(ctePosition, cteResult);
                }
            }
            return result;
        }
    }
}