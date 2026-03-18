using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SqExpress.DataAccess;
using SqExpress.QueryBuilders;
using SqExpress.QueryBuilders.Select;
using SqExpress.QueryBuilders.Update;
using SqExpress.SqlExport;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;
using SqExpress.SyntaxTreeOperations;
using SqExpress.SyntaxTreeOperations.ExportImport;
using SqExpress.SyntaxTreeOperations.ExportImport.Internal;
using SqExpress.SyntaxTreeOperations.Internal;

namespace SqExpress
{
    public static class ExprExtension
    {
        //Sync handler
        public static Task<TAgg> Query<TAgg>(this IExprQuery query, ISqDatabase database, TAgg seed, Func<TAgg,ISqDataRecordReader, TAgg> aggregator, CancellationToken cancellationToken = default) 
            => database.Query(query, seed, aggregator, cancellationToken);

        public static Task<TAgg> Query<TAgg>(this IExprQueryFinal query, ISqDatabase database, TAgg seed, Func<TAgg,ISqDataRecordReader, TAgg> aggregator, CancellationToken cancellationToken = default) 
            => database.Query(query.Done(), seed, aggregator, cancellationToken);

        public static Task Query(this IExprQuery query, ISqDatabase database, Action<ISqDataRecordReader> handler, CancellationToken cancellationToken = default) 
            => database.Query(query, handler, cancellationToken: cancellationToken);

        public static Task Query(this IExprQueryFinal query, ISqDatabase database, Action<ISqDataRecordReader> handler, CancellationToken cancellationToken = default) 
            => database.Query(query.Done(), handler, cancellationToken: cancellationToken);

        //Async handler
#if !NETSTANDARD
        public static IAsyncEnumerable<ISqDataRecordReader> Query(this IExprQuery query, ISqDatabase database, CancellationToken cancellationToken = default)
            => database.Query(query, cancellationToken);

        public static IAsyncEnumerable<ISqDataRecordReader> Query(this IExprQueryFinal query, ISqDatabase database, CancellationToken cancellationToken = default)
            => database.Query(query.Done(), cancellationToken);
#endif

        public static Task<TAgg> Query<TAgg>(this IExprQuery query, ISqDatabase database, TAgg seed, Func<TAgg,ISqDataRecordReader, Task<TAgg>> aggregator, CancellationToken cancellationToken = default) 
            => database.Query(query, seed, aggregator, cancellationToken);

        public static Task<TAgg> Query<TAgg>(this IExprQueryFinal query, ISqDatabase database, TAgg seed, Func<TAgg,ISqDataRecordReader, Task<TAgg>> aggregator, CancellationToken cancellationToken = default) 
            => database.Query(query.Done(), seed, aggregator, cancellationToken);

        public static Task Query(this IExprQuery query, ISqDatabase database, Func<ISqDataRecordReader, Task> handler, CancellationToken cancellationToken = default) 
            => database.Query(query, handler, cancellationToken: cancellationToken);

        public static Task Query(this IExprQueryFinal query, ISqDatabase database, Func<ISqDataRecordReader, Task> handler, CancellationToken cancellationToken = default) 
            => database.Query(query.Done(), handler, cancellationToken: cancellationToken);

        public static Task<List<T>> QueryList<T>(this IExprQuery query, ISqDatabase database, Func<ISqDataRecordReader, T> factory, CancellationToken cancellationToken = default) 
            => database.QueryList(query, factory, cancellationToken: cancellationToken);

        public static Task<List<T>> QueryList<T>(this IExprQueryFinal query, ISqDatabase database, Func<ISqDataRecordReader, T> factory, CancellationToken cancellationToken = default) 
            => database.QueryList(query.Done(), factory, cancellationToken: cancellationToken);

        public static Task<Dictionary<TKey, TValue>> QueryDictionary<TKey, TValue>(
            this IExprQuery query, 
            ISqDatabase database, 
            Func<ISqDataRecordReader, TKey> keyFactory,
            Func<ISqDataRecordReader, TValue> valueFactory, 
            SqDatabaseExtensions.KeyDuplicationHandler<TKey, TValue>? keyDuplicationHandler = null,
            Func<TKey, TValue, bool>? predicate = null,
            CancellationToken cancellationToken = default)
        where TKey : notnull
            => database.QueryDictionary(query, keyFactory, valueFactory, keyDuplicationHandler, predicate, cancellationToken: cancellationToken);

        public static Task<Dictionary<TKey, TValue>> QueryDictionary<TKey, TValue>(
            this IExprQueryFinal query, 
            ISqDatabase database, 
            Func<ISqDataRecordReader, TKey> keyFactory,
            Func<ISqDataRecordReader, TValue> valueFactory, 
            SqDatabaseExtensions.KeyDuplicationHandler<TKey, TValue>? keyDuplicationHandler = null,
            Func<TKey, TValue, bool>? predicate = null)
        where TKey : notnull
            => database.QueryDictionary(query.Done(), keyFactory, valueFactory, keyDuplicationHandler, predicate);

        public static Task<DataPage<T>> QueryPage<T>(this ISelectOffsetFetchBuilderFinal builder, ISqDatabase database, Func<ISqDataRecordReader, T> reader, CancellationToken cancellationToken = default)
            => builder.Done().QueryPage(database, reader, cancellationToken: cancellationToken);

        public static async Task<DataPage<T>> QueryPage<T>(this ExprSelectOffsetFetch query, ISqDatabase database, Func<ISqDataRecordReader, T> reader, CancellationToken cancellationToken = default)
        {
            var countColumn = CustomColumnFactory.Int32("$count$");

            var selectQuery = (ExprQuerySpecification)query.SelectQuery;

            query = query.WithSelectQuery(
                selectQuery.WithSelectList(selectQuery.SelectList.Combine(SqQueryBuilder.CountOne().Over().As(countColumn))));

            var res = await query.Query(database,
                new KeyValuePair<List<T>, int?>(new List<T>(), null),
                (acc, r) =>
                {
                    acc.Key.Add(reader(r));
                    var total = acc.Value ?? countColumn.Read(r);
                    return new KeyValuePair<List<T>, int?>(acc.Key, total);
                });

            var offsetLiteral = query.OrderBy.OffsetFetch.Offset as ExprInt32Literal;

            return new DataPage<T>(res.Key, offsetLiteral?.Value ?? 0, res.Value ?? 0);
        }

        public static Task<object?> QueryScalar(this IExprQuery query, ISqDatabase database, CancellationToken cancellationToken = default)
            => database.QueryScalar(query, cancellationToken);

        public static Task<object?> QueryScalar(this IExprQueryFinal query, ISqDatabase database, CancellationToken cancellationToken = default)
            => database.QueryScalar(query.Done(), cancellationToken);

        public static Task Exec(this IExprExec query, ISqDatabase database, CancellationToken cancellationToken = default)
            => database.Exec(query, cancellationToken);

        public static Task Exec(this IExprExecFinal query, ISqDatabase database, CancellationToken cancellationToken = default)
            => database.Exec(query.Done(), cancellationToken);

        public static Task Exec(this IUpdateDataBuilderFinal builder, ISqDatabase database, CancellationToken cancellationToken = default)
            => database.Exec(builder.Done(), cancellationToken);

        public static string ToSql(this IExpr expr, ISqlExporter exporter) 
            => exporter.ToSql(expr);

        public static string ToSql(this IExprExecFinal expr, ISqlExporter exporter)
            => expr.Done().ToSql(exporter);

        public static string ToSql(this IExprQueryFinal expr, ISqlExporter exporter)
            => expr.Done().ToSql(exporter);

        public static string ToSql(this IStatement expr, ISqlExporter exporter)
            => exporter.ToSql(expr);

        public static T WithParams<T>(this T expr, IReadOnlyDictionary<string, ParamValue> values) where T : IExpr
        {
            if (values.Count == 0)
            {
                return expr;
            }

            var normalizedValues = NormalizeParamDictionary(values);

            var result = expr.SyntaxTree()
                .ModifyDescendants(e =>
                    {
                        if (e is ExprInValues inValues)
                        {
                            return ReplaceInValues(inValues, normalizedValues);
                        }

                        if (e is ExprParameter parameter)
                        {
                            var tagName = parameter.TagName;
                            if (tagName != null && tagName.Length > 0 && normalizedValues.TryGetValue(tagName, out var value))
                            {
                                if (value.IsSingle)
                                {
                                    return value.AsSingle;
                                }

                                return e;
                            }

                            if (tagName != null && tagName.Length > 0)
                            {
                                throw new SqExpressException($"Could not find parameter {tagName}");
                            }
                        }

                        return e;
                    }
                );

            EnsureNoListParamsOutsideIn(result, normalizedValues);
            return result;
        }

        public static IExprQuery AsQuery(this IExpr expr)
            => EnsureQuery(expr);

        public static IExprExec AsNonQuery(this IExpr expr)
            => EnsureNonQuery(expr);

#if NET8_0_OR_GREATER

        public static T WithParams<T>(this T expr, string paramName, ParamValue paramValue) where T : IExpr 
            => WithParams(expr, [(paramName, paramValue)]);

        public static T WithParams<T>(this T expr, params ReadOnlySpan<(string paramName, ParamValue paramExprValue)> values) where T: IExpr
        {
            if (values.Length == 0)
            {
                return expr;
            }

            if (values.Length <= 4)
            {
                string? n0 = null;
                string? n1 = null;
                string? n2 = null;
                string? n3 = null;
                ParamValue? v0 = null;
                ParamValue? v1 = null;
                ParamValue? v2 = null;
                ParamValue? v3 = null;

                for (int i = 0; i < values.Length; i++)
                {
                    var (paramName, paramExprValue) = values[i];
                    paramName = NormalizeParamName(paramName);

                    if ((n0 != null && StringComparer.Ordinal.Equals(paramName, n0))
                        || (n1 != null && StringComparer.Ordinal.Equals(paramName, n1))
                        || (n2 != null && StringComparer.Ordinal.Equals(paramName, n2))
                        || (n3 != null && StringComparer.Ordinal.Equals(paramName, n3)))
                    {
                        throw new SqExpressException($"Duplicate parameter name '{paramName}'");
                    }

                    switch (i)
                    {
                        case 0:
                            n0 = paramName;
                            v0 = paramExprValue;
                            break;
                        case 1:
                            n1 = paramName;
                            v1 = paramExprValue;
                            break;
                        case 2:
                            n2 = paramName;
                            v2 = paramExprValue;
                            break;
                        case 3:
                            n3 = paramName;
                            v3 = paramExprValue;
                            break;
                    }
                }

                var result = expr.SyntaxTree()
                    .ModifyDescendants(e =>
                        {
                            if (e is ExprInValues inValues)
                            {
                                return ReplaceInValues(inValues, n0, v0, n1, v1, n2, v2, n3, v3);
                            }

                            if (e is ExprParameter parameter)
                            {
                                var tagName = parameter.TagName;
                                if (tagName is { Length: > 0 })
                                {
                                    if (n0 != null && StringComparer.Ordinal.Equals(tagName, n0) && v0.HasValue)
                                    {
                                        return v0.Value.IsSingle ? v0.Value.AsSingle : e;
                                    }
                                    if (n1 != null && StringComparer.Ordinal.Equals(tagName, n1) && v1.HasValue)
                                    {
                                        return v1.Value.IsSingle ? v1.Value.AsSingle : e;
                                    }
                                    if (n2 != null && StringComparer.Ordinal.Equals(tagName, n2) && v2.HasValue)
                                    {
                                        return v2.Value.IsSingle ? v2.Value.AsSingle : e;
                                    }
                                    if (n3 != null && StringComparer.Ordinal.Equals(tagName, n3) && v3.HasValue)
                                    {
                                        return v3.Value.IsSingle ? v3.Value.AsSingle : e;
                                    }

                                    throw new SqExpressException($"Could not find parameter {tagName}");
                                }
                            }

                            return e;
                        }
                    );
                EnsureNoListParamsOutsideIn(result, n0, v0, n1, v1, n2, v2, n3, v3);
                return result;
            }

            var dictionary = new Dictionary<string, ParamValue>(values.Length, StringComparer.Ordinal);
            for (int i = 0; i < values.Length; i++)
            {
                var (paramName, paramExprValue) = values[i];
                paramName = NormalizeParamName(paramName);

                if (!dictionary.TryAdd(paramName, paramExprValue))
                {
                    throw new SqExpressException($"Duplicate parameter name '{paramName}'");
                }
            }

            return expr.WithParams(dictionary);
        }
#endif

        private static ExprInValues ReplaceInValues(ExprInValues inValues, IReadOnlyDictionary<string, ParamValue> values)
        {
            List<ExprValue>? newItems = null;

            for (var index = 0; index < inValues.Items.Count; index++)
            {
                var item = inValues.Items[index];
                if (item is ExprParameter { TagName: { Length: > 0 } tagName } && values.TryGetValue(tagName, out var value))
                {
                    newItems ??= new List<ExprValue>(inValues.Items.Count);
                    if (newItems.Count == 0 && index > 0)
                    {
                        for (var j = 0; j < index; j++)
                        {
                            newItems.Add(inValues.Items[j]);
                        }
                    }

                    if (value.IsSingle)
                    {
                        newItems.Add(value.AsSingle);
                    }
                    else
                    {
                        foreach (var listValue in value.AsList)
                        {
                            newItems.Add(listValue);
                        }
                    }
                }
                else if (newItems != null)
                {
                    newItems.Add(item);
                }
            }

            return newItems != null ? new ExprInValues(inValues.TestExpression, newItems) : inValues;
        }

#if NET8_0_OR_GREATER
        private static ExprInValues ReplaceInValues(
            ExprInValues inValues,
            string? n0,
            ParamValue? v0,
            string? n1,
            ParamValue? v1,
            string? n2,
            ParamValue? v2,
            string? n3,
            ParamValue? v3)
        {
            List<ExprValue>? newItems = null;

            for (var index = 0; index < inValues.Items.Count; index++)
            {
                var item = inValues.Items[index];
                ParamValue? replacement = null;

                if (item is ExprParameter { TagName: { Length: > 0 } tagName })
                {
                    if (n0 != null && StringComparer.Ordinal.Equals(tagName, n0))
                    {
                        replacement = v0;
                    }
                    else if (n1 != null && StringComparer.Ordinal.Equals(tagName, n1))
                    {
                        replacement = v1;
                    }
                    else if (n2 != null && StringComparer.Ordinal.Equals(tagName, n2))
                    {
                        replacement = v2;
                    }
                    else if (n3 != null && StringComparer.Ordinal.Equals(tagName, n3))
                    {
                        replacement = v3;
                    }
                }

                if (replacement.HasValue)
                {
                    newItems ??= new List<ExprValue>(inValues.Items.Count);
                    if (newItems.Count == 0 && index > 0)
                    {
                        for (var j = 0; j < index; j++)
                        {
                            newItems.Add(inValues.Items[j]);
                        }
                    }

                    if (replacement.Value.IsSingle)
                    {
                        newItems.Add(replacement.Value.AsSingle);
                    }
                    else
                    {
                        foreach (var listValue in replacement.Value.AsList)
                        {
                            newItems.Add(listValue);
                        }
                    }
                }
                else if (newItems != null)
                {
                    newItems.Add(item);
                }
            }

            return newItems != null ? new ExprInValues(inValues.TestExpression, newItems) : inValues;
        }
#endif

        private static void EnsureNoListParamsOutsideIn<T>(T expr, IReadOnlyDictionary<string, ParamValue> values) where T : IExpr
        {
            foreach (var parameter in expr.SyntaxTree().DescendantsAndSelf())
            {
                if (parameter is ExprParameter { TagName: { Length: > 0 } tagName }
                    && values.TryGetValue(tagName, out var value)
                    && value.IsList)
                {
                    throw new SqExpressException($"List parameter {tagName} can be used only in IN(...)");
                }
            }
        }

        private static IReadOnlyDictionary<string, ParamValue> NormalizeParamDictionary(IReadOnlyDictionary<string, ParamValue> values)
        {
            var result = new Dictionary<string, ParamValue>(values.Count, StringComparer.Ordinal);

            foreach (var pair in values)
            {
                var paramName = NormalizeParamName(pair.Key);
                if (result.ContainsKey(paramName))
                {
                    throw new SqExpressException($"Duplicate parameter name '{paramName}'");
                }

                result.Add(paramName, pair.Value);
            }

            return result;
        }

        private static string NormalizeParamName(string? paramName)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                throw new SqExpressException("Parameter name cannot be null or empty");
            }

            var notNullParamName = paramName!;

            var index = 0;
            while (index < notNullParamName.Length && notNullParamName[index] == '@')
            {
                index++;
            }

            if (index == notNullParamName.Length)
            {
                throw new SqExpressException("Parameter name cannot be null or empty");
            }

            return index == 0 ? notNullParamName : notNullParamName.Substring(index);
        }

        private static IExprQuery EnsureQuery(IExpr expr)
        {
            if (expr is IExprQuery query)
            {
                return query;
            }

            throw new SqExpressException(
                $"Expression '{expr.GetType().Name}' is not a query. Use {nameof(AsNonQuery)}() for INSERT/UPDATE/DELETE/MERGE statements.");
        }

        private static IExprExec EnsureNonQuery(IExpr expr)
        {
            if (expr is IExprExec exec)
            {
                return exec;
            }

            throw new SqExpressException(
                $"Expression '{expr.GetType().Name}' is not a non-query statement. Use {nameof(AsQuery)}() for SELECT statements.");
        }

#if NET8_0_OR_GREATER
        private static void EnsureNoListParamsOutsideIn<T>(
            T expr,
            string? n0,
            ParamValue? v0,
            string? n1,
            ParamValue? v1,
            string? n2,
            ParamValue? v2,
            string? n3,
            ParamValue? v3) where T : IExpr
        {
            foreach (var parameter in expr.SyntaxTree().DescendantsAndSelf())
            {
                if (parameter is not ExprParameter { TagName: { Length: > 0 } tagName })
                {
                    continue;
                }

                if (n0 != null && StringComparer.Ordinal.Equals(tagName, n0) && v0.HasValue && v0.Value.IsList)
                {
                    throw new SqExpressException($"List parameter {tagName} can be used only in IN(...)");
                }

                if (n1 != null && StringComparer.Ordinal.Equals(tagName, n1) && v1.HasValue && v1.Value.IsList)
                {
                    throw new SqExpressException($"List parameter {tagName} can be used only in IN(...)");
                }

                if (n2 != null && StringComparer.Ordinal.Equals(tagName, n2) && v2.HasValue && v2.Value.IsList)
                {
                    throw new SqExpressException($"List parameter {tagName} can be used only in IN(...)");
                }

                if (n3 != null && StringComparer.Ordinal.Equals(tagName, n3) && v3.HasValue && v3.Value.IsList)
                {
                    throw new SqExpressException($"List parameter {tagName} can be used only in IN(...)");
                }
            }
        }
#endif

        public static Task Exec(this IStatement expr, ISqDatabase database, CancellationToken cancellationToken = default)
            => database.Statement(expr, cancellationToken);

        public static SyntaxTreeActions<TExpr> SyntaxTree<TExpr>(this TExpr expr) where TExpr : IExpr
        {
            return new SyntaxTreeActions<TExpr>(expr);
        }

        public readonly struct SyntaxTreeActions<TExpr> where TExpr : IExpr
        {
            private readonly TExpr _expr;

            internal SyntaxTreeActions(TExpr expr)
            {
                this._expr = expr;
            }

            public void WalkThrough<TCtx>(IWalkerVisitor<TCtx> walkerVisitor, TCtx context)
            {
                this._expr.Accept(new ExprWalker<TCtx>(walkerVisitor), new WalkerContext<TCtx>(null, context));
            }

            public TCtx WalkThrough<TCtx>(Func<IExpr, TCtx, VisitorResult<TCtx>> walker, TCtx context)
            {
                var walkerVisitor = new DefaultWalkerVisitor<TCtx>(walker, context);
                this._expr.Accept(new ExprWalker<TCtx>(walkerVisitor), new WalkerContext<TCtx>(null, context));
                return walkerVisitor.CurrentCtx;
            }

            public void WalkThroughWithParent<TCtx>(IWalkerVisitorWithParent<TCtx> walkerVisitor, TCtx context)
            {
                this._expr.Accept(new ExprWalker<TCtx>(walkerVisitor), new WalkerContext<TCtx>(null, context));
            }

            public TCtx WalkThroughWithParent<TCtx>(Func<IExpr, IExpr?, TCtx, VisitorResult<TCtx>> walker, TCtx context)
            {
                var walkerVisitor = new DefaultParentWalkerVisitorWithParent<TCtx>(walker, context);
                this._expr.Accept(new ExprWalker<TCtx>(walkerVisitor), new WalkerContext<TCtx>(null, context));
                return walkerVisitor.CurrentCtx;
            }

            public IEnumerable<IExpr> Descendants()
            {
                return ExprWalkerPull.GetEnumerable(this._expr, false);
            }

            public IEnumerable<IExpr> DescendantsAndSelf()
            {
                return ExprWalkerPull.GetEnumerable(this._expr, true);
            }

            public TExprNode? FirstOrDefault<TExprNode>(Predicate<TExprNode>? filter = null) where TExprNode : class, IExpr
            {
                TExprNode? result = null;
                this._expr.Accept(new ExprWalker<object?>(new DefaultWalkerVisitor<object?>((e, c) =>
                {
                    if (e is TExprNode te && (filter == null || filter.Invoke(te)))
                    {
                        result = te;
                        return VisitorResult<object?>.Stop(c);
                    }
                    return VisitorResult<object?>.Continue(c);
                })), new WalkerContext<object?>(null, null));
                return result;
            }

            public IExpr? Modify(Func<IExpr, IExpr?> modifier)
            {
                return this._expr.Accept(new ExprModifier(), modifier);
            }

            public IExpr? Modify<TExprNode>(Func<TExprNode, IExpr?> modifier) where TExprNode: IExpr
            {
                return this._expr.Accept(new ExprModifier(),
                    e =>
                    {
                        if (e is TExprNode te)
                        {
                            return modifier(te);
                        }
                        return e;
                    });
            }

            public TExpr ModifyDescendants(Func<IExpr, IExpr?> modifier)
            {
                var thisExpr = this._expr;
                return (TExpr)thisExpr.Accept(new ExprModifier(),
                    e =>
                    {
                        if (!ReferenceEquals(e, thisExpr))
                        {
                            return modifier(e);
                        }
                        return e;
                    })!;
            }

            public TExpr ModifyDescendants<TExprNode>(Func<TExprNode, IExpr?> modifier) where TExprNode : IExpr
            {
                var thisExpr = this._expr;
                return (TExpr)thisExpr.Accept(new ExprModifier(),
                    e =>
                    {
                        if (!ReferenceEquals(e, thisExpr) && e is TExprNode te)
                        {
                            return modifier(te);
                        }
                        return e;
                    })!;
            }

            public IReadOnlyList<T> ExportToPlainList<T>(PlainItemFactory<T> plainItemFactory) where T : IPlainItem
            {
                var walkerVisitor = new ExprPlainWriter<T>(plainItemFactory);
                WalkThrough(walkerVisitor, 0);
                return walkerVisitor.Result;
            }

            public void ExportToXml(XmlWriter xmlWriter)
            {
                var walkerVisitor = new ExprXmlWriter();
                WalkThrough(walkerVisitor, xmlWriter);
            }
#if !NETSTANDARD
            public void ExportToJson(System.Text.Json.Utf8JsonWriter jsonWriter)
            {
                var walkerVisitor = new ExprJsonWriter();
                WalkThrough(walkerVisitor, jsonWriter);
            }
#endif

            internal IExpr ParametrizeLiterals(int? limit, out int numOfParams, out int numOfSkips)
            {
                var counter = 0;
                var skipsCounter = 0;

                var res = this._expr.SyntaxTree()
                    .Modify(e =>
                        {
                            if (e is ExprLiteral v)
                            {
                                if (limit.HasValue && counter < limit.Value)
                                {
                                    counter++;
                                    return new ExprParameter(v, null);
                                }
                                skipsCounter++;
                            }

                            return e;
                        }
                    );
                numOfParams = counter;
                numOfSkips = skipsCounter;
                return res!;
            }
        }
    }
}
