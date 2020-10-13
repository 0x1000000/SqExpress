using System.Collections.Generic;
using System.Text;
using SqExpress.SqlExport.Internal;
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

namespace SqExpress.SqlExport
{
    public class TSqlBuilder : SqlBuilderBase
    {
        public TSqlBuilder(SqlBuilderOptions? options = null, StringBuilder? externalBuilder = null) : base(options, externalBuilder)
        {
        }

        public override bool VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral)
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

        public override bool VisitExprBoolLiteral(ExprBoolLiteral boolLiteral)
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

        public override bool VisitExprStringConcat(ExprStringConcat exprStringConcat)
        {
            exprStringConcat.Left.Accept(this);
            this.Builder.Append('+');
            exprStringConcat.Right.Accept(this);
            return true;
        }
        protected override void AppendSelectTop(ExprValue top)
        {
            this.Builder.Append("TOP ");
            top.Accept(this);
            this.Builder.Append(' ');
        }

        public override bool VisitExprInsertOutput(ExprInsertOutput exprInsertOutput)
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
                        exprInsertOutput.OutputColumns[i].Accept(this);
                    }
                },
                null);
            return true;
        }

        public override bool VisitExprUpdate(ExprUpdate exprUpdate)
        {
            this.GenericUpdate(exprUpdate.Target, exprUpdate.SetClause, exprUpdate.Source, exprUpdate.Filter);
            return true;
        }

        private void GenericUpdate(ExprTable targetIn, IReadOnlyList<ExprColumnSetClause> sets, IExprTableSource? source, ExprBoolean? filter)
        {
            this.AssertNotEmptyList(sets, "'UPDATE' statement should have at least one set clause");

            IExprColumnSource target = targetIn.FullName;

            if (targetIn.Alias != null)
            {
                target = targetIn.Alias;
                source ??= targetIn;
            }

            this.Builder.Append("UPDATE ");
            target.Accept(this);

            this.Builder.Append(" SET ");
            this.AcceptListComaSeparated(sets);

            if (source != null)
            {
                this.Builder.Append(" FROM ");
                source.Accept(this);
            }
            if (filter != null)
            {
                this.Builder.Append(" WHERE ");
                filter.Accept(this);
            }
        }

        public override bool VisitExprDelete(ExprDelete exprDelete)
        {
            this.GenericDelete(exprDelete.Target, null, exprDelete.Source, exprDelete.Filter);
            return true;
        }

        public override bool VisitExprDeleteOutput(ExprDeleteOutput exprDeleteOutput)
        {
            this.GenericDelete(exprDeleteOutput.Delete.Target, exprDeleteOutput.OutputColumns, exprDeleteOutput.Delete.Source, exprDeleteOutput.Delete.Filter);
            return true;
        }

        private void GenericDelete(ExprTable targetIn, IReadOnlyList<ExprAliasedColumn>? output, IExprTableSource? source, ExprBoolean? filter)
        {
            IExprColumnSource target = targetIn.FullName;

            if (targetIn.Alias != null)
            {
                target = targetIn.Alias;
                source ??= targetIn;
            }

            this.Builder.Append("DELETE ");
            target.Accept(this);

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
                        col.Accept(this);
                    }
                    else
                    {
                        new ExprAliasedColumnName(col.Column.ColumnName, col.Alias).Accept(this);
                    }
                }
            }

            if (source != null)
            {
                this.Builder.Append(" FROM ");
                source.Accept(this);
            }
            if (filter != null)
            {
                this.Builder.Append(" WHERE ");
                filter.Accept(this);
            }
        }

        public override bool VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean)
        {
            this.Builder.Append("bit");
            return true;
        }

        public override bool VisitExprTypeByte(ExprTypeByte exprTypeByte)
        {
            this.Builder.Append("tinyint");
            return true;
        }

        public override bool VisitExprTypeInt16(ExprTypeInt16 exprTypeInt16)
        {
            this.Builder.Append("smallint");
            return true;
        }

        public override bool VisitExprTypeInt32(ExprTypeInt32 exprTypeInt32)
        {
            this.Builder.Append("int");
            return true;
        }

        public override bool VisitExprTypeInt64(ExprTypeInt64 exprTypeInt64)
        {
            this.Builder.Append("bigint");
            return true;
        }

        public override bool VisitExprTypeDecimal(ExprTypeDecimal exprTypeDecimal)
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

        public override bool VisitExprTypeDouble(ExprTypeDouble exprTypeDouble)
        {
            this.Builder.Append("float");
            return true;
        }

        public override bool VisitExprTypeDateTime(ExprTypeDateTime exprTypeDateTime)
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

        public override bool VisitExprTypeGuid(ExprTypeGuid exprTypeGuid)
        {
            this.Builder.Append("uniqueidentifier");
            return true;
        }

        public override bool VisitExprTypeString(ExprTypeString exprTypeString)
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

        public override bool VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull)
        {
            this.Builder.Append("ISNULL(");
            exprFuncIsNull.Test.Accept(this);
            this.Builder.Append(',');
            exprFuncIsNull.Alt.Accept(this);
            this.Builder.Append(')');
            return true;
        }

        public override bool VisitExprFuncGetDate(ExprGetDate exprGetDate)
        {
            this.Builder.Append("GETDATE()");
            return true;
        }

        public override bool VisitExprFuncGetUtcDate(ExprGetUtcDate exprGetUtcDate)
        {
            this.Builder.Append("GETUTCDATE()");
            return true;
        }

        public override void AppendName(string name)
        {
            this.Builder.Append('[');
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