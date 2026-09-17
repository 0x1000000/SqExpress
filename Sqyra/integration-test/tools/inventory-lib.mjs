import { createHash } from "node:crypto";
import { readFileSync, readdirSync } from "node:fs";
import { resolve } from "node:path";

export const projectRoot = resolve(import.meta.dirname, "..");
export const sourceRoot = resolve(projectRoot, "..", "..", "Test", "SqExpress.IntTest");
const sha256 = (value) => createHash("sha256").update(value).digest("hex");
const dialects = ["tsql", "pgsql", "mysql-oracle", "mariadb", "sqlite"];
const parameterizations = ["none", "literal-fallback", "throw-on-limit"];
const restrictions = {
  ScCancellation: { dialects: ["mariadb"], source: "ScCancellation.cs:12-21" },
  ScPgMergeIdentityPolyfill: { dialects: ["pgsql"], source: "ScPgMergeIdentityPolyfill.cs:15-19" },
  ScTreeClosure: {
    dialects: dialects.filter((value) => value !== "mysql-oracle"),
    source: "ScTreeClosure.cs:13-17",
  },
  ScTransactionsDeadlock: {
    dialects: dialects.filter((value) => value !== "sqlite"),
    source: "ScTransactionsDeadlock.cs:16-20",
  },
  ScCreateDynamicTable: {
    dialects: dialects.filter((value) => value !== "mysql-oracle"),
    source: "ScCreateDynamicTable.cs:13-17",
  },
  ScGetTablesComplex: {
    dialects: dialects.filter((value) => value !== "sqlite"),
    source: "ScGetTablesComplex.cs:12-16",
  },
  ScParametrizationLimitBoundary: {
    parameterizations: parameterizations.filter((value) => value !== "none"),
    source: "ScParametrizationLimitBoundary.cs:16-19",
  },
};

export function buildInventory() {
  const statuses = JSON.parse(readFileSync(resolve(projectRoot, "scenario-status.json"), "utf8"));
  const program = readFileSync(resolve(sourceRoot, "Program.cs"), "utf8");
  const chain = program.match(
    /return new ScCreateTables\(\)([\s\S]*?)\.Then\(new ScPortableScalarFunctions\(\)\);/,
  );
  if (chain === null) throw new Error("Cannot find complete BuildScenario chain in C# Program.cs.");
  const defaultChain = [...chain[0].matchAll(/new (Sc[A-Za-z0-9]+)/g)].map((match) => match[1]);
  const order = [...program.matchAll(/(?:new |\.Then\(new )(Sc[A-Za-z0-9]+)/g)].map(
    (match) => match[1],
  );
  const files = readdirSync(resolve(sourceRoot, "Scenarios"), { withFileTypes: true })
    .filter((entry) => entry.isFile() && /^Sc.*\.cs$/.test(entry.name))
    .sort((a, b) => a.name.localeCompare(b.name));
  const scenarios = files.map((entry) => {
    const name = entry.name.slice(0, -3);
    const relativeSource = `Test/SqExpress.IntTest/Scenarios/${entry.name}`;
    const source = readFileSync(resolve(sourceRoot, "Scenarios", entry.name), "utf8");
    const mapping = statuses[name];
    if (mapping === undefined) throw new Error(`Missing status for ${name}.`);
    const restriction = restrictions[name];
    return {
      name,
      source: relativeSource,
      sha256: sha256(source),
      order: order.flatMap((item, index) => (item === name ? [index] : [])),
      defaultChainOrder: defaultChain.flatMap((item, index) => (item === name ? [index] : [])),
      applicableDialects: restriction?.dialects ?? dialects,
      applicableParameterizations: restriction?.parameterizations ?? parameterizations,
      restrictionSource: restriction?.source ?? null,
      dialectReferences: [
        ...new Set([...source.matchAll(/SqlDialect\.([A-Za-z0-9]+)/g)].map((match) => match[1])),
      ].sort(),
      ...mapping,
    };
  });
  for (const name of Object.keys(statuses))
    if (!scenarios.some((item) => item.name === name))
      throw new Error(`Status references missing source scenario ${name}.`);
  const testData = ["users.json", "company.json"].map((name) => {
    const source = readFileSync(resolve(sourceRoot, "TestData", name));
    const copy = readFileSync(resolve(projectRoot, "data", name));
    return {
      source: `Test/SqExpress.IntTest/TestData/${name}`,
      copy: `Sqyra/integration-test/data/${name}`,
      sourceSha256: sha256(source),
      copySha256: sha256(copy),
    };
  });
  return {
    schemaVersion: 3,
    sourceProgram: { path: "Test/SqExpress.IntTest/Program.cs", sha256: sha256(program) },
    parameterizations,
    dialects,
    defaultChain,
    testData,
    scenarios,
  };
}
