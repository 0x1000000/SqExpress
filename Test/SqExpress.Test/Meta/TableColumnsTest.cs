using System;
using System.Globalization;
using System.Linq;
using Moq;
using NUnit.Framework;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;

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

            Assert.Throws<SqExpressException>(() => this.Table.ColBoolean.ReadAsString(reader.Object));
            reader.Setup(r=>r.GetNullableBoolean(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(true);
            Assert.AreEqual(true.ToString(),this.Table.ColBoolean.ReadAsString(reader.Object));

            Assert.AreEqual("1", this.Table.ColBoolean.FromString("True").ToSql());
            Assert.Throws<SqExpressException>(() => this.Table.ColBoolean.FromString(null));
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

            Assert.IsNull(this.Table.ColNullableBoolean.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableBoolean(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(true);
            Assert.AreEqual(true.ToString(), this.Table.ColNullableBoolean.ReadAsString(reader.Object));

            Assert.AreEqual("0", this.Table.ColNullableBoolean.FromString("False").ToSql());
            Assert.AreEqual("NULL", this.Table.ColNullableBoolean.FromString(null).ToSql());
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

            Assert.Throws<SqExpressException>(() => this.Table.ColByte.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableByte(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(255);
            Assert.AreEqual(255.ToString(), this.Table.ColByte.ReadAsString(reader.Object));

            Assert.AreEqual("255", this.Table.ColByte.FromString("255").ToSql());
            Assert.Throws<SqExpressException>(() => this.Table.ColByte.FromString(null));
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

            Assert.IsNull(this.Table.ColNullableByte.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableByte(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(255);
            Assert.AreEqual(255.ToString(), this.Table.ColNullableByte.ReadAsString(reader.Object));

            Assert.AreEqual("255", this.Table.ColNullableByte.FromString("255").ToSql());
            Assert.AreEqual("NULL", this.Table.ColNullableByte.FromString(null).ToSql());
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

            Assert.Throws<SqExpressException>(() => this.Table.ColInt16.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableInt16(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(10000);
            Assert.AreEqual(10000.ToString(), this.Table.ColInt16.ReadAsString(reader.Object));

            Assert.AreEqual("10000", this.Table.ColInt16.FromString("10000").ToSql());
            Assert.Throws<SqExpressException>(() => this.Table.ColInt16.FromString(null));
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

            Assert.IsNull(this.Table.ColNullableInt16.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableInt16(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(255);
            Assert.AreEqual(255.ToString(), this.Table.ColNullableInt16.ReadAsString(reader.Object));

            Assert.AreEqual("255", this.Table.ColNullableInt16.FromString("255").ToSql());
            Assert.AreEqual("NULL", this.Table.ColNullableInt16.FromString(null).ToSql());
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

            Assert.Throws<SqExpressException>(() => this.Table.ColInt32.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableInt32(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(Int32.MaxValue);
            Assert.AreEqual(Int32.MaxValue.ToString(), this.Table.ColInt32.ReadAsString(reader.Object));

            Assert.AreEqual(Int32.MinValue.ToString(), this.Table.ColInt32.FromString(Int32.MinValue.ToString()).ToSql());
            Assert.Throws<SqExpressException>(() => this.Table.ColInt32.FromString(null));
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

            Assert.IsNull(this.Table.ColNullableInt32.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableInt32(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(Int32.MaxValue);
            Assert.AreEqual(Int32.MaxValue.ToString(), this.Table.ColNullableInt32.ReadAsString(reader.Object));

            Assert.AreEqual(Int32.MinValue.ToString(), this.Table.ColNullableInt32.FromString(Int32.MinValue.ToString()).ToSql());
            Assert.AreEqual("NULL", this.Table.ColNullableInt32.FromString(null).ToSql());
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

            Assert.Throws<SqExpressException>(() => this.Table.ColInt64.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableInt64(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(Int64.MaxValue);
            Assert.AreEqual(Int64.MaxValue.ToString(), this.Table.ColInt64.ReadAsString(reader.Object));

            Assert.AreEqual(Int64.MinValue.ToString(), this.Table.ColInt64.FromString(Int64.MinValue.ToString()).ToSql());
            Assert.Throws<SqExpressException>(() => this.Table.ColInt64.FromString(null));
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

            Assert.IsNull(this.Table.ColNullableInt64.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableInt64(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(Int64.MaxValue);
            Assert.AreEqual(Int64.MaxValue.ToString(), this.Table.ColNullableInt64.ReadAsString(reader.Object));

            Assert.AreEqual(Int64.MinValue.ToString(), this.Table.ColNullableInt64.FromString(Int64.MinValue.ToString()).ToSql());
            Assert.AreEqual("NULL", this.Table.ColNullableInt64.FromString(null).ToSql());
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

            Assert.Throws<SqExpressException>(() => this.Table.ColDecimal.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableDecimal(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(-12.34567m);
            Assert.AreEqual((-12.34567m).ToString("F",CultureInfo.InvariantCulture), this.Table.ColDecimal.ReadAsString(reader.Object));

            Assert.AreEqual(12.34567m.ToString("F",CultureInfo.InvariantCulture), this.Table.ColDecimal.FromString(12.34567m.ToString("F", CultureInfo.InvariantCulture)).ToSql());
            Assert.Throws<SqExpressException>(() => this.Table.ColDecimal.FromString(null));
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

            Assert.IsNull(this.Table.ColNullableDecimal.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableDecimal(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(-12.34567m);
            Assert.AreEqual((-12.34567m).ToString("F", CultureInfo.InvariantCulture), this.Table.ColNullableDecimal.ReadAsString(reader.Object));

            Assert.AreEqual(12.34567m.ToString("F", CultureInfo.InvariantCulture), this.Table.ColNullableDecimal.FromString(12.34567m.ToString("F", CultureInfo.InvariantCulture)).ToSql());
            Assert.AreEqual("NULL", this.Table.ColNullableDecimal.FromString(null).ToSql());
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

            Assert.Throws<SqExpressException>(() => this.Table.ColDouble.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableDouble(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(-12.34567);
            Assert.AreEqual((-12.34567).ToString("F", CultureInfo.InvariantCulture), this.Table.ColDouble.ReadAsString(reader.Object));

            Assert.AreEqual(12.34567.ToString("F", CultureInfo.InvariantCulture), this.Table.ColDouble.FromString(12.34567.ToString("F", CultureInfo.InvariantCulture)).ToSql());
            Assert.Throws<SqExpressException>(() => this.Table.ColDouble.FromString(null));
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

            Assert.IsNull(this.Table.ColNullableDouble.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableDouble(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(-12.34567);
            Assert.AreEqual((-12.34567).ToString("F", CultureInfo.InvariantCulture), this.Table.ColNullableDouble.ReadAsString(reader.Object));

            Assert.AreEqual(12.34567.ToString("F", CultureInfo.InvariantCulture), this.Table.ColNullableDouble.FromString(12.34567.ToString("F", CultureInfo.InvariantCulture)).ToSql());
            Assert.AreEqual("NULL", this.Table.ColNullableDouble.FromString(null).ToSql());
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

            var date1 = new DateTime(2021, 10, 21, 9, 25, 37);
            var dateString1 = date1.ToString("yyyy-MM-ddTHH:mm:ss.fff");

            Assert.Throws<SqExpressException>(() => this.Table.ColDateTime.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableDateTime(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(date1);
            Assert.AreEqual(dateString1, this.Table.ColDateTime.ReadAsString(reader.Object));

            Assert.AreEqual($"'{dateString1}'", this.Table.ColDateTime.FromString(dateString1).ToSql());
            Assert.Throws<SqExpressException>(() => this.Table.ColDateTime.FromString(null));

            var date2 = new DateTime(2021, 10, 21);
            var dateString2 = date2.ToString("yyyy-MM-dd");
            var dateString2Full = date2.ToString("yyyy-MM-ddTHH:mm:ss.fff");

            reader.Setup(r => r.GetNullableDateTime(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(date2);
            Assert.AreEqual(dateString2Full, this.Table.ColDateTime.ReadAsString(reader.Object));

            Assert.AreEqual($"'{dateString2}'", this.Table.ColDateTime.FromString(dateString2).ToSql());
            Assert.Throws<SqExpressException>(() => this.Table.ColDateTime.FromString(null));
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

            var date = new DateTime(2021, 10, 21, 9, 25, 37);
            var dateString = date.ToString("yyyy-MM-ddTHH:mm:ss.fff");

            Assert.IsNull(this.Table.ColNullableDateTime.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableDateTime(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(date);
            Assert.AreEqual(dateString, this.Table.ColNullableDateTime.ReadAsString(reader.Object));

            Assert.AreEqual($"'{dateString}'", this.Table.ColNullableDateTime.FromString(dateString).ToSql());
            Assert.AreEqual("NULL", this.Table.ColNullableDateTime.FromString(null).ToSql());
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

            var guid = new Guid("3E0F7FA1-E7CA-4F6E-BF19-69C398565EA2");
            var guidString = guid.ToString("D");

            Assert.Throws<SqExpressException>(() => this.Table.ColGuid.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableGuid(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(guid);
            Assert.AreEqual(guidString, this.Table.ColGuid.ReadAsString(reader.Object));

            Assert.AreEqual($"'{guidString}'", this.Table.ColGuid.FromString(guidString).ToSql());
            Assert.Throws<SqExpressException>(() => this.Table.ColGuid.FromString(null));
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

            var guid = new Guid("3E0F7FA1-E7CA-4F6E-BF19-69C398565EA2");
            var guidString = guid.ToString("D");

            Assert.IsNull(this.Table.ColNullableGuid.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableGuid(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns(guid);
            Assert.AreEqual(guidString, this.Table.ColNullableGuid.ReadAsString(reader.Object));

            Assert.AreEqual($"'{guidString}'", this.Table.ColNullableGuid.FromString(guidString).ToSql());
            Assert.AreEqual("NULL", this.Table.ColNullableGuid.FromString(null).ToSql());
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

            Assert.Throws<SqExpressException>(() => this.Table.ColString.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableString(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns("AbC");
            Assert.AreEqual("AbC", this.Table.ColString.ReadAsString(reader.Object));

            Assert.AreEqual($"'{"AbC"}'", this.Table.ColString.FromString("AbC").ToSql());
            Assert.Throws<SqExpressException>(() => this.Table.ColString.FromString(null));

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

            Assert.IsNull(this.Table.ColNullableString.ReadAsString(reader.Object));
            reader.Setup(r => r.GetNullableString(It.Is<string>(name => name == customColumn.ColumnName.Name))).Returns("AbC");
            Assert.AreEqual("AbC", this.Table.ColNullableString.ReadAsString(reader.Object));

            Assert.AreEqual($"'{"AbC"}'", this.Table.ColNullableString.FromString("AbC").ToSql());
            Assert.AreEqual("NULL", this.Table.ColNullableString.FromString(null).ToSql());
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
            this.ColDecimal = this.CreateDecimalColumn(nameof(ColDecimal), new DecimalPrecisionScale(10, 6));
            this.ColNullableDecimal = this.CreateNullableDecimalColumn(nameof(ColNullableDecimal), new DecimalPrecisionScale(10, 6));
            this.ColDouble = this.CreateDoubleColumn(nameof(ColDouble));
            this.ColNullableDouble = this.CreateNullableDoubleColumn(nameof(ColNullableDouble));
            this.ColDateTime = this.CreateDateTimeColumn(nameof(ColDateTime));
            this.ColNullableDateTime = this.CreateNullableDateTimeColumn(nameof(ColNullableDateTime));
            this.ColGuid = this.CreateGuidColumn(nameof(ColGuid));
            this.ColNullableGuid = this.CreateNullableGuidColumn(nameof(ColNullableGuid));
            this.ColString = this.CreateStringColumn(nameof(this.ColString), null, true);
            this.ColNullableString = this.CreateNullableStringColumn(nameof(this.ColNullableString), null, true);

        }

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