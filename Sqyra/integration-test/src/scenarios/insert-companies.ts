import {
  toExpr,
  aggregate,
  caseWhen,
  column,
  columnExpression,
  dateTimeValue,
  exprDateTimeLiteral,
  exprGuidLiteral,
  exprIsNull,
  exprTypeGuid,
  exprTypeInt64,
  guidValue,
  insertInto,
  select,
  sqlType,
  values,
} from "sqyra";
import { scalar } from "../context.js";
import { defineIntegrationTables } from "../tables.js";
import { chunks, readCompanies } from "../test-data.js";
import type { Scenario } from "./types.js";

export const insertCompaniesScenario: Scenario = {
  source: "ScInsertCompanies",
  async run(context) {
    const companies = await readCompanies();
    const { company, customer } = defineIntegrationTables(context.dialect);
    const now = exprDateTimeLiteral({
      value: dateTimeValue(new Date().toISOString().replace("Z", "")),
    });
    const customersBefore = Number(
      scalar(await context.query(select(aggregate("COUNT", 1).cast(exprTypeInt64)).from(customer))),
    );
    const insertedIds: number[] = [];
    for (const batch of chunks(companies, context.dialect === "tsql" ? 200 : 1000)) {
      const rows = batch.map((item) => ({
        ExternalId: item.external_id,
        CompanyName: item.name,
        Version: 1,
        Created: now,
        Modified: now,
      }));
      const [first, ...rest] = rows;
      if (first === undefined) continue;
      const insertion = insertInto(company)
        .columns("ExternalId", "CompanyName", "Version", "Created", "Modified")
        .values(first, ...rest);
      if (context.dialect === "mysql-oracle") await context.execute(insertion);
      else {
        const output = await context.query(insertion.output(company.CompanyId));
        if (output.length !== batch.length || output.some((row) => Number(row.CompanyId) <= 0))
          throw new Error("INSERT OUTPUT lost company identities.");
        insertedIds.push(...output.map((row) => Number(row.CompanyId)));
      }
    }
    const count = scalar(
      await context.query(select(aggregate("COUNT", 1).cast(exprTypeInt64)).from(company)),
    );
    if (count !== 150 && count !== 150n)
      throw new Error(`Expected 150 inserted companies, received ${String(count)}.`);
    const beforeDuplicateCount = Number(count);
    if (context.dialect === "mysql-oracle") {
      const [firstId, ...remainingIds] = companies.map((item) => item.external_id);
      if (firstId === undefined) throw new Error("Source company fixture must not be empty.");
      const output = await context.query(
        select(company.CompanyId)
          .from(company)
          .where(company.ExternalId.inList(firstId, ...remainingIds))
          .orderBy(company.CompanyId),
      );
      insertedIds.push(...output.map((row) => Number(row.CompanyId)));
    }
    if (insertedIds.length !== companies.length || new Set(insertedIds).size !== insertedIds.length)
      throw new Error("Inserted company identities are missing or duplicated.");
    if (context.dialect !== "mysql-oracle") {
      const duplicateRows = companies
        .slice(0, 2)
        .map(
          (item) =>
            [
              toExpr(exprGuidLiteral({ value: guidValue(item.external_id) })).cast(exprTypeGuid),
              item.name,
            ] as const,
        );
      const duplicates = values(duplicateRows, "DuplicateCompany", {
        ExternalId: column(sqlType.guid),
        CompanyName: column(sqlType.string(250)),
      });
      const existing = select(1)
        .from(company)
        .where(company.ExternalId.cast(exprTypeGuid).eq(duplicates.ExternalId));
      const duplicateInsert = select({
        ExternalId: duplicates.ExternalId,
        CompanyName: duplicates.CompanyName,
        Version: 1,
        Created: now,
        Modified: now,
      })
        .from(duplicates)
        .where(existing.exists().not());
      const output = await context.query(
        insertInto(company)
          .from(duplicateInsert, "ExternalId", "CompanyName", "Version", "Created", "Modified")
          .output(company.CompanyId),
      );
      if (output.length !== 0)
        throw new Error("Existence-filtered INSERT OUTPUT returned duplicate identities.");
    }
    const afterDuplicateCount = Number(
      scalar(await context.query(select(aggregate("COUNT", 1).cast(exprTypeInt64)).from(company))),
    );
    if (afterDuplicateCount !== beforeDuplicateCount)
      throw new Error("CheckExistenceBy equivalent inserted duplicate companies.");
    const [firstCustomer, ...remainingCustomers] = insertedIds.map((CompanyId) => ({ CompanyId }));
    if (firstCustomer === undefined)
      throw new Error("No inserted company identity is available for customers.");
    await context.execute(
      insertInto(customer)
        .columns("CompanyId")
        .values(firstCustomer, ...remainingCustomers),
    );
    const totalCustomers = scalar(
      await context.query(select(aggregate("COUNT", 1).cast(exprTypeInt64)).from(customer)),
    );
    const expectedCustomers = customersBefore + 150;
    if (totalCustomers !== expectedCustomers && totalCustomers !== BigInt(expectedCustomers))
      throw new Error(
        `Expected ${expectedCustomers} customers after company insertion, received ${String(totalCustomers)}.`,
      );
    const user = defineIntegrationTables(context.dialect).user;
    const userPresent = exprIsNull({ test: columnExpression(user.UserId), not: true });
    const companyPresent = exprIsNull({ test: columnExpression(company.CompanyId), not: true });
    const customerNames = select({
      CustomerId: customer.CustomerId,
      CustomerTypeId: caseWhen(userPresent).then(1).when(companyPresent).then(2).else(0),
      Name: caseWhen(userPresent)
        .then(user.FirstName.concat(" ").concat(user.LastName))
        .when(companyPresent)
        .then(company.CompanyName)
        .else("-"),
    })
      .from(customer)
      .leftJoin(user)
      .leftJoin(company);
    const users = await context.query(
      select({
        Id: customerNames.CustomerId,
        TypeId: customerNames.CustomerTypeId,
        Name: customerNames.Name,
      })
        .from(customerNames)
        .where(customerNames.CustomerTypeId.eq(1))
        .orderBy(customerNames.Name)
        .offsetFetch(0, 5),
    );
    const companyNames = await context.query(
      select({
        Id: customerNames.CustomerId,
        TypeId: customerNames.CustomerTypeId,
        Name: customerNames.Name,
      })
        .from(customerNames)
        .where(customerNames.CustomerTypeId.eq(2))
        .orderBy(customerNames.CustomerId)
        .offsetFetch(0, 5),
    );
    if (
      users.length !== 5 ||
      users.some((row) => Number(row.TypeId) !== 1 || typeof row.Name !== "string")
    )
      throw new Error("Derived CustomerName query did not return five user models.");
    if (
      companyNames.length !== 5 ||
      companyNames.some((row) => Number(row.TypeId) !== 2 || typeof row.Name !== "string")
    )
      throw new Error("Derived CustomerName query did not return five company models.");
  },
};
