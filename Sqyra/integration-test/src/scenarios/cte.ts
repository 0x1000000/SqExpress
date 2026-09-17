import {
  column,
  columnExpression,
  cte,
  defineTable,
  deleteFrom,
  exprMul,
  insertInto,
  lit,
  select,
  sqlType,
  update,
  type InlineExportOptions,
} from "sqyra";
import type { Scenario } from "./types.js";

const simple = (suffix = "") =>
  cte(`SimpleRecursiveCte${suffix}`, { Num: column(sqlType.int32) }, (self) =>
    select({ Num: lit(1) }).unionAll(
      select({ Num: self.Num.add(1) })
        .from(self)
        .where(self.Num.lt(10)),
    ),
  );
const withOriginal = (multiplier: number) =>
  cte(
    "RefSimpleRecursiveWithOriginalCte",
    { Num: column(sqlType.int32), OriginalNum: column(sqlType.int32) },
    () => {
      const source = simple();
      return select({
        OriginalNum: source.Num,
        Num: exprMul({ left: columnExpression(source.Num), right: lit(multiplier) }),
      })
        .from(source)
        .where(source.Num.lt(10));
    },
  );

function assertRows(
  rows: ReadonlyArray<Readonly<Record<string, unknown>>>,
  expectedCount: number,
  multiplier: number,
  exactCount = true,
): void {
  if (exactCount ? rows.length !== expectedCount : rows.length < expectedCount)
    throw new Error(
      `Expected ${exactCount ? "exactly" : "at least"} ${expectedCount} recursive CTE rows, received ${rows.length}.`,
    );
  for (let index = 0; index < expectedCount; index++) {
    const row = rows[index]!;
    if (Number(row.Val1) !== index + 1 || Number(row.Val2) !== (index + 1) * multiplier)
      throw new Error(
        `Expected (${index + 1},${(index + 1) * multiplier}), received (${String(row.Val1)},${String(row.Val2)}).`,
      );
  }
}

export const cteScenario: Scenario = {
  source: "ScCte",
  async run(context) {
    const target = defineTable({
      schema: null,
      name: "TargetTable",
      temporary: true,
      columns: {
        Val1: column(sqlType.int32, { primaryKey: true, identity: true }),
        Val2: column(sqlType.int32),
      },
    });
    const script: InlineExportOptions = {
      dialect: context.database.dialect,
      ...(context.database.mysqlFlavor === undefined
        ? {}
        : { mysqlFlavor: context.database.mysqlFlavor }),
    };
    await context.database.executeScript(target.$script.dropIfExists().toSql(script));
    await context.database.executeScript(target.$script.create().toSql(script));
    const rows = async () =>
      context.query(
        select(target.Val1, target.Val2).from(target).orderBy(target.Val1, target.Val2),
      );
    try {
      const initial = withOriginal(10);
      const insertAst = insertInto(target)
        .from(
          select({ Val1: initial.OriginalNum, Val2: initial.Num }).from(initial),
          "Val1",
          "Val2",
        )
        .identity("Val1");
      await context.execute(insertAst);
      assertRows(await rows(), 9, 10);
      const scaled = withOriginal(100),
        simple2 = simple("2");
      const updateAst = update(target)
        .set({ Val2: scaled.Num })
        .from(target)
        .innerJoin(simple2, simple2.Num.eq(target.Val1))
        .innerJoin(scaled, scaled.OriginalNum.eq(simple2.Num))
        .done().ast;
      await context.execute(updateAst);
      assertRows(await rows(), 9, 100);
      if (context.dialect === "sqlite")
        await context.execute(deleteFrom(target).where(target.Val1.gt(5)));
      else {
        const reduced = select({ Num: scaled.Num }).from(scaled).where(scaled.Num.lt(50));
        await context.execute(
          deleteFrom(target)
            .from(target)
            .innerJoin(simple2, simple2.Num.eq(target.Val1))
            .innerJoin(reduced, reduced.Num.eq(simple2.Num))
            .done().ast,
        );
      }
      // C# checks the first five rows, without asserting the joined DELETE's count.
      assertRows(await rows(), 5, 100, false);
      if (context.dialect !== "mysql-oracle" && context.dialect !== "mariadb") {
        const deletion =
          context.dialect === "sqlite"
            ? deleteFrom(target).output(target.Val1, target.Val2)
            : deleteFrom(target)
                .from(target)
                .innerJoin(simple2, simple2.Num.eq(target.Val1))
                .done()
                .output(target.Val1, target.Val2);
        const output = await context.query(deletion.ast);
        assertRows(output, 5, 100, false);
        if ((await rows()).length !== 0)
          throw new Error("DELETE OUTPUT did not remove all remaining target rows.");
      }
    } finally {
      await context.database.executeScript(target.$script.dropIfExists().toSql(script));
    }
  },
};
