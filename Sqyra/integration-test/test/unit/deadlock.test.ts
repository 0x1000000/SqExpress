import { expect, it } from "vitest";
import { isDeadlock } from "../../src/scenarios/transactions-deadlock.js";

it("accepts provider deadlock diagnostics without treating arbitrary SQL failures as success", () => {
  expect(isDeadlock({ code: "40P01" })).toBe(true);
  expect(isDeadlock({ code: "ER_LOCK_DEADLOCK" })).toBe(true);
  expect(isDeadlock({ number: 1205 })).toBe(true);
  expect(isDeadlock(new Error("Transaction was deadlocked on lock resources"))).toBe(true);
  expect(isDeadlock(new Error("Invalid column name Version"))).toBe(false);
  expect(isDeadlock({ code: "ECONNRESET" })).toBe(false);
});
