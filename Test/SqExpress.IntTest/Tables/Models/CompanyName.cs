using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.IntTest.Tables;
using SqExpress.Syntax.Names;
using System.Collections.Generic;

namespace SqExpress.IntTest.Tables.Models
{
    public class CompanyName
    {
        public CompanyName(int id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        public static CompanyName Read(ISqDataRecordReader record, TableItCompany table)
        {
            return new CompanyName(id: table.CompanyId.Read(record), name: table.CompanyName.Read(record));
        }

        public static CompanyName ReadOrdinal(ISqDataRecordReader record, TableItCompany table, int offset)
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

        public static TableColumn[] GetColumns(TableItCompany table)
        {
            return new TableColumn[]{table.CompanyId, table.CompanyName};
        }

        public static IRecordSetterNext GetMapping(IDataMapSetter<TableItCompany, CompanyName> s)
        {
            return s.Set(s.Target.CompanyName, s.Source.Name);
        }

        public static IRecordSetterNext GetUpdateKeyMapping(IDataMapSetter<TableItCompany, CompanyName> s)
        {
            return s.Set(s.Target.CompanyId, s.Source.Id);
        }

        public static IRecordSetterNext GetUpdateMapping(IDataMapSetter<TableItCompany, CompanyName> s)
        {
            return s.Set(s.Target.CompanyName, s.Source.Name);
        }

        public static ISqModelReader<CompanyName, TableItCompany> GetReader()
        {
            return CompanyNameReader.Instance;
        }

        private class CompanyNameReader : ISqModelReader<CompanyName, TableItCompany>
        {
            public static CompanyNameReader Instance { get; } = new CompanyNameReader();
            IReadOnlyList<ExprColumn> ISqModelReader<CompanyName, TableItCompany>.GetColumns(TableItCompany table)
            {
                return CompanyName.GetColumns(table);
            }

            CompanyName ISqModelReader<CompanyName, TableItCompany>.Read(ISqDataRecordReader record, TableItCompany table)
            {
                return CompanyName.Read(record, table);
            }

            CompanyName ISqModelReader<CompanyName, TableItCompany>.ReadOrdinal(ISqDataRecordReader record, TableItCompany table, int offset)
            {
                return CompanyName.ReadOrdinal(record, table, offset);
            }
        }

        public static ISqModelUpdaterKey<CompanyName, TableItCompany> GetUpdater()
        {
            return CompanyNameUpdater.Instance;
        }

        private class CompanyNameUpdater : ISqModelUpdaterKey<CompanyName, TableItCompany>
        {
            public static CompanyNameUpdater Instance { get; } = new CompanyNameUpdater();
            IRecordSetterNext ISqModelUpdater<CompanyName, TableItCompany>.GetMapping(IDataMapSetter<TableItCompany, CompanyName> dataMapSetter)
            {
                return CompanyName.GetMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<CompanyName, TableItCompany>.GetUpdateKeyMapping(IDataMapSetter<TableItCompany, CompanyName> dataMapSetter)
            {
                return CompanyName.GetUpdateKeyMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<CompanyName, TableItCompany>.GetUpdateMapping(IDataMapSetter<TableItCompany, CompanyName> dataMapSetter)
            {
                return CompanyName.GetUpdateMapping(dataMapSetter);
            }
        }
    }
}