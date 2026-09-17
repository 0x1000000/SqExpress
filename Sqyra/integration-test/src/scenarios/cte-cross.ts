import { column, cte, lit, select, sqlType } from "sqyra";
import type { Scenario } from "./types.js";

export const cteCrossScenario: Scenario = {
  source: "ScCteCross",
  async run(context) {
    const cte12 = cte("Cte12", { Val: column(sqlType.int32) }, () =>
      select(lit(1).as("Val")).unionAll(select(lit(2).as("Val"))),
    );
    const cte34 = cte("Cte34", { Val34: column(sqlType.int32) }, () =>
      select(lit(3).as("Val34")).unionAll(select(lit(4).as("Val34"))),
    );
    const rows = await context.query(
      select({ V1: cte12.Val, V2: cte34.Val34 })
        .from(cte12)
        .crossJoin(cte34)
        .orderBy(cte12.Val, cte34.Val34),
    );
    const expected = [
      [1, 3],
      [1, 4],
      [2, 3],
      [2, 4],
    ] as const;
    if (
      rows.length !== expected.length ||
      rows.some(
        (row, index) =>
          Number(row.V1) !== expected[index]?.[0] || Number(row.V2) !== expected[index]?.[1],
      )
    )
      throw new Error("Crossed CTE result did not match the four expected pairs.");
  },
};
