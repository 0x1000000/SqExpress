import sql from "mssql/msnodesqlv8.js";
import type { config, ConnectionPool, ISqlType, Request, Transaction } from "mssql";
import type { CompiledSql, SqlParameter } from "sqyra";
import { normalizeRows } from "../normalize.js";
import type {
  DatabaseAdapter,
  DatabaseTransaction,
  ExecutionOptions,
  ExecutionResult,
} from "../types.js";

type Executor = ConnectionPool | Transaction;
export class TsqlAdapter implements DatabaseAdapter {
  readonly name = "tsql" as const;
  readonly dialect = "tsql" as const;
  private pool: ConnectionPool | null = null;
  constructor(
    private readonly connection: string | config,
    private readonly executor: Executor | null = null,
  ) {}
  async open(): Promise<void> {
    if (this.executor !== null || this.pool !== null) return;
    const pool = await new sql.ConnectionPool(this.connection).connect();
    const acquire: unknown = Reflect.get(pool, "acquire");
    const release: unknown = Reflect.get(pool, "release");
    if (typeof acquire !== "function" || typeof release !== "function")
      throw new Error("mssql/msnodesqlv8 pool lacks acquire/release methods.");
    const connection: object = await Reflect.apply(acquire, pool, [pool]);
    try {
      // msnodesqlv8 defaults BIGINT to JS number and NUMERIC to a rounded
      // number. Set its native connection switches before returning the sole
      // pooled connection; the fixed-size pool preserves this setting.
      for (const [name, enabled] of [
        ["setUseBigIntAsNative", true],
        ["setUseNumericString", true],
      ] as const) {
        const method: unknown = Reflect.get(connection, name);
        if (typeof method !== "function")
          throw new Error(`msnodesqlv8 connection lacks '${name}'.`);
        Reflect.apply(method, connection, [enabled]);
      }
    } finally {
      Reflect.apply(release, pool, [connection]);
    }
    this.pool = pool;
  }
  async close(): Promise<void> {
    if (this.executor === null) await this.pool?.close();
    this.pool = null;
  }
  async healthCheck(): Promise<void> {
    await this.execute({ sql: "SELECT 1 AS value", parameters: [] });
  }
  async execute(compiled: CompiledSql, options?: ExecutionOptions): Promise<ExecutionResult> {
    const request = createRequest(this.executor ?? this.required());
    const names = compiled.parameters.map((parameter) => parameter.name);
    if (new Set(names).size !== names.length)
      throw new Error(`Duplicate T-SQL parameters in compiled SQL: ${names.join(",")}.`);
    // The installed ODBC driver confuses short numeric parameter prefixes in
    // large batches (p1 and p10). Keep Sqyra's compiled names intact and use
    // fixed-width names only at this driver boundary.
    const renames = new Map(
      names.map((name, index) => [name, `sq_${String(index).padStart(6, "0")}`]),
    );
    const driverSql = renameTsqlVariables(compiled.sql, renames);
    for (const parameter of compiled.parameters)
      request.input(
        renames.get(parameter.name)!,
        parameterType(parameter),
        parameterValue(parameter),
      );
    const abort = (): void => request.cancel();
    options?.signal?.addEventListener("abort", abort, { once: true });
    try {
      const result = await request.query<Readonly<Record<string, unknown>>>(driverSql);
      return {
        rows: normalizeTsqlRows(result.recordset ?? []),
        affectedRows: (result.rowsAffected ?? []).reduce(
          (sum: number, count: number) => sum + count,
          0,
        ),
      };
    } catch (error) {
      throw new Error(
        `T-SQL query failed. SQL: ${compiled.sql.slice(0, 800)}. Parameters: ${names.slice(0, 40).join(",")}. Cause: ${error instanceof Error ? error.message : String(error)}`,
      );
    } finally {
      options?.signal?.removeEventListener("abort", abort);
    }
  }
  async executeScript(text: string): Promise<void> {
    await createRequest(this.executor ?? this.required()).batch(text);
  }
  async beginTransaction(isolation?: "serializable"): Promise<DatabaseTransaction> {
    if (this.executor !== null)
      throw new Error("There is an already running transaction associated with this connection");
    const transaction = new sql.Transaction(this.required());
    await transaction.begin(
      isolation === "serializable"
        ? sql.ISOLATION_LEVEL.SERIALIZABLE
        : sql.ISOLATION_LEVEL.READ_COMMITTED,
    );
    const nested = new TsqlAdapter(this.connection, transaction);
    let completed = false;
    const finish = async (commit: boolean): Promise<void> => {
      if (completed) throw new Error("The transaction has already completed");
      completed = true;
      if (commit) await transaction.commit();
      else await transaction.rollback();
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
    const result = new TsqlAdapter(this.connection);
    await result.open();
    return result;
  }
  private required(): ConnectionPool {
    if (this.pool === null) throw new Error("T-SQL adapter is not open.");
    return this.pool;
  }
}

export function normalizeTsqlRows(
  rows: ReadonlyArray<Readonly<Record<string, unknown>>>,
): ReturnType<typeof normalizeRows> {
  const columns: unknown = Reflect.get(rows, "columns");
  if (typeof columns !== "object" || columns === null) return normalizeRows(rows);
  const declarations = new Map<string, string>();
  for (const [name, metadata] of Object.entries(columns)) {
    if (typeof metadata !== "object" || metadata === null) continue;
    const type: unknown = Reflect.get(metadata, "type");
    const declaration: unknown =
      typeof type === "function" ? Reflect.get(type, "declaration") : null;
    if (typeof declaration === "string") declarations.set(name, declaration.toLowerCase());
  }
  return normalizeRows(
    rows.map((row) =>
      Object.fromEntries(
        Object.entries(row).map(([name, value]) => {
          const type = declarations.get(name);
          if (typeof value === "string" && /^-?\d+$/.test(value) && type === "bigint")
            return [name, BigInt(value)];
          if (
            typeof value === "string" &&
            /^-?\d+$/.test(value) &&
            ["tinyint", "smallint", "int"].includes(type ?? "")
          )
            return [name, Number(value)];
          if (
            typeof value === "string" &&
            /^-?\d+\.\d+$/.test(value) &&
            (type === "decimal" || type === "numeric")
          )
            return [name, value.replace(/(\.\d*?[1-9])0+$/, "$1").replace(/\.0+$/, "")];
          if (typeof value === "string" && (value === "0" || value === "1") && type === "bit")
            return [name, value === "1"];
          if (typeof value === "string" && type === "xml")
            return [name, value.replace(/\s*\/>/g, " />")];
          return [name, value];
        }),
      ),
    ),
  );
}

export function renameTsqlVariables(sqlText: string, names: ReadonlyMap<string, string>): string {
  let output = "",
    quote: "'" | '"' | "[" | null = null;
  for (let index = 0; index < sqlText.length;) {
    const char = sqlText[index]!;
    if (quote !== null) {
      output += char;
      if (char === (quote === "[" ? "]" : quote)) {
        if (sqlText[index + 1] === char) {
          output += char;
          index += 2;
          continue;
        }
        quote = null;
      }
      index++;
      continue;
    }
    if (char === "'" || char === '"' || char === "[") {
      quote = char;
      output += char;
      index++;
      continue;
    }
    if (char === "@" && /[A-Za-z_]/.test(sqlText[index + 1] ?? "")) {
      let end = index + 2;
      while (/[A-Za-z_0-9]/.test(sqlText[end] ?? "")) end++;
      const name = sqlText.slice(index + 1, end);
      output += `@${names.get(name) ?? name}`;
      index = end;
      continue;
    }
    output += char;
    index++;
  }
  return output;
}

function parameterValue(parameter: SqlParameter): unknown {
  return typeof parameter.value === "bigint"
    ? parameter.value.toString()
    : parameter.value instanceof Uint8Array
      ? Buffer.from(parameter.value)
      : parameter.value;
}
function createRequest(executor: Executor): Request {
  return "isolationLevel" in executor ? new sql.Request(executor) : new sql.Request(executor);
}
function parameterType(parameter: SqlParameter): ISqlType | (() => ISqlType) {
  switch (parameter.type) {
    case "ExprByteLiteral":
      return sql.TinyInt;
    case "ExprInt16Literal":
      return sql.SmallInt;
    case "ExprInt32Literal":
      return sql.Int;
    case "ExprInt64Literal":
      return sql.BigInt;
    case "ExprDecimalLiteral":
      return sql.Decimal(38, 18);
    case "ExprDoubleLiteral":
      return sql.Float;
    case "ExprBoolLiteral":
      return sql.Bit;
    case "ExprGuidLiteral":
      return sql.UniqueIdentifier;
    case "ExprByteArrayLiteral":
      return sql.VarBinary(sql.MAX);
    case "ExprDateTimeLiteral":
      return sql.DateTime2;
    case "ExprDateTimeOffsetLiteral":
      return sql.DateTimeOffset;
    default:
      return typeof parameter.value === "string" && parameter.value.length <= 4000
        ? sql.NVarChar(Math.max(1, parameter.value.length))
        : sql.NVarChar(sql.MAX);
  }
}
