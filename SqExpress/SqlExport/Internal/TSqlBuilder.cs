using System;
using System.Collections.Generic;
using System.Text;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Expressions;
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
    }
}