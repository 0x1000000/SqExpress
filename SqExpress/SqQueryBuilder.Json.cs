using System;
using System.Collections.Generic;
using SqExpress.Internal;
using SqExpress.QueryBuilders;
using SqExpress.QueryBuilders.Json;
using SqExpress.Syntax.Json;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress;

public static partial class SqQueryBuilder
{
    /// <summary>Extracts a JSON string scalar at the specified path.</summary>
    /// <param name="document">A SQL expression containing a JSON document.</param>
    /// <param name="path">The path of the value to extract.</param>
    /// <returns>A nullable string expression.</returns>
    public static ExprJsonValue JsonValue(ExprValue document, SqJsonPath path)
        => new(document, RequirePath(path), null);

    /// <summary>Extracts a JSON scalar at the specified path and converts it to a supported SQL type.</summary>
    /// <param name="document">A SQL expression containing a JSON document.</param>
    /// <param name="path">The path of the value to extract.</param>
    /// <param name="returningType">The string, Boolean, integer, decimal, or double result type.</param>
    /// <returns>A nullable typed scalar expression.</returns>
    public static ExprJsonValue JsonValue(ExprValue document, SqJsonPath path, ExprType returningType)
    {
        JsonTableBuilder.ValidateType(returningType);
        return new ExprJsonValue(document, RequirePath(path), returningType);
    }

    /// <summary>Extracts an object or array fragment at the specified path.</summary>
    /// <param name="document">A SQL expression containing a JSON document.</param>
    /// <param name="path">The path of the object or array to extract.</param>
    /// <returns>A nullable JSON fragment expression.</returns>
    public static ExprJsonQuery JsonQuery(ExprValue document, SqJsonPath path) => new(document, RequirePath(path));

    /// <summary>Validates and marks a root object or array as a JSON fragment.</summary>
    /// <param name="document">A SQL expression containing a root JSON object or array.</param>
    /// <returns>A nullable JSON fragment expression.</returns>
    public static ExprJsonQuery JsonQuery(ExprValue document) => new(document, "$");

    /// <summary>Creates an expression representing the JSON null value.</summary>
    /// <returns>A JSON null expression.</returns>
    public static ExprJsonNull JsonNull() => ExprJsonNull.Instance;

    /// <summary>Creates or replaces a JSON value at the specified path.</summary>
    /// <param name="document">A SQL expression containing a JSON document.</param>
    /// <param name="path">The path to modify.</param>
    /// <param name="value">A scalar value or JSON fragment to store.</param>
    /// <returns>The modified JSON document expression.</returns>
    public static ExprJsonSet JsonSet(ExprValue document, SqJsonPath path, ExprValue value)
        => new(document, RequirePath(path), value);

    /// <summary>Removes a value from a JSON document.</summary>
    /// <param name="document">A SQL expression containing a JSON document.</param>
    /// <param name="path">The non-root path to remove.</param>
    /// <returns>The modified JSON document expression.</returns>
    public static ExprJsonRemove JsonRemove(ExprValue document, SqJsonPath path)
    {
        var value = RequirePath(path);
        if (value == "$") throw new SqExpressException("The JSON document root cannot be removed.");
        return new ExprJsonRemove(document, value);
    }

    /// <summary>Defines a statically named member for <see cref="JsonObject"/>.</summary>
    /// <param name="name">The JSON property name.</param>
    /// <param name="value">A scalar value or JSON fragment.</param>
    /// <returns>A JSON object member definition.</returns>
    public static ExprJsonMember JsonProperty(string name, ExprValue value)
    {
        if (string.IsNullOrEmpty(name)) throw new SqExpressException("JSON property name cannot be empty.");
        return new ExprJsonMember(name, value);
    }

    /// <summary>Constructs a JSON object from statically named members.</summary>
    /// <param name="members">The members to include.</param>
    /// <returns>A JSON object expression.</returns>
    public static ExprJsonObject JsonObject(params ExprJsonMember[] members)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in members)
            if (!names.Add(member.Name))
                throw new SqExpressException($"Duplicate JSON property '{member.Name}'.");
        return new ExprJsonObject(members);
    }

    /// <summary>Constructs a JSON array, preserving item order and SQL null values.</summary>
    /// <param name="items">Scalar values or JSON fragments to include.</param>
    /// <returns>A JSON array expression.</returns>
    public static ExprJsonArray JsonArray(params ExprValue[] items) => new(items);

    /// <summary>Begins a typed relational projection over a JSON array.</summary>
    /// <param name="document">A SQL expression containing a JSON document.</param>
    /// <param name="path">The path of the array to expand.</param>
    /// <returns>A JSON table builder.</returns>
    public static JsonTableBuilder JsonTable(ExprValue document, SqJsonPath path) => new(document, RequirePath(path));

    /// <summary>Assigns a terminal object-member path to a selection consumed by <c>ForJson()</c>.</summary>
    /// <param name="expression">The selection to place in the JSON output.</param>
    /// <param name="path">A property-only output path.</param>
    /// <returns>A terminal JSON output selection.</returns>
    public static ExprJsonOutputColumn AsJson(this IExprSelecting expression, SqJsonPath path)
    {
        var value = RequirePath(path);
        SqJsonPathParser.ValidateOutputPath(value);
        return new ExprJsonOutputColumn(expression, value);
    }

    /// <summary>Serializes a completed query as JSON.</summary>
    /// <param name="query">The query builder to serialize.</param>
    /// <param name="withoutArrayWrapper">Return a single object instead of an array; zero rows return SQL null and multiple rows fail.</param>
    /// <param name="includeNullValues">Include properties whose SQL value is null.</param>
    /// <returns>A query producing one <c>Json</c> column.</returns>
    public static ExprQueryAsJson ForJson(this IExprQueryFinal query, bool withoutArrayWrapper = false, bool includeNullValues = true)
        => new(query.Done(), withoutArrayWrapper, includeNullValues);

    /// <summary>Serializes a query as JSON.</summary>
    /// <param name="query">The query to serialize.</param>
    /// <param name="withoutArrayWrapper">Return a single object instead of an array; zero rows return SQL null and multiple rows fail.</param>
    /// <param name="includeNullValues">Include properties whose SQL value is null.</param>
    /// <returns>A query producing one <c>Json</c> column.</returns>
    public static ExprQueryAsJson ForJson(this IExprQuery query, bool withoutArrayWrapper = false, bool includeNullValues = true)
        => new(query, withoutArrayWrapper, includeNullValues);

    private static string RequirePath(SqJsonPath path)
    {
        if (path.Value == null) throw new SqExpressException("JSON path cannot be default or null.");
        SqJsonPathParser.Parse(path.Value);
        return path.Value;
    }
}
