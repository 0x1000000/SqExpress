using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.IntTest.Tables;
using SqExpress.Syntax.Names;
using System.Collections.Generic;

namespace SqExpress.IntTest.Tables.Models
{
    public class OrderDateCreated
    {
        public OrderDateCreated(int orderId, DateTime dateCreated)
        {
            this.OrderId = orderId;
            this.DateCreated = dateCreated;
        }

        public static OrderDateCreated Read(ISqDataRecordReader record, TableItOrder table)
        {
            return new OrderDateCreated(orderId: table.OrderId.Read(record), dateCreated: table.DateCreated.Read(record));
        }

        public static OrderDateCreated ReadOrdinal(ISqDataRecordReader record, TableItOrder table, int offset)
        {
            return new OrderDateCreated(orderId: table.OrderId.Read(record, offset), dateCreated: table.DateCreated.Read(record, offset + 1));
        }

        public int OrderId { get; }

        public DateTime DateCreated { get; }

        public OrderDateCreated WithOrderId(int orderId)
        {
            return new OrderDateCreated(orderId: orderId, dateCreated: this.DateCreated);
        }

        public OrderDateCreated WithDateCreated(DateTime dateCreated)
        {
            return new OrderDateCreated(orderId: this.OrderId, dateCreated: dateCreated);
        }

        public static TableColumn[] GetColumns(TableItOrder table)
        {
            return new TableColumn[]{table.OrderId, table.DateCreated};
        }

        public static IRecordSetterNext GetMapping(IDataMapSetter<TableItOrder, OrderDateCreated> s)
        {
            return s.Set(s.Target.DateCreated, s.Source.DateCreated);
        }

        public static IRecordSetterNext GetUpdateKeyMapping(IDataMapSetter<TableItOrder, OrderDateCreated> s)
        {
            return s.Set(s.Target.OrderId, s.Source.OrderId);
        }

        public static IRecordSetterNext GetUpdateMapping(IDataMapSetter<TableItOrder, OrderDateCreated> s)
        {
            return s.Set(s.Target.DateCreated, s.Source.DateCreated);
        }

        public static ISqModelReader<OrderDateCreated, TableItOrder> GetReader()
        {
            return OrderDateCreatedReader.Instance;
        }

        private class OrderDateCreatedReader : ISqModelReader<OrderDateCreated, TableItOrder>
        {
            public static OrderDateCreatedReader Instance { get; } = new OrderDateCreatedReader();
            IReadOnlyList<ExprColumn> ISqModelReader<OrderDateCreated, TableItOrder>.GetColumns(TableItOrder table)
            {
                return OrderDateCreated.GetColumns(table);
            }

            OrderDateCreated ISqModelReader<OrderDateCreated, TableItOrder>.Read(ISqDataRecordReader record, TableItOrder table)
            {
                return OrderDateCreated.Read(record, table);
            }

            OrderDateCreated ISqModelReader<OrderDateCreated, TableItOrder>.ReadOrdinal(ISqDataRecordReader record, TableItOrder table, int offset)
            {
                return OrderDateCreated.ReadOrdinal(record, table, offset);
            }
        }

        public static ISqModelUpdaterKey<OrderDateCreated, TableItOrder> GetUpdater()
        {
            return OrderDateCreatedUpdater.Instance;
        }

        private class OrderDateCreatedUpdater : ISqModelUpdaterKey<OrderDateCreated, TableItOrder>
        {
            public static OrderDateCreatedUpdater Instance { get; } = new OrderDateCreatedUpdater();
            IRecordSetterNext ISqModelUpdater<OrderDateCreated, TableItOrder>.GetMapping(IDataMapSetter<TableItOrder, OrderDateCreated> dataMapSetter)
            {
                return OrderDateCreated.GetMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<OrderDateCreated, TableItOrder>.GetUpdateKeyMapping(IDataMapSetter<TableItOrder, OrderDateCreated> dataMapSetter)
            {
                return OrderDateCreated.GetUpdateKeyMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<OrderDateCreated, TableItOrder>.GetUpdateMapping(IDataMapSetter<TableItOrder, OrderDateCreated> dataMapSetter)
            {
                return OrderDateCreated.GetUpdateMapping(dataMapSetter);
            }
        }
    }
}