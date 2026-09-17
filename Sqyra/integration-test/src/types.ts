import type {
  BuiltStatement,
  CompiledSql,
  Expr,
  ParameterizationStrategy,
  SqlDialect,
} from "sqyra";
export type ScenarioExpression = Expr | BuiltStatement;

export type IntegrationDialect = "tsql" | "pgsql" | "mysql-oracle" | "mariadb" | "sqlite";
export type IntegrationParameterization = ParameterizationStrategy;
export type CanonicalValue = null | string | number | bigint | boolean | Uint8Array;
export type CanonicalRow = Readonly<Record<string, CanonicalValue>>;
export interface ExecutionResult<Row extends CanonicalRow = CanonicalRow> {
  readonly rows: ReadonlyArray<Row>;
  readonly affectedRows: number;
}
export interface ExecutionOptions {
  readonly signal?: AbortSignal;
}
export interface DatabaseTransaction {
  readonly database: DatabaseAdapter;
  commit(): Promise<void>;
  rollback(): Promise<void>;
}

export interface DatabaseAdapter {
  readonly name: IntegrationDialect;
  readonly dialect: SqlDialect;
  readonly mysqlFlavor?: "mariadb" | "oracle";
  readonly schemaMap?: ReadonlyArray<{ readonly from: string; readonly to: string }>;
  open(): Promise<void>;
  close(): Promise<void>;
  healthCheck(): Promise<void>;
  execute(compiled: CompiledSql, options?: ExecutionOptions): Promise<ExecutionResult>;
  executeScript(sql: string): Promise<void>;
  beginTransaction(isolation?: "serializable"): Promise<DatabaseTransaction>;
  transaction<T>(action: (transaction: DatabaseAdapter) => Promise<T>): Promise<T>;
  openSibling(): Promise<DatabaseAdapter>;
}

export interface ScenarioContext {
  readonly database: DatabaseAdapter;
  readonly dialect: IntegrationDialect;
  readonly parameterization: IntegrationParameterization;
  compile(expression: ScenarioExpression): CompiledSql;
  query<Row extends CanonicalRow>(expression: ScenarioExpression): Promise<ReadonlyArray<Row>>;
  execute(expression: ScenarioExpression): Promise<ExecutionResult>;
}
