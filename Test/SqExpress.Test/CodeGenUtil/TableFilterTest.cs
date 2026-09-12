#if NET
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SqExpress.CodeGenUtil;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.Test.CodeGenUtil;

[TestFixture]
public class TableFilterTest
{
    [Test]
    public void IncludeAndExcludePatternsFilterPhysicalTableNames()
    {
        var tables = new[]
        {
            Table("sales", "Order"),
            Table("sales", "OrderArchive1"),
            Table("dbo", "Order"),
            Table("dbo", "Customer")
        };

        var result = TableFilter.Apply(
            tables,
            ["sales.*", "Customer"],
            ["*.OrderArchive?"]
        );

        Assert.That(result.Select(static table => table.DbName.ToString()),
            Is.EqualTo(new[] { "sales.Order", "dbo.Customer" }));
    }

    [Test]
    public void MatchingIsCaseInsensitiveAndRegexCharactersAreLiteral()
    {
        var tables = new[]
        {
            Table("dbo", "Audit.Log"),
            Table("dbo", "AuditXLog")
        };

        var result = TableFilter.Apply(tables, ["DBO.AUDIT.LOG"], new string[0]);

        Assert.That(result.Single().DbName.Name, Is.EqualTo("Audit.Log"));
    }

    [Test]
    public void ExcludeWinsOverInclude()
    {
        var result = TableFilter.Apply(
            [Table("dbo", "Customer")],
            ["Customer"],
            ["Cust*"]
        );

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ForeignKeysToFilteredTablesAreRemoved()
    {
        var customer = Table("dbo", "Customer");
        var order = Table(
            "dbo",
            "Order",
            new ColumnModel(
                "CustomerId",
                new ColumnRef("dbo", "Order", "CustomerId"),
                0,
                new Int32ColumnType(false),
                null,
                false,
                null,
                [new ColumnRef("dbo", "Customer", "Id")]
            ));

        var result = TableFilter.Apply([customer, order], ["Order"], new string[0]);

        Assert.That(result.Single().Columns.Single().Fk, Is.Null);
        Assert.That(order.Columns.Single().Fk, Has.Count.EqualTo(1), "Source metadata must not be mutated.");
    }

    [Test]
    public void ForeignKeysWithinSelectionArePreserved()
    {
        var customer = Table("dbo", "Customer");
        var order = Table(
            "dbo",
            "Order",
            new ColumnModel(
                "CustomerId",
                new ColumnRef("dbo", "Order", "CustomerId"),
                0,
                new Int32ColumnType(false),
                null,
                false,
                null,
                [new ColumnRef("dbo", "Customer", "Id")]
            ));

        var result = TableFilter.Apply([customer, order], ["dbo.*"], new string[0]);

        Assert.That(result.Single(table => table.DbName.Name == "Order").Columns.Single().Fk, Has.Count.EqualTo(1));
    }

    [Test]
    public void EmptyPatternIsRejected()
    {
        Assert.Throws<SqExpressCodeGenException>(() =>
            TableFilter.Apply([Table("dbo", "Customer")], [" "], new string[0]));
    }

    private static TableModel Table(string schema, string name, params ColumnModel[] columns) =>
        new TableModel("Table" + name, new TableRef(schema, name), columns.ToList(), []);
}
#endif
