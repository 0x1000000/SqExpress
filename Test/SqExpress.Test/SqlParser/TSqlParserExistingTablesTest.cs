using System;
using NUnit.Framework;
using SqExpress.DbMetadata;
using SqExpress.SqlParser;
using SqExpress.Syntax;

namespace SqExpress.Test.SqlParser
{
    public class TSqlParserExistingTablesTest
    {
        [Test]
        public void TryParse_WithExistingTables_WhenExactMatch_ReturnsTrue()
        {
            var sql = "SELECT [u].[Id],[o].[OrderId] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[Id]";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id")),
                CreateTable("dbo", "Orders", a => a.AppendInt32Column("OrderId").AppendInt32Column("UserId"))
            };

            var ok = SqTSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void TryParse_WithExistingTables_WhenParserFails_ReturnsParseError()
        {
            var sql = "SELECT FROM [dbo].[Users]";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"))
            };

            var ok = SqTSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.False);
            Assert.That(expr, Is.Null);
            Assert.That(error, Does.Contain("SELECT list is missing"));
        }

        [Test]
        public void TryParse_WithExistingTables_WhenProvidedTablesContainExtraEntries_StillReturnsTrue()
        {
            var sql = "SELECT [u].[Id] FROM [dbo].[Users] [u]";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id")),
                CreateTable("dbo", "Orders", a => a.AppendInt32Column("OrderId"))
            };

            var ok = SqTSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void TryParse_WithExistingTables_WhenUnexpectedTableParsed_ReturnsMismatchError()
        {
            var sql = "SELECT [u].[Id],[o].[OrderId] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[Id]";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"))
            };

            var ok = SqTSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.False);
            Assert.That(expr, Is.Null);
            Assert.That(error, Does.Contain("Unexpected tables: [dbo].[Orders]"));
        }

        [Test]
        public void TryParse_WithExistingTables_WhenExpectedHasMoreColumns_StillReturnsTrue()
        {
            var sql = "SELECT [u].[Id],[u].[Name] FROM [dbo].[Users] [u]";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a
                    .AppendInt32Column("Id")
                    .AppendStringColumn("Name", 255, isUnicode: true)
                    .AppendStringColumn("Email", 255, isUnicode: true))
            };

            var ok = SqTSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void TryParse_WithExistingTables_WhenUnexpectedColumnParsed_ReturnsMismatchError()
        {
            var sql = "SELECT [u].[Id],[u].[Name] FROM [dbo].[Users] [u]";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"))
            };

            var ok = SqTSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.False);
            Assert.That(expr, Is.Null);
            Assert.That(error, Does.Contain("extra columns: [Name]"));
        }

        [Test]
        public void TryParse_WithExistingTables_WhenWildcardQueryReferencesKnownColumn_ReturnsTrue()
        {
            var sql = "SELECT * FROM [dbo].[Users] WHERE [UserId] = @userId";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a
                    .AppendInt32Column("UserId")
                    .AppendStringColumn("Name", 255, isUnicode: true)
                    .AppendBooleanColumn("IsActive"))
            };

            var ok = SqTSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void TryParse_WithExistingTables_WhenWildcardQueryReferencesUnknownColumn_ReturnsMismatchError()
        {
            var sql = "SELECT * FROM [dbo].[Users] WHERE [UserKey] = @userId";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a
                    .AppendInt32Column("UserId")
                    .AppendStringColumn("Name", 255, isUnicode: true))
            };

            var ok = SqTSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.False);
            Assert.That(expr, Is.Null);
            Assert.That(error, Does.Contain("extra columns: [UserKey]"));
        }

        [Test]
        public void TryParse_WithExistingTables_WhenColumnTypeDiffers_StillReturnsTrue()
        {
            var sql = "SELECT [u].[Id],[o].[OrderId] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[Id]";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendStringColumn("Id", 255, isUnicode: true)),
                CreateTable("dbo", "Orders", a => a.AppendInt32Column("OrderId").AppendInt32Column("UserId"))
            };

            var ok = SqTSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void TryParse_WithExistingTables_WhenColumnNullabilityDiffers_StillReturnsTrue()
        {
            var sql = "SELECT [u].[Id],[o].[OrderId] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[Id] WHERE [u].[Id] IS NULL";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id")),
                CreateTable("dbo", "Orders", a => a.AppendInt32Column("OrderId").AppendInt32Column("UserId"))
            };

            var ok = SqTSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void TryParse_WithExistingTables_WhenSimpleUpdateWithoutFrom_UsesUpdateTargetTableForValidation()
        {
            var sql = "UPDATE [dbo].[Users] SET [Name]='X' WHERE [Id]=1";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a
                    .AppendInt32Column("Id")
                    .AppendStringColumn("Name", 255, isUnicode: true))
            };
            var ok = SqTSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);
            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void TryParse_WithExistingTables_WhenCustomDefaultSchemaMatchesUnqualifiedTable_ReturnsTrue()
        {
            var sql = "SELECT [u].[Id] FROM [Users] [u]";
            var existing = new TableBase[]
            {
                CreateTable("sales", "Users", a => a.AppendInt32Column("Id"))
            };

            var ok = SqTSqlParser.TryParse(
                sql,
                existing,
                new SqTSqlParserOptions { DefaultSchema = "sales" },
                out IExpr? expr,
                out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void TryParse_WithExistingTables_WhenDefaultSchemaIsNull_MatchesSchemaLessTable()
        {
            var sql = "SELECT [u].[Id] FROM [Users] [u]";
            var existing = new TableBase[]
            {
                CreateTable(null, "Users", a => a.AppendInt32Column("Id"))
            };

            var ok = SqTSqlParser.TryParse(
                sql,
                existing,
                new SqTSqlParserOptions { DefaultSchema = null },
                out IExpr? expr,
                out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void TryParse_WithExistingTables_WhenSchemaMatchesNonDbo_ReturnsTrue()
        {
            var sql = "SELECT [u].[Id],[o].[OrderId] FROM [sales].[Users] [u] JOIN [sales].[Orders] [o] ON [o].[UserId]=[u].[Id]";
            var existing = new TableBase[]
            {
                CreateTable("sales", "Users", a => a.AppendInt32Column("Id")),
                CreateTable("sales", "Orders", a => a.AppendInt32Column("OrderId").AppendInt32Column("UserId"))
            };

            var ok = SqTSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void TryParse_WithExistingTables_WhenNamesDifferOnlyByCase_ReturnsMismatchError()
        {
            var sql = "SELECT [U].[ID],[O].[ORDERID] FROM [DBO].[USERS] [U] JOIN [DBO].[ORDERS] [O] ON [O].[USERID]=[U].[ID]";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "users", a => a.AppendInt32Column("id")),
                CreateTable("dbo", "orders", a => a.AppendInt32Column("orderid").AppendInt32Column("userid"))
            };

            var ok = SqTSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.False);
            Assert.That(expr, Is.Null);
            Assert.That(error, Does.Contain("DifferentName"));
        }

        [Test]
        public void TryParse_WithExistingTables_WhenSqlHasNoTablesAndExpectedIsEmpty_ReturnsTrue()
        {
            var sql = "SELECT 1";
            var existing = Array.Empty<TableBase>();

            var ok = SqTSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void TryParse_WithExistingTables_WhenSqlHasNoTablesButExpectedNotEmpty_ReturnsTrue()
        {
            var sql = "SELECT 1";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"))
            };

            var ok = SqTSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void TryParse_WithExistingTables_WhenArgumentIsNull_SkipsValidationAndReturnsTrue()
        {
            var sql = "SELECT [u].[Id],[o].[OrderId] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[Id]";
            IExpr? expr;
            string? error;

            var ok = SqTSqlParser.TryParse(sql, (System.Collections.Generic.IReadOnlyList<TableBase>?)null, out expr, out error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void Parse_WhenExistingTablesAreNotProvided_SkipsValidationAndReturnsExpression()
        {
            var sql = "SELECT [u].[Id],[o].[OrderId] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[Id]";

            #pragma warning disable SQEX011
            var expr = SqTSqlParser.Parse(sql);
            #pragma warning restore SQEX011

            Assert.That(expr, Is.Not.Null);
        }

        [Test]
        public void Parse_WithExistingTables_WhenParserFails_ThrowsSqExpressTSqlParserException()
        {
            var sql = "SELECT FROM [dbo].[Users]";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"))
            };

            #pragma warning disable SQEX010
            var ex = Assert.Throws<SqExpressTSqlParserException>(() => SqTSqlParser.Parse(sql, existing));
            #pragma warning restore SQEX010
            Assert.That(ex!.Message, Does.Contain("SELECT list is missing"));
        }

        [Test]
        public void Parse_WithExistingTables_WhenParsedTableIsMissing_ThrowsSqExpressTSqlParserException()
        {
            var sql = "SELECT [u].[Id],[o].[OrderId] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[Id]";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"))
            };

            #pragma warning disable SQEX011
            var ex = Assert.Throws<SqExpressTSqlParserException>(() => SqTSqlParser.Parse(sql, existing));
            #pragma warning restore SQEX011
            Assert.That(ex!.Message, Does.Contain("Unexpected tables: [dbo].[Orders]"));
        }

        private static SqTable CreateTable(
            string? schema,
            string tableName,
            Func<ITableColumnAppender, ITableColumnAppender> columns)
            => SqTable.Create(schema, tableName, a => columns(a));
    }
}


