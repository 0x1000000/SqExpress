using System.Linq;
using Moq;
using NUnit.Framework;
using SqExpress.Syntax.Names;

namespace SqExpress.Test.Meta
{
    [TestFixture]
    public class TableColumnsTest
    {
        private readonly AllColumnTypes Table = new AllColumnTypes("AT");

        private readonly ExprTableAlias NewSource = new ExprTableAlias(new ExprAlias("NS"));

        [Test]
        public void Test()
        {
            AllColumnTypes allColumnTypes = this.Table;
            foreach (var tableColumn in allColumnTypes.Columns)
            {
                Assert.AreEqual(this.Table, tableColumn.Table);
                Assert.AreEqual(this.Table.Alias, tableColumn.Source);

                var newColumn = tableColumn.WithSource(this.NewSource);

                Assert.AreEqual(newColumn.GetType(), tableColumn.GetType());
                Assert.AreNotEqual(newColumn, tableColumn);

                Assert.AreEqual(newColumn.Table, this.Table);
                Assert.AreEqual(newColumn.Source, this.NewSource);
            }
        }

        [Test]
        public void TestBoolean()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.Boolean, this.Table.ColBoolean.SqlType);
            Assert.AreEqual("[AT].[ColBoolean]", this.Table.ColBoolean.ToSql());
            Assert.IsFalse(this.Table.ColBoolean.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColBoolean.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColBoolean.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColBoolean.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColBoolean.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColBoolean.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetBoolean(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestNullableBoolean()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.Boolean, this.Table.ColNullableBoolean.SqlType);
            Assert.AreEqual("[AT].[ColNullableBoolean]", this.Table.ColNullableBoolean.ToSql());
            Assert.IsTrue(this.Table.ColNullableBoolean.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColNullableBoolean.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColNullableBoolean.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColNullableBoolean.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColNullableBoolean.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColNullableBoolean.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetNullableBoolean(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestByte()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.Byte, this.Table.ColByte.SqlType);
            Assert.AreEqual("[AT].[ColByte]", this.Table.ColByte.ToSql());
            Assert.IsFalse(this.Table.ColByte.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColByte.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColByte.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColByte.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColByte.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColByte.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetByte(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestNullableByte()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.Byte, this.Table.ColNullableByte.SqlType);
            Assert.AreEqual("[AT].[ColNullableByte]", this.Table.ColNullableByte.ToSql());
            Assert.IsTrue(this.Table.ColNullableByte.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColNullableByte.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColNullableByte.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColNullableByte.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColNullableByte.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColNullableByte.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetNullableByte(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestInt16()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.Int16, this.Table.ColInt16.SqlType);
            Assert.AreEqual("[AT].[ColInt16]", this.Table.ColInt16.ToSql());
            Assert.IsFalse(this.Table.ColInt16.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColInt16.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColInt16.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColInt16.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColInt16.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColInt16.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetInt16(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestNullableInt16()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.Int16, this.Table.ColNullableInt16.SqlType);
            Assert.AreEqual("[AT].[ColNullableInt16]", this.Table.ColNullableInt16.ToSql());
            Assert.IsTrue(this.Table.ColNullableInt16.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColNullableInt16.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColNullableInt16.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColNullableInt16.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColNullableInt16.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColNullableInt16.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetNullableInt16(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestInt32()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.Int32, this.Table.ColInt32.SqlType);
            Assert.AreEqual("[AT].[ColInt32]", this.Table.ColInt32.ToSql());
            Assert.IsFalse(this.Table.ColInt32.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColInt32.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColInt32.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColInt32.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColInt32.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColInt32.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetInt32(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestNullableInt32()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.Int32, this.Table.ColNullableInt32.SqlType);
            Assert.AreEqual("[AT].[ColNullableInt32]", this.Table.ColNullableInt32.ToSql());
            Assert.IsTrue(this.Table.ColNullableInt32.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColNullableInt32.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColNullableInt32.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColNullableInt32.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColNullableInt32.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColNullableInt32.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetNullableInt32(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestInt64()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.Int64, this.Table.ColInt64.SqlType);
            Assert.AreEqual("[AT].[ColInt64]", this.Table.ColInt64.ToSql());
            Assert.IsFalse(this.Table.ColInt64.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColInt64.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColInt64.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColInt64.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColInt64.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColInt64.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetInt64(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestNullableInt64()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.Int64, this.Table.ColNullableInt64.SqlType);
            Assert.AreEqual("[AT].[ColNullableInt64]", this.Table.ColNullableInt64.ToSql());
            Assert.IsTrue(this.Table.ColNullableInt64.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColNullableInt64.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColNullableInt64.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColNullableInt64.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColNullableInt64.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColNullableInt64.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetNullableInt64(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestDecimal()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.Decimal().GetType(), this.Table.ColDecimal.SqlType.GetType());
            Assert.AreEqual("[AT].[ColDecimal]", this.Table.ColDecimal.ToSql());
            Assert.IsFalse(this.Table.ColDecimal.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColDecimal.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColDecimal.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColDecimal.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColDecimal.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColDecimal.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetDecimal(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestNullableDecimal()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.Decimal().GetType(), this.Table.ColNullableDecimal.SqlType.GetType());
            Assert.AreEqual("[AT].[ColNullableDecimal]", this.Table.ColNullableDecimal.ToSql());
            Assert.IsTrue(this.Table.ColNullableDecimal.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColNullableDecimal.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColNullableDecimal.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColNullableDecimal.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColNullableDecimal.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColNullableDecimal.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetNullableDecimal(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestDouble()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.Double, this.Table.ColDouble.SqlType);
            Assert.AreEqual("[AT].[ColDouble]", this.Table.ColDouble.ToSql());
            Assert.IsFalse(this.Table.ColDouble.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColDouble.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColDouble.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColDouble.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColDouble.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColDouble.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetDouble(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestNullableDouble()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.Double, this.Table.ColNullableDouble.SqlType);
            Assert.AreEqual("[AT].[ColNullableDouble]", this.Table.ColNullableDouble.ToSql());
            Assert.IsTrue(this.Table.ColNullableDouble.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColNullableDouble.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColNullableDouble.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColNullableDouble.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColNullableDouble.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColNullableDouble.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetNullableDouble(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestDateTime()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.DateTime().GetType(), this.Table.ColDateTime.SqlType.GetType());
            Assert.AreEqual("[AT].[ColDateTime]", this.Table.ColDateTime.ToSql());
            Assert.IsFalse(this.Table.ColDateTime.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColDateTime.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColDateTime.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColDateTime.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColDateTime.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColDateTime.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetDateTime(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestNullableDateTime()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.DateTime().GetType(), this.Table.ColNullableDateTime.SqlType.GetType());
            Assert.AreEqual("[AT].[ColNullableDateTime]", this.Table.ColNullableDateTime.ToSql());
            Assert.IsTrue(this.Table.ColNullableDateTime.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColNullableDateTime.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColNullableDateTime.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColNullableDateTime.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColNullableDateTime.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColNullableDateTime.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetNullableDateTime(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestGuid()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.Guid, this.Table.ColGuid.SqlType);
            Assert.AreEqual("[AT].[ColGuid]", this.Table.ColGuid.ToSql());
            Assert.IsFalse(this.Table.ColGuid.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColGuid.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColGuid.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColGuid.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColGuid.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColGuid.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetGuid(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestNullableGuid()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.Guid, this.Table.ColNullableGuid.SqlType);
            Assert.AreEqual("[AT].[ColNullableGuid]", this.Table.ColNullableGuid.ToSql());
            Assert.IsTrue(this.Table.ColNullableGuid.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColNullableGuid.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColNullableGuid.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColNullableGuid.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColNullableGuid.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColNullableGuid.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetNullableGuid(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestString()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.String().GetType(), this.Table.ColString.SqlType.GetType());
            Assert.AreEqual("[AT].[ColString]", this.Table.ColString.ToSql());
            Assert.IsFalse(this.Table.ColString.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColString.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColString.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColString.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColString.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColString.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetString(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

        [Test]
        public void TestNullableString()
        {
            Assert.AreEqual(SqQueryBuilder.SqlType.String().GetType(), this.Table.ColNullableString.SqlType.GetType());
            Assert.AreEqual("[AT].[ColNullableString]", this.Table.ColNullableString.ToSql());
            Assert.IsTrue(this.Table.ColNullableString.IsNullable);

            DerivedTable dt = new DerivedTable();
            var customColumn = this.Table.ColNullableString.AddToDerivedTable(dt);
            Assert.AreEqual(this.Table.ColNullableString.ColumnName, customColumn.ColumnName);
            Assert.IsTrue(dt.Columns.Contains(customColumn));

            var customColumn2 = this.Table.ColNullableString.ToCustomColumn(this.NewSource);
            Assert.AreEqual(this.Table.ColNullableString.ColumnName, customColumn2.ColumnName);

            var reader = new Mock<ISqDataRecordReader>();

            this.Table.ColNullableString.Read(reader.Object);
            customColumn.Read(reader.Object);

            reader.Verify(r => r.GetNullableString(It.Is<string>(name => name == customColumn.ColumnName.Name)), Times.Exactly(2));
        }

    }

    public class DerivedTable : DerivedTableBase
    {
        public DerivedTable() : base("DT")
        {
        }

        protected override IExprSubQuery CreateQuery()
        {
            return SqQueryBuilder.SelectOne().Done();
        }
    }

    public class AllColumnTypes : TableBase
    {
        public AllColumnTypes(Alias alias = default) : base("dbo", "AllColumnTypes", alias)
        {
            this.ColBoolean = this.CreateBooleanColumn(nameof(ColBoolean));
            this.ColNullableBoolean = this.CreateNullableBooleanColumn(nameof(ColNullableBoolean));
            this.ColByte = this.CreateByteColumn(nameof(ColByte));
            this.ColNullableByte = this.CreateNullableByteColumn(nameof(ColNullableByte));
            this.ColInt16 = this.CreateInt16Column(nameof(ColInt16));
            this.ColNullableInt16 = this.CreateNullableInt16Column(nameof(ColNullableInt16));
            this.ColInt32 = this.CreateInt32Column(nameof(ColInt32));
            this.ColNullableInt32 = this.CreateNullableInt32Column(nameof(ColNullableInt32));
            this.ColInt64 = this.CreateInt64Column(nameof(ColInt64));
            this.ColNullableInt64 = this.CreateNullableInt64Column(nameof(ColNullableInt64));
            this.ColDecimal = this.CreateDecimalColumn(nameof(ColDecimal), (10, 6));
            this.ColNullableDecimal = this.CreateNullableDecimalColumn(nameof(ColNullableDecimal), (10, 6));
            this.ColDouble = this.CreateDoubleColumn(nameof(ColDouble));
            this.ColNullableDouble = this.CreateNullableDoubleColumn(nameof(ColNullableDouble));
            this.ColDateTime = this.CreateDateTimeColumn(nameof(ColDateTime));
            this.ColNullableDateTime = this.CreateNullableDateTimeColumn(nameof(ColNullableDateTime));
            this.ColGuid = this.CreateGuidColumn(nameof(ColGuid));
            this.ColNullableGuid = this.CreateNullableGuidColumn(nameof(ColNullableGuid));
            this.ColString = this.CreateStringColumn(nameof(this.ColString), null, true);
            this.ColNullableString = this.CreateNullableStringColumn(nameof(this.ColNullableString), null, true);
        }

        public StringTableColumn ColString5 { get; set; }

        public NullableStringTableColumn ColNullableStringMax { get; set; }

        public StringTableColumn ColStringMax { get; set; }

        public NullableStringTableColumn ColNullableString { get; set; }

        public StringTableColumn ColString { get; set; }

        public NullableGuidTableColumn ColNullableGuid { get; set; }

        public GuidTableColumn ColGuid { get; set; }

        public NullableDateTimeTableColumn ColNullableDateTime { get; set; }

        public DateTimeTableColumn ColDateTime { get; set; }

        public NullableDoubleTableColumn ColNullableDouble { get; set; }

        public DoubleTableColumn ColDouble { get; set; }

        public NullableDecimalTableColumn ColNullableDecimal { get; set; }

        public DecimalTableColumn ColDecimal { get; set; }

        public NullableInt64TableColumn ColNullableInt64 { get; set; }

        public Int64TableColumn ColInt64 { get; set; }

        public NullableInt32TableColumn ColNullableInt32 { get; set; }

        public Int32TableColumn ColInt32 { get; set; }

        public NullableInt16TableColumn ColNullableInt16 { get; set; }

        public Int16TableColumn ColInt16 { get; set; }

        public NullableByteTableColumn ColNullableByte { get; set; }

        public ByteTableColumn ColByte { get; set; }

        public NullableBooleanTableColumn ColNullableBoolean { get; set; }

        public BooleanTableColumn ColBoolean { get; set; }
    }

}