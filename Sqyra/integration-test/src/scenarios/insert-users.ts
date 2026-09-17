import {
  aggregate,
  dateTimeValue,
  exprDateTimeLiteral,
  exprTypeInt64,
  insertInto,
  select,
} from "sqyra";
import { scalar } from "../context.js";
import { defineIntegrationTables } from "../tables.js";
import { chunks, readUsers } from "../test-data.js";
import type { Scenario } from "./types.js";

export const insertUsersScenario: Scenario = {
  source: "ScInsertUserData",
  async run(context) {
    const users = await readUsers();
    const { user, customer } = defineIntegrationTables(context.dialect);
    const now = exprDateTimeLiteral({
      value: dateTimeValue(new Date().toISOString().replace("Z", "")),
    });
    for (const batch of chunks(users, context.dialect === "tsql" ? 200 : 1000)) {
      const rows = batch.map((item) => ({
        ExternalId: item.external_id,
        FirstName: item.first_name,
        LastName: item.last_name,
        Email: item.email,
        RegDate: now,
        Version: 1,
        Created: now,
        Modified: now,
      }));
      const [first, ...rest] = rows;
      if (first === undefined) continue;
      const insertion = insertInto(user)
        .columns(
          "ExternalId",
          "FirstName",
          "LastName",
          "Email",
          "RegDate",
          "Version",
          "Created",
          "Modified",
        )
        .values(first, ...rest);
      if (context.dialect === "mysql-oracle") await context.execute(insertion);
      else {
        const output = await context.query(
          insertion.output(user.UserId, user.FirstName, user.LastName),
        );
        if (output.length !== batch.length) throw new Error("INSERT OUTPUT lost user rows.");
        const actualNames = output
          .map((row) => `${String(row.FirstName)} ${String(row.LastName)}`)
          .sort();
        const expectedNames = batch.map((row) => `${row.first_name} ${row.last_name}`).sort();
        if (
          actualNames.some((name, index) => name !== expectedNames[index]) ||
          output.some((row) => Number(row.UserId) <= 0)
        )
          throw new Error("INSERT OUTPUT changed inserted user values.");
      }
    }
    const count = scalar(
      await context.query(select(aggregate("COUNT", 1).cast(exprTypeInt64)).from(user)),
    );
    if (count !== 1000 && count !== 1000n)
      throw new Error(`Expected 1000 inserted users, received ${String(count)}.`);
    if (context.dialect === "mysql-oracle") {
      const [firstId, ...remainingIds] = users.map((item) => item.external_id);
      if (firstId === undefined) throw new Error("Source user fixture must not be empty.");
      const ids = await context.query(
        select(user.UserId, user.FirstName, user.LastName)
          .from(user)
          .where(user.ExternalId.inList(firstId, ...remainingIds))
          .orderBy(user.UserId),
      );
      if (ids.length !== users.length)
        throw new Error("Oracle MySQL inserted-user lookup lost rows.");
    }
    await context.execute(
      insertInto(customer).from(
        select(user.UserId)
          .from(user)
          .where(select(1).from(customer).where(customer.UserId.eq(user.UserId)).exists().not()),
        "UserId",
      ),
    );
    const customerCount = scalar(
      await context.query(select(aggregate("COUNT", 1).cast(exprTypeInt64)).from(customer)),
    );
    if (customerCount !== 1000 && customerCount !== 1000n)
      throw new Error(`Expected 1000 user customers, received ${String(customerCount)}.`);
    const sample = await context.query(
      select(
        {
          CustomerId: customer.CustomerId,
          UserId: user.UserId,
          Count: aggregate("COUNT", 1).over().cast(exprTypeInt64),
        },
        { distinct: true },
      )
        .from(customer)
        .innerJoin(user)
        .orderBy(user.UserId)
        .offsetFetch(0, 5),
    );
    if (sample.length !== 5 || sample.some((row) => Number(row.Count) !== 1000))
      throw new Error("DISTINCT customer/window count query lost rows or totals.");
  },
};
