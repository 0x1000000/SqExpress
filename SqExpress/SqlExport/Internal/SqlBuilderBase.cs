using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Internal;
using SqExpress.Syntax.Json;
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
        private readonly SqlAliasGenerator _aliasGenerator;
        private readonly bool _dismissCteInject;

        private IStatementVisitor? _statementBuilder;
        private int? _cteSlot;
        private Dictionary<string, ExprCte>? _uniqueCteCheck;
        private List<DbParameterValue>? _parameters;
        private int _parameterNumberOffset;
        private string? _cachedFinalSql;
        private bool _renderingForJson;
        private Dictionary<string, string>? _jsonOutputAliases;

        protected SqlBuilderBase(SqlBuilderOptions? options, SqlAliasGenerator aliasGenerator, bool dismissCteInject)
            : this(
                options,
                new SqlFormattingWriter(
                    (options ?? SqlBuilderOptions.Default).FormattingProfile ?? SqlFormattingProfile.Unformatted),
                aliasGenerator,
                dismissCteInject)
        {
        }

        protected SqlBuilderBase(
            SqlBuilderOptions? options,
            SqlFormattingWriter formattingWriter,
            SqlAliasGenerator aliasGenerator,
            bool dismissCteInject)
        {
            this.Options = options ?? SqlBuilderOptions.Default;
            this._dismissCteInject = dismissCteInject;
            this._aliasGenerator = aliasGenerator;
            this.FormattingWriter = formattingWriter;
        }

        protected SqlBuilderOptions Options { get; }

        protected SqlFormattingWriter FormattingWriter { get; }

        protected bool RenderingForJson => this._renderingForJson;

        protected void RenderForJsonSource(IExprQuery query, IExpr parent)
        {
            var previous = this._renderingForJson;
            var previousAliases = this._jsonOutputAliases;
            var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
            var jsonColumnIndex = 0;
            foreach (var selecting in query.ExtractSelecting())
            {
                if (selecting is ExprJsonOutputColumn json)
                {
                    aliases.Add(json.JsonPath, JsonOutputShape.InternalColumnName(jsonColumnIndex++));
                }
            }
            this._renderingForJson = true;
            this._jsonOutputAliases = aliases;
            try
            {
                query.Accept(this, parent);
            }
            finally
            {
                this._renderingForJson = previous;
                this._jsonOutputAliases = previousAliases;
            }
        }

        protected void AppendJsonOutputAlias(ExprJsonOutputColumn expression)
        {
            if (this._jsonOutputAliases == null || !this._jsonOutputAliases.TryGetValue(expression.JsonPath, out var alias))
                throw new SqExpressException("AsJson() output column is not part of the ForJson() projection.");
            this.AppendName(alias);
        }

        public IReadOnlyList<DbParameterValue>? ParameterValues => this._parameters;

        // Dialect extension points

        protected abstract SqlBuilderBase CreateInstance(SqlAliasGenerator aliasGenerator, bool dismissCteInject);

        protected abstract void EscapeStringLiteral(string literal);

        protected abstract void AppendUnicodePrefix(string str);

        protected abstract void AppendByteArrayLiteralPrefix();

        protected abstract void AppendByteArrayLiteralSuffix();

        protected abstract void AppendSelectTop(ExprValue top, IExpr? parent);

        protected abstract void AppendSelectLimit(ExprValue top, IExpr? parent);

        protected abstract bool ForceParenthesesForQueryExpressionPart(IExprSubQuery subQuery);

        protected abstract bool VisitExprParameter(ExprParameter exprParameter, int paramNumber, IExpr? parent, out string? name);

        protected abstract DbParameterValueVisitorExtractor GetDbParameterValueVisitorExtractor();

        protected abstract IStatementVisitor CreateStatementSqlBuilder();

        protected abstract void AppendRecursiveCteKeyword();

        protected abstract bool SupportsInlineCte();

        // Boolean expressions

        public bool VisitExprBooleanAnd(ExprBooleanAnd expr, IExpr? parent)
        {
            bool leftPar = expr.Left is ExprBooleanOr;
            bool rightPar = expr.Right is ExprBooleanOr;
            if (leftPar) this.AcceptPar('(', expr.Left, ')', expr);
            else expr.Left.Accept(this, expr);
            int leftCompactSpaces = leftPar ? 0 : 1;
            this.FormattingWriter.AppendBoundary(SqlRenderSite.BooleanOperator, leftCompactSpaces);
            this.FormattingWriter.Append("AND");
            int rightCompactSpaces = rightPar ? 0 : 1;
            this.FormattingWriter.AppendBoundary(SqlRenderSite.BooleanRight, rightCompactSpaces);
            if (rightPar) this.AcceptPar('(', expr.Right, ')', expr);
            else expr.Right.Accept(this, expr);
            return true;
        }

        public bool VisitExprBooleanOr(ExprBooleanOr expr, IExpr? parent)
        {
            expr.Left.Accept(this, expr);
            this.FormattingWriter.AppendBoundary(SqlRenderSite.BooleanOperator, 1);
            this.FormattingWriter.Append("OR");
            this.FormattingWriter.AppendBoundary(SqlRenderSite.BooleanRight, 1);
            expr.Right.Accept(this, expr);
            return true;
        }

        public bool VisitExprBooleanNot(ExprBooleanNot expr, IExpr? parent)
        {
            this.FormattingWriter.Append("NOT");
            if (expr.Expr is ExprPredicate)
            {
                this.FormattingWriter.Append(' ');
                expr.Expr.Accept(this, expr);
            }
            else
            {
                this.AcceptPar('(', expr.Expr, ')', expr);
            }

            return true;
        }

        // Boolean predicates

        public bool VisitExprBooleanNotEq(ExprBooleanNotEq exprBooleanNotEq, IExpr? parent)
        {
            exprBooleanNotEq.Left.Accept(this, exprBooleanNotEq);
            this.FormattingWriter.Append("!=");
            exprBooleanNotEq.Right.Accept(this, exprBooleanNotEq);

            return true;
        }

        public bool VisitExprBooleanEq(ExprBooleanEq exprBooleanEq, IExpr? parent)
        {
            exprBooleanEq.Left.Accept(this, exprBooleanEq);
            this.FormattingWriter.Append('=');
            exprBooleanEq.Right.Accept(this, exprBooleanEq);

            return true;
        }

        public bool VisitExprBooleanGt(ExprBooleanGt booleanGt, IExpr? parent)
        {
            booleanGt.Left.Accept(this, booleanGt);
            this.FormattingWriter.Append('>');
            booleanGt.Right.Accept(this, booleanGt);

            return true;
        }

        public bool VisitExprBooleanGtEq(ExprBooleanGtEq booleanGtEq, IExpr? parent)
        {
            booleanGtEq.Left.Accept(this, booleanGtEq);
            this.FormattingWriter.Append(">=");
            booleanGtEq.Right.Accept(this, booleanGtEq);

            return true;
        }

        public bool VisitExprBooleanLt(ExprBooleanLt booleanLt, IExpr? parent)
        {
            booleanLt.Left.Accept(this, booleanLt);
            this.FormattingWriter.Append('<');
            booleanLt.Right.Accept(this, booleanLt);

            return true;
        }

        public bool VisitExprBooleanLtEq(ExprBooleanLtEq booleanLtEq, IExpr? parent)
        {
            booleanLtEq.Left.Accept(this, booleanLtEq);
            this.FormattingWriter.Append("<=");
            booleanLtEq.Right.Accept(this, booleanLtEq);

            return true;
        }

        // Other Boolean predicates

        public bool VisitExprInSubQuery(ExprInSubQuery exprInSubQuery, IExpr? parent)
        {
            exprInSubQuery.TestExpression.Accept(this, exprInSubQuery);
            this.FormattingWriter.Append(" IN");
            this.AcceptPar('(', exprInSubQuery.SubQuery, ')', exprInSubQuery);
            return true;
        }

        public bool VisitExprInValues(ExprInValues exprInValues, IExpr? parent)
        {
            exprInValues.TestExpression.Accept(this, exprInValues);
            this.AssertNotEmptyList(exprInValues.Items, "'IN' Predicate cannot have an empty list of expressions");
            this.FormattingWriter.Append(" IN");
            this.AcceptListComaSeparatedPar('(', exprInValues.Items, ')', exprInValues);
            return true;
        }

        public bool VisitExprExists(ExprExists exprExists, IExpr? parent)
        {
            this.FormattingWriter.Append("EXISTS");
            this.AcceptPar('(', exprExists.SubQuery, ')', exprExists);
            return true;
        }

        public bool VisitExprIsNull(ExprIsNull exprIsNull, IExpr? parent)
        {
            exprIsNull.Test.Accept(this, exprIsNull);
            this.FormattingWriter.Append(" IS");
            if (exprIsNull.Not)
            {
                this.FormattingWriter.Append(" NOT");
            }
            this.FormattingWriter.Append(" NULL");
            return true;
        }

        public bool VisitExprLike(ExprLike exprLike, IExpr? parent)
        {
            exprLike.Test.Accept(this, exprLike);
            this.FormattingWriter.Append(" LIKE ");
            exprLike.Pattern.Accept(this, exprLike);
            return true;
        }

        // Values

        public bool VisitExprInt32Literal(ExprInt32Literal exprInt32Literal, IExpr? parent)
        {
            if (exprInt32Literal.Value == null)
            {
                this.AppendNull();
                return true;
            }

            this.FormattingWriter.Append(exprInt32Literal.Value.Value);

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
            this.FormattingWriter.Append('\'');
            if (stringLiteral.Value != null)
            {
                this.EscapeStringLiteral(stringLiteral.Value);
            }

            this.FormattingWriter.Append('\'');
            return true;
        }

        public virtual bool VisitExprDateTimeLiteral(ExprDateTimeLiteral dateTimeLiteral, IExpr? parent)
        {
            if (!dateTimeLiteral.Value.HasValue)
            {
                this.AppendNull();
            }
            else
            {
                this.FormattingWriter.Append('\'');
                if (dateTimeLiteral.Value.Value.TimeOfDay != TimeSpan.Zero)
                {
                    this.FormattingWriter.Append(dateTimeLiteral.Value.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
                }
                else
                {
                    this.FormattingWriter.Append(dateTimeLiteral.Value.Value.ToString("yyyy-MM-dd"));
                }
                this.FormattingWriter.Append('\'');
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
                this.FormattingWriter.Append('\'');
                this.FormattingWriter.Append(dateTimeLiteral.Value.Value.ToString("O"));
                this.FormattingWriter.Append('\'');
            }
            return true;
        }

        public abstract bool VisitExprBoolLiteral(ExprBoolLiteral boolLiteral, IExpr? parent);

        public bool VisitExprInt64Literal(ExprInt64Literal int64Literal, IExpr? parent)
        {
            if (int64Literal.Value.HasValue)
            {
                this.FormattingWriter.Append(int64Literal.Value.Value);
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
                this.FormattingWriter.Append(byteLiteral.Value.Value);
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
                this.FormattingWriter.Append(int16Literal.Value.Value);
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
                this.FormattingWriter.Append(decimalLiteral.Value.Value.ToString(CultureInfo.InvariantCulture));
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
                this.FormattingWriter.Append(doubleLiteral.Value.Value.ToString(CultureInfo.InvariantCulture));
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
                    this.FormattingWriter.AppendHexByte(byteArrayLiteral.Value[i]);
                }
                this.AppendByteArrayLiteralSuffix();
            }

            return true;
        }

        public bool VisitExprNull(ExprNull exprNull, IExpr? parent)
        {
            this.FormattingWriter.Append("NULL");
            return true;
        }

        public bool VisitExprDefault(ExprDefault exprDefault, IExpr? parent)
        {
            this.FormattingWriter.Append("DEFAULT");
            return true;
        }

        public bool VisitExprUnsafeValue(ExprUnsafeValue exprUnsafeValue, IExpr? parent)
        {
            this.FormattingWriter.Append(exprUnsafeValue.UnsafeValue);
            return true;
        }

        public bool VisitExprSelectingValue(ExprSelectingValue exprSelectingValue, IExpr? parent)
        {
            return exprSelectingValue.Selecting.Accept(this, exprSelectingValue);
        }

        public bool VisitExprValueQuery(ExprValueQuery exprValueQuery, IExpr? parent)
        {
            this.AcceptPar('(', exprValueQuery.Query, ')', exprValueQuery);
            return true;
        }

        // Arithmetic and bitwise expressions

        public bool VisitExprSum(ExprSum exprSum, IExpr? parent)
        {
            exprSum.Left.Accept(this, exprSum);
            this.FormattingWriter.Append('+');
            exprSum.Right.Accept(this, exprSum);
            return true;
        }

        public bool VisitExprSub(ExprSub exprSub, IExpr? parent)
        {
            exprSub.Left.Accept(this, exprSub);
            this.FormattingWriter.Append('-');
            this.CheckPlusMinusParenthesizes(exprSub.Right, exprSub);
            return true;
        }

        public bool VisitExprMul(ExprMul exprMul, IExpr? parent)
        {
            this.CheckPlusMinusParenthesizes(exprMul.Left, exprMul);
            this.FormattingWriter.Append('*');
            this.CheckPlusMinusParenthesizes(exprMul.Right, exprMul);
            return true;
        }

        public bool VisitExprDiv(ExprDiv exprDiv, IExpr? parent)
        {
            this.CheckPlusMinusParenthesizes(exprDiv.Left, exprDiv);
            this.FormattingWriter.Append('/');
            this.CheckPlusMinusParenthesizes(exprDiv.Right, exprDiv);
            return true;
        }

        public bool VisitExprModulo(ExprModulo exprModulo, IExpr? arg)
        {
            this.CheckPlusMinusParenthesizes(exprModulo.Left, exprModulo);
            this.FormattingWriter.Append('%');
            this.CheckPlusMinusParenthesizes(exprModulo.Right, exprModulo);
            return true;
        }

        public abstract bool VisitExprStringConcat(ExprStringConcat exprStringConcat, IExpr? parent);

        private void CheckPlusMinusParenthesizes(ExprValue exp, IExpr? parent)
        {
            if (exp is ExprSum || exp is ExprSub)
            {
                this.FormattingWriter.Append('(');
                exp.Accept(this, parent);
                this.FormattingWriter.Append(')');
            }
            else
            {
                exp.Accept(this, parent);
            }
        }

        public bool VisitExprBitwiseNot(ExprBitwiseNot exprBitwiseNot, IExpr? arg)
        {
            this.FormattingWriter.Append('~');
            this.CheckBitParenthesizes(exprBitwiseNot.Value, exprBitwiseNot);
            return true;

        }

        public bool VisitExprBitwiseAnd(ExprBitwiseAnd exprBitwiseAnd, IExpr? arg)
        {
            this.CheckBitParenthesizes(exprBitwiseAnd.Left, exprBitwiseAnd);
            this.FormattingWriter.Append('&');
            this.CheckBitParenthesizes(exprBitwiseAnd.Right, exprBitwiseAnd);
            return true;
        }

        public bool VisitExprBitwiseXor(ExprBitwiseXor exprBitwiseXor, IExpr? arg)
        {
            this.CheckBitParenthesizes(exprBitwiseXor.Left, exprBitwiseXor);
            this.FormattingWriter.Append('^');
            this.CheckBitParenthesizes(exprBitwiseXor.Right, exprBitwiseXor);
            return true;
        }

        public bool VisitExprBitwiseOr(ExprBitwiseOr exprBitwiseOr, IExpr? arg)
        {
            CheckBitParenthesizes(exprBitwiseOr.Left, exprBitwiseOr);
            this.FormattingWriter.Append('|');
            CheckBitParenthesizes(exprBitwiseOr.Right, exprBitwiseOr);
            return true;
        }

        private void CheckBitParenthesizes(ExprValue exp, ExprValue parent)
        {
            if ((exp is ExprBitwise || exp is ExprArithmetic) && !(exp is ExprBitwiseNot)  && parent.GetType() != exp.GetType())
            {
                this.FormattingWriter.Append('(');
                exp.Accept(this, parent);
                this.FormattingWriter.Append(')');
            }
            else
            {
                exp.Accept(this, parent);
            }
        }

        // Queries and table sources

        protected virtual bool ShouldAppendSelectTop(IExpr? parent) => true;

        public bool VisitExprQuerySpecification(ExprQuerySpecification exprQuerySpecification, IExpr? parent)
        {
            this.AddCteSlot(parent);

            this.AcceptInlineCte(exprQuerySpecification);

            this.FormattingWriter.Append("SELECT");
            if (exprQuerySpecification.Distinct)
            {
                this.FormattingWriter.Append(" DISTINCT");
            }
            if (!ReferenceEquals(exprQuerySpecification.Top, null) && this.ShouldAppendSelectTop(parent))
            {
                this.AppendSelectTop(exprQuerySpecification.Top, exprQuerySpecification);
            }

            this.AcceptItems(exprQuerySpecification.SelectList, exprQuerySpecification, SqlRenderSite.Select, 1);

            if (exprQuerySpecification.From != null)
            {
                this.FormattingWriter.AppendClause("FROM", compactTrailingSpaces: 1);
                exprQuerySpecification.From.Accept(this, exprQuerySpecification);
            }

            if (exprQuerySpecification.Where != null)
            {
                this.FormattingWriter.AppendClause("WHERE");
                this.AcceptBody(exprQuerySpecification.Where, exprQuerySpecification, SqlRenderSite.Where, 1);
            }

            if (exprQuerySpecification.GroupBy != null)
            {
                this.FormattingWriter.AppendClause("GROUP BY");
                this.AcceptItems(exprQuerySpecification.GroupBy, exprQuerySpecification, SqlRenderSite.GroupBy, 1);
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
                    this.FormattingWriter.AppendClause("JOIN", SqlRenderSite.Join, compactTrailingSpaces: 1);
                    break;
                case ExprJoinedTable.ExprJoinType.Left:
                    this.FormattingWriter.AppendClause("LEFT JOIN", SqlRenderSite.Join, compactTrailingSpaces: 1);
                    break;
                case ExprJoinedTable.ExprJoinType.Right:
                    this.FormattingWriter.AppendClause("RIGHT JOIN", SqlRenderSite.Join, compactTrailingSpaces: 1);
                    break;
                case ExprJoinedTable.ExprJoinType.Full:
                    this.FormattingWriter.AppendClause("FULL JOIN", SqlRenderSite.Join, compactTrailingSpaces: 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            joinedTable.Right.Accept(this, joinedTable);
            this.FormattingWriter.Append(" ON");
            this.AcceptBody(joinedTable.SearchCondition, joinedTable, SqlRenderSite.JoinOn, 1);

            return true;
        }

        public bool VisitExprCrossedTable(ExprCrossedTable exprCrossedTable, IExpr? parent)
        {
            exprCrossedTable.Left.Accept(this, exprCrossedTable);
            this.FormattingWriter.AppendClause("CROSS JOIN", SqlRenderSite.Join, compactTrailingSpaces: 1);
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
                    this.FormattingWriter.AppendClause("UNION ALL", SqlRenderSite.SetOperator);
                    this.FormattingWriter.AppendBoundary(SqlRenderSite.SetOperator, 1);
                    break;
                case ExprQueryExpressionType.Union:
                    this.FormattingWriter.AppendClause("UNION", SqlRenderSite.SetOperator);
                    this.FormattingWriter.AppendBoundary(SqlRenderSite.SetOperator, 1);
                    break;
                case ExprQueryExpressionType.Except:
                    this.FormattingWriter.AppendClause("EXCEPT", SqlRenderSite.SetOperator);
                    this.FormattingWriter.AppendBoundary(SqlRenderSite.SetOperator, 1);
                    break;
                case ExprQueryExpressionType.Intersect:
                    this.FormattingWriter.AppendClause("INTERSECT", SqlRenderSite.SetOperator);
                    this.FormattingWriter.AppendBoundary(SqlRenderSite.SetOperator, 1);
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

        public bool VisitExprSelect(ExprSelect exprSelect, IExpr? parent)
        {
            this.AddCteSlot(parent);

            exprSelect.SelectQuery.Accept(this, exprSelect);
            this.FormattingWriter.AppendClause("ORDER BY");
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
            if (exprSelectOffsetFetch.OrderBy.OrderList.Count > 0)
            {
                this.FormattingWriter.AppendClause("ORDER BY");
                exprSelectOffsetFetch.OrderBy.Accept(this, exprSelectOffsetFetch);
            }
            else
            {
                this.VisitExprUnorderedOffsetFetch(exprSelectOffsetFetch.OrderBy.OffsetFetch, exprSelectOffsetFetch);
            }
            return true;
        }

        protected virtual void VisitExprUnorderedOffsetFetch(ExprOffsetFetch exprOffsetFetch, ExprSelectOffsetFetch parent)
        {
            if (parent.SelectQuery is ExprQuerySpecification specification && !ReferenceEquals(specification.Top, null))
            {
                this.AppendSelectLimit(specification.Top, parent);
            }

            exprOffsetFetch.Accept(this, parent.OrderBy);
        }

        public bool VisitExprOrderBy(ExprOrderBy exprOrderBy, IExpr? parent)
        {
            this.AcceptItems(exprOrderBy.OrderList, exprOrderBy, parent is ExprSelect ? SqlRenderSite.OrderBy : SqlRenderSite.Inline, parent is ExprSelect ? 1 : 0);
            return true;
        }

        public virtual bool VisitExprOrderByOffsetFetch(ExprOrderByOffsetFetch exprOrderByOffsetFetch, IExpr? parent)
        {
            this.AcceptItems(exprOrderByOffsetFetch.OrderList, exprOrderByOffsetFetch, SqlRenderSite.OrderBy, 1);

            if (parent is ExprSelectOffsetFetch exprSelectOffsetFetch && exprSelectOffsetFetch.SelectQuery is ExprQuerySpecification specification)
            {
                if (!ReferenceEquals(specification.Top, null))
                {
                    if (!ReferenceEquals(exprSelectOffsetFetch.OrderBy.OffsetFetch.Fetch, null))
                    {
                        var fetchFetch = exprSelectOffsetFetch.OrderBy.OffsetFetch.Fetch as ExprInt32Literal;
                        if (ReferenceEquals(fetchFetch, null) || fetchFetch.Value.HasValue)
                        {
                            throw new SqExpressException("Query with \"FETCH\" cannot be limited");
                        }
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
                this.FormattingWriter.Append(" DESC");
            }
            return true;
        }

        public abstract bool VisitExprOffsetFetch(ExprOffsetFetch exprOffsetFetch, IExpr? parent);

        protected bool VisitExprOffsetFetchCommon(ExprOffsetFetch exprOffsetFetch, IExpr? parent)
        {
            this.FormattingWriter.AppendClause("OFFSET", compactTrailingSpaces: 1);
            exprOffsetFetch.Offset.Accept(this, exprOffsetFetch);
            this.FormattingWriter.Append(" ROW");

            if (!ReferenceEquals(exprOffsetFetch.Fetch,null))
            {
                this.FormattingWriter.AppendClause("FETCH NEXT", compactTrailingSpaces: 1);
                exprOffsetFetch.Fetch.Accept(this, exprOffsetFetch);
                this.FormattingWriter.Append(" ROW ONLY");
            }

            return true;
        }

        // Output

        public bool VisitExprOutputColumnInserted(ExprOutputColumnInserted exprOutputColumnInserted, IExpr? parent)
        {
            this.FormattingWriter.Append("INSERTED.");
            exprOutputColumnInserted.ColumnName.Accept(this, exprOutputColumnInserted);
            return true;
        }

        public bool VisitExprOutputColumnDeleted(ExprOutputColumnDeleted exprOutputColumnDeleted, IExpr? parent)
        {
            this.FormattingWriter.Append("DELETED.");
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
            this.FormattingWriter.Append("$ACTION");
            if (exprOutputAction.Alias != null)
            {
                this.FormattingWriter.Append(' ');
                exprOutputAction.Alias.Accept(this, exprOutputAction);
            }
            return true;
        }

        public bool VisitExprOutput(ExprOutput exprOutput, IExpr? parent)
        {
            this.AssertNotEmptyList(exprOutput.Columns, "Output column list cannot be empty");
            this.AcceptItems(exprOutput.Columns, exprOutput, SqlRenderSite.Output, parent is ExprMergeOutput ? 1 : 0);
            return true;
        }

        protected void AppendFunctionSingleArg(string name, IReadOnlyList<ExprValue>? arguments, ExprPortableScalarFunction expr)
        {
            this.AssertArgumentsCount(arguments, 1, expr.PortableFunction);

            this.FormattingWriter.Append(name);
            this.FormattingWriter.Append('(');
            arguments![0].Accept(this, expr);
            this.FormattingWriter.Append(')');
        }

        protected void AppendFunctionTwoArgs(string name, IReadOnlyList<ExprValue>? arguments, ExprPortableScalarFunction expr)
        {
            this.AssertArgumentsCount(arguments, 2, expr.PortableFunction);

            this.FormattingWriter.Append(name);
            this.FormattingWriter.Append('(');
            arguments![0].Accept(this, expr);
            this.FormattingWriter.Append(',');
            arguments![1].Accept(this, expr);
            this.FormattingWriter.Append(')');
        }

        protected void AppendFunctionThreeArgs(string name, IReadOnlyList<ExprValue>? arguments, ExprPortableScalarFunction expr)
        {
            this.AssertArgumentsCount(arguments, 3, expr.PortableFunction);

            this.FormattingWriter.Append(name);
            this.FormattingWriter.Append('(');
            arguments![0].Accept(this, expr);
            this.FormattingWriter.Append(',');
            arguments[1].Accept(this, expr);
            this.FormattingWriter.Append(',');
            arguments[2].Accept(this, expr);
            this.FormattingWriter.Append(')');
        }

        protected void AssertArgumentsCount(IReadOnlyList<ExprValue>? arguments, int expected, PortableScalarFunction function)
        {
            var actual = arguments?.Count ?? 0;

            if (actual != expected)
            {
                throw new SqExpressException($"Function \"{function}\" expects {expected} argument(s), but got {actual}.");
            }
        }

        protected void AppendNull()
        {
            this.FormattingWriter.Append("NULL");
        }

        // Functions

        public bool VisitExprAggregateFunction(ExprAggregateFunction exprAggregateFunction, IExpr? parent)
        {
            exprAggregateFunction.Name.Accept(this, exprAggregateFunction);
            this.FormattingWriter.Append('(');
            if (exprAggregateFunction.IsDistinct)
            {
                this.FormattingWriter.Append("DISTINCT ");
            }

            exprAggregateFunction.Expression.Accept(this, exprAggregateFunction);
            this.FormattingWriter.Append(')');

            return true;
        }

        public abstract bool VisitExprStringAgg(ExprStringAgg exprStringAgg, IExpr? parent);

        public bool VisitExprAggregateOverFunction(ExprAggregateOverFunction exprAggregateFunction, IExpr? arg)
        {
            exprAggregateFunction.Function.Accept(this, exprAggregateFunction);
            exprAggregateFunction.Over.Accept(this, exprAggregateFunction);

            return true;
        }

        public virtual bool VisitExprScalarFunction(ExprScalarFunction exprScalarFunction, IExpr? parent)
        {
            if (exprScalarFunction.Schema != null)
            {
                if (exprScalarFunction.Schema.Accept(this, exprScalarFunction))
                {
                    this.FormattingWriter.Append('.');
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
                this.FormattingWriter.Append('(');
                this.FormattingWriter.Append(')');
            }
            
            return true;
        }

        public abstract bool VisitExprPortableScalarFunction(ExprPortableScalarFunction exprPortableScalarFunction, IExpr? arg);

        public abstract bool VisitExprJsonValue(ExprJsonValue expr, IExpr? parent);
        public abstract bool VisitExprJsonQuery(ExprJsonQuery expr, IExpr? parent);
        public abstract bool VisitExprJsonNull(ExprJsonNull expr, IExpr? parent);
        public abstract bool VisitExprJsonSet(ExprJsonSet expr, IExpr? parent);
        public abstract bool VisitExprJsonRemove(ExprJsonRemove expr, IExpr? parent);
        public abstract bool VisitExprJsonObject(ExprJsonObject expr, IExpr? parent);
        public abstract bool VisitExprJsonArray(ExprJsonArray expr, IExpr? parent);
        public abstract bool VisitExprJsonMember(ExprJsonMember expr, IExpr? parent);
        public abstract bool VisitExprJsonTableValueColumn(ExprJsonTableValueColumn expr, IExpr? parent);
        public abstract bool VisitExprJsonTableQueryColumn(ExprJsonTableQueryColumn expr, IExpr? parent);
        public abstract bool VisitExprJsonTableOrdinalColumn(ExprJsonTableOrdinalColumn expr, IExpr? parent);
        public abstract bool VisitExprJsonTable(ExprJsonTable expr, IExpr? parent);
        public abstract bool VisitExprJsonOutputColumn(ExprJsonOutputColumn expr, IExpr? parent);
        public abstract bool VisitExprQueryAsJson(ExprQueryAsJson expr, IExpr? parent);

        public virtual bool VisitExprTableFunction(ExprTableFunction exprTableFunction, IExpr? arg)
        {
            if (exprTableFunction.Schema != null)
            {
                if (exprTableFunction.Schema.Accept(this, exprTableFunction))
                {
                    this.FormattingWriter.Append('.');
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
                this.FormattingWriter.Append('(');
                this.FormattingWriter.Append(')');
            }

            return true;
        }

        public bool VisitExprAliasedTableFunction(ExprAliasedTableFunction exprTableFunction, IExpr? arg)
        {
            exprTableFunction.Function.Accept(this, exprTableFunction);
            this.AcceptBody(exprTableFunction.Alias, exprTableFunction, SqlRenderSite.TableAlias, 1);

            return true;
        }

        public bool VisitExprAnalyticFunction(ExprAnalyticFunction exprAnalyticFunction, IExpr? parent)
        {
            exprAnalyticFunction.Name.Accept(this, exprAnalyticFunction);
            this.FormattingWriter.Append('(');
            if (exprAnalyticFunction.Arguments != null)
            {
                this.AssertNotEmptyList(exprAnalyticFunction.Arguments, "Arguments list cannot be empty");
                this.AcceptListComaSeparated(exprAnalyticFunction.Arguments, exprAnalyticFunction);
            }
            this.FormattingWriter.Append(')');
            exprAnalyticFunction.Over.Accept(this, exprAnalyticFunction);
            return true;
        }

        public bool VisitExprOver(ExprOver exprOver, IExpr? parent)
        {
            this.FormattingWriter.Append("OVER(");

            if (exprOver.Partitions != null)
            {
                this.AssertNotEmptyList(exprOver.Partitions, "Partition list cannot be empty");
                this.FormattingWriter.Append("PARTITION BY ");
                this.AcceptListComaSeparated(exprOver.Partitions, exprOver);
            }

            if (exprOver.OrderBy != null)
            {
                if (exprOver.Partitions != null)
                {
                    this.FormattingWriter.Append(' ');
                }
                this.FormattingWriter.Append("ORDER BY ");
                exprOver.OrderBy.Accept(this, exprOver);
            }

            if (exprOver.FrameClause != null)
            {
                this.FormattingWriter.Append(' ');
                exprOver.FrameClause.Accept(this, exprOver);
            }

            this.FormattingWriter.Append(")");
            return true;
        }

        public bool VisitExprFrameClause(ExprFrameClause exprFrameClause, IExpr? arg)
        {
            this.FormattingWriter.Append("ROWS ");
            
            if (exprFrameClause.End != null)
            {
                this.FormattingWriter.Append("BETWEEN ");
                exprFrameClause.Start.Accept(this, exprFrameClause);
                this.FormattingWriter.Append(" AND ");
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
                    this.FormattingWriter.Append(" PRECEDING");
                    break;
                case FrameBorderDirection.Following:
                    this.FormattingWriter.Append(" FOLLOWING");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return true;
        }

        public bool VisitExprCurrentRowFrameBorder(ExprCurrentRowFrameBorder exprCurrentRowFrameBorder, IExpr? arg)
        {
            this.FormattingWriter.Append("CURRENT ROW");
            return true;
        }

        public bool VisitExprUnboundedFrameBorder(ExprUnboundedFrameBorder exprUnboundedFrameBorder, IExpr? arg)
        {
            switch (exprUnboundedFrameBorder.FrameBorderDirection)
            {
                case FrameBorderDirection.Preceding:
                    this.FormattingWriter.Append("UNBOUNDED PRECEDING");
                    break;
                case FrameBorderDirection.Following:
                    this.FormattingWriter.Append("UNBOUNDED FOLLOWING");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        public bool VisitExprCase(ExprCase exprCase, IExpr? parent)
        {
            this.AssertNotEmptyList(exprCase.Cases, "Cases cannot be empty");

            this.FormattingWriter.Append("CASE");
            for (int i = 0; i < exprCase.Cases.Count; i++)
            {
                this.FormattingWriter.Append(' ');
                exprCase.Cases[i].Accept(this, exprCase);
            }
            this.FormattingWriter.Append(" ELSE ");
            exprCase.DefaultValue.Accept(this, exprCase);
            this.FormattingWriter.Append(" END");
            return true;
        }

        public bool VisitExprCaseWhenThen(ExprCaseWhenThen exprCaseWhenThen, IExpr? parent)
        {
            this.FormattingWriter.Append("WHEN ");
            exprCaseWhenThen.Condition.Accept(this, exprCaseWhenThen);
            this.FormattingWriter.Append(" THEN ");
            exprCaseWhenThen.Value.Accept(this, exprCaseWhenThen);

            return true;
        }

        // Known functions
        public bool VisitExprFuncCoalesce(ExprFuncCoalesce exprFuncCoalesce, IExpr? parent)
        {
            this.FormattingWriter.Append("COALESCE(");
            exprFuncCoalesce.Test.Accept(this, exprFuncCoalesce);
            this.FormattingWriter.Append(',');
            this.AssertNotEmptyList(exprFuncCoalesce.Alts, "Alt argument list cannot be empty in 'COALESCE' function call");
            this.AcceptListComaSeparated(exprFuncCoalesce.Alts, exprFuncCoalesce);
            this.FormattingWriter.Append(')');
            return true;
        }

        public abstract bool VisitExprGetDate(ExprGetDate exprGetDate, IExpr? parent);

        public abstract bool VisitExprGetUtcDate(ExprGetUtcDate exprGetUtcDate, IExpr? parent);

        public abstract bool VisitExprDateAdd(ExprDateAdd exprDateAdd, IExpr? arg);

        public abstract bool VisitExprDateDiff(ExprDateDiff exprDateDiff, IExpr? arg);

        public abstract bool VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull, IExpr? parent);

        // Names, aliases, and table metadata

        public abstract void AppendName(string name, char? prefix = null);

        public bool VisitExprColumn(ExprColumn exprColumn, IExpr? parent)
        {
            if (exprColumn.Source != null)
            {
                exprColumn.Source.Accept(this, exprColumn);
                this.FormattingWriter.Append('.');
            }

            exprColumn.ColumnName.Accept(this, exprColumn);

            return true;
        }

        public bool VisitExprTable(ExprTable exprTable, IExpr? parent)
        {
            exprTable.FullName.Accept(this, exprTable);
            if (exprTable.Alias != null)
            {
                this.AcceptBody(exprTable.Alias, exprTable, SqlRenderSite.TableAlias, 1);
            }
            return true;
        }

        public bool VisitExprAllColumns(ExprAllColumns exprAllColumns, IExpr? parent)
        {
            if (exprAllColumns.Source != null)
            {
                exprAllColumns.Source.Accept(this, exprAllColumns);
                this.FormattingWriter.Append('.');
            }

            this.FormattingWriter.Append('*');

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
                    this.FormattingWriter.Append('.');
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
                this.FormattingWriter.Append(' ');
                exprAliasedColumn.Alias?.Accept(this, exprAliasedColumn);
            }
            return true;
        }

        public bool VisitExprAliasedColumnName(ExprAliasedColumnName exprAliasedColumnName, IExpr? parent)
        {
            exprAliasedColumnName.Column.Accept(this, exprAliasedColumnName);
            if (exprAliasedColumnName.Alias != null)
            {
                this.FormattingWriter.Append(' ');
                exprAliasedColumnName.Alias.Accept(this, exprAliasedColumnName);
            }
            return true;
        }

        public bool VisitExprAliasedSelecting(ExprAliasedSelecting exprAliasedSelecting, IExpr? parent)
        {
            exprAliasedSelecting.Value.Accept(this, exprAliasedSelecting);
            this.FormattingWriter.Append(' ');
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
                this.FormattingWriter.Append('.');
            }

            exprDbSchema.Schema.Accept(this, exprDbSchema);
            return true;
        }

        public bool VisitExprFunctionName(ExprFunctionName exprFunctionName, IExpr? parent)
        {
            if (exprFunctionName.BuiltIn)
            {
                SqlInjectionChecker.AssertValidBuildInFunctionName(exprFunctionName.Name);
                this.FormattingWriter.Append(exprFunctionName.Name);
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
            this.FormattingWriter.Append("VALUES");

            var nullCast = CheckForNullColCast(tableValueConstructor.Items);

            this.AcceptItems(tableValueConstructor.Items.Count, SqlRenderSite.Values, rowIndex =>
            {
                var rowValue = tableValueConstructor.Items[rowIndex];

                if (rowIndex == 0)
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
            }, 1);

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

        public virtual bool VisitExprDerivedTableQuery(ExprDerivedTableQuery exprDerivedTableQuery, IExpr? parent)
        {
            this.AcceptPar('(', exprDerivedTableQuery.Query, ')', exprDerivedTableQuery);
            this.AcceptBody(exprDerivedTableQuery.Alias, exprDerivedTableQuery, SqlRenderSite.TableAlias);

            if (exprDerivedTableQuery.Columns is { Count: > 0 })
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
                    this.AcceptBody(exprCte.Alias, exprCte, SqlRenderSite.TableAlias, 1);
                }
            }

            return true;
        }

        protected bool VisitExprDerivedTableValuesCommon(ExprDerivedTableValues derivedTableValues, IExpr? parent)
        {
            this.AcceptPar('(', derivedTableValues.Values, ')', derivedTableValues);
            this.AcceptBody(derivedTableValues.Alias, derivedTableValues, SqlRenderSite.TableAlias);
            derivedTableValues.Columns.AssertNotEmpty("List of columns in a derived table with values literals cannot be empty");
            this.AcceptListComaSeparatedPar('(', derivedTableValues.Columns, ')', derivedTableValues);

            return true;
        }

        public bool VisitExprColumnSetClause(ExprColumnSetClause columnSetClause, IExpr? parent)
        {
            columnSetClause.Column.Accept(this, columnSetClause);
            this.FormattingWriter.Append('=');
            columnSetClause.Value.Accept(this, columnSetClause);

            return true;
        }

        // MERGE

        public abstract bool VisitExprMerge(ExprMerge merge, IExpr? parent);

        public abstract bool VisitExprMergeOutput(ExprMergeOutput mergeOutput, IExpr? parent);

        public abstract bool VisitExprMergeMatchedUpdate(ExprMergeMatchedUpdate mergeMatchedUpdate, IExpr? parent);

        public abstract bool VisitExprMergeMatchedDelete(ExprMergeMatchedDelete mergeMatchedDelete, IExpr? parent);

        public abstract bool VisitExprExprMergeNotMatchedInsert(ExprExprMergeNotMatchedInsert exprMergeNotMatchedInsert, IExpr? parent);

        public abstract bool VisitExprExprMergeNotMatchedInsertDefault(ExprExprMergeNotMatchedInsertDefault exprExprMergeNotMatchedInsertDefault, IExpr? parent);

        // INSERT
        public abstract bool VisitExprInsert(ExprInsert exprInsert, IExpr? parent);

        protected void GenericInsert(ExprInsert exprInsert, Action? middleHandler, Action? endHandler)
        {
            this.FormattingWriter.Append("INSERT INTO ");
            exprInsert.Target.Accept(this, exprInsert);
            if (exprInsert.TargetColumns != null)
            {
                this.AssertNotEmptyList(exprInsert.TargetColumns, "Insert column list cannot be empty");
                this.AcceptListComaSeparatedPar('(', exprInsert.TargetColumns, ')', exprInsert);
            }

            middleHandler?.Invoke();

            this.FormattingWriter.AppendBoundary(SqlRenderSite.Clause, 1);
            exprInsert.Source.Accept(this, exprInsert);
            if (endHandler != null)
            {
                this.FormattingWriter.Append(' ');
                endHandler();
            }
        }

        public abstract bool VisitExprInsertOutput(ExprInsertOutput exprInsertOutput, IExpr? parent);

        public bool VisitExprInsertValues(ExprInsertValues exprInsertValues, IExpr? parent)
        {
            if (exprInsertValues.Items.Count < 1) throw new SqExpressException("Insert values should have at least one record");
            this.FormattingWriter.Append("VALUES");
            this.AcceptItems(exprInsertValues.Items, exprInsertValues, SqlRenderSite.Values, 1);
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

        // UPDATE

        public abstract bool VisitExprUpdate(ExprUpdate exprUpdate, IExpr? parent);

        // DELETE

        public abstract bool VisitExprDelete(ExprDelete exprDelete, IExpr? parent);

        public abstract bool VisitExprDeleteOutput(ExprDeleteOutput exprDeleteOutput, IExpr? parent);

        public bool VisitExprList(ExprList exprList, IExpr? parent)
        {
            for (var index = 0; index < exprList.Expressions.Count; index++)
            {
                var expr = exprList.Expressions[index];

                if (index != 0 && exprList.Expressions[index-1] is not ExprStatement)
                {
                    this.FormattingWriter.Append(';');
                }
                if (index > 0) this.FormattingWriter.AppendBoundary(SqlRenderSite.Statement, 0);
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
                    this.FormattingWriter.Append(';');
                }

                if (index > 0) this.FormattingWriter.AppendBoundary(SqlRenderSite.Statement, 0);
                expr.Accept(this, exprList);
            }
            return true;
        }

        public abstract bool VisitExprCast(ExprCast exprCast, IExpr? parent);

        protected bool VisitExprCastCommon(ExprCast exprCast, IExpr? parent)
        {
            this.FormattingWriter.Append("CAST(");
            exprCast.Expression.Accept(this, exprCast);
            this.FormattingWriter.Append(" AS ");
            exprCast.SqlType.Accept(this, exprCast);
            this.FormattingWriter.Append(')');
            return true;
        }

        // Casts and SQL types
        
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

        // Statements and parameters

        public bool VisitExprStatement(ExprStatement statement, IExpr? parent)
        {
            statement.Statement.Accept(this.GetStatementSqlBuilder());
            return true;
        }

        public bool VisitExprParameter(ExprParameter exprParameter, IExpr? parent)
        {
            if (exprParameter.ReplacedValue is null)
            {
                throw new SqExpressException("Could not process pure parameter");
            }
            this._parameters ??= new List<DbParameterValue>();

            if (this.VisitExprParameter(
                    exprParameter,
                    this._parameterNumberOffset + this._parameters.Count + 1,
                    parent,
                    out var paramName
                ))
            {
                var dbParameterValue = exprParameter.ReplacedValue.Accept(this.GetDbParameterValueVisitorExtractor(), paramName);
                if (!dbParameterValue.HasValue)
                {
                    throw new SqExpressException("Unsupported parameter value");
                }

                this._parameters.Add(dbParameterValue.Value);
                return true;
            }

            return false;
        }

        // Formatting helpers

        protected bool AcceptBody(IExpr expression, IExpr? parent, SqlRenderSite site, int compactSpaces = 0)
        {
            using (this.FormattingWriter.BeginBody(site, compactSpaces))
            {
                return expression.Accept(this, parent);
            }
        }

        protected SqlFormattingWriter.Scope BeginParentheses(SqlRenderSite site, char start = '(', char end = ')')
        {
            this.FormattingWriter.Append(start);
            return this.FormattingWriter.BeginParentheses(site, end);
        }

        protected void AcceptItems(int count, SqlRenderSite site, Action<int> render, int compactSpaces = 0)
            => this.FormattingWriter.AppendItems(count, site, render, compactSpaces);

        protected void AcceptItems<T>(IReadOnlyList<T> list, IExpr? parent, SqlRenderSite site, int compactSpaces = 0)
            where T : IExpr
            => this.AcceptItems(list.Count, site, i => list[i].Accept(this, parent), compactSpaces);


        protected bool AcceptPar(char start, IExpr list, char end, IExpr? parent)
        {
            var site = list is IExprSubQuery
                ? parent is ExprCte ? SqlRenderSite.CteBody : SqlRenderSite.Subquery
                : list is ExprBoolean
                    ? SqlRenderSite.BooleanParentheses
                    : SqlRenderSite.Inline;

            using (this.BeginParentheses(site, start, end))
            {
                list.Accept(this, parent);
            }
            return true;
        }

        internal bool AcceptListComaSeparatedPar(char start, IReadOnlyList<IExpr> list, char end, IExpr? parent)
        {
            this.FormattingWriter.Append(start);
            this.AcceptListComaSeparated(list, parent);
            this.FormattingWriter.Append(end);

            return true;
        }

        protected bool AcceptListComaSeparated(IReadOnlyList<IExpr> list, IExpr? parent)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (i != 0)
                {
                    this.FormattingWriter.Append(',');
                }

                list[i].Accept(this, parent);
            }

            return true;
        }

        protected void AssertNotEmptyList<T>(IReadOnlyList<T> list, string errorText)
        {
            if (list == null || list.Count < 1)
            {
                throw new SqExpressException(errorText);
            }
        }

        // Embedded statements

        protected IStatementVisitor GetStatementSqlBuilder()
        {
            this._statementBuilder ??= this.CreateStatementSqlBuilder();
            return this._statementBuilder;
        }

        // Common table expressions

        protected void AddCteSlot(IExpr? parent)
        {
            if (!this.SupportsInlineCte() && parent == null && this._cteSlot == null)
            {
                this._cteSlot = this.FormattingWriter.Length;
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

            this.FormattingWriter.Append("WITH ");
            if (recursive)
            {
                this.AppendRecursiveCteKeyword();
            }

            for (var index = 0; index < result.Count; index++)
            {
                var exprCte = result[index];
                if (index != 0)
                {
                    this.FormattingWriter.Append(',');
                }

                if (index > 0) this.FormattingWriter.AppendBoundary(SqlRenderSite.CteSeparator, 0);
                this.AppendName(exprCte.Name);
                this.FormattingWriter.Append(" AS");
                this.AcceptPar('(', exprCte.CreateQuery(), ')', exprCte);
            }
        }

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
            this.FormattingWriter.AppendBoundary(SqlRenderSite.Clause, 1);
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
            cteBuilder.FormattingWriter.IndentLevel = this.FormattingWriter.IndentLevel;
            cteBuilder._parameterNumberOffset = this._parameters?.Count ?? 0;

            var list = this._uniqueCteCheck.Values.ToList();

            cteBuilder.AcceptCteExpressions(list);
            if (cteBuilder.ParameterValues?.Count > 0)
            {
                this._parameters ??= new List<DbParameterValue>();
                this._parameters.AddRange(cteBuilder.ParameterValues);
            }

            cteBuilder.FormattingWriter.AppendBoundary(SqlRenderSite.Clause, 0);
            return cteBuilder.ToString();
        }

        public override string ToString()
        {
            if (this._cachedFinalSql != null)
            {
                return this._cachedFinalSql;
            }

            var result = this.FormattingWriter.ToString();
            if (!this._dismissCteInject)
            {
                var cteResult = this.InjectCteToSlot(out var ctePosition);
                if (cteResult != null)
                {
                    result = result.Insert(ctePosition, cteResult);
                }
            }
            this._cachedFinalSql = result;
            return result;
        }

    }
}
