using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SqExpress.SqlExport;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress.Test.Utils
{
    [TestFixture]
    public class TempTableBuilderTest
    {
        [Test]
        public void BasicTest()
        {
            var data = new[]
            {
                new
                {
                    Bool = true,
                    Byte = byte.MaxValue,
                    Int16 = short.MaxValue,
                    Int32 = int.MaxValue,
                    Int64 = long.MaxValue,
                    Decimal = decimal.MaxValue,
                    Double = 123.456,
                    String = "ABCD",
                    DateTime = new DateTime(2099, 1, 1),
                    Guid = Guid.Parse("37197F28-AAED-480B-BA66-2C4D67A57E33")
                },
                new
                {
                    Bool = true,
                    Byte = byte.MinValue,
                    Int16 = short.MinValue,
                    Int32 = int.MinValue,
                    Int64 = long.MinValue,
                    Decimal = decimal.MinValue,
                    Double = -123.456,
                    String = "ABCDABCD",
                    DateTime = new DateTime(1900, 1, 1),
                    Guid = Guid.Parse("E8C3620B-B8CD-4574-A074-ACE09AA3DA8A")
                },
            };

            List<List<ExprValue>> dataList = new List<List<ExprValue>>(data.Length);

            foreach (var row in data)
            {
                var l = new List<ExprValue>();

                l.Add(SqQueryBuilder.Literal(row.Bool));
                l.Add(SqQueryBuilder.Literal(row.Byte));
                l.Add(SqQueryBuilder.Literal(row.Int16));
                l.Add(SqQueryBuilder.Literal(row.Int32));
                l.Add(SqQueryBuilder.Literal(row.Int64));
                l.Add(SqQueryBuilder.Literal(row.Decimal));
                l.Add(SqQueryBuilder.Literal(row.Double));
                l.Add(SqQueryBuilder.Literal(row.String));
                l.Add(SqQueryBuilder.Literal(row.DateTime));
                l.Add(SqQueryBuilder.Literal(row.Guid));

                dataList.Add(l);
            }

            var derivedTable = SqQueryBuilder.Values(dataList).AsColumns("Bool","Byte", "Int16", "Int32", "Int64", "Decimal", "Double", "String", "DateTime", "Guid");

            var q = TempTableData.FromDerivedTableValuesInsert(derivedTable, new []{ derivedTable.Columns[2], derivedTable.Columns[3] }, out var table, name: "TestTmpTable");

            var sql = q.ToMySql();
            Assert.AreEqual(sql, "CREATE TEMPORARY TABLE `TestTmpTable`(`Bool` bit,`Byte` tinyint unsigned,`Int16` smallint,`Int32` int,`Int64` bigint,`Decimal` decimal(29,0),`Double` double,`String` varchar(8) character set utf8,`DateTime` datetime,`Guid` binary(16),CONSTRAINT PRIMARY KEY (`Int16`,`Int32`));INSERT INTO `TestTmpTable`(`Bool`,`Byte`,`Int16`,`Int32`,`Int64`,`Decimal`,`Double`,`String`,`DateTime`,`Guid`) VALUES (true,255,32767,2147483647,9223372036854775807,79228162514264337593543950335,123.456,'ABCD','2099-01-01',0x287F1937EDAA0B48BA662C4D67A57E33),(true,0,-32768,-2147483648,-9223372036854775808,-79228162514264337593543950335,-123.456,'ABCDABCD','1900-01-01',0x0B62C3E8CDB87445A074ACE09AA3DA8A)");

            sql = q.ToSql();
            Assert.AreEqual(sql, "CREATE TABLE [#TestTmpTable]([Bool] bit,[Byte] tinyint,[Int16] smallint,[Int32] int,[Int64] bigint,[Decimal] decimal(29,0),[Double] float,[String] [nvarchar](8),[DateTime] datetime,[Guid] uniqueidentifier,CONSTRAINT [PK_TestTmpTable] PRIMARY KEY ([Int16],[Int32]));INSERT INTO [#TestTmpTable]([Bool],[Byte],[Int16],[Int32],[Int64],[Decimal],[Double],[String],[DateTime],[Guid]) VALUES (1,255,32767,2147483647,9223372036854775807,79228162514264337593543950335,123.456,'ABCD','2099-01-01','37197f28-aaed-480b-ba66-2c4d67a57e33'),(1,0,-32768,-2147483648,-9223372036854775808,-79228162514264337593543950335,-123.456,'ABCDABCD','1900-01-01','e8c3620b-b8cd-4574-a074-ace09aa3da8a')");
            
        }

        [Test]
        public void CalcStringSize()
        {
            var data = new string[] { "123456789", "1234", "1234567" };

            var derivedTable = SqQueryBuilder.Values(data.Select(SqQueryBuilder.Literal).ToList()).AsColumns("Str");

            var tmpQuery = TempTableData.FromDerivedTableValuesInsert(derivedTable, new[] { derivedTable.Columns[0] }, out _, name: "tmpTable");

            var sql = tmpQuery.ToMySql();
            
            Assert.AreEqual("CREATE TEMPORARY TABLE `tmpTable`(`Str` varchar(9) character set utf8,CONSTRAINT PRIMARY KEY (`Str`));INSERT INTO `tmpTable`(`Str`) VALUES ('123456789'),('1234'),('1234567')", sql);
        }

        [Test]
        public void CalcDecimalPrecisionScale_BasicTest()
        {
            var ps = Helpers.CalcDecimalPrecisionScale(123.21m);

            Assert.AreEqual(5, ps.Precision);
            Assert.AreEqual(2, ps.Scale);

            ps = Helpers.CalcDecimalPrecisionScale(123.210m);

            Assert.AreEqual(6, ps.Precision);
            Assert.AreEqual(3, ps.Scale);


            ps = Helpers.CalcDecimalPrecisionScale(123123.210m);

            Assert.AreEqual(9, ps.Precision);
            Assert.AreEqual(3, ps.Scale);
        }
    }
}