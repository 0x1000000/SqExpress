# Cross-Dialect JSON

SqExpress provides a portable JSON model for SQL Server 2022+, PostgreSQL 12+, MySQL 8.0+, MariaDB 10.6+, and SQLite with JSON1. JSON documents are ordinary string expressions; portable native JSON column metadata is outside version 1.

## Helper reference

These helpers are members of `SqQueryBuilder`.

| Helper | Purpose |
|---|---|
| `JsonValue(document, path)` | Extract a JSON string scalar. |
| `JsonValue(document, path, type)` | Strictly extract and convert a scalar to string, Boolean, Int32, Int64, decimal, or double. |
| `JsonQuery(document, path)` | Extract an object or array fragment; scalar values return SQL `NULL`. |
| `JsonQuery(document)` | Validate and mark a root object or array as embedded JSON; equivalent to path `$`. |
| `JsonNull()` | Produce JSON null rather than removing a value. |
| `JsonSet(document, path, value)` | Create or replace a final object member, or replace an existing array element. |
| `JsonRemove(document, path)` | Remove a non-root member or array element. |
| `JsonProperty(name, value)` | Define a statically named member for `JsonObject`. |
| `JsonObject(properties...)` | Construct an object. |
| `JsonArray(items...)` | Construct an array while preserving item order. |
| `JsonTable(document, path)` | Expand an array into a typed relational table. |
| `selection.AsJson(path)` | Map a selected value to a nested property path for `ForJson`. |
| `query.ForJson(...)` | Serialize relational rows as a JSON array or single object. |

`JsonTable` continues with:

| Member | Purpose |
|---|---|
| `.Value(name, path, type)` | Add a strictly typed scalar output column. |
| `.Query(name, path)` | Add an object-or-array fragment output column. |
| `.Ordinal(name)` | Add a zero-based array-position column. |
| `.As(alias)` | Complete the table source and assign its alias. |

String, numeric, Boolean, and SQL-null values convert to `ExprValue`, so `Literal(...)` is normally unnecessary:

```csharp
JsonTable("[1,2]", "$")
JsonSet("{}", "$.status", "active")
JsonArray(1, "two", JsonNull())
```

## Paths

JSON APIs take `SqJsonPath`. Strings convert implicitly; conversion back to string is explicit.

```csharp
SqJsonPath path = "$.customer.address.city";
string text = (string)path;
```

The grammar accepts root `$`, quoted and unquoted properties, and zero-based nonnegative indexes: `$.name`, `$.items[0].price`, `$."property with spaces"`. Wildcards, ranges, filters, recursive descent, negative indexes, malformed paths, and `default(SqJsonPath)` are rejected. The analyzer diagnoses invalid constant paths; dynamic paths are validated when constructed.

## Reading

In the following example, `customer` is an existing SqExpress table descriptor and `customer.Profile` is the column containing the JSON document. SqExpress treats `Profile` portably as a string expression; the physical PostgreSQL column may be `jsonb` (and other databases may use their native JSON storage type) as long as the exporter/database mapping supplies the JSON document expected by these helpers.

```csharp
var query = Select(
        JsonValue(customer.Profile, "$.name").As("Name"),
        JsonValue(customer.Profile, "$.active", SqlType.Boolean).As("Active"),
        JsonValue(customer.Profile, "$.age", SqlType.Int32).As("Age"),
        JsonValue(customer.Profile, "$.balance", SqlType.Decimal()).As("Balance"),
        JsonQuery(customer.Profile, "$.orders").As("Orders"))
    .From(customer)
    .Done();
```

Untyped `JsonValue` returns a string. Typed extraction is kind-strict: strings become strings, numbers become numeric values, and booleans become Boolean values. Missing paths, JSON null, kind mismatch, nonintegral integers, overflow, and conversion failure return SQL `NULL`.

`JsonQuery` returns only objects and arrays. Its root overload marks a string expression as embedded JSON instead of a JSON string scalar:

```csharp
var result = JsonObject(
    JsonProperty("metadata", JsonQuery(customer.Metadata)),
    JsonProperty("caption", customer.Caption));
```

`JsonQuery(SQL NULL)` and operations on a SQL-null document return SQL `NULL`. Malformed input documents may raise a database error.

For a complete executable example, see [ScJsonRead.cs](../Test/SqExpress.IntTest/Scenarios/ScJsonRead.cs).

## Mutation and construction

```csharp
var payload = JsonObject(
    JsonProperty("id", order.Id),
    JsonProperty("tags", JsonArray("new", "priority")),
    JsonProperty("metadata", JsonQuery(order.Metadata)),
    JsonProperty("optional", Null),
    JsonProperty("explicitJsonNull", JsonNull()));

var updated = JsonSet(order.Payload, "$.status", "active");
var cleaned = JsonRemove(updated, "$.legacy");
```

Ordinary `ExprValue` arguments are JSON scalars. `ExprJson` values such as `JsonQuery`, `JsonObject`, and `JsonArray` are embedded fragments. Objects reject duplicate static keys and include SQL null as JSON null. Arrays preserve item order and include SQL null.

`JsonSet` creates or replaces a final object member when its parent exists and replaces only an existing array index. A missing or wrong-kind parent and an out-of-range index leave the document unchanged. `JsonRemove` is idempotent; removing `$` is rejected.

For a complete executable example, see [ScJsonMutationAndConstruction.cs](../Test/SqExpress.IntTest/Scenarios/ScJsonMutationAndConstruction.cs).

## Expanding a simple array

`JsonTable` can turn a JSON array of scalar values into relational rows:

```csharp
var numbers = JsonTable("[10,20,30]", "$")
    .Value("Number", "$", SqlType.Int32)
    .Ordinal("Index")
    .As("numbers");

var query = Select(
        numbers.Column("Index"),
        numbers.Column("Number"))
    .From(numbers)
    .Done();
```

The result contains `(0, 10)`, `(1, 20)`, and `(2, 30)`. The value path is `$` because each array element is itself the scalar being extracted.

For a complete executable example of typed columns, JSON fragments, and ordinality, see [ScJsonTable.cs](../Test/SqExpress.IntTest/Scenarios/ScJsonTable.cs).

## Expanding a complex array

```csharp
var people = JsonTable(
        """
        [
          {
            "id": 1,
            "name": "Alice",
            "active": true,
            "address": { "city": "Toronto", "postalCode": "M5V" },
            "roles": ["admin", "editor"]
          },
          {
            "id": 2,
            "name": "Bob",
            "active": false,
            "address": { "city": "Montreal", "postalCode": "H2X" },
            "roles": ["viewer"]
          }
        ]
        """,
        "$")
    .Value("Id", "$.id", SqlType.Int32)
    .Value("Name", "$.name", SqlType.String())
    .Value("Active", "$.active", SqlType.Boolean)
    .Value("City", "$.address.city", SqlType.String())
    .Value("PostalCode", "$.address.postalCode", SqlType.String())
    .Query("Roles", "$.roles")
    .Ordinal("Index")
    .As("people");

var query = Select(
        people.Column("Index"),
        people.Column("Id"),
        people.Column("Name"),
        people.Column("City"),
        people.Column("Roles"))
    .From(people)
    .Done();
```

Value columns use strict scalar semantics, query columns retain nested JSON, and ordinality is zero-based. Empty, null, or non-array input produces no rows.

### Flattening a nested array

```csharp
var roles = JsonTable(people.Column("Roles"), "$")
    .Value("Role", "$", SqlType.String())
    .As("roles");

var flattened = Select(
        people.Column("Id"),
        people.Column("Name"),
        roles.Column("Role"))
    .From(people)
    .OuterApply(roles)
    .Done();
```

`CrossApply` omits an outer row whose nested array is empty; `OuterApply` preserves it with SQL-null nested columns. SQL Server uses `CROSS/OUTER APPLY`. PostgreSQL's outer form is `LEFT JOIN LATERAL ... ON TRUE`. Oracle MySQL uses a lateral join, while MariaDB uses its implicitly correlated `JSON_TABLE` form.

SQLite can flatten nested JSON with correlated `json_each`, but the current exporter cannot map a correlated `JsonTable` derived projection back to its logical named columns. Correlated `CrossApply`/`OuterApply` over `JsonTable` is therefore a known SQLite limitation. Top-level `JsonTable` is supported.

For the complete nested-role example, see [ScGetTablesComplex.cs](../Test/SqExpress.IntTest/Scenarios/ScGetTablesComplex.cs).

## Passing a large row set

`JsonTable` can carry a structured batch as one JSON value and join it to database tables:

```csharp
string batchJson = SerializeBatch(items);

var input = JsonTable(batchJson, "$")
    .Value("Id", "$.id", SqlType.Int32)
    .Value("Quantity", "$.quantity", SqlType.Int32)
    .As("input");

var query = Select(input.Column("Id"), product.Price, input.Column("Quantity"))
    .From(input)
    .InnerJoin(product, on: input.Column("Id") == product.Id)
    .Done();
```

Use a parametrizing database/export mode for large documents so `batchJson` is sent as a parameter instead of embedded in SQL. JSON batching is portable and convenient for inserts, updates, deletes, and joins. Provider-native bulk copy, PostgreSQL `COPY`, or SQL Server table-valued parameters may be faster for very large ingestion workloads.

## Relational output with `ForJson`

`ForJson()` produces one row with one column named `Json`. By default it contains an array, and zero source rows produce `[]`.

```csharp
var result = Select(
        customer.Id.As("id"),
        customer.Name.As("name"),
        customer.City.AsJson("$.address.city"))
    .From(customer)
    .ForJson();
```

Ordinary named selections become root properties. `AsJson` supplies a nested, property-only output path; any intermediate SQL alias is generated internally. `ExprJsonOutputColumn` is terminal and cannot be exported outside `ForJson()`.

Unnamed, duplicate, indexed, and structurally conflicting paths are rejected. CTEs and compatible set operations are supported, using the left projection as the set shape. Version 1 does not guarantee array order.

```csharp
query.ForJson(
    withoutArrayWrapper: true,
    includeNullValues: false);
```

`includeNullValues` defaults to `true`. `withoutArrayWrapper` defaults to `false`; when enabled, zero rows return SQL `NULL`, one row returns an object, and multiple rows raise a database error.

For a complete executable example of output paths, embedded fragments, null options, empty results, and set operations, see [ScForJson.cs](../Test/SqExpress.IntTest/Scenarios/ScForJson.cs).

### Nested arrays from related tables

```csharp
var booksJson = Select(
        book.BookId.AsJson("$.id"),
        book.Title.AsJson("$.bookdata.title"),
        book.Author.AsJson("$.bookdata.author"))
    .From(book)
    .Where(book.ShelveId == shelve.ShelveId)
    .ForJson();

var shelvesJson = Select(
        shelve.ShelveId,
        shelve.Position,
        ValueQuery(booksJson).AsJson("$.book"))
    .From(shelve)
    .ForJson();
```

This produces a shelf array containing a `book` array, whose elements contain nested `bookdata` objects. `ValueQuery(booksJson)` puts the JSON-producing query in scalar-expression position; `AsJson("$.book")` embeds its result rather than quoting it.

For the complete temporary-table example and result validation, see [ScForJsonNestedBooks.cs](../Test/SqExpress.IntTest/Scenarios/ScForJsonNestedBooks.cs).

## Dialect matrix

| Capability | SqExpress helper | SQL Server | PostgreSQL | MySQL / MariaDB | SQLite JSON1 |
|---|---|---|---|---|---|
| Scalar extraction | `JsonValue` | `JSON_VALUE` plus guards | `jsonb_path_query_first` plus guards | `JSON_EXTRACT`, `JSON_TYPE` | `json_extract`, `json_type` |
| Fragment extraction | `JsonQuery` | `JSON_QUERY` | `jsonb_path_query_first` | `JSON_EXTRACT` | `json_extract` with `json(...)` provenance |
| Mutation | `JsonSet`, `JsonRemove` | `JSON_MODIFY` | `jsonb_set`, `#-` | `JSON_SET`, `JSON_REMOVE` | `json_set`, `json_remove` |
| Construction | `JsonObject`, `JsonArray` | `JSON_OBJECT`, `JSON_ARRAY` | `jsonb_build_object`, `jsonb_build_array` | `JSON_OBJECT`, `JSON_ARRAY` | `json_object`, `json_array` |
| Array expansion | `JsonTable` | `OPENJSON` | `jsonb_array_elements` | `JSON_TABLE` | `json_each` |
| Relational output | `ForJson` | `FOR JSON PATH` | `jsonb_agg` | `JSON_ARRAYAGG` | `json_group_array` |

The portable `JsonTable` schema is intentionally narrower than the full SQL Server `OPENJSON` surface. Schema-less key/value/type enumeration, vendor type codes, strict/lax modes, and advanced JSONPath remain vendor-specific. Exporters parse paths and quote components as data; the original path is never inserted with `UnsafeValue`.

## T-SQL parser and transpiler

The fail-closed portable subset covers `JSON_VALUE`, `JSON_QUERY`, supported `JSON_MODIFY`, `JSON_OBJECT`, `JSON_ARRAY`, typed `OPENJSON ... WITH`, and `FOR JSON PATH, INCLUDE_NULL_VALUES`. Literal paths and static object keys are required. Dotted output aliases map to `AsJson`.

Bare `OPENJSON`, `AUTO`, `ROOT`, append/strict paths, unsupported types, dynamic paths, `ABSENT ON NULL`, and `RETURNING` are rejected. Parser support is limited to forms that retain portable semantics.

For complete parser cases, see [TSqlParserJsonTest.cs](../Test/SqExpress.Test/SqlParser/TSqlParserJsonTest.cs). Compile-time path diagnostics are covered by [SqJsonPathAnalyzerTest.cs](../Test/SqExpress.Analyzers.Test/SqJsonPathAnalyzerTest.cs).

## Limitations and migration

Version 1 omits portable native JSON storage metadata, guaranteed aggregate ordering, root wrappers, advanced JSONPath, and bare object enumeration. SQLite correlated nested `JsonTable` expansion is a known limitation. Vendor APIs remain appropriate for intentionally nonportable behavior.

Replace vendor scalar extraction with `JsonValue`, fragment extraction or validation with `JsonQuery`, mutation with `JsonSet`/`JsonRemove`, row expansion with `JsonTable`, and relational aggregation with `ForJson`. Implementers of public generic or no-argument visitor contracts must add the JSON handlers; this visitor expansion is a breaking change.
