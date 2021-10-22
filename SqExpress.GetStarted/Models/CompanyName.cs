using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.GetStarted;
using SqExpress.Syntax.Names;
using System.Collections.Generic;

namespace SqExpress.GetStarted.Models
{
    public class CompanyName
    {
        public CompanyName(int id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        public static CompanyName Read(ISqDataRecordReader record, TableCompany table)
        {
            return new CompanyName(id: table.CompanyId.Read(record), name: table.CompanyName.Read(record));
        }

        public static CompanyName ReadOrdinal(ISqDataRecordReader record, TableCompany table, int offset)
        {
            return new CompanyName(id: table.CompanyId.Read(record, offset), name: table.CompanyName.Read(record, offset + 1));
        }

        public int Id { get; }

        public string Name { get; }

        public CompanyName WithId(int id)
        {
            return new CompanyName(id: id, name: this.Name);
        }

        public CompanyName WithName(string name)
        {
            return new CompanyName(id: this.Id, name: name);
        }

        public static TableColumn[] GetColumns(TableCompany table)
        {
            return new TableColumn[]{table.CompanyId, table.CompanyName};
        }

        public static IRecordSetterNext GetMapping(IDataMapSetter<TableCompany, CompanyName> s)
        {
            return s.Set(s.Target.CompanyName, s.Source.Name);
        }

        public static IRecordSetterNext GetUpdateKeyMapping(IDataMapSetter<TableCompany, CompanyName> s)
        {
            return s.Set(s.Target.CompanyId, s.Source.Id);
        }

        public static IRecordSetterNext GetUpdateMapping(IDataMapSetter<TableCompany, CompanyName> s)
        {
            return s.Set(s.Target.CompanyName, s.Source.Name);
        }

        public static ISqModelReader<CompanyName, TableCompany> GetReader()
        {
            return CompanyNameReader.Instance;
        }

        private class CompanyNameReader : ISqModelReader<CompanyName, TableCompany>
        {
            public static CompanyNameReader Instance { get; } = new CompanyNameReader();
            IReadOnlyList<ExprColumn> ISqModelReader<CompanyName, TableCompany>.GetColumns(TableCompany table)
            {
                return CompanyName.GetColumns(table);
            }

            CompanyName ISqModelReader<CompanyName, TableCompany>.Read(ISqDataRecordReader record, TableCompany table)
            {
                return CompanyName.Read(record, table);
            }

            CompanyName ISqModelReader<CompanyName, TableCompany>.ReadOrdinal(ISqDataRecordReader record, TableCompany table, int offset)
            {
                return CompanyName.ReadOrdinal(record, table, offset);
            }
        }

        public static ISqModelUpdaterKey<CompanyName, TableCompany> GetUpdater()
        {
            return CompanyNameUpdater.Instance;
        }

        private class CompanyNameUpdater : ISqModelUpdaterKey<CompanyName, TableCompany>
        {
            public static CompanyNameUpdater Instance { get; } = new CompanyNameUpdater();
            IRecordSetterNext ISqModelUpdater<CompanyName, TableCompany>.GetMapping(IDataMapSetter<TableCompany, CompanyName> dataMapSetter)
            {
                return CompanyName.GetMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<CompanyName, TableCompany>.GetUpdateKeyMapping(IDataMapSetter<TableCompany, CompanyName> dataMapSetter)
            {
                return CompanyName.GetUpdateKeyMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<CompanyName, TableCompany>.GetUpdateMapping(IDataMapSetter<TableCompany, CompanyName> dataMapSetter)
            {
                return CompanyName.GetUpdateMapping(dataMapSetter);
            }
        }
    }
}