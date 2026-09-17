import {
  aggregate,
  column,
  dateTimeValue,
  exprDateTimeLiteral,
  exprGuidLiteral,
  guidValue,
  mergeInto,
  select,
  sqlType,
  values,
} from "sqyra";
import { scalar } from "../context.js";
import { defineIntegrationTables } from "../tables.js";
import type { Scenario } from "./types.js";

export const mergeExprScenario: Scenario = {
  source: "ScMergeExpr",
  async run(context) {
    const { user } = defineIntegrationTables(context.dialect);
    const existing = await context.query(
      select(user.UserId, user.FirstName).from(user).orderBy(user.UserId).offsetFetch(0, 10),
    );
    if (existing.length !== 10)
      throw new Error(`Expected ten source users for MERGE, received ${existing.length}.`);
    const inputs = [
      ...existing.map((row) => [Number(row.UserId), `Mode ${String(row.FirstName)}`] as const),
      [-1777, "Mode New User"] as const,
    ];
    const source = values(inputs, "MergeUsers", {
      UserId: column(sqlType.int32),
      FirstName: column(sqlType.string(255)),
    });
    const timestamp = exprDateTimeLiteral({
      value: dateTimeValue(new Date().toISOString().slice(0, 19)),
    });
    const guid = exprGuidLiteral({ value: guidValue("a4c7b59a-6076-4f15-a83f-08de8bd02026") });
    await context.execute(
      mergeInto(user, source)
        .on(user.UserId.eq(source.UserId))
        .whenMatchedUpdate({ FirstName: source.FirstName })
        .whenNotMatchedInsert({
          FirstName: source.FirstName,
          LastName: "Last Name",
          ExternalId: guid,
          Email: "LastName@email.com",
          RegDate: timestamp,
        })
        .whenNotMatchedBySourceUpdate({ Version: user.Version.add(1) }),
    );
    const count = scalar(
      await context.query(
        select(aggregate("COUNT", user.UserId)).from(user).where(user.FirstName.like("Mode %")),
      ),
    );
    if (String(count) !== "11")
      throw new Error(`Expected 11 MERGE-updated/inserted users, received ${String(count)}.`);
  },
};
