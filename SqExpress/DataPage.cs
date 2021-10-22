using System;
using System.Collections.Generic;
using System.Linq;

namespace SqExpress
{
    public class DataPage<T>
    {
        internal DataPage(IReadOnlyList<T> items, int offset, int total)
        {
            this.Items = items;
            this.Offset = offset;
            this.Total = total;
        }

        public IReadOnlyList<T> Items { get; }

        public int Offset { get; }

        public int Total { get; }

        public DataPage<TNext> Select<TNext>(Func<T, TNext> selector)
        {
            return new DataPage<TNext>(this.Items.Select(selector).ToList(), this.Offset, this.Total);
        }
    }
}