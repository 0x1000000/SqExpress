import { describe, expect, it } from "vitest";
import { checkContract, stableJson, type SourceContract } from "../../tools/source-contract.mjs";

const contract: SourceContract = { schemaVersion: 1, sourceCommit: "abc", relevantWorkingTreeChanges: [], roots: ["source"], files: [{ path: "source/A.cs", sha256: "one", tests: [{ id: "A.Test", method: "Test", source: "Test" }] }], totals: { files: 1, tests: 1, parserTestFiles: 1, parserTests: 1 } };

describe("source contract verifier", () => {
  it("accepts an identical contract", () => expect(checkContract(contract, JSON.parse(stableJson(contract)) as SourceContract)).toEqual([]));
  it("detects deleted mappings", () => expect(checkContract(contract, { ...contract, files: [] })).toContain("source file removed: source/A.cs"));
  it("detects altered hashes", () => expect(checkContract(contract, { ...contract, files: [{ ...contract.files[0]!, sha256: "two" }] })).toContain("source hash changed: source/A.cs"));
  it("detects changed tests", () => expect(checkContract(contract, { ...contract, files: [{ ...contract.files[0]!, tests: [] }] })).toContain("source tests changed: source/A.cs"));
});
