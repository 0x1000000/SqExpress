import { lit, select } from "sqyra";
import type { Scenario } from "./types.js";

export const literalPayloads = [
  "admin'--",
  "10; DROP TABLE members /*",
  "Line1\r\n\tLine2\\%%\b",
  "--",
  "'",
  "\\",
  "\\'",
  "\\\\",
  "`\"'",
  "``\\`\\\\`\"\\\"\\\\\"'\\'\\\\'",
  "\"\\\"''\\'*`\\`;",
  "'; EXEC xp_cmdshell('whoami');--",
  "1); WAITFOR DELAY '00:00:05'--",
  "');SELECT * FROM users WHERE '1'='1",
  "/*comment*/' OR 'x'='x",
  "UNION SELECT username,password FROM members--",
] as const;
export const identifierPayloads = [
  "alias'--",
  "alias; DROP TABLE members /*",
  "x y",
  "x.y",
  "x/y",
  "x\\y",
  "x--comment",
  "x/*comment*/",
  "from",
  "select",
  "@@version",
  "[x]",
  "`x`",
  '"x"',
  "0leading",
  "a]b",
  "a`b",
  'a"b',
  "a,b",
  "a:b",
] as const;

export const sqlInjectionsScenario: Scenario = {
  source: "ScSqlInjections",
  async run(context) {
    await assertRoundTrip(
      context,
      literalPayloads.map((value, index) => ({ alias: `v${index}`, value })),
      "literal",
    );
    await assertRoundTrip(
      context,
      identifierPayloads.map((value) => ({ alias: value, value })),
      "identifier",
    );
  },
};

async function assertRoundTrip(
  context: Parameters<Scenario["run"]>[0],
  payloads: ReadonlyArray<{ readonly alias: string; readonly value: string }>,
  kind: string,
): Promise<void> {
  const query = select(payloads.map((item) => lit(item.value).as(item.alias)));
  const row = (await context.query(query))[0];
  if (row === undefined) throw new Error(`No ${kind} round-trip row returned.`);
  for (const item of payloads)
    if (row[item.alias] !== item.value)
      throw new Error(`${kind} round trip failed for alias '${item.alias}'.`);
}
