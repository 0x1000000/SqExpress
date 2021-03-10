using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.IntTest.Tables;

namespace SqExpress.IntTest.Tables.Models
{
    public class Audit
    {
        public Audit(int version, DateTime created, DateTime modified)
        {
            this.Version = version;
            this.Created = created;
            this.Modified = modified;
        }

        public static Audit Read(ISqDataRecordReader record, TableItCompany table)
        {
            return new Audit(version: table.Version.Read(record), created: table.Created.Read(record), modified: table.Modified.Read(record));
        }

        public static Audit Read(ISqDataRecordReader record, TableItUser table)
        {
            return new Audit(version: table.Version.Read(record), created: table.Created.Read(record), modified: table.Modified.Read(record));
        }

        public int Version { get; }

        public DateTime Created { get; }

        public DateTime Modified { get; }

        public static TableColumn[] GetColumns(TableItCompany table)
        {
            return new TableColumn[]{table.Version, table.Created, table.Modified};
        }

        public static TableColumn[] GetColumns(TableItUser table)
        {
            return new TableColumn[]{table.Version, table.Created, table.Modified};
        }

        public static IRecordSetterNext GetMapping(IDataMapSetter<TableItCompany, Audit> s)
        {
            return s.Set(s.Target.Version, s.Source.Version).Set(s.Target.Created, s.Source.Created).Set(s.Target.Modified, s.Source.Modified);
        }
    }
}