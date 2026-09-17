import { select } from "sqyra";
import { scalar } from "../context.js";
import { defineIntegrationTables } from "../tables.js";
import type { Scenario } from "./types.js";

export const selectValueScenario: Scenario = {
  source: "ScSelectValue",
  async run(context) {
    const { user } = defineIntegrationTables(context.dialect);
    const sub = user("sub");
    const userId = Number(scalar(await context.query(select(user.UserId, { top: 1 }).from(user))));
    const email = await context.query(
      select(user.UserId, user.Email)
        .from(user)
        .where(
          user.UserId.eq(
            select(sub.UserId).from(sub).where(sub.UserId.eq(userId)).scalarSubquery(),
          ),
        ),
    );
    if (Number(email[0]?.UserId) !== userId)
      throw new Error("Scalar-subquery filtering returned an incorrect user.");
    const projected = await context.query(
      select({
        v: select(sub.UserId).from(sub).where(sub.UserId.eq(userId)).scalarSubquery(),
        UserId: user.UserId,
      })
        .from(user)
        .where(user.UserId.eq(userId)),
    );
    if (Number(projected[0]?.v) !== Number(projected[0]?.UserId))
      throw new Error("Projected scalar subquery differs from the user id.");
    const ids = (
      await context.query(
        select(user.UserId)
          .from(user)
          .where(
            user.UserId.inQuery(
              select(sub.UserId)
                .from(sub)
                .where(sub.UserId.eq(1).or(sub.UserId.eq(2))),
            ),
          ),
      )
    )
      .map((row) => Number(row.UserId))
      .sort();
    if (ids.length !== 2 || ids[0] !== 1 || ids[1] !== 2)
      throw new Error(`Expected user ids 1 and 2, received ${ids.join(",")}.`);
  },
};
