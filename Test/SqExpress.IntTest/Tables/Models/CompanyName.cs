using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.IntTest.Tables;

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

        public int Id { get; }

        public string Name { get; }

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
    }
}