using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.IntTest.Tables;
using SqExpress.Syntax.Names;
using System.Collections.Generic;

namespace SqExpress.IntTest.Tables.Models
{
    public class Customer
    {
        public Customer(int customerId, int? userId, int? companyId)
        {
            this.CustomerId = customerId;
            this.UserId = userId;
            this.CompanyId = companyId;
        }

        public static Customer Read(ISqDataRecordReader record, TableItCustomer table)
        {
            return new Customer(customerId: table.CustomerId.Read(record), userId: table.UserId.Read(record), companyId: table.CompanyId.Read(record));
        }

        public static Customer ReadOrdinal(ISqDataRecordReader record, TableItCustomer table, int offset)
        {
            return new Customer(customerId: table.CustomerId.Read(record, offset), userId: table.UserId.Read(record, offset + 1), companyId: table.CompanyId.Read(record, offset + 2));
        }

        public int CustomerId { get; }

        public int? UserId { get; }

        public int? CompanyId { get; }

        public Customer WithCustomerId(int customerId)
        {
            return new Customer(customerId: customerId, userId: this.UserId, companyId: this.CompanyId);
        }

        public Customer WithUserId(int? userId)
        {
            return new Customer(customerId: this.CustomerId, userId: userId, companyId: this.CompanyId);
        }

        public Customer WithCompanyId(int? companyId)
        {
            return new Customer(customerId: this.CustomerId, userId: this.UserId, companyId: companyId);
        }

        public static TableColumn[] GetColumns(TableItCustomer table)
        {
            return new TableColumn[]{table.CustomerId, table.UserId, table.CompanyId};
        }

        public static IRecordSetterNext GetMapping(IDataMapSetter<TableItCustomer, Customer> s)
        {
            return s.Set(s.Target.UserId, s.Source.UserId).Set(s.Target.CompanyId, s.Source.CompanyId);
        }

        public static IRecordSetterNext GetUpdateKeyMapping(IDataMapSetter<TableItCustomer, Customer> s)
        {
            return s.Set(s.Target.CustomerId, s.Source.CustomerId);
        }

        public static IRecordSetterNext GetUpdateMapping(IDataMapSetter<TableItCustomer, Customer> s)
        {
            return s.Set(s.Target.UserId, s.Source.UserId).Set(s.Target.CompanyId, s.Source.CompanyId);
        }

        public static ISqModelReader<Customer, TableItCustomer> GetReader()
        {
            return CustomerReader.Instance;
        }

        private class CustomerReader : ISqModelReader<Customer, TableItCustomer>
        {
            public static CustomerReader Instance { get; } = new CustomerReader();
            IReadOnlyList<ExprColumn> ISqModelReader<Customer, TableItCustomer>.GetColumns(TableItCustomer table)
            {
                return Customer.GetColumns(table);
            }

            Customer ISqModelReader<Customer, TableItCustomer>.Read(ISqDataRecordReader record, TableItCustomer table)
            {
                return Customer.Read(record, table);
            }

            Customer ISqModelReader<Customer, TableItCustomer>.ReadOrdinal(ISqDataRecordReader record, TableItCustomer table, int offset)
            {
                return Customer.ReadOrdinal(record, table, offset);
            }
        }

        public static ISqModelUpdaterKey<Customer, TableItCustomer> GetUpdater()
        {
            return CustomerUpdater.Instance;
        }

        private class CustomerUpdater : ISqModelUpdaterKey<Customer, TableItCustomer>
        {
            public static CustomerUpdater Instance { get; } = new CustomerUpdater();
            IRecordSetterNext ISqModelUpdater<Customer, TableItCustomer>.GetMapping(IDataMapSetter<TableItCustomer, Customer> dataMapSetter)
            {
                return Customer.GetMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<Customer, TableItCustomer>.GetUpdateKeyMapping(IDataMapSetter<TableItCustomer, Customer> dataMapSetter)
            {
                return Customer.GetUpdateKeyMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<Customer, TableItCustomer>.GetUpdateMapping(IDataMapSetter<TableItCustomer, Customer> dataMapSetter)
            {
                return Customer.GetUpdateMapping(dataMapSetter);
            }
        }
    }
}