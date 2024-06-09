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
using SqExpress.Syntax.Select;
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

            return new DataPage<T>(res.Key, query.OrderBy.OffsetFetch.Offset.Value ?? 0, res.Value ?? 0);
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
        }
    }
}