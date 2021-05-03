using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.IntTest.Tables.Derived;
using SqExpress.Syntax.Names;

namespace SqExpress.IntTest.Tables.Models
{
    public class CustomerNameData
    {
        public CustomerNameData(int id, short typeId, string name)
        {
            this.Id = id;
            this.TypeId = typeId;
            this.Name = name;
        }

        public static CustomerNameData Read(ISqDataRecordReader record, CustomerName table)
        {
            return new CustomerNameData(id: table.CustomerId.Read(record), typeId: table.CustomerTypeId.Read(record), name: table.Name.Read(record));
        }

        public int Id { get; }

        public short TypeId { get; }

        public string Name { get; }

        public CustomerNameData WithId(int id)
        {
            return new CustomerNameData(id: id, typeId: this.TypeId, name: this.Name);
        }

        public CustomerNameData WithTypeId(short typeId)
        {
            return new CustomerNameData(id: this.Id, typeId: typeId, name: this.Name);
        }

        public CustomerNameData WithName(string name)
        {
            return new CustomerNameData(id: this.Id, typeId: this.TypeId, name: name);
        }

        public static ExprColumn[] GetColumns(CustomerName table)
        {
            return new ExprColumn[]{table.CustomerId, table.CustomerTypeId, table.Name};
        }

        public static ISqModelDerivedReaderReader<CustomerNameData, CustomerName> GetReader()
        {
            return CustomerNameDataReader.Instance;
        }

        private class CustomerNameDataReader : ISqModelDerivedReaderReader<CustomerNameData, CustomerName>
        {
            public static CustomerNameDataReader Instance { get; } = new CustomerNameDataReader();
            ExprColumn[] ISqModelDerivedReaderReader<CustomerNameData, CustomerName>.GetColumns(CustomerName table)
            {
                return CustomerNameData.GetColumns(table);
            }

            CustomerNameData ISqModelDerivedReaderReader<CustomerNameData, CustomerName>.Read(ISqDataRecordReader record, CustomerName table)
            {
                return CustomerNameData.Read(record, table);
            }
        }
    }
}