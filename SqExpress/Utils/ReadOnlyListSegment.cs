using System;
using System.Collections.Generic;

namespace SqExpress.Utils
{
    internal readonly struct ReadOnlyListSegment<T>
    {
        private readonly IReadOnlyList<T> _list;
        private readonly int _offset;

        public int Count { get; }

        public ReadOnlyListSegment(IReadOnlyList<T> list, int offset, int count)
        {
            if (offset< 0 || count < 0 || offset + count > list.Count)
            {
                throw new IndexOutOfRangeException("Incorrect offset and count");
            }

            this._list = list;
            this._offset = offset;
            this.Count = count;
        }

        public ReadOnlyListSegment(IReadOnlyList<T> list, int offset) : this(list, offset, list.Count - offset)
        {
        }

        public ReadOnlyListSegment(IReadOnlyList<T> list) : this(list, 0, list.Count)
        {
        }

        public T this[int index] => this._list[this._offset + index];
    }
}