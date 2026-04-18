using NUnit.Framework;
using SqExpress.DbMetadata.Internal.DbManagers.Sqlite;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.Test.DbMetadata
{
    [TestFixture]
    public class SqliteDbStrategyTest
    {
        [TestCase("BIGINT", typeof(Int64ColumnType))]
        [TestCase("SMALLINT", typeof(Int16ColumnType))]
        [TestCase("TINYINT", typeof(ByteColumnType))]
        [TestCase("INTEGER", typeof(Int32ColumnType))]
        public void TryGetColType_IntegerFamilies_PreserveDeclaredIntent(string typeName, System.Type expectedType)
        {
            var strategy = new SqliteDbStrategy(new TestSqDatabase(), "main", null!);
            var raw = new ColumnRawModel(
                new ColumnRef("dbo", "Sample", "Value"),
                ordinalPosition: 1,
                identity: false,
                nullable: true,
                typeName: typeName,
                defaultValue: null,
                size: null,
                precision: null,
                scale: null,
                extra: null
            );

            var colType = strategy.TryGetColType(raw);

            Assert.That(colType, Is.Not.Null);
            Assert.That(colType, Is.TypeOf(expectedType));
            Assert.That(colType!.IsNullable, Is.True);
        }
    }
}
