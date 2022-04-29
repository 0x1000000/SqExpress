using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SqExpress.DataAccess
{
    public static class SqDatabaseExtensions
    {
        public static Task Query(this ISqDatabase database, IExprQuery query, Action<ISqDataRecordReader> handler, CancellationToken cancellationToken = default)
        {
            return database.Query<object?>(query,
                null,
                (acc, r) =>
                {
                    handler(r);
                    return acc;
                },
                cancellationToken);
        }

        public static Task Query(this ISqDatabase database, IExprQuery query, Func<ISqDataRecordReader, Task> handler, CancellationToken cancellationToken = default)
        {
            return database.Query<object?>(query,
                null,
                async (acc, r) =>
                {
                    await handler(r);
                    return acc;
                },
                cancellationToken);
        }
        public static Task<List<T>> QueryList<T>(this ISqDatabase database, IExprQuery expr, Func<ISqDataRecordReader, T> factory, Predicate<T>? predicateItem = null, CancellationToken cancellationToken = default)
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
                },
                cancellationToken);
        }


        public delegate void KeyDuplicationHandler<TKey, TValue>(TKey key, TValue oldValue, TValue newValue, Dictionary<TKey, TValue> dictionary)
            where TKey: notnull;

        public static Task<Dictionary<TKey, TValue>> QueryDictionary<TKey, TValue>(
            this ISqDatabase database,
            IExprQuery expr, 
            Func<ISqDataRecordReader, TKey> keyFactory, 
            Func<ISqDataRecordReader, TValue> valueFactory,
            KeyDuplicationHandler<TKey, TValue>? keyDuplicationHandler = null,
            Func<TKey, TValue, bool>? predicate = null,
            CancellationToken cancellationToken = default)
            where TKey : notnull
        {
            return database.Query(expr,
                new Dictionary<TKey, TValue>(),
                (acc, record) =>
                {
                    var key = keyFactory(record);
                    var value = valueFactory(record);

                    if (predicate != null && !predicate(key, value))
                    {
                        return acc;
                    }

                    if (keyDuplicationHandler == null)
                    {
                        acc.Add(key, value);
                    }
                    else
                    {
                        if (acc.TryGetValue(key, out var oldValue))
                        {
                            keyDuplicationHandler(key, oldValue, value, acc);
                        }
                        else
                        {
                            acc.Add(key, value);
                        }
                    }
                    return acc;
                },
                cancellationToken);
        }

    }
}