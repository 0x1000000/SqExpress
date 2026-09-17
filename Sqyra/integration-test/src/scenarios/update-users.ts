import { aggregate, exprTypeInt64, select, update } from "sqyra";
import { scalar } from "../context.js";
import { defineIntegrationTables } from "../tables.js";
import type { Scenario } from "./types.js";

export const updateUsersScenario: Scenario = {
  source: "ScUpdateUsers",
  async run(context) {
    const { user, customer } = defineIntegrationTables(context.dialect);
    const maxVersion = Number(
      scalar(await context.query(select(aggregate("MAX", user.Version)).from(user))),
    );
    const countBefore = scalar(
      await context.query(
        select(aggregate("COUNT", 1).cast(exprTypeInt64))
          .from(user)
          .where(
            user.Version.eq(maxVersion).and(
              select(1).from(customer).where(customer.UserId.eq(user.UserId)).exists(),
            ),
          ),
      ),
    );
    await context.execute(
      update(user)
        .set({ Version: user.Version.add(1) })
        .from(user)
        .innerJoin(customer),
    );
    const countAfter = scalar(
      await context.query(
        select(aggregate("COUNT", 1).cast(exprTypeInt64))
          .from(user)
          .where(user.Version.eq(maxVersion + 1)),
      ),
    );
    if (countBefore !== countAfter)
      throw new Error(
        `Updated count mismatch: before=${String(countBefore)}, after=${String(countAfter)}.`,
      );
  },
};
