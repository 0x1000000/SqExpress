using System;
using System.Linq;
using NUnit.Framework;
using SqExpress.DbMetadata;
using SqExpress.Syntax.Value;

namespace SqExpress.Test.Meta
{
    [TestFixture]
    public class TableComparisonExtensionsTest
    {
        [Test]
        public void CompareWith_Table_WhenEqual_ReturnsNull()
        {
            var left = SqTable.Create("dbo", "Users", a => a
                .AppendInt32Column("Id")
                .AppendStringColumn("Name", 255, isUnicode: true));
            var right = SqTable.Create("dbo", "Users", a => a
                .AppendInt32Column("Id")
                .AppendStringColumn("Name", 255, isUnicode: true));

            var diff = left.CompareWith(right);

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_Table_WhenMissingColumn_ReturnsMissedColumn()
        {
            var expected = SqTable.Create("dbo", "Users", a => a
                .AppendInt32Column("Id")
                .AppendStringColumn("Name", 255, isUnicode: true));
            var actual = SqTable.Create("dbo", "Users", a => a
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
            var expected = SqTable.Create("dbo", "Users", a => a
                .AppendInt32Column("Id"));
            var actual = SqTable.Create("dbo", "Users", a => a
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
            var expected = SqTable.Create("dbo", "Users", a => a
                .AppendInt32Column("Id"));
            var actual = SqTable.Create("dbo", "Users", a => a
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
            var expected = SqTable.Create("dbo", "Users", a => a
                .AppendInt32Column("Id"));
            var actual = SqTable.Create("dbo", "Users", a => a
                .AppendNullableInt32Column("Id"));

            var diff = expected.CompareWith(actual);

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.DifferentColumns.Count, Is.EqualTo(1));
            Assert.That(diff.DifferentColumns[0].ColumnComparison.HasFlag(TableColumnComparison.DifferentNullability), Is.True);
        }

        [Test]
        public void CompareWith_Table_WhenColumnArgumentsDiffer_ReturnsDifferentArguments()
        {
            var expected = SqTable.Create("dbo", "Users", a => a
                .AppendStringColumn("Name", 128, isUnicode: true));
            var actual = SqTable.Create("dbo", "Users", a => a
                .AppendStringColumn("Name", 64, isUnicode: true));

            var diff = expected.CompareWith(actual);

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.DifferentColumns.Count, Is.EqualTo(1));
            Assert.That(diff.DifferentColumns[0].ColumnComparison.HasFlag(TableColumnComparison.DifferentArguments), Is.True);
        }

        [Test]
        public void CompareWith_Table_WhenColumnMetaDiffers_ReturnsDifferentMeta()
        {
            var expected = SqTable.Create("dbo", "Users", a => a
                .AppendInt32Column("Id", ColumnMeta.PrimaryKey()));
            var actual = SqTable.Create("dbo", "Users", a => a
                .AppendInt32Column("Id"));

            var diff = expected.CompareWith(actual);

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.DifferentColumns.Count, Is.EqualTo(1));
            Assert.That(diff.DifferentColumns[0].ColumnComparison.HasFlag(TableColumnComparison.DifferentMeta), Is.True);
        }

        [Test]
        public void CompareWith_Table_WhenColumnOrderDiffers_ReturnsNull()
        {
            var left = SqTable.Create("dbo", "Users", a => a
                .AppendInt32Column("Id")
                .AppendStringColumn("Name", 255, isUnicode: true)
                .AppendBooleanColumn("IsActive"));
            var right = SqTable.Create("dbo", "Users", a => a
                .AppendBooleanColumn("IsActive")
                .AppendStringColumn("Name", 255, isUnicode: true)
                .AppendInt32Column("Id"));

            var diff = left.CompareWith(right);

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_Table_WhenColumnNameDiffersOnlyByCase_ReturnsMissedAndExtraColumns()
        {
            var expected = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"));
            var actual = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("ID"));

            var diff = expected.CompareWith(actual);

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.MissedColumns.Count, Is.EqualTo(1));
            Assert.That(diff.ExtraColumns.Count, Is.EqualTo(1));
            Assert.That(diff.DifferentColumns.Count, Is.EqualTo(0));
        }

        [Test]
        public void CompareWith_TableList_WhenMissingTable_ReturnsMissedTable()
        {
            var users = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"));
            var orders = SqTable.Create("dbo", "Orders", a => a.AppendInt32Column("OrderId"));

            var diff = new TableBase[] { users, orders }.CompareWith(new TableBase[] { users });

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.MissedTables.Count, Is.EqualTo(1));
            Assert.That(diff.MissedTables[0].FullName.AsExprTableFullName().TableName.Name, Is.EqualTo("Orders"));
            Assert.That(diff.ExtraTables.Count, Is.EqualTo(0));
        }

        [Test]
        public void CompareWith_TableList_WhenExtraTable_ReturnsExtraTable()
        {
            var users = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"));
            var orders = SqTable.Create("dbo", "Orders", a => a.AppendInt32Column("OrderId"));

            var diff = new TableBase[] { users }.CompareWith(new TableBase[] { users, orders });

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.ExtraTables.Count, Is.EqualTo(1));
            Assert.That(diff.ExtraTables[0].FullName.AsExprTableFullName().TableName.Name, Is.EqualTo("Orders"));
            Assert.That(diff.MissedTables.Count, Is.EqualTo(0));
        }

        [Test]
        public void CompareWith_TableList_WhenOtherListEmpty_ReturnsAllMissed()
        {
            var users = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"));
            var orders = SqTable.Create("dbo", "Orders", a => a.AppendInt32Column("OrderId"));

            var diff = new TableBase[] { users, orders }.CompareWith(Array.Empty<TableBase>());

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.MissedTables.Count, Is.EqualTo(2));
            Assert.That(diff.ExtraTables.Count, Is.EqualTo(0));
        }

        [Test]
        public void CompareWith_TableList_WhenThisListEmpty_ReturnsAllExtra()
        {
            var users = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"));
            var orders = SqTable.Create("dbo", "Orders", a => a.AppendInt32Column("OrderId"));

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
                SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"))
            };
            var actual = new TableBase[]
            {
                SqTable.Create("sales", "Users", a => a.AppendInt32Column("Id"))
            };

            var diff = expected.CompareWith(actual, fullName => fullName.AsExprTableFullName().TableName.Name);

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_TableList_WhenSchemaDiffersAndNoCustomKey_ReturnsMismatch()
        {
            var expected = new TableBase[]
            {
                SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"))
            };
            var actual = new TableBase[]
            {
                SqTable.Create("sales", "Users", a => a.AppendInt32Column("Id"))
            };

            var diff = expected.CompareWith(actual);

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.MissedTables.Count, Is.EqualTo(1));
            Assert.That(diff.ExtraTables.Count, Is.EqualTo(1));
        }

        [Test]
        public void CompareWith_TableList_WhenBothListsEqual_ReturnsNull()
        {
            var users = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"));
            var orders = SqTable.Create("dbo", "Orders", a => a.AppendInt32Column("OrderId"));

            var diff = new TableBase[] { users, orders }.CompareWith(new TableBase[] { users, orders });

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_TableList_WhenIgnoreSchema_MatchesSameTable()
        {
            var expected = new TableBase[]
            {
                SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"))
            };
            var actual = new TableBase[]
            {
                SqTable.Create("sales", "Users", a => a.AppendInt32Column("Id"))
            };

            var diff = expected.CompareWith(actual, TableComparisonFlags.IgnoreSchema);

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_TableList_WhenIgnoreDatabase_MatchesSameTable()
        {
            var expected = new TableBase[]
            {
                SqTable.Create("MainDb", "dbo", "Users", a => a.AppendInt32Column("Id"))
            };
            var actual = new TableBase[]
            {
                SqTable.Create("ArchiveDb", "dbo", "Users", a => a.AppendInt32Column("Id"))
            };

            var diff = expected.CompareWith(actual, TableComparisonFlags.IgnoreDatabase);

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_TableList_WhenCustomKeyExtractorProvided_TakesPrecedenceOverFlags()
        {
            var expected = new TableBase[]
            {
                SqTable.Create("MainDb", "dbo", "Users", a => a.AppendInt32Column("Id"))
            };
            var actual = new TableBase[]
            {
                SqTable.Create("ArchiveDb", "sales", "Users", a => a.AppendInt32Column("Id"))
            };

            var diff = expected.CompareWith(
                actual,
                TableComparisonFlags.Strict,
                fullName => fullName.AsExprTableFullName().TableName.Name);

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_Table_WhenIgnoreIndexes_ReturnsNullForIndexOnlyDifference()
        {
            var expected = SqTable.Create(
                "dbo",
                "Users",
                a => a.AppendInt32Column("Id"),
                i => i.AppendIndex(i.Asc("Id")));
            var actual = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"));

            var diff = expected.CompareWith(actual, TableComparisonFlags.IgnoreIndexes);

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_Table_WhenIgnoreColumnTypes_ReturnsNullForTypeDifference()
        {
            var expected = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"));
            var actual = SqTable.Create("dbo", "Users", a => a.AppendStringColumn("Id", 255, isUnicode: true));

            var diff = expected.CompareWith(actual, TableComparisonFlags.IgnoreColumnTypes);

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_Table_WhenIgnoreColumnTypeArguments_ReturnsNullForArgumentDifferenceButNotTypeDifference()
        {
            var sizeExpected = SqTable.Create("dbo", "Users", a => a.AppendStringColumn("Name", 128, isUnicode: true));
            var sizeActual = SqTable.Create("dbo", "Users", a => a.AppendStringColumn("Name", 64, isUnicode: true));

            var sizeDiff = sizeExpected.CompareWith(sizeActual, TableComparisonFlags.IgnoreColumnTypeArguments);

            Assert.That(sizeDiff, Is.Null);

            var typeExpected = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Name"));
            var typeActual = SqTable.Create("dbo", "Users", a => a.AppendStringColumn("Name", 64, isUnicode: true));

            var typeDiff = typeExpected.CompareWith(typeActual, TableComparisonFlags.IgnoreColumnTypeArguments);

            Assert.That(typeDiff, Is.Not.Null);
            Assert.That(typeDiff!.DifferentColumns.Count, Is.EqualTo(1));
            Assert.That(typeDiff.DifferentColumns[0].ColumnComparison.HasFlag(TableColumnComparison.DifferentType), Is.True);
        }

        [Test]
        public void CompareWith_Table_WhenIgnoreColumnNullability_ReturnsNullForNullabilityDifference()
        {
            var expected = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"));
            var actual = SqTable.Create("dbo", "Users", a => a.AppendNullableInt32Column("Id"));

            var diff = expected.CompareWith(actual, TableComparisonFlags.IgnoreColumnNullability);

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_Table_WhenIgnoreColumnMeta_ReturnsNullForMetaDifference()
        {
            var expected = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id", ColumnMeta.PrimaryKey().Identity().DefaultValue(1)));
            var actual = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"));

            var diff = expected.CompareWith(actual, TableComparisonFlags.IgnoreColumnMeta);

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_Table_WhenIgnoreColumnPrimaryKey_OnlySuppressesPrimaryKeyDifference()
        {
            var expected = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id", ColumnMeta.PrimaryKey().DefaultValue(1)));
            var actual = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"));

            var diff = expected.CompareWith(actual, TableComparisonFlags.IgnoreColumnPrimaryKey);

            Assert.That(diff, Is.Not.Null);
            Assert.That(diff!.DifferentColumns.Count, Is.EqualTo(1));
            Assert.That(diff.DifferentColumns[0].ColumnComparison.HasFlag(TableColumnComparison.DifferentMeta), Is.True);
        }

        [Test]
        public void Includes_WhenOtherIsSubset_ReturnsTrue()
        {
            var superset = new TableBase[]
            {
                SqTable.Create("dbo", "Users", a => a
                    .AppendInt32Column("Id")
                    .AppendStringColumn("Name", 255, isUnicode: true)
                    .AppendBooleanColumn("IsActive")),
                SqTable.Create("dbo", "Orders", a => a.AppendInt32Column("OrderId"))
            };
            var subset = new TableBase[]
            {
                SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"))
            };

            var includes = superset.Includes(subset);

            Assert.That(includes, Is.True);
        }

        [Test]
        public void Includes_WhenOnlyIndexesAndMetaDiffer_ReturnsTrue()
        {
            var superset = new TableBase[]
            {
                SqTable.Create(
                    "dbo",
                    "Users",
                    a => a.AppendInt32Column("Id"),
                    i => i.AppendIndex(i.Asc("Id")))
            };
            var subset = new TableBase[]
            {
                SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id", ColumnMeta.PrimaryKey().DefaultValue(1)))
            };

            var includes = superset.Includes(subset);

            Assert.That(includes, Is.True);
        }

        [Test]
        public void Includes_WhenOtherHasMissingTable_ReturnsFalse()
        {
            var superset = new TableBase[]
            {
                SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"))
            };
            var subset = new TableBase[]
            {
                SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id")),
                SqTable.Create("dbo", "Orders", a => a.AppendInt32Column("OrderId"))
            };

            var includes = superset.Includes(subset);

            Assert.That(includes, Is.False);
        }

        [Test]
        public void Includes_WhenOtherHasMissingColumn_ReturnsFalse()
        {
            var superset = new TableBase[]
            {
                SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"))
            };
            var subset = new TableBase[]
            {
                SqTable.Create("dbo", "Users", a => a
                    .AppendInt32Column("Id")
                    .AppendStringColumn("Name", 255, isUnicode: true))
            };

            var includes = superset.Includes(subset);

            Assert.That(includes, Is.False);
        }

        [Test]
        public void Includes_WhenTypeMismatchExists_ReturnsFalseUnlessIgnoreColumnShapeProvided()
        {
            var superset = new TableBase[]
            {
                SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"))
            };
            var subset = new TableBase[]
            {
                SqTable.Create("dbo", "Users", a => a.AppendStringColumn("Id", 255, isUnicode: true))
            };

            Assert.That(superset.Includes(subset), Is.False);
            Assert.That(superset.Includes(subset, TableIncludesFlags.IgnoreColumnShape), Is.True);
        }

        [Test]
        public void Includes_WhenIgnoreSchemaProvided_UsesBuiltInMatchingRule()
        {
            var superset = new TableBase[]
            {
                SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"))
            };
            var subset = new TableBase[]
            {
                SqTable.Create("sales", "Users", a => a.AppendInt32Column("Id"))
            };

            var includes = superset.Includes(subset, TableIncludesFlags.IgnoreSchema);

            Assert.That(includes, Is.True);
        }

        [Test]
        public void Includes_WhenCustomKeyExtractorProvided_UsesExtractor()
        {
            var superset = new TableBase[]
            {
                SqTable.Create("MainDb", "dbo", "Users", a => a.AppendInt32Column("Id"))
            };
            var subset = new TableBase[]
            {
                SqTable.Create("ArchiveDb", "sales", "Users", a => a.AppendInt32Column("Id"))
            };

            var includes = superset.Includes(
                subset,
                TableIncludesFlags.Strict,
                fullName => fullName.AsExprTableFullName().TableName.Name);

            Assert.That(includes, Is.True);
        }

        [Test]
        public void Includes_WhenNullabilityMismatchExists_ReturnsFalseUnlessIgnoreNullabilityProvided()
        {
            var superset = new TableBase[]
            {
                SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"))
            };
            var subset = new TableBase[]
            {
                SqTable.Create("dbo", "Users", a => a.AppendNullableInt32Column("Id"))
            };

            Assert.That(superset.Includes(subset), Is.False);
            Assert.That(superset.Includes(subset, TableIncludesFlags.IgnoreColumnNullability), Is.True);
        }

        [Test]
        public void Includes_WhenDefaultValueMismatchExists_ReturnsTrueByDefaultAndWithDedicatedFlags()
        {
            var superset = new TableBase[]
            {
                SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id", ColumnMeta.DefaultValue(1)))
            };
            var subset = new TableBase[]
            {
                SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id", ColumnMeta.DefaultValue(2)))
            };

            Assert.That(superset.Includes(subset), Is.True);
            Assert.That(superset.Includes(subset, TableIncludesFlags.IgnoreColumnDefaultValues), Is.True);
        }

        [Test]
        public void CompareWith_Table_WhenIgnoreMissingColumns_ReturnsNullForMissingColumnDifference()
        {
            var expected = SqTable.Create("dbo", "Users", a => a
                .AppendInt32Column("Id")
                .AppendStringColumn("Name", 255, isUnicode: true));
            var actual = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"));

            var diff = expected.CompareWith(actual, TableComparisonFlags.IgnoreMissingColumns);

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_Table_WhenIgnoreExtraColumns_ReturnsNullForExtraColumnDifference()
        {
            var expected = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id"));
            var actual = SqTable.Create("dbo", "Users", a => a
                .AppendInt32Column("Id")
                .AppendStringColumn("Name", 255, isUnicode: true));

            var diff = expected.CompareWith(actual, TableComparisonFlags.IgnoreExtraColumns);

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_Table_WhenIgnoreColumnIdentity_OnlySuppressesIdentityDifference()
        {
            var expected = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id", ColumnMeta.Identity().DefaultValue(1)));
            var actual = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id", ColumnMeta.DefaultValue(1)));

            var diff = expected.CompareWith(actual, TableComparisonFlags.IgnoreColumnIdentity);

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_Table_WhenIgnoreColumnForeignKeys_OnlySuppressesForeignKeyDifference()
        {
            var expected = new OrdersWithForeignKeyTable();
            var actual = new OrdersWithoutForeignKeyTable();

            var diff = expected.CompareWith(actual, TableComparisonFlags.IgnoreColumnForeignKeys);

            Assert.That(diff, Is.Null);
        }

        [Test]
        public void CompareWith_Table_WhenIgnoreColumnDefaultValues_OnlySuppressesDefaultValueDifference()
        {
            var expected = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id", ColumnMeta.DefaultValue(1)));
            var actual = SqTable.Create("dbo", "Users", a => a.AppendInt32Column("Id", ColumnMeta.DefaultValue(2)));

            var diff = expected.CompareWith(actual, TableComparisonFlags.IgnoreColumnDefaultValues);

            Assert.That(diff, Is.Null);
        }

        private class UsersTable : TableBase
        {
            public UsersTable() : this(default)
            {
            }

            public UsersTable(Alias alias = default) : base("meta", "UsersFk", alias)
            {
                this.Id = this.CreateInt32Column("Id", ColumnMeta.PrimaryKey());
            }

            public Int32TableColumn Id { get; }
        }

        private class OrdersWithForeignKeyTable : TableBase
        {
            public OrdersWithForeignKeyTable() : this(default)
            {
            }

            public OrdersWithForeignKeyTable(Alias alias = default) : base("meta", "OrdersFk", alias)
            {
                this.UserId = this.CreateInt32Column("UserId", ColumnMeta.DefaultValue(1).ForeignKey<UsersTable>(u => u.Id));
            }

            public Int32TableColumn UserId { get; }
        }

        private class OrdersWithoutForeignKeyTable : TableBase
        {
            public OrdersWithoutForeignKeyTable() : this(default)
            {
            }

            public OrdersWithoutForeignKeyTable(Alias alias = default) : base("meta", "OrdersFk", alias)
            {
                this.UserId = this.CreateInt32Column("UserId", ColumnMeta.DefaultValue(1));
            }

            public Int32TableColumn UserId { get; }
        }
    }
}

