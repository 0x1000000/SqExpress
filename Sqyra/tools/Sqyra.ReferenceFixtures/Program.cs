using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Globalization;
using SqExpress;
using SqExpress.SqlExport;
using SqExpress.SqlParser;
using SqExpress.Syntax;
using SqExpress.DbMetadata;

if (args.Length == 2 && args[0] == "--import-simple") { ImportSimpleCases(args[1]); return 0; }
if (args.Length == 2 && args[0] == "--import-samples") { ImportSampleCases(args[1]); return 0; }
if (args.Length != 2) { Console.Error.WriteLine("Usage: Sqyra.ReferenceFixtures <cases.json> <fixtures.json>"); return 2; }
var input = JsonSerializer.Deserialize<Case[]>(File.ReadAllText(args[0]), JsonOptions()) ?? throw new InvalidOperationException("Cases file must contain an array.");
if (input.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != input.Length) throw new InvalidOperationException("Fixture case IDs must be unique.");
var output = input.OrderBy(c => c.Id, StringComparer.Ordinal).Select(Build).ToArray();
File.WriteAllText(args[1], JsonSerializer.Serialize(output, JsonOptions()) + "\n");
Console.WriteLine($"Generated {output.Length} C# reference fixtures."); return 0;

static Fixture Build(Case item)
{
    var options = new SqTSqlParserOptions { DefaultSchema = item.DefaultSchema };
    var existing = item.ExistingTables?.Select(CreateTable).Cast<TableBase>().ToArray();
    bool parsed; IExpr? expression; string? error; IReadOnlyList<SqTable>? tables;
    if (existing is null) parsed = SqTSqlParser.TryParse(item.Sql, options, out expression, out tables, out error);
    else { parsed = SqTSqlParser.TryParse(item.Sql, existing, options, out expression, out error); tables = null; }
    if (!parsed) return new(item.Id, item.Sql, item.DefaultSchema, item.ExistingTables, item.CompareSemantic, false, null, null, null, error, item.ExpectedErrorPart, item.ExpectedErrorExact);
    if (!item.CompareSemantic) return new(item.Id, item.Sql, item.DefaultSchema, item.ExistingTables, false, true, null, null, null, null, item.ExpectedErrorPart, item.ExpectedErrorExact);
    return new(item.Id, item.Sql, item.DefaultSchema, item.ExistingTables, true, true, AstJson(expression!), tables!.Select(t => {
        var name = t.FullName.AsExprTableFullName();
        return new TableArtifact(name.DbSchema?.Database?.Name, name.DbSchema?.Schema.Name, name.TableName.Name,
            t.Columns.Select(c => new ColumnArtifact(c.ColumnName.Name, c.SqlType.GetType().Name, c.IsNullable)).ToArray());
    }).ToArray(), new Dictionary<string, ExportResult> {
        ["tsql"] = Export(() => TSqlExporter.Default.ToSql(expression!)), ["postgresql"] = Export(() => PgSqlExporter.Default.ToSql(expression!)),
        ["mysql"] = Export(() => MySqlExporter.OracleDefault.ToSql(expression!)), ["sqlite"] = Export(() => SqliteExporter.Default.ToSql(expression!))
    }, null, item.ExpectedErrorPart, item.ExpectedErrorExact);
}
static SqTable CreateTable(TableInput input) => SqTable.Create(input.Schema, input.Name, appender =>
{
    foreach (var column in input.Columns) appender = column.Type switch
    {
        "boolean" => appender.AppendBooleanColumn(column.Name),
        "decimal" => appender.AppendDecimalColumn(column.Name),
        "dateTime" => appender.AppendDateTimeColumn(column.Name),
        "string" => appender.AppendStringColumn(column.Name, 255, true),
        _ => appender.AppendInt32Column(column.Name)
    };
    return appender;
});
static JsonElement AstJson(IExpr expression)
{
    using var stream = new MemoryStream(); using (var writer = new Utf8JsonWriter(stream)) expression.SyntaxTree().ExportToJson(writer);
    var node = JsonNode.Parse(stream.ToArray()) ?? throw new InvalidOperationException("AST JSON was empty."); NormalizeExactValues(node);
    return JsonSerializer.SerializeToElement(node);
}
static void NormalizeExactValues(JsonNode node)
{
    if (node is JsonObject obj)
    {
        if (obj["$type"]?.GetValue<string>() == "DecimalLiteral" && obj["Value"] is JsonValue value && value.TryGetValue<decimal>(out var number)) obj["Value"] = number.ToString(CultureInfo.InvariantCulture);
        foreach (var child in obj.ToArray()) if (child.Value is not null) NormalizeExactValues(child.Value);
    }
    else if (node is JsonArray array) foreach (var child in array) if (child is not null) NormalizeExactValues(child);
}
static ExportResult Export(Func<string> render) { try { return new(true, render(), null); } catch (Exception error) { return new(false, null, $"{error.GetType().Name}: {error.Message}"); } }
static JsonSerializerOptions JsonOptions() => new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

static void ImportSimpleCases(string path)
{
    var assemblyPath = Path.GetFullPath("../Test/SqExpress.Test/bin/Debug/net8.0/SqExpress.Test.dll");
    if (!File.Exists(assemblyPath)) throw new InvalidOperationException($"Build the C# test project first: {assemblyPath}");
    var assembly = System.Reflection.Assembly.LoadFrom(assemblyPath);
    var type = assembly.GetType("SqExpress.Test.SqlParser.TSqlParserSimpleObviousCasesTest", true)!;
    var result = new List<Case>(JsonSerializer.Deserialize<Case[]>(File.ReadAllText(path), JsonOptions()) ?? []);
    result.RemoveAll(item => item.Id.StartsWith("SqExpress.Test.SqlParser.TSqlParserSimpleObviousCasesTest.", StringComparison.Ordinal));
    var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (var methodName in new[] { "Cases", "UnhappyCases" })
    {
        var method = type.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static) ?? throw new InvalidOperationException($"Missing {methodName}.");
        foreach (var data in (System.Collections.IEnumerable)(method.Invoke(null, null) ?? throw new InvalidOperationException($"{methodName} returned null.")))
        {
            var arguments = (object[])(data!.GetType().GetProperty("Arguments")!.GetValue(data) ?? throw new InvalidOperationException("Test case has no arguments."));
            var name = System.Text.RegularExpressions.Regex.Replace((string)arguments[0], "_[0-9A-F]{6,8}(?=_|$)", "_Case"); var sql = (string)arguments[1]; var error = arguments.Length > 2 ? arguments[2] as string : null;
            occurrences.TryGetValue(name, out var count); occurrences[name] = ++count;
            result.Add(new($"SqExpress.Test.SqlParser.TSqlParserSimpleObviousCasesTest.{name}#{count}", sql, "dbo", error));
        }
    }
    File.WriteAllText(path, JsonSerializer.Serialize(result, JsonOptions()) + "\n");
    Console.WriteLine("Imported SqTSqlParser simple-obvious source cases.");
}

static void ImportSampleCases(string path)
{
    var assemblyPath = Path.GetFullPath("../Test/SqExpress.Test/bin/Debug/net8.0/SqExpress.Test.dll");
    var assembly = System.Reflection.Assembly.LoadFrom(assemblyPath);
    var type = assembly.GetType("SqExpress.Test.SqlParser.TSqlParserSamplesTest", true)!;
    var field = type.GetField("AllSamples", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static) ?? throw new InvalidOperationException("Missing AllSamples.");
    var result = new List<Case>(JsonSerializer.Deserialize<Case[]>(File.ReadAllText(path), JsonOptions()) ?? []);
    result.RemoveAll(item => item.Id.StartsWith("SqExpress.Test.SqlParser.TSqlParserSamplesTest.", StringComparison.Ordinal));
    var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (var sample in (System.Collections.IEnumerable)(field.GetValue(null) ?? throw new InvalidOperationException("AllSamples is null.")))
    {
        var sampleType = sample!.GetType(); string Read(string name) => (string)(sampleType.GetProperty(name)!.GetValue(sample) ?? "");
        string? ReadNullable(string name) => sampleType.GetProperty(name)!.GetValue(sample) as string;
        var name = Read("Name"); var sql = Read("Sql"); var reason = ReadNullable("UnsupportedReason");
        var ids = new List<string> { name, "Pg_" + name }; if (name.StartsWith("Structured_", StringComparison.Ordinal)) ids.Add(name);
        foreach (var id in ids.Distinct(StringComparer.Ordinal)) { occurrences.TryGetValue(id, out var count); occurrences[id] = ++count; var full = $"SqExpress.Test.SqlParser.TSqlParserSamplesTest.{id}#{count}"; result.Add(new(full, sql, "dbo", reason, reason)); }
    }
    File.WriteAllText(path, JsonSerializer.Serialize(result, JsonOptions()) + "\n");
    Console.WriteLine("Imported SqTSqlParser sample source cases.");
}

sealed record Case(string Id, string Sql, string? DefaultSchema = "dbo", string? ExpectedErrorPart = null, string? ExpectedErrorExact = null, TableInput[]? ExistingTables = null, bool CompareSemantic = true);
sealed record TableInput(string? Schema, string Name, ColumnInput[] Columns);
sealed record ColumnInput(string Name, string Type = "int32");
sealed record Fixture(string Id, string Sql, string? DefaultSchema, TableInput[]? ExistingTables, bool CompareSemantic, bool Success, JsonElement? Ast, TableArtifact[]? Tables, Dictionary<string, ExportResult>? Exports, string? Error, string? ExpectedErrorPart, string? ExpectedErrorExact)
{
    public string Producer => "SqExpress.CSharp";
}
sealed record TableArtifact(string? Database, string? Schema, string Name, ColumnArtifact[] Columns);
sealed record ColumnArtifact(string Name, string SqlType, bool Nullable);
sealed record ExportResult(bool Success, string? Sql, string? Error);
