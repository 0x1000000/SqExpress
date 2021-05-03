using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.GetStarted;
using SqExpress.Syntax.Names;

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

        public int CustomerId { get; }

        public short Type { get; }
    }
}