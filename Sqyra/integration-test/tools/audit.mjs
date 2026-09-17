import { execFileSync } from "node:child_process";
import { existsSync, readFileSync, readdirSync } from "node:fs";
import { dirname, relative, resolve } from "node:path";
import { buildInventory, projectRoot } from "./inventory-lib.mjs";

const errors = [];
const expected = `${JSON.stringify(buildInventory(), null, 2)}\n`;
const inventoryPath = resolve(projectRoot, "scenario-inventory.json");
if (!existsSync(inventoryPath) || readFileSync(inventoryPath, "utf8") !== expected)
  errors.push("scenario-inventory.json is stale; run npm run inventory");
const inventory = JSON.parse(expected);
const requireComplete = process.argv.includes("--require-complete");
if (requireComplete) {
  const review = readFileSync(resolve(projectRoot, "SOURCE_REVIEW.md"), "utf8");
  for (const item of inventory.scenarios)
    if (!new RegExp(`^\\|\\s*${item.name}\\s*\\|`, "m").test(review))
      errors.push(`${item.name}: direct C# assertion review is missing`);
}
const packageJson = JSON.parse(readFileSync(resolve(projectRoot, "package.json"), "utf8"));
if (packageJson.private !== true || packageJson.dependencies?.sqyra !== "file:..")
  errors.push("integration package must remain private and use Sqyra's packaged file:.. API");
const parentPackage = JSON.parse(readFileSync(resolve(projectRoot, "..", "package.json"), "utf8"));
for (const driver of ["mssql", "msnodesqlv8", "pg", "mysql2", "better-sqlite3"])
  if (
    parentPackage.dependencies?.[driver] !== undefined ||
    parentPackage.optionalDependencies?.[driver] !== undefined
  )
    errors.push(`database driver ${driver} leaked into Sqyra runtime dependencies`);
const registrySource = readFileSync(resolve(projectRoot, "src/scenarios/registry.ts"), "utf8");
const registryList = registrySource.match(/Object\.freeze\(\[([\s\S]*?)\]\)/)?.[1];
if (registryList === undefined) errors.push("scenario registry has no static ordered list");
else {
  const imports = new Map(
    [...registrySource.matchAll(/import \{ (\w+Scenario) \} from "(\.\/[^"]+)";/g)].map((match) => [
      match[1],
      match[2],
    ]),
  );
  const selected = [...registryList.matchAll(/\b(\w+Scenario)\b/g)].map((match) => match[1]);
  const registered = selected
    .map((symbol) => {
      const module = imports.get(symbol);
      if (!module) {
        errors.push(`registry symbol ${symbol} has no import`);
        return null;
      }
      const sourcePath = resolve(projectRoot, "src/scenarios", `${module.slice(2, -3)}.ts`);
      const source = readFileSync(sourcePath, "utf8").match(/source: "(Sc\w+)"/)?.[1];
      if (!source) errors.push(`${module}: no source identity`);
      return source;
    })
    .filter(Boolean);
  const expectedOrder = inventory.defaultChain
    .filter((name) =>
      inventory.scenarios.some((item) => item.name === name && item.status === "ported"),
    )
    .filter((name, index, names) => index === 0 || name !== names[index - 1]);
  if (JSON.stringify(registered) !== JSON.stringify(expectedOrder))
    errors.push("scenario registry order differs from the C# BuildScenario chain");
}
for (const item of inventory.scenarios) {
  if (
    item.applicableDialects.length === 0 ||
    item.applicableDialects.some((value) => !inventory.dialects.includes(value))
  )
    errors.push(`${item.name}: invalid applicable dialect list`);
  if (
    item.applicableParameterizations.length === 0 ||
    item.applicableParameterizations.some((value) => !inventory.parameterizations.includes(value))
  )
    errors.push(`${item.name}: invalid applicable parameterization list`);
  if (item.restrictionSource !== null) {
    const sourceFile = item.restrictionSource.split(":")[0];
    if (!item.source.endsWith(sourceFile))
      errors.push(`${item.name}: restriction does not identify its source file`);
  }
  if (item.status === "ported" && (!item.module || !existsSync(resolve(projectRoot, item.module))))
    errors.push(`${item.name}: ported module is missing`);
  if (
    item.excludedAssertions !== undefined &&
    (!Array.isArray(item.excludedAssertions) ||
      item.excludedAssertions.length === 0 ||
      item.excludedAssertions.some(
        (assertion) => !assertion.source || !assertion.reason || assertion.reason.length < 24,
      ))
  )
    errors.push(
      `${item.name}: excluded assertions need exact source locations and capability reasons`,
    );
  for (const assertion of item.excludedAssertions ?? []) {
    const location = assertion.source.match(/^(Sc\w+\.cs):(\d+)-(\d+)$/);
    if (location === null || !item.source.endsWith(location[1])) {
      errors.push(`${item.name}: excluded assertion location does not identify its C# scenario`);
      continue;
    }
    const lineCount = readFileSync(resolve(projectRoot, "..", "..", item.source), "utf8").split(
      /\r?\n/,
    ).length;
    if (+location[2] < 1 || +location[3] < +location[2] || +location[3] > lineCount)
      errors.push(`${item.name}: excluded assertion lines are outside the C# scenario`);
  }
  if (
    item.status === "partial" &&
    (!item.module || !existsSync(resolve(projectRoot, item.module)) || !item.remaining)
  )
    errors.push(`${item.name}: partial mapping needs a module and remaining-work explanation`);
  if (item.status === "excluded" && (!item.reason || item.reason.length < 24))
    errors.push(`${item.name}: exclusion needs a concrete reason`);
  if (!new Set(["ported", "partial", "pending", "excluded"]).has(item.status))
    errors.push(`${item.name}: unknown mapping status '${item.status}'`);
  if (requireComplete && (item.status === "pending" || item.status === "partial"))
    errors.push(`${item.name}: mapping remains ${item.status}`);
}
for (const item of inventory.testData)
  if (item.sourceSha256 !== item.copySha256)
    errors.push(`${item.copy}: copied test data differs from ${item.source}`);
function filesBelow(path) {
  return readdirSync(path, { withFileTypes: true }).flatMap((entry) =>
    entry.isDirectory() ? filesBelow(resolve(path, entry.name)) : [resolve(path, entry.name)],
  );
}
for (const path of [
  ...filesBelow(resolve(projectRoot, "src")),
  ...filesBelow(resolve(projectRoot, "test")),
])
  if (/\.[cm]?tsx?$/.test(path)) {
    const source = readFileSync(path, "utf8");
    if (
      /\b(?:it|test|describe)\.(?:skip|todo|only)\s*\(|\b(?:xit|xtest|xdescribe)\s*\(/.test(source)
    )
      errors.push(`forbidden skipped/focused test marker: ${path}`);
    for (const match of source.matchAll(/(?:from\s*|import\s*\()["']([^"']+)["']/g)) {
      const specifier = match[1];
      const internalPath = specifier.startsWith(".")
        ? relative(resolve(projectRoot, "..", "src"), resolve(dirname(path), specifier))
        : null;
      if (
        specifier.startsWith("sqyra/") ||
        (internalPath !== null &&
          !internalPath.startsWith("..") &&
          !/^[A-Za-z]:/.test(internalPath))
      )
        errors.push(`integration imports Sqyra internal modules: ${path}: ${specifier}`);
    }
  }
const packed = JSON.parse(
  execFileSync("npm", ["pack", "--dry-run", "--json"], {
    cwd: resolve(projectRoot, ".."),
    encoding: "utf8",
    shell: process.platform === "win32",
    env: { ...process.env, npm_config_cache: resolve(projectRoot, ".npm-cache") },
  }),
);
const leaked = packed[0]?.files?.filter((file) => file.path.startsWith("integration-test/")) ?? [];
if (leaked.length > 0)
  errors.push(
    `integration project leaked into package: ${leaked.map((file) => file.path).join(", ")}`,
  );
if (errors.length > 0) {
  console.error(errors.map((error) => `- ${error}`).join("\n"));
  process.exit(1);
}
const ported = inventory.scenarios.filter((item) => item.status === "ported").length;
const partial = inventory.scenarios.filter((item) => item.status === "partial").length;
const excluded = inventory.scenarios.filter((item) => item.status === "excluded").length;
const pending = inventory.scenarios.length - ported - partial - excluded;
console.log(
  `Integration mappings: ${ported} ported, ${partial} partial, ${excluded} excluded, ${pending} pending; package isolation passed.`,
);
