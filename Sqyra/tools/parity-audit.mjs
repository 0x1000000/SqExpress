import { existsSync, readFileSync, readdirSync } from "node:fs";
import { resolve } from "node:path";
import { createHash } from "node:crypto";

const root = resolve(import.meta.dirname, "..");
const requiredFiles = [
  "parity-manifest.json",
  "tools/parity-manifest.schema.json",
  "baseline/source-contract.json",
  "baseline/csharp-tests.json",
  "baseline/csharp-discovered-tests.json",
  "baseline/test-mappings.json",
  "baseline/ported-tests.json",
  "test/fixtures/csharp-reference.json",
  "test/fixtures/csharp-reference.sha256",
];
const errors = requiredFiles
  .filter((path) => !existsSync(resolve(root, path)))
  .map((path) => `required file is missing: ${path}`);
if (errors.length === 0) {
  const manifest = JSON.parse(readFileSync(resolve(root, "parity-manifest.json"), "utf8"));
  if (manifest.schemaVersion !== 1) errors.push("unsupported manifest schemaVersion");
  for (const name of [
    "ast",
    "traversal",
    "descriptors",
    "parser",
    "exporters",
    "builders",
    "documentation",
  ]) {
    const status = manifest.subsystems?.[name]?.status;
    if (!["not-started", "in-progress", "complete"].includes(status))
      errors.push(`invalid or missing subsystem status: ${name}`);
    if (process.argv.includes("--require-complete") && status !== "complete")
      errors.push(`subsystem is not complete: ${name} (${status})`);
  }
  const expectedHash = readFileSync(resolve(root, "test/fixtures/csharp-reference.sha256"), "utf8")
    .trim()
    .split(/\s+/)[0];
  const actualHash = createHash("sha256")
    .update(readFileSync(resolve(root, "test/fixtures/csharp-reference.json")))
    .digest("hex");
  if (actualHash !== expectedHash)
    errors.push("C# reference fixture hash does not match its reviewed checksum");
  const fixtures = JSON.parse(
    readFileSync(resolve(root, "test/fixtures/csharp-reference.json"), "utf8"),
  );
  if (
    !Array.isArray(fixtures) ||
    fixtures.length === 0 ||
    fixtures.some((fixture) => fixture.producer !== "SqExpress.CSharp")
  )
    errors.push("reference fixtures do not carry C# producer provenance");
  const discovered = JSON.parse(
    readFileSync(resolve(root, "baseline/csharp-discovered-tests.json"), "utf8"),
  );
  const mappings = JSON.parse(readFileSync(resolve(root, "baseline/test-mappings.json"), "utf8"));
  const fixtureIds = new Set(fixtures.map((fixture) => fixture.id));
  let mappedTotal = 0;
  let sourceTotal = 0;
  for (const name of ["parser", "traversal", "queryBuilder", "exporter"]) {
    const tests = discovered.groups?.[name]?.tests;
    if (!Array.isArray(tests) || tests.length === 0)
      errors.push(`C# discovered-test group is empty: ${name}`);
    else if (new Set(tests.map((test) => test.id)).size !== tests.length)
      errors.push(`C# discovered-test group has duplicate identities: ${name}`);
    const entries = mappings.groups?.[name]?.tests;
    if (!Array.isArray(entries)) {
      errors.push(`source-test mapping group is missing: ${name}`);
      continue;
    }
    const sourceIds = new Set(tests.map((test) => test.id));
    const mappedIds = entries.map((entry) => entry.sourceId);
    if (new Set(mappedIds).size !== mappedIds.length)
      errors.push(`source-test mapping group has duplicate identities: ${name}`);
    for (const id of sourceIds)
      if (!mappedIds.includes(id)) errors.push(`source test has no mapping entry: ${id}`);
    for (const entry of entries) {
      if (!sourceIds.has(entry.sourceId))
        errors.push(`mapping references unknown source test: ${entry.sourceId}`);
      if (!["mapped", "pending"].includes(entry.status))
        errors.push(`invalid mapping status for ${entry.sourceId}`);
      if (
        entry.status === "mapped" &&
        entry.fixtureId !== undefined &&
        (entry.fixtureId !== entry.sourceId || !fixtureIds.has(entry.fixtureId))
      )
        errors.push(`mapped source test has no matching C# fixture: ${entry.sourceId}`);
      if (entry.status === "mapped" && entry.port !== undefined) {
        const path = resolve(root, entry.port.testFile ?? "");
        if (!existsSync(path)) errors.push(`ported test file is missing: ${entry.sourceId}`);
        else if (!readFileSync(path, "utf8").includes(`it("${entry.port.testName}"`))
          errors.push(`ported test name is missing: ${entry.sourceId}`);
      }
      if (entry.status === "mapped" && entry.fixtureId === undefined && entry.port === undefined)
        errors.push(`mapped source test has no fixture or TypeScript port: ${entry.sourceId}`);
      if (process.argv.includes("--require-complete") && entry.status !== "mapped")
        errors.push(`source test is not mapped: ${entry.sourceId}`);
    }
    mappedTotal += entries.filter((entry) => entry.status === "mapped").length;
    sourceTotal += entries.length;
  }
  if (errors.length === 0) console.log(`Source test mappings: ${mappedTotal}/${sourceTotal}.`);
}
function filesBelow(path) {
  if (!existsSync(path)) return [];
  return readdirSync(path, { withFileTypes: true }).flatMap((entry) =>
    entry.isDirectory() ? filesBelow(resolve(path, entry.name)) : [resolve(path, entry.name)],
  );
}
for (const path of filesBelow(resolve(root, "test"))) {
  if (!/\.[cm]?tsx?$/.test(path)) continue;
  const source = readFileSync(path, "utf8");
  if (
    /\b(?:it|test|describe)\.(?:skip|todo|only)\s*\(/.test(source) ||
    /\b(?:xit|xtest|xdescribe)\s*\(/.test(source)
  )
    errors.push(`forbidden skipped/focused test marker: ${path}`);
}
if (errors.length) {
  console.error(`Parity audit failed:\n${errors.map((error) => `- ${error}`).join("\n")}`);
  process.exit(1);
}
console.log("Parity manifest foundations and test policy are valid.");
