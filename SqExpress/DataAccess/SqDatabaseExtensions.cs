using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SqExpress.DataAccess
{
    public static class SqDatabaseExtensions
    {
        public static Task<List<T>> QueryList<T>(this ISqDatabase database, IExprQuery expr, Func<ISqDataRecordReader, T> factory, Predicate<T>? predicateItem = null)
        {
            return database.Query(expr,
                new List<T>(),
                (acc, record) =>
                {
                    var item = factory(record);
                    if (predicateItem != null)
                    {
                        if (!predicateItem(item))
                        {
                            return acc;
                        }
                    }
                    acc.Add(item);
                    return acc;
                });
        }
    }
}