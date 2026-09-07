using System;
using System.Collections.Generic;
using System.Linq;
using SqExpress.SqlExport.Statement.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress.SqlExport.Internal
{
    internal class TSqlBuilder : SqlBuilderBase, IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>
    {
        public TSqlBuilder(SqlBuilderOptions? options = null) : base(options, new SqlAliasGenerator(), false)
        {
        }

        private TSqlBuilder(SqlBuilderOptions? options, SqlAliasGenerator aliasGenerator, bool dismissCteInject)
            : base(options, aliasGenerator, dismissCteInject)
        {
        }

        internal TSqlBuilder(SqlBuilderOptions? options, SqlFormattingWriter formattingWriter)
            : base(options, formattingWriter, new SqlAliasGenerator(), false)
        {
        }

        // Dialect hooks

        protected override SqlBuilderBase CreateInstance(SqlAliasGenerator aliasGenerator, bool dismissCteInject)
        {
            return new TSqlBuilder(this.Options, aliasGenerator, dismissCteInject);
        }

        protected override void EscapeStringLiteral(string literal)
        {
            this.FormattingWriter.AppendEscapedSingleQuote(literal);
        }

        protected override void AppendByteArrayLiteralPrefix()
        {
            this.FormattingWriter.Append('0');
            this.FormattingWriter.Append('x');
        }

        protected override void AppendByteArrayLiteralSuffix()
        {
        }

        protected override void AppendSelectTop(ExprValue top, IExpr? parent)
        {
            this.FormattingWriter.Append(" TOP ");
            top.Accept(this, top);
        }

        protected override void AppendSelectLimit(ExprValue top, IExpr? parent)
        {
            if (parent is ExprSelectOffsetFetch)
            {
                this.FormattingWriter.AppendClause("FETCH NEXT", compactTrailingSpaces: 1);
                top.Accept(this, parent);
                this.FormattingWriter.Append(" ROW ONLY");
            }
        }

        protected override bool ShouldAppendSelectTop(IExpr? parent) => !(parent is ExprSelectOffsetFetch);

        protected override void VisitExprUnorderedOffsetFetch(ExprOffsetFetch exprOffsetFetch, ExprSelectOffsetFetch parent)
        {
            this.FormattingWriter.AppendClause("ORDER BY (SELECT NULL)");
            exprOffsetFetch.Accept(this, parent.OrderBy);

            if (parent.SelectQuery is ExprQuerySpecification specification && !ReferenceEquals(specification.Top, null))
            {
                this.AppendSelectLimit(specification.Top, parent);
            }
        }

        protected override bool ForceParenthesesForQueryExpressionPart(IExprSubQuery subQuery)
        {
            return false;
        }

        protected override void AppendRecursiveCteKeyword()
        {
        }

        protected override bool SupportsInlineCte() => false;

        protected override bool VisitExprParameter(ExprParameter exprParameter, int paramNumber, IExpr? parent, out string? name)
        {

            if (!string.IsNullOrEmpty(exprParameter.TagName))
            {
                var tagName = exprParameter.TagName!;
                name = $"@{NormalizeParameterTagName(tagName)}";

                this.FormattingWriter.Append('(');
                this.FormattingWriter.Append(name);
                this.FormattingWriter.Append(')');
            }
            else
            {
                name = $"@{paramNumber}";
                this.FormattingWriter.Append('(');
                this.FormattingWriter.Append(name);
                this.FormattingWriter.Append(')');
            }

            return true;
        }

        protected override DbParameterValueVisitorExtractor GetDbParameterValueVisitorExtractor()
            => DbParameterValueVisitorExtractor.Instance;

        protected override void AppendUnicodePrefix(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return;
            }

            bool unicode = false;
            for (int i = 0; i < str.Length && !unicode; i++)
            {
                if (str[i] > 255)
                {
                    unicode = true;
                }
            }

            if (unicode)
            {
                this.FormattingWriter.Append('N');
            }
        }

        protected override IStatementVisitor CreateStatementSqlBuilder()
            => new TSqlStatementBuilder(this.Options.WithFormatting(null), this.FormattingWriter);

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
            this.FormattingWriter.Append('\'');

            return true;
        }

        public override bool VisitExprStringAgg(ExprStringAgg exprStringAgg, IExpr? parent)
        {
            this.FormattingWriter.Append("STRING_AGG(");
            exprStringAgg.Expression.Accept(this, exprStringAgg);
            this.FormattingWriter.Append(',');
            if (exprStringAgg.Separator is ExprParameter { ReplacedValue: ExprStringLiteral parameterLiteral })
            {
                // SQL Server rejects an nvarchar parameter as the separator when the
                // aggregated expression is varchar. Preserve the literal's own SQL type.
                parameterLiteral.Accept(this, exprStringAgg);
            }
            else
            {
                exprStringAgg.Separator.Accept(this, exprStringAgg);
            }
            this.FormattingWriter.Append(')');
            if (exprStringAgg.OrderBy != null)
            {
                this.FormattingWriter.Append(" WITHIN GROUP (ORDER BY ");
                exprStringAgg.OrderBy.Accept(this, exprStringAgg);
                this.FormattingWriter.Append(')');
            }
            return true;
        }

        public override bool VisitExprDateTimeOffsetLiteral(ExprDateTimeOffsetLiteral dateTimeLiteral, IExpr? arg)
        {
            return this.VisitExprDateTimeOffsetLiteralCommon(dateTimeLiteral, arg);
        }

        public override bool VisitExprBoolLiteral(ExprBoolLiteral boolLiteral, IExpr? parent)
        {
            if (boolLiteral.Value.HasValue)
            {
                this.FormattingWriter.Append(boolLiteral.Value.Value ? '1' : '0');
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
            this.FormattingWriter.Append('+');
            exprStringConcat.Right.Accept(this, exprStringConcat);
            return true;
        }
        // Queries and table sources

        public override bool VisitExprOrderByOffsetFetch(ExprOrderByOffsetFetch exprOrderByOffsetFetch, IExpr? parent)
        {
            this.AcceptItems(exprOrderByOffsetFetch.OrderList, exprOrderByOffsetFetch, SqlRenderSite.OrderBy, 1);
            exprOrderByOffsetFetch.OffsetFetch.Accept(this, exprOrderByOffsetFetch);

            if (parent is ExprSelectOffsetFetch selectOffsetFetch
                && selectOffsetFetch.SelectQuery is ExprQuerySpecification specification
                && !ReferenceEquals(specification.Top, null))
            {
                if (!ReferenceEquals(exprOrderByOffsetFetch.OffsetFetch.Fetch, null))
                {
                    throw new SqExpressException("Query with \"FETCH\" cannot be limited");
                }

                this.AppendSelectLimit(specification.Top, selectOffsetFetch);
            }

            return true;
        }

        public override bool VisitExprLateralCrossedTable(ExprLateralCrossedTable exprCrossedTable, IExpr? parent)
        {
            exprCrossedTable.Left.Accept(this, exprCrossedTable);
            this.FormattingWriter.AppendClause(
                exprCrossedTable.Outer ? "OUTER APPLY" : "CROSS APPLY",
                SqlRenderSite.Join,
                compactTrailingSpaces: 1);
            exprCrossedTable.Right.Accept(this, exprCrossedTable);
            return true;
        }

        public override bool VisitExprOffsetFetch(ExprOffsetFetch exprOffsetFetch, IExpr? parent)
        {
            return this.VisitExprOffsetFetchCommon(exprOffsetFetch, parent);
        }

        public override bool VisitExprTempTableName(ExprTempTableName tempTableName, IExpr? parent)
        {
            char? prefix = null;
            if (tempTableName.Name.Length > 0 && tempTableName.Name[0] != '#')
            {
                prefix = '#';
            }
            this.AppendName(tempTableName.Name, prefix);
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

        public override bool VisitExprMerge(ExprMerge merge, IExpr? parent)
        {
            this.FormattingWriter.Append("MERGE ");
            merge.TargetTable.Accept(this, merge);
            this.FormattingWriter.AppendClause("USING", compactTrailingSpaces: 1);
            merge.Source.Accept(this, merge);
            this.FormattingWriter.AppendClause("ON");
            this.AcceptBody(merge.On, merge, SqlRenderSite.JoinOn, 1);
            if (merge.WhenMatched != null)
            {
                this.FormattingWriter.AppendClause("WHEN MATCHED");
                merge.WhenMatched.Accept(this, merge);
            }
            if (merge.WhenNotMatchedByTarget != null)
            {
                this.FormattingWriter.AppendClause("WHEN NOT MATCHED");
                merge.WhenNotMatchedByTarget.Accept(this, merge);
            }
            if (merge.WhenNotMatchedBySource != null)
            {
                this.FormattingWriter.AppendClause("WHEN NOT MATCHED BY SOURCE");
                merge.WhenNotMatchedBySource.Accept(this, merge);
            }
            this.FormattingWriter.Append(';');

            return true;
        }

        public override bool VisitExprMergeOutput(ExprMergeOutput mergeOutput, IExpr? parent)
        {
            if (this.VisitExprMerge(mergeOutput, mergeOutput))
            {
                this.FormattingWriter.Length = this.FormattingWriter.Length - 1;// ; <-
                this.FormattingWriter.AppendClause("OUTPUT");
                mergeOutput.Output.Accept(this, mergeOutput);
                this.FormattingWriter.Append(';');
                return true;
            }
            return false;
        }

        public override bool VisitExprMergeMatchedUpdate(ExprMergeMatchedUpdate mergeMatchedUpdate, IExpr? parent)
        {
            if (mergeMatchedUpdate.And != null)
            {
                this.FormattingWriter.Append(" AND ");
                mergeMatchedUpdate.And.Accept(this, mergeMatchedUpdate);
            }

            this.AssertNotEmptyList(mergeMatchedUpdate.Set, "Set Clause cannot be empty");

            this.FormattingWriter.AppendClause("THEN UPDATE SET");

            this.AcceptItems(mergeMatchedUpdate.Set, mergeMatchedUpdate, SqlRenderSite.Set, 1);

            return true;
        }

        public override bool VisitExprMergeMatchedDelete(ExprMergeMatchedDelete mergeMatchedDelete, IExpr? parent)
        {
            if (mergeMatchedDelete.And != null)
            {
                this.FormattingWriter.Append(" AND ");
                mergeMatchedDelete.And.Accept(this, mergeMatchedDelete);
            }

            this.FormattingWriter.AppendClause("THEN  DELETE");

            return true;
        }

        public override bool VisitExprExprMergeNotMatchedInsert(ExprExprMergeNotMatchedInsert exprMergeNotMatchedInsert, IExpr? parent)
        {
            if (exprMergeNotMatchedInsert.And != null)
            {
                this.FormattingWriter.Append(" AND ");
                exprMergeNotMatchedInsert.And.Accept(this, exprMergeNotMatchedInsert);
            }

            this.AssertNotEmptyList(exprMergeNotMatchedInsert.Values, "Values cannot be empty");

            if (exprMergeNotMatchedInsert.Columns.Count > 0 &&
                exprMergeNotMatchedInsert.Columns.Count != exprMergeNotMatchedInsert.Values.Count)
            {
                throw new SqExpressException("Columns and values numbers do not match");
            }

            this.FormattingWriter.AppendClause("THEN INSERT");
            this.AcceptListComaSeparatedPar('(', exprMergeNotMatchedInsert.Columns, ')', exprMergeNotMatchedInsert);
            this.FormattingWriter.AppendClause("VALUES");
            this.AcceptItems(1, SqlRenderSite.Values, _ => this.AcceptListComaSeparatedPar('(', exprMergeNotMatchedInsert.Values, ')', exprMergeNotMatchedInsert));

            return true;
        }

        public override bool VisitExprExprMergeNotMatchedInsertDefault(ExprExprMergeNotMatchedInsertDefault exprExprMergeNotMatchedInsertDefault, IExpr? parent)
        {
            if (exprExprMergeNotMatchedInsertDefault.And != null)
            {
                this.FormattingWriter.Append(" AND ");
                exprExprMergeNotMatchedInsertDefault.And.Accept(this, exprExprMergeNotMatchedInsertDefault);
            }

            this.FormattingWriter.AppendClause("THEN INSERT DEFAULT VALUES");

            return true;
        }

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
                () =>
                {
                    exprInsertOutput.OutputColumns.AssertNotEmpty("INSERT OUTPUT cannot be empty");
                    this.FormattingWriter.AppendClause("OUTPUT");
                    this.AcceptItems(exprInsertOutput.OutputColumns.Count, SqlRenderSite.Output, i =>
            {

                        this.FormattingWriter.Append("INSERTED.");
                        exprInsertOutput.OutputColumns[i].Accept(this, exprInsertOutput);

            }, 1);
                },
                null);
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

            this.FormattingWriter.Append("SET IDENTITY_INSERT ");
            exprIdentityInsert.Insert.Target.Accept(this, exprIdentityInsert);
            this.FormattingWriter.Append(" ON;");
            this.FormattingWriter.AppendBoundary(SqlRenderSite.Statement, 0);

            this.AddCteSlot(parent);

            var result = exprIdentityInsert.Insert.Accept(this, exprIdentityInsert);

            this.FormattingWriter.Append(';');
            this.FormattingWriter.AppendBoundary(SqlRenderSite.Statement, 0);
            this.FormattingWriter.Append("SET IDENTITY_INSERT ");
            exprIdentityInsert.Insert.Target.Accept(this, exprIdentityInsert);
            this.FormattingWriter.Append(" OFF;");
            return result;
        }

        public override bool VisitExprUpdate(ExprUpdate exprUpdate, IExpr? parent)
        {
            this.AddCteSlot(parent);

            IExprTableSource? source = exprUpdate.Source;
            this.AssertNotEmptyList(exprUpdate.SetClause, "'UPDATE' statement should have at least one set clause");

            IExprColumnSource target = exprUpdate.Target.FullName;

            if (exprUpdate.Target.Alias != null)
            {
                target = exprUpdate.Target.Alias;
                source ??= exprUpdate.Target;
            }

            this.FormattingWriter.Append("UPDATE ");
            target.Accept(this, exprUpdate);

            this.FormattingWriter.AppendClause("SET");
            this.AcceptItems(exprUpdate.SetClause, exprUpdate, SqlRenderSite.Set, 1);

            if (source != null)
            {
                this.FormattingWriter.AppendClause("FROM", compactTrailingSpaces: 1);
                source.Accept(this, exprUpdate);
            }
            if (exprUpdate.Filter != null)
            {
                this.FormattingWriter.AppendClause("WHERE");
                this.AcceptBody(exprUpdate.Filter, exprUpdate, SqlRenderSite.Where, 1);
            }

            return true;
        }

        private void GenericUpdate(ExprTable targetIn, IReadOnlyList<ExprColumnSetClause> sets, IExprTableSource? source, ExprBoolean? filter, IExpr? parent)
        {
            this.AssertNotEmptyList(sets, "'UPDATE' statement should have at least one set clause");

            IExprColumnSource target = targetIn.FullName;

            if (targetIn.Alias != null)
            {
                target = targetIn.Alias;
                source ??= targetIn;
            }

            this.FormattingWriter.Append("UPDATE ");
            target.Accept(this, parent);

            this.FormattingWriter.AppendClause("SET");
            this.AcceptItems(sets, parent, SqlRenderSite.Set, 1);

            if (source != null)
            {
                this.FormattingWriter.AppendClause("FROM", compactTrailingSpaces: 1);
                source.Accept(this, parent);
            }
            if (filter != null)
            {
                this.FormattingWriter.AppendClause("WHERE");
                this.AcceptBody(filter, parent, SqlRenderSite.Where, 1);
            }
        }

        public override bool VisitExprDelete(ExprDelete exprDelete, IExpr? parent)
        {
            this.AddCteSlot(parent);
            this.GenericDelete(exprDelete.Target, null, exprDelete.Source, exprDelete.Filter, parent);
            return true;
        }

        public override bool VisitExprDeleteOutput(ExprDeleteOutput exprDeleteOutput, IExpr? parent)
        {
            this.AddCteSlot(parent);
            this.GenericDelete(exprDeleteOutput.Delete.Target, exprDeleteOutput.OutputColumns, exprDeleteOutput.Delete.Source, exprDeleteOutput.Delete.Filter, exprDeleteOutput);
            return true;
        }

        public override bool VisitExprCast(ExprCast exprCast, IExpr? parent)
        {
            return this.VisitExprCastCommon(exprCast, parent);
        }

        private void GenericDelete(ExprTable targetIn, IReadOnlyList<ExprAliasedColumn>? output, IExprTableSource? source, ExprBoolean? filter, IExpr? parent)
        {
            IExprColumnSource target = targetIn.FullName;

            if (targetIn.Alias != null)
            {
                target = targetIn.Alias;
                source ??= targetIn;
            }

            this.FormattingWriter.Append("DELETE ");
            target.Accept(this, parent);

            if (output != null)
            {
                this.AssertNotEmptyList(output, "Output list in 'DELETE' statement cannot be empty");
                this.FormattingWriter.AppendClause("OUTPUT");
                this.AcceptItems(output.Count, SqlRenderSite.Output, i =>
            {

                    this.FormattingWriter.Append("DELETED.");

                    var col = output[i];

                    if (col.Column.Source == null)
                    {
                        col.Accept(this, parent);
                    }
                    else
                    {
                        new ExprAliasedColumnName(col.Column.ColumnName, col.Alias).Accept(this, parent);
                    }

            }, 1);
            }

            if (source != null)
            {
                this.FormattingWriter.AppendClause("FROM", compactTrailingSpaces: 1);
                source.Accept(this, parent);
            }
            if (filter != null)
            {
                this.FormattingWriter.AppendClause("WHERE");
                this.AcceptBody(filter, parent, SqlRenderSite.Where, 1);
            }
        }

        public override bool VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean, IExpr? parent)
        {
            this.FormattingWriter.Append("bit");
            return true;
        }

        public override bool VisitExprTypeByte(ExprTypeByte exprTypeByte, IExpr? parent)
        {
            this.FormattingWriter.Append("tinyint");
            return true;
        }

        public override bool VisitExprTypeByteArray(ExprTypeByteArray exprTypeByte, IExpr? arg)
        {
            this.FormattingWriter.Append("varbinary(");
            if (exprTypeByte.Size.HasValue && exprTypeByte.Size.Value <= 8000)
            {
                this.FormattingWriter.Append(exprTypeByte.Size.Value.ToString());
                this.FormattingWriter.Append(')');
            }
            else
            {
                this.FormattingWriter.Append("MAX)");
            }

            return true;
        }

        public override bool VisitExprTypeFixSizeByteArray(ExprTypeFixSizeByteArray exprTypeFixSizeByteArray, IExpr? arg)
        {
            this.FormattingWriter.Append("binary(");
            this.FormattingWriter.Append(exprTypeFixSizeByteArray.Size.ToString());
            this.FormattingWriter.Append(')');

            return true;
        }

        public override bool VisitExprTypeInt16(ExprTypeInt16 exprTypeInt16, IExpr? parent)
        {
            this.FormattingWriter.Append("smallint");
            return true;
        }

        public override bool VisitExprTypeInt32(ExprTypeInt32 exprTypeInt32, IExpr? parent)
        {
            this.FormattingWriter.Append("int");
            return true;
        }

        public override bool VisitExprTypeInt64(ExprTypeInt64 exprTypeInt64, IExpr? parent)
        {
            this.FormattingWriter.Append("bigint");
            return true;
        }

        public override bool VisitExprTypeDecimal(ExprTypeDecimal exprTypeDecimal, IExpr? parent)
        {
            this.FormattingWriter.Append("decimal");
            if (exprTypeDecimal.PrecisionScale.HasValue)
            {
                this.FormattingWriter.Append('(');
                this.FormattingWriter.Append(exprTypeDecimal.PrecisionScale.Value.Precision);
                if(exprTypeDecimal.PrecisionScale.Value.Scale.HasValue)
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
            this.FormattingWriter.Append("float");
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
                this.FormattingWriter.Append("datetime");
            }

            return true;
        }

        public override bool VisitExprTypeDateTimeOffset(ExprTypeDateTimeOffset exprTypeDateTimeOffset, IExpr? arg)
        {
            this.FormattingWriter.Append("datetimeoffset");
            return true;
        }

        public override bool VisitExprTypeGuid(ExprTypeGuid exprTypeGuid, IExpr? parent)
        {
            this.FormattingWriter.Append("uniqueidentifier");
            return true;
        }

        public override bool VisitExprTypeString(ExprTypeString exprTypeString, IExpr? parent)
        {
            if (exprTypeString.IsUnicode)
            {
                if (exprTypeString.IsText)
                {
                    this.AppendName("ntext");
                }
                else
                {
                    this.AppendName("nvarchar");
                }
            }
            else
            {
                if (exprTypeString.IsText)
                {
                    this.AppendName("text");
                }
                else
                {
                    this.AppendName("varchar");
                }
            }

            if (exprTypeString.Size.HasValue)
            {
                if (!exprTypeString.IsText)
                {
                    this.FormattingWriter.Append('(');
                    this.FormattingWriter.Append(exprTypeString.Size.Value);
                    this.FormattingWriter.Append(')');
                }
                else
                {
                    throw new SqExpressException("text type cannot have a length");
                }
            }
            else
            {
                if (!exprTypeString.IsText)
                {
                    this.FormattingWriter.Append("(MAX)");
                }
            }

            return true;
        }

        public override bool VisitExprTypeFixSizeString(ExprTypeFixSizeString exprTypeFixSizeString, IExpr? arg)
        {
            if (exprTypeFixSizeString.IsUnicode)
            {
                this.AppendName("nchar");
            }
            else
            {
                this.AppendName("char");
            }

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
            this.AppendFunctionSingleArg("LEN", ctx.Arguments, ctx);
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
            this.AppendFunctionSingleArg("CEILING", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseDataLen(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionSingleArg("DATALENGTH", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseYear(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionSingleArg("YEAR", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseMonth(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionSingleArg("MONTH", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseDay(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionSingleArg("DAY", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseHour(ExprPortableScalarFunction ctx)
        {
            this.AppendDatePart("HOUR", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseMinute(ExprPortableScalarFunction ctx)
        {
            this.AppendDatePart("MINUTE", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseSecond(ExprPortableScalarFunction ctx)
        {
            this.AppendDatePart("SECOND", ctx.Arguments, ctx);
            return true;
        }

        bool IPortableScalarFunctionVisitor<bool, ExprPortableScalarFunction>.CaseIndexOf(ExprPortableScalarFunction ctx)
        {
            this.AppendFunctionTwoArgs("CHARINDEX", ctx.Arguments, ctx);
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
            this.AppendFunctionTwoArgs("REPLICATE", ctx.Arguments, ctx);
            return true;
        }

        private void AppendDatePart(string datePart, IReadOnlyList<ExprValue>? arguments, ExprPortableScalarFunction expr)
        {
            this.AssertArgumentsCount(arguments, 1, expr.PortableFunction);

            this.FormattingWriter.Append("DATEPART(");
            this.FormattingWriter.Append(datePart);
            this.FormattingWriter.Append(',');
            arguments![0].Accept(this, expr);
            this.FormattingWriter.Append(')');
        }

        public override bool VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull, IExpr? parent)
        {
            this.FormattingWriter.Append("ISNULL(");
            exprFuncIsNull.Test.Accept(this, exprFuncIsNull);
            this.FormattingWriter.Append(',');
            exprFuncIsNull.Alt.Accept(this, exprFuncIsNull);
            this.FormattingWriter.Append(')');
            return true;
        }

        public override bool VisitExprGetDate(ExprGetDate exprGetDat, IExpr? parent)
        {
            this.FormattingWriter.Append("GETDATE()");
            return true;
        }

        public override bool VisitExprGetUtcDate(ExprGetUtcDate exprGetUtcDate, IExpr? parent)
        {
            this.FormattingWriter.Append("GETUTCDATE()");
            return true;
        }

        public override bool VisitExprDateAdd(ExprDateAdd exprDateAdd, IExpr? arg)
        {
            this.FormattingWriter.Append("DATEADD(");

            var datePart = exprDateAdd.DatePart switch
            {
                DateAddDatePart.Year => "yy",
                DateAddDatePart.Month => "m",
                DateAddDatePart.Day => "d",
                DateAddDatePart.Week => "wk",
                DateAddDatePart.Hour => "hh",
                DateAddDatePart.Minute => "mi",
                DateAddDatePart.Second => "s",
                DateAddDatePart.Millisecond => "ms",
                _ => throw new ArgumentOutOfRangeException()
            };

            this.FormattingWriter.Append(datePart);
            this.FormattingWriter.Append(',');
            this.FormattingWriter.Append(exprDateAdd.Number);
            this.FormattingWriter.Append(',');
            exprDateAdd.Date.Accept(this, exprDateAdd);
            this.FormattingWriter.Append(')');

            return true;
        }

        public override bool VisitExprDateDiff(ExprDateDiff exprDateDiff, IExpr? arg)
        {
            this.FormattingWriter.Append("DATEDIFF(");

            var datePart = exprDateDiff.DatePart switch
            {
                DateDiffDatePart.Year => "YEAR",
                DateDiffDatePart.Month => "MONTH",
                DateDiffDatePart.Day => "DAY",
                DateDiffDatePart.Hour => "HOUR",
                DateDiffDatePart.Minute => "MINUTE",
                DateDiffDatePart.Second => "SECOND",
                DateDiffDatePart.Millisecond => "MILLISECOND",
                _ => throw new ArgumentOutOfRangeException()
            };
            this.FormattingWriter.Append(datePart);
            this.FormattingWriter.Append(',');
            exprDateDiff.StartDate.Accept(this, exprDateDiff);
            this.FormattingWriter.Append(',');
            exprDateDiff.EndDate.Accept(this, exprDateDiff);
            this.FormattingWriter.Append(')');

            return true;
        }

        public override bool VisitExprColumnName(ExprColumnName columnName, IExpr? parent)
            => this.VisitExprColumnNameCommon(columnName);

        public override bool VisitExprTableFullName(ExprTableFullName exprTableFullName, IExpr? parent) 
            => this.VisitExprTableFullNameCommon(exprTableFullName, parent);

        private static string NormalizeParameterTagName(string tagName)
        {
            var normalized = tagName[0] == '@' ? tagName.Substring(1) : tagName;

            if (normalized.Length < 1 || !IsValidFirstChar(normalized[0]))
            {
                throw BuildInvalidTagNameException(tagName);
            }

            for (var i = 1; i < normalized.Length; i++)
            {
                if (!IsValidNonFirstChar(normalized[i]))
                {
                    throw BuildInvalidTagNameException(tagName);
                }
            }

            return normalized;

            static bool IsValidFirstChar(char c) => char.IsLetter(c) || c == '_';

            static bool IsValidNonFirstChar(char c) => char.IsLetterOrDigit(c) || c == '_';

            static SqExpressException BuildInvalidTagNameException(string name) =>
                new SqExpressException(
                    $"Invalid SQL parameter tag name '{name}'. Expected '@' optional prefix and identifier pattern '[A-Za-z_][A-Za-z0-9_]*'.");
        }

        public override void AppendName(string name, char? prefix = null)
        {
            this.FormattingWriter.Append('[');
            if (prefix.HasValue)
            {
                this.FormattingWriter.Append(prefix.Value);
            }
            this.FormattingWriter.AppendEscapedClosingSquare(name);
            this.FormattingWriter.Append(']');
        }

    }
}
