using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.ModelSelect;

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
    }
}