# Sqyra integration tests

This private Node project ports the executable scenarios from `Test/SqExpress.IntTest`. It owns database drivers, credentials, transactions, result normalization, and execution. Scenarios import only Sqyra's public packaged API. The project is excluded from Sqyra's npm package and from the library's ordinary `npm run verify`.

It runs on Node.js 20.19.5 or newer. The project installs `mssql` with `msnodesqlv8`, `pg`, `mysql2`, and `better-sqlite3` locally. No driver becomes a Sqyra runtime dependency.

Build Sqyra and install this project before running tests:

```sh
npm --prefix .. run build
npm ci
npm run refresh:sqyra
npm run verify
```

`verify` builds the parent package, refreshes the local `file:..` dependency, type-checks, runs test-local unit tests, audits source mappings and package isolation, and executes the live scenario chain. Run a selected matrix with:

The SQL Server native driver is pinned to `msnodesqlv8` 5.4.0, which supports the required exact numeric conversion switches and passes the Node 20 Windows smoke tests. Its newer 5.5.0 prebuild failed those smoke tests after a clean installation.

```sh
npm run test:int -- --dialects sqlite --parametrization none,literal-fallback
npm run test:int -- --dialects tsql,pgsql --parametrization throw-on-limit
npm run test:int -- --scenarios ScCreateTables,ScInsertUserData
```

Dialect selectors are `tsql`, `pgsql`, `mysql-oracle`, `mariadb`, and `sqlite`; parameterization selectors are `none`, `literal-fallback`, and `throw-on-limit`. Omitted selectors run every target and mode. A selected database is health-checked before its scenarios and an unavailable target fails clearly. Tests drop and recreate the physical table chain in foreign-key-safe order, so a complete run can be repeated without manual cleanup.

Defaults match the C# runner: Windows-integrated `(local)` SQL Server `TestDatabase`, PostgreSQL `postgres:test@localhost:5432/test`, Oracle MySQL `test:test@127.0.0.1:3306/test`, and MariaDB `test:test@127.0.0.1:3307/test`. Override connections with `SQYRA_TSQL_CONNECTION_STRING`, `SQYRA_PG_CONNECTION_STRING`, `SQYRA_MYSQL_CONNECTION_STRING`, or `SQYRA_MARIADB_CONNECTION_STRING`. `SQYRA_SQLITE_FILE` chooses a SQLite file. The SQL Server default uses the installed ODBC Driver 17 for SQL Server; Windows authentication may require running the Node process with the same OS permissions as the C# runner. PostgreSQL maps `dbo` to `public`.

Adapters bind Sqyra's compiled `{ sql, parameters }`: SQL Server uses named variables, PostgreSQL positional `$n`, and both MySQL flavors and SQLite ordered `?` values. SQL Server's ODBC adapter assigns fixed-width names at the driver boundary because the installed driver collides short numeric prefixes in large batches; Sqyra's compiled parameter names remain intact. Results preserve null, exact decimals as strings, Int64 as bigint, binary as copied `Uint8Array`, Booleans, temporal strings, column names, and row order. Transactions, sibling connections, deadlocks, and cancellation remain test-local.

Scenarios use fluent query and expression methods, such as `select(1).forJson()` and `lit(1).add(2)`. The scenario context accepts executable DML builders directly, without `.ast` or optional `.done()`, and invokes their `.toSql(options)` method with the same dialect and parameterization settings. Raw generated AST nodes still use the standalone exporter.

`scenario-inventory.json` records all 47 C# scenario files, hashes, default chain order, applicable dialect/mode information, and the TypeScript mapping. There are 43 executed ports and four fully excluded scenarios: `ScGetTables`, `ScGetTablesComplex`, `ScModelSelector`, and `ScUpdateUserData`. `ScGetTablesComplex` needs a public correlated `OUTER APPLY` JSON-table builder, while `ScGetTables` needs database metadata discovery. `ScCreateDynamicTable` executes its dynamic descriptor create/drop script; its live `DatabaseMetadata` discovery and `CompareWith` assertions are an assertion-level exclusion. `ScAllColumnTypes` ports the data round-trip with exact values, while its model-generated reread and mapped bulk reinsert are explicitly excluded. The exact reasons and line locations are recorded in `scenario-status.json`; none is represented as a skipped Vitest test. The runner labels source-restricted dialect/mode combinations `NOT APPLICABLE` with C# source locations.

Run `npm run inventory` when the C# sources change. `npm run audit -- --require-complete` detects source/hash drift, missing modules or test data, unfinished mappings, forbidden skipped/focused tests, and npm package leakage. `npm run typecheck`, `npm test`, and `npm run build` can be run separately. The parent library's `npm run verify` is independent of live database services.

Current source-review findings are in [SOURCE_REVIEW.md](SOURCE_REVIEW.md). Recorded local command results, live matrix coverage, exclusions, and remaining acceptance issues are in [VERIFICATION.md](VERIFICATION.md).
