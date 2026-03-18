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
- Current expression/function/window subset already covered by parser tests

## Explicitly Unsupported or Rejected

- `HAVING`
- `PIVOT`
- `UNPIVOT`
- `FOR JSON`
- `FOR XML`
- `OPTION(...)`
- `OUTPUT ... INTO`
- Malformed joins, malformed clause bodies, malformed delimited tokens, duplicate visible table aliases/names, and ambiguous unqualified columns in multi-table scope

## Notes

- The parser is intentionally not a full ScriptDom replacement.
- Backward-compatible public entry points should be preserved where possible.
- When new syntax is added, tests should be updated together with this subset contract.
