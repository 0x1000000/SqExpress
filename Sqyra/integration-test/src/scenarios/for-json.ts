import { lit, exprTypeInt32, jsonOutput, jsonTable, select, selectJson } from "sqyra";
import { scalar } from "../context.js";
import type { Scenario } from "./types.js";

export const forJsonScenario: Scenario = {
  source: "ScForJson",
  async run(context) {
    const json = scalar(
      await context.query(
        selectJson(
          jsonOutput(1, "$.id"),
          jsonOutput("Ada", "$.customer.name"),
          jsonOutput(lit('{"active":true}').jsonQuery(), "$.metadata"),
          jsonOutput(null, "$.optional"),
        ).forJson(),
      ),
    );
    if (typeof json !== "string")
      throw new Error("FOR JSON returned SQL NULL or a non-string value.");
    const rows = JSON.parse(json) as Array<{
      id?: unknown;
      customer?: { name?: unknown };
      metadata?: { active?: unknown };
      optional?: unknown;
    }>;
    if (
      rows[0]?.id !== 1 ||
      rows[0]?.customer?.name !== "Ada" ||
      rows[0]?.metadata?.active !== true ||
      rows[0]?.optional !== null
    )
      throw new Error(`Portable relational JSON output returned an unexpected result: ${json}`);
    const objectValue = scalar(
      await context.query(
        select({ id: 1, optional: null }).forJson({
          withoutArrayWrapper: true,
          includeNullValues: false,
        }),
      ),
    );
    if (typeof objectValue !== "string") throw new Error("FOR JSON object mode returned SQL NULL.");
    const object = JSON.parse(objectValue) as { id?: unknown; optional?: unknown };
    if (object.id !== 1 || "optional" in object)
      throw new Error("Portable FOR JSON object/null options returned an unexpected result.");
    const empty = jsonTable("[]", "$").value("id", "$", exprTypeInt32).as("emptyRows");
    if (
      scalar(
        await context.query(select(empty.id).from(empty).forJson({ withoutArrayWrapper: true })),
      ) !== null
    )
      throw new Error("FOR JSON object mode must return SQL NULL for zero rows.");
    const two = jsonTable("[1,2]", "$").value("id", "$", exprTypeInt32).as("twoRows");
    let rejected = false;
    try {
      await context.query(select(two.id).from(two).forJson({ withoutArrayWrapper: true }));
    } catch {
      rejected = true;
    }
    if (!rejected) throw new Error("FOR JSON object mode must reject multiple rows.");
  },
};
