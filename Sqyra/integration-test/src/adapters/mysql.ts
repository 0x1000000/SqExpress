import mysql, {
  type FieldPacket,
  type Pool,
  type PoolConnection,
  type ResultSetHeader,
  type RowDataPacket,
} from "mysql2/promise";
import type { CompiledSql } from "sqyra";
import { normalizeRows, parameterValues } from "../normalize.js";
import type {
  DatabaseAdapter,
  DatabaseTransaction,
  ExecutionOptions,
  ExecutionResult,
  IntegrationDialect,
} from "../types.js";

export class MysqlAdapter implements DatabaseAdapter {
  readonly dialect = "mysql" as const;
  private pool: Pool | null = null;
  constructor(
    readonly name: Extract<IntegrationDialect, "mysql-oracle" | "mariadb">,
    readonly mysqlFlavor: "mariadb" | "oracle",
    private readonly connectionString: string,
    private readonly connection: PoolConnection | null = null,
  ) {}
  async open(): Promise<void> {
    if (this.connection === null)
      this.pool ??= mysql.createPool({
        uri: this.connectionString,
        supportBigNumbers: true,
        bigNumberStrings: true,
        dateStrings: true,
        jsonStrings: true,
        multipleStatements: true,
      });
  }
  async close(): Promise<void> {
    if (this.connection !== null) this.connection.release();
    else await this.pool?.end();
    this.pool = null;
  }
  async healthCheck(): Promise<void> {
    await this.execute({ sql: "SELECT 1 AS value", parameters: [] });
  }
  async execute(compiled: CompiledSql, options?: ExecutionOptions): Promise<ExecutionResult> {
    if (options?.signal?.aborted) throw options.signal.reason;
    const values = parameterValues(compiled.parameters).map((value, index) =>
      compiled.parameters[index]?.type === "ExprDateTimeLiteral" && typeof value === "string"
        ? value.replace("T", " ")
        : value,
    );
    let result: RowDataPacket[] | ResultSetHeader, fields: FieldPacket[];
    if (options?.signal !== undefined && this.connection === null) {
      // An in-flight mysql2 query has no AbortSignal API. A dedicated pooled
      // connection can be destroyed without cancelling another scenario's work.
      const connection = await this.required().getConnection();
      const signal = options.signal;
      let rejectAbort: (reason: unknown) => void = () => {};
      const aborted = new Promise<never>((_resolve, reject) => {
        rejectAbort = reject;
      });
      const abort = () => {
        connection.destroy();
        rejectAbort(signal.reason);
      };
      signal.addEventListener("abort", abort, { once: true });
      if (signal.aborted) abort();
      try {
        [result, fields] = (await Promise.race([
          connection.query(compiled.sql, [...values]),
          aborted,
        ])) as [RowDataPacket[] | ResultSetHeader, FieldPacket[]];
      } catch (error) {
        if (signal.aborted) throw signal.reason;
        throw error;
      } finally {
        signal.removeEventListener("abort", abort);
        if (!signal.aborted) connection.release();
      }
    } else
      [result, fields] = (await (this.connection ?? this.required()).query(compiled.sql, [
        ...values,
      ])) as [RowDataPacket[] | ResultSetHeader, FieldPacket[]];
    const multipleStatements =
      Array.isArray(result) &&
      result.length > 0 &&
      result.every(
        (item) =>
          Array.isArray(item) ||
          (typeof item === "object" && item !== null && "affectedRows" in item),
      );
    if (multipleStatements) {
      const statementResults = result as unknown[];
      const statementFields = fields as unknown[];
      let affectedRows = 0;
      let rows: ReturnType<typeof normalizeRows> = [];
      for (let index = 0; index < statementResults.length; index++) {
        const item = statementResults[index];
        const itemFields = statementFields[index];
        if (Array.isArray(item) && Array.isArray(itemFields))
          rows = normalizeRows(
            normalizeMysqlValues(item as RowDataPacket[], itemFields as FieldPacket[]),
          );
        else if (
          typeof item === "object" &&
          item !== null &&
          "affectedRows" in item &&
          typeof item.affectedRows === "number"
        )
          affectedRows += item.affectedRows;
      }
      return { rows, affectedRows };
    }
    if (Array.isArray(result))
      return { rows: normalizeRows(normalizeMysqlValues(result, fields)), affectedRows: 0 };
    return { rows: [], affectedRows: result.affectedRows };
  }
  async executeScript(sql: string): Promise<void> {
    await (this.connection ?? this.required()).query(sql);
  }
  async beginTransaction(isolation?: "serializable"): Promise<DatabaseTransaction> {
    if (this.connection !== null)
      throw new Error("There is an already running transaction associated with this connection");
    const connection = await this.required().getConnection();
    const nested = new MysqlAdapter(this.name, this.mysqlFlavor, this.connectionString, connection);
    if (isolation === "serializable")
      await connection.query("SET TRANSACTION ISOLATION LEVEL SERIALIZABLE");
    await connection.beginTransaction();
    let completed = false;
    const finish = async (commit: boolean): Promise<void> => {
      if (completed) throw new Error("The transaction has already completed");
      completed = true;
      try {
        if (commit) await connection.commit();
        else await connection.rollback();
      } finally {
        connection.release();
      }
    };
    return { database: nested, commit: () => finish(true), rollback: () => finish(false) };
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
    const result = new MysqlAdapter(this.name, this.mysqlFlavor, this.connectionString);
    await result.open();
    return result;
  }
  private required(): Pool {
    if (this.pool === null) throw new Error(`${this.name} adapter is not open.`);
    return this.pool;
  }
}

function normalizeMysqlValues(
  rows: RowDataPacket[],
  fields: FieldPacket[],
): ReadonlyArray<Readonly<Record<string, unknown>>> {
  return rows.map((row) =>
    Object.fromEntries(
      fields.map((field) => {
        const value: unknown = row[field.name];
        // mysql2 returns exact BIGINT values as strings when bigNumberStrings is enabled.
        if (field.columnType === 8 && typeof value === "string" && /^-?\d+$/.test(value))
          return [field.name, BigInt(value)];
        if (
          (field.columnType === 0 || field.columnType === 246) &&
          typeof value === "string" &&
          /^-?\d+\.\d+$/.test(value)
        )
          return [field.name, value.replace(/(\.\d*?[1-9])0+$/, "$1").replace(/\.0+$/, "")];
        if (field.columnType === 245 && typeof value === "object" && value !== null)
          return [field.name, JSON.stringify(value)];
        if (
          typeof value === "object" &&
          value !== null &&
          !(value instanceof Uint8Array) &&
          !(value instanceof Date)
        )
          return [field.name, JSON.stringify(value)];
        return [field.name, value];
      }),
    ),
  );
}
