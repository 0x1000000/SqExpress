import { lit, exprTypeBoolean, exprTypeDecimal, exprTypeInt32, exprTypeInt64, select } from "sqyra";
import type { Scenario } from "./types.js";

export const jsonReadScenario: Scenario = {
  source: "ScJsonRead",
  async run(context) {
    const document =
      '{"name":"Ada","active":true,"count":12,"price":12.5,"items":[{"id":7}],"none":null}';
    const rows = await context.query(
      select({
        Name: lit(document).jsonValue("$.name"),
        Active: lit(document).jsonValue("$.active", exprTypeBoolean),
        Count: lit(document).jsonValue("$.count", exprTypeInt32),
        Price: lit(document).jsonValue("$.price", exprTypeDecimal({ precisionScale: null })),
        ItemId: lit(document).jsonValue("$.items[0].id", exprTypeInt64),
        Items: lit(document).jsonQuery("$.items"),
        Missing: lit(document).jsonValue("$.missing"),
        JsonNull: lit(document).jsonValue("$.none"),
        WrongKind: lit(document).jsonValue("$.name", exprTypeInt32),
        SqlNull: lit(null).jsonValue("$.name"),
      }),
    );
    const row = rows[0];
    if (row === undefined) throw new Error("Portable JSON scalar extraction returned no row.");
    if (
      row.Name !== "Ada" ||
      !isTrue(row.Active) ||
      Number(row.Count) !== 12 ||
      Number(row.Price) !== 12.5 ||
      BigInt(String(row.ItemId)) !== 7n ||
      typeof row.Items !== "string" ||
      row.Missing !== null ||
      row.JsonNull !== null ||
      row.WrongKind !== null ||
      row.SqlNull !== null
    )
      throw new Error(
        `Portable JSON scalar extraction returned an unexpected result: ${JSON.stringify(row, (_, value: unknown) => (typeof value === "bigint" ? `${value}n` : value))}`,
      );
  },
};
function isTrue(value: unknown): boolean {
  return value === true || value === 1 || value === 1n;
}
