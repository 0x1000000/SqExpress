using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.ModelSelect;
using SqExpress.Syntax.Select;

namespace SqExpress
{
    public static class ModelRequestExtensions
    {
        public static Task<List<TRes>> QueryList<TRes>(this ModelRequestData<TRes> modelRequestData, ISqDatabase database)
            => modelRequestData.Expr.QueryList(database, modelRequestData.Mapper);

        public static Task Query<TRes>(this ModelRequestData<TRes> modelRequestData, ISqDatabase database, Action<TRes> handler)
            => modelRequestData.Expr.Query(database, r=> handler(modelRequestData.Mapper(r)));

        public static Task<TAcc> Query<TRes,TAcc>(this ModelRequestData<TRes> modelRequestData, ISqDatabase database, TAcc seed, Func<TAcc, TRes, TAcc> handler)
            => modelRequestData.Expr.Query(database, seed, (acc, r)=> handler(acc, modelRequestData.Mapper(r)));

        public static Task<DataPage<TRes>> QueryPage<TRes>(this ModelRangeRequestData<TRes> modelRequestData, ISqDatabase database)
            => modelRequestData.Expr.QueryPage(database, modelRequestData.Mapper);

        internal static async Task<DataPage<T>> QueryPage<T>(this ExprSelectOffsetFetch query, ISqDatabase database, Func<ISqDataRecordReader, T> reader)
        {
            var countColumn = CustomColumnFactory.Int32("$count");

            var selectQuery = (ExprQuerySpecification)query.SelectQuery;

            query = query.WithSelectQuery(
                selectQuery.WithSelectList(selectQuery.SelectList.Concat(SqQueryBuilder.CountOneOver().As(countColumn))));


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
    }
}