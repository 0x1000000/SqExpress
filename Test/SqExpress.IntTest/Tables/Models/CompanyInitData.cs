using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.IntTest.Tables;

namespace SqExpress.IntTest.Tables.Models
{
    public class CompanyInitData
    {
        public CompanyInitData(int id, Guid externalId, string name)
        {
            this.Id = id;
            this.ExternalId = externalId;
            this.Name = name;
        }

        public static CompanyInitData Read(ISqDataRecordReader record, TableItCompany table)
        {
            return new CompanyInitData(id: table.CompanyId.Read(record), externalId: table.ExternalId.Read(record), name: table.CompanyName.Read(record));
        }

        public int Id { get; }

        public Guid ExternalId { get; }

        public string Name { get; }

        public static TableColumn[] GetColumns(TableItCompany table)
        {
            return new TableColumn[]{table.CompanyId, table.ExternalId, table.CompanyName};
        }

        public static IRecordSetterNext GetMapping(IDataMapSetter<TableItCompany, CompanyInitData> s)
        {
            return s.Set(s.Target.ExternalId, s.Source.ExternalId).Set(s.Target.CompanyName, s.Source.Name);
        }

        public static IRecordSetterNext GetUpdateKeyMapping(IDataMapSetter<TableItCompany, CompanyInitData> s)
        {
            return s.Set(s.Target.CompanyId, s.Source.Id);
        }

        public static IRecordSetterNext GetUpdateMapping(IDataMapSetter<TableItCompany, CompanyInitData> s)
        {
            return s.Set(s.Target.ExternalId, s.Source.ExternalId).Set(s.Target.CompanyName, s.Source.Name);
        }

        public CompanyInitData WithId(int id)
        {
            return new CompanyInitData(id: id, externalId: this.ExternalId, name: this.Name);
        }

        public CompanyInitData WithExternalId(Guid externalId)
        {
            return new CompanyInitData(id: this.Id, externalId: externalId, name: this.Name);
        }

        public CompanyInitData WithName(string name)
        {
            return new CompanyInitData(id: this.Id, externalId: this.ExternalId, name: name);
        }
    }
}