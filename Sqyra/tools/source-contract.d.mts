export interface SourceTestIdentity { readonly id: string; readonly method: string | null; readonly source: "Test" | "TestCase" | "TestCaseSource" | "SetName"; }
export interface SourceContractFile { readonly path: string; readonly sha256: string; readonly tests?: readonly SourceTestIdentity[]; }
export interface SourceContract { readonly schemaVersion: number; readonly sourceCommit: string; readonly relevantWorkingTreeChanges: readonly string[]; readonly roots: readonly string[]; readonly files: readonly SourceContractFile[]; readonly totals: { readonly files: number; readonly tests: number; readonly parserTestFiles: number; readonly parserTests: number }; }
export function createContract(): SourceContract;
export function stableJson(value: unknown): string;
export function checkContract(expected: SourceContract, actual: SourceContract): string[];
