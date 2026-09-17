import Database from "better-sqlite3";
import type { CompiledSql } from "sqyra";
import { normalizeRows, parameterValues } from "../normalize.js";
import { defineIntegrationTables } from "../tables.js";
import type {
  DatabaseAdapter,
  DatabaseTransaction,
  ExecutionOptions,
  ExecutionResult,
} from "../types.js";

export class SqliteAdapter implements DatabaseAdapter {
  readonly name = "sqlite" as const;
  readonly dialect = "sqlite" as const;
  private database: Database.Database | null = null;
  constructor(private readonly filename = ":memory:") {}
  async open(): Promise<void> {
    this.database ??= new Database(this.filename);
    this.database.defaultSafeIntegers(true);
  }
  async close(): Promise<void> {
    this.database?.close();
    this.database = null;
  }
  async healthCheck(): Promise<void> {
    await this.execute({ sql: "SELECT 1 AS value", parameters: [] });
  }
  async execute(compiled: CompiledSql, options?: ExecutionOptions): Promise<ExecutionResult> {
    if (options?.signal?.aborted) throw options.signal.reason;
    const commands = splitSql(compiled.sql);
    const values = parameterValues(compiled.parameters);
    let offset = 0;
    let affectedRows = 0;
    let rows: ExecutionResult["rows"] = [];
    for (const command of commands) {
      const statement = this.required().prepare(command.sql);
      const bound = values.slice(offset, offset + command.placeholders);
      offset += command.placeholders;
      if (statement.reader)
        rows = normalizeSqliteRows(
          statement.all(...bound) as ReadonlyArray<Readonly<Record<string, unknown>>>,
          statement.columns(),
        );
      else affectedRows += statement.run(...bound).changes;
    }
    if (offset !== values.length)
      throw new RangeError(`SQLite SQL uses ${offset} parameters but received ${values.length}.`);
    return { rows, affectedRows };
  }
  async executeScript(sql: string): Promise<void> {
    this.required().exec(sql);
  }
  async beginTransaction(): Promise<DatabaseTransaction> {
    const db = this.required();
    if (db.inTransaction)
      throw new Error("There is an already running transaction associated with this connection");
    db.exec("BEGIN");
    let completed = false;
    const finish = async (sql: "COMMIT" | "ROLLBACK"): Promise<void> => {
      if (completed) throw new Error("The transaction has already completed");
      completed = true;
      db.exec(sql);
    };
    return { database: this, commit: () => finish("COMMIT"), rollback: () => finish("ROLLBACK") };
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
    const result = new SqliteAdapter(this.filename);
    await result.open();
    return result;
  }
  private required(): Database.Database {
    if (this.database === null) throw new Error("SQLite adapter is not open.");
    return this.database;
  }
}

export function normalizeSqliteRows(
  rows: ReadonlyArray<Readonly<Record<string, unknown>>>,
  columns: ReadonlyArray<Readonly<{ readonly name: string; readonly type: string | null }>>,
): ReturnType<typeof normalizeRows> {
  const types = new Map(columns.map((column) => [column.name, column.type?.toLowerCase() ?? ""]));
  // SQLite DDL uses INTEGER for every integer width; our test-local typed
  // descriptors supply the width that the driver cannot report.
  const known = knownColumnTypes();
  return normalizeRows(
    rows.map((row) =>
      Object.fromEntries(
        Object.entries(row).map(([name, value]) => {
          const type = types.get(name) ?? "";
          const kind = known.get(name);
          if (
            (typeof value === "number" || typeof value === "bigint") &&
            (kind === "decimal" || /^(?:decimal|numeric)(?:\(|$)/.test(type))
          )
            return [name, String(value)];
          if (
            typeof value === "bigint" &&
            (kind === "byte" || kind === "int16" || kind === "int32")
          )
            return [name, Number(value)];
          if (
            (value === 0 || value === 1 || value === 0n || value === 1n) &&
            (kind === "boolean" || /^(?:bool|boolean|bit)(?:\(|$)/.test(type))
          )
            return [name, value === 1 || value === 1n];
          return [name, value];
        }),
      ),
    ),
  );
}
let columnTypes: Map<string, string> | null = null;
function knownColumnTypes(): ReadonlyMap<string, string> {
  if (columnTypes === null) {
    columnTypes = new Map();
    for (const table of defineIntegrationTables("sqlite").ordered)
      for (const [name, definition] of Object.entries(table.$metadata.definitions)) {
        const existing = columnTypes.get(name);
        if (existing !== undefined && existing !== definition.sqlType.name)
          throw new Error(`Integration column ${name} has conflicting typed descriptors.`);
        columnTypes.set(name, definition.sqlType.name);
      }
  }
  return columnTypes;
}

function splitSql(
  sql: string,
): ReadonlyArray<{ readonly sql: string; readonly placeholders: number }> {
  const commands: Array<{ sql: string; placeholders: number }> = [];
  let start = 0,
    placeholders = 0,
    quote: "'" | '"' | "`" | null = null;
  for (let index = 0; index < sql.length; index++) {
    const char = sql[index];
    if (quote !== null) {
      if (char === quote) {
        if (sql[index + 1] === quote) index++;
        else quote = null;
      }
      continue;
    }
    if (char === "'" || char === '"' || char === "`") {
      quote = char;
      continue;
    }
    if (char === "?") placeholders++;
    if (char === ";") {
      const command = sql.slice(start, index).trim();
      if (command.length > 0) commands.push({ sql: command, placeholders });
      start = index + 1;
      placeholders = 0;
    }
  }
  const last = sql.slice(start).trim();
  if (last.length > 0) commands.push({ sql: last, placeholders });
  return commands;
}
