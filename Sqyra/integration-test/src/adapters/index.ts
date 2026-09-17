import type { DatabaseAdapter, IntegrationDialect } from "../types.js";
import { resolve } from "node:path";
import { MysqlAdapter } from "./mysql.js";
import { PgsqlAdapter } from "./pgsql.js";
import { SqliteAdapter } from "./sqlite.js";
import { TsqlAdapter } from "./tsql.js";

export function createAdapter(dialect: IntegrationDialect): DatabaseAdapter {
  switch (dialect) {
    case "tsql":
      return new TsqlAdapter(
        Object.assign(
          {
            server: "(local)",
            database: "TestDatabase",
            options: { trustedConnection: true },
            pool: { max: 1, min: 1 },
          },
          {
            connectionString:
              process.env.SQYRA_TSQL_CONNECTION_STRING ??
              "Driver={ODBC Driver 17 for SQL Server};Server=(local);Database=TestDatabase;Trusted_Connection=Yes;Encrypt=No;",
          },
        ),
      );
    case "pgsql":
      return new PgsqlAdapter(
        process.env.SQYRA_PG_CONNECTION_STRING ?? "postgresql://postgres:test@localhost:5432/test",
      );
    case "mysql-oracle":
      return new MysqlAdapter(
        dialect,
        "oracle",
        process.env.SQYRA_MYSQL_CONNECTION_STRING ?? "mysql://test:test@127.0.0.1:3306/test",
      );
    case "mariadb":
      return new MysqlAdapter(
        dialect,
        "mariadb",
        process.env.SQYRA_MARIADB_CONNECTION_STRING ?? "mysql://test:test@127.0.0.1:3307/test",
      );
    case "sqlite":
      return new SqliteAdapter(
        process.env.SQYRA_SQLITE_FILE ?? resolve(process.cwd(), ".sqyra-int.sqlite"),
      );
  }
}
