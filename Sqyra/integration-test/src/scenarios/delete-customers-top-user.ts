import { deleteFrom, select } from "sqyra";
import { defineIntegrationTables } from "../tables.js";
import type { Scenario } from "./types.js";

export const deleteCustomersTopUserScenario: Scenario = {
  source: "ScDeleteCustomersByTopUser",
  async run(context) {
    const { user, customer } = defineIntegrationTables(context.dialect);
    const topUsers = select({ UserId: user.UserId })
      .from(user)
      .orderBy(user.FirstName)
      .offsetFetch(0, 5);
    const statement =
      context.dialect === "sqlite"
        ? deleteFrom(customer).where(customer.UserId.inQuery(topUsers))
        : deleteFrom(customer)
            .from(customer)
            .innerJoin(topUsers, topUsers.UserId.eq(customer.UserId))
            .done();
    const result = await context.execute(statement.ast);
    if (result.affectedRows !== 5)
      throw new Error(`Expected five deleted customers, received ${result.affectedRows}.`);
    const rows = await context.query(
      select(user.FirstName, user.LastName)
        .from(user)
        .where(select(1).from(customer).where(customer.UserId.eq(user.UserId)).exists().not())
        .orderBy(user.FirstName),
    );
    if (rows.length !== 5)
      throw new Error(`Expected five users without customers, received ${rows.length}.`);
  },
};
