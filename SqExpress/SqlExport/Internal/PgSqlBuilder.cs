using System;
using System.Collections.Generic;
using System.Linq;
using SqExpress.SqlExport.Statement.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress.SqlExport.Internal
{
    internal partial class PgSqlBuilder : SqlBuilderBase, IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>
    {
        private const string InformationSchemaLowerInvariant = "information_schema";

        public PgSqlBuilder(SqlBuilderOptions? options = null) : base(options, new SqlAliasGenerator(), false)
        {
        }

        protected PgSqlBuilder(SqlBuilderOptions? options, SqlAliasGenerator aliasGenerator, bool dismissCteInject)
            : base(options, aliasGenerator, dismissCteInject)
        {
        }

        internal PgSqlBuilder(SqlBuilderOptions? options, SqlFormattingWriter formattingWriter)
            : base(options, formattingWriter, new SqlAliasGenerator(), false)
        {
        }

        // Dialect hooks

        protected override SqlBuilderBase CreateInstance(SqlAliasGenerator aliasGenerator, bool dismissCteInject)
        {
            return new PgSqlBuilder(this.Options, aliasGenerator, dismissCteInject);
        }

        protected override void EscapeStringLiteral(string literal)
        {
            this.FormattingWriter.AppendEscapedSingleQuote(literal);
        }

        protected override void AppendByteArrayLiteralPrefix()
        {
            this.FormattingWriter.Append("E'\\\\x");
        }

        protected override void AppendByteArrayLiteralSuffix()
        {
            this.FormattingWriter.Append('\'');
        }

        protected override void AppendSelectTop(ExprValue top, IExpr? parent)
        {
            //N/A
        }

        protected override void AppendSelectLimit(ExprValue top, IExpr? parent)
        {
            this.FormattingWriter.AppendClause("LIMIT", compactTrailingSpaces: 1);
            top.Accept(this, parent);
        }

        protected override bool ForceParenthesesForQueryExpressionPart(IExprSubQuery subQuery)
        {
            return subQuery switch
            {
                ExprQuerySpecification specification => !ReferenceEquals(specification.Top, null),
                _ => true
            };
        }

        protected override void AppendRecursiveCteKeyword()
        {
            this.FormattingWriter.Append("RECURSIVE ");
        }

        protected override bool SupportsInlineCte() => false;

        protected override bool VisitExprParameter(ExprParameter exprParameter, int paramNumber, IExpr? parent, out string? name)
        {
            name = null;
            this.FormattingWriter.Append('$');
            this.FormattingWriter.Append(paramNumber);
            return true;
        }

        protected override DbParameterValueVisitorExtractor GetDbParameterValueVisitorExtractor()
            => PgDbParameterValueVisitorExtractor.Instance;

        protected override void AppendUnicodePrefix(string str) { }

        protected override IStatementVisitor CreateStatementSqlBuilder()
            => new PgSqlStatementBuilder(this.Options.WithFormatting(null), this.FormattingWriter);

        // Values and operators

        public override bool VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral, IExpr? parent)
        {
            if (exprGuidLiteral.Value == null)
            {
                this.AppendNull();
                return true;
            }

            this.FormattingWriter.Append('\'');
            this.FormattingWriter.Append(exprGuidLiteral.Value.Value.ToString("D"));
            this.FormattingWriter.Append("'::uuid");

            return true;
        }

        public override bool VisitExprStringAgg(ExprStringAgg exprStringAgg, IExpr? parent)
        {
            this.FormattingWriter.Append("STRING_AGG(");
            exprStringAgg.Expression.Accept(this, exprStringAgg);
            this.FormattingWriter.Append(',');
            exprStringAgg.Separator.Accept(this, exprStringAgg);
            if (exprStringAgg.OrderBy != null)
            {
                this.FormattingWriter.AppendClause("ORDER BY", compactTrailingSpaces: 1);
                exprStringAgg.OrderBy.Accept(this, exprStringAgg);
            }
            this.FormattingWriter.Append(')');
            return true;
        }

        public override bool VisitExprDateTimeOffsetLiteral(ExprDateTimeOffsetLiteral dateTimeLiteral, IExpr? arg)
        {
            if (!dateTimeLiteral.Value.HasValue)
            {
                this.AppendNull();
            }
            else
            {
                this.FormattingWriter.Append('\'');
                this.FormattingWriter.Append(dateTimeLiteral.Value.Value.ToString("O"));
                this.FormattingWriter.Append("'::timestamptz");
            }

            return true;
        }

        public override bool VisitExprDateTimeLiteral(ExprDateTimeLiteral dateTimeLiteral, IExpr? parent)
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
                this.FormattingWriter.Append("'::timestamp");
            }

            return true;
        }

        public override bool VisitExprBoolLiteral(ExprBoolLiteral boolLiteral, IExpr? parent)
        {
            if (boolLiteral.Value.HasValue)
            {
                this.FormattingWriter.Append(boolLiteral.Value.Value ? "true" : "false");
            }
            else
            {
                this.AppendNull();
            }

            return true;
        }

        public override bool VisitExprStringConcat(ExprStringConcat exprStringConcat, IExpr? parent)
        {
            exprStringConcat.Left.Accept(this, exprStringConcat);
            this.FormattingWriter.Append("||");
            exprStringConcat.Right.Accept(this, exprStringConcat);
            return true;
        }

        // Queries and table sources

        public override bool VisitExprLateralCrossedTable(ExprLateralCrossedTable exprCrossedTable, IExpr? parent)
        {
            exprCrossedTable.Left.Accept(this, exprCrossedTable);
            this.FormattingWriter.AppendClause(
                exprCrossedTable.Outer ? "LEFT JOIN LATERAL" : "CROSS JOIN LATERAL",
                SqlRenderSite.Join,
                compactTrailingSpaces: exprCrossedTable.Outer ? 0 : 1);
            exprCrossedTable.Right.Accept(this, exprCrossedTable);
            if (exprCrossedTable.Outer)
            {
                this.FormattingWriter.Append(" ON TRUE");
            }
            return true;
        }

        public override bool VisitExprOffsetFetch(ExprOffsetFetch exprOffsetFetch, IExpr? parent)
        {
            return this.VisitExprOffsetFetchCommon(exprOffsetFetch, parent);
        }

        public override bool VisitExprTempTableName(ExprTempTableName tempTableName, IExpr? parent)
        {
            this.AppendName(tempTableName.Name);
            return true;
        }

        public override bool VisitExprDbSchema(ExprDbSchema exprDbSchema, IExpr? parent)
        {
            return this.VisitExprDbSchemaCommon(exprDbSchema, parent);
        }

        public override bool VisitExprDerivedTableValues(ExprDerivedTableValues derivedTableValues, IExpr? parent)
        {
            return this.VisitExprDerivedTableValuesCommon(derivedTableValues, parent);
        }

        // DML

        public override bool VisitExprInsert(ExprInsert exprInsert, IExpr? parent)
        {
            this.AddCteSlot(parent);
            this.GenericInsert(exprInsert, null, null);
            return true;
        }

        public override bool VisitExprInsertOutput(ExprInsertOutput exprInsertOutput, IExpr? parent)
        {
            this.AddCteSlot(parent);
            this.GenericInsert(exprInsertOutput.Insert,
                null,
                () =>
                {
                    exprInsertOutput.OutputColumns.AssertNotEmpty("INSERT OUTPUT cannot be empty");
                    this.FormattingWriter.AppendClause("RETURNING", compactLeadingSpaces: 2);
                    this.AcceptItems(exprInsertOutput.OutputColumns, exprInsertOutput, SqlRenderSite.Output, 1);
                });
            return true;
        }

        public override bool VisitExprInsertQuery(ExprInsertQuery exprInsertQuery, IExpr? parent)
        {
            return this.VisitExprInsertQueryCommon(exprInsertQuery, parent);
        }

        public override bool VisitExprIdentityInsert(ExprIdentityInsert exprIdentityInsert, IExpr? parent)
        {
            if (exprIdentityInsert.IdentityColumns.Count < 1)
            {
                return exprIdentityInsert.Insert.Accept(this, exprIdentityInsert);
            }

            const string insertCteName = "__sqexpress_identity_insert";

            this.FormattingWriter.Append("WITH ");
            this.AppendName(insertCteName);
            this.FormattingWriter.Append(" AS ");
            using (this.BeginParentheses(SqlRenderSite.CteBody))
            {

            this.AddCteSlot(parent);
            this.GenericInsert(
                exprIdentityInsert.Insert,
                () => this.FormattingWriter.Append(" OVERRIDING SYSTEM VALUE"),
                () =>
                {
                    this.FormattingWriter.AppendClause("RETURNING");
                    this.AcceptItems(exprIdentityInsert.IdentityColumns, exprIdentityInsert, SqlRenderSite.Output, 1);
                }
            );

            }
            this.FormattingWriter.AppendClause("SELECT");

            var exprTableSource = new ExprTable(exprIdentityInsert.Insert.Target, null);
            var exprInsertedSource = new ExprTable(
                new ExprTableFullName(null, new ExprTableName(insertCteName)),
                null
            );
            this.AcceptItems(exprIdentityInsert.IdentityColumns.Count, SqlRenderSite.Select, i =>
            {
                var column = exprIdentityInsert.IdentityColumns[i];
                this.FormattingWriter.Append("setval(pg_get_serial_sequence('");
                exprIdentityInsert.Insert.Target.Accept(this, exprIdentityInsert.Insert);
                this.FormattingWriter.Append("','");
                this.EscapeStringLiteral(column.Name);
                this.FormattingWriter.Append("'),GREATEST(");
                using (this.BeginParentheses(SqlRenderSite.Subquery))
                {
                SqQueryBuilder.Select(SqQueryBuilder.Max(column.WithSource(null)))
                    .From(exprTableSource)
                    .Done()
                    .Accept(this, exprIdentityInsert);
                }
                this.FormattingWriter.Append(',');
                using (this.BeginParentheses(SqlRenderSite.Subquery))
                {
                SqQueryBuilder.Select(SqQueryBuilder.Max(column.WithSource(null)))
                    .From(exprInsertedSource)
                    .Done()
                    .Accept(this, exprIdentityInsert);
                }
                this.FormattingWriter.Append("))");
            }, 1);

            this.FormattingWriter.AppendClause("FROM", compactTrailingSpaces: 1);
            this.AppendName(insertCteName);
            this.FormattingWriter.AppendClause("LIMIT 1");

            return true;
        }

        public override bool VisitExprUpdate(ExprUpdate exprUpdate, IExpr? parent)
        {
            this.AddCteSlot(parent);

            this.AssertNotEmptyList(exprUpdate.SetClause, "'UPDATE' statement should have at least one set clause");

            this.FormattingWriter.Append("UPDATE ");
            exprUpdate.Target.Accept(this, exprUpdate);
            this.FormattingWriter.AppendClause("SET");
            this.AcceptItems(exprUpdate.SetClause.Count, SqlRenderSite.Set, i =>
            {
                var setClause = exprUpdate.SetClause[i];
                setClause.Column.ColumnName.Accept(this, exprUpdate);
                this.FormattingWriter.Append('=');
                setClause.Value.Accept(this, exprUpdate);

            }, 1);

            ExprBoolean? sourceFilter = null;
            if (exprUpdate.Source != null)
            {
                IReadOnlyList<IExprTableSource> tables;
                (tables, sourceFilter) = exprUpdate.Source.ToTableMultiplication();
                this.AssertNotEmptyList(tables, "List of tables in 'UPDATE' statement cannot be empty");
                if (tables.Count > 1)
                {
                    this.FormattingWriter.AppendClause("FROM", compactTrailingSpaces: 1);
                    int itemAppendCount = 0;
                    for (int i = 0; i < tables.Count; i++)
                    {
                        var source = tables[i];

                        if (source is ExprTable sTable && sTable.Equals(exprUpdate.Target))
                        {
                            continue;
                        }

                        if (itemAppendCount != 0)
                        {
                            this.FormattingWriter.Append(',');
                        }

                        source.Accept(this, exprUpdate);
                        itemAppendCount++;
                    }

                    if (itemAppendCount >= tables.Count)
                    {
                        throw new SqExpressException("Could not found target table in 'UPDATE' statement");
                    }
                }
            }

            var filter = Helpers.CombineNotNull(sourceFilter, exprUpdate.Filter, (l, r) => l & r);

            if (filter != null)
            {
                this.FormattingWriter.AppendClause("WHERE");
                this.AcceptBody(filter, exprUpdate, SqlRenderSite.Where, 1);
            }
            return true;
        }

        public override bool VisitExprDelete(ExprDelete exprDelete, IExpr? parent)
        {
            this.AddCteSlot(parent);

            this.FormattingWriter.Append("DELETE FROM ");
            exprDelete.Target.Accept(this, exprDelete);

            ExprBoolean? sourceFilter = null;
            if (exprDelete.Source != null)
            {
                IReadOnlyList<IExprTableSource> tables;
                (tables, sourceFilter) = exprDelete.Source.ToTableMultiplication();

                this.AssertNotEmptyList(tables, "List of tables in 'DELETE' statement cannot be empty");

                if (tables.Count > 1)
                {
                    this.FormattingWriter.AppendClause("USING", compactTrailingSpaces: 1);
                    int itemAppendCount = 0;
                    for (int i = 0; i < tables.Count; i++)
                    {
                        var source = tables[i];

                        if (source is ExprTable sTable && sTable.Equals(exprDelete.Target))
                        {
                            continue;
                        }

                        if (itemAppendCount != 0)
                        {
                            this.FormattingWriter.Append(',');
                        }

                        source.Accept(this, exprDelete);
                        itemAppendCount++;
                    }

                    if (itemAppendCount >= tables.Count)
                    {
                        throw new SqExpressException("Could not found target table in 'DELETE' statement");
                    }
                }
            }

            var filter = Helpers.CombineNotNull(sourceFilter, exprDelete.Filter, (l,r) => l & r);

            if (filter != null)
            {
                this.FormattingWriter.AppendClause("WHERE");
                this.AcceptBody(filter, exprDelete, SqlRenderSite.Where, 1);
            }
            return true;
        }

        public override bool VisitExprDeleteOutput(ExprDeleteOutput exprDeleteOutput, IExpr? parent)
        {
            this.AddCteSlot(parent);

            exprDeleteOutput.Delete.Accept(this, exprDeleteOutput);
            this.AssertNotEmptyList(exprDeleteOutput.OutputColumns, "Output list in 'DELETE' statement cannot be empty");

            this.FormattingWriter.AppendClause("RETURNING");

            this.AcceptItems(exprDeleteOutput.OutputColumns, exprDeleteOutput, SqlRenderSite.Output, 1);

            return true;
        }

        public override bool VisitExprCast(ExprCast exprCast, IExpr? parent)
        {
            return this.VisitExprCastCommon(exprCast, parent);
        }

        public override bool VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean, IExpr? parent)
        {
            this.FormattingWriter.Append("bool");
            return true;
        }

        public override bool VisitExprTypeByte(ExprTypeByte exprTypeByte, IExpr? parent)
        {
            throw new NotSupportedException("PostgresSQL does not support 1 byte numeric");
        }

        public override bool VisitExprTypeByteArray(ExprTypeByteArray exprTypeByte, IExpr? arg)
        {
            this.FormattingWriter.Append("bytea");
            return true;
        }

        public override bool VisitExprTypeFixSizeByteArray(ExprTypeFixSizeByteArray exprTypeFixSizeByteArray, IExpr? arg)
        {
            //PostgreSQL does not support binary type with fixed length
            this.FormattingWriter.Append("bytea");
            return true;
        }

        public override bool VisitExprTypeInt16(ExprTypeInt16 exprTypeInt16, IExpr? parent)
        {
            this.FormattingWriter.Append("int2");
            return true;
        }

        public override bool VisitExprTypeInt32(ExprTypeInt32 exprTypeInt32, IExpr? parent)
        {
            this.FormattingWriter.Append("int4");
            return true;
        }

        public override bool VisitExprTypeInt64(ExprTypeInt64 exprTypeInt64, IExpr? parent)
        {
            this.FormattingWriter.Append("int8");
            return true;
        }

        public override bool VisitExprTypeDecimal(ExprTypeDecimal exprTypeDecimal, IExpr? parent)
        {
            this.FormattingWriter.Append("decimal");
            if (exprTypeDecimal.PrecisionScale.HasValue)
            {
                this.FormattingWriter.Append('(');
                this.FormattingWriter.Append(exprTypeDecimal.PrecisionScale.Value.Precision);
                if (exprTypeDecimal.PrecisionScale.Value.Scale.HasValue)
                {
                    this.FormattingWriter.Append(',');
                    this.FormattingWriter.Append(exprTypeDecimal.PrecisionScale.Value.Scale.Value);
                }
                this.FormattingWriter.Append(')');
            }
            return true;
        }

        public override bool VisitExprTypeDouble(ExprTypeDouble exprTypeDouble, IExpr? parent)
        {
            this.FormattingWriter.Append("float8");
            return true;
        }

        public override bool VisitExprTypeDateTime(ExprTypeDateTime exprTypeDateTime, IExpr? parent)
        {
            if (exprTypeDateTime.IsDate)
            {
                this.FormattingWriter.Append("date");
            }
            else
            {
                this.FormattingWriter.Append("timestamp");
            }

            return true;
        }

        public override bool VisitExprTypeDateTimeOffset(ExprTypeDateTimeOffset exprTypeDateTimeOffset, IExpr? arg)
        {
            this.FormattingWriter.Append("timestamp with time zone");
            return true;
        }

        public override bool VisitExprTypeGuid(ExprTypeGuid exprTypeGuid, IExpr? parent)
        {
            this.FormattingWriter.Append("uuid");
            return true;
        }

        public override bool VisitExprTypeString(ExprTypeString exprTypeString, IExpr? parent)
        {
            this.FormattingWriter.Append("character varying");
            if (exprTypeString.Size.HasValue)
            {
                this.FormattingWriter.Append('(');
                this.FormattingWriter.Append(exprTypeString.Size.Value);
                this.FormattingWriter.Append(')');
            }
            return true;
        }

        public override bool VisitExprTypeFixSizeString(ExprTypeFixSizeString exprTypeFixSizeString, IExpr? arg)
        {
            this.FormattingWriter.Append("character");
            this.FormattingWriter.Append('(');
            this.FormattingWriter.Append(exprTypeFixSizeString.Size.ToString());
            this.FormattingWriter.Append(')');
            return true;
        }

        public override bool VisitExprTypeXml(ExprTypeXml exprTypeXml, IExpr? arg)
        {
            this.FormattingWriter.Append("xml");
            return true;
        }

        public override bool VisitExprPortableScalarFunction(ExprPortableScalarFunction exprPortableScalarFunction, IExpr? arg)
        {
            return exprPortableScalarFunction.PortableFunction.Accept(this, exprPortableScalarFunction);
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseLen(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionSingleArg("CHAR_LENGTH", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseNullIf(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionTwoArgs("NULLIF", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseAbs(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionSingleArg("ABS", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseLower(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionSingleArg("LOWER", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseUpper(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionSingleArg("UPPER", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseTrim(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionSingleArg("TRIM", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseLTrim(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionSingleArg("LTRIM", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseRTrim(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionSingleArg("RTRIM", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseReplace(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionThreeArgs("REPLACE", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseSubstring(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionThreeArgs("SUBSTRING", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseRound(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionTwoArgs("ROUND", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseFloor(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionSingleArg("FLOOR", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseCeiling(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionSingleArg("CEIL", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseDataLen(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionSingleArg("OCTET_LENGTH", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseYear(ExprPortableScalarFunction ctx)
        {
            this.AppendExtractSingleArg("YEAR", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseMonth(ExprPortableScalarFunction ctx)
        {
            this.AppendExtractSingleArg("MONTH", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseDay(ExprPortableScalarFunction ctx)
        {
            this.AppendExtractSingleArg("DAY", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseHour(ExprPortableScalarFunction ctx)
        {
            this.AppendExtractSingleArg("HOUR", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseMinute(ExprPortableScalarFunction ctx)
        {
            this.AppendExtractSingleArg("MINUTE", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseSecond(ExprPortableScalarFunction ctx)
        {
            this.AppendExtractSingleArg("SECOND", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseIndexOf(ExprPortableScalarFunction ctx)
        {
            this.AssertArgumentsCount(ctx.Arguments, 2, ctx.PortableFunction);
            this.FormattingWriter.Append("STRPOS(");
            ctx.Arguments![1].Accept(this, ctx);
            this.FormattingWriter.Append(',');
            ctx.Arguments[0].Accept(this, ctx);
            this.FormattingWriter.Append(')');
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseLeft(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionTwoArgs("LEFT", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseRight(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionTwoArgs("RIGHT", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseRepeat(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionTwoArgs("REPEAT", ctx.Arguments, ctx);
            return true;
        }

        private void AppendExtractSingleArg(string part, IReadOnlyList<ExprValue>? arguments, ExprPortableScalarFunction expr)
        {
            this.AssertArgumentsCount(arguments, 1, expr.PortableFunction);

            var value = arguments![0];
            if (value is ExprDateTimeLiteral)
            {
                value = SqQueryBuilder.Cast(value, SqQueryBuilder.SqlType.DateTime());
            }
            else if (value is ExprDateTimeOffsetLiteral)
            {
                value = SqQueryBuilder.Cast(value, SqQueryBuilder.SqlType.DateTimeOffset);
            }

            this.FormattingWriter.Append("EXTRACT(");
            this.FormattingWriter.Append(part);
            this.FormattingWriter.AppendClause("FROM", compactTrailingSpaces: 1);
            value.Accept(this, expr);
            this.FormattingWriter.Append(')');
        }

        public override bool VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull, IExpr? parent)
        {
            SqQueryBuilder.Coalesce(exprFuncIsNull.Test, exprFuncIsNull.Alt).Accept(this, exprFuncIsNull);
            return true;
        }

        public override bool VisitExprGetDate(ExprGetDate exprGetDate, IExpr? parent)
        {
            this.FormattingWriter.Append("now()");
            return true;
        }

        public override bool VisitExprGetUtcDate(ExprGetUtcDate exprGetUtcDate, IExpr? parent)
        {
            this.FormattingWriter.Append("now() at time zone 'utc'");
            return true;
        }

        public override bool VisitExprDateAdd(ExprDateAdd exprDateAdd, IExpr? arg)
        {
            var datePart = exprDateAdd.DatePart switch
            {
                DateAddDatePart.Year => "y",
                DateAddDatePart.Month => "month",
                DateAddDatePart.Day => "d",
                DateAddDatePart.Week => "w",
                DateAddDatePart.Hour => "h",
                DateAddDatePart.Minute => "m",
                DateAddDatePart.Second => "s",
                DateAddDatePart.Millisecond => "ms",
                _ => throw new ArgumentOutOfRangeException()
            };

            char sign = exprDateAdd.Number < 0 ? '-' : '+';
            int val = exprDateAdd.Number < 0 ? exprDateAdd.Number * -1 : exprDateAdd.Number;

            exprDateAdd.Date.Accept(this, exprDateAdd);
            this.FormattingWriter.Append(sign);
            this.FormattingWriter.Append("INTERVAL'");
            this.FormattingWriter.Append(val);
            this.FormattingWriter.Append(datePart);
            this.FormattingWriter.Append('\'');

            return true;
        }

        public override bool VisitExprDateDiff(ExprDateDiff exprDateDiff, IExpr? arg)
        {
            ExprValue result;

            if (exprDateDiff.DatePart == DateDiffDatePart.Year)
            {
                result = DatePart("YEAR", exprDateDiff.EndDate) - DatePart("YEAR", exprDateDiff.StartDate);

            }
            else if (exprDateDiff.DatePart == DateDiffDatePart.Month)
            {
                var year = DatePart("YEAR", exprDateDiff.EndDate) - DatePart("YEAR", exprDateDiff.StartDate);
                var month = DatePart("MONTH", exprDateDiff.EndDate) - DatePart("MONTH", exprDateDiff.StartDate);

                result = year * 12 + month;
            }
            else
            {
                string? truncInterval;
                string? partInterval;

                int? divider = null;
                int? factor = null;
                switch (exprDateDiff.DatePart)
                {
                    case DateDiffDatePart.Day:
                        truncInterval = partInterval = "DAY";
                        break;
                    case DateDiffDatePart.Hour:
                        truncInterval = "HOUR";
                        partInterval = "EPOCH";
                        divider = 60 * 60;
                        break;
                    case DateDiffDatePart.Minute:
                        truncInterval = "MINUTE";
                        partInterval = "EPOCH";
                        divider = 60;
                        break;
                    case DateDiffDatePart.Second:
                        truncInterval = "SECOND";
                        partInterval = "EPOCH";
                        break;
                    case DateDiffDatePart.Millisecond:
                        truncInterval = "MILLISECOND";
                        partInterval = "EPOCH";
                        factor = 1000;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var diff = Diff(truncInterval, exprDateDiff.StartDate, exprDateDiff.EndDate);

                result = DatePart(partInterval, diff);

                if (divider != null)
                {
                    result /= divider.Value;
                }

                if (factor != null)
                {
                    result *= factor.Value;
                }
            }

            result = SqQueryBuilder.Cast(result, SqQueryBuilder.SqlType.Int32);
            result.Accept(this, exprDateDiff);

            return true;

            static ExprValue Diff(string interval, ExprValue start, ExprValue end)
                => DateTrunc(interval, end) - DateTrunc(interval, start);

            static ExprValue DateTrunc(string interval, ExprValue value)
                => SqQueryBuilder.ScalarFunctionSys("DATE_TRUNC", IntervalLiteral(interval), EnsureLiteral(value));

            static ExprValue DatePart(string interval, ExprValue value)
                => SqQueryBuilder.ScalarFunctionSys("DATE_PART", IntervalLiteral(interval), EnsureLiteral(value));

            static ExprValue IntervalLiteral(string interval)
                => SqQueryBuilder.UnsafeValue($"'{interval}'");

            static ExprValue EnsureLiteral(ExprValue value)
            {
                if (value is ExprLiteral)
                {
                    value = SqQueryBuilder.Cast(value, SqQueryBuilder.SqlType.DateTime());
                }
                return value;
            }
        }

        public override bool VisitExprColumnName(ExprColumnName columnName, IExpr? parent)
        {
            if (parent is TableColumn tableColumn && tableColumn.Table.FullName.LowerInvariantSchemaName == InformationSchemaLowerInvariant)
            {
                this.AppendNameNoQuotas(columnName.Name, null);
            }
            else
            {
                this.VisitExprColumnNameCommon(columnName);
            }
            return true;
        }

        public override bool VisitExprTableFullName(ExprTableFullName exprTableFullName, IExpr? parent)
        {
            IExprTableFullName tableFullName = exprTableFullName;
            if (tableFullName.LowerInvariantSchemaName == InformationSchemaLowerInvariant && IsSafeName(tableFullName.TableName))
            {
                //Preventing quoting
                this.FormattingWriter.Append(exprTableFullName.DbSchema!.Schema.Name);
                this.FormattingWriter.Append('.');
                this.FormattingWriter.Append(exprTableFullName.TableName.Name);
                return true;
            }

            return this.VisitExprTableFullNameCommon(exprTableFullName, parent);

            bool IsSafeName(string name)
            {
                for (var index = 0; index < name.Length; index++)
                {
                    char ch = name[index];

                    if (index == 0 && !char.IsLetter(ch))
                    {
                        return false;
                    }

                    if (!char.IsLetter(ch) && ch != '_')
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public override void AppendName(string name, char? prefix = null)
        {
            this.FormattingWriter.Append('\"');
            this.AppendNameNoQuotas(name, prefix);
            this.FormattingWriter.Append('\"');
        }

        private void AppendNameNoQuotas(string name, char? prefix)
        {
            if (prefix.HasValue)
            {
                this.FormattingWriter.Append(prefix.Value);
            }
                this.FormattingWriter.AppendEscapedDoubleQuote(name);
        }

        public override bool VisitExprMerge(ExprMerge merge, IExpr? parent)
        {
            var sourceAlias = merge.Source.Alias?.Alias
                ?? throw new SqExpressException("MERGE source should have an alias");
            var sourceColumns = ExtractSourceColumns(merge.Source);
            if (sourceColumns.Count < 1)
            {
                throw new SqExpressException("Could not determine MERGE source columns");
            }

            var sourceCteName = "__sqexpress_merge_source";
            var sourceTable = new ExprTable(
                new ExprTableFullName(null, new ExprTableName(sourceCteName)),
                merge.Source.Alias
            );

            var actionCtes = new List<(string CteName, IExprExec Statement)>();

            var matchedAction = BuildWhenMatchedAction(merge, sourceTable);
            if (matchedAction != null)
            {
                actionCtes.Add(("__sqexpress_merge_matched", matchedAction));
            }

            var notMatchedByTargetAction = BuildNotMatchedByTargetAction(merge, sourceTable, sourceAlias);
            if (notMatchedByTargetAction != null)
            {
                actionCtes.Add(("__sqexpress_merge_not_matched_by_target", notMatchedByTargetAction));
            }

            var notMatchedBySourceAction = BuildNotMatchedBySourceAction(merge, sourceTable);
            if (notMatchedBySourceAction != null)
            {
                actionCtes.Add(("__sqexpress_merge_not_matched_by_source", notMatchedBySourceAction));
            }

            this.FormattingWriter.Append("WITH ");
            this.AppendName(sourceCteName);
            this.AcceptListComaSeparatedPar('(', sourceColumns, ')', merge);
            this.FormattingWriter.Append(" AS");
            using (this.BeginParentheses(SqlRenderSite.CteBody))
            {
            if (merge.Source is ExprDerivedTableValues sourceValues)
            {
                sourceValues.Values.Accept(this, sourceValues);
            }
            else
            {
                merge.Source.CreateSubQuery().Accept(this, merge.Source);
            }
            }

            for (var i = 0; i < actionCtes.Count; i++)
            {
                var action = actionCtes[i];
                this.FormattingWriter.Append(',');
                this.FormattingWriter.AppendBoundary(SqlRenderSite.CteSeparator, 0);
                this.AppendName(action.CteName);
                this.FormattingWriter.Append(" AS");
                using (this.BeginParentheses(SqlRenderSite.CteBody))
                {
                    action.Statement.Accept(this, merge);
                    this.FormattingWriter.AppendClause("RETURNING");
                    using (this.FormattingWriter.BeginBody(SqlRenderSite.Output, 1)) this.FormattingWriter.Append('1');
                }
            }

            this.FormattingWriter.AppendClause("SELECT");
            this.AcceptItems(Math.Max(1, actionCtes.Count), SqlRenderSite.Select, i =>
            {
            if (actionCtes.Count < 1)
            {
                this.FormattingWriter.Append('1');
            }
            else
            {
                using (this.BeginParentheses(SqlRenderSite.Subquery))
                {
                    this.FormattingWriter.Append("SELECT");
                    using (this.FormattingWriter.BeginBody(SqlRenderSite.Select, 1)) this.FormattingWriter.Append("COUNT(*)");
                    this.FormattingWriter.AppendClause("FROM", compactTrailingSpaces: 1);
                    this.AppendName(actionCtes[i].CteName);
                }
            }
            }, 1);

            return true;
        }

        public override bool VisitExprMergeOutput(ExprMergeOutput mergeOutput, IExpr? parent)
            => VisitMergeNotSupported();

        public override bool VisitExprMergeMatchedUpdate(ExprMergeMatchedUpdate mergeMatchedUpdate, IExpr? parent)
            => VisitMergeNotSupported();

        public override bool VisitExprMergeMatchedDelete(ExprMergeMatchedDelete mergeMatchedDelete, IExpr? parent)
            => VisitMergeNotSupported();

        public override bool VisitExprExprMergeNotMatchedInsert(ExprExprMergeNotMatchedInsert exprMergeNotMatchedInsert, IExpr? parent)
            => VisitMergeNotSupported();

        public override bool VisitExprExprMergeNotMatchedInsertDefault(ExprExprMergeNotMatchedInsertDefault exprExprMergeNotMatchedInsertDefault, IExpr? parent)
            => VisitMergeNotSupported();

        private static IExprExec? BuildWhenMatchedAction(ExprMerge merge, ExprTable sourceTable)
        {
            if (merge.WhenMatched == null)
            {
                return null;
            }

            if (merge.WhenMatched is ExprMergeMatchedUpdate update)
            {
                return SqQueryBuilder
                    .Update(merge.TargetTable)
                    .Set(update.Set)
                    .From(merge.TargetTable)
                    .InnerJoin(sourceTable, merge.On)
                    .Where(update.And);
            }

            if (merge.WhenMatched is ExprMergeMatchedDelete delete)
            {
                ExprBoolean filter = SqQueryBuilder.Exists(
                    SqQueryBuilder.SelectOne()
                        .From(sourceTable)
                        .Where(merge.On)
                );

                if (delete.And != null)
                {
                    filter = filter & delete.And;
                }

                return SqQueryBuilder.Delete(merge.TargetTable).From(merge.TargetTable).Where(filter);
            }

            throw new SqExpressException($"Unknown type: '{merge.WhenMatched.GetType().Name}'");
        }

        private static IExprExec? BuildNotMatchedByTargetAction(ExprMerge merge, ExprTable sourceTable, IExprAlias sourceAlias)
        {
            if (merge.WhenNotMatchedByTarget == null)
            {
                return null;
            }

            if (merge.WhenNotMatchedByTarget is ExprExprMergeNotMatchedInsert insert)
            {
                var filter = !SqQueryBuilder.Exists(
                    SqQueryBuilder.SelectOne()
                        .From(merge.TargetTable)
                        .Where(merge.On)
                );

                if (insert.And != null)
                {
                    filter = filter & insert.And;
                }

                var sourceValuesList = insert.Values.SelectToReadOnlyList(i =>
                    i is ExprValue v
                        ? v
                        : throw new SqExpressException("DEFAULT value cannot be used in MERGE polyfill"));

                return SqQueryBuilder.InsertInto(merge.TargetTable, insert.Columns)
                    .From(
                        SqQueryBuilder.Select(sourceValuesList)
                            .From(sourceTable)
                            .Where(filter)
                    );
            }

            if (merge.WhenNotMatchedByTarget is ExprExprMergeNotMatchedInsertDefault insertDefault)
            {
                var keys = ExtractKeys(merge, sourceAlias);

                var filter = !SqQueryBuilder.Exists(
                    SqQueryBuilder.SelectOne()
                        .From(merge.TargetTable)
                        .Where(merge.On)
                );

                if (insertDefault.And != null)
                {
                    filter = filter & insertDefault.And;
                }

                return SqQueryBuilder.InsertInto(merge.TargetTable, keys.TargetKeys)
                    .From(
                        SqQueryBuilder.Select(keys.SourceKeys)
                            .From(sourceTable)
                            .Where(filter)
                    );
            }

            throw new SqExpressException($"Unknown type: '{merge.WhenNotMatchedByTarget.GetType().Name}'");
        }

        private static IExprExec? BuildNotMatchedBySourceAction(ExprMerge merge, ExprTable sourceTable)
        {
            if (merge.WhenNotMatchedBySource == null)
            {
                return null;
            }

            if (merge.WhenNotMatchedBySource is ExprMergeMatchedDelete delete)
            {
                ExprBoolean filter = !SqQueryBuilder.Exists(
                    SqQueryBuilder.SelectOne()
                        .From(sourceTable)
                        .Where(merge.On)
                );

                if (delete.And != null)
                {
                    filter = filter & delete.And;
                }

                return SqQueryBuilder.Delete(merge.TargetTable).From(merge.TargetTable).Where(filter);
            }

            if (merge.WhenNotMatchedBySource is ExprMergeMatchedUpdate update)
            {
                ExprBoolean filter = !SqQueryBuilder.Exists(
                    SqQueryBuilder.SelectOne()
                        .From(sourceTable)
                        .Where(merge.On)
                );

                if (update.And != null)
                {
                    filter = filter & update.And;
                }

                return SqQueryBuilder.Update(merge.TargetTable).Set(update.Set).Where(filter);
            }

            throw new SqExpressException($"Unknown type: '{merge.WhenNotMatchedBySource.GetType().Name}'");
        }

        private static IReadOnlyList<ExprColumnName> ExtractSourceColumns(IExprTableSource source)
        {
            var result = new List<ExprColumnName>();
            var selectings = source.ExtractSelecting();
            for (var i = 0; i < selectings.Count; i++)
            {
                if (selectings[i] is IExprNamedSelecting named && !string.IsNullOrWhiteSpace(named.OutputName))
                {
                    result.Add(new ExprColumnName(named.OutputName!));
                    continue;
                }

                result.Add(new ExprColumnName($"Expr{i + 1}"));
            }

            return result;
        }

        private static ExtractKeysResult ExtractKeys(ExprMerge merge, IExprAlias sourceAlias)
        {
            var targetAlias = merge.TargetTable.Alias?.Alias ?? throw new SqExpressException("Target table should have an alias");

            var sourceColumns = new List<ExprColumnName>();
            var targetColumns = new List<ExprColumnName>();

            var eqs = merge.On.SyntaxTree().DescendantsAndSelf().OfType<ExprBooleanEq>();
            foreach (var eq in eqs)
            {
                if (eq.Left is ExprColumn left
                    && eq.Right is ExprColumn right
                    && left.Source is ExprTableAlias leftAlias
                    && right.Source is ExprTableAlias rightAlias)
                {
                    if (rightAlias.Alias.Equals(sourceAlias) && leftAlias.Alias.Equals(targetAlias))
                    {
                        targetColumns.Add(left.ColumnName);
                        sourceColumns.Add(right.ColumnName);
                    }
                    else if (leftAlias.Alias.Equals(sourceAlias) && rightAlias.Alias.Equals(targetAlias))
                    {
                        targetColumns.Add(right.ColumnName);
                        sourceColumns.Add(left.ColumnName);
                    }
                }
            }

            return new ExtractKeysResult(targetColumns, sourceColumns);
        }

        private readonly struct ExtractKeysResult
        {
            public readonly IReadOnlyList<ExprColumnName> TargetKeys;
            public readonly IReadOnlyList<ExprColumnName> SourceKeys;

            public ExtractKeysResult(IReadOnlyList<ExprColumnName> targetKeys, IReadOnlyList<ExprColumnName> sourceKeys)
            {
                this.TargetKeys = targetKeys;
                this.SourceKeys = sourceKeys;
            }
        }

        private static bool VisitMergeNotSupported() =>
            throw new SqExpressException("Pg SQL does not support MERGE expression");
    }
}
