using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress.SqlExport.Internal
{
    internal class SqliteBuilder : PgSqlBuilder
    {
        public SqliteBuilder(SqlBuilderOptions? options = null, StringBuilder? externalBuilder = null)
            : base(options, externalBuilder)
        {
        }

        private SqliteBuilder(
            SqlBuilderOptions? options,
            StringBuilder? externalBuilder,
            SqlAliasGenerator aliasGenerator,
            bool dismissCteInject)
            : base(options, externalBuilder, aliasGenerator, dismissCteInject)
        {
        }

        protected override SqlBuilderBase CreateInstance(SqlAliasGenerator aliasGenerator, bool dismissCteInject)
        {
            return new SqliteBuilder(this.Options, new StringBuilder(), aliasGenerator, dismissCteInject);
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

        protected override bool VisitExprParameter(ExprParameter exprParameter, int paramNumber, IExpr? parent, out string? name)
        {
            name = "$" + paramNumber;
            this.Builder.Append(name);
            return true;
        }

        protected override DbParameterValueVisitorExtractor GetDbParameterValueVisitorExtractor()
            => DbParameterValueVisitorExtractor.Instance;

        protected override void AppendByteArrayLiteralPrefix()
        {
            this.Builder.Append("X'");
        }

        protected override void AppendByteArrayLiteralSuffix()
        {
            this.Builder.Append('\'');
        }

        public override bool VisitExprDateTimeOffsetLiteral(ExprDateTimeOffsetLiteral dateTimeLiteral, IExpr? arg)
        {
            if (!dateTimeLiteral.Value.HasValue)
            {
                this.AppendNull();
            }
            else
            {
                this.Builder.Append('\'');
                this.Builder.Append(dateTimeLiteral.Value.Value.ToString("O"));
                this.Builder.Append('\'');
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
                this.Builder.Append('\'');
                if (dateTimeLiteral.Value.Value.TimeOfDay != TimeSpan.Zero)
                {
                    this.Builder.Append(dateTimeLiteral.Value.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                }
                else
                {
                    this.Builder.Append(dateTimeLiteral.Value.Value.ToString("yyyy-MM-dd"));
                }
                this.Builder.Append('\'');
            }

            return true;
        }

        public override bool VisitExprGetDate(ExprGetDate exprGetDat, IExpr? parent)
        {
            this.Builder.Append("CURRENT_DATE");
            return true;
        }

        public override bool VisitExprGetUtcDate(ExprGetUtcDate exprGetUtcDate, IExpr? parent)
        {
            this.Builder.Append("CURRENT_TIMESTAMP");
            return true;
        }

        public override bool VisitExprTempTableName(ExprTempTableName tempTableName, IExpr? parent)
        {
            this.AppendName(tempTableName.Name);
            return true;
        }

        public override bool VisitExprDbSchema(ExprDbSchema exprDbSchema, IExpr? parent)
        {
            return true;
        }

        public override bool VisitExprTableFullName(ExprTableFullName exprTableFullName, IExpr? parent)
        {
            this.AppendName(exprTableFullName.TableName.Name);
            return true;
        }

        public override bool VisitExprOffsetFetch(ExprOffsetFetch exprOffsetFetch, IExpr? parent)
        {
            if (!ReferenceEquals(exprOffsetFetch.Fetch, null))
            {
                this.Builder.Append(" LIMIT ");
                exprOffsetFetch.Fetch.Accept(this, exprOffsetFetch);
            }

            this.Builder.Append(" OFFSET ");
            exprOffsetFetch.Offset.Accept(this, exprOffsetFetch);
            return true;
        }

        protected override bool ForceParenthesesForQueryExpressionPart(IExprSubQuery subQuery)
        {
            return false;
        }

        public override bool VisitExprDerivedTableValues(ExprDerivedTableValues derivedTableValues, IExpr? parent)
        {
            derivedTableValues.Columns.AssertNotEmpty("List of columns in a derived table with values literals cannot be empty");

            this.Builder.Append("(SELECT ");
            for (var i = 0; i < derivedTableValues.Columns.Count; i++)
            {
                if (i > 0)
                {
                    this.Builder.Append(',');
                }

                this.Builder.Append("column");
                this.Builder.Append(i + 1);
                this.Builder.Append(" AS ");
                derivedTableValues.Columns[i].Accept(this, derivedTableValues);
            }

            this.Builder.Append(" FROM ");
            this.AcceptPar('(', derivedTableValues.Values, ')', derivedTableValues);
            this.Builder.Append(')');
            this.Builder.Append(" AS ");
            derivedTableValues.Alias.Accept(this, derivedTableValues);
            return true;
        }

        public override bool VisitExprDerivedTableQuery(ExprDerivedTableQuery exprDerivedTableQuery, IExpr? parent)
        {
            this.AcceptPar('(', exprDerivedTableQuery.Query, ')', exprDerivedTableQuery);
            this.Builder.Append(" AS ");
            exprDerivedTableQuery.Alias.Accept(this, exprDerivedTableQuery);

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

        public override bool VisitExprDelete(ExprDelete exprDelete, IExpr? parent)
        {
            if (exprDelete.Source != null)
            {
                throw new SqExpressException("SQLite exporter does not support DELETE with source tables");
            }

            this.AddCteSlot(parent);

            this.Builder.Append("DELETE FROM ");
            exprDelete.Target.FullName.Accept(this, exprDelete);

            if (exprDelete.Filter != null)
            {
                this.Builder.Append(" WHERE ");
                var filter = exprDelete.Filter;
                if (exprDelete.Target.Alias != null)
                {
                    filter = (ExprBoolean)filter.SyntaxTree()
                        .Modify<ExprColumn>(cn => cn.Source != null && cn.Source.Equals(exprDelete.Target.Alias)
                            ? new ExprColumn(exprDelete.Target.FullName, cn.ColumnName)
                            : cn)!;
                }

                filter.Accept(this, exprDelete);
            }

            return true;
        }

        public override bool VisitExprDeleteOutput(ExprDeleteOutput exprDeleteOutput, IExpr? parent)
        {
            if (exprDeleteOutput.Delete.Source != null)
            {
                throw new SqExpressException("SQLite exporter does not support DELETE OUTPUT with source tables");
            }

            this.VisitExprDelete(exprDeleteOutput.Delete, exprDeleteOutput);
            this.AssertNotEmptyList(exprDeleteOutput.OutputColumns, "Output list in 'DELETE' statement cannot be empty");

            this.Builder.Append(" RETURNING ");

            var columns = exprDeleteOutput.OutputColumns;
            var targetAlias = exprDeleteOutput.Delete.Target.Alias;

            if (targetAlias != null)
            {
                columns = columns.SelectToReadOnlyList(column =>
                    column.Column.Source != null && column.Column.Source.Equals(targetAlias)
                        ? new ExprAliasedColumn(new ExprColumn(null, column.Column.ColumnName), column.Alias)
                        : column);
            }

            this.AcceptListComaSeparated(columns, exprDeleteOutput);

            return true;
        }

        public override bool VisitExprScalarFunction(ExprScalarFunction exprScalarFunction, IExpr? parent)
        {
            if (exprScalarFunction.Schema == null &&
                string.Equals(exprScalarFunction.Name.Name, "CONCAT", StringComparison.OrdinalIgnoreCase) &&
                exprScalarFunction.Arguments is { Count: > 0 })
            {
                for (var i = 0; i < exprScalarFunction.Arguments.Count; i++)
                {
                    if (i > 0)
                    {
                        this.Builder.Append("||");
                    }

                    exprScalarFunction.Arguments[i].Accept(this, exprScalarFunction);
                }

                return true;
            }

            exprScalarFunction.Name.Accept(this, exprScalarFunction);

            if (exprScalarFunction.Arguments != null)
            {
                this.AssertNotEmptyList(exprScalarFunction.Arguments, "Argument list cannot be empty");
                this.AcceptListComaSeparatedPar('(', exprScalarFunction.Arguments, ')', exprScalarFunction);
            }
            else
            {
                this.Builder.Append('(');
                this.Builder.Append(')');
            }

            return true;
        }

        public override bool VisitExprTableFunction(ExprTableFunction exprTableFunction, IExpr? arg)
        {
            exprTableFunction.Name.Accept(this, exprTableFunction);

            if (exprTableFunction.Arguments != null)
            {
                this.AssertNotEmptyList(exprTableFunction.Arguments, "Argument list cannot be empty");
                this.AcceptListComaSeparatedPar('(', exprTableFunction.Arguments, ')', exprTableFunction);
            }
            else
            {
                this.Builder.Append('(');
                this.Builder.Append(')');
            }

            return true;
        }

        public override bool VisitExprPortableScalarFunction(ExprPortableScalarFunction exprPortableScalarFunction, IExpr? arg)
        {
            switch (exprPortableScalarFunction.PortableFunction)
            {
                case PortableScalarFunction.NullIf:
                    AppendFunction("NULLIF", exprPortableScalarFunction, 2);
                    return true;
                case PortableScalarFunction.Abs:
                    AppendFunction("ABS", exprPortableScalarFunction, 1);
                    return true;
                case PortableScalarFunction.Lower:
                    AppendFunction("LOWER", exprPortableScalarFunction, 1);
                    return true;
                case PortableScalarFunction.Upper:
                    AppendFunction("UPPER", exprPortableScalarFunction, 1);
                    return true;
                case PortableScalarFunction.Trim:
                    AppendFunction("TRIM", exprPortableScalarFunction, 1);
                    return true;
                case PortableScalarFunction.LTrim:
                    AppendFunction("LTRIM", exprPortableScalarFunction, 1);
                    return true;
                case PortableScalarFunction.RTrim:
                    AppendFunction("RTRIM", exprPortableScalarFunction, 1);
                    return true;
                case PortableScalarFunction.Replace:
                    AppendFunction("REPLACE", exprPortableScalarFunction, 3);
                    return true;
                case PortableScalarFunction.Substring:
                    AppendFunction("SUBSTR", exprPortableScalarFunction, 3);
                    return true;
                case PortableScalarFunction.Round:
                    AppendFunction("ROUND", exprPortableScalarFunction, 2);
                    return true;
                case PortableScalarFunction.Floor:
                    AppendFloor(exprPortableScalarFunction.Arguments![0], exprPortableScalarFunction);
                    return true;
                case PortableScalarFunction.Ceiling:
                    AppendCeiling(exprPortableScalarFunction.Arguments![0], exprPortableScalarFunction);
                    return true;
                case PortableScalarFunction.Len:
                    AppendFunction("LENGTH", exprPortableScalarFunction, 1);
                    return true;
                case PortableScalarFunction.DataLen:
                    this.AssertArgumentsCount(exprPortableScalarFunction.Arguments, 1, exprPortableScalarFunction.PortableFunction);
                    this.Builder.Append("LENGTH(CAST(");
                    exprPortableScalarFunction.Arguments![0].Accept(this, exprPortableScalarFunction);
                    this.Builder.Append(" AS BLOB))");
                    return true;
                case PortableScalarFunction.Year:
                    AppendExtract(exprPortableScalarFunction, "%Y");
                    return true;
                case PortableScalarFunction.Month:
                    AppendExtract(exprPortableScalarFunction, "%m");
                    return true;
                case PortableScalarFunction.Day:
                    AppendExtract(exprPortableScalarFunction, "%d");
                    return true;
                case PortableScalarFunction.Hour:
                    AppendExtract(exprPortableScalarFunction, "%H");
                    return true;
                case PortableScalarFunction.Minute:
                    AppendExtract(exprPortableScalarFunction, "%M");
                    return true;
                case PortableScalarFunction.Second:
                    AppendExtract(exprPortableScalarFunction, "%S");
                    return true;
                case PortableScalarFunction.IndexOf:
                    this.AssertArgumentsCount(exprPortableScalarFunction.Arguments, 2, exprPortableScalarFunction.PortableFunction);
                    this.Builder.Append("INSTR(");
                    exprPortableScalarFunction.Arguments![1].Accept(this, exprPortableScalarFunction);
                    this.Builder.Append(',');
                    exprPortableScalarFunction.Arguments[0].Accept(this, exprPortableScalarFunction);
                    this.Builder.Append(')');
                    return true;
                case PortableScalarFunction.Left:
                    this.AssertArgumentsCount(exprPortableScalarFunction.Arguments, 2, exprPortableScalarFunction.PortableFunction);
                    this.Builder.Append("SUBSTR(");
                    exprPortableScalarFunction.Arguments![0].Accept(this, exprPortableScalarFunction);
                    this.Builder.Append(",1,MAX(");
                    exprPortableScalarFunction.Arguments[1].Accept(this, exprPortableScalarFunction);
                    this.Builder.Append(",0))");
                    return true;
                case PortableScalarFunction.Right:
                    this.AssertArgumentsCount(exprPortableScalarFunction.Arguments, 2, exprPortableScalarFunction.PortableFunction);
                    this.Builder.Append("CASE WHEN ");
                    exprPortableScalarFunction.Arguments![1].Accept(this, exprPortableScalarFunction);
                    this.Builder.Append("<=0 THEN '' ELSE SUBSTR(");
                    exprPortableScalarFunction.Arguments[0].Accept(this, exprPortableScalarFunction);
                    this.Builder.Append(",-(");
                    exprPortableScalarFunction.Arguments[1].Accept(this, exprPortableScalarFunction);
                    this.Builder.Append(")) END");
                    return true;
                case PortableScalarFunction.Repeat:
                    this.AssertArgumentsCount(exprPortableScalarFunction.Arguments, 2, exprPortableScalarFunction.PortableFunction);
                    this.Builder.Append("CASE WHEN ");
                    exprPortableScalarFunction.Arguments![1].Accept(this, exprPortableScalarFunction);
                    this.Builder.Append("<=0 THEN '' ELSE REPLACE(HEX(ZEROBLOB(");
                    exprPortableScalarFunction.Arguments[1].Accept(this, exprPortableScalarFunction);
                    this.Builder.Append(")),'00',");
                    exprPortableScalarFunction.Arguments[0].Accept(this, exprPortableScalarFunction);
                    this.Builder.Append(") END");
                    return true;
                default:
                    return base.VisitExprPortableScalarFunction(exprPortableScalarFunction, arg);
            }
        }

        public override bool VisitExprIdentityInsert(ExprIdentityInsert exprIdentityInsert, IExpr? parent)
        {
            this.AddCteSlot(parent);
            return exprIdentityInsert.Insert.Accept(this, exprIdentityInsert);
        }

        public override bool VisitExprDateAdd(ExprDateAdd exprDateAdd, IExpr? arg)
        {
            if (exprDateAdd.DatePart == DateAddDatePart.Year)
            {
                AppendClampedMonthAdd(exprDateAdd, exprDateAdd.Number * 12);
                return true;
            }

            if (exprDateAdd.DatePart == DateAddDatePart.Month)
            {
                AppendClampedMonthAdd(exprDateAdd, exprDateAdd.Number);
                return true;
            }

            if (exprDateAdd.DatePart == DateAddDatePart.Millisecond)
            {
                this.Builder.Append("STRFTIME('%Y-%m-%d %H:%M:%f',JULIANDAY(");
                exprDateAdd.Date.Accept(this, exprDateAdd);
                this.Builder.Append(")+(");
                this.Builder.Append(exprDateAdd.Number);
                this.Builder.Append("/86400000.0))");
                return true;
            }

            string interval = exprDateAdd.DatePart switch
            {
                DateAddDatePart.Year => " years",
                DateAddDatePart.Month => " months",
                DateAddDatePart.Day => " days",
                DateAddDatePart.Week => " days",
                DateAddDatePart.Hour => " hours",
                DateAddDatePart.Minute => " minutes",
                DateAddDatePart.Second => " seconds",
                _ => throw new ArgumentOutOfRangeException()
            };

            var number = exprDateAdd.DatePart == DateAddDatePart.Week
                ? exprDateAdd.Number * 7
                : exprDateAdd.Number;

            this.Builder.Append("DATETIME(");
            exprDateAdd.Date.Accept(this, exprDateAdd);
            this.Builder.Append(",'");
            if (number >= 0)
            {
                this.Builder.Append('+');
            }
            this.Builder.Append(number);
            this.Builder.Append(interval);
            this.Builder.Append("')");
            return true;
        }

        public override bool VisitExprDateDiff(ExprDateDiff exprDateDiff, IExpr? arg)
        {
            switch (exprDateDiff.DatePart)
            {
                case DateDiffDatePart.Year:
                    this.Builder.Append("CAST(STRFTIME('%Y',");
                    exprDateDiff.EndDate.Accept(this, exprDateDiff);
                    this.Builder.Append(") AS INTEGER)-CAST(STRFTIME('%Y',");
                    exprDateDiff.StartDate.Accept(this, exprDateDiff);
                    this.Builder.Append(") AS INTEGER)");
                    return true;
                case DateDiffDatePart.Month:
                    this.Builder.Append("((CAST(STRFTIME('%Y',");
                    exprDateDiff.EndDate.Accept(this, exprDateDiff);
                    this.Builder.Append(") AS INTEGER)-CAST(STRFTIME('%Y',");
                    exprDateDiff.StartDate.Accept(this, exprDateDiff);
                    this.Builder.Append(") AS INTEGER))*12)+(CAST(STRFTIME('%m',");
                    exprDateDiff.EndDate.Accept(this, exprDateDiff);
                    this.Builder.Append(") AS INTEGER)-CAST(STRFTIME('%m',");
                    exprDateDiff.StartDate.Accept(this, exprDateDiff);
                    this.Builder.Append(") AS INTEGER))");
                    return true;
                case DateDiffDatePart.Day:
                    AppendUnixDiff(exprDateDiff, "DATE(", ")", 86400);
                    return true;
                case DateDiffDatePart.Hour:
                    AppendUnixDiff(exprDateDiff, "STRFTIME('%Y-%m-%d %H:00:00',", ")", 3600);
                    return true;
                case DateDiffDatePart.Minute:
                    AppendUnixDiff(exprDateDiff, "STRFTIME('%Y-%m-%d %H:%M:00',", ")", 60);
                    return true;
                case DateDiffDatePart.Second:
                    AppendUnixDiff(exprDateDiff, "STRFTIME('%Y-%m-%d %H:%M:%S',", ")", 1);
                    return true;
                case DateDiffDatePart.Millisecond:
                    this.Builder.Append("CAST(ROUND((JULIANDAY(");
                    exprDateDiff.EndDate.Accept(this, exprDateDiff);
                    this.Builder.Append(")-JULIANDAY(");
                    exprDateDiff.StartDate.Accept(this, exprDateDiff);
                    this.Builder.Append("))*86400000.0) AS INTEGER)");
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool VisitExprMerge(ExprMerge merge, IExpr? parent)
        {
            MergeSimulation.ConvertMerge(merge, "tmpMergeDataSource", useTargetOnlyDelete: true).Accept(this, parent);
            return true;
        }

        public override bool VisitExprMergeOutput(ExprMergeOutput mergeOutput, IExpr? parent)
        {
            throw new SqExpressException("SQLite exporter does not support MERGE expressions");
        }

        public override bool VisitExprMergeMatchedUpdate(ExprMergeMatchedUpdate mergeMatchedUpdate, IExpr? parent)
        {
            throw new SqExpressException("SQLite exporter does not support MERGE expressions");
        }

        public override bool VisitExprMergeMatchedDelete(ExprMergeMatchedDelete mergeMatchedDelete, IExpr? parent)
        {
            throw new SqExpressException("SQLite exporter does not support MERGE expressions");
        }

        public override bool VisitExprExprMergeNotMatchedInsert(ExprExprMergeNotMatchedInsert exprMergeNotMatchedInsert, IExpr? parent)
        {
            throw new SqExpressException("SQLite exporter does not support MERGE expressions");
        }

        public override bool VisitExprExprMergeNotMatchedInsertDefault(ExprExprMergeNotMatchedInsertDefault exprExprMergeNotMatchedInsertDefault, IExpr? parent)
        {
            throw new SqExpressException("SQLite exporter does not support MERGE expressions");
        }

        public override bool VisitExprUpdate(ExprUpdate exprUpdate, IExpr? parent)
        {
            this.AddCteSlot(parent);

            this.AssertNotEmptyList(exprUpdate.SetClause, "'UPDATE' statement should have at least one set clause");

            this.Builder.Append("UPDATE ");
            exprUpdate.Target.FullName.Accept(this, exprUpdate);
            this.Builder.Append(" SET ");

            var targetAlias = exprUpdate.Target.Alias;

            for (int i = 0; i < exprUpdate.SetClause.Count; i++)
            {
                var setClause = exprUpdate.SetClause[i];
                if (i != 0)
                {
                    this.Builder.Append(',');
                }

                setClause.Column.ColumnName.Accept(this, exprUpdate);
                this.Builder.Append('=');
                RewriteTargetAlias(setClause.Value, targetAlias, exprUpdate.Target.FullName).Accept(this, exprUpdate);
            }

            ExprBoolean? sourceFilter = null;
            if (exprUpdate.Source != null)
            {
                IReadOnlyList<IExprTableSource> tables;
                (tables, sourceFilter) = exprUpdate.Source.ToTableMultiplication();
                this.AssertNotEmptyList(tables, "List of tables in 'UPDATE' statement cannot be empty");

                int itemAppendCount = 0;
                for (int i = 0; i < tables.Count; i++)
                {
                    var source = tables[i];

                    if (source is ExprTable sTable && sTable.Equals(exprUpdate.Target))
                    {
                        continue;
                    }

                    if (itemAppendCount == 0)
                    {
                        this.Builder.Append(" FROM ");
                    }
                    else
                    {
                        this.Builder.Append(',');
                    }

                    source.Accept(this, exprUpdate);
                    itemAppendCount++;
                }
            }

            var filter = Helpers.CombineNotNull(sourceFilter, exprUpdate.Filter, (l, r) => l & r);

            if (filter != null)
            {
                this.Builder.Append(" WHERE ");
                RewriteTargetAlias(filter, targetAlias, exprUpdate.Target.FullName).Accept(this, exprUpdate);
            }

            return true;
        }

        public override bool VisitExprTypeBoolean(ExprTypeBoolean exprTypeBoolean, IExpr? parent)
        {
            this.Builder.Append("INTEGER");
            return true;
        }

        public override bool VisitExprTypeByte(ExprTypeByte exprTypeByte, IExpr? parent)
        {
            this.Builder.Append("INTEGER");
            return true;
        }

        public override bool VisitExprTypeByteArray(ExprTypeByteArray exprTypeByte, IExpr? arg)
        {
            this.Builder.Append("BLOB");
            return true;
        }

        public override bool VisitExprTypeFixSizeByteArray(ExprTypeFixSizeByteArray exprTypeFixSizeByteArray, IExpr? arg)
        {
            this.Builder.Append("BLOB");
            return true;
        }

        public override bool VisitExprTypeInt16(ExprTypeInt16 exprTypeInt16, IExpr? parent)
        {
            this.Builder.Append("INTEGER");
            return true;
        }

        public override bool VisitExprTypeInt32(ExprTypeInt32 exprTypeInt32, IExpr? parent)
        {
            this.Builder.Append("INTEGER");
            return true;
        }

        public override bool VisitExprTypeInt64(ExprTypeInt64 exprTypeInt64, IExpr? parent)
        {
            this.Builder.Append("INTEGER");
            return true;
        }

        public override bool VisitExprTypeDecimal(ExprTypeDecimal exprTypeDecimal, IExpr? parent)
        {
            this.Builder.Append("NUMERIC");
            return true;
        }

        public override bool VisitExprTypeDouble(ExprTypeDouble exprTypeDouble, IExpr? parent)
        {
            this.Builder.Append("REAL");
            return true;
        }

        public override bool VisitExprTypeDateTime(ExprTypeDateTime exprTypeDateTime, IExpr? parent)
        {
            this.Builder.Append("TEXT");
            return true;
        }

        public override bool VisitExprTypeDateTimeOffset(ExprTypeDateTimeOffset exprTypeDateTimeOffset, IExpr? arg)
        {
            this.Builder.Append("TEXT");
            return true;
        }

        public override bool VisitExprTypeGuid(ExprTypeGuid exprTypeGuid, IExpr? parent)
        {
            this.Builder.Append("TEXT");
            return true;
        }

        public override bool VisitExprTypeString(ExprTypeString exprTypeString, IExpr? parent)
        {
            this.Builder.Append("TEXT");
            return true;
        }

        public override bool VisitExprTypeFixSizeString(ExprTypeFixSizeString exprTypeFixSizeString, IExpr? arg)
        {
            this.Builder.Append("TEXT");
            return true;
        }

        public override bool VisitExprTypeXml(ExprTypeXml exprTypeXml, IExpr? arg)
        {
            this.Builder.Append("TEXT");
            return true;
        }

        private static TExpr RewriteTargetAlias<TExpr>(TExpr expr, ExprTableAlias? targetAlias, IExprColumnSource? replacementSource = null)
            where TExpr : class, IExpr
        {
            if (targetAlias == null)
            {
                return expr;
            }

            return (TExpr)expr.SyntaxTree()
                .Modify<ExprColumn>(cn => cn.Source != null && cn.Source.Equals(targetAlias)
                    ? new ExprColumn(replacementSource, cn.ColumnName)
                    : cn)!;
        }

        private void AppendUnixDiff(ExprDateDiff exprDateDiff, string wrapperPrefix, string wrapperSuffix, int divisor)
        {
            this.Builder.Append("CAST((CAST(STRFTIME('%s',");
            this.Builder.Append(wrapperPrefix);
            exprDateDiff.EndDate.Accept(this, exprDateDiff);
            this.Builder.Append(wrapperSuffix);
            this.Builder.Append(") AS INTEGER)-CAST(STRFTIME('%s',");
            this.Builder.Append(wrapperPrefix);
            exprDateDiff.StartDate.Accept(this, exprDateDiff);
            this.Builder.Append(wrapperSuffix);
            this.Builder.Append(") AS INTEGER))");

            if (divisor != 1)
            {
                this.Builder.Append('/');
                this.Builder.Append(divisor);
            }

            this.Builder.Append(" AS INTEGER)");
        }

        private void AppendFunction(string functionName, ExprPortableScalarFunction exprPortableScalarFunction, int expectedArguments)
        {
            this.AssertArgumentsCount(exprPortableScalarFunction.Arguments, expectedArguments, exprPortableScalarFunction.PortableFunction);
            this.Builder.Append(functionName);
            this.AcceptListComaSeparatedPar('(', exprPortableScalarFunction.Arguments!, ')', exprPortableScalarFunction);
        }

        private void AppendExtract(ExprPortableScalarFunction exprPortableScalarFunction, string format)
        {
            this.AssertArgumentsCount(exprPortableScalarFunction.Arguments, 1, exprPortableScalarFunction.PortableFunction);
            this.Builder.Append("CAST(STRFTIME('");
            this.Builder.Append(format);
            this.Builder.Append("',");
            exprPortableScalarFunction.Arguments![0].Accept(this, exprPortableScalarFunction);
            this.Builder.Append(") AS INTEGER)");
        }

        private void AppendFloor(ExprValue value, ExprPortableScalarFunction exprPortableScalarFunction)
        {
            this.Builder.Append("CASE WHEN ");
            value.Accept(this, exprPortableScalarFunction);
            this.Builder.Append(">=CAST(");
            value.Accept(this, exprPortableScalarFunction);
            this.Builder.Append(" AS INTEGER) THEN CAST(");
            value.Accept(this, exprPortableScalarFunction);
            this.Builder.Append(" AS INTEGER) ELSE CAST(");
            value.Accept(this, exprPortableScalarFunction);
            this.Builder.Append(" AS INTEGER)-1 END");
        }

        private void AppendCeiling(ExprValue value, ExprPortableScalarFunction exprPortableScalarFunction)
        {
            this.Builder.Append("CASE WHEN ");
            value.Accept(this, exprPortableScalarFunction);
            this.Builder.Append("<=CAST(");
            value.Accept(this, exprPortableScalarFunction);
            this.Builder.Append(" AS INTEGER) THEN CAST(");
            value.Accept(this, exprPortableScalarFunction);
            this.Builder.Append(" AS INTEGER) ELSE CAST(");
            value.Accept(this, exprPortableScalarFunction);
            this.Builder.Append(" AS INTEGER)+1 END");
        }

        private void AppendClampedMonthAdd(ExprDateAdd exprDateAdd, int monthsToAdd)
        {
            this.Builder.Append("DATETIME(");
            this.Builder.Append("printf('%s-%02d',STRFTIME('%Y-%m',DATE(");
            exprDateAdd.Date.Accept(this, exprDateAdd);
            this.Builder.Append(",'start of month',");
            AppendSignedModifier(monthsToAdd, " months");
            this.Builder.Append(")),MIN(CAST(STRFTIME('%d',");
            exprDateAdd.Date.Accept(this, exprDateAdd);
            this.Builder.Append(") AS INTEGER),CAST(STRFTIME('%d',DATE(");
            exprDateAdd.Date.Accept(this, exprDateAdd);
            this.Builder.Append(",'start of month',");
            AppendSignedModifier(monthsToAdd + 1, " months");
            this.Builder.Append(",'-1 day')) AS INTEGER)))||SUBSTR(STRFTIME('%Y-%m-%d %H:%M:%f',");
            exprDateAdd.Date.Accept(this, exprDateAdd);
            this.Builder.Append("),11))");
        }

        private void AppendSignedModifier(int value, string unitSuffix)
        {
            this.Builder.Append('\'');
            if (value >= 0)
            {
                this.Builder.Append('+');
            }

            this.Builder.Append(value);
            this.Builder.Append(unitSuffix);
            this.Builder.Append('\'');
        }
    }
}
