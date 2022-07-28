using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.GetStarted;
using SqExpress.Syntax.Names;
using System.Collections.Generic;
using SqExpress.Syntax.Select.SelectItems;

namespace SqExpress.GetStarted.Models
{
    public class AuditData
    {
        //Auto-generated by SqExpress Code-gen util
        public AuditData(int version, DateTime modifiedAt)
        {
            this.Version = version;
            this.ModifiedAt = modifiedAt;
        }

        //Auto-generated by SqExpress Code-gen util
        public static AuditData Read(ISqDataRecordReader record, TableCompany table)
        {
            return new AuditData(version: table.Version.Read(record), modifiedAt: table.ModifiedAt.Read(record));
        }

        //Auto-generated by SqExpress Code-gen util
        public static AuditData Read(ISqDataRecordReader record, TableUser table)
        {
            return new AuditData(version: table.Version.Read(record), modifiedAt: table.ModifiedAt.Read(record));
        }

        //Auto-generated by SqExpress Code-gen util
        public static AuditData ReadWithPrefix(ISqDataRecordReader record, TableCompany table, string prefix)
        {
            return new AuditData(version: table.Version.Read(record, prefix + table.Version.ColumnName.Name), modifiedAt: table.ModifiedAt.Read(record, prefix + table.ModifiedAt.ColumnName.Name));
        }

        //Auto-generated by SqExpress Code-gen util
        public static AuditData ReadWithPrefix(ISqDataRecordReader record, TableUser table, string prefix)
        {
            return new AuditData(version: table.Version.Read(record, prefix + table.Version.ColumnName.Name), modifiedAt: table.ModifiedAt.Read(record, prefix + table.ModifiedAt.ColumnName.Name));
        }

        //Auto-generated by SqExpress Code-gen util
        public static AuditData ReadOrdinal(ISqDataRecordReader record, TableCompany table, int offset)
        {
            return new AuditData(version: table.Version.Read(record, offset), modifiedAt: table.ModifiedAt.Read(record, offset + 1));
        }

        //Auto-generated by SqExpress Code-gen util
        public static AuditData ReadOrdinal(ISqDataRecordReader record, TableUser table, int offset)
        {
            return new AuditData(version: table.Version.Read(record, offset), modifiedAt: table.ModifiedAt.Read(record, offset + 1));
        }

        //Auto-generated by SqExpress Code-gen util
        public int Version { get; }

        //Auto-generated by SqExpress Code-gen util
        public DateTime ModifiedAt { get; }

        //Auto-generated by SqExpress Code-gen util
        public AuditData WithVersion(int version)
        {
            return new AuditData(version: version, modifiedAt: this.ModifiedAt);
        }

        //Auto-generated by SqExpress Code-gen util
        public AuditData WithModifiedAt(DateTime modifiedAt)
        {
            return new AuditData(version: this.Version, modifiedAt: modifiedAt);
        }

        //Auto-generated by SqExpress Code-gen util
        public static TableColumn[] GetColumns(TableCompany table)
        {
            return new TableColumn[]{table.Version, table.ModifiedAt};
        }

        //Auto-generated by SqExpress Code-gen util
        public static TableColumn[] GetColumns(TableUser table)
        {
            return new TableColumn[]{table.Version, table.ModifiedAt};
        }

        //Auto-generated by SqExpress Code-gen util
        public static ExprAliasedColumn[] GetColumnsWithPrefix(TableCompany table, string prefix)
        {
            return new ExprAliasedColumn[]{table.Version.As(prefix + table.Version.ColumnName.Name), table.ModifiedAt.As(prefix + table.ModifiedAt.ColumnName.Name)};
        }

        //Auto-generated by SqExpress Code-gen util
        public static ExprAliasedColumn[] GetColumnsWithPrefix(TableUser table, string prefix)
        {
            return new ExprAliasedColumn[]{table.Version.As(prefix + table.Version.ColumnName.Name), table.ModifiedAt.As(prefix + table.ModifiedAt.ColumnName.Name)};
        }

        //Auto-generated by SqExpress Code-gen util
        public static bool IsNull(ISqDataRecordReader record, TableCompany table)
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
        public static bool IsNull(ISqDataRecordReader record, TableUser table)
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
        public static bool IsNullWithPrefix(ISqDataRecordReader record, TableCompany table, string prefix)
        {
            foreach (var column in GetColumnsWithPrefix(table, prefix))
            {
                if (!record.IsDBNull(column.Alias.Name))
                {
                    return false;
                }
            }

            return true;
        }

        //Auto-generated by SqExpress Code-gen util
        public static bool IsNullWithPrefix(ISqDataRecordReader record, TableUser table, string prefix)
        {
            foreach (var column in GetColumnsWithPrefix(table, prefix))
            {
                if (!record.IsDBNull(column.Alias.Name))
                {
                    return false;
                }
            }

            return true;
        }

        //Auto-generated by SqExpress Code-gen util
        public static IRecordSetterNext GetMapping(IDataMapSetter<TableCompany, AuditData> s)
        {
            return s.Set(s.Target.Version, s.Source.Version).Set(s.Target.ModifiedAt, s.Source.ModifiedAt);
        }

        //Auto-generated by SqExpress Code-gen util
        public static IRecordSetterNext GetMapping(IDataMapSetter<TableUser, AuditData> s)
        {
            return s.Set(s.Target.Version, s.Source.Version).Set(s.Target.ModifiedAt, s.Source.ModifiedAt);
        }

        //Auto-generated by SqExpress Code-gen util
        public static ISqModelReader<AuditData, TableCompany> GetReaderForTableCompany()
        {
            return AuditDataReaderForTableCompany.Instance;
        }

        //Auto-generated by SqExpress Code-gen util
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

        //Auto-generated by SqExpress Code-gen util
        public static ISqModelReader<AuditData, TableUser> GetReaderForTableUser()
        {
            return AuditDataReaderForTableUser.Instance;
        }

        //Auto-generated by SqExpress Code-gen util
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

        //Auto-generated by SqExpress Code-gen util
        public static ISqModelUpdater<AuditData, TableCompany> GetUpdaterForTableCompany()
        {
            return AuditDataUpdaterForTableCompany.Instance;
        }

        //Auto-generated by SqExpress Code-gen util
        private class AuditDataUpdaterForTableCompany : ISqModelUpdater<AuditData, TableCompany>
        {
            public static AuditDataUpdaterForTableCompany Instance { get; } = new AuditDataUpdaterForTableCompany();
            IRecordSetterNext ISqModelUpdater<AuditData, TableCompany>.GetMapping(IDataMapSetter<TableCompany, AuditData> dataMapSetter)
            {
                return AuditData.GetMapping(dataMapSetter);
            }
        }

        //Auto-generated by SqExpress Code-gen util
        public static ISqModelUpdater<AuditData, TableUser> GetUpdaterForTableUser()
        {
            return AuditDataUpdaterForTableUser.Instance;
        }

        //Auto-generated by SqExpress Code-gen util
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