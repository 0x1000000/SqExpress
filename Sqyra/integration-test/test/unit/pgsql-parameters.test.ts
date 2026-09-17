import { expect, it } from "vitest";
import { castPgParameters } from "../../src/adapters/pgsql.js";

it("adds PostgreSQL driver type casts only to parameter tokens", () => {
  const parameters = [{ name: "p1", type: "ExprInt32Literal", value: 1 }] as const;
  expect(
    castPgParameters(
      "SELECT $1,'$1',\"$1\",$$ $1 $$,$body$ $1 $body$ -- $1\n/* $1 /* $1 */ */,$2",
      parameters,
    ),
  ).toBe(
    "SELECT CAST($1 AS int4),'$1',\"$1\",$$ $1 $$,$body$ $1 $body$ -- $1\n/* $1 /* $1 */ */,$2",
  );
  expect(castPgParameters("SELECT E'it\\'s $1',$1,'it''s $1'", parameters)).toBe(
    "SELECT E'it\\'s $1',CAST($1 AS int4),'it''s $1'",
  );
});
