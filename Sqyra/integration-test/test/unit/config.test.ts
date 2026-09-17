import { describe, expect, it } from "vitest";
import { parseRunnerConfig } from "../../src/config.js";

describe("integration runner configuration", () => {
  it("defaults to the complete C# dialect and parameterization matrix", () => {
    expect(parseRunnerConfig([])).toEqual({
      dialects: ["tsql", "pgsql", "mysql-oracle", "mariadb", "sqlite"],
      parameterizations: ["none", "literal-fallback", "throw-on-limit"],
      scenarios: null,
    });
  });
  it("parses explicit selections and rejects unknown values", () => {
    expect(
      parseRunnerConfig([
        "--dialects",
        "sqlite,pgsql",
        "--parametrization",
        "none",
        "--scenarios",
        "ScBitwise",
      ]).scenarios,
    ).toEqual(["ScBitwise"]);
    expect(() => parseRunnerConfig(["--dialects", "oracle"])).toThrow("Unknown dialect 'oracle'");
  });
  it("rejects selections and option typos that would silently run no coverage", () => {
    for (const option of ["--dialects", "--parametrization", "--scenarios"])
      expect(() => parseRunnerConfig([option, ",,"])).toThrow("Empty");
    expect(() => parseRunnerConfig(["--dialect", "sqlite"])).toThrow("Unknown option");
    expect(() => parseRunnerConfig(["--dialects", "sqlite", "--dialects", "pgsql"])).toThrow(
      "Duplicate option",
    );
    expect(() => parseRunnerConfig(["--dialects"])).toThrow("Missing value");
  });
});
