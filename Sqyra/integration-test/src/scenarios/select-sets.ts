import { column, select, sqlType, values } from "sqyra";
import { defineIntegrationTables } from "../tables.js";
import type { CanonicalRow } from "../types.js";
import type { Scenario } from "./types.js";

export const selectSetsScenario: Scenario = {
  source: "ScSelectSets",
  async run(context) {
    const { user, company } = defineIntegrationTables(context.dialect);
    const userQuery = select(
      { Name: user.FirstName.concat("-").concat(user.LastName) },
      { top: 2 },
    ).from(user);
    const companyQuery = select({ Name: company.CompanyName }, { top: 2 }).from(company);
    let unionRows: ReadonlyArray<CanonicalRow>;
    if (context.dialect === "sqlite") {
      const users = await context.query(userQuery);
      const companies = await context.query(companyQuery);
      const left = valueSet(
        users.map((row) => String(row.Name)),
        "US",
      );
      const right = valueSet(
        companies.map((row) => String(row.Name)),
        "CP",
      );
      unionRows = await context.query(
        select(left.Name).from(left).union(select(right.Name).from(right)),
      );
    } else unionRows = await context.query(userQuery.union(companyQuery));
    const unionResult = unionRows.map((row) => String(row.Name));
    if (unionResult.length < 2) throw new Error("UNION returned too few values.");
    const removed = unionResult.filter((_, index) => index % 2 === 0);
    const excluded = valueSet(removed, "EX");
    let exceptRows: ReadonlyArray<CanonicalRow>;
    if (context.dialect === "sqlite") {
      const materialized = valueSet(unionResult, "UN");
      exceptRows = await context.query(
        select(materialized.Name).from(materialized).except(select(excluded.Name).from(excluded)),
      );
    } else
      exceptRows = await context.query(
        userQuery.union(companyQuery).except(select(excluded.Name).from(excluded)),
      );
    const actual = exceptRows.map((row) => String(row.Name));
    const expected = unionResult.filter((_, index) => index % 2 !== 0);
    if (
      actual.length !== expected.length ||
      actual.some((value, index) => value !== expected[index])
    )
      throw new Error(`UNION/EXCEPT mismatch: ${actual.join("|")} != ${expected.join("|")}`);
  },
};
function valueSet(items: ReadonlyArray<string>, alias: string) {
  if (items.length === 0) throw new Error("A set-operation fixture cannot be empty.");
  return values(
    items.map((item) => [item]),
    alias,
    { Name: column(sqlType.string()) },
  );
}
