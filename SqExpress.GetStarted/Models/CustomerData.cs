using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.GetStarted;
using SqExpress.Syntax.Names;
using System.Collections.Generic;

namespace SqExpress.GetStarted.Models
{
    public class CustomerData
    {
        public CustomerData(int id, short customerType, string name)
        {
            this.Id = id;
            this.CustomerType = customerType;
            this.Name = name;
        }

        public static CustomerData Read(ISqDataRecordReader record, DerivedTableCustomer table)
        {
            return new CustomerData(id: table.CustomerId.Read(record), customerType: table.Type.Read(record), name: table.Name.Read(record));
        }

        public static CustomerData ReadOrdinal(ISqDataRecordReader record, DerivedTableCustomer table, int offset)
        {
            return new CustomerData(id: table.CustomerId.Read(record, offset), customerType: table.Type.Read(record, offset + 1), name: table.Name.Read(record, offset + 2));
        }

        public int Id { get; }

        public short CustomerType { get; }

        public string Name { get; }

        public CustomerData WithId(int id)
        {
            return new CustomerData(id: id, customerType: this.CustomerType, name: this.Name);
        }

        public CustomerData WithCustomerType(short customerType)
        {
            return new CustomerData(id: this.Id, customerType: customerType, name: this.Name);
        }

        public CustomerData WithName(string name)
        {
            return new CustomerData(id: this.Id, customerType: this.CustomerType, name: name);
        }

        public static ExprColumn[] GetColumns(DerivedTableCustomer table)
        {
            return new ExprColumn[]{table.CustomerId, table.Type, table.Name};
        }

        public static ISqModelReader<CustomerData, DerivedTableCustomer> GetReader()
        {
            return CustomerDataReader.Instance;
        }

        private class CustomerDataReader : ISqModelReader<CustomerData, DerivedTableCustomer>
        {
            public static CustomerDataReader Instance { get; } = new CustomerDataReader();
            IReadOnlyList<ExprColumn> ISqModelReader<CustomerData, DerivedTableCustomer>.GetColumns(DerivedTableCustomer table)
            {
                return CustomerData.GetColumns(table);
            }

            CustomerData ISqModelReader<CustomerData, DerivedTableCustomer>.Read(ISqDataRecordReader record, DerivedTableCustomer table)
            {
                return CustomerData.Read(record, table);
            }

            CustomerData ISqModelReader<CustomerData, DerivedTableCustomer>.ReadOrdinal(ISqDataRecordReader record, DerivedTableCustomer table, int offset)
            {
                return CustomerData.ReadOrdinal(record, table, offset);
            }
        }
    }
}