import { expect, it } from "vitest";
import { exprJsonNull, toSql } from "../../src/index.js";

it("exports the distinct JSON-null value like the C# dialect visitors", () => {
  expect(toSql(exprJsonNull, "tsql")).toBe("NULL");
  expect(toSql(exprJsonNull, "pgsql")).toBe("'null'::jsonb");
  expect(toSql(exprJsonNull, "mysql")).toBe("JSON_EXTRACT('null','$')");
  expect(toSql(exprJsonNull, "sqlite")).toBe("json('null')");
});
