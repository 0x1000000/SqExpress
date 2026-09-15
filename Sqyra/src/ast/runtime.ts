export interface AstNodeBase<K extends string> { readonly kind: K; }
export interface DecimalValue { readonly value: string; }
export interface DecimalPrecisionScale { readonly precision: number; readonly scale: number; }
declare const guidValueType: unique symbol;
export type GuidValue = string & { readonly [guidValueType]: true };
export interface DateTimeValue { readonly value: string; readonly kind: "unspecified" | "utc" | "local"; }
export interface DateTimeOffsetValue { readonly value: string; }
/** @internal Carries the immutable AST behind a fluent builder facade. */
export const queryAstNode = Symbol.for("sqyra.queryAstNode");
/** @internal Returns a fluent query's backing AST, or the input itself. */
export function unwrapAstNode<T>(value: T): T {
  if (typeof value !== "object" || value === null) return value;
  const ast = (value as { readonly [queryAstNode]?: T })[queryAstNode];
  return ast ?? value;
}

export function decimalValue(value: string): DecimalValue {
  if (!/^[+-]?(?:0|[1-9]\d*)(?:\.\d+)?$/.test(value)) throw new RangeError(`Invalid exact decimal: ${value}`);
  return Object.freeze({ value });
}
export function guidValue(value: string): GuidValue {
  if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value)) throw new RangeError(`Invalid GUID: ${value}`);
  return value as GuidValue;
}
export function dateTimeValue(value: string, kind: DateTimeValue["kind"] = "unspecified"): DateTimeValue {
  if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?$/.test(value)) throw new RangeError(`Invalid date-time value: ${value}`);
  return Object.freeze({ value, kind });
}
export function dateTimeOffsetValue(value: string): DateTimeOffsetValue {
  if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]\d{2}:\d{2})$/.test(value)) throw new RangeError(`Invalid date-time-offset value: ${value}`);
  return Object.freeze({ value });
}
export function int64Value(value: bigint): bigint {
  if (value < -9223372036854775808n || value > 9223372036854775807n) throw new RangeError(`Int64 is out of range: ${value}`);
  return value;
}
export function binaryValue(value: Uint8Array): Uint8Array { return Uint8Array.from(value); }

/** @internal Used by generated, field-specific factories. */
export function freezeNode<K extends string, T extends object>(kind: K, fields: T, expectedFields: ReadonlyArray<string>): Readonly<AstNodeBase<K> & T> {
  const actual = Object.keys(fields);
  if (actual.length !== expectedFields.length || expectedFields.some((name) => !Object.prototype.hasOwnProperty.call(fields, name)) || actual.some((name) => !expectedFields.includes(name))) throw new TypeError(`${kind} requires exactly these fields: ${expectedFields.join(", ")}`);
  for (const name of expectedFields) if ((fields as Readonly<Record<string, unknown>>)[name] === undefined) throw new TypeError(`${kind}.${name} cannot be undefined.`);
  const copy = Object.fromEntries(Object.entries(fields).map(([name, value]) => [name, Array.isArray(value) ? Object.freeze([...value]) : value]));
  // Object spread preserves every field in T, and only replaces mutable arrays with readonly copies.
  return Object.freeze({ kind, ...copy }) as Readonly<AstNodeBase<K> & T>;
}
