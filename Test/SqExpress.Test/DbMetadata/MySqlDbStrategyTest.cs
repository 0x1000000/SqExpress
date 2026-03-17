using NUnit.Framework;
using SqExpress.DbMetadata.Internal.DbManagers.MySql;
using SqExpress.DbMetadata.Internal.Model;
using SqExpress.SqlExport;
using System.Threading.Tasks;

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

        [Test]
        public void GetColType_Year_MapsToInt16()
        {
            var strategy = new MySqlDbStrategy(new TestSqDatabase(), "test", MySqlFlavor.MariaDb);
            var raw = new ColumnRawModel(
                new ColumnRef("sakila", "film", "release_year"),
                ordinalPosition: 1,
                identity: false,
                nullable: true,
                typeName: "year",
                defaultValue: null,
                size: null,
                precision: null,
                scale: null,
                extra: null
            );

            var colType = strategy.GetColType(raw);

            Assert.That(colType, Is.TypeOf<Int16ColumnType>());
            Assert.That(colType.IsNullable, Is.True);
        }

        [TestCase("double")]
        [TestCase("double precision")]
        public void GetColType_DoubleVariants_MapToDoubleColumnType(string typeName)
        {
            var strategy = new MySqlDbStrategy(new TestSqDatabase(), "test", MySqlFlavor.MariaDb);
            var raw = new ColumnRawModel(
                new ColumnRef("sakila", "film", "rating"),
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

            var colType = strategy.GetColType(raw);

            Assert.That(colType, Is.TypeOf<DoubleColumnType>());
            Assert.That(colType.IsNullable, Is.True);
        }

        [TestCase("set", 100, true, false)]
        [TestCase("json", null, true, false)]
        public void GetColType_StringFamilies_MapToStringColumnType(string typeName, int? size, bool expectedUnicode, bool expectedText)
        {
            var strategy = new MySqlDbStrategy(new TestSqDatabase(), "test", MySqlFlavor.MariaDb);
            var raw = new ColumnRawModel(
                new ColumnRef("sakila", "film", "tags"),
                ordinalPosition: 1,
                identity: false,
                nullable: true,
                typeName: typeName,
                defaultValue: null,
                size: size,
                precision: null,
                scale: null,
                extra: "utf8mb4"
            );

            var colType = (StringColumnType)strategy.GetColType(raw);

            Assert.That(colType.IsNullable, Is.True);
            Assert.That(colType.IsUnicode, Is.EqualTo(expectedUnicode));
            Assert.That(colType.IsFixed, Is.False);
            Assert.That(colType.IsText, Is.EqualTo(expectedText));
            Assert.That(colType.Size, Is.EqualTo(size));
        }

        [Test]
        public void GetColType_TinyBlob_MapsToVariableByteArray()
        {
            var strategy = new MySqlDbStrategy(new TestSqDatabase(), "test", MySqlFlavor.MariaDb);
            var raw = new ColumnRawModel(
                new ColumnRef("sakila", "film", "thumbnail"),
                ordinalPosition: 1,
                identity: false,
                nullable: true,
                typeName: "tinyblob",
                defaultValue: null,
                size: 255,
                precision: null,
                scale: null,
                extra: null
            );

            var colType = (ByteArrayColumnType)strategy.GetColType(raw);

            Assert.That(colType.IsNullable, Is.True);
            Assert.That(colType.IsFixed, Is.False);
            Assert.That(colType.Size, Is.EqualTo(255));
        }

        [Test]
        public async Task LoadRawModels_NullIndexCollation_DoesNotThrow()
        {
            var queryIndex = 0;
            var database = new TestSqDatabase(
                async (query, seed, aggregator) =>
                {
                    queryIndex++;
                    switch (queryIndex)
                    {
                        case 1:
                        case 2:
                            return seed;
                        case 3:
                            return aggregator(
                                seed,
                                new TestSqDataRecordReader(
                                    name =>
                                    {
                                        switch (name)
                                        {
                                            case "TABLE_SCHEMA":
                                                return "test";
                                            case "TABLE_NAME":
                                                return "Users";
                                            case "NON_UNIQUE":
                                                return 0L;
                                            case "INDEX_NAME":
                                                return "IX_Users_UserName";
                                            case "COLUMN_NAME":
                                                return "UserName";
                                            case "COLLATION":
                                                return null!;
                                            default:
                                                throw new AssertionException($"Unexpected column: {name}");
                                        }
                                    }
                                )
                            );
                        default:
                            throw new AssertionException($"Unexpected query count: {queryIndex}");
                    }
                }
            );
            var strategy = new MySqlDbStrategy(database, "test", MySqlFlavor.MariaDb);

            var rawModels = await strategy.LoadRawModels();

            Assert.That(rawModels.Indexes.Indexes.Count, Is.EqualTo(1));
            Assert.That(rawModels.Indexes.Indexes[new TableRef("test", "Users")][0].Columns[0].IsDescending, Is.False);
        }

        [Test]
        public void LoadRawModels_FunctionalIndex_ThrowsClearError()
        {
            var queryIndex = 0;
            var database = new TestSqDatabase(
                async (query, seed, aggregator) =>
                {
                    queryIndex++;
                    switch (queryIndex)
                    {
                        case 1:
                        case 2:
                            return seed;
                        case 3:
                            return aggregator(
                                seed,
                                new TestSqDataRecordReader(
                                    name =>
                                    {
                                        switch (name)
                                        {
                                            case "TABLE_SCHEMA":
                                                return "test";
                                            case "TABLE_NAME":
                                                return "Users";
                                            case "NON_UNIQUE":
                                                return 0L;
                                            case "INDEX_NAME":
                                                return "IX_Users_Func";
                                            case "COLUMN_NAME":
                                                return null!;
                                            case "COLLATION":
                                                return "A";
                                            default:
                                                throw new AssertionException($"Unexpected column: {name}");
                                        }
                                    }
                                )
                            );
                        default:
                            throw new AssertionException($"Unexpected query count: {queryIndex}");
                    }
                }
            );
            var strategy = new MySqlDbStrategy(database, "test", MySqlFlavor.MariaDb);

            var ex = Assert.ThrowsAsync<SqExpressException>(() => strategy.LoadRawModels());

            Assert.That(ex!.Message, Does.Contain("Functional or expression-based indexes are not supported"));
            Assert.That(ex.Message, Does.Contain("test.Users.IX_Users_Func"));
        }
    }
}
