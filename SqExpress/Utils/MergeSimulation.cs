using System.Collections.Generic;
using System.Linq;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Internal;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;

namespace SqExpress.Utils
{
    internal static class MergeSimulation
    {
        public static ExprList ConvertMerge(ExprMerge merge, string tempTableName)
        {
            var derivedTableValues = merge.Source as ExprDerivedTableValues;

            if (derivedTableValues == null)
            {
                throw new SqExpressException("Only derived table values can be used as a source in MERGE simulation");
            }

            var acc = new List<IExprExec>();

            var keys = ExtractKeys(merge, derivedTableValues.Alias.Alias);

            var exprInsertIntoTmp = TempTableData.FromDerivedTableValuesInsert(derivedTableValues, keys.SourceKeys, out var tempTable, name: tempTableName, alias: Alias.From(derivedTableValues.Alias.Alias));

            acc.AddRange(exprInsertIntoTmp.Expressions);

            //MATCHED
            var e = WhenMatched(merge, tempTable);
            if (e != null)
            {
                acc.Add(e);
            }

            //NOT MATCHED BY TARGET
            e = WhenNotMatchedByTarget(merge, tempTable, keys);
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

        private static IExprExec? WhenNotMatchedByTarget(ExprMerge merge, TempTableBase tempTable, ExtractKeysResult keys)
        {
            IExprExec? e = null;
            if (merge.WhenNotMatchedByTarget != null)
            {
                if (merge.WhenNotMatchedByTarget is ExprExprMergeNotMatchedInsert insert)
                {
                    var filter = !SqQueryBuilder.Exists(SqQueryBuilder
                        .SelectOne()
                        .From(merge.TargetTable)
                        .Where(merge.On));

                    if (insert.And != null)
                    {
                        filter = filter & insert.And;
                    }

                    e = SqQueryBuilder.InsertInto(merge.TargetTable, insert.Columns)
                        .From(SqQueryBuilder.Select(insert.Values.SelectToReadOnlyList(i =>
                                i is ExprValue v
                                    ? v
                                    : throw new SqExpressException("DEFAULT value cannot be used in MERGE polyfill")))
                            .From(tempTable)
                            .Where(filter));
                }
                else if (merge.WhenNotMatchedByTarget is ExprExprMergeNotMatchedInsertDefault insertDefault)
                {
                    var filter = !SqQueryBuilder.Exists(SqQueryBuilder
                        .SelectOne()
                        .From(merge.TargetTable)
                        .Where(merge.On));

                    if (insertDefault.And != null)
                    {
                        filter = filter & insertDefault.And;
                    }

                    e = SqQueryBuilder.InsertInto(merge.TargetTable, keys.TargetKeys)
                        .From(SqQueryBuilder.Select(keys.SourceKeys)
                            .From(tempTable)
                            .Where(filter));

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