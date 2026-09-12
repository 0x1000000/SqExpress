using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.TableDeclarationAttributes;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios;

public class ScStringAgg : IScenario
{
    private const string Separator = "'|";

    public async Task Exec(IScenarioContext context)
    {
        var table = new TempStringAggItems();
        await context.Database.Statement(table.Script.DropIfExist());
        await context.Database.Statement(table.Script.Create());

        try
        {
            var rows = new[]
            {
                new StringAggRow(1, 1, 2, "Beta"),
                new StringAggRow(2, 1, 1, "Alpha"),
                new StringAggRow(3, 1, 3, null),
                new StringAggRow(4, 2, 1, "Solo"),
                new StringAggRow(5, 3, 1, null)
            };

            await InsertDataInto(table, rows)
                .MapData(s => s
                    .Set(s.Target.Id, s.Source.Id)
                    .Set(s.Target.GroupId, s.Source.GroupId)
                    .Set(s.Target.SortKey, s.Source.SortKey)
                    .Set(s.Target.Value, s.Source.Value))
                .Exec(context.Database);

            var orderedColumn = CustomColumnFactory.NullableString("OrderedValues");
            var unorderedColumn = CustomColumnFactory.NullableString("UnorderedValues");
            var result = await Select(
                    table.GroupId,
                    StringAgg(table.Value, Separator).OrderBy(Asc(table.SortKey)).As(orderedColumn),
                    StringAgg(table.Value, Separator).As(unorderedColumn))
                .From(table)
                .GroupBy(table.GroupId)
                .OrderBy(table.GroupId)
                .QueryList(context.Database, r => new
                {
                    GroupId = table.GroupId.Read(r),
                    Ordered = orderedColumn.Read(r),
                    Unordered = unorderedColumn.Read(r)
                });

            var byGroup = result.ToDictionary(i => i.GroupId);
            AssertEqual("Alpha'|Beta", byGroup[1].Ordered, "ordered group 1");
            AssertEqual("Solo", byGroup[2].Ordered, "ordered group 2");
            AssertEqual(null, byGroup[3].Ordered, "ordered all-null group");
            AssertUnordered(["Alpha", "Beta"], byGroup[1].Unordered, "unordered group 1");
            AssertUnordered(["Solo"], byGroup[2].Unordered, "unordered group 2");
            AssertEqual(null, byGroup[3].Unordered, "unordered all-null group");
        }
        finally
        {
            await context.Database.Statement(table.Script.DropIfExist());
        }
    }

    private static void AssertUnordered(IReadOnlyList<string> expected, string? actual, string label)
    {
        if (actual == null)
        {
            throw new SqExpressException($"{label}: expected values but got null");
        }

        var actualItems = actual.Split([Separator], StringSplitOptions.None).OrderBy(i => i).ToArray();
        var expectedItems = expected.OrderBy(i => i).ToArray();
        if (!actualItems.SequenceEqual(expectedItems))
        {
            throw new SqExpressException($"{label}: expected [{string.Join(",", expectedItems)}] but got [{string.Join(",", actualItems)}]");
        }
    }

    private static void AssertEqual(string? expected, string? actual, string label)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new SqExpressException($"{label}: expected '{expected ?? "<null>"}' but got '{actual ?? "<null>"}'");
        }
    }

    private sealed record StringAggRow(int Id, int GroupId, int SortKey, string? Value);
}

[TempTableDescriptor("TmpStringAggItems")]
[Int32Column("Id", Pk = true)]
[Int32Column("GroupId")]
[Int32Column("SortKey")]
[NullableStringColumn("Value", MaxLength = 50)]
public partial class TempStringAggItems
{
}
