export type SqlTokenType =
  | "endOfFile"
  | "identifier"
  | "bracketIdentifier"
  | "stringLiteral"
  | "numberLiteral"
  | "comma"
  | "dot"
  | "openParen"
  | "closeParen"
  | "semicolon"
  | "operator"
  | "symbol";
export interface SqlToken {
  readonly type: SqlTokenType;
  readonly text: string;
  readonly start: number;
  readonly length: number;
  readonly end: number;
}
export type TokenizeResult =
  | { readonly success: true; readonly tokens: ReadonlyArray<SqlToken> }
  | { readonly success: false; readonly error: string };

const token = (type: SqlTokenType, text: string, start: number, length = text.length): SqlToken =>
  Object.freeze({ type, text, start, length, end: start + length });
const identifierStart = (char: string): boolean => /[\p{L}_#@$]/u.test(char);
const identifierPart = (char: string): boolean => /[\p{L}\p{N}_#@$]/u.test(char);

export function tokenize(sql: string): ReadonlyArray<SqlToken> {
  const result = tryTokenize(sql);
  if (!result.success) throw new Error(result.error);
  return result.tokens;
}

export function tryTokenize(sql: string): TokenizeResult {
  const result: SqlToken[] = [];
  let index = 0;
  while (index < sql.length) {
    const char = sql[index]!;
    if (/\s/u.test(char)) {
      index++;
      continue;
    }
    if (char === "-" && sql[index + 1] === "-") {
      index += 2;
      while (index < sql.length && sql[index] !== "\r" && sql[index] !== "\n") index++;
      continue;
    }
    if (char === "/" && sql[index + 1] === "*") {
      index += 2;
      let closed = false;
      while (index + 1 < sql.length) {
        if (sql[index] === "*" && sql[index + 1] === "/") {
          index += 2;
          closed = true;
          break;
        }
        index++;
      }
      if (!closed) return { success: false, error: "Syntax error: unterminated block comment." };
      continue;
    }
    if (char === "[") {
      const start = index++;
      let closed = false;
      while (index < sql.length) {
        if (sql[index] === "]") {
          if (sql[index + 1] === "]") {
            index += 2;
            continue;
          }
          index++;
          closed = true;
          break;
        }
        index++;
      }
      if (!closed)
        return { success: false, error: "Syntax error: unterminated bracket identifier." };
      result.push(token("bracketIdentifier", sql.slice(start, index), start));
      continue;
    }
    if ((char === "N" || char === "n") && sql[index + 1] === "'") {
      const read = readString(sql, index, true);
      if (!read.success) return read;
      result.push(read.token);
      index = read.end;
      continue;
    }
    if (char === "'") {
      const read = readString(sql, index, false);
      if (!read.success) return read;
      result.push(read.token);
      index = read.end;
      continue;
    }
    if (/\d/u.test(char)) {
      const start = index++;
      while (index < sql.length && /\d/u.test(sql[index]!)) index++;
      if (sql[index] === ".") {
        index++;
        while (index < sql.length && /\d/u.test(sql[index]!)) index++;
      }
      result.push(token("numberLiteral", sql.slice(start, index), start));
      continue;
    }
    if (identifierStart(char)) {
      const start = index++;
      while (index < sql.length && identifierPart(sql[index]!)) index++;
      result.push(token("identifier", sql.slice(start, index), start));
      continue;
    }
    const punctuation: Partial<Record<string, SqlTokenType>> = {
      ",": "comma",
      ".": "dot",
      "(": "openParen",
      ")": "closeParen",
      ";": "semicolon",
    };
    const type = punctuation[char] ?? ("+-*/%=<>!|&^~".includes(char) ? "operator" : "symbol");
    result.push(token(type, char, index, 1));
    index++;
  }
  result.push(token("endOfFile", "", sql.length, 0));
  return { success: true, tokens: Object.freeze(result) };
}

function readString(
  sql: string,
  start: number,
  unicode: boolean,
):
  | { readonly success: true; readonly token: SqlToken; readonly end: number }
  | { readonly success: false; readonly error: string } {
  let index = start + (unicode ? 2 : 1);
  while (index < sql.length) {
    if (sql[index] === "'") {
      if (sql[index + 1] === "'") {
        index += 2;
        continue;
      }
      index++;
      return {
        success: true,
        token: token("stringLiteral", sql.slice(start, index), start),
        end: index,
      };
    }
    index++;
  }
  return { success: false, error: "Syntax error: unterminated string literal." };
}

export function isKeyword(value: SqlToken, keyword: string): boolean {
  return (
    value.type === "identifier" &&
    value.text.toLocaleLowerCase("en-US") === keyword.toLocaleLowerCase("en-US")
  );
}
export function isIdentifierLike(value: SqlToken): boolean {
  return value.type === "identifier" || value.type === "bracketIdentifier";
}
export function identifierValue(value: SqlToken): string {
  return value.type === "bracketIdentifier" && value.text.length >= 2
    ? value.text.slice(1, -1).split("]]").join("]")
    : value.text;
}
