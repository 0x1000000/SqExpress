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

            var ok = TSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

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

            var ok = TSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.False);
            Assert.That(expr, Is.Null);
            Assert.That(error, Does.Contain("SELECT list is missing"));
        }

        [Test]
        public void TryParse_WithExistingTables_WhenExpectedTableMissing_ReturnsMismatchError()
        {
            var sql = "SELECT [u].[Id] FROM [dbo].[Users] [u]";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id")),
                CreateTable("dbo", "Orders", a => a.AppendInt32Column("OrderId"))
            };

            var ok = TSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.False);
            Assert.That(expr, Is.Null);
            Assert.That(error, Does.Contain("Missing tables: [dbo].[Orders]"));
        }

        [Test]
        public void TryParse_WithExistingTables_WhenUnexpectedTableParsed_ReturnsMismatchError()
        {
            var sql = "SELECT [u].[Id],[o].[OrderId] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[Id]";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"))
            };

            var ok = TSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.False);
            Assert.That(expr, Is.Null);
            Assert.That(error, Does.Contain("Unexpected tables: [dbo].[Orders]"));
        }

        [Test]
        public void TryParse_WithExistingTables_WhenExpectedColumnMissing_ReturnsMismatchError()
        {
            var sql = "SELECT [u].[Id],[u].[Name] FROM [dbo].[Users] [u]";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a
                    .AppendInt32Column("Id")
                    .AppendStringColumn("Name", 255, isUnicode: true)
                    .AppendStringColumn("Email", 255, isUnicode: true))
            };

            var ok = TSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.False);
            Assert.That(expr, Is.Null);
            Assert.That(error, Does.Contain("missing columns: [Email]"));
        }

        [Test]
        public void TryParse_WithExistingTables_WhenUnexpectedColumnParsed_ReturnsMismatchError()
        {
            var sql = "SELECT [u].[Id],[u].[Name] FROM [dbo].[Users] [u]";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"))
            };

            var ok = TSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.False);
            Assert.That(expr, Is.Null);
            Assert.That(error, Does.Contain("extra columns: [Name]"));
        }

        [Test]
        public void TryParse_WithExistingTables_WhenColumnTypeDiffers_ReturnsMismatchError()
        {
            var sql = "SELECT [u].[Id] FROM [dbo].[Users] [u]";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendStringColumn("Id", 255, isUnicode: true))
            };

            var ok = TSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.False);
            Assert.That(expr, Is.Null);
            Assert.That(error, Does.Contain("changed columns: [Id] (DifferentType)"));
        }

        [Test]
        public void TryParse_WithExistingTables_WhenColumnNullabilityDiffers_ReturnsMismatchError()
        {
            var sql = "SELECT [u].[Id] FROM [dbo].[Users] [u] WHERE [u].[Id] IS NULL";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"))
            };

            var ok = TSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.False);
            Assert.That(expr, Is.Null);
            Assert.That(error, Does.Contain("DifferentNullability"));
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

            var ok = TSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

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

            var ok = TSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.False);
            Assert.That(expr, Is.Null);
            Assert.That(error, Does.Contain("DifferentName"));
        }

        [Test]
        public void TryParse_WithExistingTables_WhenSqlHasNoTablesAndExpectedIsEmpty_ReturnsTrue()
        {
            var sql = "SELECT 1";
            var existing = Array.Empty<TableBase>();

            var ok = TSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(expr, Is.Not.Null);
            Assert.That(error, Is.Null);
        }

        [Test]
        public void TryParse_WithExistingTables_WhenSqlHasNoTablesButExpectedNotEmpty_ReturnsMismatchError()
        {
            var sql = "SELECT 1";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"))
            };

            var ok = TSqlParser.TryParse(sql, existing, out IExpr? expr, out var error);

            Assert.That(ok, Is.False);
            Assert.That(expr, Is.Null);
            Assert.That(error, Does.Contain("Missing tables: [dbo].[Users]"));
        }

        [Test]
        public void TryParse_WithExistingTables_WhenArgumentIsNull_Throws()
        {
            var sql = "SELECT 1";
            IExpr? expr;
            string? error;

            Assert.Throws<ArgumentNullException>(() =>
            {
                TSqlParser.TryParse(sql, null!, out expr, out error);
            });
        }

        [Test]
        public void Parse_WithExistingTables_WhenParserFails_ThrowsSqExpressTSqlParserException()
        {
            var sql = "SELECT FROM [dbo].[Users]";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"))
            };

            var ex = Assert.Throws<SqExpressTSqlParserException>(() => TSqlParser.Parse(sql, existing));
            Assert.That(ex!.Message, Does.Contain("SELECT list is missing"));
        }

        [Test]
        public void Parse_WithExistingTables_WhenTableArtifactsDoNotMatch_ThrowsSqExpressTSqlParserException()
        {
            var sql = "SELECT [u].[Id] FROM [dbo].[Users] [u]";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id")),
                CreateTable("dbo", "Orders", a => a.AppendInt32Column("OrderId"))
            };

            var ex = Assert.Throws<SqExpressTSqlParserException>(() => TSqlParser.Parse(sql, existing));
            Assert.That(ex!.Message, Does.Contain("Missing tables: [dbo].[Orders]"));
        }

        private static SqTable CreateTable(
            string schema,
            string tableName,
            Func<ITableColumnAppender, ITableColumnAppender> columns)
            => SqTable.Create(schema, tableName, a => columns(a));
    }
}
