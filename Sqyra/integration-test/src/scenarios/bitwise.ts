import { exprBitwiseAnd, exprBitwiseOr, select, toExpr } from "sqyra";
import { scalar } from "../context.js";
import type { Scenario } from "./types.js";

export const bitwiseScenario: Scenario = {
  source: "ScBitwise",
  async run(context) {
    const expression = exprBitwiseOr({
      left: toExpr(3),
      right: exprBitwiseAnd({ left: toExpr(5), right: toExpr(2) }),
    });
    const value = scalar(await context.query(select(expression)));
    if (value !== 3 && value !== 3n) throw new Error(`Unexpected bitwise result: ${String(value)}`);
  },
};
