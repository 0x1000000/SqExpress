using System;
using System.Collections.Generic;
using SqExpress.Syntax.Select;
using SqExpress.ModelSelect;
using SqExpress.Syntax.Names;

namespace SqExpress
{
    public static class SqModelSelectBuilder
    {
        public static ModelSelect<T, TTable> Select<T, TTable>(ISqModelReader<T, TTable> reader)
            where TTable : IExprTableSource, new()
        {
            return new ModelSelect<T, TTable>(reader);
        }
    }

    public static class ModelEmptyReader
    {
        public static ISqModelReader<object?, TTable> Get<TTable>() => EmptyModelReaderStorage<TTable>.Instance;

        public class EmptyModelReaderStorage<TTable> : ISqModelReader<object?, TTable>
        {
            public static readonly EmptyModelReaderStorage<TTable> Instance = new EmptyModelReaderStorage<TTable>();

            private EmptyModelReaderStorage() { }
#if !NETFRAMEWORK
            public IReadOnlyList<ExprColumn> GetColumns(TTable table) => Array.Empty<TableColumn>();
#else
            public IReadOnlyList<ExprColumn> GetColumns(TTable table) => new ExprColumn[0];
#endif

            public int GetColumnCount() => 0;

            public object? Read(ISqDataRecordReader record, TTable table) => null;

            public object? ReadOrdinal(ISqDataRecordReader record, TTable table, int offset) => null;
        }
    }
}