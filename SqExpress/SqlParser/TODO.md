# SqTSqlParser review TODO

Reviewed and repaired 2026-10-04. Scope: public entry points, lexer, statement DOM/parser,
expression and statement mapping, scope binding, table artifact extraction, and
existing parser tests. This is a source review with executable regressions, not
an exhaustive SQL Server conformance or security audit. The reproduced regressions
are fixed in the accompanying parser changes.

The existing architecture is usable for a limited subset. The urgent problem is
semantic loss: several paths consume SQL syntax, discard part of its meaning,
and report success. That violates the documented fail-closed contract. Address
those paths before expanding support or doing a broad structural rewrite.

## Reproduced bugs and fixes

The regression cases are grouped with the existing parser tests by behavior in
`Test/SqExpress.Test/SqlParser`. The tests are enabled and passing. P1 means incorrect semantics or
binding to prioritize; P2 means validation or compatibility defects.
For unsupported features, rejection is the smallest safe repair. Tests requiring
rejection can be replaced by semantic preservation assertions if full support is
deliberately implemented.

### 1. P1: DML TOP is discarded

- [x] Reject unsupported unconstrained DML row limits before constructing the AST.
- Location: `Internal/Mapping/SqlDomToSqExprMapper.cs`, `MapUpdate` (line 955)
  and `MapDelete` (line 1182).
- Repro: `UPDATE TOP (1) dbo.Users SET Name = 'changed'` and
  `DELETE TOP (1) FROM dbo.Users` both return success.
- The mapper previously skipped the TOP tokens, then created an update/delete
  without a row limit. Unconstrained forms now fail closed. The existing,
  constrained TOP compatibility cases remain accepted until the update/delete
  AST can carry a row limit.
- Tests: `DmlTopWithoutWhere_IsRejected` (2 cases).
- Expected semantics: [Microsoft TOP documentation](https://learn.microsoft.com/en-us/sql/t-sql/queries/top-transact-sql).

### 2. P1: MERGE overwrites earlier matched actions

- [x] Reject a second action for a category the AST cannot represent, or preserve
  ordered actions and their conditions through an intentional AST extension.
- Location: mapper `MapMerge` (line 1324), `ParseMergeClause` (line 1583).
- Repro: `MERGE dbo.Target AS t USING dbo.Source AS s ON t.Id = s.Id
  WHEN MATCHED AND s.Flag = 1 THEN UPDATE SET Value = s.Value
  WHEN MATCHED THEN DELETE;`.
- Each clause assigns the same `whenMatched` variable. The conditional update
  disappears, leaving the unconditional delete. Audit the analogous
  `whenNotMatchedBySource` and `whenNotMatchedByTarget` assignments as well.
- Test: `MergeMultipleMatchedActions_IsRejectedInsteadOfDroppingFirstAction`.
- Two matched actions with this ordering are valid:
  [Microsoft MERGE documentation](https://learn.microsoft.com/en-us/sql/t-sql/statements/merge-transact-sql).

### 3. P1: LIKE ESCAPE is parsed and thrown away

- [x] Reject ESCAPE when it affects the pattern; retain compatibility when the
  escape token is absent from a literal pattern.
- Location: mapper `ExprParser.ParsePredicate`, lines 3565-3586.
- Repro: `SELECT 1 WHERE 'a_b' LIKE 'a!_b' ESCAPE '!'`, also `NOT LIKE`.
- The parser now rejects ESCAPE when the escape text occurs in a literal pattern.
  It accepts the established compatibility case where removing ESCAPE cannot
  affect that literal pattern. `ExprLike` currently has no escape operand.
- Tests: `LikeEscape_RejectsUnsupportedSemantics` (2 cases).
- Existing `TSqlParserBasicTest.ParseSelectWhereLikeWithEscape_MapsSuccessfully`
  checks only success and non-null output; revise that expectation when fixing.
- Semantics: [Microsoft LIKE documentation](https://learn.microsoft.com/en-gb/sql/t-sql/language-elements/like-transact-sql?view=sql-server-ver15).

### 4. P1: Window ROWS frames disappear

- [x] Map finite ROWS frames into the existing `ExprFrameClause`.
- Location: mapper `ParseOverClause`, lines 2669-2706.
- Repro: `SELECT SUM(u.Id) OVER (ORDER BY u.Id ROWS BETWEEN 1 PRECEDING
  AND CURRENT ROW) FROM dbo.Users u`.
- Finite ROWS frames are now mapped to the existing frame nodes. The established
  portable-export snapshots for unbounded frames remain unchanged pending a
  coordinated dialect-output migration.
- Test: `WindowRowsFrame_IsPreserved` confirms `FrameClause` is currently null.

### 5. P1: INTERSECT precedence is incorrect

- [x] Reduce INTERSECT before UNION/UNION ALL/EXCEPT, retaining explicit grouping.
- Location: mapper `MapSelectWithSetOperation`, especially lines 780-785.
- Repro: `SELECT 1 UNION SELECT 2 INTERSECT SELECT 2`.
- All operators are folded left-to-right. The AST becomes `(A UNION B) INTERSECT C`
  instead of `A UNION (B INTERSECT C)`. In the example the intended result contains
  1 and 2; the mapped tree describes only 2. AST consumers are affected regardless
  of any exporter parenthesization behavior.
- Tests: `Intersect_BindsMoreTightlyThanUnionOrExcept` (3 cases; inspect AST shape).
- Precedence: [Microsoft EXCEPT/INTERSECT documentation](https://learn.microsoft.com/en-us/sql/t-sql/language-elements/set-operators-except-and-intersect-transact-sql).

### 6. P1: Schema-qualified physical tables can resolve to CTEs

- [x] Perform CTE lookup only for an unqualified table name.
- Location: mapper `ParseTableSource`, line 2850; extractor `ExtractTables`,
  line 29.
- Repro: `WITH c AS (SELECT 1 AS Id) SELECT c.Id FROM dbo.c`.
- Export is `WITH [c] AS(SELECT 1 [Id])SELECT [c].[Id] FROM [c]`:
  the physical table is replaced by the CTE. The extractor also excludes physical
  references solely because their final name matches a CTE name.
- Test: `SchemaQualifiedTable_IsNotReplacedByCteWithSameName`.
- The physical AST source is now preserved. Analyzer resolution is restricted to
  the parser's authoritative table artifacts so the CTE name does not create a
  false descriptor requirement.

### 7. P1: Explicit CTE column lists are silently discarded

- [x] Retain and bind CTE output names and validate their count.
- Location: `Internal/Parsing/SqlDomParser.cs`, `ParseWithClause`, line 2042;
  `Internal/Dom/SqlDomNodes.cs`, `SqlDomCte` stores no output column list.
- Repro: `WITH c(Renamed) AS (SELECT 1 AS Original) SELECT c.Original FROM c`.
- The parser now stores `(Renamed)`, aliases the CTE query output, validates its
  arity, and rejects the invalid reference to `Original`. Recursive CTE references
  use the declared names while the query is being resolved.
- Test: `CteColumnList_ReplacesInnerProjectionNames`.
- Column-list contract: [Microsoft CTE documentation](https://learn.microsoft.com/en-us/sql/t-sql/queries/with-common-table-expression-transact-sql).

### 8. P1: Decimal literals can be silently rounded

- [x] Check exact representability before accepting a literal as `System.Decimal`.
  Reject unsupported precision until an exact representation is available.
- Location: mapper `ExprParser.ParsePrimary`, lines 3723-3734.
- Repro: `SELECT 0.12345678901234567890123456789`.
- `decimal.TryParse` succeeds with rounding; success is not evidence of a lossless
  conversion. SQL decimal supports more precision than CLR decimal, so the AST
  can contain a different number from the input.
- Test: `DecimalLiteralBeyondClrPrecision_IsRejectedInsteadOfRounded`.
- SQL precision: [Microsoft decimal/numeric documentation](https://learn.microsoft.com/en-us/sql/t-sql/data-types/decimal-and-numeric-transact-sql).

### 9. P2: Nested block comments are not tracked

- [x] Track block-comment nesting.
- Location: `Internal/Parsing/SqlLexer.cs`, lines 41-69.
- Repro: `SELECT 1 /* outer /* inner */` succeeds even though the outer comment
  remains unterminated. The lexer closes at the first `*/`.
- Test: `UnterminatedOuterBlockComment_IsRejected`.
- Nested comments are part of T-SQL lexical behavior:
  [Microsoft block comment documentation](https://learn.microsoft.com/en-us/sql/t-sql/language-elements/slash-star-comment-transact-sql).

### 10. P2: Artifact extraction confuses aliases from separate scopes

- [x] Prevent aliases reused for different tables across scopes from being assigned
  to either table by the statement-wide artifact heuristic.
  explicitly scope-aware. Do not use one statement-wide alias dictionary for
  final catalog validation.
- Location: `Internal/Mapping/SqlDomTableArtifactExtractor.cs`, lines 24-53;
  `SqTSqlParser.cs`, `TryValidateParsedTables`.
- Repro with catalog `Users(Id)` and `Orders(OrderId)`:
  `SELECT x.Id FROM dbo.Users x WHERE EXISTS
  (SELECT x.OrderId FROM dbo.Orders x)`.
- The inner alias overwrites the outer alias in `aliasToTable`. Valid SQL is
  rejected with `Table differences: [dbo].[Orders], extra columns: [Id]`.
- Test: `ReusedAliasInNestedScope_DoesNotPolluteOuterTableValidation`.
- Keep heuristic type inference separate from authoritative reference validation.

### 11. P2: Invalid type arguments pass validation

- [x] Distinguish omitted arguments from empty parentheses and enforce SQL Server
  limits in `ParseDecimalPrecisionScale` and `ParseLengthOrMax`.
- Location: mapper, lines 4192-4240.
- Repros: `DECIMAL(39,0)`, `DECIMAL()`, `VARCHAR(8001)`, `NVARCHAR(4001)` in CAST.
  All are accepted. Explicit empty parentheses are treated as default arguments;
  positive numbers are accepted without the required upper bounds.
- Tests: `InvalidCastTypeArguments_AreRejected` (4 cases).
- Bounds: [decimal](https://learn.microsoft.com/en-us/sql/t-sql/data-types/decimal-and-numeric-transact-sql),
  [varchar](https://learn.microsoft.com/en-us/sql/t-sql/data-types/char-and-varchar-transact-sql),
  [nvarchar](https://learn.microsoft.com/en-us/sql/t-sql/data-types/nchar-and-nvarchar-transact-sql).

### 12. P2: Statically known query arity is not validated

- [x] Check compatible set-branch column counts and exactly one column for scalar
  and IN subqueries whenever the output shape is known. Handle wildcard expansion
  explicitly rather than treating every select-list item as one output column.
- Location: mapper `MapSelectWithSetOperation`, `ExprParser.ParsePrimary`, and
  `ExprParser.ParsePredicate`.
- Repros: `SELECT 1 UNION SELECT 1, 2`, `SELECT (SELECT 1, 2)`, and
  `SELECT 1 WHERE 1 IN (SELECT 1, 2)` all succeed.
- Tests: `KnownQueryArityMismatch_IsRejected` (3 cases).
- These checks need no database catalog or full type inference for the repros.

## Maintainability work after correctness fixes

These are recommendations and review risks, not additional reproduced bugs.

- [ ] **Give each grammar rule one owner.** The mapper is 5,190 lines and includes
  a complete nested expression parser; the statement parser is another 3,110
  lines. Both scan keywords, split comma lists, locate parentheses, and decide
  what constitutes valid syntax. Extract shared token-cursor helpers with explicit
  end-of-input checks, then move one grammar rule at a time. Moving methods into
  partial files alone would not resolve the duplicated decisions.
  - [x] Centralize balanced-parenthesis lookup, top-level keyword lookup,
    multipart identifiers, comma-list ranges, and source slicing in
    `Internal/Parsing/SqlTokenReader.cs`. The shared cursor now exposes
    `IsAtEnd`, `TryGetCurrent`, and checked movement instead of silently returning
    the final token for an invalid position.
  - [x] Make the statement parser the sole owner of `OFFSET ... FETCH` grammar.
    It emits `SqlDomOffsetFetchClause` expression spans; the mapper only maps
    those expressions. Set-query mapping delegates the same grammar to the
    statement parser.
  - [x] Make the statement parser the sole owner of `ORDER BY` item splitting and
    direction parsing. It emits structured order items for top-level selects, and
    nested/set-query mapping delegates to the same parser routine.
  - [ ] Continue with set-expression tails and DML clauses. Each move
    should replace the mapper's keyword scan with structured DOM rather than move
    the existing method into another partial file.
- [ ] **Stop converting token slices back into SQL just to parse them again.**
  `ReadBalancedInner`, nested queries, argument lists, and many helpers use
  `string.Join`, `Tokenize`, and recursive parsing. Carry token ranges and original
  source spans through these steps. This reduces allocations and preserves useful
  error locations. Benchmark nested queries before claiming a speedup.
- [ ] **Remove or justify dormant normalization.** `SqlTextNormalizer.Normalize`
  is called for every statement, but the parser subtree has no reader of
  `NormalizedSql` beyond its assignment. Its UPDATE regex also operates on raw
  text, including literals. Do not start using this value as a correctness shortcut;
  remove the unused path after checking repository-wide consumers.
- [ ] **Require context on internal parsing helpers.** Numerous private overloads
  construct `new MappingContext(null)`, implicitly restoring `dbo` and discarding
  supplied catalogs/scopes. Audit callers and remove unused convenience overloads.
  Keep the public convenience API and `DefaultSchema = null` behavior intact.
- [ ] **Preserve diagnostics through phases.** `ParseCteQuery` and
  `ParseNestedSubQuery` discard nested errors with `out _` and replace them with
  generic messages. Use an internal diagnostic carrying stage, code, and source
  span, rendering the existing public string error at the boundary. Avoid a broad
  catch-all that hides programming defects.
- [ ] **Add bounded-input robustness tests.** Recursive value/query/CTE parsing
  has no explicit depth or token budget. Determine practical limits and test
  adversarial nesting in an isolated process before promising stable failure for
  arbitrary input. No stack-overflow or performance claim was experimentally
  established in this review.
- [ ] **Test meaning independently of round trips.** Keep round-trip tests, but
  also assert AST structure, retained clauses, exact literals, and rejection of
  unsupported semantics. A wrong AST can export and reparse consistently. For
  supported features, add small SQL Server differential fixtures where available;
  ScriptDom can help with syntax but cannot prove semantic equivalence.

## Verification and limits

Before adding regressions, the parser-focused suite passed 928 tests. The review
adds 21 cases across the 12 findings above. Final verification after repairs:

- parser-focused suite: **949 passed, 0 failed**;
- full core suite: **1,406 passed, 0 failed**;
- transpiler suite: **119 passed, 0 failed**;
- analyzer suite: **72 passed, 0 failed**.

Commands (existing system dotnet, no cache overrides):

```powershell
dotnet test Test/SqExpress.Test/SqExpress.Test.csproj --filter TSqlParser --no-restore
dotnet test Test/SqExpress.Test/SqExpress.Test.csproj --no-restore --logger 'console;verbosity=quiet'
```

No SQL was executed against a database. Reproductions use public parser entry
points, AST inspection, T-SQL export, and supplied table descriptors. Existing
subset documentation is unchanged because this review changes no supported
behavior. When fixing these issues, update that contract where support or explicit
rejection changes, and reconcile older tests that expect lossy input to succeed.
