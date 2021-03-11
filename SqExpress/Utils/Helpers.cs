using System;
using System.Collections.Generic;
using System.Linq;

namespace SqExpress.Utils
{
    internal static class Helpers
    {
        public static T AssertNotNull<T>(this T? source, string message) where T: class
        {
            if (source == null)
            {
                throw new SqExpressException(message);
            }

            return source;
        }

        public static T AssertNotNull<T>(this T? source, string message) where T: struct
        {
            if (source == null)
            {
                throw new SqExpressException(message);
            }

            return source.Value;
        }

        public static T AssertArgumentNotNull<T>(this T? source, string paramName, string? message = null) where T: class
        {
            if (source == null)
            {
                throw new ArgumentNullException(paramName, message);
            }

            return source;
        }

        public static T AssertFatalNotNull<T>(this T? source, string name) where T: class
        {
            if (source == null)
            {
                throw new SqExpressException($"\"{name}\" cannot be null");
            }

            return source;
        }

        public static T? AssertFatalNull<T>(this T? source, string name) where T: class
        {
            if (source != null)
            {
                throw new SqExpressException($"\"{name}\" has to be null");
            }

            return source;
        }

        public static IReadOnlyList<T> AssertNotEmpty<T>(this IReadOnlyList<T>? source, string message)
        {
            if (source == null || source.AssertNotNull(message).Count <= 0)
            {
                throw new SqExpressException(message);
            }

            return source;
        }

        public static IReadOnlyList<T> Combine<T>(T arg, T[] rest)
        {
            if (rest.Length < 1)
            {
                return new[] {arg};
            }
            var result = new T[rest.Length + 1];
            result[0] = arg;
            Array.Copy(rest, 0, result, 1, rest.Length);
            return result;
        }

        public static T[] Combine<T>(T[] source, T newItem)
        {
            if (source.Length < 1)
            {
                return new[] {newItem};
            }
            var result = new T[source.Length + 1];
            result[result.Length - 1] = newItem;
            Array.Copy(source, 0, result, 0, source.Length);
            return result;
        }

        public static IReadOnlyList<TRes> Combine<T,TRes>(T arg, IReadOnlyList<T> rest, Func<T, TRes> mapper)
        {
            if (rest.Count < 1)
            {
                return new[] { mapper(arg) };
            }
            var result = new TRes[rest.Count + 1];
            result[0] = mapper(arg);

            for (int i = 0; i < rest.Count; i++)
            {
                result[i + 1] = mapper(rest[i]);
            }

            return result;
        }

        public static IReadOnlyList<T> Combine<T>(T arg1, T arg2, T[] rest)
        {
            if (rest.Length < 1)
            {
                return new[] {arg1, arg2};
            }
            var result = new T[rest.Length + 2];
            result[0] = arg1;
            result[1] = arg2;
            Array.Copy(rest, 0, result, 2, rest.Length);
            return result;
        }

        public static IReadOnlyList<T> Combine<T>(IReadOnlyList<T> arg1, IReadOnlyList<T> arg2)
        {
            if (arg1.Count < 1)
            {
                return arg2;
            }
            if (arg2.Count < 1)
            {
                return arg1;
            }


            var result = new T[arg1.Count + arg2.Count];
            for (int i = 0; i < arg1.Count; i++)
            {
                result[i] = arg1[i];
            }
            for (int i = arg1.Count; i < result.Length; i++)
            {
                result[i] = arg2[i-arg1.Count];
            }
            return result;
        }

        public static IReadOnlyList<T> Combine<T>(IReadOnlyList<T> arg1, T arg2, T[] rest)
        {
            if (arg1.Count < 1)
            {
                return Combine(arg2, rest);
            }

            var result = new T[arg1.Count +1 +rest.Length];
            for (int i = 0; i < arg1.Count; i++)
            {
                result[i] = arg1[i];
            }

            result[arg1.Count] = arg2;

            for (int i = 0; i < rest.Length; i++)
            {
                result[arg1.Count + 1 + i] = rest[i];
            }
            return result;
        }        
        
        public static IReadOnlyList<TRes> Combine<TRes,T>(IReadOnlyList<TRes> arg1, T arg2, T[] rest, Func<T, TRes> mapper)
        {
            if (arg1.Count < 1)
            {
                return Combine(arg2, rest, mapper);
            }

            var result = new TRes[arg1.Count +1 +rest.Length];
            for (int i = 0; i < arg1.Count; i++)
            {
                result[i] = arg1[i];
            }

            result[arg1.Count] = mapper(arg2);

            for (int i = 0; i < rest.Length; i++)
            {
                result[arg1.Count + 1+ i] = mapper(rest[i]);
            }
            return result;
        }

        public static T? CombineNotNull<T>(T? a, T? b, Func<T,T,T> combiner) where T : class
        {
            if (a != null && b != null)
            {
                return combiner(a, b);
            }
            if (a != null)
            {
                return a;
            }
            return b;
        }

        public static IReadOnlyList<TRes> SelectToReadOnlyList<T, TRes>(this IEnumerable<T> source, Func<T, TRes> mapper)
        {
            if (source is IReadOnlyList<T> list)
            {
                return SelectToReadOnlyList(list, mapper);
            }
            return source.AssertNotNull($"\"{nameof(source)}\" cannot be null").Select(mapper).ToList();
        }

        public static IReadOnlyList<TRes> SelectToReadOnlyList<T, TRes>(this IReadOnlyList<T> source, Func<T, TRes> mapper)
        {
            source.AssertNotNull($"\"{nameof(source)}\" cannot be null");
            mapper.AssertNotNull($"\"{nameof(mapper)}\" cannot be null");

            TRes[] result = new TRes[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                result[i] = mapper(source[i]);
            }
            return result;
        }

        public static IReadOnlyList<TRes> SelectToReadOnlyList<T, TRes>(this IReadOnlyList<T> source, Predicate<T> predicate, Func<T, TRes> mapper)
        {
            source.AssertNotNull($"\"{nameof(source)}\" cannot be null");
            mapper.AssertNotNull($"\"{nameof(mapper)}\" cannot be null");
            predicate.AssertNotNull($"\"{nameof(predicate)}\" cannot be null");

            int count = 0;

            for (int i = 0; i < source.Count; i++)
            {
                if (predicate(source[i]))
                {
                    count++;
                }
            }

            TRes[] result = new TRes[count];
            if (count > 0)
            {
                int resultIndex = 0;
                for (int i = 0; i < source.Count; i++)
                {
                    if (predicate(source[i]))
                    {
                        if (resultIndex >= result.Length)
                        {
                            throw new SqExpressException("The predicate function should be idempotent",
                                new IndexOutOfRangeException());
                        }

                        result[resultIndex] = mapper(source[i]);
                        resultIndex++;
                    }
                }

                if (resultIndex != result.Length)
                {
                    throw new SqExpressException("The predicate function should be idempotent");
                }
            }
            return result;
        }

        public static bool TryToCheckLength<T>(this IEnumerable<T> source, out int length)
        {
            if (source is IReadOnlyCollection<T> collection)
            {
                length = collection.Count;
                return true;
            }

            length = 0;
            return false;
        }
    }
}