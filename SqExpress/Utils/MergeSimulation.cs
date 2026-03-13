using System.Collections.Generic;
using System.Linq;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Internal;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;

namespace SqExpress.Utils
{
    internal static class MergeSimulation
    {
        public static ExprList ConvertMerge(ExprMerge merge, string tempTableName, bool useJoinBasedInsertAntiMatch = false)
        {
            var acc = new List<IExprExec>();

            var sourceAlias = GetSourceAlias(merge.Source);

            var keys = ExtractKeys(merge, sourceAlias);

            IReadOnlyDictionary<ExprColumnName, TableColumn>? hints = ExtractHints(merge, ExtractSourceColumnNames(merge.Source));

            var exprInsertIntoTmp = TempTableData.FromTableSourceInsert(
                tableSource: merge.Source,
                keys: keys.SourceKeys,
                tempTable: out var tempTable,
                name: tempTableName,
                alias: Alias.From(sourceAlias),
                hints: hints
            );

            acc.AddRange(exprInsertIntoTmp.Expressions);

            //MATCHED
            var e = WhenMatched(merge, tempTable);
            if (e != null)
            {
                acc.Add(e);
            }

            //NOT MATCHED BY TARGET
            e = WhenNotMatchedByTarget(merge, tempTable, keys, useJoinBasedInsertAntiMatch);
            if (e != null)
            {
                acc.Add(e);
            }

            //NOT MATCHED BY SOURCE
            e = WhenNotMatchedBySource(merge, tempTable);
            if (e != null)
            {
                acc.Add(e);
            }

            acc.Add(new ExprStatement(tempTable.Script.Drop()));
            return new ExprList(acc);
        }

        private static IReadOnlyDictionary<ExprColumnName, TableColumn>? ExtractHints(
            ExprMerge merge,
            IReadOnlyCollection<ExprColumnName> sourceColumns)
        {
            Dictionary<ExprColumnName, TableColumn>? result = null; 

            if (merge.WhenMatched is ExprMergeMatchedUpdate mu)
            {
                foreach (var exprColumnSetClause in mu.Set)
                {
                    if (exprColumnSetClause.Column is TableColumn targetColumn && exprColumnSetClause.Value is ExprColumn col && sourceColumns.Contains(col.ColumnName))
                    {
                        result ??= new();
                        result[col] = targetColumn;
                    }
                }
            }

            if (merge.WhenNotMatchedByTarget is ExprExprMergeNotMatchedInsert i)
            {
                for (var index = 0; index < i.Columns.Count; index++)
                {
                    var exprColumnName = i.Columns[index];
                    var assigning = i.Values[index];

                    if (assigning is ExprColumn col && sourceColumns.Contains(col.ColumnName))
                    {
                        if (merge.TargetTable is TableBase tb)
                        {
                            var targetColumn = tb.Columns.FirstOrDefault(x => x.ColumnName.Equals(exprColumnName));
                            if (!ReferenceEquals(targetColumn, null))
                            {
                                result ??= new();
                                result[col] = targetColumn;
                            }
                        }
                    }

                }
            }

            return result;
        }

        private static IExprAlias GetSourceAlias(IExprTableSource source)
        {
            return source.Alias?.Alias
                ?? throw new SqExpressException("MERGE simulation requires a source with an exposed alias");
        }

        private static IReadOnlyCollection<ExprColumnName> ExtractSourceColumnNames(IExprTableSource source)
        {
            var result = new List<ExprColumnName>();
            var selectings = source.ExtractSelecting();

            for (var i = 0; i < selectings.Count; i++)
            {
                if (selectings[i] is IExprNamedSelecting named && !string.IsNullOrWhiteSpace(named.OutputName))
                {
                    result.Add(new ExprColumnName(named.OutputName!));
                }
            }

            return result;
        }

        private static ExtractKeysResult ExtractKeys(ExprMerge merge, IExprAlias sourceAlias)
        {
            IExprAlias targetAlias = merge.TargetTable.Alias?.Alias ?? throw new SqExpressException("Target table should have an alias");

            var accSource = new List<ExprColumnName>();
            var accTarget = new List<ExprColumnName>();

            var eqs = merge.On.SyntaxTree().DescendantsAndSelf().OfType<ExprBooleanEq>();
            foreach (var exprBooleanEq in eqs)
            {
                if (exprBooleanEq.Left is ExprColumn left 
                    && exprBooleanEq.Right is ExprColumn right 
                    && left.Source is ExprTableAlias ta 
                    && right.Source is ExprTableAlias sa)
                {

                    if (sa.Alias.Equals(sourceAlias) && ta.Alias.Equals(targetAlias))
                    {
                        accTarget.Add(left.ColumnName);
                        accSource.Add(right.ColumnName);
                    }
                    else if(ta.Alias.Equals(sourceAlias) && sa.Alias.Equals(targetAlias))
                    {
                        accTarget.Add(right.ColumnName);
                        accSource.Add(left.ColumnName);
                    }
                }
            }

            return new ExtractKeysResult(accTarget, accSource);
        }

        private static IExprExec? WhenMatched(ExprMerge merge, TempTableBase tempTable)
        {
            IExprExec? e = null;
            if (merge.WhenMatched != null)
            {
                if (merge.WhenMatched is ExprMergeMatchedUpdate update)
                {
                    e = SqQueryBuilder
                        .Update(merge.TargetTable)
                        .Set(update.Set)
                        .From(merge.TargetTable)
                        .InnerJoin(tempTable, merge.On)
                        .Where(update.And);
                }
                else if (merge.WhenMatched is ExprMergeMatchedDelete delete)
                {
                    ExprBoolean filter = SqQueryBuilder.Exists(SqQueryBuilder.SelectOne()
                        .From(tempTable)
                        .Where(merge.On));

                    if (delete.And != null)
                    {
                        filter = filter & delete.And;
                    }

                    e = SqQueryBuilder.Delete(merge.TargetTable).From(merge.TargetTable).Where(filter);
                }
                else
                {
                    throw new SqExpressException($"Unknown type: '{merge.WhenMatched.GetType().Name}'");
                }
            }
            return e;
        }

        private static IExprExec? WhenNotMatchedByTarget(ExprMerge merge, TempTableBase tempTable, ExtractKeysResult keys, bool useJoinBasedInsertAntiMatch)
        {
            IExprExec? e = null;
            if (merge.WhenNotMatchedByTarget != null)
            {
                ExprBoolean BuildFilter(ExprBoolean? and)
                {
                    ExprBoolean filter = !SqQueryBuilder.Exists(SqQueryBuilder
                        .SelectOne()
                        .From(merge.TargetTable)
                        .Where(merge.On));

                    if (and != null)
                    {
                        filter = filter & and;
                    }

                    return filter;
                }

                IExprExec BuildOracleSafeInsert(IExprExec insert)
                {
                    var deleteMatchedFromTemp = SqQueryBuilder.Delete(tempTable)
                        .From(tempTable)
                        .InnerJoin(merge.TargetTable, merge.On)
                        .All();

                    return new ExprList(new IExprExec[] { deleteMatchedFromTemp, insert });
                }

                if (merge.WhenNotMatchedByTarget is ExprExprMergeNotMatchedInsert insert)
                {
                    var insertFromSelect = SqQueryBuilder.InsertInto(merge.TargetTable, insert.Columns)
                        .From(SqQueryBuilder.Select(insert.Values.SelectToReadOnlyList(i =>
                                i is ExprValue v
                                    ? v
                                    : throw new SqExpressException("DEFAULT value cannot be used in MERGE polyfill")))
                            .From(tempTable)
                            .Where(useJoinBasedInsertAntiMatch ? insert.And : BuildFilter(insert.And)));

                    e = useJoinBasedInsertAntiMatch
                        ? BuildOracleSafeInsert(insertFromSelect)
                        : insertFromSelect;
                }
                else if (merge.WhenNotMatchedByTarget is ExprExprMergeNotMatchedInsertDefault insertDefault)
                {
                    var insertFromSelect = SqQueryBuilder.InsertInto(merge.TargetTable, keys.TargetKeys)
                        .From(SqQueryBuilder.Select(keys.SourceKeys)
                            .From(tempTable)
                            .Where(useJoinBasedInsertAntiMatch ? insertDefault.And : BuildFilter(insertDefault.And)));

                    e = useJoinBasedInsertAntiMatch
                        ? BuildOracleSafeInsert(insertFromSelect)
                        : insertFromSelect;

                }
                else
                {
                    throw new SqExpressException($"Unknown type: '{merge.WhenNotMatchedByTarget.GetType().Name}'");
                }
            }
            return e;
        }

        private static IExprExec? WhenNotMatchedBySource(ExprMerge merge, TempTableBase tempTable)
        {
            IExprExec? e = null;
            if (merge.WhenNotMatchedBySource != null)
            {
                if (merge.WhenNotMatchedBySource is ExprMergeMatchedDelete delete)
                {
                    ExprBoolean filter = !SqQueryBuilder.Exists(SqQueryBuilder.SelectOne()
                        .From(tempTable)
                        .Where(merge.On));

                    if (delete.And != null)
                    {
                        filter = filter & delete.And;
                    }

                    e = SqQueryBuilder.Delete(merge.TargetTable).From(merge.TargetTable).Where(filter);
                }
                else if (merge.WhenNotMatchedBySource is ExprMergeMatchedUpdate update)
                {
                    ExprBoolean filter = !SqQueryBuilder.Exists(SqQueryBuilder.SelectOne()
                        .From(tempTable)
                        .Where(merge.On));
                    if (update.And != null)
                    {
                        filter = filter & update.And;
                    }

                    e = SqQueryBuilder.Update(merge.TargetTable).Set(update.Set).Where(filter);
                }
                else
                {
                    throw new SqExpressException($"Unknown type: '{merge.WhenNotMatchedBySource.GetType().Name}'");
                }
            }

            return e;
        }

        private readonly struct ExtractKeysResult
        {
            public readonly IReadOnlyList<ExprColumnName> TargetKeys;
            public readonly IReadOnlyList<ExprColumnName> SourceKeys;

            public ExtractKeysResult(IReadOnlyList<ExprColumnName> targetKeys, IReadOnlyList<ExprColumnName> sourceKeys)
            {
                this.TargetKeys = targetKeys;
                this.SourceKeys = sourceKeys;
            }
        }
    }
}
