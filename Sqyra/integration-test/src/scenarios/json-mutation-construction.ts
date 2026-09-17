import { lit, exprJsonNull, jsonArray, jsonObject, select } from "sqyra";
import type { Scenario } from "./types.js";

export const jsonMutationConstructionScenario: Scenario = {
  source: "ScJsonMutationAndConstruction",
  async run(context) {
    const constructed = jsonObject({
      name: "Ada",
      nothing: null,
      nested: lit('{"ok":true}').jsonQuery(),
      items: jsonArray(1, exprJsonNull, null),
    });
    const changed = lit('{"a":1,"remove":2}').jsonSet("$.a", 3).jsonRemove("$.remove");
    const rows = await context.query(select({ Constructed: constructed, Changed: changed }));
    const row = rows[0];
    if (row === undefined || typeof row.Constructed !== "string" || typeof row.Changed !== "string")
      throw new Error("Portable JSON construction returned non-string output.");
    const object = JSON.parse(row.Constructed) as {
      name?: unknown;
      nothing?: unknown;
      nested?: { ok?: unknown };
      items?: unknown[];
    };
    const mutation = JSON.parse(row.Changed) as { a?: unknown; remove?: unknown };
    if (
      object.name !== "Ada" ||
      object.nothing !== null ||
      object.nested?.ok !== true ||
      object.items?.length !== 3 ||
      mutation.a !== 3 ||
      "remove" in mutation
    )
      throw new Error(
        `Portable JSON construction or mutation returned an unexpected result: ${row.Constructed}; ${row.Changed}.`,
      );
  },
};
