using System;
using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;

namespace SqExpress.ModelSelect
{
    internal static class ModelQueryBuilderHelper
    {
        public static IReadOnlyList<IExprSelecting> BuildColumns(out int[] offsets, params IReadOnlyList<ExprColumn>[] columns)
        {
            offsets = new int[columns.Length + 1];
            int total = 0;
            for (var index = 0; index < columns.Length; index++)
            {
                var list = columns[index];
                offsets[index] = total;
                total += list.Count;
            }

            offsets[offsets.Length-1] = total;

            var result = new List<IExprSelecting>(total);

            foreach (var list in columns)
            {
                result.AddRange(list);
            }

            return result;
        }

        public static bool IsAllNull(ISqDataRecordReader reader, int[] offset, int index)
        {
            int start = offset[index];
            int end = index < offset.Length ? offset[index + 1] : offset.Length;
            if (end - start <= 0)
            {
                return true;
            }

            for (int i = start; i < end; i++)
            {
                if (!reader.IsDBNull(i))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class ModelSelect<T, TTable> where TTable : IExprTableSource, new()
    {
        public ModelSelect(ISqModelReader<T, TTable> reader)
        {
            this.Reader = reader;
        }

        internal ISqModelReader<T, TTable> Reader { get; }

        public ModelSelect10<T, TTable, TJ, TJTable> InnerJoin<TJ, TJTable>(ISqModelReader<TJ, TJTable> reader,
            Func<IModelSelectTablesContext<TTable, TJTable>, ExprBoolean> on) where TJTable : IExprTableSource, new()
            => new ModelSelect10<T, TTable, TJ, TJTable>(this, reader, on);

        public ModelSelect01Left<T, TTable, TL, TLTable> LeftJoin<TL, TLTable>(ISqModelReader<TL, TLTable> reader,
            Func<IModelSelectTablesContext<TTable, TLTable>, ExprBoolean> on) where TLTable : IExprTableSource, new()
            => new ModelSelect01Left<T, TTable, TL, TLTable>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(Func<TTable, ExprBoolean>? filter, Func<TTable, ExprOrderBy>? order,
            Func<T, TRes> mapper)
        {
            IExprQuery query;

            var table = new TTable();

            var filtered = SqQueryBuilder
                .Select(this.Reader.GetColumns(table))
                .From(table)
                .Where(filter?.Invoke(table));

            if (order != null)
            {
                query = filtered.OrderBy(order(table)).Done();
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r) => mapper(this.Reader.ReadOrdinal(r, table, 0));

            return new ModelRequestData<TRes>(query, ResultMapper);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize, Func<TTable, ExprBoolean>? filter,
            Func<TTable, ExprOrderBy> order, Func<T, TRes> mapper)
        {
            var table = new TTable();

            IExprQuery query = SqQueryBuilder
                .Select(this.Reader.GetColumns(table))
                .From(table)
                .Where(filter?.Invoke(table))
                .OrderBy(order(table))
                .OffsetFetch(offset, pageSize)
                .Done();

            TRes ResultMapper(ISqDataRecordReader r) => mapper(this.Reader.ReadOrdinal(r, table, 0));

            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)query, ResultMapper);
        }
    }

    public readonly struct ModelRowData<T, TJ1>
    {
        public ModelRowData(T model, TJ1 joinedModel1)
        {
            this.Model = model;
            this.JoinedModel1 = joinedModel1;
        }

        public T Model { get; }

        public TJ1 JoinedModel1 { get; }

        public void Deconstruct(out T model, out TJ1 joinedModel1)
        {
            model = this.Model;
            joinedModel1 = this.JoinedModel1;
        }
    }

    public readonly struct ModelRowData<T, TJ1, TJ2>
    {
        public ModelRowData(T model, TJ1 joinedModel1, TJ2 joinedModel2)
        {
            this.Model = model;
            this.JoinedModel1 = joinedModel1;
            this.JoinedModel2 = joinedModel2;
        }

        public T Model { get; }

        public TJ1 JoinedModel1 { get; }

        public TJ2 JoinedModel2 { get; }

        public void Deconstruct(out T model, out TJ1 joinedModel1, out TJ2 joinedModel2)
        {
            model = this.Model;
            joinedModel1 = this.JoinedModel1;
            joinedModel2 = this.JoinedModel2;
        }
    }

    public readonly struct ModelRowData<T, TJ1, TJ2, TJ3>
    {
        public ModelRowData(T model, TJ1 joinedModel1, TJ2 joinedModel2, TJ3 joinedModel3)
        {
            this.Model = model;
            this.JoinedModel1 = joinedModel1;
            this.JoinedModel2 = joinedModel2;
            this.JoinedModel3 = joinedModel3;
        }

        public T Model { get; }

        public TJ1 JoinedModel1 { get; }

        public TJ2 JoinedModel2 { get; }

        public TJ3 JoinedModel3 { get; }

        public void Deconstruct(out T model, out TJ1 joinedModel1, out TJ2 joinedModel2, out TJ3 joinedModel3)
        {
            model = this.Model;
            joinedModel1 = this.JoinedModel1;
            joinedModel2 = this.JoinedModel2;
            joinedModel3 = this.JoinedModel3;
        }
    }

    public class ModelRowData<T, TJ1, TJ2, TJ3, TJ4>
    {
        public ModelRowData(T model, TJ1 joinedModel1, TJ2 joinedModel2, TJ3 joinedModel3, TJ4 joinedModel4)
        {
            this.Model = model;
            this.JoinedModel1 = joinedModel1;
            this.JoinedModel2 = joinedModel2;
            this.JoinedModel3 = joinedModel3;
            this.JoinedModel4 = joinedModel4;
        }

        public T Model { get; }

        public TJ1 JoinedModel1 { get; }

        public TJ2 JoinedModel2 { get; }

        public TJ3 JoinedModel3 { get; }

        public TJ4 JoinedModel4 { get; }

        public void Deconstruct(out T model, out TJ1 joinedModel1, out TJ2 joinedModel2, out TJ3 joinedModel3,
            out TJ4 joinedModel4)
        {
            model = this.Model;
            joinedModel1 = this.JoinedModel1;
            joinedModel2 = this.JoinedModel2;
            joinedModel3 = this.JoinedModel3;
            joinedModel4 = this.JoinedModel4;
        }
    }

    public class ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5>
    {
        public ModelRowData(T model, TJ1 joinedModel1, TJ2 joinedModel2, TJ3 joinedModel3, TJ4 joinedModel4,
            TJ5 joinedModel5)
        {
            this.Model = model;
            this.JoinedModel1 = joinedModel1;
            this.JoinedModel2 = joinedModel2;
            this.JoinedModel3 = joinedModel3;
            this.JoinedModel4 = joinedModel4;
            this.JoinedModel5 = joinedModel5;
        }

        public T Model { get; }

        public TJ1 JoinedModel1 { get; }

        public TJ2 JoinedModel2 { get; }

        public TJ3 JoinedModel3 { get; }

        public TJ4 JoinedModel4 { get; }

        public TJ5 JoinedModel5 { get; }

        public void Deconstruct(out T model, out TJ1 joinedModel1, out TJ2 joinedModel2, out TJ3 joinedModel3,
            out TJ4 joinedModel4, out TJ5 joinedModel5)
        {
            model = this.Model;
            joinedModel1 = this.JoinedModel1;
            joinedModel2 = this.JoinedModel2;
            joinedModel3 = this.JoinedModel3;
            joinedModel4 = this.JoinedModel4;
            joinedModel5 = this.JoinedModel5;
        }
    }

    public class ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TJ6>
    {
        public ModelRowData(T model, TJ1 joinedModel1, TJ2 joinedModel2, TJ3 joinedModel3, TJ4 joinedModel4,
            TJ5 joinedModel5, TJ6 joinedModel6)
        {
            this.Model = model;
            this.JoinedModel1 = joinedModel1;
            this.JoinedModel2 = joinedModel2;
            this.JoinedModel3 = joinedModel3;
            this.JoinedModel4 = joinedModel4;
            this.JoinedModel5 = joinedModel5;
            this.JoinedModel6 = joinedModel6;
        }

        public T Model { get; }

        public TJ1 JoinedModel1 { get; }

        public TJ2 JoinedModel2 { get; }

        public TJ3 JoinedModel3 { get; }

        public TJ4 JoinedModel4 { get; }

        public TJ5 JoinedModel5 { get; }

        public TJ6 JoinedModel6 { get; }

        public void Deconstruct(out T model, out TJ1 joinedModel1, out TJ2 joinedModel2, out TJ3 joinedModel3,
            out TJ4 joinedModel4, out TJ5 joinedModel5, out TJ6 joinedModel6)
        {
            model = this.Model;
            joinedModel1 = this.JoinedModel1;
            joinedModel2 = this.JoinedModel2;
            joinedModel3 = this.JoinedModel3;
            joinedModel4 = this.JoinedModel4;
            joinedModel5 = this.JoinedModel5;
            joinedModel6 = this.JoinedModel6;
        }
    }

    public class ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TJ6, TJ7>
    {
        public ModelRowData(T model, TJ1 joinedModel1, TJ2 joinedModel2, TJ3 joinedModel3, TJ4 joinedModel4,
            TJ5 joinedModel5, TJ6 joinedModel6, TJ7 joinedModel7)
        {
            this.Model = model;
            this.JoinedModel1 = joinedModel1;
            this.JoinedModel2 = joinedModel2;
            this.JoinedModel3 = joinedModel3;
            this.JoinedModel4 = joinedModel4;
            this.JoinedModel5 = joinedModel5;
            this.JoinedModel6 = joinedModel6;
            this.JoinedModel7 = joinedModel7;
        }

        public T Model { get; }

        public TJ1 JoinedModel1 { get; }

        public TJ2 JoinedModel2 { get; }

        public TJ3 JoinedModel3 { get; }

        public TJ4 JoinedModel4 { get; }

        public TJ5 JoinedModel5 { get; }

        public TJ6 JoinedModel6 { get; }

        public TJ7 JoinedModel7 { get; }

        public void Deconstruct(out T model, out TJ1 joinedModel1, out TJ2 joinedModel2, out TJ3 joinedModel3,
            out TJ4 joinedModel4, out TJ5 joinedModel5, out TJ6 joinedModel6, out TJ7 joinedModel7)
        {
            model = this.Model;
            joinedModel1 = this.JoinedModel1;
            joinedModel2 = this.JoinedModel2;
            joinedModel3 = this.JoinedModel3;
            joinedModel4 = this.JoinedModel4;
            joinedModel5 = this.JoinedModel5;
            joinedModel6 = this.JoinedModel6;
            joinedModel7 = this.JoinedModel7;
        }
    }

    public interface IModelSelectTablesContext<TTable, TJoinedTable1>
    {
        TTable Table { get; }
        TJoinedTable1 JoinedTable1 { get; }
    }

    internal class ModelSelectTablesContext<TTable, TJoinedTable1> : IModelSelectTablesContext<TTable, TJoinedTable1>
    {
        public ModelSelectTablesContext(TTable table, TJoinedTable1 joinedTable1)
        {
            this.Table = table;
            this.JoinedTable1 = joinedTable1;
        }

        public TTable Table { get; }

        public TJoinedTable1 JoinedTable1 { get; }

        public void Deconstruct(out TTable table, out TJoinedTable1 joinedTable1)
        {
            table = this.Table;
            joinedTable1 = this.JoinedTable1;
        }
    }

    public interface
        IModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2> : IModelSelectTablesContext<TTable,
            TJoinedTable1>
    {
        TJoinedTable2 JoinedTable2 { get; }
    }

    internal class
        ModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2> : IModelSelectTablesContext<TTable, TJoinedTable1
            , TJoinedTable2>
    {
        public ModelSelectTablesContext(TTable table, TJoinedTable1 joinedTable1, TJoinedTable2 joinedTable2)
        {
            this.Table = table;
            this.JoinedTable1 = joinedTable1;
            this.JoinedTable2 = joinedTable2;
        }

        public TTable Table { get; }
        public TJoinedTable1 JoinedTable1 { get; }
        public TJoinedTable2 JoinedTable2 { get; }

        public void Deconstruct(out TTable table, out TJoinedTable1 joinedTable1, out TJoinedTable2 joinedTable2)
        {
            table = this.Table;
            joinedTable1 = this.JoinedTable1;
            joinedTable2 = this.JoinedTable2;
        }
    }

    public interface
        IModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2, TJoinedTable3> : IModelSelectTablesContext<
            TTable, TJoinedTable1, TJoinedTable2>
    {
        TJoinedTable3 JoinedTable3 { get; }
    }

    internal class
        ModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2, TJoinedTable3> : IModelSelectTablesContext<TTable
            , TJoinedTable1, TJoinedTable2, TJoinedTable3>
    {
        public ModelSelectTablesContext(TTable table, TJoinedTable1 joinedTable1, TJoinedTable2 joinedTable2,
            TJoinedTable3 joinedTable3)
        {
            this.Table = table;
            this.JoinedTable1 = joinedTable1;
            this.JoinedTable2 = joinedTable2;
            this.JoinedTable3 = joinedTable3;
        }

        public TTable Table { get; }
        public TJoinedTable1 JoinedTable1 { get; }
        public TJoinedTable2 JoinedTable2 { get; }
        public TJoinedTable3 JoinedTable3 { get; }

        public void Deconstruct(out TTable table, out TJoinedTable1 joinedTable1, out TJoinedTable2 joinedTable2,
            out TJoinedTable3 joinedTable3)
        {
            table = this.Table;
            joinedTable1 = this.JoinedTable1;
            joinedTable2 = this.JoinedTable2;
            joinedTable3 = this.JoinedTable3;
        }
    }

    public interface
        IModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2, TJoinedTable3, TJoinedTable4> :
            IModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2, TJoinedTable3>
    {
        TJoinedTable4 JoinedTable4 { get; }
    }

    internal class ModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2, TJoinedTable3, TJoinedTable4> :
        IModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2, TJoinedTable3, TJoinedTable4>
    {
        public ModelSelectTablesContext(TTable table, TJoinedTable1 joinedTable1, TJoinedTable2 joinedTable2,
            TJoinedTable3 joinedTable3, TJoinedTable4 joinedTable4)
        {
            this.Table = table;
            this.JoinedTable1 = joinedTable1;
            this.JoinedTable2 = joinedTable2;
            this.JoinedTable3 = joinedTable3;
            this.JoinedTable4 = joinedTable4;
        }

        public TTable Table { get; }
        public TJoinedTable1 JoinedTable1 { get; }
        public TJoinedTable2 JoinedTable2 { get; }
        public TJoinedTable3 JoinedTable3 { get; }
        public TJoinedTable4 JoinedTable4 { get; }

        public void Deconstruct(out TTable table, out TJoinedTable1 joinedTable1, out TJoinedTable2 joinedTable2,
            out TJoinedTable3 joinedTable3, out TJoinedTable4 joinedTable4)
        {
            table = this.Table;
            joinedTable1 = this.JoinedTable1;
            joinedTable2 = this.JoinedTable2;
            joinedTable3 = this.JoinedTable3;
            joinedTable4 = this.JoinedTable4;
        }
    }

    public interface IModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2, TJoinedTable3, TJoinedTable4,
        TJoinedTable5> : IModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2, TJoinedTable3, TJoinedTable4>
    {
        TJoinedTable5 JoinedTable5 { get; }
    }

    internal class ModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2, TJoinedTable3, TJoinedTable4,
        TJoinedTable5> : IModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2, TJoinedTable3, TJoinedTable4,
        TJoinedTable5>
    {
        public ModelSelectTablesContext(TTable table, TJoinedTable1 joinedTable1, TJoinedTable2 joinedTable2,
            TJoinedTable3 joinedTable3, TJoinedTable4 joinedTable4, TJoinedTable5 joinedTable5)
        {
            this.Table = table;
            this.JoinedTable1 = joinedTable1;
            this.JoinedTable2 = joinedTable2;
            this.JoinedTable3 = joinedTable3;
            this.JoinedTable4 = joinedTable4;
            this.JoinedTable5 = joinedTable5;
        }

        public TTable Table { get; }
        public TJoinedTable1 JoinedTable1 { get; }
        public TJoinedTable2 JoinedTable2 { get; }
        public TJoinedTable3 JoinedTable3 { get; }
        public TJoinedTable4 JoinedTable4 { get; }
        public TJoinedTable5 JoinedTable5 { get; }

        public void Deconstruct(out TTable table, out TJoinedTable1 joinedTable1, out TJoinedTable2 joinedTable2,
            out TJoinedTable3 joinedTable3, out TJoinedTable4 joinedTable4, out TJoinedTable5 joinedTable5)
        {
            table = this.Table;
            joinedTable1 = this.JoinedTable1;
            joinedTable2 = this.JoinedTable2;
            joinedTable3 = this.JoinedTable3;
            joinedTable4 = this.JoinedTable4;
            joinedTable5 = this.JoinedTable5;
        }
    }

    public interface IModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2, TJoinedTable3, TJoinedTable4,
        TJoinedTable5, TJoinedTable6> : IModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2, TJoinedTable3,
        TJoinedTable4, TJoinedTable5>
    {
        TJoinedTable6 JoinedTable6 { get; }
    }

    internal class ModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2, TJoinedTable3, TJoinedTable4,
        TJoinedTable5, TJoinedTable6> : IModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2, TJoinedTable3,
        TJoinedTable4, TJoinedTable5, TJoinedTable6>
    {
        public ModelSelectTablesContext(TTable table, TJoinedTable1 joinedTable1, TJoinedTable2 joinedTable2,
            TJoinedTable3 joinedTable3, TJoinedTable4 joinedTable4, TJoinedTable5 joinedTable5,
            TJoinedTable6 joinedTable6)
        {
            this.Table = table;
            this.JoinedTable1 = joinedTable1;
            this.JoinedTable2 = joinedTable2;
            this.JoinedTable3 = joinedTable3;
            this.JoinedTable4 = joinedTable4;
            this.JoinedTable5 = joinedTable5;
            this.JoinedTable6 = joinedTable6;
        }

        public TTable Table { get; }
        public TJoinedTable1 JoinedTable1 { get; }
        public TJoinedTable2 JoinedTable2 { get; }
        public TJoinedTable3 JoinedTable3 { get; }
        public TJoinedTable4 JoinedTable4 { get; }
        public TJoinedTable5 JoinedTable5 { get; }
        public TJoinedTable6 JoinedTable6 { get; }

        public void Deconstruct(out TTable table, out TJoinedTable1 joinedTable1, out TJoinedTable2 joinedTable2,
            out TJoinedTable3 joinedTable3, out TJoinedTable4 joinedTable4, out TJoinedTable5 joinedTable5,
            out TJoinedTable6 joinedTable6)
        {
            table = this.Table;
            joinedTable1 = this.JoinedTable1;
            joinedTable2 = this.JoinedTable2;
            joinedTable3 = this.JoinedTable3;
            joinedTable4 = this.JoinedTable4;
            joinedTable5 = this.JoinedTable5;
            joinedTable6 = this.JoinedTable6;
        }
    }

    public interface IModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2, TJoinedTable3, TJoinedTable4,
        TJoinedTable5, TJoinedTable6, TJoinedTable7> : IModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2,
        TJoinedTable3, TJoinedTable4, TJoinedTable5, TJoinedTable6>
    {
        TJoinedTable7 JoinedTable7 { get; }
    }

    internal class ModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2, TJoinedTable3, TJoinedTable4,
        TJoinedTable5, TJoinedTable6, TJoinedTable7> : IModelSelectTablesContext<TTable, TJoinedTable1, TJoinedTable2,
        TJoinedTable3, TJoinedTable4, TJoinedTable5, TJoinedTable6, TJoinedTable7>
    {
        public ModelSelectTablesContext(TTable table, TJoinedTable1 joinedTable1, TJoinedTable2 joinedTable2,
            TJoinedTable3 joinedTable3, TJoinedTable4 joinedTable4, TJoinedTable5 joinedTable5,
            TJoinedTable6 joinedTable6, TJoinedTable7 joinedTable7)
        {
            this.Table = table;
            this.JoinedTable1 = joinedTable1;
            this.JoinedTable2 = joinedTable2;
            this.JoinedTable3 = joinedTable3;
            this.JoinedTable4 = joinedTable4;
            this.JoinedTable5 = joinedTable5;
            this.JoinedTable6 = joinedTable6;
            this.JoinedTable7 = joinedTable7;
        }

        public TTable Table { get; }
        public TJoinedTable1 JoinedTable1 { get; }
        public TJoinedTable2 JoinedTable2 { get; }
        public TJoinedTable3 JoinedTable3 { get; }
        public TJoinedTable4 JoinedTable4 { get; }
        public TJoinedTable5 JoinedTable5 { get; }
        public TJoinedTable6 JoinedTable6 { get; }
        public TJoinedTable7 JoinedTable7 { get; }

        public void Deconstruct(out TTable table, out TJoinedTable1 joinedTable1, out TJoinedTable2 joinedTable2,
            out TJoinedTable3 joinedTable3, out TJoinedTable4 joinedTable4, out TJoinedTable5 joinedTable5,
            out TJoinedTable6 joinedTable6, out TJoinedTable7 joinedTable7)
        {
            table = this.Table;
            joinedTable1 = this.JoinedTable1;
            joinedTable2 = this.JoinedTable2;
            joinedTable3 = this.JoinedTable3;
            joinedTable4 = this.JoinedTable4;
            joinedTable5 = this.JoinedTable5;
            joinedTable6 = this.JoinedTable6;
            joinedTable7 = this.JoinedTable7;
        }
    }

//CodeGenStart
    public class ModelSelect01Left<T, TTable, TL1, TL1Table> where TTable : IExprTableSource, new()
        where TL1Table : IExprTableSource, new()
    {
        internal readonly ModelSelect<T, TTable> Source;
        internal readonly ISqModelReader<TL1, TL1Table> Reader1;
        internal readonly Func<IModelSelectTablesContext<TTable, TL1Table>, ExprBoolean> On1;

        internal ModelSelect01Left(ModelSelect<T, TTable> source, ISqModelReader<TL1, TL1Table> reader,
            Func<IModelSelectTablesContext<TTable, TL1Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader1 = reader;
            this.On1 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;

        public ModelSelect02Left<T, TTable, TL1, TL1Table, TL2, TL2Table> LeftJoin<TL2, TL2Table>(
            ISqModelReader<TL2, TL2Table> reader,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table>, ExprBoolean> on)
            where TL2Table : IExprTableSource, new()
            => new ModelSelect02Left<T, TTable, TL1, TL1Table, TL2, TL2Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(Func<IModelSelectTablesContext<TTable, TL1Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table>, ExprOrderBy>? order,
            Func<ModelRowData<T, TL1?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TL1Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table>, ExprOrderBy> order,
            Func<ModelRowData<T, TL1?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TL1?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TL1Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table>, ExprOrderBy>? order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext = new ModelSelectTablesContext<TTable, TL1Table>(new TTable(), new TL1Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1)))
                .From(tablesContext.Table)
                .LeftJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TL1?>(this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 1)
                        ? default
                        : this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect10<T, TTable, TJ1, TJ1Table> where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
    {
        internal readonly ModelSelect<T, TTable> Source;
        internal readonly ISqModelReader<TJ1, TJ1Table> Reader1;
        internal readonly Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1;

        internal ModelSelect10(ModelSelect<T, TTable> source, ISqModelReader<TJ1, TJ1Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader1 = reader;
            this.On1 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;

        public ModelSelect20<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table> InnerJoin<TJ2, TJ2Table>(
            ISqModelReader<TJ2, TJ2Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> on)
            where TJ2Table : IExprTableSource, new()
            => new ModelSelect20<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table>(this, reader, on);

        public ModelSelect11Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table> LeftJoin<TL2, TL2Table>(
            ISqModelReader<TL2, TL2Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table>, ExprBoolean> on)
            where TL2Table : IExprTableSource, new()
            => new ModelSelect11Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprOrderBy>? order,
            Func<ModelRowData<T, TJ1>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprOrderBy> order,
            Func<ModelRowData<T, TJ1>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TJ1>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprOrderBy>? order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext = new ModelSelectTablesContext<TTable, TJ1Table>(new TTable(), new TJ1Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1>(this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect02Left<T, TTable, TL1, TL1Table, TL2, TL2Table> where TTable : IExprTableSource, new()
        where TL1Table : IExprTableSource, new()
        where TL2Table : IExprTableSource, new()
    {
        internal readonly ModelSelect01Left<T, TTable, TL1, TL1Table> Source;
        internal readonly ISqModelReader<TL2, TL2Table> Reader2;
        internal readonly Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table>, ExprBoolean> On2;

        internal ModelSelect02Left(ModelSelect01Left<T, TTable, TL1, TL1Table> source,
            ISqModelReader<TL2, TL2Table> reader,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader2 = reader;
            this.On2 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TL1, TL1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TL1Table>, ExprBoolean> On1 => this.Source.On1;

        public ModelSelect03Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table> LeftJoin<TL3, TL3Table>(
            ISqModelReader<TL3, TL3Table> reader,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table>, ExprBoolean> on)
            where TL3Table : IExprTableSource, new()
            => new ModelSelect03Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table>, ExprOrderBy>? order,
            Func<ModelRowData<T, TL1?, TL2?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table>, ExprOrderBy> order,
            Func<ModelRowData<T, TL1?, TL2?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TL1?, TL2?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table>, ExprOrderBy>? order,
            KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TL1Table, TL2Table>(new TTable(), new TL1Table(), new TL2Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2)))
                .From(tablesContext.Table)
                .LeftJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .LeftJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TL1?, TL2?>(this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 1)
                        ? default
                        : this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 2)
                        ? default
                        : this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect11Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table> where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TL2Table : IExprTableSource, new()
    {
        internal readonly ModelSelect10<T, TTable, TJ1, TJ1Table> Source;
        internal readonly ISqModelReader<TL2, TL2Table> Reader2;
        internal readonly Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table>, ExprBoolean> On2;

        internal ModelSelect11Left(ModelSelect10<T, TTable, TJ1, TJ1Table> source, ISqModelReader<TL2, TL2Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader2 = reader;
            this.On2 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;

        public ModelSelect12Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table> LeftJoin<TL3, TL3Table>(
            ISqModelReader<TL3, TL3Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table>, ExprBoolean> on)
            where TL3Table : IExprTableSource, new()
            => new ModelSelect12Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table>, ExprOrderBy>? order,
            Func<ModelRowData<T, TJ1, TL2?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table>, ExprOrderBy> order,
            Func<ModelRowData<T, TJ1, TL2?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TJ1, TL2?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table>, ExprOrderBy>? order,
            KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TL2Table>(new TTable(), new TJ1Table(), new TL2Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .LeftJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TL2?>(this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 2)
                        ? default
                        : this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect20<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table> where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
    {
        internal readonly ModelSelect10<T, TTable, TJ1, TJ1Table> Source;
        internal readonly ISqModelReader<TJ2, TJ2Table> Reader2;
        internal readonly Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2;

        internal ModelSelect20(ModelSelect10<T, TTable, TJ1, TJ1Table> source, ISqModelReader<TJ2, TJ2Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader2 = reader;
            this.On2 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;

        public ModelSelect30<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table> InnerJoin<TJ3, TJ3Table>(
            ISqModelReader<TJ3, TJ3Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean> on)
            where TJ3Table : IExprTableSource, new()
            => new ModelSelect30<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table>(this, reader, on);

        public ModelSelect21Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table> LeftJoin<TL3, TL3Table>(
            ISqModelReader<TL3, TL3Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table>, ExprBoolean> on)
            where TL3Table : IExprTableSource, new()
            => new ModelSelect21Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprOrderBy>? order,
            Func<ModelRowData<T, TJ1, TJ2>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprOrderBy> order,
            Func<ModelRowData<T, TJ1, TJ2>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TJ1, TJ2>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprOrderBy>? order,
            KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table>(new TTable(), new TJ1Table(), new TJ2Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2>(this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect03Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table>
        where TTable : IExprTableSource, new()
        where TL1Table : IExprTableSource, new()
        where TL2Table : IExprTableSource, new()
        where TL3Table : IExprTableSource, new()
    {
        internal readonly ModelSelect02Left<T, TTable, TL1, TL1Table, TL2, TL2Table> Source;
        internal readonly ISqModelReader<TL3, TL3Table> Reader3;
        internal readonly Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table>, ExprBoolean> On3;

        internal ModelSelect03Left(ModelSelect02Left<T, TTable, TL1, TL1Table, TL2, TL2Table> source,
            ISqModelReader<TL3, TL3Table> reader,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader3 = reader;
            this.On3 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TL1, TL1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TL1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TL2, TL2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table>, ExprBoolean> On2 => this.Source.On2;

        public ModelSelect04Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table>
            LeftJoin<TL4, TL4Table>(ISqModelReader<TL4, TL4Table> reader,
                Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean> on)
            where TL4Table : IExprTableSource, new()
            => new ModelSelect04Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table>(this,
                reader,
                on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table>, ExprOrderBy>? order,
            Func<ModelRowData<T, TL1?, TL2?, TL3?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table>, ExprOrderBy> order,
            Func<ModelRowData<T, TL1?, TL2?, TL3?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TL1?, TL2?, TL3?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table>, ExprOrderBy>? order,
            KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table>(new TTable(),
                    new TL1Table(),
                    new TL2Table(),
                    new TL3Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3)))
                .From(tablesContext.Table)
                .LeftJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .LeftJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .LeftJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TL1?, TL2?, TL3?>(this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 1)
                        ? default
                        : this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 2)
                        ? default
                        : this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 3)
                        ? default
                        : this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect12Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table>
        where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TL2Table : IExprTableSource, new()
        where TL3Table : IExprTableSource, new()
    {
        internal readonly ModelSelect11Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table> Source;
        internal readonly ISqModelReader<TL3, TL3Table> Reader3;
        internal readonly Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table>, ExprBoolean> On3;

        internal ModelSelect12Left(ModelSelect11Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table> source,
            ISqModelReader<TL3, TL3Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader3 = reader;
            this.On3 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TL2, TL2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table>, ExprBoolean> On2 => this.Source.On2;

        public ModelSelect13Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table>
            LeftJoin<TL4, TL4Table>(ISqModelReader<TL4, TL4Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean> on)
            where TL4Table : IExprTableSource, new()
            => new ModelSelect13Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table>(this,
                reader,
                on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table>, ExprOrderBy>? order,
            Func<ModelRowData<T, TJ1, TL2?, TL3?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table>, ExprOrderBy> order,
            Func<ModelRowData<T, TJ1, TL2?, TL3?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TJ1, TL2?, TL3?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table>, ExprOrderBy>? order,
            KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table>(new TTable(),
                    new TJ1Table(),
                    new TL2Table(),
                    new TL3Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .LeftJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .LeftJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TL2?, TL3?>(this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 2)
                        ? default
                        : this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 3)
                        ? default
                        : this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect21Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table>
        where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TL3Table : IExprTableSource, new()
    {
        internal readonly ModelSelect20<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table> Source;
        internal readonly ISqModelReader<TL3, TL3Table> Reader3;
        internal readonly Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table>, ExprBoolean> On3;

        internal ModelSelect21Left(ModelSelect20<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table> source,
            ISqModelReader<TL3, TL3Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader3 = reader;
            this.On3 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;

        public ModelSelect22Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table>
            LeftJoin<TL4, TL4Table>(ISqModelReader<TL4, TL4Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table>, ExprBoolean> on)
            where TL4Table : IExprTableSource, new()
            => new ModelSelect22Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table>(this,
                reader,
                on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table>, ExprOrderBy>? order,
            Func<ModelRowData<T, TJ1, TJ2, TL3?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table>, ExprOrderBy> order,
            Func<ModelRowData<T, TJ1, TJ2, TL3?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TJ1, TJ2, TL3?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table>, ExprOrderBy>? order,
            KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table>(new TTable(),
                    new TJ1Table(),
                    new TJ2Table(),
                    new TL3Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .LeftJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TL3?>(this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 3)
                        ? default
                        : this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect30<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table>
        where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TJ3Table : IExprTableSource, new()
    {
        internal readonly ModelSelect20<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table> Source;
        internal readonly ISqModelReader<TJ3, TJ3Table> Reader3;
        internal readonly Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean> On3;

        internal ModelSelect30(ModelSelect20<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table> source,
            ISqModelReader<TJ3, TJ3Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader3 = reader;
            this.On3 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;

        public ModelSelect40<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table>
            InnerJoin<TJ4, TJ4Table>(ISqModelReader<TJ4, TJ4Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprBoolean> on)
            where TJ4Table : IExprTableSource, new()
            => new ModelSelect40<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table>(this,
                reader,
                on);

        public ModelSelect31Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table>
            LeftJoin<TL4, TL4Table>(ISqModelReader<TL4, TL4Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table>, ExprBoolean> on)
            where TL4Table : IExprTableSource, new()
            => new ModelSelect31Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table>(this,
                reader,
                on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprOrderBy>? order,
            Func<ModelRowData<T, TJ1, TJ2, TJ3>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprOrderBy> order,
            Func<ModelRowData<T, TJ1, TJ2, TJ3>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TJ1, TJ2, TJ3>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprOrderBy>? order,
            KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>(new TTable(),
                    new TJ1Table(),
                    new TJ2Table(),
                    new TJ3Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .InnerJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TJ3>(this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect04Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table>
        where TTable : IExprTableSource, new()
        where TL1Table : IExprTableSource, new()
        where TL2Table : IExprTableSource, new()
        where TL3Table : IExprTableSource, new()
        where TL4Table : IExprTableSource, new()
    {
        internal readonly ModelSelect03Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table> Source;
        internal readonly ISqModelReader<TL4, TL4Table> Reader4;

        internal readonly Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean>
            On4;

        internal ModelSelect04Left(ModelSelect03Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table> source,
            ISqModelReader<TL4, TL4Table> reader,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader4 = reader;
            this.On4 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TL1, TL1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TL1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TL2, TL2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TL3, TL3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        public ModelSelect05Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table>
            LeftJoin<TL5, TL5Table>(ISqModelReader<TL5, TL5Table> reader,
                Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>
                    on) where TL5Table : IExprTableSource, new()
            => new ModelSelect05Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5,
                TL5Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table>, ExprOrderBy>? order,
            Func<ModelRowData<T, TL1?, TL2?, TL3?, TL4?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table>, ExprOrderBy> order,
            Func<ModelRowData<T, TL1?, TL2?, TL3?, TL4?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TL1?, TL2?, TL3?, TL4?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table>, ExprOrderBy>? order,
            KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table>(new TTable(),
                    new TL1Table(),
                    new TL2Table(),
                    new TL3Table(),
                    new TL4Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4)))
                .From(tablesContext.Table)
                .LeftJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .LeftJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .LeftJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .LeftJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TL1?, TL2?, TL3?, TL4?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 1)
                        ? default
                        : this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 2)
                        ? default
                        : this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 3)
                        ? default
                        : this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 4)
                        ? default
                        : this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect13Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table>
        where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TL2Table : IExprTableSource, new()
        where TL3Table : IExprTableSource, new()
        where TL4Table : IExprTableSource, new()
    {
        internal readonly ModelSelect12Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table> Source;
        internal readonly ISqModelReader<TL4, TL4Table> Reader4;

        internal readonly Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean>
            On4;

        internal ModelSelect13Left(ModelSelect12Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table> source,
            ISqModelReader<TL4, TL4Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader4 = reader;
            this.On4 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TL2, TL2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TL3, TL3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        public ModelSelect14Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table>
            LeftJoin<TL5, TL5Table>(ISqModelReader<TL5, TL5Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>
                    on) where TL5Table : IExprTableSource, new()
            => new ModelSelect14Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5,
                TL5Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table>, ExprOrderBy>? order,
            Func<ModelRowData<T, TJ1, TL2?, TL3?, TL4?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table>, ExprOrderBy> order,
            Func<ModelRowData<T, TJ1, TL2?, TL3?, TL4?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TJ1, TL2?, TL3?, TL4?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table>, ExprOrderBy>? order,
            KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table>(new TTable(),
                    new TJ1Table(),
                    new TL2Table(),
                    new TL3Table(),
                    new TL4Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .LeftJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .LeftJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .LeftJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TL2?, TL3?, TL4?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 2)
                        ? default
                        : this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 3)
                        ? default
                        : this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 4)
                        ? default
                        : this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect22Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table>
        where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TL3Table : IExprTableSource, new()
        where TL4Table : IExprTableSource, new()
    {
        internal readonly ModelSelect21Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table> Source;
        internal readonly ISqModelReader<TL4, TL4Table> Reader4;

        internal readonly Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table>, ExprBoolean>
            On4;

        internal ModelSelect22Left(ModelSelect21Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table> source,
            ISqModelReader<TL4, TL4Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader4 = reader;
            this.On4 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TL3, TL3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        public ModelSelect23Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table>
            LeftJoin<TL5, TL5Table>(ISqModelReader<TL5, TL5Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>
                    on) where TL5Table : IExprTableSource, new()
            => new ModelSelect23Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table, TL5,
                TL5Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table>, ExprOrderBy>? order,
            Func<ModelRowData<T, TJ1, TJ2, TL3?, TL4?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table>, ExprOrderBy> order,
            Func<ModelRowData<T, TJ1, TJ2, TL3?, TL4?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TJ1, TJ2, TL3?, TL4?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table>, ExprOrderBy>? order,
            KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table>(new TTable(),
                    new TJ1Table(),
                    new TJ2Table(),
                    new TL3Table(),
                    new TL4Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .LeftJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .LeftJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TL3?, TL4?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 3)
                        ? default
                        : this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 4)
                        ? default
                        : this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect31Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table>
        where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TJ3Table : IExprTableSource, new()
        where TL4Table : IExprTableSource, new()
    {
        internal readonly ModelSelect30<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table> Source;
        internal readonly ISqModelReader<TL4, TL4Table> Reader4;

        internal readonly Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table>, ExprBoolean>
            On4;

        internal ModelSelect31Left(ModelSelect30<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table> source,
            ISqModelReader<TL4, TL4Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader4 = reader;
            this.On4 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TJ3, TJ3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        public ModelSelect32Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table, TL5, TL5Table>
            LeftJoin<TL5, TL5Table>(ISqModelReader<TL5, TL5Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table>, ExprBoolean>
                    on) where TL5Table : IExprTableSource, new()
            => new ModelSelect32Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table, TL5,
                TL5Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table>, ExprOrderBy>? order,
            Func<ModelRowData<T, TJ1, TJ2, TJ3, TL4?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table>, ExprOrderBy> order,
            Func<ModelRowData<T, TJ1, TJ2, TJ3, TL4?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TJ1, TJ2, TJ3, TL4?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table>, ExprOrderBy>? order,
            KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table>(new TTable(),
                    new TJ1Table(),
                    new TJ2Table(),
                    new TJ3Table(),
                    new TL4Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .InnerJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .LeftJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TJ3, TL4?>(this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 4)
                        ? default
                        : this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect40<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table>
        where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TJ3Table : IExprTableSource, new()
        where TJ4Table : IExprTableSource, new()
    {
        internal readonly ModelSelect30<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table> Source;
        internal readonly ISqModelReader<TJ4, TJ4Table> Reader4;

        internal readonly Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprBoolean>
            On4;

        internal ModelSelect40(ModelSelect30<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table> source,
            ISqModelReader<TJ4, TJ4Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader4 = reader;
            this.On4 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TJ3, TJ3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        public ModelSelect50<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table>
            InnerJoin<TJ5, TJ5Table>(ISqModelReader<TJ5, TJ5Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table>, ExprBoolean>
                    on) where TJ5Table : IExprTableSource, new()
            => new ModelSelect50<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table>(
                this,
                reader,
                on);

        public ModelSelect41Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TL5, TL5Table>
            LeftJoin<TL5, TL5Table>(ISqModelReader<TL5, TL5Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table>, ExprBoolean>
                    on) where TL5Table : IExprTableSource, new()
            => new ModelSelect41Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TL5,
                TL5Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprOrderBy>? order,
            Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprOrderBy> order,
            Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprOrderBy>? order,
            KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>(new TTable(),
                    new TJ1Table(),
                    new TJ2Table(),
                    new TJ3Table(),
                    new TJ4Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .InnerJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .InnerJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TJ3, TJ4>(this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect05Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table>
        where TTable : IExprTableSource, new()
        where TL1Table : IExprTableSource, new()
        where TL2Table : IExprTableSource, new()
        where TL3Table : IExprTableSource, new()
        where TL4Table : IExprTableSource, new()
        where TL5Table : IExprTableSource, new()
    {
        internal readonly ModelSelect04Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table>
            Source;

        internal readonly ISqModelReader<TL5, TL5Table> Reader5;

        internal readonly Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table>,
            ExprBoolean> On5;

        internal ModelSelect05Left(
            ModelSelect04Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table> source,
            ISqModelReader<TL5, TL5Table> reader,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader5 = reader;
            this.On5 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TL1, TL1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TL1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TL2, TL2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TL3, TL3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TL4, TL4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        public
            ModelSelect06Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table, TL6,
                TL6Table> LeftJoin<TL6, TL6Table>(ISqModelReader<TL6, TL6Table> reader,
                Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                    ExprBoolean> on) where TL6Table : IExprTableSource, new()
            => new ModelSelect06Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5,
                TL5Table, TL6, TL6Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprOrderBy>?
                order, Func<ModelRowData<T, TL1?, TL2?, TL3?, TL4?, TL5?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprOrderBy>
                order, Func<ModelRowData<T, TL1?, TL2?, TL3?, TL4?, TL5?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(
            Func<ModelRowData<T, TL1?, TL2?, TL3?, TL4?, TL5?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprOrderBy>?
                order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext = new ModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table>(
                new TTable(),
                new TL1Table(),
                new TL2Table(),
                new TL3Table(),
                new TL4Table(),
                new TL5Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5)))
                .From(tablesContext.Table)
                .LeftJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .LeftJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .LeftJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .LeftJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .LeftJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TL1?, TL2?, TL3?, TL4?, TL5?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 1)
                        ? default
                        : this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 2)
                        ? default
                        : this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 3)
                        ? default
                        : this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 4)
                        ? default
                        : this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 5)
                        ? default
                        : this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect14Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table>
        where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TL2Table : IExprTableSource, new()
        where TL3Table : IExprTableSource, new()
        where TL4Table : IExprTableSource, new()
        where TL5Table : IExprTableSource, new()
    {
        internal readonly ModelSelect13Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table>
            Source;

        internal readonly ISqModelReader<TL5, TL5Table> Reader5;

        internal readonly Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table>,
            ExprBoolean> On5;

        internal ModelSelect14Left(
            ModelSelect13Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table> source,
            ISqModelReader<TL5, TL5Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader5 = reader;
            this.On5 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TL2, TL2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TL3, TL3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TL4, TL4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        public
            ModelSelect15Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table, TL6,
                TL6Table> LeftJoin<TL6, TL6Table>(ISqModelReader<TL6, TL6Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                    ExprBoolean> on) where TL6Table : IExprTableSource, new()
            => new ModelSelect15Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5,
                TL5Table, TL6, TL6Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprOrderBy>?
                order, Func<ModelRowData<T, TJ1, TL2?, TL3?, TL4?, TL5?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprOrderBy>
                order, Func<ModelRowData<T, TJ1, TL2?, TL3?, TL4?, TL5?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TJ1, TL2?, TL3?, TL4?, TL5?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprOrderBy>?
                order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext = new ModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table>(
                new TTable(),
                new TJ1Table(),
                new TL2Table(),
                new TL3Table(),
                new TL4Table(),
                new TL5Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .LeftJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .LeftJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .LeftJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .LeftJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TL2?, TL3?, TL4?, TL5?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 2)
                        ? default
                        : this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 3)
                        ? default
                        : this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 4)
                        ? default
                        : this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 5)
                        ? default
                        : this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect23Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table>
        where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TL3Table : IExprTableSource, new()
        where TL4Table : IExprTableSource, new()
        where TL5Table : IExprTableSource, new()
    {
        internal readonly ModelSelect22Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table>
            Source;

        internal readonly ISqModelReader<TL5, TL5Table> Reader5;

        internal readonly Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table>,
            ExprBoolean> On5;

        internal ModelSelect23Left(
            ModelSelect22Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table> source,
            ISqModelReader<TL5, TL5Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader5 = reader;
            this.On5 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TL3, TL3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TL4, TL4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        public
            ModelSelect24Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table, TL6,
                TL6Table> LeftJoin<TL6, TL6Table>(ISqModelReader<TL6, TL6Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                    ExprBoolean> on) where TL6Table : IExprTableSource, new()
            => new ModelSelect24Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table, TL5,
                TL5Table, TL6, TL6Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table>, ExprOrderBy>?
                order, Func<ModelRowData<T, TJ1, TJ2, TL3?, TL4?, TL5?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table>, ExprOrderBy>
                order, Func<ModelRowData<T, TJ1, TJ2, TL3?, TL4?, TL5?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TJ1, TJ2, TL3?, TL4?, TL5?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table>, ExprOrderBy>?
                order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext = new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table>(
                new TTable(),
                new TJ1Table(),
                new TJ2Table(),
                new TL3Table(),
                new TL4Table(),
                new TL5Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .LeftJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .LeftJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .LeftJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TL3?, TL4?, TL5?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 3)
                        ? default
                        : this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 4)
                        ? default
                        : this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 5)
                        ? default
                        : this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect32Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table, TL5, TL5Table>
        where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TJ3Table : IExprTableSource, new()
        where TL4Table : IExprTableSource, new()
        where TL5Table : IExprTableSource, new()
    {
        internal readonly ModelSelect31Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table>
            Source;

        internal readonly ISqModelReader<TL5, TL5Table> Reader5;

        internal readonly Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table>,
            ExprBoolean> On5;

        internal ModelSelect32Left(
            ModelSelect31Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table> source,
            ISqModelReader<TL5, TL5Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader5 = reader;
            this.On5 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TJ3, TJ3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TL4, TL4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        public
            ModelSelect33Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table, TL5, TL5Table, TL6,
                TL6Table> LeftJoin<TL6, TL6Table>(ISqModelReader<TL6, TL6Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table>,
                    ExprBoolean> on) where TL6Table : IExprTableSource, new()
            => new ModelSelect33Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table, TL5,
                TL5Table, TL6, TL6Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table>, ExprOrderBy>?
                order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TL4?, TL5?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table>, ExprOrderBy>
                order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TL4?, TL5?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TJ1, TJ2, TJ3, TL4?, TL5?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table>, ExprOrderBy>?
                order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext = new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table>(
                new TTable(),
                new TJ1Table(),
                new TJ2Table(),
                new TJ3Table(),
                new TL4Table(),
                new TL5Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .InnerJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .LeftJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .LeftJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TJ3, TL4?, TL5?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 4)
                        ? default
                        : this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 5)
                        ? default
                        : this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect41Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TL5, TL5Table>
        where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TJ3Table : IExprTableSource, new()
        where TJ4Table : IExprTableSource, new()
        where TL5Table : IExprTableSource, new()
    {
        internal readonly ModelSelect40<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table> Source;
        internal readonly ISqModelReader<TL5, TL5Table> Reader5;

        internal readonly Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table>,
            ExprBoolean> On5;

        internal ModelSelect41Left(
            ModelSelect40<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table> source,
            ISqModelReader<TL5, TL5Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader5 = reader;
            this.On5 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TJ3, TJ3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TJ4, TJ4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        public
            ModelSelect42Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TL5, TL5Table, TL6,
                TL6Table> LeftJoin<TL6, TL6Table>(ISqModelReader<TL6, TL6Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table>,
                    ExprBoolean> on) where TL6Table : IExprTableSource, new()
            => new ModelSelect42Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TL5,
                TL5Table, TL6, TL6Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table>, ExprOrderBy>?
                order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TL5?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table>, ExprOrderBy>
                order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TL5?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TL5?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table>, ExprOrderBy>?
                order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext = new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table>(
                new TTable(),
                new TJ1Table(),
                new TJ2Table(),
                new TJ3Table(),
                new TJ4Table(),
                new TL5Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .InnerJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .InnerJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .LeftJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TL5?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 5)
                        ? default
                        : this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class ModelSelect50<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table>
        where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TJ3Table : IExprTableSource, new()
        where TJ4Table : IExprTableSource, new()
        where TJ5Table : IExprTableSource, new()
    {
        internal readonly ModelSelect40<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table> Source;
        internal readonly ISqModelReader<TJ5, TJ5Table> Reader5;

        internal readonly Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table>,
            ExprBoolean> On5;

        internal ModelSelect50(
            ModelSelect40<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table> source,
            ISqModelReader<TJ5, TJ5Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table>, ExprBoolean> on)
        {
            this.Source = source;
            this.Reader5 = reader;
            this.On5 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TJ3, TJ3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TJ4, TJ4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        public
            ModelSelect60<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table, TJ6,
                TJ6Table> InnerJoin<TJ6, TJ6Table>(ISqModelReader<TJ6, TJ6Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table>,
                    ExprBoolean> on) where TJ6Table : IExprTableSource, new()
            => new ModelSelect60<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table,
                TJ6, TJ6Table>(this, reader, on);

        public
            ModelSelect51Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table, TL6,
                TL6Table> LeftJoin<TL6, TL6Table>(ISqModelReader<TL6, TL6Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table>,
                    ExprBoolean> on) where TL6Table : IExprTableSource, new()
            => new ModelSelect51Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5,
                TJ5Table, TL6, TL6Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table>, ExprOrderBy>?
                order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table>, ExprOrderBy>
                order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table>, ExprBoolean>?
                filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table>, ExprOrderBy>?
                order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext = new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table>(
                new TTable(),
                new TJ1Table(),
                new TJ2Table(),
                new TJ3Table(),
                new TJ4Table(),
                new TJ5Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .InnerJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .InnerJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .InnerJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class
        ModelSelect06Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table, TL6,
            TL6Table> where TTable : IExprTableSource, new()
        where TL1Table : IExprTableSource, new()
        where TL2Table : IExprTableSource, new()
        where TL3Table : IExprTableSource, new()
        where TL4Table : IExprTableSource, new()
        where TL5Table : IExprTableSource, new()
        where TL6Table : IExprTableSource, new()
    {
        internal readonly ModelSelect05Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5,
            TL5Table> Source;

        internal readonly ISqModelReader<TL6, TL6Table> Reader6;

        internal readonly
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean> On6;

        internal ModelSelect06Left(
            ModelSelect05Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table>
                source, ISqModelReader<TL6, TL6Table> reader,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean> on)
        {
            this.Source = source;
            this.Reader6 = reader;
            this.On6 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TL1, TL1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TL1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TL2, TL2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TL3, TL3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TL4, TL4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        internal ISqModelReader<TL5, TL5Table> Reader5 => this.Source.Reader5;

        internal Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>
            On5 => this.Source.On5;

        public
            ModelSelect07Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table, TL6,
                TL6Table, TL7, TL7Table> LeftJoin<TL7, TL7Table>(ISqModelReader<TL7, TL7Table> reader,
                Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table,
                    TL7Table>, ExprBoolean> on) where TL7Table : IExprTableSource, new()
            => new ModelSelect07Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5,
                TL5Table, TL6, TL6Table, TL7, TL7Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprOrderBy>? order, Func<ModelRowData<T, TL1?, TL2?, TL3?, TL4?, TL5?, TL6?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprOrderBy> order, Func<ModelRowData<T, TL1?, TL2?, TL3?, TL4?, TL5?, TL6?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(
            Func<ModelRowData<T, TL1?, TL2?, TL3?, TL4?, TL5?, TL6?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprOrderBy>? order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>(
                    new TTable(),
                    new TL1Table(),
                    new TL2Table(),
                    new TL3Table(),
                    new TL4Table(),
                    new TL5Table(),
                    new TL6Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5),
                    this.Reader6.GetColumns(tablesContext.JoinedTable6)))
                .From(tablesContext.Table)
                .LeftJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .LeftJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .LeftJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .LeftJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .LeftJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .LeftJoin(tablesContext.JoinedTable6, this.On6(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TL1?, TL2?, TL3?, TL4?, TL5?, TL6?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 1)
                        ? default
                        : this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 2)
                        ? default
                        : this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 3)
                        ? default
                        : this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 4)
                        ? default
                        : this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 5)
                        ? default
                        : this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 6)
                        ? default
                        : this.Reader6.ReadOrdinal(r, tablesContext.JoinedTable6, offsets[6]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class
        ModelSelect15Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table, TL6,
            TL6Table> where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TL2Table : IExprTableSource, new()
        where TL3Table : IExprTableSource, new()
        where TL4Table : IExprTableSource, new()
        where TL5Table : IExprTableSource, new()
        where TL6Table : IExprTableSource, new()
    {
        internal readonly ModelSelect14Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5,
            TL5Table> Source;

        internal readonly ISqModelReader<TL6, TL6Table> Reader6;

        internal readonly
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean> On6;

        internal ModelSelect15Left(
            ModelSelect14Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table>
                source, ISqModelReader<TL6, TL6Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean> on)
        {
            this.Source = source;
            this.Reader6 = reader;
            this.On6 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TL2, TL2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TL3, TL3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TL4, TL4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        internal ISqModelReader<TL5, TL5Table> Reader5 => this.Source.Reader5;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>
            On5 => this.Source.On5;

        public
            ModelSelect16Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table, TL6,
                TL6Table, TL7, TL7Table> LeftJoin<TL7, TL7Table>(ISqModelReader<TL7, TL7Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table,
                    TL7Table>, ExprBoolean> on) where TL7Table : IExprTableSource, new()
            => new ModelSelect16Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5,
                TL5Table, TL6, TL6Table, TL7, TL7Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprOrderBy>? order, Func<ModelRowData<T, TJ1, TL2?, TL3?, TL4?, TL5?, TL6?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprOrderBy> order, Func<ModelRowData<T, TJ1, TL2?, TL3?, TL4?, TL5?, TL6?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(
            Func<ModelRowData<T, TJ1, TL2?, TL3?, TL4?, TL5?, TL6?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprOrderBy>? order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>(
                    new TTable(),
                    new TJ1Table(),
                    new TL2Table(),
                    new TL3Table(),
                    new TL4Table(),
                    new TL5Table(),
                    new TL6Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5),
                    this.Reader6.GetColumns(tablesContext.JoinedTable6)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .LeftJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .LeftJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .LeftJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .LeftJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .LeftJoin(tablesContext.JoinedTable6, this.On6(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TL2?, TL3?, TL4?, TL5?, TL6?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 2)
                        ? default
                        : this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 3)
                        ? default
                        : this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 4)
                        ? default
                        : this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 5)
                        ? default
                        : this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 6)
                        ? default
                        : this.Reader6.ReadOrdinal(r, tablesContext.JoinedTable6, offsets[6]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class
        ModelSelect24Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table, TL6,
            TL6Table> where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TL3Table : IExprTableSource, new()
        where TL4Table : IExprTableSource, new()
        where TL5Table : IExprTableSource, new()
        where TL6Table : IExprTableSource, new()
    {
        internal readonly ModelSelect23Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table, TL5,
            TL5Table> Source;

        internal readonly ISqModelReader<TL6, TL6Table> Reader6;

        internal readonly
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean> On6;

        internal ModelSelect24Left(
            ModelSelect23Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table>
                source, ISqModelReader<TL6, TL6Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean> on)
        {
            this.Source = source;
            this.Reader6 = reader;
            this.On6 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TL3, TL3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TL4, TL4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        internal ISqModelReader<TL5, TL5Table> Reader5 => this.Source.Reader5;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>
            On5 => this.Source.On5;

        public
            ModelSelect25Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table, TL6,
                TL6Table, TL7, TL7Table> LeftJoin<TL7, TL7Table>(ISqModelReader<TL7, TL7Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table,
                    TL7Table>, ExprBoolean> on) where TL7Table : IExprTableSource, new()
            => new ModelSelect25Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table, TL5,
                TL5Table, TL6, TL6Table, TL7, TL7Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprOrderBy>? order, Func<ModelRowData<T, TJ1, TJ2, TL3?, TL4?, TL5?, TL6?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprOrderBy> order, Func<ModelRowData<T, TJ1, TJ2, TL3?, TL4?, TL5?, TL6?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(
            Func<ModelRowData<T, TJ1, TJ2, TL3?, TL4?, TL5?, TL6?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
                ExprOrderBy>? order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table>(
                    new TTable(),
                    new TJ1Table(),
                    new TJ2Table(),
                    new TL3Table(),
                    new TL4Table(),
                    new TL5Table(),
                    new TL6Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5),
                    this.Reader6.GetColumns(tablesContext.JoinedTable6)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .LeftJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .LeftJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .LeftJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .LeftJoin(tablesContext.JoinedTable6, this.On6(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TL3?, TL4?, TL5?, TL6?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 3)
                        ? default
                        : this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 4)
                        ? default
                        : this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 5)
                        ? default
                        : this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 6)
                        ? default
                        : this.Reader6.ReadOrdinal(r, tablesContext.JoinedTable6, offsets[6]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class
        ModelSelect33Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table, TL5, TL5Table, TL6,
            TL6Table> where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TJ3Table : IExprTableSource, new()
        where TL4Table : IExprTableSource, new()
        where TL5Table : IExprTableSource, new()
        where TL6Table : IExprTableSource, new()
    {
        internal readonly ModelSelect32Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table, TL5,
            TL5Table> Source;

        internal readonly ISqModelReader<TL6, TL6Table> Reader6;

        internal readonly
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean> On6;

        internal ModelSelect33Left(
            ModelSelect32Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table, TL5, TL5Table>
                source, ISqModelReader<TL6, TL6Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean> on)
        {
            this.Source = source;
            this.Reader6 = reader;
            this.On6 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TJ3, TJ3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TL4, TL4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        internal ISqModelReader<TL5, TL5Table> Reader5 => this.Source.Reader5;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table>, ExprBoolean>
            On5 => this.Source.On5;

        public
            ModelSelect34Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table, TL5, TL5Table, TL6,
                TL6Table, TL7, TL7Table> LeftJoin<TL7, TL7Table>(ISqModelReader<TL7, TL7Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table,
                    TL7Table>, ExprBoolean> on) where TL7Table : IExprTableSource, new()
            => new ModelSelect34Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table, TL5,
                TL5Table, TL6, TL6Table, TL7, TL7Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table>,
                ExprOrderBy>? order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TL4?, TL5?, TL6?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table>,
                ExprOrderBy> order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TL4?, TL5?, TL6?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(
            Func<ModelRowData<T, TJ1, TJ2, TJ3, TL4?, TL5?, TL6?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table>,
                ExprOrderBy>? order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table>(
                    new TTable(),
                    new TJ1Table(),
                    new TJ2Table(),
                    new TJ3Table(),
                    new TL4Table(),
                    new TL5Table(),
                    new TL6Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5),
                    this.Reader6.GetColumns(tablesContext.JoinedTable6)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .InnerJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .LeftJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .LeftJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .LeftJoin(tablesContext.JoinedTable6, this.On6(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TJ3, TL4?, TL5?, TL6?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 4)
                        ? default
                        : this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 5)
                        ? default
                        : this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 6)
                        ? default
                        : this.Reader6.ReadOrdinal(r, tablesContext.JoinedTable6, offsets[6]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class
        ModelSelect42Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TL5, TL5Table, TL6,
            TL6Table> where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TJ3Table : IExprTableSource, new()
        where TJ4Table : IExprTableSource, new()
        where TL5Table : IExprTableSource, new()
        where TL6Table : IExprTableSource, new()
    {
        internal readonly ModelSelect41Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TL5,
            TL5Table> Source;

        internal readonly ISqModelReader<TL6, TL6Table> Reader6;

        internal readonly
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table>,
                ExprBoolean> On6;

        internal ModelSelect42Left(
            ModelSelect41Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TL5, TL5Table>
                source, ISqModelReader<TL6, TL6Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table>,
                ExprBoolean> on)
        {
            this.Source = source;
            this.Reader6 = reader;
            this.On6 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TJ3, TJ3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TJ4, TJ4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        internal ISqModelReader<TL5, TL5Table> Reader5 => this.Source.Reader5;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table>, ExprBoolean>
            On5 => this.Source.On5;

        public
            ModelSelect43Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TL5, TL5Table, TL6,
                TL6Table, TL7, TL7Table> LeftJoin<TL7, TL7Table>(ISqModelReader<TL7, TL7Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table,
                    TL7Table>, ExprBoolean> on) where TL7Table : IExprTableSource, new()
            => new ModelSelect43Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TL5,
                TL5Table, TL6, TL6Table, TL7, TL7Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table>,
                ExprOrderBy>? order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TL5?, TL6?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table>,
                ExprOrderBy> order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TL5?, TL6?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(
            Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TL5?, TL6?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table>,
                ExprOrderBy>? order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table>(
                    new TTable(),
                    new TJ1Table(),
                    new TJ2Table(),
                    new TJ3Table(),
                    new TJ4Table(),
                    new TL5Table(),
                    new TL6Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5),
                    this.Reader6.GetColumns(tablesContext.JoinedTable6)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .InnerJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .InnerJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .LeftJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .LeftJoin(tablesContext.JoinedTable6, this.On6(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TL5?, TL6?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 5)
                        ? default
                        : this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 6)
                        ? default
                        : this.Reader6.ReadOrdinal(r, tablesContext.JoinedTable6, offsets[6]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class
        ModelSelect51Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table, TL6,
            TL6Table> where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TJ3Table : IExprTableSource, new()
        where TJ4Table : IExprTableSource, new()
        where TJ5Table : IExprTableSource, new()
        where TL6Table : IExprTableSource, new()
    {
        internal readonly ModelSelect50<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5,
            TJ5Table> Source;

        internal readonly ISqModelReader<TL6, TL6Table> Reader6;

        internal readonly
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table>,
                ExprBoolean> On6;

        internal ModelSelect51Left(
            ModelSelect50<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table> source,
            ISqModelReader<TL6, TL6Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table>,
                ExprBoolean> on)
        {
            this.Source = source;
            this.Reader6 = reader;
            this.On6 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TJ3, TJ3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TJ4, TJ4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        internal ISqModelReader<TJ5, TJ5Table> Reader5 => this.Source.Reader5;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table>, ExprBoolean>
            On5 => this.Source.On5;

        public
            ModelSelect52Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table, TL6,
                TL6Table, TL7, TL7Table> LeftJoin<TL7, TL7Table>(ISqModelReader<TL7, TL7Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table,
                    TL7Table>, ExprBoolean> on) where TL7Table : IExprTableSource, new()
            => new ModelSelect52Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5,
                TJ5Table, TL6, TL6Table, TL7, TL7Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table>,
                ExprOrderBy>? order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TL6?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table>,
                ExprOrderBy> order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TL6?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(
            Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TL6?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table>,
                ExprOrderBy>? order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table>(
                    new TTable(),
                    new TJ1Table(),
                    new TJ2Table(),
                    new TJ3Table(),
                    new TJ4Table(),
                    new TJ5Table(),
                    new TL6Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5),
                    this.Reader6.GetColumns(tablesContext.JoinedTable6)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .InnerJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .InnerJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .InnerJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .LeftJoin(tablesContext.JoinedTable6, this.On6(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TL6?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 6)
                        ? default
                        : this.Reader6.ReadOrdinal(r, tablesContext.JoinedTable6, offsets[6]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class
        ModelSelect60<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table, TJ6,
            TJ6Table> where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TJ3Table : IExprTableSource, new()
        where TJ4Table : IExprTableSource, new()
        where TJ5Table : IExprTableSource, new()
        where TJ6Table : IExprTableSource, new()
    {
        internal readonly ModelSelect50<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5,
            TJ5Table> Source;

        internal readonly ISqModelReader<TJ6, TJ6Table> Reader6;

        internal readonly
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table>,
                ExprBoolean> On6;

        internal ModelSelect60(
            ModelSelect50<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table> source,
            ISqModelReader<TJ6, TJ6Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table>,
                ExprBoolean> on)
        {
            this.Source = source;
            this.Reader6 = reader;
            this.On6 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TJ3, TJ3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TJ4, TJ4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        internal ISqModelReader<TJ5, TJ5Table> Reader5 => this.Source.Reader5;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table>, ExprBoolean>
            On5 => this.Source.On5;

        public
            ModelSelect70<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table, TJ6,
                TJ6Table, TJ7, TJ7Table> InnerJoin<TJ7, TJ7Table>(ISqModelReader<TJ7, TJ7Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table,
                    TJ7Table>, ExprBoolean> on) where TJ7Table : IExprTableSource, new()
            => new ModelSelect70<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table,
                TJ6, TJ6Table, TJ7, TJ7Table>(this, reader, on);

        public
            ModelSelect61Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table, TJ6,
                TJ6Table, TL7, TL7Table> LeftJoin<TL7, TL7Table>(ISqModelReader<TL7, TL7Table> reader,
                Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table,
                    TL7Table>, ExprBoolean> on) where TL7Table : IExprTableSource, new()
            => new ModelSelect61Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5,
                TJ5Table, TJ6, TJ6Table, TL7, TL7Table>(this, reader, on);

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table>,
                ExprOrderBy>? order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TJ6>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table>,
                ExprOrderBy> order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TJ6>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(
            Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TJ6>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table>,
                ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table>,
                ExprOrderBy>? order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table>(
                    new TTable(),
                    new TJ1Table(),
                    new TJ2Table(),
                    new TJ3Table(),
                    new TJ4Table(),
                    new TJ5Table(),
                    new TJ6Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5),
                    this.Reader6.GetColumns(tablesContext.JoinedTable6)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .InnerJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .InnerJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .InnerJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .InnerJoin(tablesContext.JoinedTable6, this.On6(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TJ6>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]),
                    this.Reader6.ReadOrdinal(r, tablesContext.JoinedTable6, offsets[6]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class
        ModelSelect07Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table, TL6,
            TL6Table, TL7, TL7Table> where TTable : IExprTableSource, new()
        where TL1Table : IExprTableSource, new()
        where TL2Table : IExprTableSource, new()
        where TL3Table : IExprTableSource, new()
        where TL4Table : IExprTableSource, new()
        where TL5Table : IExprTableSource, new()
        where TL6Table : IExprTableSource, new()
        where TL7Table : IExprTableSource, new()
    {
        internal readonly ModelSelect06Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5,
            TL5Table, TL6, TL6Table> Source;

        internal readonly ISqModelReader<TL7, TL7Table> Reader7;

        internal readonly
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean> On7;

        internal ModelSelect07Left(
            ModelSelect06Left<T, TTable, TL1, TL1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table, TL6,
                TL6Table> source, ISqModelReader<TL7, TL7Table> reader,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean> on)
        {
            this.Source = source;
            this.Reader7 = reader;
            this.On7 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TL1, TL1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TL1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TL2, TL2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TL3, TL3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TL4, TL4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        internal ISqModelReader<TL5, TL5Table> Reader5 => this.Source.Reader5;

        internal Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>
            On5 => this.Source.On5;

        internal ISqModelReader<TL6, TL6Table> Reader6 => this.Source.Reader6;

        internal Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
            ExprBoolean> On6 => this.Source.On6;

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprOrderBy>? order, Func<ModelRowData<T, TL1?, TL2?, TL3?, TL4?, TL5?, TL6?, TL7?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprOrderBy> order, Func<ModelRowData<T, TL1?, TL2?, TL3?, TL4?, TL5?, TL6?, TL7?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(
            Func<ModelRowData<T, TL1?, TL2?, TL3?, TL4?, TL5?, TL6?, TL7?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprOrderBy>? order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TL1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table,
                    TL7Table>(new TTable(),
                    new TL1Table(),
                    new TL2Table(),
                    new TL3Table(),
                    new TL4Table(),
                    new TL5Table(),
                    new TL6Table(),
                    new TL7Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5),
                    this.Reader6.GetColumns(tablesContext.JoinedTable6),
                    this.Reader7.GetColumns(tablesContext.JoinedTable7)))
                .From(tablesContext.Table)
                .LeftJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .LeftJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .LeftJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .LeftJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .LeftJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .LeftJoin(tablesContext.JoinedTable6, this.On6(tablesContext))
                .LeftJoin(tablesContext.JoinedTable7, this.On7(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TL1?, TL2?, TL3?, TL4?, TL5?, TL6?, TL7?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 1)
                        ? default
                        : this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 2)
                        ? default
                        : this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 3)
                        ? default
                        : this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 4)
                        ? default
                        : this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 5)
                        ? default
                        : this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 6)
                        ? default
                        : this.Reader6.ReadOrdinal(r, tablesContext.JoinedTable6, offsets[6]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 7)
                        ? default
                        : this.Reader7.ReadOrdinal(r, tablesContext.JoinedTable7, offsets[7]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class
        ModelSelect16Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table, TL6,
            TL6Table, TL7, TL7Table> where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TL2Table : IExprTableSource, new()
        where TL3Table : IExprTableSource, new()
        where TL4Table : IExprTableSource, new()
        where TL5Table : IExprTableSource, new()
        where TL6Table : IExprTableSource, new()
        where TL7Table : IExprTableSource, new()
    {
        internal readonly ModelSelect15Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5,
            TL5Table, TL6, TL6Table> Source;

        internal readonly ISqModelReader<TL7, TL7Table> Reader7;

        internal readonly
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean> On7;

        internal ModelSelect16Left(
            ModelSelect15Left<T, TTable, TJ1, TJ1Table, TL2, TL2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table, TL6,
                TL6Table> source, ISqModelReader<TL7, TL7Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean> on)
        {
            this.Source = source;
            this.Reader7 = reader;
            this.On7 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TL2, TL2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TL3, TL3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TL4, TL4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        internal ISqModelReader<TL5, TL5Table> Reader5 => this.Source.Reader5;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>
            On5 => this.Source.On5;

        internal ISqModelReader<TL6, TL6Table> Reader6 => this.Source.Reader6;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
            ExprBoolean> On6 => this.Source.On6;

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprOrderBy>? order, Func<ModelRowData<T, TJ1, TL2?, TL3?, TL4?, TL5?, TL6?, TL7?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprOrderBy> order, Func<ModelRowData<T, TJ1, TL2?, TL3?, TL4?, TL5?, TL6?, TL7?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(
            Func<ModelRowData<T, TJ1, TL2?, TL3?, TL4?, TL5?, TL6?, TL7?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprOrderBy>? order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TL2Table, TL3Table, TL4Table, TL5Table, TL6Table,
                    TL7Table>(new TTable(),
                    new TJ1Table(),
                    new TL2Table(),
                    new TL3Table(),
                    new TL4Table(),
                    new TL5Table(),
                    new TL6Table(),
                    new TL7Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5),
                    this.Reader6.GetColumns(tablesContext.JoinedTable6),
                    this.Reader7.GetColumns(tablesContext.JoinedTable7)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .LeftJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .LeftJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .LeftJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .LeftJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .LeftJoin(tablesContext.JoinedTable6, this.On6(tablesContext))
                .LeftJoin(tablesContext.JoinedTable7, this.On7(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TL2?, TL3?, TL4?, TL5?, TL6?, TL7?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 2)
                        ? default
                        : this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 3)
                        ? default
                        : this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 4)
                        ? default
                        : this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 5)
                        ? default
                        : this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 6)
                        ? default
                        : this.Reader6.ReadOrdinal(r, tablesContext.JoinedTable6, offsets[6]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 7)
                        ? default
                        : this.Reader7.ReadOrdinal(r, tablesContext.JoinedTable7, offsets[7]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class
        ModelSelect25Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table, TL6,
            TL6Table, TL7, TL7Table> where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TL3Table : IExprTableSource, new()
        where TL4Table : IExprTableSource, new()
        where TL5Table : IExprTableSource, new()
        where TL6Table : IExprTableSource, new()
        where TL7Table : IExprTableSource, new()
    {
        internal readonly ModelSelect24Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table, TL5,
            TL5Table, TL6, TL6Table> Source;

        internal readonly ISqModelReader<TL7, TL7Table> Reader7;

        internal readonly
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean> On7;

        internal ModelSelect25Left(
            ModelSelect24Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TL3, TL3Table, TL4, TL4Table, TL5, TL5Table, TL6,
                TL6Table> source, ISqModelReader<TL7, TL7Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean> on)
        {
            this.Source = source;
            this.Reader7 = reader;
            this.On7 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TL3, TL3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TL4, TL4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        internal ISqModelReader<TL5, TL5Table> Reader5 => this.Source.Reader5;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table>, ExprBoolean>
            On5 => this.Source.On5;

        internal ISqModelReader<TL6, TL6Table> Reader6 => this.Source.Reader6;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table>,
            ExprBoolean> On6 => this.Source.On6;

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprOrderBy>? order, Func<ModelRowData<T, TJ1, TJ2, TL3?, TL4?, TL5?, TL6?, TL7?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprOrderBy> order, Func<ModelRowData<T, TJ1, TJ2, TL3?, TL4?, TL5?, TL6?, TL7?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(
            Func<ModelRowData<T, TJ1, TJ2, TL3?, TL4?, TL5?, TL6?, TL7?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprOrderBy>? order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TL3Table, TL4Table, TL5Table, TL6Table,
                    TL7Table>(new TTable(),
                    new TJ1Table(),
                    new TJ2Table(),
                    new TL3Table(),
                    new TL4Table(),
                    new TL5Table(),
                    new TL6Table(),
                    new TL7Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5),
                    this.Reader6.GetColumns(tablesContext.JoinedTable6),
                    this.Reader7.GetColumns(tablesContext.JoinedTable7)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .LeftJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .LeftJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .LeftJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .LeftJoin(tablesContext.JoinedTable6, this.On6(tablesContext))
                .LeftJoin(tablesContext.JoinedTable7, this.On7(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TL3?, TL4?, TL5?, TL6?, TL7?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 3)
                        ? default
                        : this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 4)
                        ? default
                        : this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 5)
                        ? default
                        : this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 6)
                        ? default
                        : this.Reader6.ReadOrdinal(r, tablesContext.JoinedTable6, offsets[6]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 7)
                        ? default
                        : this.Reader7.ReadOrdinal(r, tablesContext.JoinedTable7, offsets[7]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class
        ModelSelect34Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table, TL5, TL5Table, TL6,
            TL6Table, TL7, TL7Table> where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TJ3Table : IExprTableSource, new()
        where TL4Table : IExprTableSource, new()
        where TL5Table : IExprTableSource, new()
        where TL6Table : IExprTableSource, new()
        where TL7Table : IExprTableSource, new()
    {
        internal readonly ModelSelect33Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table, TL5,
            TL5Table, TL6, TL6Table> Source;

        internal readonly ISqModelReader<TL7, TL7Table> Reader7;

        internal readonly
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean> On7;

        internal ModelSelect34Left(
            ModelSelect33Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TL4, TL4Table, TL5, TL5Table, TL6,
                TL6Table> source, ISqModelReader<TL7, TL7Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean> on)
        {
            this.Source = source;
            this.Reader7 = reader;
            this.On7 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TJ3, TJ3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TL4, TL4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        internal ISqModelReader<TL5, TL5Table> Reader5 => this.Source.Reader5;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table>, ExprBoolean>
            On5 => this.Source.On5;

        internal ISqModelReader<TL6, TL6Table> Reader6 => this.Source.Reader6;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table>,
            ExprBoolean> On6 => this.Source.On6;

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprOrderBy>? order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TL4?, TL5?, TL6?, TL7?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprOrderBy> order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TL4?, TL5?, TL6?, TL7?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(
            Func<ModelRowData<T, TJ1, TJ2, TJ3, TL4?, TL5?, TL6?, TL7?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table, TL7Table>
                , ExprOrderBy>? order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TL4Table, TL5Table, TL6Table,
                    TL7Table>(new TTable(),
                    new TJ1Table(),
                    new TJ2Table(),
                    new TJ3Table(),
                    new TL4Table(),
                    new TL5Table(),
                    new TL6Table(),
                    new TL7Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5),
                    this.Reader6.GetColumns(tablesContext.JoinedTable6),
                    this.Reader7.GetColumns(tablesContext.JoinedTable7)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .InnerJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .LeftJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .LeftJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .LeftJoin(tablesContext.JoinedTable6, this.On6(tablesContext))
                .LeftJoin(tablesContext.JoinedTable7, this.On7(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TJ3, TL4?, TL5?, TL6?, TL7?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 4)
                        ? default
                        : this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 5)
                        ? default
                        : this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 6)
                        ? default
                        : this.Reader6.ReadOrdinal(r, tablesContext.JoinedTable6, offsets[6]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 7)
                        ? default
                        : this.Reader7.ReadOrdinal(r, tablesContext.JoinedTable7, offsets[7]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class
        ModelSelect43Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TL5, TL5Table, TL6,
            TL6Table, TL7, TL7Table> where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TJ3Table : IExprTableSource, new()
        where TJ4Table : IExprTableSource, new()
        where TL5Table : IExprTableSource, new()
        where TL6Table : IExprTableSource, new()
        where TL7Table : IExprTableSource, new()
    {
        internal readonly ModelSelect42Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TL5,
            TL5Table, TL6, TL6Table> Source;

        internal readonly ISqModelReader<TL7, TL7Table> Reader7;

        internal readonly
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean> On7;

        internal ModelSelect43Left(
            ModelSelect42Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TL5, TL5Table, TL6,
                TL6Table> source, ISqModelReader<TL7, TL7Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean> on)
        {
            this.Source = source;
            this.Reader7 = reader;
            this.On7 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TJ3, TJ3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TJ4, TJ4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        internal ISqModelReader<TL5, TL5Table> Reader5 => this.Source.Reader5;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table>, ExprBoolean>
            On5 => this.Source.On5;

        internal ISqModelReader<TL6, TL6Table> Reader6 => this.Source.Reader6;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table>,
            ExprBoolean> On6 => this.Source.On6;

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table, TL7Table>
                , ExprOrderBy>? order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TL5?, TL6?, TL7?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table, TL7Table>
                , ExprOrderBy> order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TL5?, TL6?, TL7?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(
            Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TL5?, TL6?, TL7?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table, TL7Table>
                , ExprOrderBy>? order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TL5Table, TL6Table,
                    TL7Table>(new TTable(),
                    new TJ1Table(),
                    new TJ2Table(),
                    new TJ3Table(),
                    new TJ4Table(),
                    new TL5Table(),
                    new TL6Table(),
                    new TL7Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5),
                    this.Reader6.GetColumns(tablesContext.JoinedTable6),
                    this.Reader7.GetColumns(tablesContext.JoinedTable7)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .InnerJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .InnerJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .LeftJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .LeftJoin(tablesContext.JoinedTable6, this.On6(tablesContext))
                .LeftJoin(tablesContext.JoinedTable7, this.On7(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TL5?, TL6?, TL7?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 5)
                        ? default
                        : this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 6)
                        ? default
                        : this.Reader6.ReadOrdinal(r, tablesContext.JoinedTable6, offsets[6]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 7)
                        ? default
                        : this.Reader7.ReadOrdinal(r, tablesContext.JoinedTable7, offsets[7]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class
        ModelSelect52Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table, TL6,
            TL6Table, TL7, TL7Table> where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TJ3Table : IExprTableSource, new()
        where TJ4Table : IExprTableSource, new()
        where TJ5Table : IExprTableSource, new()
        where TL6Table : IExprTableSource, new()
        where TL7Table : IExprTableSource, new()
    {
        internal readonly ModelSelect51Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5,
            TJ5Table, TL6, TL6Table> Source;

        internal readonly ISqModelReader<TL7, TL7Table> Reader7;

        internal readonly
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table, TL7Table>
                , ExprBoolean> On7;

        internal ModelSelect52Left(
            ModelSelect51Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table, TL6,
                TL6Table> source, ISqModelReader<TL7, TL7Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table, TL7Table>
                , ExprBoolean> on)
        {
            this.Source = source;
            this.Reader7 = reader;
            this.On7 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TJ3, TJ3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TJ4, TJ4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        internal ISqModelReader<TJ5, TJ5Table> Reader5 => this.Source.Reader5;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table>, ExprBoolean>
            On5 => this.Source.On5;

        internal ISqModelReader<TL6, TL6Table> Reader6 => this.Source.Reader6;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table>,
            ExprBoolean> On6 => this.Source.On6;

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table, TL7Table>
                , ExprOrderBy>? order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TL6?, TL7?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table, TL7Table>
                , ExprOrderBy> order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TL6?, TL7?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(
            Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TL6?, TL7?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table, TL7Table>
                , ExprOrderBy>? order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TL6Table,
                    TL7Table>(new TTable(),
                    new TJ1Table(),
                    new TJ2Table(),
                    new TJ3Table(),
                    new TJ4Table(),
                    new TJ5Table(),
                    new TL6Table(),
                    new TL7Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5),
                    this.Reader6.GetColumns(tablesContext.JoinedTable6),
                    this.Reader7.GetColumns(tablesContext.JoinedTable7)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .InnerJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .InnerJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .InnerJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .LeftJoin(tablesContext.JoinedTable6, this.On6(tablesContext))
                .LeftJoin(tablesContext.JoinedTable7, this.On7(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TL6?, TL7?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 6)
                        ? default
                        : this.Reader6.ReadOrdinal(r, tablesContext.JoinedTable6, offsets[6]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 7)
                        ? default
                        : this.Reader7.ReadOrdinal(r, tablesContext.JoinedTable7, offsets[7]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class
        ModelSelect61Left<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table, TJ6,
            TJ6Table, TL7, TL7Table> where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TJ3Table : IExprTableSource, new()
        where TJ4Table : IExprTableSource, new()
        where TJ5Table : IExprTableSource, new()
        where TJ6Table : IExprTableSource, new()
        where TL7Table : IExprTableSource, new()
    {
        internal readonly ModelSelect60<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5,
            TJ5Table, TJ6, TJ6Table> Source;

        internal readonly ISqModelReader<TL7, TL7Table> Reader7;

        internal readonly
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table, TL7Table>
                , ExprBoolean> On7;

        internal ModelSelect61Left(
            ModelSelect60<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table, TJ6,
                TJ6Table> source, ISqModelReader<TL7, TL7Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table, TL7Table>
                , ExprBoolean> on)
        {
            this.Source = source;
            this.Reader7 = reader;
            this.On7 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TJ3, TJ3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TJ4, TJ4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        internal ISqModelReader<TJ5, TJ5Table> Reader5 => this.Source.Reader5;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table>, ExprBoolean>
            On5 => this.Source.On5;

        internal ISqModelReader<TJ6, TJ6Table> Reader6 => this.Source.Reader6;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table>,
            ExprBoolean> On6 => this.Source.On6;

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table, TL7Table>
                , ExprOrderBy>? order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TJ6, TL7?>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table, TL7Table>
                , ExprOrderBy> order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TJ6, TL7?>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(
            Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TJ6, TL7?>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table, TL7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table, TL7Table>
                , ExprOrderBy>? order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table,
                    TL7Table>(new TTable(),
                    new TJ1Table(),
                    new TJ2Table(),
                    new TJ3Table(),
                    new TJ4Table(),
                    new TJ5Table(),
                    new TJ6Table(),
                    new TL7Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5),
                    this.Reader6.GetColumns(tablesContext.JoinedTable6),
                    this.Reader7.GetColumns(tablesContext.JoinedTable7)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .InnerJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .InnerJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .InnerJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .InnerJoin(tablesContext.JoinedTable6, this.On6(tablesContext))
                .LeftJoin(tablesContext.JoinedTable7, this.On7(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TJ6, TL7?>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]),
                    this.Reader6.ReadOrdinal(r, tablesContext.JoinedTable6, offsets[6]),
                    ModelQueryBuilderHelper.IsAllNull(r, offsets, 7)
                        ? default
                        : this.Reader7.ReadOrdinal(r, tablesContext.JoinedTable7, offsets[7]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }

    public class
        ModelSelect70<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table, TJ6,
            TJ6Table, TJ7, TJ7Table> where TTable : IExprTableSource, new()
        where TJ1Table : IExprTableSource, new()
        where TJ2Table : IExprTableSource, new()
        where TJ3Table : IExprTableSource, new()
        where TJ4Table : IExprTableSource, new()
        where TJ5Table : IExprTableSource, new()
        where TJ6Table : IExprTableSource, new()
        where TJ7Table : IExprTableSource, new()
    {
        internal readonly ModelSelect60<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5,
            TJ5Table, TJ6, TJ6Table> Source;

        internal readonly ISqModelReader<TJ7, TJ7Table> Reader7;

        internal readonly
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table, TJ7Table>
                , ExprBoolean> On7;

        internal ModelSelect70(
            ModelSelect60<T, TTable, TJ1, TJ1Table, TJ2, TJ2Table, TJ3, TJ3Table, TJ4, TJ4Table, TJ5, TJ5Table, TJ6,
                TJ6Table> source, ISqModelReader<TJ7, TJ7Table> reader,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table, TJ7Table>
                , ExprBoolean> on)
        {
            this.Source = source;
            this.Reader7 = reader;
            this.On7 = on;
        }

        internal ISqModelReader<T, TTable> Reader => this.Source.Reader;
        internal ISqModelReader<TJ1, TJ1Table> Reader1 => this.Source.Reader1;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table>, ExprBoolean> On1 => this.Source.On1;
        internal ISqModelReader<TJ2, TJ2Table> Reader2 => this.Source.Reader2;
        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table>, ExprBoolean> On2 => this.Source.On2;
        internal ISqModelReader<TJ3, TJ3Table> Reader3 => this.Source.Reader3;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table>, ExprBoolean> On3 =>
            this.Source.On3;

        internal ISqModelReader<TJ4, TJ4Table> Reader4 => this.Source.Reader4;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table>, ExprBoolean> On4 =>
            this.Source.On4;

        internal ISqModelReader<TJ5, TJ5Table> Reader5 => this.Source.Reader5;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table>, ExprBoolean>
            On5 => this.Source.On5;

        internal ISqModelReader<TJ6, TJ6Table> Reader6 => this.Source.Reader6;

        internal Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table>,
            ExprBoolean> On6 => this.Source.On6;

        public ModelRequestData<TRes> Get<TRes>(
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table, TJ7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table, TJ7Table>
                , ExprOrderBy>? order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TJ6, TJ7>, TRes> mapper)
        {
            return GenericGet(mapper, filter, order, null);
        }

        public ModelRangeRequestData<TRes> Find<TRes>(int offset, int pageSize,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table, TJ7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table, TJ7Table>
                , ExprOrderBy> order, Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TJ6, TJ7>, TRes> mapper)
        {
            var r = GenericGet(mapper, filter, order, new KeyValuePair<int, int>(offset, pageSize));
            return new ModelRangeRequestData<TRes>((ExprSelectOffsetFetch)r.Expr, r.Mapper);
        }

        private ModelRequestData<TRes> GenericGet<TRes>(
            Func<ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TJ6, TJ7>, TRes> mapper,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table, TJ7Table>
                , ExprBoolean>? filter,
            Func<IModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table, TJ7Table>
                , ExprOrderBy>? order, KeyValuePair<int, int>? offsetFetch)
        {
            var tablesContext =
                new ModelSelectTablesContext<TTable, TJ1Table, TJ2Table, TJ3Table, TJ4Table, TJ5Table, TJ6Table,
                    TJ7Table>(new TTable(),
                    new TJ1Table(),
                    new TJ2Table(),
                    new TJ3Table(),
                    new TJ4Table(),
                    new TJ5Table(),
                    new TJ6Table(),
                    new TJ7Table());
            IExprQuery query;
            var filtered = SqQueryBuilder.Select(ModelQueryBuilderHelper.BuildColumns(out var offsets,
                    this.Reader.GetColumns(tablesContext.Table),
                    this.Reader1.GetColumns(tablesContext.JoinedTable1),
                    this.Reader2.GetColumns(tablesContext.JoinedTable2),
                    this.Reader3.GetColumns(tablesContext.JoinedTable3),
                    this.Reader4.GetColumns(tablesContext.JoinedTable4),
                    this.Reader5.GetColumns(tablesContext.JoinedTable5),
                    this.Reader6.GetColumns(tablesContext.JoinedTable6),
                    this.Reader7.GetColumns(tablesContext.JoinedTable7)))
                .From(tablesContext.Table)
                .InnerJoin(tablesContext.JoinedTable1, this.On1(tablesContext))
                .InnerJoin(tablesContext.JoinedTable2, this.On2(tablesContext))
                .InnerJoin(tablesContext.JoinedTable3, this.On3(tablesContext))
                .InnerJoin(tablesContext.JoinedTable4, this.On4(tablesContext))
                .InnerJoin(tablesContext.JoinedTable5, this.On5(tablesContext))
                .InnerJoin(tablesContext.JoinedTable6, this.On6(tablesContext))
                .InnerJoin(tablesContext.JoinedTable7, this.On7(tablesContext))
                .Where(filter?.Invoke(tablesContext));

            if (order != null)
            {
                var ordered = filtered.OrderBy(order(tablesContext));

                if (offsetFetch != null)
                {
                    query = ordered.OffsetFetch(offsetFetch.Value.Key, offsetFetch.Value.Value).Done();
                }
                else
                {
                    query = ordered.Done();
                }
            }
            else
            {
                query = filtered.Done();
            }

            TRes ResultMapper(ISqDataRecordReader r)
            {
                var tuple = new ModelRowData<T, TJ1, TJ2, TJ3, TJ4, TJ5, TJ6, TJ7>(
                    this.Reader.ReadOrdinal(r, tablesContext.Table, 0),
                    this.Reader1.ReadOrdinal(r, tablesContext.JoinedTable1, offsets[1]),
                    this.Reader2.ReadOrdinal(r, tablesContext.JoinedTable2, offsets[2]),
                    this.Reader3.ReadOrdinal(r, tablesContext.JoinedTable3, offsets[3]),
                    this.Reader4.ReadOrdinal(r, tablesContext.JoinedTable4, offsets[4]),
                    this.Reader5.ReadOrdinal(r, tablesContext.JoinedTable5, offsets[5]),
                    this.Reader6.ReadOrdinal(r, tablesContext.JoinedTable6, offsets[6]),
                    this.Reader7.ReadOrdinal(r, tablesContext.JoinedTable7, offsets[7]));
                return mapper(tuple);
            }

            return new ModelRequestData<TRes>(query, ResultMapper);
        }
    }
    //CodeGenEnd
}