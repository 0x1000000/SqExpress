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

        public Audit WithVersion(int version)
        {
            return new Audit(version: version, created: this.Created, modified: this.Modified);
        }

        public Audit WithCreated(DateTime created)
        {
            return new Audit(version: this.Version, created: created, modified: this.Modified);
        }

        public Audit WithModified(DateTime modified)
        {
            return new Audit(version: this.Version, created: this.Created, modified: modified);
        }

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

        public static IRecordSetterNext GetMapping(IDataMapSetter<TableItUser, Audit> s)
        {
            return s.Set(s.Target.Version, s.Source.Version).Set(s.Target.Created, s.Source.Created).Set(s.Target.Modified, s.Source.Modified);
        }

        public static ISqModelReader<Audit, TableItCompany> GetReaderForTableItCompany()
        {
            return AuditReaderForTableItCompany.Instance;
        }

        private class AuditReaderForTableItCompany : ISqModelReader<Audit, TableItCompany>
        {
            public static AuditReaderForTableItCompany Instance { get; } = new AuditReaderForTableItCompany();
            TableColumn[] ISqModelReader<Audit, TableItCompany>.GetColumns(TableItCompany table)
            {
                return Audit.GetColumns(table);
            }

            Audit ISqModelReader<Audit, TableItCompany>.Read(ISqDataRecordReader record, TableItCompany table)
            {
                return Audit.Read(record, table);
            }
        }

        public static ISqModelReader<Audit, TableItUser> GetReaderForTableItUser()
        {
            return AuditReaderForTableItUser.Instance;
        }

        private class AuditReaderForTableItUser : ISqModelReader<Audit, TableItUser>
        {
            public static AuditReaderForTableItUser Instance { get; } = new AuditReaderForTableItUser();
            TableColumn[] ISqModelReader<Audit, TableItUser>.GetColumns(TableItUser table)
            {
                return Audit.GetColumns(table);
            }

            Audit ISqModelReader<Audit, TableItUser>.Read(ISqDataRecordReader record, TableItUser table)
            {
                return Audit.Read(record, table);
            }
        }

        public static ISqModelUpdater<Audit, TableItCompany> GetUpdaterForTableItCompany()
        {
            return AuditUpdaterForTableItCompany.Instance;
        }

        private class AuditUpdaterForTableItCompany : ISqModelUpdater<Audit, TableItCompany>
        {
            public static AuditUpdaterForTableItCompany Instance { get; } = new AuditUpdaterForTableItCompany();
            IRecordSetterNext ISqModelUpdater<Audit, TableItCompany>.GetMapping(IDataMapSetter<TableItCompany, Audit> dataMapSetter)
            {
                return Audit.GetMapping(dataMapSetter);
            }
        }

        public static ISqModelUpdater<Audit, TableItUser> GetUpdaterForTableItUser()
        {
            return AuditUpdaterForTableItUser.Instance;
        }

        private class AuditUpdaterForTableItUser : ISqModelUpdater<Audit, TableItUser>
        {
            public static AuditUpdaterForTableItUser Instance { get; } = new AuditUpdaterForTableItUser();
            IRecordSetterNext ISqModelUpdater<Audit, TableItUser>.GetMapping(IDataMapSetter<TableItUser, Audit> dataMapSetter)
            {
                return Audit.GetMapping(dataMapSetter);
            }
        }
    }
}