using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.IntTest.Tables;
using SqExpress.Syntax.Names;
using System.Collections.Generic;
using SqExpress.Syntax.Select.SelectItems;

namespace SqExpress.IntTest.Tables.Models
{
    public class CompanyInitData
    {
        //Auto-generated by SqExpress Code-gen util
        public CompanyInitData(int id, Guid externalId, string name)
        {
            this.Id = id;
            this.ExternalId = externalId;
            this.Name = name;
        }

        //Auto-generated by SqExpress Code-gen util
        public static CompanyInitData Read(ISqDataRecordReader record, TableItCompany table)
        {
            return new CompanyInitData(id: table.CompanyId.Read(record), externalId: table.ExternalId.Read(record), name: table.CompanyName.Read(record));
        }

        //Auto-generated by SqExpress Code-gen util
        public static CompanyInitData ReadWithPrefix(ISqDataRecordReader record, TableItCompany table, string prefix)
        {
            return new CompanyInitData(id: table.CompanyId.Read(record, prefix + table.CompanyId.ColumnName.Name), externalId: table.ExternalId.Read(record, prefix + table.ExternalId.ColumnName.Name), name: table.CompanyName.Read(record, prefix + table.CompanyName.ColumnName.Name));
        }

        //Auto-generated by SqExpress Code-gen util
        public static CompanyInitData ReadOrdinal(ISqDataRecordReader record, TableItCompany table, int offset)
        {
            return new CompanyInitData(id: table.CompanyId.Read(record, offset), externalId: table.ExternalId.Read(record, offset + 1), name: table.CompanyName.Read(record, offset + 2));
        }

        //Auto-generated by SqExpress Code-gen util
        public int Id { get; }

        //Auto-generated by SqExpress Code-gen util
        public Guid ExternalId { get; }

        //Auto-generated by SqExpress Code-gen util
        public string Name { get; }

        //Auto-generated by SqExpress Code-gen util
        public CompanyInitData WithId(int id)
        {
            return new CompanyInitData(id: id, externalId: this.ExternalId, name: this.Name);
        }

        //Auto-generated by SqExpress Code-gen util
        public CompanyInitData WithExternalId(Guid externalId)
        {
            return new CompanyInitData(id: this.Id, externalId: externalId, name: this.Name);
        }

        //Auto-generated by SqExpress Code-gen util
        public CompanyInitData WithName(string name)
        {
            return new CompanyInitData(id: this.Id, externalId: this.ExternalId, name: name);
        }

        //Auto-generated by SqExpress Code-gen util
        public static TableColumn[] GetColumns(TableItCompany table)
        {
            return new TableColumn[]{table.CompanyId, table.ExternalId, table.CompanyName};
        }

        //Auto-generated by SqExpress Code-gen util
        public static ExprAliasedColumn[] GetColumnsWithPrefix(TableItCompany table, string prefix)
        {
            return new ExprAliasedColumn[]{table.CompanyId.As(prefix + table.CompanyId.ColumnName.Name), table.ExternalId.As(prefix + table.ExternalId.ColumnName.Name), table.CompanyName.As(prefix + table.CompanyName.ColumnName.Name)};
        }

        //Auto-generated by SqExpress Code-gen util
        public static bool IsNull(ISqDataRecordReader record, TableItCompany table)
        {
            foreach (var column in GetColumns(table))
            {
                if (!record.IsDBNull(column.ColumnName.Name))
                {
                    return false;
                }
            }

            return true;
        }

        //Auto-generated by SqExpress Code-gen util
        public static bool IsNullWithPrefix(ISqDataRecordReader record, TableItCompany table, string prefix)
        {
            foreach (var column in GetColumnsWithPrefix(table, prefix))
            {
                if (!record.IsDBNull(column.Alias!.Name))
                {
                    return false;
                }
            }

            return true;
        }

        //Auto-generated by SqExpress Code-gen util
        public static IRecordSetterNext GetMapping(IDataMapSetter<TableItCompany, CompanyInitData> s)
        {
            return s.Set(s.Target.ExternalId, s.Source.ExternalId).Set(s.Target.CompanyName, s.Source.Name);
        }

        //Auto-generated by SqExpress Code-gen util
        public static IRecordSetterNext GetUpdateKeyMapping(IDataMapSetter<TableItCompany, CompanyInitData> s)
        {
            return s.Set(s.Target.CompanyId, s.Source.Id);
        }

        //Auto-generated by SqExpress Code-gen util
        public static IRecordSetterNext GetUpdateMapping(IDataMapSetter<TableItCompany, CompanyInitData> s)
        {
            return s.Set(s.Target.ExternalId, s.Source.ExternalId).Set(s.Target.CompanyName, s.Source.Name);
        }

        //Auto-generated by SqExpress Code-gen util
        public static ISqModelReader<CompanyInitData, TableItCompany> GetReader()
        {
            return CompanyInitDataReader.Instance;
        }

        //Auto-generated by SqExpress Code-gen util
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

        //Auto-generated by SqExpress Code-gen util
        public static ISqModelUpdaterKey<CompanyInitData, TableItCompany> GetUpdater()
        {
            return CompanyInitDataUpdater.Instance;
        }

        //Auto-generated by SqExpress Code-gen util
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