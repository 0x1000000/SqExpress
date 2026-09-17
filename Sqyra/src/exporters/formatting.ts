export type SqlNewLineStyle = "platform" | "lf" | "crlf";
export type SqlClauseBodyPlacement = "inline" | "next-line-indented";
export type SqlTableAliasPlacement = "inline" | "next-line-indented";
export type SqlBooleanOperatorPlacement = "inline" | "line-start" | "separate-line";
export type SqlListLayout = "inline" | "one-item-per-line";

export interface SqlFormattingOptions {
  readonly indentationSize?: number;
  readonly newLineStyle?: SqlNewLineStyle;
  readonly selectBody?: SqlClauseBodyPlacement;
  readonly whereBody?: SqlClauseBodyPlacement;
  readonly groupByBody?: SqlClauseBodyPlacement;
  readonly orderByBody?: SqlClauseBodyPlacement;
  readonly setBody?: SqlClauseBodyPlacement;
  readonly joinOnBody?: SqlClauseBodyPlacement;
  readonly outputBody?: SqlClauseBodyPlacement;
  readonly valuesBody?: SqlClauseBodyPlacement;
  readonly tableAliasPlacement?: SqlTableAliasPlacement;
  readonly booleanOperatorPlacement?: SqlBooleanOperatorPlacement;
  readonly selectList?: SqlListLayout;
  readonly groupByList?: SqlListLayout;
  readonly orderByList?: SqlListLayout;
  readonly setList?: SqlListLayout;
  readonly outputList?: SqlListLayout;
  readonly valuesRows?: SqlListLayout;
  readonly newLineBeforeClauses?: boolean;
  readonly newLineBeforeJoins?: boolean;
  readonly newLineAroundSetOperators?: boolean;
  readonly multilineSubqueries?: boolean;
  readonly multilineBooleanParentheses?: boolean;
  readonly multilineCteBodies?: boolean;
  readonly newLineBetweenCtes?: boolean;
  readonly newLineBetweenStatements?: boolean;
}

const bodyValues = new Set(["inline", "next-line-indented"]);
const aliasValues = new Set(["inline", "next-line-indented"]);
const booleanValues = new Set(["inline", "line-start", "separate-line"]);
const listValues = new Set(["inline", "one-item-per-line"]);
const newlineValues = new Set(["platform", "lf", "crlf"]);
export class SqlFormattingProfile implements Required<SqlFormattingOptions> {
  static readonly unformatted = new SqlFormattingProfile({});
  static readonly spacious = SqlFormattingProfile.unformatted.withOptions({
    selectBody: "next-line-indented",
    whereBody: "next-line-indented",
    groupByBody: "next-line-indented",
    orderByBody: "next-line-indented",
    setBody: "next-line-indented",
    joinOnBody: "next-line-indented",
    outputBody: "next-line-indented",
    valuesBody: "next-line-indented",
    tableAliasPlacement: "next-line-indented",
    booleanOperatorPlacement: "separate-line",
    selectList: "one-item-per-line",
    groupByList: "one-item-per-line",
    orderByList: "one-item-per-line",
    setList: "one-item-per-line",
    outputList: "one-item-per-line",
    valuesRows: "one-item-per-line",
    newLineBeforeClauses: true,
    newLineBeforeJoins: true,
    newLineAroundSetOperators: true,
    multilineSubqueries: true,
    multilineBooleanParentheses: true,
    multilineCteBodies: true,
    newLineBetweenCtes: true,
    newLineBetweenStatements: true,
  });
  readonly indentationSize: number;
  readonly newLineStyle: SqlNewLineStyle;
  readonly selectBody: SqlClauseBodyPlacement;
  readonly whereBody: SqlClauseBodyPlacement;
  readonly groupByBody: SqlClauseBodyPlacement;
  readonly orderByBody: SqlClauseBodyPlacement;
  readonly setBody: SqlClauseBodyPlacement;
  readonly joinOnBody: SqlClauseBodyPlacement;
  readonly outputBody: SqlClauseBodyPlacement;
  readonly valuesBody: SqlClauseBodyPlacement;
  readonly tableAliasPlacement: SqlTableAliasPlacement;
  readonly booleanOperatorPlacement: SqlBooleanOperatorPlacement;
  readonly selectList: SqlListLayout;
  readonly groupByList: SqlListLayout;
  readonly orderByList: SqlListLayout;
  readonly setList: SqlListLayout;
  readonly outputList: SqlListLayout;
  readonly valuesRows: SqlListLayout;
  readonly newLineBeforeClauses: boolean;
  readonly newLineBeforeJoins: boolean;
  readonly newLineAroundSetOperators: boolean;
  readonly multilineSubqueries: boolean;
  readonly multilineBooleanParentheses: boolean;
  readonly multilineCteBodies: boolean;
  readonly newLineBetweenCtes: boolean;
  readonly newLineBetweenStatements: boolean;
  private constructor(source: SqlFormattingOptions) {
    this.indentationSize = source.indentationSize ?? 4;
    this.newLineStyle = source.newLineStyle ?? "platform";
    this.selectBody = source.selectBody ?? "inline";
    this.whereBody = source.whereBody ?? "inline";
    this.groupByBody = source.groupByBody ?? "inline";
    this.orderByBody = source.orderByBody ?? "inline";
    this.setBody = source.setBody ?? "inline";
    this.joinOnBody = source.joinOnBody ?? "inline";
    this.outputBody = source.outputBody ?? "inline";
    this.valuesBody = source.valuesBody ?? "inline";
    this.tableAliasPlacement = source.tableAliasPlacement ?? "inline";
    this.booleanOperatorPlacement = source.booleanOperatorPlacement ?? "inline";
    this.selectList = source.selectList ?? "inline";
    this.groupByList = source.groupByList ?? "inline";
    this.orderByList = source.orderByList ?? "inline";
    this.setList = source.setList ?? "inline";
    this.outputList = source.outputList ?? "inline";
    this.valuesRows = source.valuesRows ?? "inline";
    this.newLineBeforeClauses = source.newLineBeforeClauses ?? false;
    this.newLineBeforeJoins = source.newLineBeforeJoins ?? false;
    this.newLineAroundSetOperators = source.newLineAroundSetOperators ?? false;
    this.multilineSubqueries = source.multilineSubqueries ?? false;
    this.multilineBooleanParentheses = source.multilineBooleanParentheses ?? false;
    this.multilineCteBodies = source.multilineCteBodies ?? false;
    this.newLineBetweenCtes = source.newLineBetweenCtes ?? false;
    this.newLineBetweenStatements = source.newLineBetweenStatements ?? false;
    if (!Number.isInteger(this.indentationSize) || this.indentationSize < 0)
      throw new RangeError("indentationSize must be a non-negative integer.");
    for (const [name, value, allowed] of [
      ["newLineStyle", this.newLineStyle, newlineValues],
      ["selectBody", this.selectBody, bodyValues],
      ["whereBody", this.whereBody, bodyValues],
      ["groupByBody", this.groupByBody, bodyValues],
      ["orderByBody", this.orderByBody, bodyValues],
      ["setBody", this.setBody, bodyValues],
      ["joinOnBody", this.joinOnBody, bodyValues],
      ["outputBody", this.outputBody, bodyValues],
      ["valuesBody", this.valuesBody, bodyValues],
      ["tableAliasPlacement", this.tableAliasPlacement, aliasValues],
      ["booleanOperatorPlacement", this.booleanOperatorPlacement, booleanValues],
      ["selectList", this.selectList, listValues],
      ["groupByList", this.groupByList, listValues],
      ["orderByList", this.orderByList, listValues],
      ["setList", this.setList, listValues],
      ["outputList", this.outputList, listValues],
      ["valuesRows", this.valuesRows, listValues],
    ] as const)
      if (!allowed.has(value)) throw new RangeError(`Invalid ${name}.`);
    Object.freeze(this);
  }
  withOptions(options: SqlFormattingOptions): SqlFormattingProfile {
    if (options === null || typeof options !== "object")
      throw new TypeError("Formatting options are required.");
    return new SqlFormattingProfile({ ...this, ...options });
  }
  get newLine(): string {
    return this.newLineStyle === "crlf" ? "\r\n" : "\n";
  }
}

function splitComma(value: string): string[] {
  const result: string[] = [];
  let start = 0,
    depth = 0,
    quote = "";
  for (let i = 0; i < value.length; i++) {
    const c = value[i]!;
    if (quote !== "") {
      if (c === quote) {
        if (value[i + 1] === quote) i++;
        else quote = "";
      } else if (c === "\\" && quote === "`") i++;
      continue;
    }
    if (c === "'" || c === '"' || c === "`") quote = c;
    else if (c === "[") quote = "]";
    else if (c === "(") depth++;
    else if (c === ")") depth--;
    else if (c === "," && depth === 0) {
      result.push(value.slice(start, i));
      start = i + 1;
    }
  }
  result.push(value.slice(start));
  return result;
}
export function formatSql(sql: string, profile: SqlFormattingProfile): string {
  if (profile === SqlFormattingProfile.unformatted) return sql.replace(/\)(AND|OR) \(/g, ")$1(");
  const nl = profile.newLine;
  const indent = " ".repeat(profile.indentationSize);
  const indentContinuation = (value: string): string => {
    let result = "",
      quote = "";
    for (let i = 0; i < value.length; i++) {
      const c = value[i]!;
      result += c;
      if (quote !== "") {
        if (c === quote) {
          if (value[i + 1] === quote) result += value[++i];
          else quote = "";
        } else if (c === "\\" && quote === "`" && i + 1 < value.length) result += value[++i];
      } else if (c === "'" || c === '"' || c === "`") quote = c;
      else if (c === "[") quote = "]";
      if (c === "\n" && quote === "") result += indent;
    }
    return result;
  };
  const atTopLevel = (value: string, callback: (index: number) => boolean): number => {
    let depth = 0,
      quote = "";
    for (let i = 0; i < value.length; i++) {
      const c = value[i]!;
      if (quote !== "") {
        if (c === quote) {
          if (value[i + 1] === quote) i++;
          else quote = "";
        } else if (c === "\\" && quote === "`") i++;
        continue;
      }
      if (c === "'" || c === '"' || c === "`") quote = c;
      else if (c === "[") quote = "]";
      else if (c === "(") depth++;
      else if (c === ")") depth--;
      else if (depth === 0 && callback(i)) return i;
    }
    return -1;
  };
  if (/^WITH(?: RECURSIVE)? /.test(sql) && profile.multilineCteBodies) {
    const prefix = /^WITH(?: RECURSIVE)? /.exec(sql)![0];
    const definitions: string[] = [];
    let position = prefix.length;
    while (position < sql.length) {
      const openMarker = sql.indexOf(" AS(", position);
      if (openMarker < 0) break;
      const name = sql.slice(position, openMarker);
      const open = openMarker + 3;
      let depth = 1,
        quote = "",
        close = -1;
      for (let i = open + 1; i < sql.length; i++) {
        const c = sql[i]!;
        if (quote !== "") {
          if (c === quote) {
            if (sql[i + 1] === quote) i++;
            else quote = "";
          } else if (c === "\\" && quote === "`") i++;
          continue;
        }
        if (c === "'" || c === '"' || c === "`") quote = c;
        else if (c === "[") quote = "]";
        else if (c === "(") depth++;
        else if (c === ")" && --depth === 0) {
          close = i;
          break;
        }
      }
      if (close < 0) break;
      const body = formatSql(sql.slice(open + 1, close), profile)
        .split(nl)
        .map((line) => indent + line)
        .join(nl);
      definitions.push(`${name} AS(${nl}${body}${nl})`);
      position = close + 1;
      if (sql[position] === ",") {
        position++;
        continue;
      }
      break;
    }
    if (definitions.length > 0)
      return `${prefix}${definitions.join(profile.newLineBetweenCtes ? `,${nl}` : ",")}${nl}${formatSql(sql.slice(position).trimStart(), profile)}`.replace(
        /(?:\r?\n)+$/,
        "",
      );
  }
  if (profile.multilineSubqueries && sql.includes("(SELECT")) {
    let expanded = "";
    for (let i = 0; i < sql.length;) {
      if (sql[i] !== "(") {
        expanded += sql[i++];
        continue;
      }
      let depth = 1,
        quote = "",
        close = -1;
      for (let j = i + 1; j < sql.length; j++) {
        const c = sql[j]!;
        if (quote !== "") {
          if (c === quote) {
            if (sql[j + 1] === quote) j++;
            else quote = "";
          } else if (c === "\\" && quote === "`") j++;
          continue;
        }
        if (c === "'" || c === '"' || c === "`") quote = c;
        else if (c === "[") quote = "]";
        else if (c === "(") depth++;
        else if (c === ")" && --depth === 0) {
          close = j;
          break;
        }
      }
      if (close < 0) {
        expanded += sql.slice(i);
        break;
      }
      const inner = sql.slice(i + 1, close);
      if (/^SELECT\b/.test(inner)) {
        const formatted = formatSql(inner, profile);
        expanded += `(${nl}${indent}${formatted.split(nl).join(nl + indent)}${nl})`;
      } else expanded += sql.slice(i, close + 1);
      i = close + 1;
    }
    sql = expanded;
  }
  const statementIndex = atTopLevel(sql, (index) => sql[index] === ";");
  if (statementIndex >= 0) {
    const separator = profile.newLineBetweenStatements ? nl : "";
    return `${formatSql(sql.slice(0, statementIndex), profile)};${separator}${formatSql(sql.slice(statementIndex + 1), profile)}`;
  }
  const setOperators = [" UNION ALL ", " INTERSECT ", " EXCEPT ", " UNION "];
  const setIndex = atTopLevel(sql, (index) =>
    setOperators.some((operator) => sql.startsWith(operator, index)),
  );
  if (setIndex >= 0) {
    const operator = setOperators.find((item) => sql.startsWith(item, setIndex))!;
    const left = sql.slice(0, setIndex);
    const right = sql.slice(setIndex + operator.length);
    const separator = profile.newLineAroundSetOperators ? nl : " ";
    return `${formatSql(left, profile)}${separator}${operator.trim()}${separator}${formatSql(right, profile)}`;
  }
  const clauseWords = [
    " GROUP BY ",
    " ORDER BY ",
    " LEFT JOIN ",
    " RIGHT JOIN ",
    " FULL JOIN ",
    " CROSS JOIN ",
    " JOIN ",
    " WHERE ",
    " FROM ",
    " OUTPUT ",
    " VALUES ",
    " SET ",
    " ON ",
  ];
  const boundaries: Array<{ index: number; word: string }> = [];
  let offset = 0;
  while (offset < sql.length) {
    const relative = atTopLevel(sql.slice(offset), (index) =>
      clauseWords.some((word) => sql.startsWith(word, offset + index)),
    );
    if (relative < 0) break;
    const index = offset + relative;
    const word = clauseWords.find((item) => sql.startsWith(item, index))!;
    boundaries.push({ index, word });
    offset = index + word.length;
  }
  const first = boundaries[0]?.index ?? sql.length;
  const head = sql.slice(0, first);
  let output: string;
  const select = /^(SELECT(?: DISTINCT)?(?: TOP [^ ]+)?) (.*)$/s.exec(head);
  if (select !== null) {
    let items = splitComma(select[2]!);
    if (profile.selectList === "one-item-per-line") items = items.map(indentContinuation);
    const boundary = profile.selectBody === "next-line-indented" ? nl + indent : " ";
    const separator = profile.selectList === "one-item-per-line" ? `,${nl}${indent}` : ",";
    output = `${select[1]}${boundary}${items.join(separator)}`;
  } else output = head;
  const booleanBody = (body: string): string => {
    if (profile.multilineBooleanParentheses)
      body = body.replace(
        /\(([^()]*(?: AND | OR )[^()]*)\)/g,
        (_match, inner: string) =>
          `(${nl}${indent}${indent}${booleanBody(inner)
            .split(nl)
            .join(nl + indent)}${nl}${indent})`,
      );
    if (profile.booleanOperatorPlacement === "inline") return body;
    const parts: string[] = [];
    let start = 0;
    while (start < body.length) {
      const index = atTopLevel(body.slice(start), (i) => {
        const p = start + i;
        return (
          body.startsWith(" AND ", p) ||
          body.startsWith(" OR ", p) ||
          (p > 0 && (body.startsWith("AND ", p) || body.startsWith("OR ", p)))
        );
      });
      if (index < 0) {
        parts.push(body.slice(start));
        break;
      }
      const absolute = start + index;
      const leading = body[absolute] === " ";
      const isAnd = body.startsWith(leading ? " AND " : "AND ", absolute);
      parts.push(body.slice(start, absolute), isAnd ? "AND" : "OR");
      start = absolute + (isAnd ? (leading ? 5 : 4) : leading ? 4 : 3);
    }
    let result = parts[0] ?? "";
    for (let i = 1; i < parts.length; i += 2)
      result +=
        profile.booleanOperatorPlacement === "line-start"
          ? `${nl}${indent}${parts[i]} ${parts[i + 1]}`
          : `${nl}${indent}${parts[i]}${nl}${indent}${parts[i + 1]}`;
    return result;
  };
  for (let b = 0; b < boundaries.length; b++) {
    const current = boundaries[b]!;
    const next = boundaries[b + 1]?.index ?? sql.length;
    const keyword = current.word.trim();
    let body = sql.slice(current.index + current.word.length, next);
    const clause = ["FROM", "WHERE", "GROUP BY", "ORDER BY", "OUTPUT", "VALUES", "SET"].includes(
      keyword,
    );
    const join = keyword.endsWith("JOIN");
    const newlineBefore = clause
      ? profile.newLineBeforeClauses
      : join
        ? profile.newLineBeforeJoins
        : false;
    const placement =
      keyword === "WHERE"
        ? profile.whereBody
        : keyword === "GROUP BY"
          ? profile.groupByBody
          : keyword === "ORDER BY"
            ? profile.orderByBody
            : keyword === "SET"
              ? profile.setBody
              : keyword === "OUTPUT"
                ? profile.outputBody
                : keyword === "VALUES"
                  ? profile.valuesBody
                  : keyword === "ON"
                    ? profile.joinOnBody
                    : "inline";
    const layout =
      keyword === "GROUP BY"
        ? profile.groupByList
        : keyword === "ORDER BY"
          ? profile.orderByList
          : keyword === "SET"
            ? profile.setList
            : keyword === "OUTPUT"
              ? profile.outputList
              : keyword === "VALUES"
                ? profile.valuesRows
                : "inline";
    if (keyword === "WHERE" || keyword === "ON") body = booleanBody(body);
    if (layout === "one-item-per-line") body = splitComma(body).join(`,${nl}${indent}`);
    if ((keyword === "FROM" || join) && profile.tableAliasPlacement === "next-line-indented")
      body = body.replace(/ (\[[^\]]+\]|"[^"]+"|`[^`]+`)$/, `${nl}${indent}$1`);
    output += `${newlineBefore ? nl : " "}${keyword}${placement === "next-line-indented" ? nl + indent : " "}${body}`;
  }
  if (boundaries.length === 0 && !/^SELECT\b/.test(sql) && /(?: AND | OR |\)AND |\)OR )/.test(sql))
    output = booleanBody(sql)
      .split(nl)
      .map((line) => line.trimStart())
      .join(nl);
  return output.replace(/(?:\r?\n)+$/, "");
}
