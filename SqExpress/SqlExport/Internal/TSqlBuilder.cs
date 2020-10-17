﻿using System.Collections.Generic;
using System.Text;
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

        public override bool VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral, object? arg)
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

        public override bool VisitExprBoolLiteral(ExprBoolLiteral boolLiteral, object? arg)
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

        public override bool VisitExprStringConcat(ExprStringConcat exprStringConcat, object? arg)
        {
            exprStringConcat.Left.Accept(this, arg);
            this.Builder.Append('+');
            exprStringConcat.Right.Accept(this, arg);
            return true;
        }
        protected override void AppendSelectTop(ExprValue top, object? arg)
        {
            this.Builder.Append("TOP ");
            top.Accept(this, arg);
            this.Builder.Append(' ');
        }

        public override bool VisitExprTempTableName(ExprTempTableName tempTableName, object? arg)
        {
            char? prefix = null;
            if (tempTableName.Name.Length > 0 && tempTableName.Name[0] != '#')
            {
                prefix = '#';
            }
            this.AppendName(tempTableName.Name, prefix);
            return true;
        }

        public override bool VisitExprInsertOutput(ExprInsertOutput exprInsertOutput, object? arg)
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
                        exprInsertOutput.OutputColumns[i].Accept(this, arg);
                    }
                },
                null, 
                arg);
            return true;
        }

        public override bool VisitExprUpdate(ExprUpdate exprUpdate, object? arg)
        {
            this.GenericUpdate(exprUpdate.Target, exprUpdate.SetClause, exprUpdate.Source, exprUpdate.Filter ,arg);
            return true;
        }

        private void GenericUpdate(ExprTable targetIn, IReadOnlyList<ExprColumnSetClause> sets, IExprTableSource? source, ExprBoolean? filter, object? arg)
        {
            this.AssertNotEmptyList(sets, "'UPDATE' statement should have at least one set clause");

            IExprColumnSource target = targetIn.FullName;

            if (targetIn.Alias != null)
            {
                target = targetIn.Alias;
                source ??= targetIn;
            }

            this.Builder.Append("UPDATE ");
            target.Accept(this, arg);

            this.Builder.Append(" SET ");
            this.AcceptListComaSeparated(sets, arg);

            if (source != null)
            {
                this.Builder.Append(" FROM ");
                source.Accept(this, arg);
            }
            if (filter != null)
            {
                this.Builder.Append(" WHERE ");
                filter.Accept(this, arg);
            }
        }

        public override bool VisitExprDelete(ExprDelete exprDelete, object? arg)
        {
            this.GenericDelete(exprDelete.Target, null, exprDelete.Source, exprDelete.Filter, arg);
            return true;
        }

        public override bool VisitExprDeleteOutput(ExprDeleteOutput exprDeleteOutput, object? arg)
        {
            this.GenericDelete(exprDeleteOutput.Delete.Target, exprDeleteOutput.OutputColumns, exprDeleteOutput.Delete.Source, exprDeleteOutput.Delete.Filter, arg);
            return true;
        }

        private void GenericDelete(ExprTable targetIn, IReadOnlyList<ExprAliasedColumn>? output, IExprTableSource? source, ExprBoolean? filter, object? arg)
        {
            IExprColumnSource target = targetIn.FullName;

            if (targetIn.Alias != null)
            {
                target = targetIn.Alias;
                source ??= targetIn;
            }

            this.Builder.Append("DELETE ");
            target.Accept(this, arg);

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
                        col.Accept(this, arg);
                    }
                    else
                    {
                        new ExprAliasedColumnName(col.Column.ColumnName, col.Alias).Accept(this, arg);
                    }
                }
            }

            if (source != null)
            {
                this.Builder.Append(" FROM ");
                source.Accept(this, arg);
            }
            if (filter != null)
            {
                this.Builder.Append(" WHERE ");
                filter.Accept(this, arg);
            }
        }

        public override bool VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean, object? arg)
        {
            this.Builder.Append("bit");
            return true;
        }

        public override bool VisitExprTypeByte(ExprTypeByte exprTypeByte, object? arg)
        {
            this.Builder.Append("tinyint");
            return true;
        }

        public override bool VisitExprTypeInt16(ExprTypeInt16 exprTypeInt16, object? arg)
        {
            this.Builder.Append("smallint");
            return true;
        }

        public override bool VisitExprTypeInt32(ExprTypeInt32 exprTypeInt32, object? arg)
        {
            this.Builder.Append("int");
            return true;
        }

        public override bool VisitExprTypeInt64(ExprTypeInt64 exprTypeInt64, object? arg)
        {
            this.Builder.Append("bigint");
            return true;
        }

        public override bool VisitExprTypeDecimal(ExprTypeDecimal exprTypeDecimal, object? arg)
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

        public override bool VisitExprTypeDouble(ExprTypeDouble exprTypeDouble, object? arg)
        {
            this.Builder.Append("float");
            return true;
        }

        public override bool VisitExprTypeDateTime(ExprTypeDateTime exprTypeDateTime, object? arg)
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

        public override bool VisitExprTypeGuid(ExprTypeGuid exprTypeGuid, object? arg)
        {
            this.Builder.Append("uniqueidentifier");
            return true;
        }

        public override bool VisitExprTypeString(ExprTypeString exprTypeString, object? arg)
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

        public override bool VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull, object? arg)
        {
            this.Builder.Append("ISNULL(");
            exprFuncIsNull.Test.Accept(this, arg);
            this.Builder.Append(',');
            exprFuncIsNull.Alt.Accept(this, arg);
            this.Builder.Append(')');
            return true;
        }

        public override bool VisitExprGetDate(ExprGetDate exprGetDat, object? arg)
        {
            this.Builder.Append("GETDATE()");
            return true;
        }

        public override bool VisitExprGetUtcDate(ExprGetUtcDate exprGetUtcDate, object? arg)
        {
            this.Builder.Append("GETUTCDATE()");
            return true;
        }

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