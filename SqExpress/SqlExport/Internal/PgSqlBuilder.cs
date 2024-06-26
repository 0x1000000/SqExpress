﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using SqExpress.SqlExport.Statement.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
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
    internal class PgSqlBuilder : SqlBuilderBase
    {
        private const string InformationSchemaLowerInvariant = "information_schema";

        public PgSqlBuilder(SqlBuilderOptions? options = null, StringBuilder? externalBuilder = null) : base(options, externalBuilder, new SqlAliasGenerator(), false)
        {
        }

        private PgSqlBuilder(SqlBuilderOptions? options, StringBuilder? externalBuilder, SqlAliasGenerator aliasGenerator, bool dismissCteInject)
            : base(options, externalBuilder, aliasGenerator, dismissCteInject)
        {
        }

        protected override SqlBuilderBase CreateInstance(SqlAliasGenerator aliasGenerator, bool dismissCteInject)
        {
            return new PgSqlBuilder(this.Options, new StringBuilder(), aliasGenerator, dismissCteInject);
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

        public override bool VisitExprDateTimeOffsetLiteral(ExprDateTimeOffsetLiteral dateTimeLiteral, IExpr? arg)
        {
            return this.VisitExprDateTimeOffsetLiteralCommon(dateTimeLiteral, arg);
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

        protected override void AppendByteArrayLiteralPrefix()
        {
            this.Builder.Append("E'\\\\x");
        }

        protected override void AppendByteArrayLiteralSuffix()
        {
            this.Builder.Append('\'');
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

        public override bool VisitExprLateralCrossedTable(ExprLateralCrossedTable exprCrossedTable, IExpr? parent)
        {
            exprCrossedTable.Left.Accept(this, exprCrossedTable);
            this.Builder.Append(exprCrossedTable.Outer ? " LEFT JOIN LATERAL" : " CROSS JOIN LATERAL ");
            exprCrossedTable.Right.Accept(this, exprCrossedTable);
            return true;
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

        protected override void AppendRecursiveCteKeyword()
        {
            this.Builder.Append("RECURSIVE ");
        }

        protected override bool SupportsInlineCte() => false;

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
                    this.Builder.Append(' ');
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

        public override bool VisitExprIdentityInsert(ExprIdentityInsert exprIdentityInsert, IExpr? parent)
        {
            if (exprIdentityInsert.IdentityColumns.Count < 1)
            {
                return exprIdentityInsert.Insert.Accept(this, exprIdentityInsert);
            }
            this.AddCteSlot(parent);
            this.GenericInsert(exprIdentityInsert.Insert,()=>this.Builder.Append(" OVERRIDING SYSTEM VALUE"), null);

            this.Builder.Append(';');
            var exprTableSource = new ExprTable(exprIdentityInsert.Insert.Target, null);
            foreach (var column in exprIdentityInsert.IdentityColumns)
            {
                this.Builder.Append("select setval(pg_get_serial_sequence('");
                exprIdentityInsert.Insert.Target.Accept(this, exprIdentityInsert.Insert);
                this.Builder.Append("','");
                this.EscapeStringLiteral(this.Builder, column.Name);
                this.Builder.Append("'),(");
                SqQueryBuilder.Select(SqQueryBuilder.Max(column.WithSource(null)))
                    .From(exprTableSource)
                    .Done()
                    .Accept(this, exprIdentityInsert);
                this.Builder.Append("));");
            }

            return true;
        }

        public override bool VisitExprUpdate(ExprUpdate exprUpdate, IExpr? parent)
        {
            this.AddCteSlot(parent);

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
            this.AddCteSlot(parent);

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
            this.AddCteSlot(parent);

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

        public override bool VisitExprTypeByteArray(ExprTypeByteArray exprTypeByte, IExpr? arg)
        {
            this.Builder.Append("bytea");
            return true;
        }

        public override bool VisitExprTypeFixSizeByteArray(ExprTypeFixSizeByteArray exprTypeFixSizeByteArray, IExpr? arg)
        {
            //PostgreSQL does not support binary type with fixed length
            this.Builder.Append("bytea");
            return true;
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

        public override bool VisitExprTypeDateTimeOffset(ExprTypeDateTimeOffset exprTypeDateTimeOffset, IExpr? arg)
        {
            this.Builder.Append("timestamp with time zone");
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

        public override bool VisitExprTypeFixSizeString(ExprTypeFixSizeString exprTypeFixSizeString, IExpr? arg)
        {
            this.Builder.Append("character");
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
                => SqQueryBuilder.ScalarFunctionSys("DATE_TRUNC", interval, EnsureLiteral(value));

            static ExprValue DatePart(string interval, ExprValue value)
                => SqQueryBuilder.ScalarFunctionSys("DATE_PART", interval, EnsureLiteral(value));

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
                this.Builder.Append(exprTableFullName.DbSchema!.Schema.Name);
                this.Builder.Append('.');
                this.Builder.Append(exprTableFullName.TableName.Name);
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
            this.Builder.Append('\"');
            this.AppendNameNoQuotas(name, prefix);
            this.Builder.Append('\"');
        }

        private void AppendNameNoQuotas(string name, char? prefix)
        {
            if (prefix.HasValue)
            {
                this.Builder.Append(prefix.Value);
            }
            SqlInjectionChecker.AppendStringEscapeDoubleQuote(this.Builder, name);
        }

        protected override void AppendUnicodePrefix(string str) { }

        protected override IStatementVisitor CreateStatementSqlBuilder()
            => new PgSqlStatementBuilder(this.Options, this.Builder);

        public override bool VisitExprMerge(ExprMerge merge, IExpr? parent)
        {
            MergeSimulation.ConvertMerge(merge, "tmpMergeDataSource").Accept(this, parent);
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

        private static bool VisitMergeNotSupported() =>
            throw new SqExpressException("Pg SQL does not support MERGE expression");
    }
}