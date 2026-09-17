import { call, select } from "sqyra";
import type { Scenario } from "./types.js";

export const cancellationScenario: Scenario = {
  source: "ScCancellation",
  async run(context) {
    if (context.dialect !== "mariadb") return;
    const controller = new AbortController();
    const query = select(call("SLEEP", 1000));
    const timer = setTimeout(
      () => controller.abort(new DOMException("Operation cancelled after 100 ms.", "AbortError")),
      100,
    );
    let cancellation: unknown;
    try {
      await context.database.execute(context.compile(query), { signal: controller.signal });
    } catch (error) {
      cancellation = error;
    } finally {
      clearTimeout(timer);
    }
    if (!(cancellation instanceof DOMException) || cancellation.name !== "AbortError")
      throw new Error(
        `Expected an AbortError for the source MariaDB cancellation scenario, received ${String(cancellation)}.`,
      );
    await context.database.healthCheck();
  },
};
