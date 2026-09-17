import { select } from "sqyra";
import { defineIntegrationTables } from "../tables.js";
import type { Scenario } from "./types.js";

export const selectTopScenario: Scenario = {
  source: "ScSelectTop",
  async run(context) {
    const { user } = defineIntegrationTables(context.dialect);
    const first = await context.query(
      select({ UserId: user.UserId, Email: user.Email }, { top: 2 })
        .from(user)
        .orderBy(user.FirstName),
    );
    if (first.length !== 2 || first.some((row) => typeof row.Email !== "string"))
      throw new Error("TOP 2 did not return two user email models.");
    const offset = await context.query(
      select({ UserId: user.UserId, Email: user.Email })
        .from(user)
        .orderBy(user.UserId)
        .offsetFetch(5, 2),
    );
    if (
      offset.length !== 2 ||
      Number(offset[0]?.UserId) <= 5 ||
      Number(offset[1]?.UserId) <= Number(offset[0]?.UserId)
    )
      throw new Error("TOP/OFFSET did not return the expected two ordered users after offset 5.");
  },
};
