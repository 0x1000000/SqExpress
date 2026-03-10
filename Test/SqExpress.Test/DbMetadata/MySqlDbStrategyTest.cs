using NUnit.Framework;
using SqExpress.DbMetadata.Internal.DbManagers.MySql;
using SqExpress.DbMetadata.Internal.Model;
using SqExpress.SqlExport;

namespace SqExpress.Test.DbMetadata
{
    [TestFixture]
    public class MySqlDbStrategyTest
    {
        [TestCase("utf8mb3", 21844, null)]
        [TestCase("utf8mb4", 16383, null)]
        [TestCase("utf8mb3", 20000, 20000)]
        [TestCase("utf8mb4", 20000, 20000)]
        public void GetColType_UnicodeVarchar_PreservesExplicitSize(string charset, int rawSize, int? expectedSize)
        {
            var strategy = new MySqlDbStrategy(new TestSqDatabase(), "test", MySqlFlavor.MariaDb);
            var raw = new ColumnRawModel(
                new ColumnRef("test", "Sample", "Name"),
                ordinalPosition: 1,
                identity: false,
                nullable: false,
                typeName: "varchar",
                defaultValue: null,
                size: rawSize,
                precision: null,
                scale: null,
                extra: charset
            );

            var colType = (StringColumnType)strategy.GetColType(raw);

            Assert.That(colType.IsUnicode, Is.True);
            Assert.That(colType.Size, Is.EqualTo(expectedSize));
            Assert.That(colType.IsText, Is.False);
            Assert.That(colType.IsFixed, Is.False);
        }
    }
}
