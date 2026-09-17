import type { IntegrationDialect, IntegrationParameterization } from "./types.js";

export interface RunnerConfig {
  readonly dialects: ReadonlyArray<IntegrationDialect>;
  readonly parameterizations: ReadonlyArray<IntegrationParameterization>;
  readonly scenarios: ReadonlyArray<string> | null;
}
const allDialects: ReadonlyArray<IntegrationDialect> = [
  "tsql",
  "pgsql",
  "mysql-oracle",
  "mariadb",
  "sqlite",
];
const allParameterizations: ReadonlyArray<IntegrationParameterization> = [
  "none",
  "literal-fallback",
  "throw-on-limit",
];

function option(args: ReadonlyArray<string>, name: string): string | undefined {
  const index = args.indexOf(name);
  if (index < 0) return undefined;
  const value = args[index + 1];
  if (value === undefined || value.startsWith("--"))
    throw new TypeError(`Missing value for '${name}'.`);
  return value;
}
function selection<const T extends string>(
  raw: string | undefined,
  allowed: ReadonlyArray<T>,
  name: string,
): ReadonlyArray<T> {
  if (raw === undefined || raw === "all") return allowed;
  const result = raw
    .split(",")
    .map((value) => value.trim())
    .filter(Boolean);
  if (result.length === 0) throw new TypeError(`Empty ${name} selection.`);
  for (const value of result)
    if (!allowed.includes(value as T)) throw new TypeError(`Unknown ${name} '${value}'.`);
  return Object.freeze([...new Set(result as T[])]);
}
export function parseRunnerConfig(args: ReadonlyArray<string>): RunnerConfig {
  const allowedOptions = new Set(["--dialects", "--parametrization", "--scenarios"]);
  const seen = new Set<string>();
  for (let index = 0; index < args.length; index += 2) {
    const key = args[index];
    if (key === undefined || !allowedOptions.has(key))
      throw new TypeError(`Unknown option '${key}'.`);
    if (seen.has(key)) throw new TypeError(`Duplicate option '${key}'.`);
    seen.add(key);
    option(args, key);
  }
  const scenarios = option(args, "--scenarios");
  if (scenarios !== undefined && scenarios.split(",").every((value) => value.trim() === ""))
    throw new TypeError("Empty scenario selection.");
  return Object.freeze({
    dialects: selection(option(args, "--dialects"), allDialects, "dialect"),
    parameterizations: selection(
      option(args, "--parametrization"),
      allParameterizations,
      "parameterization",
    ),
    scenarios:
      scenarios === undefined
        ? null
        : Object.freeze(
            scenarios
              .split(",")
              .map((value) => value.trim())
              .filter(Boolean),
          ),
  });
}
