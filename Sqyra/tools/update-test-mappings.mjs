import { readFileSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";

const root = resolve(import.meta.dirname, "..");
const discovered = JSON.parse(readFileSync(resolve(root, "baseline/csharp-discovered-tests.json"), "utf8"));
const fixtureCases = JSON.parse(readFileSync(resolve(root, "test/fixtures/cases.json"), "utf8"));
const fixtureIds = new Set(fixtureCases.map((item) => item.id));
const ports = JSON.parse(readFileSync(resolve(root, "baseline/ported-tests.json"), "utf8")).ports;
const portBySource = new Map(ports.map((port) => [port.sourceId, port]));
const groups = Object.fromEntries(Object.entries(discovered.groups).map(([name, group]) => [name, {
  tests: group.tests.map((test) => fixtureIds.has(test.id)
    ? { sourceId: test.id, status: "mapped", fixtureId: test.id }
    : portBySource.has(test.id) ? { sourceId: test.id, status: "mapped", port: portBySource.get(test.id) }
    : { sourceId: test.id, status: "pending" })
}]));
writeFileSync(resolve(root, "baseline/test-mappings.json"), `${JSON.stringify({ schemaVersion: 1, groups }, null, 2)}\n`);
console.log("Updated source-test mapping ledger.");
