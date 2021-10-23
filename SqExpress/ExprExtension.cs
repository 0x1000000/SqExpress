using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using SqExpress.DataAccess;
using SqExpress.QueryBuilders;
using SqExpress.QueryBuilders.Update;
using SqExpress.Syntax;
using SqExpress.SyntaxTreeOperations;
using SqExpress.SyntaxTreeOperations.ExportImport;
using SqExpress.SyntaxTreeOperations.ExportImport.Internal;
using SqExpress.SyntaxTreeOperations.Internal;

namespace SqExpress
{
    public static class ExprExtension
    {
        //Sync handler
        public static Task<TAgg> Query<TAgg>(this IExprQuery query, ISqDatabase database, TAgg seed, Func<TAgg,ISqDataRecordReader, TAgg> aggregator) 
            => database.Query(query, seed, aggregator);

        public static Task<TAgg> Query<TAgg>(this IExprQueryFinal query, ISqDatabase database, TAgg seed, Func<TAgg,ISqDataRecordReader, TAgg> aggregator) 
            => database.Query(query.Done(), seed, aggregator);

        public static Task Query(this IExprQuery query, ISqDatabase database, Action<ISqDataRecordReader> handler) 
            => database.Query(query, handler);

        public static Task Query(this IExprQueryFinal query, ISqDatabase database, Action<ISqDataRecordReader> handler) 
            => database.Query(query.Done(), handler);

        //Async handler
        public static Task<TAgg> Query<TAgg>(this IExprQuery query, ISqDatabase database, TAgg seed, Func<TAgg,ISqDataRecordReader, Task<TAgg>> aggregator) 
            => database.Query(query, seed, aggregator);

        public static Task<TAgg> Query<TAgg>(this IExprQueryFinal query, ISqDatabase database, TAgg seed, Func<TAgg,ISqDataRecordReader, Task<TAgg>> aggregator) 
            => database.Query(query.Done(), seed, aggregator);

        public static Task Query(this IExprQuery query, ISqDatabase database, Func<ISqDataRecordReader, Task> handler) 
            => database.Query(query, handler);

        public static Task Query(this IExprQueryFinal query, ISqDatabase database, Func<ISqDataRecordReader, Task> handler) 
            => database.Query(query.Done(), handler);

        public static Task<List<T>> QueryList<T>(this IExprQuery query, ISqDatabase database, Func<ISqDataRecordReader, T> factory) 
            => database.QueryList(query, factory);

        public static Task<List<T>> QueryList<T>(this IExprQueryFinal query, ISqDatabase database, Func<ISqDataRecordReader, T> factory) 
            => database.QueryList(query.Done(), factory);

        public static Task<Dictionary<TKey, TValue>> QueryDictionary<TKey, TValue>(
            this IExprQuery query, 
            ISqDatabase database, 
            Func<ISqDataRecordReader, TKey> keyFactory,
            Func<ISqDataRecordReader, TValue> valueFactory, 
            SqDatabaseExtensions.KeyDuplicationHandler<TKey, TValue>? keyDuplicationHandler = null,
            Func<TKey, TValue, bool>? predicate = null)
        where TKey : notnull
            => database.QueryDictionary(query, keyFactory, valueFactory, keyDuplicationHandler, predicate);

        public static Task<Dictionary<TKey, TValue>> QueryDictionary<TKey, TValue>(
            this IExprQueryFinal query, 
            ISqDatabase database, 
            Func<ISqDataRecordReader, TKey> keyFactory,
            Func<ISqDataRecordReader, TValue> valueFactory, 
            SqDatabaseExtensions.KeyDuplicationHandler<TKey, TValue>? keyDuplicationHandler = null,
            Func<TKey, TValue, bool>? predicate = null)
        where TKey : notnull
            => database.QueryDictionary(query.Done(), keyFactory, valueFactory, keyDuplicationHandler, predicate);

        public static Task<object> QueryScalar(this IExprQuery query, ISqDatabase database)
            => database.QueryScalar(query);

        public static Task<object> QueryScalar(this IExprQueryFinal query, ISqDatabase database)
            => database.QueryScalar(query.Done());

        public static Task Exec(this IExprExec query, ISqDatabase database)
            => database.Exec(query);

        public static Task Exec(this IExprExecFinal query, ISqDatabase database)
            => database.Exec(query.Done());

        public static Task Exec(this IUpdateDataBuilderFinal builder, ISqDatabase database)
            => database.Exec(builder.Done());

        public static SyntaxTreeActions SyntaxTree(this IExpr expr)
        {
            return new SyntaxTreeActions(expr);
        }

        public readonly struct SyntaxTreeActions
        {
            private readonly IExpr _expr;

            internal SyntaxTreeActions(IExpr expr)
            {
                this._expr = expr;
            }

            public void WalkThrough<TCtx>(IWalkerVisitor<TCtx> walkerVisitor, TCtx context)
            {
                this._expr.Accept(new ExprWalker<TCtx>(walkerVisitor), context);
            }

            public void WalkThrough<TCtx>(Func<IExpr, TCtx, VisitorResult<TCtx>> walker, TCtx context)
            {
                this._expr.Accept(new ExprWalker<TCtx>(new DefaultWalkerVisitor<TCtx>(walker)), context);
            }

            public IEnumerable<IExpr> Descendants()
            {
                return ExprWalkerPull.GetEnumerable(this._expr, false);
            }

            public IEnumerable<IExpr> DescendantsAndSelf()
            {
                return ExprWalkerPull.GetEnumerable(this._expr, true);
            }

            public TExpr? FirstOrDefault<TExpr>(Predicate<TExpr>? filter = null) where TExpr : class, IExpr
            {
                TExpr? result = null;
                this._expr.Accept(new ExprWalker<object?>(new DefaultWalkerVisitor<object?>((e, c) =>
                {
                    if (e is TExpr te && (filter == null || filter.Invoke(te)))
                    {
                        result = te;
                        return VisitorResult<object?>.Stop(c);
                    }
                    return VisitorResult<object?>.Continue(c);
                })), null);
                return result;
            }

            public IExpr? Modify(Func<IExpr, IExpr?> modifier)
            {
                return this._expr.Accept(ExprModifier.Instance, modifier);
            }

            public IExpr? Modify<TExpr>(Func<TExpr, IExpr?> modifier) where TExpr: IExpr
            {
                return this._expr.Accept(ExprModifier.Instance,
                    e =>
                    {
                        if (e is TExpr te)
                        {
                            return modifier(te);
                        }
                        return e;
                    });
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
#if NETCOREAPP
            public void ExportToJson(System.Text.Json.Utf8JsonWriter jsonWriter)
            {
                var walkerVisitor = new ExprJsonWriter();
                WalkThrough(walkerVisitor, jsonWriter);
            }
#endif
        }
    }
}