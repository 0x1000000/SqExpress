using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.IntTest.Tables;
using SqExpress.Syntax.Names;
using System.Collections.Generic;

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

        public static CompanyInitData ReadOrdinal(ISqDataRecordReader record, TableItCompany table, int offset)
        {
            return new CompanyInitData(id: table.CompanyId.Read(record, offset), externalId: table.ExternalId.Read(record, offset + 1), name: table.CompanyName.Read(record, offset + 2));
        }

        public int Id { get; }

        public Guid ExternalId { get; }

        public string Name { get; }

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

        public static ISqModelReader<CompanyInitData, TableItCompany> GetReader()
        {
            return CompanyInitDataReader.Instance;
        }

        private class CompanyInitDataReader : ISqModelReader<CompanyInitData, TableItCompany>
        {
            public static CompanyInitDataReader Instance { get; } = new CompanyInitDataReader();
            IReadOnlyList<ExprColumn> ISqModelReader<CompanyInitData, TableItCompany>.GetColumns(TableItCompany table)
            {
                return CompanyInitData.GetColumns(table);
            }

            CompanyInitData ISqModelReader<CompanyInitData, TableItCompany>.Read(ISqDataRecordReader record, TableItCompany table)
            {
                return CompanyInitData.Read(record, table);
            }

            CompanyInitData ISqModelReader<CompanyInitData, TableItCompany>.ReadOrdinal(ISqDataRecordReader record, TableItCompany table, int offset)
            {
                return CompanyInitData.ReadOrdinal(record, table, offset);
            }
        }

        public static ISqModelUpdaterKey<CompanyInitData, TableItCompany> GetUpdater()
        {
            return CompanyInitDataUpdater.Instance;
        }

        private class CompanyInitDataUpdater : ISqModelUpdaterKey<CompanyInitData, TableItCompany>
        {
            public static CompanyInitDataUpdater Instance { get; } = new CompanyInitDataUpdater();
            IRecordSetterNext ISqModelUpdater<CompanyInitData, TableItCompany>.GetMapping(IDataMapSetter<TableItCompany, CompanyInitData> dataMapSetter)
            {
                return CompanyInitData.GetMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<CompanyInitData, TableItCompany>.GetUpdateKeyMapping(IDataMapSetter<TableItCompany, CompanyInitData> dataMapSetter)
            {
                return CompanyInitData.GetUpdateKeyMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<CompanyInitData, TableItCompany>.GetUpdateMapping(IDataMapSetter<TableItCompany, CompanyInitData> dataMapSetter)
            {
                return CompanyInitData.GetUpdateMapping(dataMapSetter);
            }
        }
    }
}