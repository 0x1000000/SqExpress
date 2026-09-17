import { exprTypeInt32, jsonTable, select } from "sqyra";
import type { Scenario } from "./types.js";

export const jsonTableScenario: Scenario = {
  source: "ScJsonTable",
  async run(context) {
    const item = jsonTable('{"items":[{"id":2,"meta":{"x":1}},{"id":5,"meta":{"x":2}}]}', "$.items")
      .value("Id", "$.id", exprTypeInt32)
      .query("Meta", "$.meta")
      .ordinal("Ordinal")
      .as("j");
    const rows = await context.query(
      select(item.Id, item.Meta, item.Ordinal).from(item).orderBy(item.Ordinal),
    );
    if (
      rows.length !== 2 ||
      Number(rows[0]?.Id) !== 2 ||
      Number(rows[1]?.Id) !== 5 ||
      Number(rows[0]?.Ordinal) !== 0 ||
      Number(rows[1]?.Ordinal) !== 1 ||
      typeof rows[0]?.Meta !== "string" ||
      rows[0].Meta.length === 0 ||
      typeof rows[1]?.Meta !== "string" ||
      rows[1].Meta.length === 0
    )
      throw new Error("Portable JSON table expansion returned an unexpected result.");
  },
};
