using NUnit.Framework;
using SqExpress.SqlExport;
using SqExpress.Syntax.Json;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.QueryBuilder;

[TestFixture]
public class JsonTest
{
    [TestCase("$")]
    [TestCase("$.store.book[0].title")]
    [TestCase("$.\"property with spaces\"")]
    public void Path_RoundTrips(string value)
    {
        SqJsonPath path = value;
        Assert.AreEqual(value, (string)path);
        Assert.AreEqual(value, path.ToString());
    }

    [TestCase("")]
    [TestCase("store")]
    [TestCase("$.items[-1]")]
    [TestCase("$.items[*]")]
    [TestCase("$.items[0:2]")]
    [TestCase("$..name")]
    public void Path_RejectsUnsupportedGrammar(string value)
    {
        var error = Assert.Throws<SqExpressException>(() => { SqJsonPath _ = value; });
        StringAssert.Contains("position", error!.Message);
    }

    [Test]
    public void DefaultPath_IsRejectedByBuilder()
        => Assert.Throws<SqExpressException>(() => JsonValue("{}", default));

    [Test]
    public void JsonTableColumn_DefaultPath_IsRejectedByBuilder()
        => Assert.Throws<SqExpressException>(() => JsonTable("[]", "$").Value("Id", default, SqlType.Int32));

    [Test]
    public void Construction_RejectsDuplicateKeys()
        => Assert.Throws<SqExpressException>(() => JsonObject(
                JsonProperty("a", 1),
                JsonProperty("a", 2)));

    [Test]
    public void RootRemoval_IsRejected()
        => Assert.Throws<SqExpressException>(() => JsonRemove("{}", "$"));

    [Test]
    public void ScalarExtraction_UsesEachDialectJsonApi()
    {
        ExprJsonValue value = JsonValue("{\"a\":1}", "$.a", SqlType.Int32);
        StringAssert.Contains("OPENJSON", value.ToSql());
        StringAssert.Contains("jsonb_path_query_first", value.ToPgSql());
        StringAssert.Contains("JSON_EXTRACT", value.ToMySql());
        StringAssert.Contains("json_extract", SqliteExporter.Default.ToSql(value));
    }

    [Test]
    public void TSqlTypedExtraction_UsesParentObjectToPreserveJsonKind()
    {
        var value = JsonValue("{\"item\":{\"active\":true}}", "$.item.active", SqlType.Boolean).ToSql();
        StringAssert.Contains("OPENJSON('{\"item\":{\"active\":true}}','$.\"item\"')", value);
        StringAssert.Contains("[key]='active' AND [type]=3", value);
    }

    [Test]
    public void UnspecifiedJsonDecimal_KeepsFractionalPrecision()
    {
        var value = JsonValue("{\"price\":12.5}", "$.price", SqlType.Decimal());
        StringAssert.Contains("TRY_CONVERT(decimal(38,18),[value])", value.ToSql());
        StringAssert.Contains("AS decimal(38,18)", value.ToMySql());
    }

    [Test]
    public void JsonOutputColumn_IsTerminal()
    {
        var output = Literal(1).AsJson("$.nested.value");
        Assert.Throws<SqExpressException>(() => output.ToSql());
    }

    [Test]
    public void Select_UsesSelectingOverloadForJsonOutputColumns()
    {
        var query = Select(Literal(1).AsJson("$.a"), Literal(1).AsJson("$.a")).ForJson();
        Assert.Throws<SqExpressException>(() => query.ToSql(), "The expression must compile; duplicate output paths are validated during export.");
    }

    [Test]
    public void ForJson_StoresAndExportsPortableOptions()
    {
        var query = Select(Null.As("optional")).ForJson(withoutArrayWrapper: true, includeNullValues: false);
        Assert.That(query.WithoutArrayWrapper, Is.True);
        Assert.That(query.IncludeNullValues, Is.False);
        StringAssert.Contains("JSON_QUERY(J1.Json,'$[0]')", query.ToSql());
        StringAssert.DoesNotContain("INCLUDE_NULL_VALUES", query.ToSql());
        StringAssert.Contains("jsonb_strip_nulls", query.ToPgSql());
        StringAssert.Contains("JSON_MERGE_PATCH", query.ToMySql());
        StringAssert.Contains("json_patch", SqliteExporter.Default.ToSql(query));
    }

    [Test]
    public void PortableOperations_ExportAcrossEveryDialect()
    {
        var expression = Select(
                JsonValue("{\"a\":1}", "$.a", SqlType.Int32).As("Value"),
                JsonQuery("{\"a\":[]}", "$.a").As("Query"),
                JsonSet("{}", "$.a", JsonArray(1, JsonNull())).As("Set"),
                JsonRemove("{\"a\":1}", "$.a").As("Remove"),
                JsonObject(JsonProperty("a", 1)).As("Object"));

        var query = expression.Done();
        StringAssert.Contains("OPENJSON", query.ToSql());
        StringAssert.Contains("jsonb_set", query.ToPgSql());
        StringAssert.Contains("JSON_SET", query.ToMySql());
        StringAssert.Contains("json_set", SqliteExporter.Default.ToSql(query));
    }
}
