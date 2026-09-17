import type { CanonicalRow, CanonicalValue } from "./types.js";

export function normalizeValue(value: unknown): CanonicalValue {
  if (
    value === null ||
    typeof value === "string" ||
    typeof value === "number" ||
    typeof value === "bigint" ||
    typeof value === "boolean"
  )
    return value;
  if (value instanceof Uint8Array) return new Uint8Array(value);
  if (value instanceof Date) return value.toISOString();
  if (
    typeof value === "object" &&
    value !== null &&
    "toString" in value &&
    typeof value.toString === "function"
  )
    return value.toString();
  throw new TypeError(`Unsupported database value: ${Object.prototype.toString.call(value)}`);
}

export function normalizeRows(
  rows: ReadonlyArray<Readonly<Record<string, unknown>>>,
): ReadonlyArray<CanonicalRow> {
  return Object.freeze(
    rows.map((row) =>
      Object.freeze(
        Object.fromEntries(
          Object.entries(row).map(([name, value]) => [name, normalizeColumnValue(name, value)]),
        ),
      ),
    ),
  );
}

function normalizeColumnValue(name: string, value: unknown): CanonicalValue {
  if (
    /^(?:Bool|Boolean|Is|Has|Can)|Boolean$/i.test(name) &&
    (value === 0 || value === 1 || value === 0n || value === 1n)
  )
    return value === 1 || value === 1n;
  return normalizeValue(value);
}

export function parameterValues(
  parameters: ReadonlyArray<{ readonly value: unknown }>,
): ReadonlyArray<unknown> {
  return parameters.map(({ value }) =>
    typeof value === "boolean"
      ? value
        ? 1
        : 0
      : typeof value === "bigint"
        ? value.toString()
        : value instanceof Uint8Array
          ? Buffer.from(value)
          : value,
  );
}
