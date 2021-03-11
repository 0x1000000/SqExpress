using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.GetStarted;

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

        public int Id { get; }

        public string Name { get; }

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

        public CompanyName WithId(int id)
        {
            return new CompanyName(id: id, name: this.Name);
        }

        public CompanyName WithName(string name)
        {
            return new CompanyName(id: this.Id, name: name);
        }
    }
}