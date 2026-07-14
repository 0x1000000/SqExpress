using System;
using System.Linq;
using NUnit.Framework;
using SqExpress.DbMetadata;
using SqExpress.SqlParser;
using SqExpress.Syntax;
using SqExpress.Syntax.Names;

namespace SqExpress.Test.SqlParser
{
#pragma warning disable SQEX011 // Tests intentionally use runtime metadata tables.
#pragma warning disable SQEX012 // Tests intentionally exercise parser binding of raw SQL columns.
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
        public void TryParse_WithExistingTables_WhenIndexAndMetaDiffer_StillReturnsTrue()
        {
            var sql = "SELECT [u].[Id] FROM [dbo].[Users] [u]";
            var existing = new TableBase[]
            {
                SqTable.Create(
                    "dbo",
                    "Users",
                    a => a.AppendInt32Column("Id", ColumnMeta.PrimaryKey().Identity().DefaultValue(1)),
                    i => i.AppendIndex(i.Asc("Id")))
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
        public void TryParse_WithExistingTables_WhenUnqualifiedProjectionColumnsResolveUniquelyInJoin_ReturnsTrue()
        {
            var sql = "SELECT TOP 1 SalesOrderId, SUM(Amount) AS TotalSales FROM ops.Payment p JOIN ops.Invoice i ON p.InvoiceId = i.InvoiceId WHERE i.InvoiceDate >= DATEADD(month, -1, GETDATE()) GROUP BY SalesOrderId ORDER BY TotalSales DESC";
            var existing = new TableBase[]
            {
                CreateTable("ops", "Payment", a => a
                    .AppendInt32Column("InvoiceId")
                    .AppendDecimalColumn("Amount")),
                CreateTable("ops", "Invoice", a => a
                    .AppendInt32Column("InvoiceId")
                    .AppendInt32Column("SalesOrderId")
                    .AppendDateTimeColumn("InvoiceDate"))
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

            var expr = SqTSqlParser.Parse(sql);

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

            var ex = Assert.Throws<SqExpressTSqlParserException>(() => SqTSqlParser.Parse(sql, existing));
            Assert.That(ex!.Message, Does.Contain("Unexpected tables: [dbo].[Orders]"));
        }

        [Test]
        public void Parse_WithExistingTables_EmitsOwnedSqTablesAndReusesTheirColumns()
        {
            const string sql = "SELECT u.Id,o.OrderId FROM dbo.Users u JOIN dbo.Orders o ON o.UserId=u.Id";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id")),
                CreateTable("dbo", "Orders", a => a.AppendInt32Column("OrderId").AppendInt32Column("UserId"))
            };

            var expr = SqTSqlParser.Parse(sql, existing);
            var tables = expr.SyntaxTree().DescendantsAndSelf().OfType<SqTable>().ToArray();
            var columns = expr.SyntaxTree().DescendantsAndSelf().OfType<TableColumn>().ToArray();

            Assert.That(tables, Has.Length.EqualTo(2));
            Assert.That(columns, Has.Length.EqualTo(4));
            Assert.That(columns.All(column => tables.Any(table => ReferenceEquals(column.Table, table))), Is.True);
            Assert.That(columns.All(column => ((SqTable)column.Table).Columns.Any(owned => ReferenceEquals(owned, column))), Is.True);
        }

        [Test]
        public void Parse_WithExistingTables_RepeatedAliasesOwnIndependentColumnSets()
        {
            const string sql = "SELECT a.Id,b.Id FROM dbo.Users a JOIN dbo.Users b ON a.Id=b.Id";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"))
            };

            var expr = SqTSqlParser.Parse(sql, existing);
            var tables = expr.SyntaxTree().DescendantsAndSelf().OfType<SqTable>().ToArray();

            Assert.That(tables, Has.Length.EqualTo(2));
            Assert.That(tables[0], Is.Not.SameAs(tables[1]));
            Assert.That(tables[0].Columns[0], Is.Not.SameAs(tables[1].Columns[0]));
            Assert.That(tables[0].Columns[0].Table, Is.SameAs(tables[0]));
            Assert.That(tables[1].Columns[0].Table, Is.SameAs(tables[1]));
        }

        [Test]
        public void Parse_WithExistingTables_NormalizesUnqualifiedColumnToOwnedAliasColumn()
        {
            const string sql = "SELECT Id FROM dbo.Users u";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id"))
            };

            var expr = SqTSqlParser.Parse(sql, existing);
            var table = expr.SyntaxTree().DescendantsAndSelf().OfType<SqTable>().Single();
            var column = expr.SyntaxTree().DescendantsAndSelf().OfType<TableColumn>().Single();

            Assert.That(column, Is.SameAs(table.Columns[0]));
            Assert.That(column.Source, Is.SameAs(table.Alias));
            Assert.That(((ExprAlias)table.Alias!.Alias).Name, Is.EqualTo("u"));
            Assert.That(expr.ToSql(), Is.EqualTo("SELECT [u].[Id] FROM [dbo].[Users] [u]"));
        }

        [Test]
        public void Parse_WithoutExistingTables_KeepsNeutralNodes()
        {
            var expr = SqTSqlParser.Parse("SELECT u.Id FROM dbo.Users u");

            Assert.That(expr.SyntaxTree().DescendantsAndSelf().OfType<SqTable>(), Is.Empty);
            Assert.That(expr.SyntaxTree().DescendantsAndSelf().OfType<TableColumn>(), Is.Empty);
            Assert.That(expr.SyntaxTree().DescendantsAndSelf().OfType<ExprTable>(), Is.Not.Empty);
            Assert.That(expr.SyntaxTree().DescendantsAndSelf().OfType<ExprColumn>(), Is.Not.Empty);
        }

        [Test]
        public void Parse_WithExistingTables_CrossApplyBindsPhysicalColumnsAcrossCorrelatedScopes()
        {
            const string sql = "SELECT u.Id,x.OrderId FROM dbo.Users u CROSS APPLY (SELECT o.OrderId FROM dbo.Orders o WHERE o.UserId=u.Id) x";
            var existing = new TableBase[]
            {
                CreateTable("dbo", "Users", a => a.AppendInt32Column("Id")),
                CreateTable("dbo", "Orders", a => a.AppendInt32Column("OrderId").AppendInt32Column("UserId"))
            };

            var expr = SqTSqlParser.Parse(sql, existing);
            var tables = expr.SyntaxTree().DescendantsAndSelf().OfType<SqTable>().ToArray();
            var physicalColumns = expr.SyntaxTree().DescendantsAndSelf().OfType<TableColumn>().ToArray();
            var derivedColumns = expr.SyntaxTree().DescendantsAndSelf().OfType<ExprColumn>()
                .Where(column => column is not TableColumn)
                .ToArray();

            Assert.That(tables, Has.Length.EqualTo(2));
            Assert.That(physicalColumns, Has.Length.EqualTo(4));
            Assert.That(physicalColumns.All(column => ((SqTable)column.Table).Columns.Any(owned => ReferenceEquals(owned, column))), Is.True);
            Assert.That(derivedColumns.Any(column => column.ColumnName.Name == "OrderId"), Is.True);
        }

        private static SqTable CreateTable(
            string? schema,
            string tableName,
            Func<ITableColumnAppender, ITableColumnAppender> columns)
            => SqTable.Create(schema, tableName, a => columns(a));
    }
}


