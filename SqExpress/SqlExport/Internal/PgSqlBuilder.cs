using System;
using System.Collections.Generic;
using System.Text;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress.SqlExport.Internal
{
    internal class PgSqlBuilder : SqlBuilderBase
    {
        public PgSqlBuilder(SqlBuilderOptions? options = null, StringBuilder? externalBuilder = null) : base(options, externalBuilder)
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
            this.Builder.Append("'::uuid");

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
                this.Builder.Append(boolLiteral.Value.Value ? "true" : "false");
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
            this.Builder.Append("||");
            exprStringConcat.Right.Accept(this, exprStringConcat);
            return true;
        }

        protected override void AppendSelectTop(ExprValue top, IExpr? parent)
        {
            //N/A
        }

        protected override void AppendSelectLimit(ExprValue top, IExpr? parent)
        {
            this.Builder.Append(" LIMIT ");
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

        public override bool VisitExprInsertOutput(ExprInsertOutput exprInsertOutput, IExpr? parent)
        {
            this.GenericInsert(exprInsertOutput.Insert,
                null,
                () =>
                {
                    exprInsertOutput.OutputColumns.AssertNotEmpty("INSERT OUTPUT cannot be empty");
                    this.Builder.Append(" RETURNING ");
                    this.AcceptListComaSeparated(exprInsertOutput.OutputColumns, exprInsertOutput);
                });
            return true;
        }

        public override bool VisitExprInsertQuery(ExprInsertQuery exprInsertQuery, IExpr? parent)
        {
            return this.VisitExprInsertQueryCommon(exprInsertQuery, parent);
        }

        public override bool VisitExprUpdate(ExprUpdate exprUpdate, IExpr? parent)
        {
            this.AssertNotEmptyList(exprUpdate.SetClause, "'UPDATE' statement should have at least one set clause");

            this.Builder.Append("UPDATE ");
            exprUpdate.Target.Accept(this, exprUpdate);
            this.Builder.Append(" SET ");
            for (int i = 0; i < exprUpdate.SetClause.Count; i++)
            {
                var setClause = exprUpdate.SetClause[i];
                if (i != 0)
                {
                    this.Builder.Append(',');
                }
                setClause.Column.ColumnName.Accept(this, exprUpdate);
                this.Builder.Append('=');
                setClause.Value.Accept(this, exprUpdate);
            }

            ExprBoolean? sourceFilter = null;
            if (exprUpdate.Source != null)
            {
                IReadOnlyList<IExprTableSource> tables;
                (tables, sourceFilter) = exprUpdate.Source.ToTableMultiplication();
                this.AssertNotEmptyList(tables, "List of tables in 'UPDATE' statement cannot be empty");
                if (tables.Count > 1)
                {
                    this.Builder.Append(" FROM ");
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
                            this.Builder.Append(',');
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
                this.Builder.Append(" WHERE ");
                filter.Accept(this, exprUpdate);
            }
            return true;
        }

        public override bool VisitExprDelete(ExprDelete exprDelete, IExpr? parent)
        {
            this.Builder.Append("DELETE FROM ");
            exprDelete.Target.Accept(this, exprDelete);

            ExprBoolean? sourceFilter = null;
            if (exprDelete.Source != null)
            {
                IReadOnlyList<IExprTableSource> tables;
                (tables, sourceFilter) = exprDelete.Source.ToTableMultiplication();

                this.AssertNotEmptyList(tables, "List of tables in 'DELETE' statement cannot be empty");

                if (tables.Count > 1)
                {
                    this.Builder.Append(" USING ");
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
                            this.Builder.Append(',');
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
                this.Builder.Append(" WHERE ");
                filter.Accept(this, exprDelete);
            }
            return true;
        }

        public override bool VisitExprDeleteOutput(ExprDeleteOutput exprDeleteOutput, IExpr? parent)
        {
            exprDeleteOutput.Delete.Accept(this, exprDeleteOutput);
            this.AssertNotEmptyList(exprDeleteOutput.OutputColumns, "Output list in 'DELETE' statement cannot be empty");

            this.Builder.Append(" RETURNING ");

            this.AcceptListComaSeparated(exprDeleteOutput.OutputColumns, exprDeleteOutput);

            return true;
        }

        public override bool VisitExprCast(ExprCast exprCast, IExpr? parent)
        {
            return this.VisitExprCastCommon(exprCast, parent);
        }

        public override bool VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean, IExpr? parent)
        {
            this.Builder.Append("bool");
            return true;
        }

        public override bool VisitExprTypeByte(ExprTypeByte exprTypeByte, IExpr? parent)
        {
            throw new NotSupportedException("PostgresSQL does not support 1 byte numeric");
        }

        public override bool VisitExprTypeInt16(ExprTypeInt16 exprTypeInt16, IExpr? parent)
        {
            this.Builder.Append("int2");
            return true;
        }

        public override bool VisitExprTypeInt32(ExprTypeInt32 exprTypeInt32, IExpr? parent)
        {
            this.Builder.Append("int4");
            return true;
        }

        public override bool VisitExprTypeInt64(ExprTypeInt64 exprTypeInt64, IExpr? parent)
        {
            this.Builder.Append("int8");
            return true;
        }

        public override bool VisitExprTypeDecimal(ExprTypeDecimal exprTypeDecimal, IExpr? parent)
        {
            this.Builder.Append("decimal");
            if (exprTypeDecimal.PrecisionScale.HasValue)
            {
                this.Builder.Append('(');
                this.Builder.Append(exprTypeDecimal.PrecisionScale.Value.Precision);
                if (exprTypeDecimal.PrecisionScale.Value.Scale.HasValue)
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
            this.Builder.Append("float8");
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
                this.Builder.Append("timestamp");
            }

            return true;
        }

        public override bool VisitExprTypeGuid(ExprTypeGuid exprTypeGuid, IExpr? parent)
        {
            this.Builder.Append("uuid");
            return true;
        }

        public override bool VisitExprTypeString(ExprTypeString exprTypeString, IExpr? parent)
        {
            this.Builder.Append("character varying");
            if (exprTypeString.Size.HasValue)
            {
                this.Builder.Append('(');
                this.Builder.Append(exprTypeString.Size.Value);
                this.Builder.Append(')');
            }
            return true;
        }

        public override bool VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull, IExpr? parent)
        {
            SqQueryBuilder.Coalesce(exprFuncIsNull.Test, exprFuncIsNull.Alt).Accept(this, exprFuncIsNull);
            return true;
        }

        public override bool VisitExprGetDate(ExprGetDate exprGetDate, IExpr? parent)
        {
            this.Builder.Append("now()");
            return true;
        }

        public override bool VisitExprGetUtcDate(ExprGetUtcDate exprGetUtcDate, IExpr? parent)
        {
            this.Builder.Append("now() at time zone 'utc'");
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
            this.Builder.Append(sign);
            this.Builder.Append("INTERVAL'");
            this.Builder.Append(val);
            this.Builder.Append(datePart);
            this.Builder.Append('\'');

            return true;
        }

        public override bool VisitExprTableFullName(ExprTableFullName exprTableFullName, IExpr? parent)
            => this.VisitExprTableFullNameCommon(exprTableFullName, parent);


        public override void AppendName(string name, char? prefix = null)
        {
            this.Builder.Append('\"');
            if (prefix.HasValue)
            {
                this.Builder.Append(prefix.Value);
            }
            SqlInjectionChecker.AppendStringEscapeDoubleQuote(this.Builder, name);
            this.Builder.Append('\"');
        }

        protected override void AppendUnicodePrefix(string str) { }
    }
}