import { readFileSync } from "node:fs";
import { expect, it } from "vitest";
import { identifierPayloads, literalPayloads } from "../../src/scenarios/sql-injections.js";

it("preserves every C# injection literal and identifier payload including escape sequences", () => {
  const source = readFileSync(
    new URL("../../../../Test/SqExpress.IntTest/Scenarios/ScSqlInjections.cs", import.meta.url),
    "utf8",
  );
  const escapes: Readonly<Record<string, string>> = {
    "\\": "\\",
    '"': '"',
    "'": "'",
    n: "\n",
    r: "\r",
    t: "\t",
    b: "\b",
    f: "\f",
    "0": "\0",
    a: "\x07",
    v: "\v",
  };
  for (const [name, actual] of [
    ["literalPayloads", literalPayloads],
    ["identifierPayloads", identifierPayloads],
  ] as const) {
    const body = source.match(new RegExp(`string\\[\\] ${name} =\\s*\\[([\\s\\S]*?)\\];`))?.[1];
    if (body === undefined) throw new Error(`Missing C# payload declaration ${name}.`);
    const expected = [...body.matchAll(/"(?:\\.|[^"\\])*"/g)].map((match) =>
      match[0].slice(1, -1).replace(/\\([\\"'nrtbf0av])/g, (_match, key: string) => {
        const decoded = escapes[key];
        if (decoded === undefined) throw new Error(`Unsupported C# escape ${key}.`);
        return decoded;
      }),
    );
    expect(actual).toEqual(expected);
  }
});
