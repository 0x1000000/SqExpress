# Sqyra

Sqyra is a TypeScript port of SqExpress. It provides a generated immutable SQL AST, typed table descriptors, fluent query and DML builders, a fail-closed T-SQL parser, tree operations, and SQL exporters for T-SQL, PostgreSQL, MySQL, and SQLite.

Sqyra targets ES2020 and works in Node.js and browser bundles. Development and verification use Node.js 20.19.5. The public declarations are compatible with TypeScript 5.0 and newer compilers.

## Installation and imports

```sh
npm install sqyra
```

ES modules:

```ts
import { parseTSql, toSql } from "sqyra";

const parsed = parseTSql("SELECT 1 AS Value");
const sql = toSql(parsed.ast, { dialect: "postgresql" });
if (sql !== 'SELECT 1 "Value"') throw new Error(sql);
```

CommonJS consumers can load the same exports with `require("sqyra")`.

## Typed table descriptors

`defineTable` retains literal table, schema, and column names. Columns are available directly on the descriptor and also through `.columns` for enumeration. A column that collides with descriptor metadata receives a `$` prefix: a physical `columns` column is `table.$columns`, while `table.columns` remains the column map. The same rule applies to `name`, `schema`, `alias`, `definitions`, `dynamic`, and `derivedSource`. `aliasTable` creates new typed references and leaves the original descriptor unchanged.

```ts
import { aliasTable, column, defineTable, nullableColumn, sqlType } from "sqyra";

const users = defineTable({
  schema: "dbo",
  name: "Users",
  columns: {
    Id: column(sqlType.int32),
    Name: nullableColumn(sqlType.string(200)),
    Version: column(sqlType.int64),
  },
});
const u = aliasTable(users, "u");
const alias: "u" = u.Id.sourceAlias;
if (alias !== "u") throw new Error(alias);
```

Use `defineDynamicTable` for metadata known only at runtime. Its names and row types are intentionally wider, making the loss of static knowledge visible to TypeScript.

## Values and expressions

Builder functions accept AST values, descriptor columns, and ordinary JavaScript primitives. `toExpr` and `lit` expose the same conversion explicitly.

```ts
import { and, eq, gt, lit, neq, toExpr } from "sqyra";

const comparison = eq(1, 2);
const predicate = and(gt(5, 2), eq("a", "a"));
const fluent = eq(1, "1").and(neq(1, "1").or(gt(1, 2)));
if (comparison.kind !== "ExprBooleanEq" || predicate.kind !== "ExprBooleanAnd") throw new Error("unexpected AST");
if (fluent.kind !== "ExprBooleanAnd") throw new Error("unexpected fluent predicate");
if (lit(1n).kind !== "ExprInt64Literal" || toExpr(true).kind !== "ExprBoolLiteral") throw new Error("unexpected literal");
```

The conversion rules are `string` to string literal, `boolean` to Boolean literal, signed 32-bit integral `number` to Int32, other `number` values to Double, `bigint` to Int64, `Uint8Array` to a defensive binary copy, and `null` to SQL NULL. Use `decimalValue`, `guidValue`, `dateTimeValue`, and `dateTimeOffsetValue` with generated factories when exact SQL value representation is required.

Comparison overloads use descriptor value types. TypeScript rejects comparisons such as an Int32 column against a string.

## SELECT queries

A staged builder prevents clauses from being called in an invalid order. Projections retain readonly key and value types.

```ts
import { aliasTable, column, defineTable, desc, eq, select, sqlType, toSql } from "sqyra";

const users = aliasTable(defineTable({ schema: "dbo", name: "Users", columns: { Id: column(sqlType.int32), Name: column(sqlType.string(100)) } }), "u");
const query = select({ id: users.Id, name: users.Name })
  .from(users)
  .where(eq(users.Id, 42))
  .orderBy(desc(users.Name))
  .offsetFetch(0, 10)
  .done();
const sql = toSql(query.ast, { dialect: "tsql" });
if (!sql.includes("WHERE [u].[Id]=42")) throw new Error(sql);
```

The SELECT builder supports DISTINCT and TOP options, inner/left/right/full/cross joins, WHERE, GROUP BY, ORDER BY, and OFFSET/FETCH. `union`, `unionAll`, `intersect`, and `except` combine completed queries. `derivedTable` exposes a query as a typed source. `cte` accepts a callback and supports both ordinary and recursive CTEs.

## INSERT, UPDATE, and DELETE

DML object values are checked against descriptor column names and value types.

```ts
import { column, defineTable, deleteFrom, eq, insertInto, nullableColumn, sqlType, toSql, update } from "sqyra";

const users = defineTable({ schema: "dbo", name: "Users", columns: { Id: column(sqlType.int32), Name: nullableColumn(sqlType.string(100)) } });
const insert = insertInto(users).values({ Id: 1, Name: "Ada" }, { Id: 2, Name: null });
const rename = update(users).set({ Name: "Grace" }).where(eq(users.Id, 1));
const remove = deleteFrom(users).where(eq(users.Id, 2));
for (const statement of [insert, rename, remove]) {
  if (toSql(statement.ast, { dialect: "tsql" }).length === 0) throw new Error("empty SQL");
}
```

## Functions and JSON

`call`, `aggregate`, `caseWhen`, `cast`, and `stringAgg` construct generated expression nodes. Portable JSON helpers include `jsonValue`, `jsonQuery`, `jsonSet`, `jsonRemove`, `jsonArray`, and `jsonObject`. JSON paths use a validated portable grammar and invalid paths fail immediately.

```ts
import { jsonArray, jsonObject, jsonSet, jsonValue, select, toSql } from "sqyra";

const query = select({
  value: jsonValue('{"a":1}', "$.a"),
  changed: jsonSet("{}", "$.a", 1),
  array: jsonArray(1, null),
  object: jsonObject({ a: 1 }),
}).done();
for (const dialect of ["tsql", "postgresql", "mysql", "sqlite"] as const) {
  if (!toSql(query.ast, { dialect }).startsWith("SELECT")) throw new Error(dialect);
}
```

## Parsing T-SQL

`parseTSql` returns the AST and inferred physical-table artifacts. It throws `SqyraParserError` on failure. `tryParseTSql` returns a discriminated success/failure union instead.

```ts
import { parseTSql, tryParseTSql } from "sqyra";

const result = parseTSql("SELECT u.Id FROM dbo.Users u WHERE u.Id=@id");
if (result.tables[0]?.name !== "Users") throw new Error("table extraction failed");
const invalid = tryParseTSql("SELECT FROM Users");
if (invalid.success) throw new Error("malformed SQL was accepted");
```

The parser consumes one complete statement and rejects trailing statements, malformed clause bodies, unsupported syntax, unknown aliases, ambiguous columns, and invalid descriptor references. Its supported subset includes SELECT, INSERT, UPDATE, DELETE, MERGE, CTEs, joins and APPLY, derived tables, subqueries, set operations, grouping, ordering and pagination, supported casts/functions/aggregates/windows, parameters, Unicode literals, portable JSON functions, typed OPENJSON, and FOR JSON PATH.

`defaultSchema` defaults to `"dbo"`; explicitly setting it to `null` keeps unqualified tables schema-less. Pass `existingTables` to validate physical table and column names.

## AST traversal and immutable modification

Generated child metadata drives all traversal. No operation enumerates arbitrary object keys.

```ts
import { descendants, exprBooleanEq, exprInt32Literal, find, modify, walk } from "sqyra";

const one = exprInt32Literal({ value: 1 });
const root = exprBooleanEq({ left: one, right: exprInt32Literal({ value: 2 }) });
if ([...walk(root)].length !== 3 || [...descendants(root)].length !== 2) throw new Error("walk failed");
if (find(root, node => node === one) !== one) throw new Error("find failed");
const changed = modify(root, node => node === one ? exprInt32Literal({ value: 3 }) : node);
if (changed === root) throw new Error("path was not rebuilt");
```

`visit` dispatches an exhaustive generated visitor; `createVisitor` supplies a fallback with typed overrides. `walkWithParent` reports parent, depth, and root-to-node path. No-op modification preserves the original root reference, while a change rebuilds only its ancestor path. Required children cannot be removed and category-incompatible replacements throw. `serializeAst` and `deserializeAst` provide deterministic JSON round trips, including tagged bigint and binary values.

## SQL export and parameters

`toSql(ast, { dialect })` renders inline SQL. `compileSql` parameterizes `param(...)` expressions and returns ordered values with names and AST value types.

```ts
import { compileSql, param, select } from "sqyra";

const query = select({ id: param(9007199254740993n, "id") }).done();
const compiled = compileSql(query.ast, { dialect: "postgresql" });
if (compiled.sql !== 'SELECT $1 "id"' || compiled.parameters[0]?.value !== 9007199254740993n) throw new Error("parameter mismatch");
```

Identifiers, literals, pagination, functions, JSON operations, and supported DML are adapted per dialect. `schemaMap` remaps schemas and `avoidNameQuoting` disables identifier quoting when required by an integration. Unsupported dialect/operation combinations throw instead of emitting misleading SQL.

## Exact values

Int64 values use range-validated `bigint`. Decimal values use validated strings so fractional precision is retained. GUIDs are validated branded strings. Date/time values retain textual precision and the DateTime kind or offset. Binary values use defensively copied `Uint8Array` instances.

## Scope and differences from SqExpress

Sqyra is independent at runtime and does not call .NET. Roslyn and the C# library are development-only tools for AST generation and reference fixtures. Stage one excludes DDL and `StatementSyntax`, database connections and command execution, schema introspection, EF integration, DTO generation, and the transpiler UI.

JavaScript has no C# operator overloading, so Sqyra uses functions such as `eq(a, b)`, `add(a, b)`, and `and(a, b)`. Runtime-known descriptors and projections expose wider types rather than claiming exact inference.

## Development and regeneration

```sh
npm install
npm run baseline:check
npm run codegen
npm run codegen:check
npm run parity:audit
npm run typecheck
npm run test:types
npm run typecheck:new
npm test
npm run test:docs
npm run build
npm run test:package
npm run verify
```

`codegen` runs the Roslyn semantic generator and rewrites tracked AST output. `codegen:check` generates separately and compares byte-for-byte without modifying tracked files. C# fixture generation is separate from normal verification. The parity audit checks source hashes, fixture provenance, source-test mappings, generated drift, and forbidden skipped or focused tests.
