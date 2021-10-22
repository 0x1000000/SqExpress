using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.GetStarted;
using SqExpress.Syntax.Names;
using System.Collections.Generic;

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

        public static AuditData ReadOrdinal(ISqDataRecordReader record, TableCompany table, int offset)
        {
            return new AuditData(version: table.Version.Read(record, offset), modifiedAt: table.ModifiedAt.Read(record, offset + 1));
        }

        public static AuditData ReadOrdinal(ISqDataRecordReader record, TableUser table, int offset)
        {
            return new AuditData(version: table.Version.Read(record, offset), modifiedAt: table.ModifiedAt.Read(record, offset + 1));
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

        public static ISqModelReader<AuditData, TableCompany> GetReaderForTableCompany()
        {
            return AuditDataReaderForTableCompany.Instance;
        }

        private class AuditDataReaderForTableCompany : ISqModelReader<AuditData, TableCompany>
        {
            public static AuditDataReaderForTableCompany Instance { get; } = new AuditDataReaderForTableCompany();
            IReadOnlyList<ExprColumn> ISqModelReader<AuditData, TableCompany>.GetColumns(TableCompany table)
            {
                return AuditData.GetColumns(table);
            }

            AuditData ISqModelReader<AuditData, TableCompany>.Read(ISqDataRecordReader record, TableCompany table)
            {
                return AuditData.Read(record, table);
            }

            AuditData ISqModelReader<AuditData, TableCompany>.ReadOrdinal(ISqDataRecordReader record, TableCompany table, int offset)
            {
                return AuditData.ReadOrdinal(record, table, offset);
            }
        }

        public static ISqModelReader<AuditData, TableUser> GetReaderForTableUser()
        {
            return AuditDataReaderForTableUser.Instance;
        }

        private class AuditDataReaderForTableUser : ISqModelReader<AuditData, TableUser>
        {
            public static AuditDataReaderForTableUser Instance { get; } = new AuditDataReaderForTableUser();
            IReadOnlyList<ExprColumn> ISqModelReader<AuditData, TableUser>.GetColumns(TableUser table)
            {
                return AuditData.GetColumns(table);
            }

            AuditData ISqModelReader<AuditData, TableUser>.Read(ISqDataRecordReader record, TableUser table)
            {
                return AuditData.Read(record, table);
            }

            AuditData ISqModelReader<AuditData, TableUser>.ReadOrdinal(ISqDataRecordReader record, TableUser table, int offset)
            {
                return AuditData.ReadOrdinal(record, table, offset);
            }
        }

        public static ISqModelUpdater<AuditData, TableCompany> GetUpdaterForTableCompany()
        {
            return AuditDataUpdaterForTableCompany.Instance;
        }

        private class AuditDataUpdaterForTableCompany : ISqModelUpdater<AuditData, TableCompany>
        {
            public static AuditDataUpdaterForTableCompany Instance { get; } = new AuditDataUpdaterForTableCompany();
            IRecordSetterNext ISqModelUpdater<AuditData, TableCompany>.GetMapping(IDataMapSetter<TableCompany, AuditData> dataMapSetter)
            {
                return AuditData.GetMapping(dataMapSetter);
            }
        }

        public static ISqModelUpdater<AuditData, TableUser> GetUpdaterForTableUser()
        {
            return AuditDataUpdaterForTableUser.Instance;
        }

        private class AuditDataUpdaterForTableUser : ISqModelUpdater<AuditData, TableUser>
        {
            public static AuditDataUpdaterForTableUser Instance { get; } = new AuditDataUpdaterForTableUser();
            IRecordSetterNext ISqModelUpdater<AuditData, TableUser>.GetMapping(IDataMapSetter<TableUser, AuditData> dataMapSetter)
            {
                return AuditData.GetMapping(dataMapSetter);
            }
        }
    }
}