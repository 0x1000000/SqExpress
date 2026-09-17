import { expect, it } from "vitest";
import { types } from "pg";
import "../../src/adapters/pgsql.js";

it("preserves PostgreSQL temporal microseconds and offsets without Date truncation", () => {
  const value = "2024-01-02 03:04:05.123456+00";
  expect(types.getTypeParser(1184, "text")(value)).toBe("2024-01-02T03:04:05.123456+00:00");
  expect(types.getTypeParser(1184, "text")("2022-07-10 11:10:45-04")).toBe(
    "2022-07-10T11:10:45-04:00",
  );
  expect(types.getTypeParser(1114, "text")("2024-01-02 03:04:05.123456")).toBe(
    "2024-01-02 03:04:05.123456",
  );
});
