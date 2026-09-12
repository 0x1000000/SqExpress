using NUnit.Framework;
using SqExpress.SqlParser;
using SqExpress.Syntax.Json;

namespace SqExpress.Test.SqlParser;

[TestFixture]
public class TSqlParserJsonTest
{
    [Test]
    public void JsonValue_LiteralPath_MapsToPortableNode()
    {
        var query = SqTSqlParser.Parse("SELECT JSON_VALUE('{\"a\":1}', '$.a') AS A");
        StringAssert.Contains("JSON_VALUE", query.ToSql());
    }

    [Test]
    public void JsonQuery_LiteralPath_MapsToPortableNode()
    {
        var query = SqTSqlParser.Parse("SELECT JSON_QUERY('{\"a\":[]}', '$.a') AS A");
        StringAssert.Contains("JSON_QUERY", query.ToSql());
    }

    [Test]
    public void JsonConstruction_MapsToPortableNodes()
    {
        var array = SqTSqlParser.Parse("SELECT JSON_ARRAY(1, 'a', NULL NULL ON NULL) AS A");
        StringAssert.Contains("JSON_ARRAY", array.ToSql());
        var obj = SqTSqlParser.Parse("SELECT JSON_OBJECT('a':1, 'b':'x' NULL ON NULL) AS A");
        StringAssert.Contains("JSON_OBJECT", obj.ToSql());
    }

    [Test]
    public void ForJsonPath_MapsDottedAliasesAndTerminalQuery()
    {
        var query = SqTSqlParser.Parse("SELECT 1 AS [id], 'Toronto' AS [address.city] FOR JSON PATH, INCLUDE_NULL_VALUES");
        var sql = query.ToSql();
        StringAssert.EndsWith("FOR JSON PATH, INCLUDE_NULL_VALUES", sql);
        StringAssert.Contains("[address.city]", sql);
    }

    [Test]
    public void ForJsonPath_MapsPortableOptions()
    {
        var query = SqTSqlParser.Parse("SELECT 1 AS [id] FOR JSON PATH, WITHOUT_ARRAY_WRAPPER");
        var asJson = (ExprQueryAsJson)query;
        Assert.That(asJson.WithoutArrayWrapper, Is.True);
        Assert.That(asJson.IncludeNullValues, Is.False);
        StringAssert.Contains("JSON_QUERY(J1.Json,'$[0]')", query.ToSql());
    }

    [Test]
    public void OpenJsonWith_MapsTypedAndFragmentColumns()
    {
        var query = SqTSqlParser.Parse("SELECT j.Id, j.Data FROM OPENJSON('[{\"id\":1}]', '$') WITH (Id int '$.id', Data nvarchar(max) '$.data' AS JSON) j");
        var sql = query.ToSql();
        StringAssert.Contains("OPENJSON", sql);
        StringAssert.Contains("AS JSON", sql);
    }

    [Test]
    public void OpenJsonWith_DefaultRootPath_IsSupported()
    {
        var query = SqTSqlParser.Parse("SELECT j.Id FROM OPENJSON('[{\"id\":1}]') WITH (Id int '$.id') j");
        StringAssert.Contains("OPENJSON", query.ToSql());
    }

#pragma warning disable SQEX010
    [Test]
    public void BareOpenJson_IsRejected()
        => Assert.Throws<SqExpressTSqlParserException>(() => SqTSqlParser.Parse("SELECT j.value FROM OPENJSON('[]') j"));
#pragma warning restore SQEX010

#pragma warning disable SQEX010
    [Test]
    public void JsonValue_DynamicPath_IsRejected()
        => Assert.Throws<SqExpressTSqlParserException>(() => SqTSqlParser.Parse("SELECT JSON_VALUE('{}', @path) AS A"));

    [Test]
    public void JsonModify_Append_IsRejected()
        => Assert.Throws<SqExpressTSqlParserException>(() => SqTSqlParser.Parse("SELECT JSON_MODIFY('{}', 'append $.a', 1) AS A"));
#pragma warning restore SQEX010
}
