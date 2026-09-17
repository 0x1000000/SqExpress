# Integration verification evidence

Verified locally on 2026-09-16 with Node.js 20.19.5. The integration project and parent library both pass their full verification. The C# CTE exporter was corrected with user authorization, its regression was demonstrated failing before the fix, and reference fixtures were regenerated from the actual corrected C# library.

## Source identity and mappings

- Source Git commit: `db4cbfad1c2bc0eec3db3f09bdfc5a9a02c67699`.
- Working tree: uncommitted Sqyra implementation and integration files, plus the authorized C# CTE exporter correction, NUnit regression, and 1.4 changelog entry.
- All 47 C# scenario files have source identities, SHA-256 hashes, source order, applicability, and mappings in `scenario-inventory.json`.
- Inventory SHA-256: `b931dfb037fa3f83097a2b1d7bbb9e58dc927828e323ecdc65cde6706ea10aa2`.
- The C# runner and both copied JSON data files are also hashed; copied data matches the originals byte-for-byte.
- 43 executed ports, four full exclusions, two assertion-level exclusions, zero partial or pending mappings.
- Direct comparison findings for all 47 source scenarios are recorded in `SOURCE_REVIEW.md`.

The full exclusions are `ScGetTables` (metadata discovery), `ScGetTablesComplex` (correlated OUTER APPLY builder), `ScModelSelector` (generated model-selection/page API), and `ScUpdateUserData` (mapped bulk UpdateData API). Exact capability reasons and assertion-level exclusions are in `scenario-status.json`.

## Live results

The final `npm run verify` exited 0 after a clean lock-file installation with the integration-only SQL Server driver pinned to `msnodesqlv8` 5.4.0. Every applicable scenario passed. The numbers below count source-mapped scenario invocations, not Vitest tests; applicability is derived from the inventory and enforced by the inspected runner.

| Target       | none     | literal-fallback | throw-on-limit |
| ------------ | -------- | ---------------- | -------------- |
| tsql         | PASS, 40 | PASS, 41         | PASS, 41       |
| pgsql        | PASS, 41 | PASS, 42         | PASS, 42       |
| mysql-oracle | PASS, 38 | PASS, 39         | PASS, 39       |
| mariadb      | PASS, 41 | PASS, 42         | PASS, 42       |
| sqlite       | PASS, 39 | PASS, 40         | PASS, 40       |

This accounts for 607 executed invocations. The 38 other combinations are explicit C# dialect/mode restrictions, reported with source locations as NOT APPLICABLE. Unsupported scenarios are inventory exclusions, not skipped or pending tests. Physical tables were recreated for each chain, in reverse order for dropping and dependency order for creation. Repeated complete and focused chains passed without manual cleanup.

## Commands and checks

| Command/check                                                                          | Result                                                                                                                                                                                             |
| -------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Clean `npm ci --offline --cache .npm-cache`                                            | Exit 0 on Node 20.19.5                                                                                                                                                                             |
| Integration `npm run build`                                                            | Exit 0                                                                                                                                                                                             |
| Integration `npm run typecheck`                                                        | Exit 0                                                                                                                                                                                             |
| Integration `npm test`                                                                 | Exit 0, 13 adapter/config/source-input unit tests                                                                                                                                                  |
| Integration `npm run audit -- --require-complete`                                      | Exit 0; hashes, mappings, source order, review coverage, test policy, and package isolation                                                                                                        |
| Integration `npm run verify`                                                           | Exit 0; all five targets and all three modes                                                                                                                                                       |
| Intentionally unavailable PostgreSQL target                                            | Expected exit 1 with explicit health-check failure; no automatic skip                                                                                                                              |
| C# `dotnet test Test/SqExpress.Test/SqExpress.Test.csproj --no-restore`                | Exit 0, 1,359 passed after the CTE correction                                                                                                                                                      |
| C# `dotnet run --project Test/SqExpress.IntTest/SqExpress.IntTest.csproj --no-restore` | Exit 0; full scenario chains passed on SQL Server, PostgreSQL, Oracle MySQL, MariaDB, and SQLite, followed by the parameter-limit/string-aggregation runs for literal fallback and throw-on-limit. |
| Parent library full verification before deep CTE correction                            | Exit 0; 1,175 runtime tests, strict TS 5.0 and newer checks, 11 executable packed README examples, ESM/CommonJS/browser package checks                                                             |
| Current parent `npm run verify` after C# and Sqyra CTE corrections                     | Exit 0; 1,177 runtime tests, 931 C# fixtures, all 1,111 source mappings, strict TS 5.0/newer checks, 11 executable README examples, and packed ESM/CommonJS/browser checks pass.                   |
| Deep CTE regression after correction                                                   | Both CTE regressions pass; references regenerated from corrected C#                                                                                                                                |

Package auditing confirms that integration files, drivers, credentials, and source data are absent from Sqyra's packed package. Parent runtime dependencies contain no database drivers. Parent tests explicitly exclude the integration directory and do not require live databases or integration dependencies.

## Sqyra regression coverage and driver adaptations

Focused library coverage includes fixed ANSI/Unicode DDL, cyclic and composite foreign-key ownership, analytic projections, INSERT ordering, SQLite joined UPDATE, SQLite DATEADD, nested JSON/correlation, JSON-null export, derived-source CTE dependency discovery, deep CTE ordering, and positional derived-column MERGE export. Successful C# scenario inputs and assertions were restored where earlier ports had simplified them.

Adapters alone handle SQL Server ODBC parameter-name collisions and XML serialization, PostgreSQL timestamp precision/offset formatting and lexical parameter type casts, SQLite numeric representation, and driver result conversion. Int64, exact decimal values, binary contents, Unicode, null, ordering, and SQL semantics remain assertions. Parameter-limit tests verify compiled names/order/types/exact values and every inserted bound/fallback row through the actual drivers. Deadlock assertions reject unrelated errors.

## CTE correction and remaining blockers

The C# exporter previously emitted `Middle` before its dependency `Base`; SQL Server rejected that ordering with `Invalid object name 'Base'`. The new NUnit regression `ExportCteChain_PlacesDependenciesBeforeConsumers` reproduced the defect before the fix. Dependency traversal now stops at each CTE reference and emits dependencies before consumers while preserving recursive descriptor proxy handling. All 1,359 C# tests pass.

The corrected C# reference runner generated 931 fixtures, including the new regression. Sqyra dependency discovery was also corrected to include nested subqueries such as EXISTS. Exact four-dialect fixture parity and both full verification commands now pass. No expectations were relaxed and no fixture was generated using Sqyra.

Remaining blockers: none for the integration project acceptance scope, including its explicit unsupported-feature exclusions.
