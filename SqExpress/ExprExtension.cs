using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.QueryBuilders;
using SqExpress.Syntax;
using SqExpress.SyntaxExplorer;

namespace SqExpress
{
    public static class ExprExtension
    {
        public static Task<TAgg> Query<TAgg>(this IExprQuery query, ISqDatabase database, TAgg seed, Func<TAgg,ISqDataRecordReader, TAgg> aggregator) 
            => database.Query(query, seed, aggregator);

        public static Task<TAgg> Query<TAgg>(this IExprQueryFinal query, ISqDatabase database, TAgg seed, Func<TAgg,ISqDataRecordReader, TAgg> aggregator) 
            => database.Query(query.Done(), seed, aggregator);

        public static Task<List<T>> QueryList<T>(this IExprQuery query, ISqDatabase database, Func<ISqDataRecordReader, T> factory) 
            => database.QueryList(query, factory);

        public static Task<List<T>> QueryList<T>(this IExprQueryFinal query, ISqDatabase database, Func<ISqDataRecordReader, T> factory) 
            => database.QueryList(query.Done(), factory);

        public static Task<object> QueryScalar(this IExprQuery query, ISqDatabase database)
            => database.QueryScalar(query);

        public static Task<object> QueryScalar(this IExprQueryFinal query, ISqDatabase database)
            => database.QueryScalar(query.Done());

        public static Task Exec(this IExprExec query, ISqDatabase database)
            => database.Exec(query);

        public static Task Exec(this IExprExecFinal query, ISqDatabase database)
            => database.Exec(query.Done());

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

            public void WalkThrough<TCtx>(Func<IExpr, TCtx, VisitorResult<TCtx>> walker, TCtx context)
            {
                this._expr.Accept(new ExprWalker<TCtx>(walker), context);
            }

            public TExpr? FirstOrDefault<TExpr>(Predicate<TExpr> filter) where TExpr : class, IExpr
            {
                TExpr? result = null;
                this._expr.Accept(new ExprWalker<object?>((e, c) =>
                {
                    if (e is TExpr te && filter.Invoke(te))
                    {
                        result = te;
                        return VisitorResult<object?>.Stop(c);
                    }
                    return VisitorResult<object?>.Continue(c);
                }), null);
                return result;
            }

            public IExpr? Modify(Func<IExpr, IExpr?> modifier)
            {
                return this._expr.Accept(ExprModifier.Instance, modifier);
            }
        }
    }
}