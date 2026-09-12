using System;
using System.Text.Json;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios;

public sealed class ScForJson : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        var query = Select(
                Literal(1).As("id"),
                Literal("Ada").AsJson("$.customer.name"),
                JsonQuery("{\"active\":true}").As("metadata"),
                Null.As("optional"))
            .ForJson();
        var json = await query.QueryScalar(context.Database);
        if (json == null) throw new Exception("ForJson returned SQL NULL.");

        var jsonText = Convert.ToString(json)!;
        using var document = JsonDocument.Parse(jsonText);
        var row = document.RootElement[0];
        if (row.GetProperty("id").GetInt32() != 1 || row.GetProperty("customer").GetProperty("name").GetString() != "Ada" ||
            !row.GetProperty("metadata").GetProperty("active").GetBoolean() || row.GetProperty("optional").ValueKind != JsonValueKind.Null)
            throw new Exception("Portable relational JSON output returned an unexpected result.");

        var objectValue = await Select(Literal(1).As("id"), Null.As("optional"))
            .ForJson(withoutArrayWrapper: true, includeNullValues: false)
            .QueryScalar(context.Database);
        if (objectValue == null) throw new Exception("ForJson object mode unexpectedly returned SQL NULL.");
        using var objectDocument = JsonDocument.Parse(Convert.ToString(objectValue)!);
        if (objectDocument.RootElement.ValueKind != JsonValueKind.Object ||
            objectDocument.RootElement.GetProperty("id").GetInt32() != 1 ||
            objectDocument.RootElement.TryGetProperty("optional", out _))
            throw new Exception("Portable ForJson object/null options returned an unexpected result.");

        var emptyRows = JsonTable("[]", "$").Value("id", "$", SqlType.Int32).As("emptyRows");
        var emptyObject = await Select(emptyRows.Column("id"))
            .From(emptyRows)
            .ForJson(withoutArrayWrapper: true)
            .QueryScalar(context.Database);
        if (emptyObject != null && emptyObject != DBNull.Value)
            throw new Exception("ForJson object mode must return SQL NULL for zero rows.");

        var twoRows = JsonTable("[1,2]", "$").Value("id", "$", SqlType.Int32).As("twoRows");
        var rejectedMultipleRows = false;
        try
        {
            await Select(twoRows.Column("id"))
                .From(twoRows)
                .ForJson(withoutArrayWrapper: true)
                .QueryScalar(context.Database);
        }
        catch
        {
            rejectedMultipleRows = true;
        }
        if (!rejectedMultipleRows)
            throw new Exception("ForJson object mode must reject multiple rows.");
    }
}
