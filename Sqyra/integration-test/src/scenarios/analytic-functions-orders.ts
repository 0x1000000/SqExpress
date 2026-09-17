import {
  column,
  columnExpression,
  defineTable,
  exprAnalyticFunction,
  exprCast,
  exprFrameClause,
  exprFunctionName,
  exprMul,
  exprOrderBy,
  exprOrderByItem,
  exprOver,
  exprTypeInt64,
  exprUnboundedFrameBorder,
  insertInto,
  lit,
  select,
  sqlType,
  type ExprValue,
  type InlineExportOptions,
} from "sqyra";
import type { Scenario } from "./types.js";

const groups = [1, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4] as const;
function analytic(
  name: string,
  argumentsList: ReadonlyArray<ExprValue> | null,
  partition: ExprValue | null,
  order: ExprValue,
  descending = false,
  frame: ReturnType<typeof exprFrameClause> | null = null,
) {
  return exprAnalyticFunction({
    name: exprFunctionName({ builtIn: true, name }),
    arguments: argumentsList,
    over: exprOver({
      partitions: partition === null ? null : [partition],
      orderBy: exprOrderBy({
        orderList: [exprOrderByItem({ value: order, descendant: descending })],
      }),
      frameClause: frame,
    }),
  });
}
const frameStart = exprUnboundedFrameBorder({ frameBorderDirection: "Preceding" });
const frameEnd = exprUnboundedFrameBorder({ frameBorderDirection: "Following" });
const asInt64 = (expression: ReturnType<typeof analytic>) =>
  exprCast({ expression, sqlType: exprTypeInt64 });

export const analyticFunctionsOrdersScenario: Scenario = {
  source: "ScAnalyticFunctionsOrders",
  async run(context) {
    const table = defineTable({
      schema: null,
      name: "TestData",
      temporary: true,
      columns: { Index: column(sqlType.int32), Group: column(sqlType.int32) },
    });
    const script: InlineExportOptions = {
      dialect: context.database.dialect,
      ...(context.database.mysqlFlavor === undefined
        ? {}
        : { mysqlFlavor: context.database.mysqlFlavor }),
    };
    await context.database.executeScript(table.$script.dropIfExists().toSql(script));
    await context.database.executeScript(table.$script.create().toSql(script));
    try {
      const data = groups.map((Group, index) => ({ Index: index + 1, Group }));
      const first = data[0];
      if (first === undefined) throw new Error("Source analytic data is empty.");
      await context.execute(
        insertInto(table)
          .columns("Index", "Group")
          .values(first, ...data.slice(1)),
      );
      const index = columnExpression(table.Index),
        group = columnExpression(table.Group);
      const rowQuery = select({
        Index: table.Index,
        Group: table.Group,
        Num: asInt64(analytic("ROW_NUMBER", null, null, index, true)),
      }).from(table);
      const rowNo = await context.query(rowQuery);
      if (
        rowNo.length !== data.length ||
        rowNo.some((row) => Number(row.Index) !== 15 - Number(row.Num))
      )
        throw new Error("ROW_NUMBER descending assertion failed.");
      const partitioned = await context.query(
        select({
          Index: table.Index,
          Group: table.Group,
          Num: asInt64(analytic("ROW_NUMBER", null, group, index)),
        }).from(table),
      );
      const expectedRank = [1, 2, 3, 4, 5, 1, 2, 3, 4, 1, 2, 3, 4, 1];
      if (
        partitioned.length !== data.length ||
        partitioned.some((row, position) => Number(row.Num) !== expectedRank[position])
      )
        throw new Error("Partitioned ROW_NUMBER assertion failed.");
      const source = select({ Index: exprMul({ left: index, right: lit(10) }), Group: table.Group })
        .from(table)
        .as("sub", { Index: column(sqlType.int32), Group: column(sqlType.int32) });
      const sourceIndex = columnExpression(source.Index),
        sourceGroup = columnExpression(source.Group);
      const rankQuery = select({
        Index: source.Index,
        Group: source.Group,
        Num: asInt64(analytic("RANK", null, sourceGroup, sourceIndex)),
      }).from(source);
      const ranked = await context.query(rankQuery);
      if (
        ranked.length !== data.length ||
        ranked.some((row, position) => Number(row.Num) !== expectedRank[position])
      )
        throw new Error("RANK assertion failed.");
      const values = await context.query(
        select({
          Index: table.Index,
          Group: table.Group,
          First: analytic(
            "FIRST_VALUE",
            [index],
            group,
            index,
            false,
            exprFrameClause({ start: frameStart, end: null }),
          ),
          Last: analytic(
            "LAST_VALUE",
            [index],
            group,
            index,
            false,
            exprFrameClause({ start: frameStart, end: frameEnd }),
          ),
        }).from(table),
      );
      const expectedFirst = [1, 1, 1, 1, 1, 6, 6, 6, 6, 10, 10, 10, 10, 14];
      const expectedLast = [5, 5, 5, 5, 5, 9, 9, 9, 9, 13, 13, 13, 13, 14];
      if (
        values.length !== data.length ||
        values.some(
          (row, position) =>
            Number(row.First) !== expectedFirst[position] ||
            Number(row.Last) !== expectedLast[position],
        )
      )
        throw new Error("FIRST_VALUE/LAST_VALUE frame assertions failed.");
      const lagLead = await context.query(
        select({
          Index: table.Index,
          Group: table.Group,
          Lag: analytic("LAG", [index], group, index),
          Lag2: analytic("LAG", [index, lit(2)], group, index),
          LagDef: analytic(
            "LAG",
            context.dialect === "mysql-oracle" || context.dialect === "mariadb"
              ? [index, lit(1)]
              : [index, lit(1), lit(100)],
            group,
            index,
          ),
          Lead: analytic("LEAD", [index], group, index),
          Lead2: analytic("LEAD", [index, lit(2)], group, index),
          LeadDef: analytic(
            "LEAD",
            context.dialect === "mysql-oracle" || context.dialect === "mariadb"
              ? [index, lit(1)]
              : [index, lit(1), lit(100)],
            group,
            index,
          ),
        }).from(table),
      );
      if (lagLead.length !== data.length) throw new Error("LAG/LEAD row-count assertion failed.");
      const other = await context.query(
        select({
          Index: table.Index,
          Group: table.Group,
          DenseRank: analytic("DENSE_RANK", null, group, index),
          Ntile: analytic("NTILE", [lit(2)], group, index),
          CumeDist: analytic("CUME_DIST", null, group, index),
          PercentRank: analytic("PERCENT_RANK", null, group, index),
        }).from(table),
      );
      if (other.length !== data.length)
        throw new Error("Additional analytic function row-count assertion failed.");
    } finally {
      await context.database.executeScript(table.$script.drop().toSql(script));
    }
  },
};
