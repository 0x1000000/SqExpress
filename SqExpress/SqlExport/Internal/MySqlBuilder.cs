using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SqExpress.SqlExport.Statement.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;
using SqExpress.SyntaxTreeOperations;
using SqExpress.Utils;

namespace SqExpress.SqlExport.Internal
{
    internal class MySqlBuilder : SqlBuilderBase
    {
        public MySqlBuilder(SqlBuilderOptions? options = null, StringBuilder? externalBuilder = null) : base(options, externalBuilder, new SqlAliasGenerator(), false)
        {
        }

        private MySqlBuilder(SqlBuilderOptions? options, StringBuilder? externalBuilder, SqlAliasGenerator aliasGenerator, bool dismissCteInject) 
            : base(options, externalBuilder, aliasGenerator, dismissCteInject)
        {
        }

        protected override SqlBuilderBase CreateInstance(SqlAliasGenerator aliasGenerator, bool dismissCteInject)
        {
            return new MySqlBuilder(this.Options, new StringBuilder(), aliasGenerator, dismissCteInject);
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

        public override bool VisitExprDateTimeOffsetLiteral(ExprDateTimeOffsetLiteral dateTimeLiteral, IExpr? arg)
        {
            throw new SqExpressException("My SQL does not support DateTimeOffset type");
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
            this.Builder.Append('0');
            this.Builder.Append('x');
        }

        protected override void AppendByteArrayLiteralSuffix()
        {
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

        protected override void AppendRecursiveCteKeyword()
        {
            this.Builder.Append("RECURSIVE ");
        }

        protected override bool SupportsInlineCte() => true;

        private static bool VisitMergeNotSupported() =>
            throw new SqExpressException("My SQL does not support MERGE expression");

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

        public override bool VisitExprExprMergeNotMatchedInsertDefault(
            ExprExprMergeNotMatchedInsertDefault exprExprMergeNotMatchedInsertDefault, IExpr? parent)
            => VisitMergeNotSupported();

        public override bool VisitExprInsert(ExprInsert exprInsert, IExpr? parent)
        {
            exprInsert = PrepareGenericInsert(exprInsert, out var middleHandler);

            this.GenericInsert(exprInsert, ()=>
            {
                if (middleHandler != null)
                {
                    this.Builder.Append(' ');
                    middleHandler();
                }
            }, null);

            return true;
        }

        protected ExprInsert PrepareGenericInsert(ExprInsert exprInsert, out Action? prefixBuilder)
        {
            //My SQL does not properly support "Derived Table Values"
            //Also take a look at VisitExprInsertQuery.
            List<ExprDerivedTableValues>? derivedTables = null;

            var result = exprInsert.Source.SyntaxTree()
                .Modify<ExprQuerySpecification>(query =>
                {
                    if (query.Where != null && query.From is ExprDerivedTableValues values)
                    {
                        derivedTables ??= new List<ExprDerivedTableValues>();
                        derivedTables.Add(values);

                        return query.WithFrom(
                            new ExprTable(
                                new ExprTableFullName(
                                    null,
                                    new ExprTableName(
                                        BuildNameByIndex(derivedTables.Count-1))),
                                values.Alias));
                    }

                    return query;
                });

            if (derivedTables != null)
            {
                prefixBuilder = () => PreInsert(this, this.Builder, derivedTables);
            }
            else
            {
                prefixBuilder = null;
            }

            var newSource = result as IExprInsertSource ?? throw new SqExpressException($"{nameof(IExprInsertSource)} was expected");

            return newSource != exprInsert.Source ? exprInsert.WithSource(newSource) : exprInsert;

            //Functions
            static string BuildNameByIndex(int index) => $"CTE_Derived_Table_{index}";

            static void PreInsert(SqlBuilderBase sqlBuilder, StringBuilder stringBuilder, IReadOnlyList<ExprDerivedTableValues> derivedTables)
            {
                stringBuilder.Append("WITH ");
                for (int i = 0; i < derivedTables.Count; i++)
                {
                    var derivedTable = derivedTables[i];
                    if (i != 0)
                    {
                        stringBuilder.Append(',');
                    }

                    stringBuilder.Append(BuildNameByIndex(i));

                    sqlBuilder.AcceptListComaSeparatedPar('(', derivedTable.Columns, ')', derivedTable);

                    stringBuilder.Append(" AS(");
                    derivedTable.Values.Accept(sqlBuilder, derivedTable);
                    stringBuilder.Append(")");
                }
            }
        }

        public override bool VisitExprInsertOutput(ExprInsertOutput exprInsertOutput, IExpr? parent)
        {
            var insertExpr = PrepareGenericInsert(exprInsertOutput.Insert, out var middleBuilder);

            this.GenericInsert(insertExpr,
                () =>
                {
                    if (middleBuilder != null)
                    {
                        this.Builder.Append(' ');
                        middleBuilder();
                    }
                },
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
            //Also take a look at PrepareGenericInsert where Derived Tables are replaced with CTE if select has "Where" expression
            var newQuery = exprInsertQuery.Query.SyntaxTree()
                .Modify<ExprQuerySpecification>(es =>
                {
                    if (es.From is ExprDerivedTableValues values)
                    {
                        var newSelecting = new List<IExprSelecting>(es.SelectList.Count);
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
                        return es.WithSelectList(newSelecting);
                    }

                    return es;
                });

            newQuery?.Accept(this, exprInsertQuery);
            return true;
        }

        public override bool VisitExprIdentityInsert(ExprIdentityInsert exprIdentityInsert, IExpr? arg)
        {
            return exprIdentityInsert.Insert.Accept(this, exprIdentityInsert);
        }

        public override bool VisitExprUpdate(ExprUpdate exprUpdate, IExpr? parent)
        {
            this.AssertNotEmptyList(exprUpdate.SetClause, "'UPDATE' statement should have at least one set clause");

            var derivedTableReplacements = new Dictionary<ExprDerivedTableValues, TempTableBase>();

            if (exprUpdate.Source != null)
            {
                exprUpdate = ModifySourceJoins(exprUpdate: exprUpdate, exprUpdate.Source);

                //Injecting derived tables 

                var derivedTables = new HashSet<ExprDerivedTableValues>(exprUpdate.SyntaxTree().Descendants().OfType<ExprDerivedTableValues>());

                foreach (var derivedTable in derivedTables)
                {
                    var keys = exprUpdate.Source!.SyntaxTree()
                        .Descendants()
                        .OfType<ExprBooleanEq>()
                        .Select(eq => GetKeyColumn(eq.Left, derivedTable) ?? GetKeyColumn(eq.Right, derivedTable))
                        .Where(i => !ReferenceEquals(i, null))
                        .ToList();

                    var insertExpr = TempTableData.FromDerivedTableValuesInsert(derivedTable, keys!, out var tTable, Alias.From(derivedTable.Alias.Alias));
                    insertExpr.Accept(this, null);
                    this.Builder.Append(";");
                    derivedTableReplacements.Add(derivedTable, tTable);
                }

                exprUpdate = (ExprUpdate)exprUpdate.SyntaxTree().Modify<ExprDerivedTableValues>(dt => derivedTableReplacements.TryGetValue(dt, out var r) ? r : dt)!;
            }

            this.Builder.Append("UPDATE ");
            if (exprUpdate.Source != null)
            {
                exprUpdate.Source.Accept(this, exprUpdate);
            }
            else
            {
                exprUpdate.Target.Accept(this, exprUpdate);
            }

            this.Builder.Append(" SET ");
            for (int i = 0; i < exprUpdate.SetClause.Count; i++)
            {
                var setClause = exprUpdate.SetClause[i];
                if (i != 0)
                {
                    this.Builder.Append(',');
                }
                setClause.Column.Accept(this, exprUpdate);
                this.Builder.Append('=');
                setClause.Value.Accept(this, exprUpdate);
            }

            if (exprUpdate.Filter != null)
            {
                this.Builder.Append(" WHERE ");
                exprUpdate.Filter.Accept(this, exprUpdate);
            }

            if (derivedTableReplacements.Count > 0)
            {
                this.Builder.Append(';');
                foreach (var tempTable in derivedTableReplacements.Values)
                {
                    tempTable.Script.Drop().Accept(this.GetStatementSqlBuilder());
                }
            }

            return true;

            static ExprColumnName? GetKeyColumn(ExprValue? exprValue, ExprDerivedTableValues derivedTable)
            {
                if (exprValue is ExprColumn column)
                {
                    if (column.Source != null && column.Source.Equals(derivedTable.Alias))
                    {
                        return column.ColumnName;
                    }
                }

                return null;
            }
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
                exprDelete = ModifySourceJoins(exprDelete, exprDelete.Source);
                var target = exprDelete.Target.Alias != null 
                    ? (IExprColumnSource)exprDelete.Target.Alias 
                    : exprDelete.Target.FullName;
                this.Builder.Append("DELETE ");
                target.Accept(this, exprDelete);
                this.Builder.Append(" FROM ");
                exprDelete.Source!.Accept(this, exprDelete);
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

        public override bool VisitExprTypeByteArray(ExprTypeByteArray exprTypeByte, IExpr? arg)
        {
            if (!exprTypeByte.Size.HasValue || exprTypeByte.Size.Value > 65535)
            {
                this.Builder.Append("longblob");
            }
            else
            {
                this.Builder.Append("varbinary(");
                this.Builder.Append(exprTypeByte.Size.Value.ToString());
                this.Builder.Append(')');
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

        public override bool VisitExprTypeDateTimeOffset(ExprTypeDateTimeOffset exprTypeDateTimeOffset, IExpr? arg)
        {
            this.Builder.Append("datetime");
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

        public override bool VisitExprTypeFixSizeString(ExprTypeFixSizeString exprTypeFixSizeString, IExpr? arg)
        {
            this.Builder.Append("char");

            this.Builder.Append('(');
            this.Builder.Append(exprTypeFixSizeString.Size.ToString());
            this.Builder.Append(')');

            if (exprTypeFixSizeString.IsUnicode)
            {
                this.Builder.Append(" character set utf8");
            }

            return true;
        }

        public override bool VisitExprTypeXml(ExprTypeXml exprTypeXml, IExpr? arg)
        {
            this.Builder.Append("text character set utf8");
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

        protected override IStatementVisitor CreateStatementSqlBuilder() 
            => new MySqlStatementBuilder(this.Options, this.Builder);

        private static TExpr ModifySourceJoins<TExpr>(TExpr exprUpdate, IExprTableSource tableSource) where TExpr : IExpr
        {

            var cteToModify = new HashSet<ExprCte>();
            var crossedToModify = new HashSet<ExprCrossedTable>();

            tableSource.SyntaxTree()
                .WalkThroughWithParent((e, parentNode, list) =>
                {
                    if (e is ExprCte cte && parentNode is IExprTableSource)
                    {
                        cteToModify.Add(cte);
                        return VisitorResult.StopNode(list);
                    }

                    if (e is ExprCrossedTable crossed)
                    {
                        crossedToModify.Add(crossed);
                    }

                    return VisitorResult.Continue(list);
                },
                    (object?)null);

            if (cteToModify.Count > 0 || crossedToModify.Count > 0)
            {
                exprUpdate = (TExpr)exprUpdate.SyntaxTree()
                    .Modify(e =>
                    {
                        if (cteToModify.Contains(e))
                        {
                            var cte = (ExprCte)e;

                            var subQueryAlias = cte.Alias ?? new ExprTableAlias(new ExprAlias(cte.Name));

                            return SqQueryBuilder
                                .Select(SqQueryBuilder.AllColumns())
                                .From(cte)
                                .Done()
                                .As(subQueryAlias);
                        }

                        if (crossedToModify.Contains(e))
                        {
                            var crossed = (ExprCrossedTable)e;
                            return new ExprJoinedTable(crossed.Left,
                                ExprJoinedTable.ExprJoinType.Inner,
                                crossed.Right,
                                SqQueryBuilder.Literal(1) == 1);
                        }

                        return e;
                    })!;
            }

            return exprUpdate;
        }
    }
}