import { select } from "sqyra";
import { defineIntegrationTables } from "../tables.js";
import type { Scenario } from "./types.js";

export const selectPrefixedModelsScenario: Scenario = {
  source: "ScSelectSeveralModelsWithPrefix",
  async run(context) {
    const { customer, user, company } = defineIntegrationTables(context.dialect);
    const rows = await context.query(
      select({
        cstCustomerId: customer.CustomerId,
        cstUserId: customer.UserId,
        cstCompanyId: customer.CompanyId,
        usrUserId: user.UserId,
        usrFirstName: user.FirstName,
        usrLastName: user.LastName,
        compCompanyId: company.CompanyId,
        compCompanyName: company.CompanyName,
      })
        .from(customer)
        .leftJoin(user)
        .leftJoin(company),
    );
    if (rows.length !== 1145)
      throw new Error(
        `Expected 1145 combined customer models after deleting five users, received ${rows.length}.`,
      );
    for (const row of rows)
      if ((row.usrUserId === null) === (row.compCompanyId === null))
        throw new Error("A customer row must resolve to exactly one user or company model.");
  },
};
