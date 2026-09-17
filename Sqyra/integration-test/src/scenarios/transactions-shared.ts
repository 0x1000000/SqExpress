import { dateTimeValue, deleteFrom, exprDateTimeLiteral, insertInto, select } from "sqyra";
import { defineIntegrationTables } from "../tables.js";
import type { DatabaseAdapter, ScenarioContext } from "../types.js";

const externalId = "58ad8253-4f8f-4c84-b930-4f58a8f25912";
const activeTransactions = new WeakSet<DatabaseAdapter>();
async function transactionOrExisting<T>(
  database: DatabaseAdapter,
  action: (transaction: DatabaseAdapter, newTransaction: boolean) => Promise<T>,
): Promise<T> {
  if (activeTransactions.has(database)) return action(database, false);
  return database.transaction(async (transaction) => {
    activeTransactions.add(transaction);
    try {
      return await action(transaction, true);
    } finally {
      activeTransactions.delete(transaction);
    }
  });
}
export async function exerciseTransactions(
  context: ScenarioContext,
  useSibling: boolean,
): Promise<void> {
  const database = useSibling ? await context.database.openSibling() : context.database;
  try {
    const { company } = defineIntegrationTables(context.dialect);
    const now = exprDateTimeLiteral({
      value: dateTimeValue(new Date().toISOString().replace("Z", "")),
    });
    const insertion = insertInto(company)
      .columns("ExternalId", "CompanyName", "Modified", "Created", "Version")
      .values({
        ExternalId: externalId,
        CompanyName: "TestCompany",
        Modified: now,
        Created: now,
        Version: 1,
      });
    const deletion = deleteFrom(company).where(company.ExternalId.eq(externalId));
    await database.execute(context.compile(deletion));
    const rollback = new Error("intentional rollback");
    try {
      await transactionOrExisting(database, async (tx, newTransaction) => {
        if (!newTransaction) throw new Error("A new outer transaction should have been started.");
        await transactionOrExisting(tx, async (nested, nestedIsNew) => {
          if (nestedIsNew) throw new Error("The existing transaction should have been reused.");
          await nested.execute(context.compile(insertion));
          // A reused scope owns neither commit nor rollback; disposal of the outer
          // scope below is what rolls the physical transaction back.
        });
        if (!(await exists(tx, context, company)))
          throw new Error("Inserted data is not visible inside its transaction.");
        throw rollback;
      });
    } catch (error) {
      if (error !== rollback) throw error;
    }
    if (await exists(database, context, company))
      throw new Error("Rolled-back data remained visible.");
    await database.transaction(async (tx) => {
      await tx.execute(context.compile(insertion));
    });
    if (!(await exists(database, context, company)))
      throw new Error("Committed data is not visible.");
    const serializable = await database.beginTransaction("serializable");
    try {
      await serializable.database.execute(context.compile(deletion));
      await serializable.commit();
    } catch (error) {
      await serializable.rollback();
      throw error;
    }
    if (await exists(database, context, company))
      throw new Error("Committed deletion did not remove the row.");
  } finally {
    if (useSibling) await database.close();
  }
}
async function exists(
  database: DatabaseAdapter,
  context: ScenarioContext,
  company: ReturnType<typeof defineIntegrationTables>["company"],
): Promise<boolean> {
  return (
    (
      await database.execute(
        context.compile(select({ Col: 1 }).from(company).where(company.ExternalId.eq(externalId))),
      )
    ).rows.length > 0
  );
}
