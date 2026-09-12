using System;
using System.Collections.Generic;
using SqExpress.Internal;
using SqExpress.Syntax.Json;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Value;

namespace SqExpress.SqlExport.Internal;

internal sealed class JsonOutputShape
{
    public JsonOutputShape(string name)
    {
        this.Name = name;
    }

    public string Name { get; }

    public string? ColumnName { get; private set; }

    public bool IsJson { get; private set; }

    public List<JsonOutputShape> Children { get; } = [];

    public static IReadOnlyList<JsonOutputShape> Build(IExprQuery query)
    {
        var roots = new List<JsonOutputShape>();
        var jsonColumnIndex = 0;
        foreach (var selecting in query.ExtractSelecting())
        {
            string column;
            bool leafIsJson;
            IReadOnlyList<SqJsonPathSegment> segments;
            if (selecting is ExprJsonOutputColumn json)
            {
                column = InternalColumnName(jsonColumnIndex++);
                segments = SqJsonPathParser.Parse(json.JsonPath).Segments;
                leafIsJson = IsJsonValue(json.Value);
            }
            else if (selecting is IExprNamedSelecting named && named.OutputName != null)
            {
                column = named.OutputName;
                segments = [SqJsonPathSegment.ForProperty(named.OutputName)];
                leafIsJson = IsJsonValue(selecting);
            }
            else
            {
                throw new SqExpressException("ForJson() requires every selected expression to have an output name.");
            }

            var current = roots;
            JsonOutputShape? leaf = null;
            foreach (var segment in segments)
            {
                if (segment.Kind != SqJsonPathSegmentKind.Property)
                    throw new SqExpressException("ForJson() output paths may contain object properties only.");
                var name = segment.Property!;
                leaf = current.Find(i => string.Equals(i.Name, name, StringComparison.Ordinal));
                if (leaf == null)
                {
                    leaf = new JsonOutputShape(name);
                    current.Add(leaf);
                }

                if (leaf.ColumnName != null)
                    throw new SqExpressException($"Conflicting JSON output path at property '{name}'.");
                current = leaf.Children;
            }

            if (leaf == null || leaf.Children.Count > 0 || leaf.ColumnName != null)
                throw new SqExpressException("Duplicate or conflicting JSON output path.");
            leaf.ColumnName = column;
            leaf.IsJson = leafIsJson;
        }

        return roots;
    }

    public static string InternalColumnName(int index) => "__sq_json_" + index;

    private static bool IsJsonValue(IExprSelecting selecting)
        => selecting is ExprJson
           || selecting is ExprValueQuery { Query: ExprQueryAsJson }
           || selecting is ExprAliasedSelecting { Value: ExprJson }
           || selecting is ExprAliasedSelecting { Value: ExprValueQuery { Query: ExprQueryAsJson } };
}
