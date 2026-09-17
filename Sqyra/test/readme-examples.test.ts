import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import ts from "typescript";
import { describe, expect, it } from "vitest";

describe("README examples", () => {
  const readme = readFileSync(resolve(import.meta.dirname, "../Readme.md"), "utf8");
  const examples = [...readme.matchAll(/```ts\s+([\s\S]*?)```/g)].map((match) => match[1]!);
  it("contains executable TypeScript examples", () => expect(examples.length).toBeGreaterThan(0));
  it.each(examples.map((source, index) => [index + 1, source] as const))(
    "transpiles TypeScript example %i",
    (_, source) => {
      const result = ts.transpileModule(source, {
        compilerOptions: { target: ts.ScriptTarget.ES2020, module: ts.ModuleKind.ES2020 },
        reportDiagnostics: true,
      });
      expect(result.diagnostics ?? []).toEqual([]);
    },
  );
});
