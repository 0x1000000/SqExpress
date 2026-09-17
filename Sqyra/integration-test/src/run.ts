import { createAdapter } from "./adapters/index.js";
import { parseRunnerConfig } from "./config.js";
import { createContext } from "./context.js";
import { scenarios } from "./scenarios/registry.js";
import { readFileSync } from "node:fs";

interface ScenarioApplicability {
  readonly name: string;
  readonly applicableDialects: ReadonlyArray<string>;
  readonly applicableParameterizations: ReadonlyArray<string>;
  readonly restrictionSource: string | null;
}
const inventory: unknown = JSON.parse(
  readFileSync(new URL("../scenario-inventory.json", import.meta.url), "utf8"),
);
function isApplicability(value: unknown): value is ScenarioApplicability {
  return (
    typeof value === "object" &&
    value !== null &&
    "name" in value &&
    typeof value.name === "string" &&
    "applicableDialects" in value &&
    Array.isArray(value.applicableDialects) &&
    value.applicableDialects.every((item) => typeof item === "string") &&
    "applicableParameterizations" in value &&
    Array.isArray(value.applicableParameterizations) &&
    value.applicableParameterizations.every((item) => typeof item === "string") &&
    "restrictionSource" in value &&
    (value.restrictionSource === null || typeof value.restrictionSource === "string")
  );
}
if (
  typeof inventory !== "object" ||
  inventory === null ||
  !("scenarios" in inventory) ||
  !Array.isArray(inventory.scenarios) ||
  !inventory.scenarios.every(isApplicability)
)
  throw new TypeError("Invalid scenario inventory applicability metadata.");
const applicability = new Map(inventory.scenarios.map((item) => [item.name, item]));

const config = parseRunnerConfig(process.argv.slice(2));
const selected =
  config.scenarios === null
    ? scenarios
    : scenarios.filter((scenario) => config.scenarios?.includes(scenario.source));
if (config.scenarios !== null)
  for (const requested of config.scenarios)
    if (!scenarios.some((scenario) => scenario.source === requested))
      throw new TypeError(`Unknown or excluded scenario '${requested}'.`);

for (const parameterization of config.parameterizations) {
  for (const dialect of config.dialects) {
    const database = createAdapter(dialect);
    const label = `${dialect}/${parameterization}`;
    const started = Date.now();
    try {
      try {
        await database.open();
        await database.healthCheck();
      } catch (error) {
        throw new Error(
          `${dialect} database health check failed; verify the configured host, credentials, and service availability. Driver error: ${error instanceof Error ? error.message : String(error)}`,
        );
      }
      const context = createContext(database, parameterization);
      for (const scenario of selected) {
        const item = applicability.get(scenario.source);
        if (item === undefined)
          throw new Error(`No inventory applicability for ${scenario.source}.`);
        if (
          !item.applicableDialects.includes(dialect) ||
          !item.applicableParameterizations.includes(parameterization)
        ) {
          console.log(`NOT APPLICABLE ${label}/${scenario.source} (${item.restrictionSource})`);
          continue;
        }
        const scenarioStarted = Date.now();
        await scenario.run(context);
        console.log(`PASS ${label}/${scenario.source} (${Date.now() - scenarioStarted} ms)`);
      }
      console.log(`PASS ${label} (${Date.now() - started} ms)`);
    } catch (error) {
      console.error(`FAIL ${label}`);
      throw error;
    } finally {
      await database.close();
    }
  }
}
