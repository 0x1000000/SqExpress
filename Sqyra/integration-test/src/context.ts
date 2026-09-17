import { toSql, type CompiledSql, type ParameterizedExportOptions } from "sqyra";
import type {
  CanonicalRow,
  DatabaseAdapter,
  ExecutionResult,
  IntegrationParameterization,
  ScenarioContext,
  ScenarioExpression,
} from "./types.js";

export function createContext(
  database: DatabaseAdapter,
  parameterization: IntegrationParameterization,
): ScenarioContext {
  const compile = (expression: ScenarioExpression): CompiledSql => {
    const options: ParameterizedExportOptions = {
      dialect: database.dialect,
      parameterize: parameterization,
      ...(database.mysqlFlavor === undefined ? {} : { mysqlFlavor: database.mysqlFlavor }),
      ...(database.schemaMap === undefined ? {} : { schemaMap: database.schemaMap }),
      ...(database.dialect === "pgsql" ? { strictTemporalTyping: true } : {}),
      ...(database.dialect === "tsql" ? { unicodeLiterals: true } : {}),
    };
    return "toSql" in expression ? expression.toSql(options) : toSql(expression, options);
  };
  const execute = (expression: ScenarioExpression): Promise<ExecutionResult> =>
    database.execute(compile(expression));
  return Object.freeze({
    database,
    dialect: database.name,
    parameterization,
    compile,
    execute,
    query: async <Row extends CanonicalRow>(
      expression: ScenarioExpression,
    ): Promise<ReadonlyArray<Row>> => (await execute(expression)).rows as ReadonlyArray<Row>,
  });
}

export function scalar(rows: ReadonlyArray<CanonicalRow>): CanonicalRow[string] | undefined {
  const row = rows[0];
  return row === undefined ? undefined : Object.values(row)[0];
}
