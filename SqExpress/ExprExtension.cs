using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.QueryBuilders;

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
    }
}