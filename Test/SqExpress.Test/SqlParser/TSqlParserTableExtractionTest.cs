using System;
using System.Linq;
using NUnit.Framework;
using SqExpress.DbMetadata;
using SqExpress.SqlParser;
using SqExpress.Syntax;
using SqExpress.Syntax.Type;

namespace SqExpress.Test.SqlParser
{
    public class TSqlParserTableExtractionTest
    {
        [Test]
        public void ExtractTables_CteAndSubQuery_InfersTypesAndSkipsCtePseudoTable()
        {
            const string sql =
                "wItH C aS(SeLeCt o.UserId,o.Amount FrOm dbo.Orders o WhErE o.Title LiKe 'A%') " +
                "SeLeCt u.UserId,u.Name,c.Amount " +
                "fRoM dbo.Users u " +
                "JoIn (SeLeCt x.UserId,x.Amount FrOm C x) c On c.UserId=u.UserId " +
                "wHeRe u.CreatedAt>='2025-01-01' aNd u.IsActive=1";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var tables, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(tables, Is.Not.Null);
            var actualTables = tables!;
            Assert.That(actualTables.Select(i => i.FullName.TableName).ToArray(), Is.EquivalentTo(new[] { "Users", "Orders" }));
            Assert.That(actualTables.Any(i => string.Equals(i.FullName.TableName, "C", StringComparison.OrdinalIgnoreCase)), Is.False);

            var users = actualTables.Single(i => i.FullName.TableName == "Users");
            AssertInt32(users, "UserId");
            AssertString(users, "Name", 255);
            AssertDateTime(users, "CreatedAt");
            AssertBoolean(users, "IsActive");

            var orders = actualTables.Single(i => i.FullName.TableName == "Orders");
            AssertInt32(orders, "UserId");
            AssertDecimal(orders, "Amount");
            AssertString(orders, "Title", 255);
        }

        [Test]
        public void ExtractTables_Merge_DetectsTargetAndSourceWithTypes()
        {
            const string sql =
                "mErGe dbo.Users t " +
                "uSiNg (sElEcT s.UserId,s.Name,s.Balance FrOm dbo.UsersStaging s) src " +
                "oN t.UserId=src.UserId " +
                "wHeN mAtChEd tHeN uPdAtE sEt t.Name=src.Name,t.UpdatedAt='2025-01-02' " +
                "wHeN nOt mAtChEd tHeN iNsErT(UserId,Name,Balance) vAlUeS(src.UserId,src.Name,src.Balance);";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var tables, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(tables, Is.Not.Null);
            var actualTables = tables!;
            Assert.That(actualTables.Select(i => i.FullName.TableName).ToArray(), Is.EquivalentTo(new[] { "Users", "UsersStaging" }));

            var users = actualTables.Single(i => i.FullName.TableName == "Users");
            AssertInt32(users, "UserId");
            AssertString(users, "Name", 255);
            AssertDateTime(users, "UpdatedAt");

            var staging = actualTables.Single(i => i.FullName.TableName == "UsersStaging");
            AssertInt32(staging, "UserId");
            AssertString(staging, "Name", 255);
            AssertDecimal(staging, "Balance");
        }

        [Test]
        public void ExtractTables_Update_DetectsJoinedTablesAndTypeHints()
        {
            const string sql =
                "uPdAtE u SeT u.Name='X',u.TotalAmount=12.5 " +
                "fRoM dbo.Users u " +
                "jOiN dbo.Orders o On o.UserId=u.UserId " +
                "wHeRe o.Title LiKe 'A%' aNd u.IsDeleted iS nUlL;";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var tables, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(tables, Is.Not.Null);
            var actualTables = tables!;
            Assert.That(actualTables.Select(i => i.FullName.TableName).ToArray(), Is.EquivalentTo(new[] { "Users", "Orders" }));

            var users = actualTables.Single(i => i.FullName.TableName == "Users");
            AssertString(users, "Name", 255);
            AssertDecimal(users, "TotalAmount");
            AssertBoolean(users, "IsDeleted", isNullable: true);
            AssertInt32(users, "UserId");

            var orders = actualTables.Single(i => i.FullName.TableName == "Orders");
            AssertInt32(orders, "UserId");
            AssertString(orders, "Title", 255);
        }

        [Test]
        public void ExtractTables_Delete_DetectsExistsSubquerySource()
        {
            const string sql =
                "dElEtE u fRoM dbo.Users u " +
                "wHeRe u.Email='a@b.com' " +
                "aNd eXiStS(sElEcT 1 FrOm dbo.Orders o wHeRe o.UserId=u.UserId aNd o.Amount>1.25);";

            var ok = SqTSqlParser.TryParse(sql, out IExpr? _, out var tables, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(tables, Is.Not.Null);
            var actualTables = tables!;
            Assert.That(actualTables.Select(i => i.FullName.TableName).ToArray(), Is.EquivalentTo(new[] { "Users", "Orders" }));

            var users = actualTables.Single(i => i.FullName.TableName == "Users");
            AssertString(users, "Email", 255);
            AssertInt32(users, "UserId");

            var orders = actualTables.Single(i => i.FullName.TableName == "Orders");
            AssertInt32(orders, "UserId");
            AssertDecimal(orders, "Amount");
        }

        private static TableColumn GetColumn(SqTable table, string columnName)
            => table.Columns.Single(c => string.Equals(c.ColumnName.Name, columnName, StringComparison.OrdinalIgnoreCase));

        private static void AssertInt32(SqTable table, string columnName, bool isNullable = false)
        {
            var column = GetColumn(table, columnName);
            Assert.That(column.SqlType, Is.EqualTo(ExprTypeInt32.Instance));
            Assert.That(column.IsNullable, Is.EqualTo(isNullable));
        }

        private static void AssertString(SqTable table, string columnName, int expectedSize, bool isNullable = false)
        {
            var column = GetColumn(table, columnName);
            Assert.That(column.SqlType, Is.TypeOf<ExprTypeString>());
            Assert.That(((ExprTypeString)column.SqlType).Size, Is.EqualTo(expectedSize));
            Assert.That(column.IsNullable, Is.EqualTo(isNullable));
        }

        private static void AssertBoolean(SqTable table, string columnName, bool isNullable = false)
        {
            var column = GetColumn(table, columnName);
            Assert.That(column.SqlType, Is.EqualTo(ExprTypeBoolean.Instance));
            Assert.That(column.IsNullable, Is.EqualTo(isNullable));
        }

        private static void AssertDecimal(SqTable table, string columnName, bool isNullable = false)
        {
            var column = GetColumn(table, columnName);
            Assert.That(column.SqlType, Is.TypeOf<ExprTypeDecimal>());
            Assert.That(column.IsNullable, Is.EqualTo(isNullable));
        }

        private static void AssertDateTime(SqTable table, string columnName, bool isNullable = false)
        {
            var column = GetColumn(table, columnName);
            Assert.That(column.SqlType, Is.TypeOf<ExprTypeDateTime>());
            Assert.That(column.IsNullable, Is.EqualTo(isNullable));
        }
    }
}


