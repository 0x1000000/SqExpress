using System;
using SqExpress;
using SqExpress.QueryBuilders.RecordSetter;
using SqExpress.IntTest.Tables;
using SqExpress.Syntax.Names;
using System.Collections.Generic;

namespace SqExpress.IntTest.Tables.Models
{
    public class TestMergeData
    {
        public TestMergeData(int id, int value)
        {
            this.Id = id;
            this.Value = value;
        }

        public static TestMergeData Read(ISqDataRecordReader record, TestMergeTmpTable table)
        {
            return new TestMergeData(id: table.Id.Read(record), value: table.Value.Read(record));
        }

        public static TestMergeData ReadOrdinal(ISqDataRecordReader record, TestMergeTmpTable table, int offset)
        {
            return new TestMergeData(id: table.Id.Read(record, offset), value: table.Value.Read(record, offset + 1));
        }

        public int Id { get; }

        public int Value { get; }

        public TestMergeData WithId(int id)
        {
            return new TestMergeData(id: id, value: this.Value);
        }

        public TestMergeData WithValue(int value)
        {
            return new TestMergeData(id: this.Id, value: value);
        }

        public static TableColumn[] GetColumns(TestMergeTmpTable table)
        {
            return new TableColumn[]{table.Id, table.Value};
        }

        public static IRecordSetterNext GetMapping(IDataMapSetter<TestMergeTmpTable, TestMergeData> s)
        {
            return s.Set(s.Target.Id, s.Source.Id).Set(s.Target.Value, s.Source.Value);
        }

        public static IRecordSetterNext GetUpdateKeyMapping(IDataMapSetter<TestMergeTmpTable, TestMergeData> s)
        {
            return s.Set(s.Target.Id, s.Source.Id);
        }

        public static IRecordSetterNext GetUpdateMapping(IDataMapSetter<TestMergeTmpTable, TestMergeData> s)
        {
            return s.Set(s.Target.Value, s.Source.Value);
        }

        public static ISqModelReader<TestMergeData, TestMergeTmpTable> GetReader()
        {
            return TestMergeDataReader.Instance;
        }

        private class TestMergeDataReader : ISqModelReader<TestMergeData, TestMergeTmpTable>
        {
            public static TestMergeDataReader Instance { get; } = new TestMergeDataReader();
            IReadOnlyList<ExprColumn> ISqModelReader<TestMergeData, TestMergeTmpTable>.GetColumns(TestMergeTmpTable table)
            {
                return TestMergeData.GetColumns(table);
            }

            TestMergeData ISqModelReader<TestMergeData, TestMergeTmpTable>.Read(ISqDataRecordReader record, TestMergeTmpTable table)
            {
                return TestMergeData.Read(record, table);
            }

            TestMergeData ISqModelReader<TestMergeData, TestMergeTmpTable>.ReadOrdinal(ISqDataRecordReader record, TestMergeTmpTable table, int offset)
            {
                return TestMergeData.ReadOrdinal(record, table, offset);
            }
        }

        public static ISqModelUpdaterKey<TestMergeData, TestMergeTmpTable> GetUpdater()
        {
            return TestMergeDataUpdater.Instance;
        }

        private class TestMergeDataUpdater : ISqModelUpdaterKey<TestMergeData, TestMergeTmpTable>
        {
            public static TestMergeDataUpdater Instance { get; } = new TestMergeDataUpdater();
            IRecordSetterNext ISqModelUpdater<TestMergeData, TestMergeTmpTable>.GetMapping(IDataMapSetter<TestMergeTmpTable, TestMergeData> dataMapSetter)
            {
                return TestMergeData.GetMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<TestMergeData, TestMergeTmpTable>.GetUpdateKeyMapping(IDataMapSetter<TestMergeTmpTable, TestMergeData> dataMapSetter)
            {
                return TestMergeData.GetUpdateKeyMapping(dataMapSetter);
            }

            IRecordSetterNext ISqModelUpdaterKey<TestMergeData, TestMergeTmpTable>.GetUpdateMapping(IDataMapSetter<TestMergeTmpTable, TestMergeData> dataMapSetter)
            {
                return TestMergeData.GetUpdateMapping(dataMapSetter);
            }
        }

        public int Version { get; }
    }
}