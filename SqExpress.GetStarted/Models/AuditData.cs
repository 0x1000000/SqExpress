using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.GetStarted;

namespace SqExpress.GetStarted.Models
{
    public class AuditData
    {
        public AuditData(int version, DateTime modifiedAt)
        {
            this.Version = version;
            this.ModifiedAt = modifiedAt;
        }

        public static AuditData Read(ISqDataRecordReader record, TableCompany table)
        {
            return new AuditData(version: table.Version.Read(record), modifiedAt: table.ModifiedAt.Read(record));
        }

        public static AuditData Read(ISqDataRecordReader record, TableUser table)
        {
            return new AuditData(version: table.Version.Read(record), modifiedAt: table.ModifiedAt.Read(record));
        }

        public int Version { get; }

        public DateTime ModifiedAt { get; }

        public AuditData WithVersion(int version)
        {
            return new AuditData(version: version, modifiedAt: this.ModifiedAt);
        }

        public AuditData WithModifiedAt(DateTime modifiedAt)
        {
            return new AuditData(version: this.Version, modifiedAt: modifiedAt);
        }

        public static TableColumn[] GetColumns(TableCompany table)
        {
            return new TableColumn[]{table.Version, table.ModifiedAt};
        }

        public static TableColumn[] GetColumns(TableUser table)
        {
            return new TableColumn[]{table.Version, table.ModifiedAt};
        }

        public static IRecordSetterNext GetMapping(IDataMapSetter<TableCompany, AuditData> s)
        {
            return s.Set(s.Target.Version, s.Source.Version).Set(s.Target.ModifiedAt, s.Source.ModifiedAt);
        }

        public static IRecordSetterNext GetMapping(IDataMapSetter<TableUser, AuditData> s)
        {
            return s.Set(s.Target.Version, s.Source.Version).Set(s.Target.ModifiedAt, s.Source.ModifiedAt);
        }
    }
}