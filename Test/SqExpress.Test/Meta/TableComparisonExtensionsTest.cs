using System;
using NUnit.Framework;
using SqExpress.DbMetadata;

namespace SqExpress.Test.Meta
{
    [TestFixture]
    public class TableComparisonExtensionsTest
    {
        [Test]
        public void CompareWith_Table_WhenEqual_ReturnsNull()
        {
            var left = CreateTable("dbo", "Users", a => a
                .AppendInt32Column("Id")
                .AppendStringColumn("Name", 255, isUnicode: true));
            var right = CreateTable("dbo", "Users", a => a
                .AppendInt32Column("Id")
                .AppendStringColumn("Name", 255, isUnicode: true));

            var diff = left.CompareWith(right);

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_Table_WhenMissingColumn_ReturnsMissedColumn()
        {
            var expected = CreateTable("dbo", "Users", a => a
                .AppendInt32Column("Id")
                .AppendStringColumn("Name", 255, isUnicode: true));
            var actual = CreateTable("dbo", "Users", a => a
                .AppendInt32Column("Id"));

            var diff = expected.CompareWith(actual);

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.MissedColumns.Count, Is.EqualTo(1));
            Assert.That(diff.MissedColumns[0].ColumnName.Name, Is.EqualTo("Name"));
            Assert.That(diff.ExtraColumns.Count, Is.EqualTo(0));
        }

        [Test]
        public void CompareWith_Table_WhenExtraColumn_ReturnsExtraColumn()
        {
            var expected = CreateTable("dbo", "Users", a => a
                .AppendInt32Column("Id"));
            var actual = CreateTable("dbo", "Users", a => a
                .AppendInt32Column("Id")
                .AppendStringColumn("Name", 255, isUnicode: true));

            var diff = expected.CompareWith(actual);

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.ExtraColumns.Count, Is.EqualTo(1));
            Assert.That(diff.ExtraColumns[0].ColumnName.Name, Is.EqualTo("Name"));
            Assert.That(diff.MissedColumns.Count, Is.EqualTo(0));
        }

        [Test]
        public void CompareWith_Table_WhenColumnTypeDiffers_ReturnsDifferentType()
        {
            var expected = CreateTable("dbo", "Users", a => a
                .AppendInt32Column("Id"));
            var actual = CreateTable("dbo", "Users", a => a
                .AppendStringColumn("Id", 255, isUnicode: true));

            var diff = expected.CompareWith(actual);

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.DifferentColumns.Count, Is.EqualTo(1));
            Assert.That(diff.DifferentColumns[0].Column.ColumnName.Name, Is.EqualTo("Id"));
            Assert.That(diff.DifferentColumns[0].ColumnComparison.HasFlag(TableColumnComparison.DifferentType), Is.True);
        }

        [Test]
        public void CompareWith_Table_WhenColumnNullabilityDiffers_ReturnsDifferentNullability()
        {
            var expected = CreateTable("dbo", "Users", a => a
                .AppendInt32Column("Id"));
            var actual = CreateTable("dbo", "Users", a => a
                .AppendNullableInt32Column("Id"));

            var diff = expected.CompareWith(actual);

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.DifferentColumns.Count, Is.EqualTo(1));
            Assert.That(diff.DifferentColumns[0].ColumnComparison.HasFlag(TableColumnComparison.DifferentNullability), Is.True);
        }

        [Test]
        public void CompareWith_Table_WhenColumnArgumentsDiffer_ReturnsDifferentArguments()
        {
            var expected = CreateTable("dbo", "Users", a => a
                .AppendStringColumn("Name", 128, isUnicode: true));
            var actual = CreateTable("dbo", "Users", a => a
                .AppendStringColumn("Name", 64, isUnicode: true));

            var diff = expected.CompareWith(actual);

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.DifferentColumns.Count, Is.EqualTo(1));
            Assert.That(diff.DifferentColumns[0].ColumnComparison.HasFlag(TableColumnComparison.DifferentArguments), Is.True);
        }

        [Test]
        public void CompareWith_Table_WhenColumnMetaDiffers_ReturnsDifferentMeta()
        {
            var expected = CreateTable("dbo", "Users", a => a
                .AppendInt32Column("Id", ColumnMeta.PrimaryKey()));
            var actual = CreateTable("dbo", "Users", a => a
                .AppendInt32Column("Id"));

            var diff = expected.CompareWith(actual);

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.DifferentColumns.Count, Is.EqualTo(1));
            Assert.That(diff.DifferentColumns[0].ColumnComparison.HasFlag(TableColumnComparison.DifferentMeta), Is.True);
        }

        [Test]
        public void CompareWith_Table_WhenColumnOrderDiffers_ReturnsNull()
        {
            var left = CreateTable("dbo", "Users", a => a
                .AppendInt32Column("Id")
                .AppendStringColumn("Name", 255, isUnicode: true)
                .AppendBooleanColumn("IsActive"));
            var right = CreateTable("dbo", "Users", a => a
                .AppendBooleanColumn("IsActive")
                .AppendStringColumn("Name", 255, isUnicode: true)
                .AppendInt32Column("Id"));

            var diff = left.CompareWith(right);

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_Table_WhenColumnNameDiffersOnlyByCase_ReturnsMissedAndExtraColumns()
        {
            var expected = CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"));
            var actual = CreateTable("dbo", "Users", a => a.AppendInt32Column("ID"));

            var diff = expected.CompareWith(actual);

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.MissedColumns.Count, Is.EqualTo(1));
            Assert.That(diff.ExtraColumns.Count, Is.EqualTo(1));
            Assert.That(diff.DifferentColumns.Count, Is.EqualTo(0));
        }

        [Test]
        public void CompareWith_TableList_WhenMissingTable_ReturnsMissedTable()
        {
            var users = CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"));
            var orders = CreateTable("dbo", "Orders", a => a.AppendInt32Column("OrderId"));

            var diff = new TableBase[] { users, orders }.CompareWith(new TableBase[] { users });

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.MissedTables.Count, Is.EqualTo(1));
            Assert.That(diff.MissedTables[0].FullName.AsExprTableFullName().TableName.Name, Is.EqualTo("Orders"));
            Assert.That(diff.ExtraTables.Count, Is.EqualTo(0));
        }

        [Test]
        public void CompareWith_TableList_WhenExtraTable_ReturnsExtraTable()
        {
            var users = CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"));
            var orders = CreateTable("dbo", "Orders", a => a.AppendInt32Column("OrderId"));

            var diff = new TableBase[] { users }.CompareWith(new TableBase[] { users, orders });

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.ExtraTables.Count, Is.EqualTo(1));
            Assert.That(diff.ExtraTables[0].FullName.AsExprTableFullName().TableName.Name, Is.EqualTo("Orders"));
            Assert.That(diff.MissedTables.Count, Is.EqualTo(0));
        }

        [Test]
        public void CompareWith_TableList_WhenOtherListEmpty_ReturnsAllMissed()
        {
            var users = CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"));
            var orders = CreateTable("dbo", "Orders", a => a.AppendInt32Column("OrderId"));

            var diff = new TableBase[] { users, orders }.CompareWith(Array.Empty<TableBase>());

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.MissedTables.Count, Is.EqualTo(2));
            Assert.That(diff.ExtraTables.Count, Is.EqualTo(0));
        }

        [Test]
        public void CompareWith_TableList_WhenThisListEmpty_ReturnsAllExtra()
        {
            var users = CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"));
            var orders = CreateTable("dbo", "Orders", a => a.AppendInt32Column("OrderId"));

            var diff = Array.Empty<TableBase>().CompareWith(new TableBase[] { users, orders });

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.ExtraTables.Count, Is.EqualTo(2));
            Assert.That(diff.MissedTables.Count, Is.EqualTo(0));
        }

        [Test]
        public void CompareWith_TableList_WhenUsingCustomKeyExtractor_CanIgnoreSchema()
        {
            var expected = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"))
            };
            var actual = new TableBase[]
            {
                CreateTable("sales", "Users", a => a.AppendInt32Column("Id"))
            };

            var diff = expected.CompareWith(actual, fullName => fullName.AsExprTableFullName().TableName.Name);

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_TableList_WhenSchemaDiffersAndNoCustomKey_ReturnsMismatch()
        {
            var expected = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"))
            };
            var actual = new TableBase[]
            {
                CreateTable("sales", "Users", a => a.AppendInt32Column("Id"))
            };

            var diff = expected.CompareWith(actual);

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.MissedTables.Count, Is.EqualTo(1));
            Assert.That(diff.ExtraTables.Count, Is.EqualTo(1));
        }

        [Test]
        public void CompareWith_TableList_WhenBothListsEqual_ReturnsNull()
        {
            var users = CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"));
            var orders = CreateTable("dbo", "Orders", a => a.AppendInt32Column("OrderId"));

            var diff = new TableBase[] { users, orders }.CompareWith(new TableBase[] { users, orders });

            Assert.That(diff, Is.Null);
        }

        private static SqTable CreateTable(
            string schema,
            string tableName,
            Func<ITableColumnAppender, ITableColumnAppender> columns)
            => SqTable.Create(schema, tableName, a => columns(a));
    }
}

