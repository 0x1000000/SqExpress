using System;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios;

public sealed class ScJsonTable : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        var item = JsonTable("""{"items":[{"id":2,"meta":{"x":1}},{"id":5,"meta":{"x":2}}]}""", "$.items")
            .Value("Id", "$.id", SqlType.Int32)
            .Query("Meta", "$.meta")
            .Ordinal("Ordinal")
            .As("j");

        var rows = await Select(item.Column("Id"), item.Column("Meta"), item.Column("Ordinal"))
            .From(item)
            .OrderBy(Asc(item.Column("Ordinal")))
            .QueryList(context.Database, r => (
                Id: Convert.ToInt32(r.GetValue(r.GetOrdinal("Id"))),
                Meta: r.GetString(r.GetOrdinal("Meta")),
                Ordinal: Convert.ToInt32(r.GetValue(r.GetOrdinal("Ordinal")))));

        if (rows.Count != 2 || !rows.Select(i => i.Id).SequenceEqual([2, 5]) ||
            !rows.Select(i => i.Ordinal).SequenceEqual([0, 1]) || rows.Any(i => string.IsNullOrEmpty(i.Meta)))
            throw new Exception("Portable JSON table expansion returned an unexpected result.");
    }
}
