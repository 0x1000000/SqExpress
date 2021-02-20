using System;
using System.Collections.Generic;
using System.Linq;
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
    internal class MySqlBuilder : SqlBuilderBase
    {
        public MySqlBuilder(SqlBuilderOptions? options = null, StringBuilder? externalBuilder = null) : base(options, externalBuilder)
        {
        }

        public override bool VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral, IExpr? parent)
        {
            if (exprGuidLiteral.Value == null)
            {
                this.AppendNull();
                return true;
            }

            this.Builder.Append('0');
            this.Builder.Append('x');

            var array = exprGuidLiteral.Value.Value.ToByteArray();
            foreach (var b in array)
            {
                this.Builder.Append(b.ToString("X2"));
            }

            return true;
        }

        protected override void EscapeStringLiteral(StringBuilder builder, string literal)
        {
            SqlInjectionChecker.AppendStringEscapeSingleQuoteAndBackslash(builder, literal);
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
            this.Builder.Append("CONCAT(");
            exprStringConcat.Left.Accept(this, exprStringConcat);
            this.Builder.Append(',');
            exprStringConcat.Right.Accept(this, exprStringConcat);
            this.Builder.Append(")");
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
            if (!ReferenceEquals(exprOffsetFetch.Fetch,null))
            {
                this.Builder.Append(" LIMIT ");
                exprOffsetFetch.Fetch.Accept(this, exprOffsetFetch);
            }

            this.Builder.Append(" OFFSET ");
            exprOffsetFetch.Offset.Accept(this, exprOffsetFetch);

            return true;
        }

        public override bool VisitExprGetDate(ExprGetDate exprGetDat, IExpr? parent)
        {
            this.Builder.Append("UTC_DATE()");
            return true;
        }

        public override bool VisitExprGetUtcDate(ExprGetUtcDate exprGetUtcDate, IExpr? parent)
        {
            this.Builder.Append("UTC_TIMESTAMP()");
            return true;
        }

        public override bool VisitExprDateAdd(ExprDateAdd exprDateAdd, IExpr? arg)
        {
            this.Builder.Append("DATE_ADD(");
            exprDateAdd.Date.Accept(this, exprDateAdd);
            this.Builder.Append(",INTERVAL ");
            if (exprDateAdd.DatePart == DateAddDatePart.Millisecond/*MICROSECOND*/)
            {
                this.Builder.Append(exprDateAdd.Number * 1000);
            }
            else
            {
                this.Builder.Append(exprDateAdd.Number);
            }
            this.Builder.Append(' ');

            var datePart = exprDateAdd.DatePart switch
            {
                DateAddDatePart.Year => "YEAR",
                DateAddDatePart.Month => "MONTH",
                DateAddDatePart.Day => "DAY",
                DateAddDatePart.Week => "WEEK",
                DateAddDatePart.Hour => "HOUR",
                DateAddDatePart.Minute => "MINUTE",
                DateAddDatePart.Second => "SECOND",
                DateAddDatePart.Millisecond => "MICROSECOND",
                _ => throw new ArgumentOutOfRangeException()
            };

            this.Builder.Append(datePart);
            this.Builder.Append(')');

            return true;
        }

        public override bool VisitExprTableFullName(ExprTableFullName exprTableFullName, IExpr? parent)
        {
            if (exprTableFullName.DbSchema?.Database != null)
            {
                if (exprTableFullName.DbSchema.Database.Accept(this, exprTableFullName.DbSchema))
                {
                    this.Builder.Append('.');
                }
            }
            exprTableFullName.TableName.Accept(this, exprTableFullName);
            return true;
        }

        public override bool VisitExprTempTableName(ExprTempTableName tempTableName, IExpr? parent)
        {
            this.AppendName(tempTableName.Name);
            return true;
        }

        public override bool VisitExprDbSchema(ExprDbSchema exprDbSchema, IExpr? parent)
        {
            if (exprDbSchema.Database != null)
            {
                exprDbSchema.Database.Accept(this, exprDbSchema);
                this.Builder.Append('.');
                return true;
            }
            return false;
        }

        public override bool VisitExprDerivedTableValues(ExprDerivedTableValues derivedTableValues, IExpr? parent)
        {
            this.AcceptPar('(', derivedTableValues.Values, ')', derivedTableValues);
            derivedTableValues.Alias.Accept(this, derivedTableValues);

            return true;
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
            //My SQL does not properly support "Derived Table Values"
            var newQuery = exprInsertQuery.Query.SyntaxTree()
                .Modify<ExprQuerySpecification>(es =>
                {
                    if (es.From is ExprDerivedTableValues values)
                    {
                        List<IExprSelecting> newSelecting = new List<IExprSelecting>(es.SelectList.Count);
                        bool star = false;
                        foreach (var exprSelecting in es.SelectList)
                        {
                            if (exprSelecting is ExprColumnName column)
                            {
                                if (values.Columns.Contains(column))
                                {
                                    if (!star)
                                    {
                                        star = true;
                                        newSelecting.Add(new ExprAllColumns(values.Alias));
                                    }
                                }
                                else
                                {
                                    newSelecting.Add(exprSelecting);
                                }
                            }
                            else
                            {
                                newSelecting.Add(exprSelecting);
                            }
                        }

                        return new ExprQuerySpecification(newSelecting, es.Top, es.Distinct, es.From, es.Where, es.GroupBy);
                    }

                    return es;
                });
            newQuery?.Accept(this, exprInsertQuery);
            return true;
        }

        public override bool VisitExprUpdate(ExprUpdate exprUpdate, IExpr? parent)
        {
            this.AssertNotEmptyList(exprUpdate.SetClause, "'UPDATE' statement should have at least one set clause");

            this.Builder.Append("UPDATE ");
            exprUpdate.Target.Accept(this, exprUpdate);

            ExprBoolean? sourceFilter = null;
            if (exprUpdate.Source != null)
            {
                IReadOnlyList<IExprTableSource> tables;
                (tables, sourceFilter) = exprUpdate.Source.ToTableMultiplication();
                this.AssertNotEmptyList(tables, "List of tables in 'UPDATE' statement cannot be empty");
                if (tables.Count > 1)
                {
                    int itemAppendCount = 0;
                    for (int i = 0; i < tables.Count; i++)
                    {
                        var source = tables[i];

                        if (source is ExprTable sTable && sTable.Equals(exprUpdate.Target))
                        {
                            continue;
                        }

                        this.Builder.Append(',');

                        source.Accept(this, exprUpdate);
                        itemAppendCount++;
                    }

                    if (itemAppendCount >= tables.Count)
                    {
                        throw new SqExpressException("Could not found target table in 'UPDATE' statement");
                    }
                }
            }

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
            if (exprDelete.Source == null)
            {
                this.Builder.Append("DELETE FROM ");
                exprDelete.Target.FullName.Accept(this, exprDelete);
            }
            else
            {
                var target = exprDelete.Target.Alias != null 
                    ? (IExprColumnSource)exprDelete.Target.Alias 
                    : exprDelete.Target.FullName;
                this.Builder.Append("DELETE ");
                target.Accept(this, exprDelete);
                this.Builder.Append(" FROM ");
                exprDelete.Source.Accept(this, exprDelete);
            }

            if (exprDelete.Filter != null)
            {
                this.Builder.Append(" WHERE ");
                var filter = exprDelete.Filter;
                if (exprDelete.Source == null && exprDelete.Target.Alias != null)
                {
                    //Damn MY SQL Does not support Alias for single removal
                    filter = (ExprBoolean) filter.SyntaxTree()
                        .Modify<ExprColumn>(cn => cn.Source != null && cn.Source.Equals(exprDelete.Target.Alias)
                            ? new ExprColumn(null, cn.ColumnName)
                            : cn)!;
                }

                filter.Accept(this, exprDelete);
            }

            return true;
        }

        public override bool VisitExprDeleteOutput(ExprDeleteOutput exprDeleteOutput, IExpr? parent)
        {
            exprDeleteOutput.Delete.Accept(this, exprDeleteOutput);
            var columns = exprDeleteOutput.OutputColumns;
            this.AssertNotEmptyList(columns, "Output list in 'DELETE' statement cannot be empty");

            this.Builder.Append(" RETURNING ");

            var targetAlias = exprDeleteOutput.Delete.Target.Alias;

            if (exprDeleteOutput.Delete.Source == null && targetAlias != null)
            {
                //Damn MY SQL Does not support Alias for single removal
                columns = columns.SelectToReadOnlyList(column => 
                    column.Column.Source != null && column.Column.Source.Equals(targetAlias)
                        ? new ExprAliasedColumn(new ExprColumn(null, column.Column.ColumnName), column.Alias)
                        : column);
            }

            this.AcceptListComaSeparated(columns, exprDeleteOutput);

            return true;
        }

        public override bool VisitExprCast(ExprCast exprCast, IExpr? parent)
        {
            switch (exprCast.SqlType)
            {
                case ExprTypeInt64 _:
                    this.Builder.Append("CAST(");
                    exprCast.Expression.Accept(this, exprCast);
                    this.Builder.Append(" AS SIGNED)");
                    break;
                case ExprTypeDouble _:
                    this.Builder.Append("CAST(");
                    exprCast.Expression.Accept(this, exprCast);
                    this.Builder.Append(" AS DOUBLE)");
                    break;
                case ExprTypeInt16 _:
                case ExprTypeInt32 _:
                case ExprTypeByte _:
                    exprCast.Expression.Accept(this, exprCast);
                    break;
                case ExprTypeDateTime _:
                case ExprTypeString _:
                    return this.VisitExprCastCommon(exprCast, parent);
                default:
                    throw new SqExpressException("MySQL does not support casting to " + MySqlExporter.Default.ToSql(exprCast.SqlType));
            }

            return true;
        }

        public override bool VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean, IExpr? parent)
        {
            this.Builder.Append("bit");
            return true;
        }

        public override bool VisitExprTypeByte(ExprTypeByte exprTypeByte, IExpr? parent)
        {
            this.Builder.Append("tinyint unsigned");
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
            this.Builder.Append("double");
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
            this.Builder.Append("binary(16)");
            return true;
        }

        public override bool VisitExprTypeString(ExprTypeString exprTypeString, IExpr? parent)
        {
            if (exprTypeString.IsText || (exprTypeString.Size ?? 0) > 255)
            {
                this.Builder.Append("text");
            }
            else
            {
                this.Builder.Append("varchar");
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
                    this.Builder.Append("(255)");
                }
                else
                {
                    this.Builder.Append("(65535)");
                }
            }

            if (exprTypeString.IsUnicode)
            {
                this.Builder.Append(" character set utf8");
            }

            return true;
        }

        public override bool VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull, IExpr? parent)
        {
            SqQueryBuilder.Coalesce(exprFuncIsNull.Test, exprFuncIsNull.Alt).Accept(this, exprFuncIsNull);
            return true;
        }

        public override void AppendName(string name, char? prefix = null)
        {
            this.Builder.Append('`');
            if (prefix.HasValue)
            {
                this.Builder.Append(prefix.Value);
            }
            SqlInjectionChecker.AppendStringEscapeBacktick(this.Builder, name);
            this.Builder.Append('`');
        }

        protected override void AppendUnicodePrefix(string str)
        {
        }
    }
}