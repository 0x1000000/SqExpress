using System;
using System.Collections;
using System.Collections.Generic;
using SqExpress.Syntax.Value;
#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace SqExpress
{
#if NET8_0_OR_GREATER
    [CollectionBuilder(typeof(ParamValueBuilder), nameof(ParamValueBuilder.Create))]
#endif
    public readonly struct ParamValue : IReadOnlyList<ExprValue>
    {
        private readonly ExprValue? _singleValue;
        private readonly IReadOnlyList<ExprValue>? _list;

        private ParamValue(ExprValue? singleValue, IReadOnlyList<ExprValue>? list)
        {
            this._singleValue = singleValue;
            this._list = list;
        }

        public bool IsList => this._list != null;

        public IReadOnlyList<ExprValue> AsList => this._list ?? throw new SqExpressException("Not List");

        public bool IsSingle => !ReferenceEquals(this._singleValue, null);

        public ExprValue AsSingle => this._singleValue ?? throw new SqExpressException("Not Single");

        public int Count => this._list?.Count ?? (this.IsSingle ? 1 : 0);

        public ExprValue this[int index]
        {
            get
            {
                if (this._list != null)
                {
                    return this._list[index];
                }

                if (index == 0 && !ReferenceEquals(this._singleValue, null))
                {
                    return this._singleValue!;
                }

                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public static implicit operator ParamValue(ExprValue value) => new ParamValue(value, null);

        public static implicit operator ParamValue(string? value) => new ParamValue(value, null);
        public static implicit operator ParamValue(bool value) => new ParamValue(value, null);
        public static implicit operator ParamValue(bool? value) => new ParamValue(value, null);
        public static implicit operator ParamValue(int value) => new ParamValue(value, null);
        public static implicit operator ParamValue(int? value) => new ParamValue(value, null);
        public static implicit operator ParamValue(byte value) => new ParamValue(value, null);
        public static implicit operator ParamValue(byte? value) => new ParamValue(value, null);
        public static implicit operator ParamValue(short value) => new ParamValue(value, null);
        public static implicit operator ParamValue(short? value) => new ParamValue(value, null);
        public static implicit operator ParamValue(long value) => new ParamValue(value, null);
        public static implicit operator ParamValue(long? value) => new ParamValue(value, null);
        public static implicit operator ParamValue(decimal value) => new ParamValue(value, null);
        public static implicit operator ParamValue(decimal? value) => new ParamValue(value, null);
        public static implicit operator ParamValue(double value) => new ParamValue(value, null);
        public static implicit operator ParamValue(double? value) => new ParamValue(value, null);
        public static implicit operator ParamValue(Guid value) => new ParamValue(value, null);
        public static implicit operator ParamValue(Guid? value) => new ParamValue(value, null);
        public static implicit operator ParamValue(DateTime value) => new ParamValue(value, null);
        public static implicit operator ParamValue(DateTime? value) => new ParamValue(value, null);
        public static implicit operator ParamValue(DateTimeOffset value) => new ParamValue(value, null);
        public static implicit operator ParamValue(DateTimeOffset? value) => new ParamValue(value, null);

        public static implicit operator ParamValue(string?[] values) => FromStringArray(values);
        public static implicit operator ParamValue(List<string?> values) => FromStringList(values);
        public static implicit operator ParamValue(HashSet<string?> values) => FromStringSet(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<string?> values) => FromStringSpan(values);
#endif

        public static implicit operator ParamValue(bool[] values) => FromBooleanArray(values);
        public static implicit operator ParamValue(List<bool> values) => FromBooleanList(values);
        public static implicit operator ParamValue(HashSet<bool> values) => FromBooleanSet(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<bool> values) => FromBooleanSpan(values);
#endif

        public static implicit operator ParamValue(bool?[] values) => FromNullableBooleanArray(values);
        public static implicit operator ParamValue(List<bool?> values) => FromNullableBooleanList(values);
        public static implicit operator ParamValue(HashSet<bool?> values) => FromNullableBooleanSet(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<bool?> values) => FromNullableBooleanSpan(values);
#endif

        public static implicit operator ParamValue(int[] values) => FromInt32Array(values);
        public static implicit operator ParamValue(List<int> values) => FromInt32List(values);
        public static implicit operator ParamValue(HashSet<int> values) => FromInt32Set(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<int> values) => FromInt32Span(values);
#endif

        public static implicit operator ParamValue(int?[] values) => FromNullableInt32Array(values);
        public static implicit operator ParamValue(List<int?> values) => FromNullableInt32List(values);
        public static implicit operator ParamValue(HashSet<int?> values) => FromNullableInt32Set(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<int?> values) => FromNullableInt32Span(values);
#endif

        public static implicit operator ParamValue(byte[] values) => FromByteArray(values);
        public static implicit operator ParamValue(List<byte> values) => FromByteList(values);
        public static implicit operator ParamValue(HashSet<byte> values) => FromByteSet(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<byte> values) => FromByteSpan(values);
#endif

        public static implicit operator ParamValue(byte?[] values) => FromNullableByteArray(values);
        public static implicit operator ParamValue(List<byte?> values) => FromNullableByteList(values);
        public static implicit operator ParamValue(HashSet<byte?> values) => FromNullableByteSet(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<byte?> values) => FromNullableByteSpan(values);
#endif

        public static implicit operator ParamValue(short[] values) => FromInt16Array(values);
        public static implicit operator ParamValue(List<short> values) => FromInt16List(values);
        public static implicit operator ParamValue(HashSet<short> values) => FromInt16Set(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<short> values) => FromInt16Span(values);
#endif

        public static implicit operator ParamValue(short?[] values) => FromNullableInt16Array(values);
        public static implicit operator ParamValue(List<short?> values) => FromNullableInt16List(values);
        public static implicit operator ParamValue(HashSet<short?> values) => FromNullableInt16Set(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<short?> values) => FromNullableInt16Span(values);
#endif

        private static void EnsureNotNull(object? values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }
        }

        private static ParamValue FromExprValueArray(ExprValue[] values)
        {
            if (values.Length < 1)
            {
                throw new SqExpressException("Cannot be empty");
            }

            if (values.Length == 1)
            {
                return new ParamValue(values[0], null);
            }

            return new ParamValue(null, values);
        }

#if NET8_0_OR_GREATER
        internal static ParamValue FromExprValueSpan(ReadOnlySpan<ExprValue> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        public IEnumerator<ExprValue> GetEnumerator()
        {
            if (this._list != null)
            {
                return this._list.GetEnumerator();
            }

            if (!ReferenceEquals(this._singleValue, null))
            {
                return new SingleValueEnumerator(this._singleValue!);
            }

            return ((IEnumerable<ExprValue>)Array.Empty<ExprValue>()).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private static ParamValue FromStringArray(string?[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromStringList(List<string?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromStringSet(HashSet<string?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromStringSpan(ReadOnlySpan<string?> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromBooleanArray(bool[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromBooleanList(List<bool> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromBooleanSet(HashSet<bool> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromBooleanSpan(ReadOnlySpan<bool> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromNullableBooleanArray(bool?[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableBooleanList(List<bool?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableBooleanSet(HashSet<bool?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromNullableBooleanSpan(ReadOnlySpan<bool?> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromInt32Array(int[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromInt32List(List<int> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromInt32Set(HashSet<int> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromInt32Span(ReadOnlySpan<int> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromNullableInt32Array(int?[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableInt32List(List<int?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableInt32Set(HashSet<int?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromNullableInt32Span(ReadOnlySpan<int?> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromByteArray(byte[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromByteList(List<byte> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromByteSet(HashSet<byte> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromByteSpan(ReadOnlySpan<byte> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromNullableByteArray(byte?[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableByteList(List<byte?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableByteSet(HashSet<byte?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromNullableByteSpan(ReadOnlySpan<byte?> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromInt16Array(short[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromInt16List(List<short> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromInt16Set(HashSet<short> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromInt16Span(ReadOnlySpan<short> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromNullableInt16Array(short?[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableInt16List(List<short?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableInt16Set(HashSet<short?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromNullableInt16Span(ReadOnlySpan<short?> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        public static implicit operator ParamValue(long[] values) => FromInt64Array(values);
        public static implicit operator ParamValue(List<long> values) => FromInt64List(values);
        public static implicit operator ParamValue(HashSet<long> values) => FromInt64Set(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<long> values) => FromInt64Span(values);
#endif

        public static implicit operator ParamValue(long?[] values) => FromNullableInt64Array(values);
        public static implicit operator ParamValue(List<long?> values) => FromNullableInt64List(values);
        public static implicit operator ParamValue(HashSet<long?> values) => FromNullableInt64Set(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<long?> values) => FromNullableInt64Span(values);
#endif

        public static implicit operator ParamValue(decimal[] values) => FromDecimalArray(values);
        public static implicit operator ParamValue(List<decimal> values) => FromDecimalList(values);
        public static implicit operator ParamValue(HashSet<decimal> values) => FromDecimalSet(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<decimal> values) => FromDecimalSpan(values);
#endif

        public static implicit operator ParamValue(decimal?[] values) => FromNullableDecimalArray(values);
        public static implicit operator ParamValue(List<decimal?> values) => FromNullableDecimalList(values);
        public static implicit operator ParamValue(HashSet<decimal?> values) => FromNullableDecimalSet(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<decimal?> values) => FromNullableDecimalSpan(values);
#endif

        public static implicit operator ParamValue(double[] values) => FromDoubleArray(values);
        public static implicit operator ParamValue(List<double> values) => FromDoubleList(values);
        public static implicit operator ParamValue(HashSet<double> values) => FromDoubleSet(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<double> values) => FromDoubleSpan(values);
#endif

        public static implicit operator ParamValue(double?[] values) => FromNullableDoubleArray(values);
        public static implicit operator ParamValue(List<double?> values) => FromNullableDoubleList(values);
        public static implicit operator ParamValue(HashSet<double?> values) => FromNullableDoubleSet(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<double?> values) => FromNullableDoubleSpan(values);
#endif

        public static implicit operator ParamValue(Guid[] values) => FromGuidArray(values);
        public static implicit operator ParamValue(List<Guid> values) => FromGuidList(values);
        public static implicit operator ParamValue(HashSet<Guid> values) => FromGuidSet(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<Guid> values) => FromGuidSpan(values);
#endif

        public static implicit operator ParamValue(Guid?[] values) => FromNullableGuidArray(values);
        public static implicit operator ParamValue(List<Guid?> values) => FromNullableGuidList(values);
        public static implicit operator ParamValue(HashSet<Guid?> values) => FromNullableGuidSet(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<Guid?> values) => FromNullableGuidSpan(values);
#endif

        public static implicit operator ParamValue(DateTime[] values) => FromDateTimeArray(values);
        public static implicit operator ParamValue(List<DateTime> values) => FromDateTimeList(values);
        public static implicit operator ParamValue(HashSet<DateTime> values) => FromDateTimeSet(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<DateTime> values) => FromDateTimeSpan(values);
#endif

        public static implicit operator ParamValue(DateTime?[] values) => FromNullableDateTimeArray(values);
        public static implicit operator ParamValue(List<DateTime?> values) => FromNullableDateTimeList(values);
        public static implicit operator ParamValue(HashSet<DateTime?> values) => FromNullableDateTimeSet(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<DateTime?> values) => FromNullableDateTimeSpan(values);
#endif

        public static implicit operator ParamValue(DateTimeOffset[] values) => FromDateTimeOffsetArray(values);
        public static implicit operator ParamValue(List<DateTimeOffset> values) => FromDateTimeOffsetList(values);
        public static implicit operator ParamValue(HashSet<DateTimeOffset> values) => FromDateTimeOffsetSet(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<DateTimeOffset> values) => FromDateTimeOffsetSpan(values);
#endif

        public static implicit operator ParamValue(DateTimeOffset?[] values) => FromNullableDateTimeOffsetArray(values);
        public static implicit operator ParamValue(List<DateTimeOffset?> values) => FromNullableDateTimeOffsetList(values);
        public static implicit operator ParamValue(HashSet<DateTimeOffset?> values) => FromNullableDateTimeOffsetSet(values);
#if NET8_0_OR_GREATER
        public static implicit operator ParamValue(ReadOnlySpan<DateTimeOffset?> values) => FromNullableDateTimeOffsetSpan(values);
#endif

        private static ParamValue FromInt64Array(long[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromInt64List(List<long> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromInt64Set(HashSet<long> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromInt64Span(ReadOnlySpan<long> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromNullableInt64Array(long?[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableInt64List(List<long?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableInt64Set(HashSet<long?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromNullableInt64Span(ReadOnlySpan<long?> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromDecimalArray(decimal[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromDecimalList(List<decimal> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromDecimalSet(HashSet<decimal> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromDecimalSpan(ReadOnlySpan<decimal> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromNullableDecimalArray(decimal?[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableDecimalList(List<decimal?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableDecimalSet(HashSet<decimal?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromNullableDecimalSpan(ReadOnlySpan<decimal?> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromDoubleArray(double[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromDoubleList(List<double> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromDoubleSet(HashSet<double> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromDoubleSpan(ReadOnlySpan<double> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromNullableDoubleArray(double?[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableDoubleList(List<double?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableDoubleSet(HashSet<double?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromNullableDoubleSpan(ReadOnlySpan<double?> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromGuidArray(Guid[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromGuidList(List<Guid> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromGuidSet(HashSet<Guid> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromGuidSpan(ReadOnlySpan<Guid> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromNullableGuidArray(Guid?[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableGuidList(List<Guid?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableGuidSet(HashSet<Guid?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromNullableGuidSpan(ReadOnlySpan<Guid?> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromDateTimeArray(DateTime[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromDateTimeList(List<DateTime> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromDateTimeSet(HashSet<DateTime> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromDateTimeSpan(ReadOnlySpan<DateTime> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromNullableDateTimeArray(DateTime?[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableDateTimeList(List<DateTime?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableDateTimeSet(HashSet<DateTime?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromNullableDateTimeSpan(ReadOnlySpan<DateTime?> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromDateTimeOffsetArray(DateTimeOffset[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromDateTimeOffsetList(List<DateTimeOffset> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromDateTimeOffsetSet(HashSet<DateTimeOffset> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromDateTimeOffsetSpan(ReadOnlySpan<DateTimeOffset> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private static ParamValue FromNullableDateTimeOffsetArray(DateTimeOffset?[] values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableDateTimeOffsetList(List<DateTimeOffset?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }

        private static ParamValue FromNullableDateTimeOffsetSet(HashSet<DateTimeOffset?> values)
        {
            EnsureNotNull(values);
            ExprValue[] result = new ExprValue[values.Count];
            var index = 0;
            foreach (var value in values)
            {
                result[index] = value;
                index++;
            }

            return FromExprValueArray(result);
        }

#if NET8_0_OR_GREATER
        private static ParamValue FromNullableDateTimeOffsetSpan(ReadOnlySpan<DateTimeOffset?> values)
        {
            ExprValue[] result = new ExprValue[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                result[i] = values[i];
            }

            return FromExprValueArray(result);
        }
#endif

        private sealed class SingleValueEnumerator : IEnumerator<ExprValue>
        {
            private readonly ExprValue _value;
            private int _index;

            public SingleValueEnumerator(ExprValue value)
            {
                this._value = value;
                this._index = -1;
            }

            public bool MoveNext()
            {
                if (this._index < 0)
                {
                    this._index = 0;
                    return true;
                }

                this._index = 1;
                return false;
            }

            public void Reset()
            {
                this._index = -1;
            }

            public ExprValue Current
            {
                get
                {
                    if (this._index == 0)
                    {
                        return this._value;
                    }

                    throw new InvalidOperationException();
                }
            }

            object IEnumerator.Current => this.Current;

            public void Dispose()
            {
            }
        }
    }
}
