import { exerciseTransactions } from "./transactions-shared.js";
import type { Scenario } from "./types.js";
export const transactionsScenario: Scenario = {
  source: "ScTransactions",
  async run(context) {
    await exerciseTransactions(context, false);
    await exerciseTransactions(context, true);
  },
};
