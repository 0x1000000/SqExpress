import {
  lit,
  caseWhen,
  decimalValue,
  exprAlias,
  exprAllColumns,
  exprDecimalLiteral,
  exprFuncCoalesce,
  exprFuncIsNull,
  exprGetUtcDate,
  exprIsNull,
  exprTableAlias,
  exprTypeDouble,
  exprTypeInt32,
  select,
  toExpr,
} from "sqyra";
import { scalar } from "../context.js";
import { defineIntegrationTables } from "../tables.js";
import type { Scenario } from "./types.js";

export const selectLogicScenario: Scenario = {
  source: "ScSelectLogic",
  async run(context) {
    const caseValue = scalar(
      await context.query(
        select(caseWhen(lit(1).add(2).eq(3)).then(17).else(2).cast(exprTypeInt32)),
      ),
    );
    if (Number(caseValue) !== 17) throw new Error("CASE WHEN returned an unexpected value.");
    const arithmetic = scalar(
      await context.query(
        select(lit(7).add(3).subtract(1).multiply(2).divide(6).cast(exprTypeDouble)),
      ),
    );
    if (Number(arithmetic) !== 3)
      throw new Error("Arithmetic expression returned an unexpected value.");
    const fallback = scalar(
      await context.query(select(exprFuncIsNull({ test: toExpr(null), alt: toExpr("NotNull") }))),
    );
    if (fallback !== "NotNull") throw new Error("ISNULL/COALESCE returned an unexpected value.");
    const nullCase = scalar(
      await context.query(
        select(
          caseWhen(exprIsNull({ test: toExpr(null), not: false }))
            .then("Tr".concat("ue"))
            .else("False"),
        ),
      ),
    );
    if (nullCase !== "True") throw new Error("Null CASE returned an unexpected value.");
    const exact = exprDecimalLiteral({ value: decimalValue("2.123456") });
    const coalesced = scalar(
      await context.query(
        select(exprFuncCoalesce({ test: toExpr(null), alts: [toExpr(null), exact] })),
      ),
    );
    if (Number(coalesced) !== 2.123456) throw new Error("COALESCE lost its decimal value.");
    if (scalar(await context.query(select(exprGetUtcDate))) === null)
      throw new Error("UTC date function returned null.");

    const tables = defineIntegrationTables(context.dialect);
    const user = tables.user;
    const customer = tables.customer;
    await context.query(select(exprAllColumns({ source: null }), { top: 1 }).from(user));
    const userAlias = exprTableAlias({
      alias: exprAlias({ name: user.$metadata.alias || user.$metadata.name }),
    });
    const customerAlias = exprTableAlias({
      alias: exprAlias({ name: customer.$metadata.alias || customer.$metadata.name }),
    });
    await context.query(select(exprAllColumns({ source: userAlias }), { top: 1 }).from(user));
    await context.query(
      select([exprAllColumns({ source: userAlias }), exprAllColumns({ source: customerAlias })], {
        top: 1,
      })
        .from(user)
        .innerJoin(customer),
    );
  },
};
