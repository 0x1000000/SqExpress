# SqTSqlParser Supported Subset

`SqTSqlParser` is intended to be a strict parser for a limited T-SQL subset.

## Guarantees

- Supported syntax must either parse deterministically into SqExpress AST or return a stable parser error.
- Unsupported syntax should be rejected explicitly instead of being partially parsed or silently ignored.
- Ambiguous or invalid table/column binding should fail instead of being guessed.

## Supported Statement Shapes

- `SELECT`
- `INSERT`
- `UPDATE`
- `DELETE`
- `MERGE`
- `WITH` CTEs for supported statement shapes

## Supported Query Features

- Table aliases and derived table aliases
- `INNER`, `LEFT`, `RIGHT`, `FULL`, `CROSS JOIN`
- `CROSS APPLY`, `OUTER APPLY`
- Subqueries and derived tables
- Set operations already covered by tests: `UNION`, `UNION ALL`, `INTERSECT`, `EXCEPT`
- `ORDER BY`
- `OFFSET ... FETCH`
- `GROUP BY`
- `STRING_AGG(value, separator)` with optional `WITHIN GROUP (ORDER BY ...)`
- Current expression/function/window subset already covered by parser tests
- Portable JSON scalar functions: `JSON_VALUE`, `JSON_QUERY`, and set/remove forms of `JSON_MODIFY`
- `JSON_OBJECT` and `JSON_ARRAY` with static object keys
- Typed `OPENJSON(...[, literal_path]) WITH (...)` table sources, including `AS JSON` fragment columns
- `FOR JSON PATH`, optionally with `INCLUDE_NULL_VALUES` and/or `WITHOUT_ARRAY_WRAPPER`; dotted projection aliases map to nested portable JSON output paths

## Portable JSON restrictions

- JSON paths must be string literals and must use the portable `SqJsonPath` grammar.
- `JSON_MODIFY(document, path, value)` maps to `JsonSet`; a literal SQL `NULL` value maps to `JsonRemove`.
- `OPENJSON` requires a `WITH` projection and an alias. Its portable column types are string, Boolean, Int32, Int64, decimal, and double.
- `FOR JSON` supports `PATH` with optional `INCLUDE_NULL_VALUES` and `WITHOUT_ARRAY_WRAPPER` options.
- Bare `OPENJSON`, `AUTO`, `ROOT`, append/strict paths, dynamic paths and object keys, unsupported types, `ABSENT ON NULL`, and `RETURNING` are rejected.

## Explicitly Unsupported or Rejected

- `HAVING`
- `PIVOT`
- `UNPIVOT`
- `FOR XML`
- `OPTION(...)`
- `OUTPUT ... INTO`
- `ANY` / `SOME` / `ALL` quantified predicates
- Malformed joins, malformed clause bodies, malformed delimited tokens, duplicate visible table aliases/names, and ambiguous unqualified columns in multi-table scope

## Notes

- The parser is intentionally not a full ScriptDom replacement.
- Backward-compatible public entry points should be preserved where possible.
- When new syntax is added, tests should be updated together with this subset contract.
