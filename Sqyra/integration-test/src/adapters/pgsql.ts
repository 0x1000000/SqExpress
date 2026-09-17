import { Pool, type PoolClient, types } from "pg";
import type { CompiledSql } from "sqyra";
import { normalizeRows, parameterValues } from "../normalize.js";
import type {
  DatabaseAdapter,
  DatabaseTransaction,
  ExecutionOptions,
  ExecutionResult,
} from "../types.js";

types.setTypeParser(20, (value) => BigInt(value));
types.setTypeParser(1700, (value) => value);
types.setTypeParser(1082, (value) => value);
types.setTypeParser(1114, (value) => value);
// Keep timestamptz microseconds: pg's default Date parser truncates to milliseconds.
export function normalizePgTimestampOffset(value: string): string {
  return value
    .replace(" ", "T")
    .replace(/([+-]\d{2})$/, "$1:00")
    .replace(/([+-]\d{2})(\d{2})$/, "$1:$2");
}
types.setTypeParser(1184, normalizePgTimestampOffset);
types.setTypeParser(114, (value) => value);
types.setTypeParser(3802, (value) => value);
export function castPgParameters(sql: string, parameters: CompiledSql["parameters"]): string {
  let result = "",
    index = 0;
  while (index < sql.length) {
    const start = index,
      char = sql[index];
    if (char === "'" || char === '"') {
      const escaped = char === "'" && /(?:^|[^\w])E$/i.test(sql.slice(0, index));
      index++;
      while (index < sql.length) {
        if (escaped && sql[index] === "\\") {
          index += 2;
          continue;
        }
        if (sql[index++] === char) {
          if (sql[index] === char) {
            index++;
            continue;
          }
          break;
        }
      }
    } else if (sql.startsWith("--", index)) {
      const end = sql.indexOf("\n", index);
      index = end < 0 ? sql.length : end;
    } else if (sql.startsWith("/*", index)) {
      let depth = 1;
      index += 2;
      while (index < sql.length && depth > 0) {
        if (sql.startsWith("/*", index)) {
          depth++;
          index += 2;
        } else if (sql.startsWith("*/", index)) {
          depth--;
          index += 2;
        } else index++;
      }
    } else if (char === "$") {
      const delimiter = sql.slice(index).match(/^\$(?:[A-Za-z_][A-Za-z0-9_]*)?\$/)?.[0];
      if (delimiter !== undefined) {
        const end = sql.indexOf(delimiter, index + delimiter.length);
        index = end < 0 ? sql.length : end + delimiter.length;
      } else {
        const token = sql.slice(index).match(/^\$(\d+)/);
        if (token !== null) {
          const parameter = parameters[Number(token[1]) - 1];
          if (parameter !== undefined) {
            result += `CAST(${token[0]} AS ${pgsqlParameterType(parameter.type)})`;
            index += token[0].length;
            continue;
          }
          index += token[0].length;
        } else index++;
      }
    } else index++;
    result += sql.slice(start, index);
  }
  return result;
}
export class PgsqlAdapter implements DatabaseAdapter {
  readonly name = "pgsql" as const;
  readonly dialect = "pgsql" as const;
  readonly schemaMap = [{ from: "dbo", to: "public" }] as const;
  private pool: Pool | null = null;
  constructor(
    private readonly connectionString: string,
    private readonly client: PoolClient | null = null,
  ) {}
  async open(): Promise<void> {
    if (this.client === null) this.pool ??= new Pool({ connectionString: this.connectionString });
  }
  async close(): Promise<void> {
    if (this.client !== null) this.client.release();
    else await this.pool?.end();
    this.pool = null;
  }
  async healthCheck(): Promise<void> {
    await this.execute({ sql: "SELECT 1 AS value", parameters: [] });
  }
  async execute(compiled: CompiledSql, options?: ExecutionOptions): Promise<ExecutionResult> {
    if (options?.signal?.aborted) throw options.signal.reason;
    const executor = this.client ?? this.required();
    const text = castPgParameters(compiled.sql, compiled.parameters);
    const result = await executor.query({
      text,
      values: [...parameterValues(compiled.parameters)],
    });
    return {
      rows: normalizeRows(result.rows as ReadonlyArray<Readonly<Record<string, unknown>>>),
      affectedRows: result.rowCount ?? 0,
    };
  }
  async executeScript(sql: string): Promise<void> {
    await (this.client ?? this.required()).query(sql);
  }
  async beginTransaction(isolation?: "serializable"): Promise<DatabaseTransaction> {
    if (this.client !== null)
      throw new Error("There is an already running transaction associated with this connection");
    const client = await this.required().connect();
    const nested = new PgsqlAdapter(this.connectionString, client);
    await client.query(
      isolation === "serializable" ? "BEGIN ISOLATION LEVEL SERIALIZABLE" : "BEGIN",
    );
    let completed = false;
    const finish = async (sql: "COMMIT" | "ROLLBACK"): Promise<void> => {
      if (completed) throw new Error("The transaction has already completed");
      completed = true;
      try {
        await client.query(sql);
      } finally {
        client.release();
      }
    };
    return { database: nested, commit: () => finish("COMMIT"), rollback: () => finish("ROLLBACK") };
  }
  async transaction<T>(action: (transaction: DatabaseAdapter) => Promise<T>): Promise<T> {
    const transaction = await this.beginTransaction();
    try {
      const result = await action(transaction.database);
      await transaction.commit();
      return result;
    } catch (error) {
      await transaction.rollback();
      throw error;
    }
  }
  async openSibling(): Promise<DatabaseAdapter> {
    const result = new PgsqlAdapter(this.connectionString);
    await result.open();
    return result;
  }
  private required(): Pool {
    if (this.pool === null) throw new Error("PostgreSQL adapter is not open.");
    return this.pool;
  }
}

function pgsqlParameterType(type: string | null): string {
  switch (type) {
    case "ExprByteLiteral":
    case "ExprInt16Literal":
      return "int2";
    case "ExprInt32Literal":
      return "int4";
    case "ExprInt64Literal":
      return "int8";
    case "ExprDecimalLiteral":
      return "numeric";
    case "ExprDoubleLiteral":
      return "float8";
    case "ExprBoolLiteral":
      return "boolean";
    case "ExprGuidLiteral":
      return "uuid";
    case "ExprByteArrayLiteral":
      return "bytea";
    case "ExprDateTimeLiteral":
      return "timestamp";
    case "ExprDateTimeOffsetLiteral":
      return "timestamptz";
    default:
      return "text";
  }
}
