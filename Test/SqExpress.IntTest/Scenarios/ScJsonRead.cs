using System;
using System.Globalization;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios;

public sealed class ScJsonRead : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        const string document = "{\"name\":\"Ada\",\"active\":true,\"count\":12,\"price\":12.5,\"items\":[{\"id\":7}],\"none\":null}";
        var row = await Select(
                JsonValue(document, "$.name").As("Name"),
                JsonValue(document, "$.active", SqlType.Boolean).As("Active"),
                JsonValue(document, "$.count", SqlType.Int32).As("Count"),
                JsonValue(document, "$.price", SqlType.Decimal()).As("Price"),
                JsonValue(document, "$.items[0].id", SqlType.Int64).As("ItemId"),
                JsonQuery(document, "$.items").As("Items"),
                JsonValue(document, "$.missing").As("Missing"),
                JsonValue(document, "$.none").As("JsonNull"),
                JsonValue(document, "$.name", SqlType.Int32).As("WrongKind"),
                JsonValue(Null, "$.name").As("SqlNull"))
            .QueryList(context.Database, r => new
            {
                Name = Convert.ToString(r.GetValue(r.GetOrdinal("Name")), CultureInfo.InvariantCulture),
                Active = r.IsDBNull(r.GetOrdinal("Active"))
                    ? (bool?)null
                    : Convert.ToBoolean(r.GetValue(r.GetOrdinal("Active")), CultureInfo.InvariantCulture),
                Count = Convert.ToInt32(r.GetValue(r.GetOrdinal("Count")), CultureInfo.InvariantCulture),
                Price = Convert.ToDecimal(r.GetValue(r.GetOrdinal("Price")), CultureInfo.InvariantCulture),
                ItemId = Convert.ToInt64(r.GetValue(r.GetOrdinal("ItemId")), CultureInfo.InvariantCulture),
                Items = Convert.ToString(r.GetValue(r.GetOrdinal("Items")), CultureInfo.InvariantCulture),
                Missing = r.IsDBNull(r.GetOrdinal("Missing")),
                JsonNull = r.IsDBNull(r.GetOrdinal("JsonNull")),
                WrongKind = r.IsDBNull(r.GetOrdinal("WrongKind")),
                SqlNull = r.IsDBNull(r.GetOrdinal("SqlNull"))
            });

        var actual = row[0];
        if (actual.Name != "Ada" || actual.Active != true || actual.Count != 12 || actual.Price != 12.5m || actual.ItemId != 7 ||
            actual.Items == null || !actual.Missing || !actual.JsonNull || !actual.WrongKind || !actual.SqlNull)
            throw new Exception(
                $"Portable JSON scalar extraction returned an unexpected result: " +
                $"Name={actual.Name ?? "<null>"}, Active={actual.Active?.ToString() ?? "<null>"}, Count={actual.Count}, " +
                $"Price={actual.Price}, ItemId={actual.ItemId}, Items={actual.Items ?? "<null>"}, " +
                $"Missing={actual.Missing}, JsonNull={actual.JsonNull}, WrongKind={actual.WrongKind}, SqlNull={actual.SqlNull}.");
    }
}
