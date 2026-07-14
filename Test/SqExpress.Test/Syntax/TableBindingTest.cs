using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using SqExpress.DbMetadata;
using SqExpress.SqlParser;
using SqExpress.Syntax.Names;
using SqExpress.SyntaxTreeOperations;

namespace SqExpress.Test.Syntax;

#pragma warning disable SQEX012 // Tests intentionally parse orphaned table/column references before binding.
#pragma warning disable SQEX011 // Test SQL intentionally uses runtime metadata tables.

[TestFixture]
public class TableBindingTest
{
    [Test]
    public void BindTables_ReplacesAliasedTableAndColumnWithSameOwnedObjects()
    {
        var parsed = SqTSqlParser.Parse("SELECT u.id FROM users u");
        var canonical = Users();

        var bound = parsed.BindTables(new TableBase[] { canonical });
        var table = bound.SyntaxTree().DescendantsAndSelf().OfType<SqTable>().Single();
        var column = bound.SyntaxTree().DescendantsAndSelf().OfType<TableColumn>().Single();

        Assert.That(table.FullName.SchemaName, Is.EqualTo("dbo"));
        Assert.That(table.Alias?.Alias, Is.TypeOf<ExprAlias>());
        Assert.That(((ExprAlias)table.Alias!.Alias).Name, Is.EqualTo("u"));
        Assert.That(column.Table, Is.SameAs(table));
        Assert.That(column.Source, Is.SameAs(table.Alias));
        Assert.That(column.ColumnName.Name, Is.EqualTo("Id"));
    }

    [Test]
    public void BindTables_RepeatedTableAliasesProduceIndependentTablesAndColumns()
    {
        var parsed = SqTSqlParser.Parse("SELECT a.Id,b.Id FROM dbo.Users a JOIN dbo.Users b ON a.Id=b.Id");

        var bound = parsed.BindTables(new TableBase[] { Users() });
        var tables = bound.SyntaxTree().DescendantsAndSelf().OfType<SqTable>().ToArray();
        var columns = bound.SyntaxTree().DescendantsAndSelf().OfType<TableColumn>().ToArray();

        Assert.That(tables, Has.Length.EqualTo(2));
        Assert.That(columns, Has.Length.EqualTo(4));
        Assert.That(tables[0], Is.Not.SameAs(tables[1]));
        Assert.That(columns.All(c => tables.Any(t => ReferenceEquals(t, c.Table))), Is.True);
    }

    [Test]
    public void BindTables_IsCaseInsensitiveAndCompletesMissingSchema()
    {
        var parsed = SqTSqlParser.Parse(
            "SELECT ID FROM USERS",
            null,
            new SqTSqlParserOptions { DefaultSchema = null });

        var bound = parsed.BindTables(new TableBase[] { Users() });
        var table = bound.SyntaxTree().DescendantsAndSelf().OfType<SqTable>().Single();
        var column = bound.SyntaxTree().DescendantsAndSelf().OfType<TableColumn>().Single();

        Assert.That(table.FullName.SchemaName, Is.EqualTo("dbo"));
        Assert.That(column.ColumnName.Name, Is.EqualTo("Id"));
    }

    [Test]
    public void TryBindTables_ReturnsPartialResultAndErrors()
    {
        var parsed = SqTSqlParser.Parse("SELECT u.Id,u.Missing FROM dbo.Users u");

        var success = parsed.TryBindTables(
            new TableBase[] { Users() },
            out var bound,
            out var warnings,
            out var errors);

        Assert.That(success, Is.False);
        Assert.That(warnings, Is.Empty);
        Assert.That(errors.Select(e => e.Code), Contains.Item(TableBindingDiagnosticCode.UnknownColumn));
        Assert.That(bound.SyntaxTree().DescendantsAndSelf().OfType<TableColumn>().Count(), Is.EqualTo(1));
        Assert.That(bound.SyntaxTree().DescendantsAndSelf().OfType<ExprColumn>().Any(c => c.ColumnName.Name == "Missing"), Is.True);
    }

    [Test]
    public void BindTables_SeverityResolverCanDowngradeEveryIssue()
    {
        var parsed = SqTSqlParser.Parse("SELECT Missing FROM dbo.Unknown");
        var options = new TableBindingOptions
        {
            SeverityResolver = _ => TableBindingSeverity.Warning
        };

        var bound = parsed.BindTables(new TableBase[] { Users() }, options, out var warnings);

        Assert.That(bound, Is.Not.Null);
        Assert.That(warnings, Is.Not.Empty);
        Assert.That(warnings.All(w => w.Severity == TableBindingSeverity.Warning), Is.True);
    }

    [Test]
    public void BindTables_AmbiguousSchemaDoesNotReplaceTable()
    {
        var parsed = SqTSqlParser.Parse(
            "SELECT Id FROM Users",
            null,
            new SqTSqlParserOptions { DefaultSchema = null });
        var tables = new TableBase[] { Users(), SqTable.Create("audit", "Users", c => c.AppendInt32Column("Id")) };

        var success = parsed.TryBindTables(tables, out var bound, out _, out var errors);

        Assert.That(success, Is.False);
        Assert.That(errors.Select(e => e.Code), Contains.Item(TableBindingDiagnosticCode.AmbiguousTable));
        Assert.That(bound.SyntaxTree().DescendantsAndSelf().OfType<SqTable>(), Is.Empty);
    }

    [Test]
    public void SqTableCreate_DeepClonesColumnsAndIndexes()
    {
        var original = SqTable.Create(
            "dbo",
            "Users",
            c => c.AppendInt32Column("Id"),
            i => i.AppendIndex("IX_Users_Id", i.Asc("Id")));

        var clone = SqTable.Create((TableBase)original);

        Assert.That(clone, Is.Not.SameAs(original));
        Assert.That(clone.Columns[0], Is.TypeOf<Int32TableColumn>());
        Assert.That(clone.Columns[0].Table, Is.SameAs(clone));
        Assert.That(clone.Indexes[0].Columns[0].Column, Is.SameAs(clone.Columns[0]));
    }

    [Test]
    public void BindTables_CorrelatedSubqueryUsesOuterScope()
    {
        var parsed = SqTSqlParser.Parse(
            "SELECT u.Id FROM dbo.Users u WHERE EXISTS (SELECT 1 FROM dbo.Orders o WHERE o.UserId=u.Id)");
        var orders = SqTable.Create("dbo", "Orders", c => c.AppendInt32Column("Id").AppendInt32Column("UserId"));

        var bound = parsed.BindTables(new TableBase[] { Users(), orders });
        var tables = bound.SyntaxTree().DescendantsAndSelf().OfType<SqTable>().ToArray();
        var columns = bound.SyntaxTree().DescendantsAndSelf().OfType<TableColumn>().ToArray();

        Assert.That(tables, Has.Length.EqualTo(2));
        Assert.That(columns, Has.Length.EqualTo(3));
        Assert.That(columns.All(c => tables.Any(t => ReferenceEquals(t, c.Table))), Is.True);
    }

    [Test]
    public void BindTables_LeavesDerivedOutputButBindsInnerPhysicalColumn()
    {
        var parsed = SqTSqlParser.Parse("SELECT d.Id FROM (SELECT u.Id FROM dbo.Users u) d");

        var bound = parsed.BindTables(new TableBase[] { Users() });

        Assert.That(bound.SyntaxTree().DescendantsAndSelf().OfType<TableColumn>().Count(), Is.EqualTo(1));
        Assert.That(bound.SyntaxTree().DescendantsAndSelf().OfType<ExprColumn>().Any(c => c is not TableColumn && c.ColumnName.Name == "Id"), Is.True);
    }

    [Test]
    public void BindTables_ReconnectsJsonDeserializedTableAndColumn()
    {
        var users = Users();
        var original = SqQueryBuilder.Select(users.Columns[0]).From(users).Done();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            original.SyntaxTree().ExportToJson(writer);
        }
        var restored = ExprDeserializer.DeserializeFormJson(JsonDocument.Parse(stream.ToArray()).RootElement);

        var bound = restored.BindTables(new TableBase[] { users });
        var table = bound.SyntaxTree().DescendantsAndSelf().OfType<SqTable>().Single();
        var column = bound.SyntaxTree().DescendantsAndSelf().OfType<TableColumn>().Single();

        Assert.That(column.Table, Is.SameAs(table));
    }

    [Test]
    public void BindTables_IsIdempotentForParserBoundExpression()
    {
        var users = Users();
        var parsed = SqTSqlParser.Parse("SELECT u.Id FROM dbo.Users u", new TableBase[] { users });
        var parsedTable = parsed.SyntaxTree().DescendantsAndSelf().OfType<SqTable>().Single();
        var parsedColumn = parsed.SyntaxTree().DescendantsAndSelf().OfType<TableColumn>().Single();

        var rebound = parsed.BindTables(new TableBase[] { users });

        Assert.That(rebound, Is.SameAs(parsed));
        Assert.That(rebound.SyntaxTree().DescendantsAndSelf().OfType<SqTable>().Single(), Is.SameAs(parsedTable));
        Assert.That(rebound.SyntaxTree().DescendantsAndSelf().OfType<TableColumn>().Single(), Is.SameAs(parsedColumn));
    }

    private static SqTable Users()
        => SqTable.Create("dbo", "Users", c => c.AppendInt32Column("Id").AppendStringColumn("Name", 100));
}
