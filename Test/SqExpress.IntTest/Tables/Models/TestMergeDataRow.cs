using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.IntTest.Tables;
using SqExpress.Syntax.Names;
using System.Collections.Generic;

namespace SqExpress.IntTest.Tables.Models
{
    public class TestMergeDataRow
    {
        public TestMergeDataRow(int id, int value, int version)
        {
            this.Id = id;
            this.Value = value;
            this.Version = version;
        }

        public static TestMergeDataRow Read(ISqDataRecordReader record, TestMergeTmpTable table)
        {
            return new TestMergeDataRow(id: table.Id.Read(record), value: table.Value.Read(record), version: table.Version.Read(record));
        }

        public static TestMergeDataRow ReadOrdinal(ISqDataRecordReader record, TestMergeTmpTable table, int offset)
        {
            return new TestMergeDataRow(id: table.Id.Read(record, offset), value: table.Value.Read(record, offset + 1), version: table.Version.Read(record, offset + 2));
        }

        public int Id { get; }

        public int Value { get; }

        public int Version { get; }

        public TestMergeDataRow WithId(int id)
        {
            return new TestMergeDataRow(id: id, value: this.Value, version: this.Version);
        }

        public TestMergeDataRow WithValue(int value)
        {
            return new TestMergeDataRow(id: this.Id, value: value, version: this.Version);
        }

        public TestMergeDataRow WithVersion(int version)
        {
            return new TestMergeDataRow(id: this.Id, value: this.Value, version: version);
        }

        public static TableColumn[] GetColumns(TestMergeTmpTable table)
        {
            return new TableColumn[]{table.Id, table.Value, table.Version};
        }

        public static IRecordSetterNext GetMapping(IDataMapSetter<TestMergeTmpTable, TestMergeDataRow> s)
        {
            return s.Set(s.Target.Id, s.Source.Id).Set(s.Target.Value, s.Source.Value).Set(s.Target.Version, s.Source.Version);
        }

        public static IRecordSetterNext GetUpdateKeyMapping(IDataMapSetter<TestMergeTmpTable, TestMergeDataRow> s)
        {
            return s.Set(s.Target.Id, s.Source.Id);
        }

        public static IRecordSetterNext GetUpdateMapping(IDataMapSetter<TestMergeTmpTable, TestMergeDataRow> s)
        {
            return s.Set(s.Target.Value, s.Source.Value).Set(s.Target.Version, s.Source.Version);
        }

        public static ISqModelReader<TestMergeDataRow, TestMergeTmpTable> GetReader()
        {
            return TestMergeDataRowReader.Instance;
        }

        private class TestMergeDataRowReader : ISqModelReader<TestMergeDataRow, TestMergeTmpTable>
        {
            public static TestMergeDataRowReader Instance { get; } = new TestMergeDataRowReader();
            IReadOnlyList<ExprColumn> ISqModelReader<TestMergeDataRow, TestMergeTmpTable>.GetColumns(TestMergeTmpTable table)
            {
                return TestMergeDataRow.GetColumns(table);
            }

            TestMergeDataRow ISqModelReader<TestMergeDataRow, TestMergeTmpTable>.Read(ISqDataRecordReader record, TestMergeTmpTable table)
            {
                return TestMergeDataRow.Read(record, table);
            }

            TestMergeDataRow ISqModelReader<TestMergeDataRow, TestMergeTmpTable>.ReadOrdinal(ISqDataRecordReader record, TestMergeTmpTable table, int offset)
            {
                return TestMergeDataRow.ReadOrdinal(record, table, offset);
            }
        }

        public static ISqModelUpdaterKey<TestMergeDataRow, TestMergeTmpTable> GetUpdater()
        {
            return TestMergeDataRowUpdater.Instance;
        }

        private class TestMergeDataRowUpdater : ISqModelUpdaterKey<TestMergeDataRow, TestMergeTmpTable>
        {
            public static TestMergeDataRowUpdater Instance { get; } = new TestMergeDataRowUpdater();
            IRecordSetterNext ISqModelUpdater<TestMergeDataRow, TestMergeTmpTable>.GetMapping(IDataMapSetter<TestMergeTmpTable, TestMergeDataRow> dataMapSetter)
            {
                return TestMergeDataRow.GetMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<TestMergeDataRow, TestMergeTmpTable>.GetUpdateKeyMapping(IDataMapSetter<TestMergeTmpTable, TestMergeDataRow> dataMapSetter)
            {
                return TestMergeDataRow.GetUpdateKeyMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<TestMergeDataRow, TestMergeTmpTable>.GetUpdateMapping(IDataMapSetter<TestMergeTmpTable, TestMergeDataRow> dataMapSetter)
            {
                return TestMergeDataRow.GetUpdateMapping(dataMapSetter);
            }
        }
    }
}