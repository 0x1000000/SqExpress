import {
  aggregate,
  column,
  defineTable,
  insertInto,
  nullableColumn,
  select,
  sqlType,
  type Expr,
  type InlineExportOptions,
} from "sqyra";
import { scalar } from "../context.js";
import type { ScenarioContext } from "../types.js";
import type { Scenario } from "./types.js";

export const parametrizationLimitBoundaryScenario: Scenario = {
  source: "ScParametrizationLimitBoundary",
  async run(context) {
    if (context.parameterization === "none") return;
    const boundary =
      context.dialect === "tsql"
        ? { limit: 2000, rows: 1000, columns: 2, overflowRows: 667, overflowColumns: 3 }
        : context.dialect === "sqlite"
          ? { limit: 32766, rows: 10922, columns: 3, overflowRows: 10923, overflowColumns: 3 }
          : { limit: 65535, rows: 21845, columns: 3, overflowRows: 21846, overflowColumns: 3 };
    const table2 = defineTwo();
    const table3 = defineThree();
    const script = table3.$script;
    const options = scriptOptions(context);
    await context.database.executeScript(script.dropIfExists().toSql(options));
    await context.database.executeScript(script.create().toSql(options));
    await insertRows(context, table2, table3, boundary.rows, boundary.columns);
    await assertCount(context, table3, boundary.rows);
    await context.database.executeScript(script.drop().toSql(options));
    await context.database.executeScript(script.create().toSql(options));
    if (context.parameterization === "throw-on-limit") {
      let expected = false;
      try {
        await insertRows(context, table2, table3, boundary.overflowRows, boundary.overflowColumns);
      } catch (error) {
        if (error instanceof RangeError && error.message.includes(String(boundary.limit)))
          expected = true;
        else throw error;
      }
      if (!expected) throw new Error(`Expected parameter-limit failure above ${boundary.limit}.`);
    } else {
      await insertRows(context, table2, table3, boundary.overflowRows, boundary.overflowColumns);
      await assertCount(context, table3, boundary.overflowRows);
    }
    await context.database.executeScript(script.drop().toSql(options));
  },
};

function scriptOptions(context: ScenarioContext): InlineExportOptions {
  return {
    dialect: context.database.dialect,
    ...(context.database.mysqlFlavor === undefined
      ? {}
      : { mysqlFlavor: context.database.mysqlFlavor }),
  };
}
async function insertRows(
  context: ScenarioContext,
  table2: ReturnType<typeof defineTwo>,
  table3: ReturnType<typeof defineThree>,
  rows: number,
  columns: number,
): Promise<void> {
  let expression: Expr;
  if (columns === 2) {
    const [first, ...rest] = Array.from({ length: rows }, (_, index) => ({
      Value1: index + 1,
      Value2: -(index + 1),
    }));
    if (first === undefined) throw new Error("A parameter-limit case requires rows.");
    expression = insertInto(table2).values(first, ...rest).ast;
  } else {
    const [first, ...rest] = Array.from({ length: rows }, (_, index) => ({
      Value1: index + 1,
      Value2: index + 10001,
      Value3: index + 20001,
    }));
    if (first === undefined) throw new Error("A parameter-limit case requires rows.");
    expression = insertInto(table3).values(first, ...rest).ast;
  }
  const compiled = context.compile(expression);
  const limit = context.dialect === "tsql" ? 2000 : context.dialect === "sqlite" ? 32766 : 65535;
  if (compiled.parameters.length !== Math.min(rows * columns, limit))
    throw new Error("Parameter count does not match the provider boundary/fallback strategy.");
  for (const [index, parameter] of compiled.parameters.entries()) {
    const row = Math.floor(index / columns) + 1;
    const columnIndex = index % columns;
    const expectedValue =
      columnIndex === 0 ? row : columns === 2 ? -row : row + columnIndex * 10000;
    if (
      parameter.name !== `p${index}` ||
      parameter.type !== "ExprInt32Literal" ||
      parameter.value !== expectedValue
    )
      throw new Error(`Parameter ${index} lost its name, order, type, or exact value.`);
  }
  await context.database.execute(compiled);
  const stored = await context.query(
    select(table3.Value1, table3.Value2, table3.Value3).from(table3).orderBy(table3.Value1),
  );
  if (stored.length !== rows)
    throw new Error(`Expected ${rows} bound/fallback rows, received ${stored.length}.`);
  for (const [index, storedRow] of stored.entries()) {
    const expected = index + 1;
    if (
      Number(storedRow.Value1) !== expected ||
      Number(storedRow.Value2) !== (columns === 2 ? -expected : expected + 10000) ||
      (columns === 2 ? storedRow.Value3 !== null : Number(storedRow.Value3) !== expected + 20000)
    )
      throw new Error(`Bound/fallback row ${index} changed its values or order.`);
  }
}
function defineTwo() {
  return defineTable({
    schema: null,
    name: "ParamLimitProbe",
    temporary: true,
    columns: { Value1: column(sqlType.int32), Value2: nullableColumn(sqlType.int32) },
  });
}
function defineThree() {
  return defineTable({
    schema: null,
    name: "ParamLimitProbe",
    temporary: true,
    columns: {
      Value1: column(sqlType.int32),
      Value2: nullableColumn(sqlType.int32),
      Value3: nullableColumn(sqlType.int32),
    },
  });
}
async function assertCount(
  context: ScenarioContext,
  table: ReturnType<typeof defineThree>,
  expected: number,
): Promise<void> {
  const value = scalar(
    await context.query(select(aggregate("COUNT", 1).cast({ kind: "ExprTypeInt64" })).from(table)),
  );
  if (value !== BigInt(expected) && value !== expected)
    throw new Error(`Expected ${expected} rows but received ${String(value)}.`);
}
