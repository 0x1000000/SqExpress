import { exerciseTransactions } from "./transactions-shared.js";
import type { Scenario } from "./types.js";
export const transactionsAsyncScenario: Scenario = {
  source: "ScTransactionsAsync",
  async run(context) {
    await exerciseTransactions(context, false);
    await exerciseTransactions(context, true);
  },
};
