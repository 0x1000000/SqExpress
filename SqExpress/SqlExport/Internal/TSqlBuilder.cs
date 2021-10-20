using System;
using System.Collections.Generic;
using System.Text;
using SqExpress.SqlExport.Statement.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Internal;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress.SqlExport.Internal
{
    internal class TSqlBuilder : SqlBuilderBase
    {
        public TSqlBuilder(SqlBuilderOptions? options = null, StringBuilder? externalBuilder = null) : base(options, externalBuilder)
        {
        }

        public override bool VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral, IExpr? parent)
        {
            if (exprGuidLiteral.Value == null)
            {
                this.AppendNull();
                return true;
            }

            this.Builder.Append('\'');
            this.Builder.Append(exprGuidLiteral.Value.Value.ToString("D"));
            this.Builder.Append('\'');

            return true;
        }

        protected override void EscapeStringLiteral(StringBuilder builder, string literal)
        {
            SqlInjectionChecker.AppendStringEscapeSingleQuote(builder, literal);
        }

        public override bool VisitExprBoolLiteral(ExprBoolLiteral boolLiteral, IExpr? parent)
        {
            if (boolLiteral.Value.HasValue)
            {
                this.Builder.Append(boolLiteral.Value.Value ? '1' : '0');
            }
            else
            {
                this.AppendNull();
            }

            return true;
        }

        protected override void AppendByteArrayLiteralPrefix()
        {
            this.Builder.Append('0');
            this.Builder.Append('x');
        }

        protected override void AppendByteArrayLiteralSuffix()
        {
        }

        public override bool VisitExprStringConcat(ExprStringConcat exprStringConcat, IExpr? parent)
        {
            exprStringConcat.Left.Accept(this, exprStringConcat);
            this.Builder.Append('+');
            exprStringConcat.Right.Accept(this, exprStringConcat);
            return true;
        }
        protected override void AppendSelectTop(ExprValue top, IExpr? parent)
        {
            this.Builder.Append("TOP ");
            top.Accept(this, top);
            this.Builder.Append(' ');
        }

        protected override void AppendSelectLimit(ExprValue top, IExpr? parent)
        {
            //N/A
        }

        protected override bool ForceParenthesesForQueryExpressionPart(IExprSubQuery subQuery)
        {
            return false;
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

        //Merge

        public override bool VisitExprMerge(ExprMerge merge, IExpr? parent)
        {
            this.Builder.Append("MERGE ");
            merge.TargetTable.Accept(this, merge);
            this.Builder.Append(" USING ");
            merge.Source.Accept(this, merge);
            this.Builder.Append(" ON ");
            merge.On.Accept(this, merge);
            if (merge.WhenMatched != null)
            {
                this.Builder.Append(" WHEN MATCHED");
                merge.WhenMatched.Accept(this, merge);
            }
            if (merge.WhenNotMatchedByTarget != null)
            {
                this.Builder.Append(" WHEN NOT MATCHED");
                merge.WhenNotMatchedByTarget.Accept(this, merge);
            }
            if (merge.WhenNotMatchedBySource != null)
            {
                this.Builder.Append(" WHEN NOT MATCHED BY SOURCE");
                merge.WhenNotMatchedBySource.Accept(this, merge);
            }
            this.Builder.Append(';');

            return true;
        }

        public override bool VisitExprMergeOutput(ExprMergeOutput mergeOutput, IExpr? parent)
        {
            if (this.VisitExprMerge(mergeOutput, mergeOutput))
            {
                this.Builder.Length = this.Builder.Length - 1;// ; <-
                this.Builder.Append(" OUTPUT ");
                mergeOutput.Output.Accept(this, mergeOutput);
                this.Builder.Append(';');
                return true;
            }
            return false;
        }

        public override bool VisitExprMergeMatchedUpdate(ExprMergeMatchedUpdate mergeMatchedUpdate, IExpr? parent)
        {
            if (mergeMatchedUpdate.And != null)
            {
                this.Builder.Append(" AND ");
                mergeMatchedUpdate.And.Accept(this, mergeMatchedUpdate);
            }

            this.AssertNotEmptyList(mergeMatchedUpdate.Set, "Set Clause cannot be empty");

            this.Builder.Append(" THEN UPDATE SET ");

            this.AcceptListComaSeparated(mergeMatchedUpdate.Set, mergeMatchedUpdate);

            return true;
        }

        public override bool VisitExprMergeMatchedDelete(ExprMergeMatchedDelete mergeMatchedDelete, IExpr? parent)
        {
            if (mergeMatchedDelete.And != null)
            {
                this.Builder.Append(" AND ");
                mergeMatchedDelete.And.Accept(this, mergeMatchedDelete);
            }

            this.Builder.Append(" THEN  DELETE");

            return true;
        }

        public override bool VisitExprExprMergeNotMatchedInsert(ExprExprMergeNotMatchedInsert exprMergeNotMatchedInsert, IExpr? parent)
        {
            if (exprMergeNotMatchedInsert.And != null)
            {
                this.Builder.Append(" AND ");
                exprMergeNotMatchedInsert.And.Accept(this, exprMergeNotMatchedInsert);
            }

            this.AssertNotEmptyList(exprMergeNotMatchedInsert.Values, "Values cannot be empty");

            if (exprMergeNotMatchedInsert.Columns.Count > 0 &&
                exprMergeNotMatchedInsert.Columns.Count != exprMergeNotMatchedInsert.Values.Count)
            {
                throw new SqExpressException("Columns and values numbers do not match");
            }

            this.Builder.Append(" THEN INSERT");
            this.AcceptListComaSeparatedPar('(', exprMergeNotMatchedInsert.Columns, ')', exprMergeNotMatchedInsert);
            this.Builder.Append(" VALUES");
            this.AcceptListComaSeparatedPar('(', exprMergeNotMatchedInsert.Values, ')', exprMergeNotMatchedInsert);

            return true;
        }

        public override bool VisitExprExprMergeNotMatchedInsertDefault(ExprExprMergeNotMatchedInsertDefault exprExprMergeNotMatchedInsertDefault, IExpr? parent)
        {
            if (exprExprMergeNotMatchedInsertDefault.And != null)
            {
                this.Builder.Append(" AND ");
                exprExprMergeNotMatchedInsertDefault.And.Accept(this, exprExprMergeNotMatchedInsertDefault);
            }

            this.Builder.Append(" THEN INSERT DEFAULT VALUES");

            return true;
        }

        public override bool VisitExprInsertOutput(ExprInsertOutput exprInsertOutput, IExpr? parent)
        {
            this.GenericInsert(exprInsertOutput.Insert,
                () =>
                {
                    exprInsertOutput.OutputColumns.AssertNotEmpty("INSERT OUTPUT cannot be empty");
                    this.Builder.Append("OUTPUT ");
                    for (int i = 0; i < exprInsertOutput.OutputColumns.Count; i++)
                    {
                        if (i != 0)
                        {
                            this.Builder.Append(',');
                        }

                        this.Builder.Append("INSERTED.");
                        exprInsertOutput.OutputColumns[i].Accept(this, exprInsertOutput);
                    }
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

            this.Builder.Append("SET IDENTITY_INSERT ");
            exprIdentityInsert.Insert.Target.Accept(this, exprIdentityInsert);
            this.Builder.Append(" ON;");

            var result = exprIdentityInsert.Insert.Accept(this, exprIdentityInsert);

            this.Builder.Append(";SET IDENTITY_INSERT ");
            exprIdentityInsert.Insert.Target.Accept(this, exprIdentityInsert);
            this.Builder.Append(" OFF;");
            return result;
        }

        public override bool VisitExprUpdate(ExprUpdate exprUpdate, IExpr? parent)
        {
            this.GenericUpdate(exprUpdate.Target, exprUpdate.SetClause, exprUpdate.Source, exprUpdate.Filter , exprUpdate);
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

            this.Builder.Append("UPDATE ");
            target.Accept(this, parent);

            this.Builder.Append(" SET ");
            this.AcceptListComaSeparated(sets, parent);

            if (source != null)
            {
                this.Builder.Append(" FROM ");
                source.Accept(this, parent);
            }
            if (filter != null)
            {
                this.Builder.Append(" WHERE ");
                filter.Accept(this, parent);
            }
        }

        public override bool VisitExprDelete(ExprDelete exprDelete, IExpr? parent)
        {
            this.GenericDelete(exprDelete.Target, null, exprDelete.Source, exprDelete.Filter, exprDelete);
            return true;
        }

        public override bool VisitExprDeleteOutput(ExprDeleteOutput exprDeleteOutput, IExpr? parent)
        {
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

            this.Builder.Append("DELETE ");
            target.Accept(this, parent);

            if (output != null)
            {
                this.AssertNotEmptyList(output, "Output list in 'DELETE' statement cannot be empty");
                this.Builder.Append(" OUTPUT ");
                for (int i = 0; i < output.Count; i++)
                {
                    if (i != 0)
                    {
                        this.Builder.Append(',');
                    }

                    this.Builder.Append("DELETED.");

                    var col = output[i];

                    if (col.Column.Source == null)
                    {
                        col.Accept(this, parent);
                    }
                    else
                    {
                        new ExprAliasedColumnName(col.Column.ColumnName, col.Alias).Accept(this, parent);
                    }
                }
            }

            if (source != null)
            {
                this.Builder.Append(" FROM ");
                source.Accept(this, parent);
            }
            if (filter != null)
            {
                this.Builder.Append(" WHERE ");
                filter.Accept(this, parent);
            }
        }

        public override bool VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean, IExpr? parent)
        {
            this.Builder.Append("bit");
            return true;
        }

        public override bool VisitExprTypeByte(ExprTypeByte exprTypeByte, IExpr? parent)
        {
            this.Builder.Append("tinyint");
            return true;
        }

        public override bool VisitExprTypeByteArray(ExprTypeByteArray exprTypeByte, IExpr? arg)
        {
            this.Builder.Append("varbinary(");
            if (exprTypeByte.Size.HasValue)
            {
                this.Builder.Append(exprTypeByte.Size.Value.ToString());
                this.Builder.Append(')');
            }
            else
            {
                this.Builder.Append("MAX)");
            }

            return true;
        }

        public override bool VisitExprTypeFixSizeByteArray(ExprTypeFixSizeByteArray exprTypeFixSizeByteArray, IExpr? arg)
        {
            this.Builder.Append("binary(");
            this.Builder.Append(exprTypeFixSizeByteArray.Size.ToString());
            this.Builder.Append(')');

            return true;
        }

        public override bool VisitExprTypeInt16(ExprTypeInt16 exprTypeInt16, IExpr? parent)
        {
            this.Builder.Append("smallint");
            return true;
        }

        public override bool VisitExprTypeInt32(ExprTypeInt32 exprTypeInt32, IExpr? parent)
        {
            this.Builder.Append("int");
            return true;
        }

        public override bool VisitExprTypeInt64(ExprTypeInt64 exprTypeInt64, IExpr? parent)
        {
            this.Builder.Append("bigint");
            return true;
        }

        public override bool VisitExprTypeDecimal(ExprTypeDecimal exprTypeDecimal, IExpr? parent)
        {
            this.Builder.Append("decimal");
            if (exprTypeDecimal.PrecisionScale.HasValue)
            {
                this.Builder.Append('(');
                this.Builder.Append(exprTypeDecimal.PrecisionScale.Value.Precision);
                if(exprTypeDecimal.PrecisionScale.Value.Scale.HasValue)
                {
                    this.Builder.Append(',');
                    this.Builder.Append(exprTypeDecimal.PrecisionScale.Value.Scale.Value);
                }
                this.Builder.Append(')');
            }
            return true;
        }

        public override bool VisitExprTypeDouble(ExprTypeDouble exprTypeDouble, IExpr? parent)
        {
            this.Builder.Append("float");
            return true;
        }

        public override bool VisitExprTypeDateTime(ExprTypeDateTime exprTypeDateTime, IExpr? parent)
        {
            if (exprTypeDateTime.IsDate)
            {
                this.Builder.Append("date");
            }
            else
            {
                this.Builder.Append("datetime");
            }

            return true;
        }

        public override bool VisitExprTypeGuid(ExprTypeGuid exprTypeGuid, IExpr? parent)
        {
            this.Builder.Append("uniqueidentifier");
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
                    this.Builder.Append('(');
                    this.Builder.Append(exprTypeString.Size.Value);
                    this.Builder.Append(')');
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
                    this.Builder.Append("(MAX)");
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

            this.Builder.Append('(');
            this.Builder.Append(exprTypeFixSizeString.Size.ToString());
            this.Builder.Append(')');

            return true;
        }

        public override bool VisitExprTypeXml(ExprTypeXml exprTypeXml, IExpr? arg)
        {
            this.Builder.Append("xml");
            return true;
        }

        public override bool VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull, IExpr? parent)
        {
            this.Builder.Append("ISNULL(");
            exprFuncIsNull.Test.Accept(this, exprFuncIsNull);
            this.Builder.Append(',');
            exprFuncIsNull.Alt.Accept(this, exprFuncIsNull);
            this.Builder.Append(')');
            return true;
        }

        public override bool VisitExprGetDate(ExprGetDate exprGetDat, IExpr? parent)
        {
            this.Builder.Append("GETDATE()");
            return true;
        }

        public override bool VisitExprGetUtcDate(ExprGetUtcDate exprGetUtcDate, IExpr? parent)
        {
            this.Builder.Append("GETUTCDATE()");
            return true;
        }

        public override bool VisitExprDateAdd(ExprDateAdd exprDateAdd, IExpr? arg)
        {
            this.Builder.Append("DATEADD(");

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

            this.Builder.Append(datePart);
            this.Builder.Append(',');
            this.Builder.Append(exprDateAdd.Number);
            this.Builder.Append(',');
            exprDateAdd.Date.Accept(this, exprDateAdd);
            this.Builder.Append(')');

            return true;
        }

        public override bool VisitExprTableFullName(ExprTableFullName exprTableFullName, IExpr? parent) 
            => this.VisitExprTableFullNameCommon(exprTableFullName, parent);

        public override void AppendName(string name, char? prefix = null)
        {
            this.Builder.Append('[');
            if (prefix.HasValue)
            {
                this.Builder.Append(prefix.Value);
            }
            SqlInjectionChecker.AppendStringEscapeClosingSquare(this.Builder, name);
            this.Builder.Append(']');
        }

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
                this.Builder.Append('N');
            }
        }

        protected override IStatementVisitor CreateStatementSqlBuilder()
            => new TSqlStatementBuilder(this.Options, this.Builder);
    }
}