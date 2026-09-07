# Cross-Dialect JSON Support — Design Brainstorm

## Status

This document is a design brainstorm, not a statement of currently supported behavior.

The proposed direction is to model portable JSON semantics in the SqExpress AST and let each SQL exporter choose the appropriate native syntax. It should not be implemented as a collection of caller-supplied vendor function names.

## Problem

SqExpress currently permits database-specific JSON work through `ScalarFunctionSys`, `TableFunctionSys`, casts, and `UnsafeValue`. The integration scenario in `Test/SqExpress.IntTest/Scenarios/ScJsonTableFunction.cs` demonstrates the resulting limitation: SQL Server and PostgreSQL require separate query branches, and the PostgreSQL branch embeds an exporter-produced column expression inside `UnsafeValue` to add a native `::json` cast.

There are four related but distinct JSON use cases:

1. Read scalar values and document fragments from JSON.
2. Modify or construct JSON documents.
3. Expand JSON arrays or objects into relational rows.
4. Turn relational rows into JSON documents.

The first two form the smallest useful read/write increment. Row expansion is the next high-value feature because it removes the dialect branching already present in the integration tests. Relational-to-JSON output is more structural and should follow later.

Serialization of the SqExpress AST through `ExportToJson` is unrelated to SQL JSON support and should remain a separate concept in documentation and naming.

## Recommended Public API Shape

Use `SqJson` as the public facade, with a structural `JsonPath`. The examples below are proposed APIs, not compilable examples of existing functionality. AST classes retain the established `Expr...` naming convention.

```csharp
var cityPath = JsonPath.Root
    .Property("address")
    .Property("city");

var city = SqJson.Value(customer.Profile, cityPath, SqlType.String());

var update = Update(customer)
    .Set(
        customer.Profile,
        SqJson.Set(
            customer.Profile,
            cityPath,
            SqJson.FromSql("Toronto")))
    .Where(customer.Id == customerId);
```

Document extraction and mutation might look like this:

```csharp
SqJson.Query(document, JsonPath.Root.Property("items"));

SqJson.Set(document, path, SqJson.FromSql(order.Total, SqlType.Decimal()));
SqJson.Set(document, path, SqJson.FromSql(true));
SqJson.Set(document, path, SqJson.Parse(otherDocument));
SqJson.Set(document, path, SqJson.Null);

SqJson.Remove(document, path);
```

Persisting a modified document does not require a new DML abstraction. JSON operations should produce an `ExprValue`, which can already be passed to update and insert builders.

## JSON Input Kinds

JSON construction and mutation must distinguish the intended JSON value kind explicitly:

- `SqJson.FromSql(expression, sqlType)` converts an SQL scalar to a JSON value, preserving its intended string, number, or Boolean kind. Convenience overloads can infer the type for CLR literals and known typed columns; arbitrary expressions require an explicit type when inference is insufficient.
- `SqJson.Parse(expression)` interprets existing text or native JSON as JSON, rather than quoting its contents. It is an SQL expression, not client-side deserialization or unsafe SQL insertion.
- `SqJson.Query(document, path)` extracts an object or array as JSON.
- `SqJson.Null` stores JSON `null`.
- `SqJson.Remove(document, path)` removes a member; array-item removal requires a separately verified semantic contract.
- SQL `NULL` remains a separate concept.

This distinction is essential. The supported databases do not consistently infer whether an SQL string containing `{"x":1}` is an existing JSON fragment or a string that must be escaped and quoted. A portable API must carry that intent in the AST.

In particular, setting a member to JSON `null` must not be represented by the same operation as removing it. SQL Server's `JSON_MODIFY` makes this distinction especially important because a SQL `NULL` argument in its usual lax mode deletes an object property.

## Structural Paths

The portable API should not be based solely on caller-supplied JSONPath strings. Use an immutable structural representation:

```csharp
JsonPath.Root
    .Property("foo")
    .Index(0)
    .Property("bar")
```

The first version should support only:

- the document root;
- object-property segments;
- zero-based array-index segments.

This allows each exporter to escape property names safely and translate the path into its native form. PostgreSQL, for example, can receive separate path elements instead of requiring SqExpress to parse and translate a raw SQL/JSON path string.

Wildcards, filters, recursive descent, negative indexes, append positions, and dynamic path expressions should be deferred. Existing native functions and `UnsafeValue` remain escape hatches for advanced dialect-specific behavior.

## Core Operation Matrix

The matrix below describes likely exporter strategies rather than exact required output:

| Portable intent | SQL Server | PostgreSQL | MySQL / MariaDB | SQLite |
|---|---|---|---|---|
| Scalar text | `JSON_VALUE` | `jsonb_extract_path_text` | `JSON_UNQUOTE(JSON_EXTRACT(...))` or a compatible `JSON_VALUE` | `json_extract` with result normalization where necessary |
| Object/array fragment | `JSON_QUERY` | `jsonb_extract_path` | `JSON_EXTRACT` | `json_extract`, with `json()` where JSON provenance must be retained |
| Set/update final path | `JSON_MODIFY` | `jsonb_set` | `JSON_SET` | `json_set` |
| Remove path | `JSON_MODIFY(..., NULL)` | path deletion with `#-` | `JSON_REMOVE` | `json_remove` |
| Construct object | `JSON_OBJECT` when supported | `jsonb_build_object` | `JSON_OBJECT` | `json_object` |
| Construct array | `JSON_ARRAY` when supported | `jsonb_build_array` | `JSON_ARRAY` | `json_array` |
| Expand array to rows | `OPENJSON` | `jsonb_array_elements` | `JSON_TABLE` | `json_each` |

Relevant vendor references:

- [SQL Server JSON data](https://learn.microsoft.com/en-us/sql/relational-databases/json/json-data-sql-server)
- [PostgreSQL JSON functions and operators](https://www.postgresql.org/docs/current/functions-json.html)
- [MySQL JSON functions](https://dev.mysql.com/doc/refman/8.4/en/json-functions.html)
- [MariaDB JSON functions](https://mariadb.com/docs/server/reference/sql-functions/special-functions/json-functions)
- [SQLite JSON functions](https://sqlite.org/json1.html)

## Portable Semantic Contract

The following is a desired contract to validate through integration tests, not a claim that the candidate native functions are equivalent:

- Input documents are expected to contain valid JSON. Invalid-document behavior is otherwise too inconsistent to normalize cheaply.
- A missing path produces SQL `NULL` when extracting a value.
- `SqJson.Value` extracts an SQL scalar of the requested type. Missing values and JSON null should produce SQL `NULL`. Container values, conversion failures, overflow, and scalar coercion rules must be specified before claiming portability; a native cast alone does not establish equivalent conversion behavior.
- `SqJson.Query` extracts objects or arrays. Decide explicitly whether a scalar or wrong container type returns SQL `NULL` or raises an error.
- Start mutation with object members under an existing object parent. `Set` may create or replace that final member; `Remove` should leave a missing final member unchanged. Missing parents, wrong container types, root replacement, and array insertion/removal are separate cases to specify and verify.
- Proposed SQL-null policy: a SQL-null document propagates SQL `NULL`; `FromSql` maps a SQL-null value to JSON null; `Parse` propagates SQL `NULL`. Mutation and construction must normalize nullable JSON expressions explicitly so they cannot accidentally trigger deletion. This policy needs integration verification.
- SqExpress array ordinality is always zero-based, even when a native row source reports one-based ordinality.
- Object construction includes properties whose value is JSON `null` unless the caller explicitly asks for an absent-on-null policy in a future API.
- Array aggregation is deterministic only when the caller supplies ordering.
- If an exporter cannot preserve the documented semantics for its configured target, it throws a clear `SqExpressException` instead of emitting an approximation.

Some of these rules require more SQL than a direct one-to-one function translation. For example, PostgreSQL extraction functions can return textual representations for values that SQL Server's `JSON_VALUE` considers non-scalar. The exporter may need a type guard if SqExpress promises identical scalar-versus-container behavior.

## AST Integration

### Recommended direction and compatibility

Use dedicated JSON nodes from the start. This supersedes the earlier suggestion to encode JSON operations and path segments as positional arguments in `ExprPortableScalarFunction`, or to add a portable discriminator to `ExprTableFunction`. Explicit fields preserve meaning for exporters, validators, rewriting tools, and code generation.

New node dispatch methods affect public visitor implementations. Review source and binary compatibility, including the no-argument visitor API, before implementation and choose an appropriate release or extension strategy. Do not assume that regenerating internal visitors makes the public change backward compatible.

### Path model

The following declarations are structural sketches, with constructors and visitor plumbing omitted:

```text
JsonPath
    Segments: IReadOnlyList<JsonPathSegment>

JsonPropertySegment : JsonPathSegment
    Name: string

JsonIndexSegment : JsonPathSegment
    Index: int  // Non-negative.
```

Paths can be immutable metadata initially because they contain no SQL expressions. Their serialization and reconstruction still require explicit support. Dynamic path expressions would require revisiting their traversal model.

### Scalar reads and JSON-producing expressions

```text
ExprJson : ExprValue  // Abstract base: denotes a JSON-valued SQL expression.

ExprJsonValue : ExprValue
    Document: ExprValue
    Path: JsonPath
    Returning: ExprType

ExprJsonQuery : ExprJson
    Document: ExprValue
    Path: JsonPath

ExprJsonFromSql : ExprJson
    Value: ExprValue
    SqlType: ExprType

ExprJsonParse : ExprJson
    Value: ExprValue

ExprJsonNull : ExprJson

ExprJsonSet : ExprJson
    Document: ExprValue
    Path: JsonPath
    Value: ExprJson

ExprJsonRemove : ExprJson
    Document: ExprValue
    Path: JsonPath
```

`ExprJson` is an expression category, not a new physical SQL storage type or a guarantee of runtime validity/non-nullness. Document inputs accept existing string columns through `ExprValue`; replacement values require an explicit JSON-producing expression. `Returning` and `SqlType` initially accept only a documented subset of scalar types.

Separating conversion, parsing, and extraction removes the ambiguous overloaded `Fragment` name:

```csharp
SqJson.FromSql("{\"x\":1}"); // A JSON string containing {"x":1}.
SqJson.Parse("{\"x\":1}");   // A JSON object with property x.
SqJson.Query(document, path);  // An object/array extracted from a document.
```

Parsing arbitrary JSON scalars is a desired capability, but exporter/version support must be established separately from object/array fragment support.

### Construction nodes

```text
ExprJsonObject : ExprJson
    Members: IReadOnlyList<ExprJsonMember>

ExprJsonMember : IExpr
    Name: string
    Value: ExprJson

ExprJsonArray : ExprJson
    Items: IReadOnlyList<ExprJson>
```

Begin with static member names and reject duplicate names at construction. Empty objects and arrays should be supported. Dynamic keys, absent-on-null policies, and broader SQL-type conversion can follow after their semantics are defined.

### JSON table projection

Use one structural table-source model for both simple array expansion and typed projection:

```text
ExprJsonTable : IExprTableSource
    Document: ExprValue
    Path: JsonPath  // Selects the array to expand.
    Columns: IReadOnlyList<ExprJsonTableColumn>
    Alias: ExprTableAlias

ExprJsonTableColumn : IExpr  // Abstract base.
    Name: ExprColumnName

ExprJsonScalarColumn : ExprJsonTableColumn
    Path: JsonPath  // Relative to each element.
    SqlType: ExprType

ExprJsonFragmentColumn : ExprJsonTableColumn
    Path: JsonPath

ExprJsonOrdinalColumn : ExprJsonTableColumn
    // Produces a zero-based index.
```

Illustrative builder API:

```csharp
var items = SqJson.Table(order.Payload, JsonPath.Root.Property("items"))
    .Value("ProductId", JsonPath.Root.Property("productId"), SqlType.Int32)
    .Value("Quantity", JsonPath.Root.Property("quantity"), SqlType.Int32)
    .Ordinal("Index")
    .As("items");
```

A root-path scalar column plus an ordinal column covers typed scalar-array expansion. A root-path fragment column covers object/array elements. Mixed arrays requiring a lossless JSON value for every element may need an additional JSON-value column kind; do not conflate that with object/array-only fragment extraction.

The table builder should expose typed column references and selecting metadata, and integrate with existing correlation and `CrossApply`/`OuterApply` semantics. Missing/null arrays, non-array inputs, empty arrays, and preservation of outer rows require explicit tests. Exporters can use native table functions or projections over them; typed projection plus ordinality may require more than one native construct. No separate portable table-function enum is proposed.

### AST consumers

Implement dispatch, traversal, modification, serialization/deserialization, type analysis, selecting metadata, table rebinding, and Roslyn-based transpiler output for the new nodes as applicable. JSON member values and table input expressions must remain visible to visitors. Update generators for generated regions and verify path metadata round-trips; do not presume existing generation understands the new property shapes automatically.

## Storage Types

Portable JSON operations should initially accept any `ExprValue`, including existing string columns. Do not make a native JSON column type a prerequisite for query support.

The databases have substantially different storage models:

- PostgreSQL distinguishes `json` and `jsonb`.
- MySQL has a native binary JSON representation.
- MariaDB's `JSON` behavior and storage differ from Oracle MySQL despite their shared function vocabulary.
- SQL Server historically stores JSON in character columns and newer versions add native JSON capabilities.
- SQLite can operate on text JSON and has evolving JSONB support.

SqExpress metadata currently maps a MySQL `json` column to `StringColumnType`. Changing that behavior would affect table generation, metadata comparison, typed columns, visitors, and public APIs. A future `ExprTypeJson` or `JsonTableColumn` should therefore be treated as a separate storage-policy project.

Possible future storage options might distinguish logical JSON from physical preference:

```text
JsonStoragePreference.PortableText
JsonStoragePreference.Native
JsonStoragePreference.BinaryWhenAvailable
```

This should not block the operation-level API.

## Document Construction and Relational-to-JSON Output

Object and array construction are useful write operations but need explicit capability handling, particularly for older SQL Server targets. SqExpress exporters currently do not generally capture a database server version. The implementation should either:

1. emit only syntax valid for a documented conservative baseline;
2. introduce narrowly scoped JSON capability options; or
3. provide proven semantic polyfills.

It should not automatically emit the newest available syntax without knowing the target server capability.

Relational-to-JSON output is more complex than scalar construction. A future API might use explicit composition:

```csharp
SqJson.ArrayAgg(
    SqJson.Object(
        SqJson.Property("id", SqJson.FromSql(order.Id, SqlType.Int32)),
        SqJson.Property("name", SqJson.FromSql(order.Name, SqlType.String()))),
    orderBy: new[] { Asc(order.Id) })
```

Important design questions include:

- ordering of array elements;
- empty input: SQL `NULL` versus `[]`;
- duplicate object keys;
- include-null versus absent-on-null;
- embedding nested JSON without double encoding;
- SQL Server targets where `FOR JSON` is the available mechanism rather than an aggregate expression.

A dedicated `QueryToJson` structural node may ultimately be better than forcing SQL Server's `FOR JSON` semantics into an ordinary aggregate function.

## Parser Scope

Implement exporters and builders before extending `SqTSqlParser`.

The parser currently rejects `FOR JSON`, and that should remain fail-closed until there is a corresponding structural AST model. Scalar calls such as `JSON_VALUE`, `JSON_QUERY`, and `JSON_MODIFY` could later be recognized and mapped to portable JSON operations. `OPENJSON` and `FOR JSON` require more deliberate parser and mapper work.

When parser support claims change, update `SqExpress/SqlParser/TSqlParserSupportedSubset.md` together with tests.

## Verification Strategy

JSON support needs semantic tests, not only exporter snapshots.

### Exporter tests

For each portable operation, verify SQL output for:

- `TSqlExporter`;
- `PgSqlExporter`;
- `MySqlExporter.OracleDefault`;
- `MySqlExporter.MariaDbDefault`;
- `SqliteExporter`.

### AST tests

Every new logical operation must be covered by:

- read-only traversal;
- modification/rewrite traversal;
- AST JSON export/import round-trip;
- table rebinding where table and column expressions occur inside JSON operations.
- type analysis and selecting metadata;
- structural path serialization and reconstruction;
- public visitor compatibility and Roslyn-based transpiler generation.

If generated traversal rules change, update the generator, run `SqExpress\codegen.cmd`, and verify both generated output and downstream tests.

### Integration semantics

At minimum, test:

- missing paths;
- SQL-null documents;
- JSON `null` versus member removal;
- quoted and Unicode property names;
- zero-based array indexes;
- string escaping;
- numbers and Booleans;
- nested fragments that must not be double encoded;
- setting an existing member;
- creating a final member under an existing parent;
- removing existing and missing members;
- array expansion and normalized ordinality.

The existing `ScJsonTableFunction` scenario should eventually become a single portable query executed for all supported integration-test dialects.

## Suggested Delivery Slices

### Slice 1 — Core document read/write

- `JsonPath` with property and index segments.
- Explicit JSON input kinds.
- Dedicated `ExprJson` nodes and a reviewed visitor compatibility strategy.
- `SqJson.Value` with an initially limited set of scalar return types.
- `SqJson.Query` extraction and `SqJson.Parse` interpretation.
- `SqJson.FromSql` and `SqJson.Null`.
- `SqJson.Set` and `SqJson.Remove`, initially for object-member mutation.
- Exporter tests and AST round-trip coverage.

This is the smallest increment that supports both reading and modifying stored JSON.

### Slice 2 — Portable array expansion

- `ExprJsonTable` with root-path scalar or fragment columns for array expansion.
- Standardized typed values and zero-based ordinal columns.
- Replace dialect branches in `ScJsonTableFunction`.
- Integration coverage for every supported database where the required JSON extension is available.

### Slice 3 — Document construction

- `SqJson.Object` and `SqJson.Array` with dedicated construction nodes.
- Null-handling rules.
- Exporter capability policy for version-sensitive syntax.

### Slice 4 — Typed JSON tables and aggregation

- Extend `SqJson.Table(...).Value(...).Ordinal(...)` to richer typed projections using the same table node.
- Ordered `SqJson.ArrayAgg`.
- `SqJson.ObjectAgg` only after duplicate-key semantics are defined.
- Empty-input normalization.
- Possible `QueryToJson` model.

### Slice 5 — Parser and storage types

- Portable mapping of supported T-SQL JSON functions.
- Structural parsing for `OPENJSON` and `FOR JSON` when corresponding AST nodes exist.
- Optional logical JSON column/type support as a separate compatibility-reviewed feature.

## Recommended First Milestone

The first milestone should be:

> Structural paths, scalar and fragment reads, set and remove writes, explicit JSON value kinds, and dialect-neutral array expansion.

That milestone addresses the current unsafe PostgreSQL workaround, supplies meaningful read/write functionality, avoids prematurely committing to native JSON storage, and leaves advanced JSONPath, aggregation, and parser syntax for later focused changes.
