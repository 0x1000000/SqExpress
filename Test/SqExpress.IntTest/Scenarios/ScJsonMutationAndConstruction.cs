using System;
using System.Text.Json;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios;

public sealed class ScJsonMutationAndConstruction : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        var constructed = JsonObject(
            JsonProperty("name", "Ada"),
            JsonProperty("nothing", Null),
            JsonProperty("nested", JsonQuery("{\"ok\":true}")),
            JsonProperty("items", JsonArray(1, JsonNull(), Null)));
        var changed = JsonRemove(JsonSet("{\"a\":1,\"remove\":2}", "$.a", 3), "$.remove");

        var rows = await Select(constructed.As("Constructed"), changed.As("Changed"))
            .QueryList(context.Database, r => (
                Constructed: r.GetString(r.GetOrdinal("Constructed")),
                Changed: r.GetString(r.GetOrdinal("Changed"))));

        using var objectJson = JsonDocument.Parse(rows[0].Constructed);
        using var changedJson = JsonDocument.Parse(rows[0].Changed);
        var root = objectJson.RootElement;
        if (root.GetProperty("name").GetString() != "Ada" || root.GetProperty("nothing").ValueKind != JsonValueKind.Null ||
            !root.GetProperty("nested").GetProperty("ok").GetBoolean() || root.GetProperty("items").GetArrayLength() != 3 ||
            changedJson.RootElement.GetProperty("a").GetInt32() != 3 || changedJson.RootElement.TryGetProperty("remove", out _))
            throw new Exception("Portable JSON construction or mutation returned an unexpected result.");
    }
}
