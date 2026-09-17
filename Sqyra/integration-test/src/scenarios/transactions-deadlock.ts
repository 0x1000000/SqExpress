import { update } from "sqyra";
import { defineIntegrationTables } from "../tables.js";
import type { DatabaseTransaction } from "../types.js";
import type { Scenario } from "./types.js";

const duplicateTransactionMessage =
  "There is an already running transaction associated with this connection";

export const transactionsDeadlockScenario: Scenario = {
  source: "ScTransactionsDeadlock",
  async run(context) {
    // This is the same deliberate source exclusion: SQLite serializes writers
    // rather than detecting a server-side two-resource deadlock.
    if (context.dialect === "sqlite") return;

    const connection1 = await context.database.openSibling();
    const connection2 = await context.database.openSibling();
    let transaction1: DatabaseTransaction | null = null;
    let transaction2: DatabaseTransaction | null = null;
    try {
      transaction1 = await beginAndAssertDuplicate(connection1);
      transaction2 = await beginAndAssertDuplicate(connection2);
      const { company, user } = defineIntegrationTables(context.dialect);
      const companyUpdate = context.compile(
        update(company).set({ Version: company.Version.add(1) }),
      );
      const userUpdate = context.compile(update(user).set({ Version: user.Version.add(1) }));

      await transaction1.database.execute(companyUpdate);
      await transaction2.database.execute(userUpdate);
      const crossed = await Promise.allSettled([
        transaction1.database.execute(userUpdate),
        transaction2.database.execute(companyUpdate),
      ]);
      const failures = crossed.filter((result) => result.status === "rejected");
      if (failures.length === 0) throw new Error("Deadlock exception was expected.");
      for (const failure of failures) if (!isDeadlock(failure.reason)) throw failure.reason;
    } finally {
      await rollbackQuietly(transaction1);
      await rollbackQuietly(transaction2);
      await connection1.close();
      await connection2.close();
    }
  },
};

export function isDeadlock(error: unknown): boolean {
  if (typeof error !== "object" || error === null) return false;
  if ("code" in error && (error.code === "40P01" || error.code === "ER_LOCK_DEADLOCK")) return true;
  if ("number" in error && error.number === 1205) return true;
  // mssql wraps the native ODBC error; preserve its explicit deadlock diagnostic.
  return error instanceof Error && /\bdeadlock(?:ed)?\b/i.test(error.message);
}

async function beginAndAssertDuplicate(
  database: import("../types.js").DatabaseAdapter,
): Promise<DatabaseTransaction> {
  const transaction = await database.beginTransaction();
  let message = "";
  try {
    await transaction.database.beginTransaction();
  } catch (error) {
    message = error instanceof Error ? error.message : String(error);
  }
  if (message !== duplicateTransactionMessage) {
    await rollbackQuietly(transaction);
    throw new Error(
      `Expected duplicate-transaction error '${duplicateTransactionMessage}', received '${message}'.`,
    );
  }
  return transaction;
}

async function rollbackQuietly(transaction: DatabaseTransaction | null): Promise<void> {
  if (transaction === null) return;
  try {
    await transaction.rollback();
  } catch {
    /* A server may already have rolled back its deadlock victim. */
  }
}
