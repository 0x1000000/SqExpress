import type { Expr, ExprBoolean, ExprType, ExprValue, IExprColumnSource, IExprSelecting, IExprTableFullName, IExprTableSource } from "../ast/generated/ast.generated.js";
import { exprMerge } from "../ast/generated/ast.generated.js";
import { walk } from "../ast/operations.js";
import { formatSql, type SqlFormattingProfile } from "./formatting.js";
export * from "./formatting.js";

export type SqlDialect = "tsql" | "postgresql" | "mysql" | "sqlite";
export interface ExportOptions { readonly dialect: SqlDialect; readonly mysqlFlavor?: "mariadb" | "oracle"; readonly avoidNameQuoting?: boolean; readonly schemaMap?: ReadonlyArray<{ readonly from: string; readonly to: string }>; readonly parameterize?: boolean; readonly formatting?: SqlFormattingProfile | null; }
export interface SqlParameter { readonly name: string; readonly value: unknown; readonly type: string | null; }
export interface CompiledSql { readonly sql: string; readonly parameters: ReadonlyArray<SqlParameter>; }

export function toSql(ast: Expr, options: ExportOptions): string { const sql = new Renderer(options).renderRoot(ast); return options.formatting === undefined || options.formatting === null ? sql : formatSql(sql, options.formatting); }
export function compileSql(ast: Expr, options: ExportOptions): CompiledSql { const renderer = new Renderer({ ...options, parameterize: true }); const compact = renderer.renderRoot(ast); const sql = options.formatting === undefined || options.formatting === null ? compact : formatSql(compact, options.formatting); return { sql, parameters: Object.freeze(renderer.parameters) }; }
export function toSqlType(type: ExprType, options: ExportOptions): string { return new Renderer(options).renderSqlType(type); }

class Renderer {
  readonly parameters: SqlParameter[] = [];
  private implicitColumnSource: string | null = null;
  private implicitColumnSources: ReadonlyMap<string, string> = new Map();
  private columnSourceRemap: ReadonlyMap<string, string> = new Map();
  private renderingWithRootCtes = false;
  private renderingJsonQuery = false;
  constructor(private readonly options: ExportOptions) {}
  renderSqlType(type: ExprType): string { return this.sqlType(type, false); }
  renderRoot(node: Expr): string {
    const ctes = new Map<string, Extract<Expr, { readonly kind: "ExprCteQuery" }>>();
    for (const item of walk(node)) if (item.kind === "ExprCteQuery" && !ctes.has(item.name.toLocaleLowerCase("en-US"))) ctes.set(item.name.toLocaleLowerCase("en-US"), item);
    const suppressRootCtes = (this.options.dialect === "mysql" && ["ExprDelete", "ExprUpdate", "ExprInsert", "ExprMerge"].includes(node.kind)) || (this.options.dialect === "postgresql" && node.kind === "ExprUpdate" && node.source?.kind === "ExprCteQuery");
    this.renderingWithRootCtes = ctes.size > 0 && !suppressRootCtes; const body = this.render(node); if (ctes.size === 0) return body;
    if (suppressRootCtes) return body;
    const separator = this.options.dialect === "mysql" ? " " : "";
    const recursive = this.options.dialect !== "tsql" && [...ctes.values()].some((cte) => [...walk(cte.query)].some((item) => item.kind === "ExprCteQuery" && item.name.toLocaleLowerCase("en-US") === cte.name.toLocaleLowerCase("en-US") && item.query.kind === "ExprQuerySpecification" && item.query.selectList.length === 0));
    const directDependencies = (input: Expr): string[] => {
      const query = input.kind === "ExprCteQuery" ? input.query.kind === "ExprSelectOffsetFetch" ? input.query.selectQuery : input.query : input.kind === "ExprSelect" || input.kind === "ExprSelectOffsetFetch" ? input.selectQuery : input;
      const result: string[] = []; const scanSource = (source: IExprTableSource): void => { if (source.kind === "ExprCteQuery") result.push(source.name.toLocaleLowerCase("en-US")); else if (source.kind === "ExprCrossedTable" || source.kind === "ExprJoinedTable" || source.kind === "ExprLateralCrossedTable") { scanSource(source.left); scanSource(source.right); } };
      const scanQuery = (item: Expr): void => { if (item.kind === "ExprQuerySpecification") { if (item.from !== null) scanSource(item.from); } else if (item.kind === "ExprQueryExpression") { scanQuery(item.left); scanQuery(item.right); } else if (item.kind === "ExprSelectOffsetFetch") scanQuery(item.selectQuery); };
      if (input.kind === "ExprUpdate" || input.kind === "ExprDelete") { if (input.source !== null) scanSource(input.source); } else if (input.kind === "ExprMerge") scanSource(input.source); else scanQuery(query); return [...new Set(result)];
    };
    const ordered: Array<Extract<Expr, { readonly kind: "ExprCteQuery" }>> = []; const visiting = new Set<string>(); const added = new Set<string>();
    const append = (key: string, root: boolean): void => { if (added.has(key) || visiting.has(key)) return; const cte = ctes.get(key); if (cte === undefined) return; visiting.add(key); for (const dependency of directDependencies(cte)) { if (!added.has(dependency) && !visiting.has(dependency)) { const child = ctes.get(dependency); if (child !== undefined) { added.add(dependency); ordered.push(child); appendChildren(child); } } } visiting.delete(key); if (root && !added.has(key)) { added.add(key); ordered.push(cte); } };
    const appendChildren = (cte: Extract<Expr, { readonly kind: "ExprCteQuery" }>): void => { for (const dependency of directDependencies(cte)) if (!added.has(dependency)) { const child = ctes.get(dependency); if (child !== undefined) { added.add(dependency); ordered.push(child); appendChildren(child); } } };
    for (const key of directDependencies(node)) append(key, true);
    for (const key of ctes.keys()) if (!added.has(key)) { added.add(key); ordered.push(ctes.get(key)!); }
    return `WITH${recursive ? " RECURSIVE" : ""} ${ordered.map((cte) => `${this.quote(cte.name)} AS(${this.render(cte.query)})`).join(",")}${separator}${body}`;
  }
  render(node: Expr): string {
    switch (node.kind) {
      case "ExprQuerySpecification": return this.querySpecification(node, true);
      case "ExprSelect": return `${node.selectQuery.kind === "ExprQuerySpecification" ? this.querySpecification(node.selectQuery, false) : this.render(node.selectQuery)} ORDER BY ${node.orderBy.orderList.map((item) => `${this.value(item.value)}${item.descendant ? " DESC" : ""}`).join(",")}${this.limit(node.selectQuery)}`;
      case "ExprSelectOffsetFetch": { if (node.selectQuery.kind === "ExprQuerySpecification" && node.selectQuery.top !== null && node.orderBy.offsetFetch.fetch !== null) throw new Error("TOP and OFFSET/FETCH cannot both limit the same query."); const ordered = `${node.selectQuery.kind === "ExprQuerySpecification" ? this.querySpecification(node.selectQuery, false) : this.render(node.selectQuery)} ORDER BY ${node.orderBy.orderList.map((item) => `${this.value(item.value)}${item.descendant ? " DESC" : ""}`).join(",")}`; return this.options.dialect === "mysql" || this.options.dialect === "sqlite" ? `${ordered}${node.orderBy.offsetFetch.fetch === null ? "" : ` LIMIT ${this.value(node.orderBy.offsetFetch.fetch)}`} OFFSET ${this.value(node.orderBy.offsetFetch.offset)}` : `${ordered} OFFSET ${this.value(node.orderBy.offsetFetch.offset)} ROW${node.orderBy.offsetFetch.fetch === null ? "" : ` FETCH NEXT ${this.value(node.orderBy.offsetFetch.fetch)} ROW ONLY`}`; }
      case "ExprQueryExpression": { const parenthesizeLeft = (this.options.dialect === "postgresql" || this.options.dialect === "mysql") && node.left.kind === "ExprQueryExpression"; return `${parenthesizeLeft ? `(${this.render(node.left)})` : this.render(node.left)} ${node.queryExpressionType === "UnionAll" ? "UNION ALL" : node.queryExpressionType.toUpperCase()} ${node.right.kind === "ExprQueryExpression" ? `(${this.render(node.right)})` : this.render(node.right)}`; }
      case "ExprQueryList": return node.expressions.map(item => this.render(item)).join(";");
      case "ExprQueryAsJson": return this.queryAsJson(node);
      case "ExprJsonOutputColumn": { if (!this.renderingJsonQuery) throw new Error("JSON output columns can only be exported as part of a FOR JSON query."); const name = this.jsonOutputName(node.jsonPath); return `${this.selecting(node.value)} ${this.quote(name)}`; }
      case "ExprInt32Literal": case "ExprInt16Literal": case "ExprByteLiteral": return node.value === null ? "NULL" : String(node.value);
      case "ExprInt64Literal": return node.value === null ? "NULL" : node.value.toString();
      case "ExprDecimalLiteral": return node.value === null ? "NULL" : node.value.value;
      case "ExprDoubleLiteral": return node.value === null ? "NULL" : String(node.value);
      case "ExprStringLiteral": return node.value === null ? "NULL" : `'${(this.options.dialect === "mysql" ? node.value.split("\\").join("\\\\") : node.value).split("'").join("''")}'`;
      case "ExprBoolLiteral": return node.value === null ? "NULL" : this.options.dialect === "tsql" ? `CAST(${node.value ? 1 : 0} AS bit)` : this.options.dialect === "postgresql" ? String(node.value).toUpperCase() : node.value ? "1" : "0";
      case "ExprGuidLiteral": return node.value === null ? "NULL" : `'${node.value}'`;
      case "ExprByteArrayLiteral": { if (node.value === null) return "NULL"; const hex = [...node.value].map((item) => item.toString(16).padStart(2, "0").toUpperCase()).join(""); return this.options.dialect === "tsql" ? `0x${hex}` : this.options.dialect === "postgresql" ? `'\\x${hex.toLowerCase()}'::bytea` : `X'${hex}'`; }
      case "ExprDateTimeLiteral": { if (node.value === null) return "NULL"; let value = node.value.value; if (/T00:00:00(?:\.0+)?$/.test(value)) value = value.slice(0, 10); else if (this.options.dialect === "mysql") value = value.replace("T", " "); return `'${value}'`; }
      case "ExprDateTimeOffsetLiteral": return node.value === null ? "NULL" : `'${node.value.value}'`;
      case "ExprNull": return "NULL";
      case "ExprDefault": return "DEFAULT";
      case "ExprStringConcat": return this.options.dialect === "mysql" ? `CONCAT(${this.value(node.left)},${this.value(node.right)})` : `${this.value(node.left)}${this.options.dialect === "tsql" ? "+" : "||"}${this.value(node.right)}`;
      case "ExprUnsafeValue": return node.unsafeValue;
      case "ExprParameter": {
        if (!this.options.parameterize || node.replacedValue === null) return node.replacedValue === null ? `@${node.tagName ?? "p"}` : this.value(node.replacedValue);
        const index = this.parameters.length; const name = node.tagName ?? `p${index}`; this.parameters.push(Object.freeze({ name, value: this.parameterValue(node.replacedValue), type: node.replacedValue.kind }));
        return this.options.dialect === "postgresql" ? `$${index + 1}` : this.options.dialect === "tsql" ? `@${name}` : "?";
      }
      case "ExprColumn": { const inferred = this.implicitColumnSources.get(node.columnName.name.toLocaleLowerCase("en-US")) ?? this.implicitColumnSource; const explicit = node.source === null ? inferred : this.rawColumnSource(node.source); const mapped = explicit === null ? null : this.columnSourceRemap.get(explicit.toLocaleLowerCase("en-US")) ?? explicit; return `${mapped === null || mapped === "" ? "" : `${this.quote(mapped)}.`}${this.quote(node.columnName.name)}`; }
      case "ExprAllColumns": return `${node.source === null ? "" : `${this.columnSource(node.source)}.`}*`;
      case "ExprAliasedSelecting": return `${this.selecting(node.value)} ${this.quote(node.alias.name)}`;
      case "ExprAliasedColumn": return `${this.render(node.column)}${node.alias === null ? "" : ` ${this.quote(node.alias.name)}`}`;
      case "ExprTable": return `${this.tableName(node.fullName)}${node.alias === null ? "" : ` ${this.columnSource(node.alias)}`}`;
      case "ExprBooleanEq": return `${this.value(node.left)}=${this.value(node.right)}`;
      case "ExprBooleanNotEq": return `${this.value(node.left)}!=${this.value(node.right)}`;
      case "ExprBooleanGt": return `${this.value(node.left)}>${this.value(node.right)}`;
      case "ExprBooleanGtEq": return `${this.value(node.left)}>=${this.value(node.right)}`;
      case "ExprBooleanLt": return `${this.value(node.left)}<${this.value(node.right)}`;
      case "ExprBooleanLtEq": return `${this.value(node.left)}<=${this.value(node.right)}`;
      case "ExprBooleanNot": return `NOT ${this.boolean(node.expr)}`;
      case "ExprIsNull": return `${this.value(node.test)} IS ${node.not ? "NOT " : ""}NULL`;
      case "ExprLike": return `${this.value(node.test)} LIKE ${this.value(node.pattern)}`;
      case "ExprInValues": return `${this.value(node.testExpression)} IN(${node.items.map((item) => this.value(item)).join(",")})`;
      case "ExprInSubQuery": return `${this.value(node.testExpression)} IN(${this.options.dialect === "mysql" ? this.mysqlSubQuery(node.subQuery) : this.render(node.subQuery)})`;
      case "ExprExists": return `EXISTS(${this.render(node.subQuery)})`;
      case "ExprValueQuery": return `(${this.render(node.query)})`;
      case "ExprBooleanAnd": return `${node.left.kind === "ExprBooleanOr" ? `(${this.boolean(node.left)})AND ` : `${this.boolean(node.left)} AND `}${node.right.kind === "ExprBooleanOr" ? `(${this.boolean(node.right)})` : this.boolean(node.right)}`;
      case "ExprBooleanOr": return `${this.boolean(node.left)} OR ${this.boolean(node.right)}`;
      case "ExprSum": return `${this.value(node.left)}+${this.value(node.right)}`;
      case "ExprSub": return `${this.value(node.left)}-${this.value(node.right)}`;
      case "ExprMul": return `${this.productOperand(node.left)}*${this.productOperand(node.right)}`;
      case "ExprDiv": return `${this.value(node.left)}/${this.value(node.right)}`;
      case "ExprModulo": return `${this.productOperand(node.left)}%${this.productOperand(node.right)}`;
      case "ExprBitwiseAnd": return `${this.value(node.left)}&${this.value(node.right)}`;
      case "ExprBitwiseOr": return `${this.value(node.left)}|${this.value(node.right)}`;
      case "ExprBitwiseXor": return `${this.value(node.left)}^${this.value(node.right)}`;
      case "ExprBitwiseNot": return `~${this.value(node.value)}`;
      case "ExprScalarFunction": { const prefix = node.schema === null || this.options.dialect === "sqlite" ? "" : this.options.dialect === "mysql" ? node.schema.database === null ? "" : `${this.quote(node.schema.database.name)}..` : `${node.schema.database === null ? "" : `${this.quote(node.schema.database.name)}.`}${this.quote(node.schema.schema.name)}.`; return `${prefix}${node.name.builtIn ? node.name.name.toUpperCase() : this.quote(node.name.name)}(${node.arguments?.map((item) => this.value(item)).join(",") ?? ""})`; }
      case "ExprPortableScalarFunction": return this.portableScalar(node);
      case "ExprFuncIsNull": return `${this.options.dialect === "tsql" ? "ISNULL" : "COALESCE"}(${this.value(node.test)},${this.value(node.alt)})`;
      case "ExprFuncCoalesce": return `COALESCE(${[node.test, ...node.alts].map((item) => this.value(item)).join(",")})`;
      case "ExprGetDate": return this.options.dialect === "tsql" ? "GETDATE()" : this.options.dialect === "postgresql" ? "now()" : this.options.dialect === "mysql" ? "UTC_DATE()" : "CURRENT_DATE";
      case "ExprGetUtcDate": return this.options.dialect === "tsql" ? "GETUTCDATE()" : this.options.dialect === "postgresql" ? "now() at time zone 'utc'" : this.options.dialect === "mysql" ? "UTC_TIMESTAMP()" : "CURRENT_TIMESTAMP";
      case "ExprAggregateFunction": return `${node.name.name.toUpperCase()}(${node.isDistinct ? "DISTINCT " : ""}${this.value(node.expression)})`;
      case "ExprStringAgg": return this.stringAgg(node);
      case "ExprDateAdd": return this.dateAdd(node);
      case "ExprDateDiff": return this.dateDiff(node);
      case "ExprAggregateOverFunction": return `${this.selecting(node.function)}${this.over(node.over)}`;
      case "ExprAnalyticFunction": return `${node.name.name.toUpperCase()}(${node.arguments?.map((item) => this.value(item)).join(",") ?? ""})${this.over(node.over)}`;
      case "ExprSelectingValue": return this.selecting(node.selecting);
      case "ExprJsonValue": return this.jsonValue(node);
      case "ExprJsonQuery": return this.jsonQuery(node);
      case "ExprJsonSet": { const document = this.value(node.document); const path = `'${node.path.split("'").join("''")}'`; const value = this.value(node.value); if (this.options.dialect === "tsql") return `JSON_MODIFY(${document},${path},${value})`; if (this.options.dialect === "postgresql") return `jsonb_set(CAST(${document} AS jsonb),string_to_array(trim(leading '$.' from ${path}),'.'),to_jsonb(${value}),true)`; if (this.options.dialect === "mysql") return `JSON_SET(${document},${path},${value})`; return `json_set(${document},${path},${value})`; }
      case "ExprJsonRemove": { const document = this.value(node.document); const path = `'${node.path.split("'").join("''")}'`; if (this.options.dialect === "tsql") return `JSON_MODIFY(${document},${path},NULL)`; if (this.options.dialect === "postgresql") return `CAST(${document} AS jsonb)#-string_to_array(trim(leading '$.' from ${path}),'.')`; if (this.options.dialect === "mysql") return `JSON_REMOVE(${document},${path})`; return `json_remove(${document},${path})`; }
      case "ExprJsonArray": { const items = node.items.map((item) => this.value(item)).join(","); if (this.options.dialect === "tsql") return `JSON_ARRAY(${items} NULL ON NULL)`; if (this.options.dialect === "postgresql") return `jsonb_build_array(${items})`; if (this.options.dialect === "mysql") return `JSON_ARRAY(${items})`; return `json_array(${items})`; }
      case "ExprJsonObject": { const items = node.members.flatMap((item) => [`'${item.name.split("'").join("''")}'`, this.value(item.value)]).join(","); if (this.options.dialect === "tsql") return `JSON_OBJECT(${node.members.map((item) => `'${item.name.split("'").join("''")}':${this.value(item.value)}`).join(",")} NULL ON NULL)`; if (this.options.dialect === "postgresql") return `jsonb_build_object(${items})`; if (this.options.dialect === "mysql") return `JSON_OBJECT(${items})`; return `json_object(${items})`; }
      case "ExprCast": return `CAST(${this.selecting(node.expression)} AS ${this.sqlType(node.sqlType)})`;
      case "ExprCase": return `CASE${node.cases.map((item) => ` WHEN ${this.boolean(item.condition)} THEN ${this.value(item.value)}`).join("")} ELSE ${this.value(node.defaultValue)} END`;
      case "ExprCrossedTable": return `${this.tableSource(node.left)} CROSS JOIN ${this.tableSource(node.right)}`;
      case "ExprLateralCrossedTable": return `${this.tableSource(node.left)} ${this.options.dialect === "tsql" ? `${node.outer ? "OUTER APPLY" : "CROSS APPLY"} ` : node.outer ? "LEFT JOIN LATERAL" : "CROSS JOIN LATERAL "}${this.tableSource(node.right)}${this.options.dialect !== "tsql" && node.outer ? " ON TRUE" : ""}`;
      case "ExprJoinedTable": return `${this.tableSource(node.left)} ${node.joinType === "Inner" ? "" : `${node.joinType.toUpperCase()} `}JOIN ${this.tableSource(node.right)} ON ${this.boolean(node.searchCondition)}`;
      case "ExprDerivedTableQuery": return `(${this.render(node.query)})${this.options.dialect === "sqlite" ? " AS " : ""}${this.columnSource(node.alias)}${node.columns === null ? "" : `(${node.columns.map((item) => this.quote(item.name)).join(",")})`}`;
      case "ExprDerivedTableValues": return this.derivedValues(node);
      case "ExprCteQuery": return `${this.quote(node.name)}${node.alias === null ? "" : ` ${this.columnSource(node.alias)}`}`;
      case "ExprJsonTable": return this.jsonTable(node);
      case "ExprTableFunction": return `${node.schema === null || this.options.dialect === "sqlite" || this.options.dialect === "mysql" ? "" : `${this.quote(node.schema.schema.name)}.`}${node.name.builtIn ? node.name.name.toUpperCase() : this.quote(node.name.name)}(${node.arguments?.map((item) => this.value(item)).join(",") ?? ""})`;
      case "ExprAliasedTableFunction": return `${this.render(node.function)} ${this.columnSource(node.alias)}`;
      case "ExprInsert": return `INSERT INTO ${this.tableName(node.target)}${node.targetColumns === null ? "" : `(${node.targetColumns.map((item) => this.quote(item.name)).join(",")})`} ${this.render(node.source)}`;
      case "ExprIdentityInsert": {
        const insert = this.render(node.insert); const table = this.tableName(node.insert.target);
        if (this.options.dialect === "tsql") return `SET IDENTITY_INSERT ${table} ON;${insert};SET IDENTITY_INSERT ${table} OFF;`;
        if (this.options.dialect === "postgresql") {
          if (node.identityColumns.length !== 1) throw new Error("PostgreSQL identity insert requires exactly one identity column.");
          const column = this.quote(node.identityColumns[0]!.name); const marker = node.insert.targetColumns === null ? table : `${table}(${node.insert.targetColumns.map(item => this.quote(item.name)).join(",")})`;
          const pgInsert = insert.replace(`INSERT INTO ${marker} `, `INSERT INTO ${marker} OVERRIDING SYSTEM VALUE `);
          return `WITH "__sqexpress_identity_insert" AS (${pgInsert}  RETURNING ${column}) SELECT setval(pg_get_serial_sequence('${table.split("'").join("''")}','${node.identityColumns[0]!.name.split("'").join("''")}'),GREATEST((SELECT MAX(${column}) FROM ${table}),(SELECT MAX(${column}) FROM "__sqexpress_identity_insert"))) FROM "__sqexpress_identity_insert" LIMIT 1`;
        }
        return insert;
      }
      case "ExprInsertOutput": {
        if (this.options.dialect === "mysql" && this.options.mysqlFlavor === "oracle") throw new Error("Oracle MySQL does not support generic INSERT OUTPUT/RETURNING");
        const insert = node.insert; const target = `INSERT INTO ${this.tableName(insert.target)}${insert.targetColumns === null ? "" : `(${insert.targetColumns.map((item) => this.quote(item.name)).join(",")})`}`;
        const columns = node.outputColumns.map((item) => `${this.quote(item.column.name)}${item.alias === null ? "" : ` ${this.quote(item.alias.name)}`}`).join(",");
        return this.options.dialect === "tsql" ? `${target} OUTPUT ${node.outputColumns.map((item) => `INSERTED.${this.quote(item.column.name)}${item.alias === null ? "" : ` ${this.quote(item.alias.name)}`}`).join(",")} ${this.render(insert.source)}` : `${target} ${this.render(insert.source)}   RETURNING ${columns}`;
      }
      case "ExprInsertValues": return `VALUES ${node.items.map((row) => `(${row.items.map((item) => this.render(item)).join(",")})`).join(",")}`;
      case "ExprInsertQuery": {
        if (this.options.dialect === "mysql" && node.query.kind === "ExprQuerySpecification" && node.query.from?.kind === "ExprDerivedTableValues") {
          const source = node.query.from; const alias = source.alias.alias.kind === "ExprAlias" ? source.alias.alias.name : "A0"; const cte = "CTE_Derived_Table_0";
          const previous = this.columnSourceRemap; this.columnSourceRemap = new Map([...previous, [alias.toLocaleLowerCase("en-US"), ""]]); const selected = node.query.selectList.map(item => this.selecting(item)).join(","); this.columnSourceRemap = previous;
          const values = source.values.items.map(row => `(${row.items.map(item => this.value(item)).join(",")})`).join(",");
          return `WITH ${cte}(${source.columns.map(column => this.quote(column.name)).join(",")}) AS(VALUES ${values}) SELECT ${selected} FROM ${this.quote(cte)} ${this.quote(alias)}${node.query.where === null ? "" : ` WHERE ${this.boolean(node.query.where)}`}`;
        }
        return this.render(node.query);
      }
      case "ExprUpdate": return this.update(node);
      case "ExprDelete": return this.delete(node);
      case "ExprDeleteOutput": { if (this.options.dialect === "mysql") throw new Error("Oracle MySQL does not support generic DELETE OUTPUT/RETURNING"); const deleted = node.delete; const columns = node.outputColumns.map((item) => `${this.render(item.column)}${item.alias === null ? "" : ` ${this.quote(item.alias.name)}`}`).join(","); if (this.options.dialect === "tsql") { const targetName = deleted.target.fullName.kind === "ExprTableFullName" ? deleted.target.fullName.tableName.name : deleted.target.fullName.name; const targetAlias = deleted.target.alias?.alias.kind === "ExprAlias" ? deleted.target.alias.alias.name : targetName; const source = deleted.source ?? deleted.target; return `DELETE ${this.quote(targetAlias)} OUTPUT ${node.outputColumns.map((item) => `DELETED.${this.quote(item.column.columnName.name)}${item.alias === null ? "" : ` ${this.quote(item.alias.name)}`}`).join(",")} FROM ${this.tableSource(source)}${deleted.filter === null ? "" : ` WHERE ${this.boolean(deleted.filter)}`}`; } return `${this.delete(deleted)} RETURNING ${columns}`; }
      case "ExprMerge": return this.options.dialect === "postgresql" && (node.source.kind === "ExprDerivedTableQuery" || node.source.kind === "ExprDerivedTableValues") ? this.pgMerge(node) : this.options.dialect === "mysql" && (node.source.kind === "ExprDerivedTableQuery" || node.source.kind === "ExprDerivedTableValues") ? this.mysqlMerge(node) : this.options.dialect === "sqlite" && (node.source.kind === "ExprDerivedTableQuery" || node.source.kind === "ExprDerivedTableValues") ? this.sqliteMerge(node) : `MERGE ${this.tableSource(node.targetTable)} USING ${this.tableSource(node.source)} ON ${this.boolean(node.on)}${node.whenMatched === null ? "" : ` WHEN MATCHED${node.whenMatched.and === null ? "" : ` AND ${this.boolean(node.whenMatched.and)}`} THEN ${node.whenMatched.kind === "ExprMergeMatchedDelete" ? " " : ""}${this.mergeMatched(node.whenMatched)}`}${node.whenNotMatchedByTarget === null ? "" : ` WHEN NOT MATCHED${node.whenNotMatchedByTarget.and === null ? "" : ` AND ${this.boolean(node.whenNotMatchedByTarget.and)}`} THEN ${node.whenNotMatchedByTarget.kind === "ExprExprMergeNotMatchedInsertDefault" ? "INSERT DEFAULT VALUES" : `INSERT(${node.whenNotMatchedByTarget.columns.map((column) => this.quote(column.name)).join(",")}) VALUES(${node.whenNotMatchedByTarget.values.map((value) => this.render(value)).join(",")})`}`}${node.whenNotMatchedBySource === null ? "" : ` WHEN NOT MATCHED BY SOURCE${node.whenNotMatchedBySource.and === null ? "" : ` AND ${this.boolean(node.whenNotMatchedBySource.and)}`} THEN ${node.whenNotMatchedBySource.kind === "ExprMergeMatchedDelete" ? " " : ""}${this.mergeMatched(node.whenNotMatchedBySource)}`};`;
      case "ExprMergeOutput": { if (this.options.dialect !== "tsql") throw new Error("MERGE OUTPUT is only supported by the T-SQL exporter."); const merge = exprMerge({ targetTable: node.targetTable, source: node.source, on: node.on, whenMatched: node.whenMatched, whenNotMatchedByTarget: node.whenNotMatchedByTarget, whenNotMatchedBySource: node.whenNotMatchedBySource }); const base = this.render(merge).slice(0, -1); const output = node.output.columns.map(item => item.kind === "ExprOutputColumn" ? `${this.render(item.column.column)}${item.column.alias === null ? "" : ` ${this.quote(item.column.alias.name)}`}` : item.kind === "ExprOutputColumnInserted" ? `INSERTED.${this.quote(item.columnName.column.name)}${item.columnName.alias === null ? "" : ` ${this.quote(item.columnName.alias.name)}`}` : item.kind === "ExprOutputColumnDeleted" ? `DELETED.${this.quote(item.columnName.column.name)}${item.columnName.alias === null ? "" : ` ${this.quote(item.columnName.alias.name)}`}` : `$ACTION${item.alias === null ? "" : ` ${this.quote(item.alias.name)}`}`).join(","); return `${base} OUTPUT ${output};`; }
      default: throw new Error(`SQL export for ${node.kind} has not been ported yet.`);
    }
  }
  private selecting(node: IExprSelecting): string { return this.render(node); }
  private parameterValue(node: ExprValue): unknown { switch (node.kind) { case "ExprNull": return null; case "ExprStringLiteral": case "ExprBoolLiteral": case "ExprByteLiteral": case "ExprInt16Literal": case "ExprInt32Literal": case "ExprInt64Literal": case "ExprDoubleLiteral": return node.value; case "ExprDecimalLiteral": return node.value?.value ?? null; case "ExprByteArrayLiteral": return node.value === null ? null : Uint8Array.from(node.value); case "ExprGuidLiteral": return node.value; case "ExprDateTimeLiteral": case "ExprDateTimeOffsetLiteral": return node.value?.value ?? null; default: throw new Error(`Expression ${node.kind} cannot be used as a SQL parameter value.`); } }
  private value(node: ExprValue): string { return this.render(node); }
  private boolean(node: ExprBoolean): string { return this.render(node); }
  private tableSource(node: IExprTableSource): string { return this.render(node); }
  private columnSource(node: IExprColumnSource): string { if (node.kind === "ExprTableAlias") return node.alias.kind === "ExprAlias" ? this.quote(node.alias.name) : this.quote(node.alias.id); return this.tableName(node); }
  private rawColumnSource(node: IExprColumnSource): string { if (node.kind === "ExprTableAlias") return node.alias.kind === "ExprAlias" ? node.alias.name : node.alias.id; return node.kind === "ExprTableFullName" ? node.tableName.name : node.name; }
  private querySpecification(node: Extract<Expr, { readonly kind: "ExprQuerySpecification" }>, includeLimit: boolean): string {
    const previous = this.implicitColumnSource; const previousSources = this.implicitColumnSources;
    this.implicitColumnSource = node.from?.kind === "ExprCteQuery" ? (node.from.alias?.alias.kind === "ExprAlias" ? node.from.alias.alias.name : node.from.name) : null;
    this.implicitColumnSources = this.cteColumnSources(node.from);
    const result = `SELECT${node.distinct ? " DISTINCT" : ""}${node.top === null || this.options.dialect !== "tsql" ? "" : ` TOP ${this.value(node.top)}`} ${node.selectList.map((item) => this.selectingWithDerivedName(item)).join(",")}${node.from === null ? "" : ` FROM ${this.tableSource(node.from)}`}${node.where === null ? "" : ` WHERE ${this.boolean(node.where)}`}${node.groupBy === null ? "" : ` GROUP BY ${node.groupBy.map((item) => this.value(item)).join(",")}`}${includeLimit && node.top !== null && this.options.dialect !== "tsql" ? ` LIMIT ${this.value(node.top)}` : ""}`;
    this.implicitColumnSource = previous; this.implicitColumnSources = previousSources; return result;
  }
  private selectingWithDerivedName(node: IExprSelecting): string { if (node.kind === "ExprSum" && node.right.kind === "ExprColumn") return `${this.selecting(node)} ${this.quote(node.right.columnName.name)}`; return this.selecting(node); }
  private cteColumnSources(source: IExprTableSource | null): ReadonlyMap<string, string> { const result = new Map<string, string>(); const visit = (item: IExprTableSource): void => { if (item.kind === "ExprCteQuery") { const owner = item.alias?.alias.kind === "ExprAlias" ? item.alias.alias.name : item.name; const query = item.query.kind === "ExprSelectOffsetFetch" ? item.query.selectQuery : item.query; if (query.kind === "ExprQuerySpecification") for (const selected of query.selectList) { const name = selected.kind === "ExprAliasedSelecting" ? selected.alias.name : selected.kind === "ExprColumn" ? selected.columnName.name : null; if (name !== null) result.set(name.toLocaleLowerCase("en-US"), owner); } } else if (item.kind === "ExprCrossedTable" || item.kind === "ExprJoinedTable" || item.kind === "ExprLateralCrossedTable") { visit(item.left); visit(item.right); } }; if (source !== null) visit(source); return result; }
  private update(node: Extract<Expr, { readonly kind: "ExprUpdate" }>): string {
    const previous = this.implicitColumnSource; const previousSources = this.implicitColumnSources; const previousRemap = this.columnSourceRemap;
    const targetName = node.target.fullName.kind === "ExprTableFullName" ? node.target.fullName.tableName.name : node.target.fullName.name;
    const targetAlias = node.target.alias?.alias.kind === "ExprAlias" ? node.target.alias.alias.name : targetName;
    this.implicitColumnSource = node.source === null ? null : targetName; this.implicitColumnSources = this.cteColumnSources(node.source);
    if (this.options.dialect === "sqlite" && targetAlias !== targetName) this.columnSourceRemap = new Map([[targetAlias.toLocaleLowerCase("en-US"), targetName]]);
    let result: string;
    if (this.options.dialect === "tsql") { const inferredSource = node.source === null && node.target.alias !== null && node.setClause.some((clause) => [...walk(clause.value)].some((item) => item.kind === "ExprTable")) ? node.target : node.source; result = `UPDATE ${node.target.alias === null ? this.tableName(node.target.fullName) : this.columnSource(node.target.alias)} SET ${node.setClause.map((item) => `${this.render(item.column)}=${this.render(item.value)}`).join(",")}${inferredSource === null ? "" : ` FROM ${this.tableSource(inferredSource)}`}${node.filter === null ? "" : ` WHERE ${this.boolean(node.filter)}`}`; }
    else if (this.options.dialect === "mysql") {
      if (node.source?.kind === "ExprJoinedTable" && node.source.right.kind === "ExprDerivedTableValues") {
        const source = node.source.right; const alias = source.alias.alias.kind === "ExprAlias" ? source.alias.alias.name : "A1"; const temp = "t00000000000000000000000000000000"; const rows = source.values.items;
        const type = (index: number): string => { const values = rows.map(row => row.items[index]!); if (values.every(value => value.kind === "ExprInt32Literal")) return "int"; if (values.every(value => value.kind === "ExprStringLiteral")) return `varchar(${Math.max(1, ...values.map(value => value.kind === "ExprStringLiteral" ? value.value?.length ?? 0 : 0))}) character set utf8mb4`; return "text"; };
        const key = node.source.searchCondition.kind === "ExprBooleanEq" && node.source.searchCondition.right.kind === "ExprColumn" ? node.source.searchCondition.right.columnName.name : source.columns[0]!.name;
        const create = `CREATE TEMPORARY TABLE ${this.quote(temp)}(${source.columns.map((column, index) => `${this.quote(column.name)} ${type(index)}`).join(",")},CONSTRAINT PRIMARY KEY (${this.quote(key)}));`;
        const insert = `INSERT INTO ${this.quote(temp)}(${source.columns.map(column => this.quote(column.name)).join(",")}) VALUES ${rows.map(row => `(${row.items.map(item => this.value(item)).join(",")})`).join(",")};`;
        const update = `UPDATE ${this.tableSource(node.source.left)} JOIN ${this.quote(temp)} ${this.quote(alias)} ON ${this.boolean(node.source.searchCondition)} SET ${node.setClause.map(item => `${this.render(item.column)}=${this.render(item.value)}`).join(",")}${node.filter === null ? "" : ` WHERE ${this.boolean(node.filter)}`};`;
        result = `${create}${insert}${update}DROP TABLE ${this.quote(temp)};`;
      } else { let updateSource: string; if (node.source?.kind === "ExprJoinedTable" && node.source.right.kind === "ExprCteQuery") { const cte = node.source.right; const discovered = new Map<string, Extract<Expr, { readonly kind: "ExprCteQuery" }>>(); for (const item of walk(cte)) if (item.kind === "ExprCteQuery") { const key = item.name.toLocaleLowerCase("en-US"); if (!discovered.has(key)) discovered.set(key, item); } const definitions = [...discovered.values()].reverse(); const recursive = definitions.some(item => [...walk(item.query)].some(child => child.kind === "ExprCteQuery" && child.name.toLocaleLowerCase("en-US") === item.name.toLocaleLowerCase("en-US"))); const withSql = definitions.map(item => `${this.quote(item.name)} AS(${this.render(item.query)})`).join(","); updateSource = `${this.tableSource(node.source.left)} ${node.source.joinType === "Inner" ? "" : `${node.source.joinType.toUpperCase()} `}JOIN (WITH${recursive ? " RECURSIVE" : ""} ${withSql} SELECT * FROM ${this.quote(cte.name)})${this.quote(cte.alias?.alias.kind === "ExprAlias" ? cte.alias.alias.name : cte.name)} ON ${this.boolean(node.source.searchCondition)}`; } else updateSource = (node.source?.kind === "ExprJoinedTable" || node.source?.kind === "ExprLateralCrossedTable") ? this.tableSource(node.source) : node.source?.kind === "ExprCteQuery" ? this.tableSource(node.source) : this.tableSource(node.target); result = `UPDATE ${updateSource} SET ${node.setClause.map((item) => `${this.render(item.column)}=${this.render(item.value)}`).join(",")}${node.filter === null ? "" : ` WHERE ${this.boolean(node.filter)}`}`; }
    }
    else {
      const isTargetSource = (source: IExprTableSource | null): boolean => source?.kind === "ExprTable" && source.fullName.kind === "ExprTableFullName" && source.fullName.tableName.name.toLocaleLowerCase("en-US") === targetName.toLocaleLowerCase("en-US");
      if (this.options.dialect === "postgresql" && node.source !== null) {
        const sources: IExprTableSource[] = []; const joins: ExprBoolean[] = [];
        const scan = (source: IExprTableSource, root = false): void => {
          if (source.kind === "ExprCrossedTable") { scan(source.left); scan(source.right); return; }
          if (source.kind === "ExprLateralCrossedTable" && isTargetSource(source.left)) { sources.push(source.right); return; }
          if (source.kind === "ExprJoinedTable" && source.joinType === "Inner") { scan(source.left); scan(source.right); joins.push(source.searchCondition); return; }
          if (source.kind === "ExprCteQuery" && root) return;
          if (!isTargetSource(source)) sources.push(source);
        };
        scan(node.source, true);
        const target = `${this.tableName(node.target.fullName)}${node.target.alias !== null ? ` ${this.columnSource(node.target.alias)}` : ""}`;
        const assignments = node.setClause.map(item => `${this.quote(item.column.columnName.name)}=${this.render(item.value)}`).join(",");
        const conditions = [...joins, ...(node.filter === null ? [] : [node.filter])];
        const renderSource = (item: IExprTableSource): string => item.kind === "ExprAliasedTableFunction" ? this.render(item.function) : this.tableSource(item);
        result = `UPDATE ${target} SET ${assignments}${sources.length === 0 ? "" : ` FROM ${sources.map(renderSource).join(",")}`}${conditions.length === 0 ? "" : ` WHERE ${conditions.map(item => this.boolean(item)).join(" AND ")}`}`;
        this.implicitColumnSource = previous; this.implicitColumnSources = previousSources; this.columnSourceRemap = previousRemap; return result;
      }
      const joined = node.source?.kind === "ExprJoinedTable" ? node.source : null;
      const lateral = node.source?.kind === "ExprLateralCrossedTable" && isTargetSource(node.source.left) ? node.source.right : null;
      const from = joined?.right ?? lateral ?? (this.options.dialect === "postgresql" && node.source?.kind === "ExprCteQuery" ? null : isTargetSource(node.source) ? null : node.source);
      const target = `${this.tableName(node.target.fullName)}${this.options.dialect === "postgresql" && node.target.alias !== null ? ` ${this.columnSource(node.target.alias)}` : ""}`;
      const assignments = node.setClause.map((item) => `${this.quote(item.column.columnName.name)}=${this.render(item.value)}`).join(",");
      const conditions = [joined?.searchCondition ?? null, node.filter].filter((item): item is ExprBoolean => item !== null);
      const activeRemap = this.columnSourceRemap; this.columnSourceRemap = previousRemap; const fromSql = from === null ? "" : ` FROM ${from.kind === "ExprAliasedTableFunction" ? this.render(from.function) : this.tableSource(from)}`; this.columnSourceRemap = activeRemap;
      result = `UPDATE ${target} SET ${assignments}${fromSql}${conditions.length === 0 ? "" : ` WHERE ${conditions.map((item) => this.boolean(item)).join(" AND ")}`}`;
    }
    this.implicitColumnSource = previous; this.implicitColumnSources = previousSources; this.columnSourceRemap = previousRemap; return result;
  }
  private delete(node: Extract<Expr, { readonly kind: "ExprDelete" }>): string {
    const targetName = node.target.fullName.kind === "ExprTableFullName" ? node.target.fullName.tableName.name : node.target.fullName.name;
    const targetAlias = node.target.alias?.alias.kind === "ExprAlias" ? node.target.alias.alias.name : null;
    const previousRemap = this.columnSourceRemap; const previousImplicit = this.implicitColumnSource;
    if ((this.options.dialect === "mysql" || this.options.dialect === "sqlite") && node.source === null && targetAlias !== null) { this.columnSourceRemap = new Map([[targetAlias.toLocaleLowerCase("en-US"), this.options.dialect === "mysql" ? "" : targetName]]); this.implicitColumnSource = null; }
    let result: string;
    if (node.source === null) {
      if (this.options.dialect === "tsql" && targetAlias !== null) result = `DELETE ${this.quote(targetAlias)} FROM ${this.tableSource(node.target)}`;
      else result = `DELETE${this.options.dialect === "tsql" ? " " : " FROM "}${this.tableName(node.target.fullName)}${this.options.dialect === "postgresql" && targetAlias !== null ? ` ${this.quote(targetAlias)}` : ""}`;
      if (node.filter !== null) result += ` WHERE ${this.boolean(node.filter)}`;
    } else if (this.options.dialect === "sqlite") throw new Error("SQLite exporter does not support DELETE with source tables");
    else if (this.options.dialect === "postgresql") {
      const joined = node.source.kind === "ExprJoinedTable" ? node.source : null; const using = joined?.right ?? node.source; const conditions = [joined?.searchCondition ?? null, node.filter].filter((item): item is ExprBoolean => item !== null);
      result = `DELETE FROM ${this.tableSource(node.target)} USING ${this.tableSource(using)}${conditions.length === 0 ? "" : ` WHERE ${conditions.map((item) => this.boolean(item)).join(" AND ")}`}`;
    } else {
      result = `DELETE ${this.quote(targetAlias ?? targetName)} FROM ${this.tableSource(node.source)}${node.filter === null ? "" : ` WHERE ${this.boolean(node.filter)}`}`;
    }
    this.columnSourceRemap = previousRemap; this.implicitColumnSource = previousImplicit; return result;
  }
  private productOperand(node: ExprValue): string { return node.kind === "ExprSum" || node.kind === "ExprSub" ? `(${this.value(node)})` : this.value(node); }
  private derivedValues(node: Extract<Expr, { readonly kind: "ExprDerivedTableValues" }>): string { if (this.options.dialect === "mysql") return `(${node.values.items.map((row, rowIndex) => `SELECT ${row.items.map((item, index) => `${this.value(item)}${rowIndex === 0 ? ` ${this.quote(node.columns[index]!.name)}` : ""}`).join(",")}`).join(" UNION ALL ")})${this.columnSource(node.alias)}`; if (this.options.dialect === "sqlite") return `(SELECT ${node.columns.map((column, index) => `column${index + 1} AS ${this.quote(column.name)}`).join(",")} FROM (VALUES ${node.values.items.map((row) => `(${row.items.map((item) => this.value(item)).join(",")})`).join(",")})) AS ${this.columnSource(node.alias)}`; return `(VALUES ${node.values.items.map((row) => `(${row.items.map((item) => this.value(item)).join(",")})`).join(",")})${this.columnSource(node.alias)}(${node.columns.map((column) => this.quote(column.name)).join(",")})`; }
  private mysqlSubQuery(node: Expr): string { if (this.renderingWithRootCtes) return this.render(node); const ctes = new Map<string, Extract<Expr, { readonly kind: "ExprCteQuery" }>>(); for (const item of walk(node)) if (item.kind === "ExprCteQuery" && !ctes.has(item.name.toLocaleLowerCase("en-US"))) ctes.set(item.name.toLocaleLowerCase("en-US"), item); return ctes.size === 0 ? this.render(node) : `WITH ${[...ctes.values()].map((cte) => `${this.quote(cte.name)} AS(${this.render(cte.query)})`).join(",")} ${this.render(node)}`; }
  private tableName(node: IExprTableFullName): string {
    if (node.kind === "ExprTempTableName") return this.quote(node.name);
    const schema = node.dbSchema?.schema.name; const database = node.dbSchema?.database?.name;
    const mapped = schema === undefined || this.options.dialect === "mysql" || this.options.dialect === "sqlite" ? null : this.options.schemaMap?.find((item) => item.from.toLocaleLowerCase("en-US") === schema.toLocaleLowerCase("en-US"))?.to ?? schema;
    const databasePrefix = database === undefined || this.options.dialect === "sqlite" ? "" : `${this.quote(database)}.`;
    return `${databasePrefix}${mapped === null ? "" : `${this.quote(mapped)}.`}${this.quote(node.tableName.name)}`;
  }
  private sqlType(type: ExprType, castContext = true): string {
    if (this.options.dialect === "sqlite") { switch (type.kind) { case "ExprTypeInt16": case "ExprTypeInt32": case "ExprTypeInt64": case "ExprTypeBoolean": return "INTEGER"; case "ExprTypeDecimal": case "ExprTypeDouble": return "NUMERIC"; case "ExprTypeByteArray": case "ExprTypeFixSizeByteArray": return "BLOB"; default: return "TEXT"; } }
    switch (type.kind) {
      case "ExprTypeInt16": return this.options.dialect === "postgresql" ? "int2" : "smallint";
      case "ExprTypeInt32": return this.options.dialect === "postgresql" ? "int4" : "int";
      case "ExprTypeInt64": return this.options.dialect === "postgresql" ? "int8" : this.options.dialect === "mysql" ? "SIGNED" : "bigint";
      case "ExprTypeBoolean": return this.options.dialect === "tsql" ? "bit" : "boolean";
      case "ExprTypeDouble": return this.options.dialect === "postgresql" ? "double precision" : "float";
      case "ExprTypeGuid": return this.options.dialect === "postgresql" ? "uuid" : this.options.dialect === "tsql" ? "uniqueidentifier" : "char(36)";
      case "ExprTypeXml": return "xml";
      case "ExprTypeDateTime": return type.isDate ? "date" : this.options.dialect === "postgresql" ? "timestamp" : "datetime";
      case "ExprTypeDateTimeOffset": return this.options.dialect === "postgresql" ? "timestamp with time zone" : "datetimeoffset";
      case "ExprTypeDecimal": { if (this.options.dialect === "mysql" && type.precisionScale !== null) throw new Error(`MySQL does not support casting to decimal(${type.precisionScale.precision},${type.precisionScale.scale})`); return type.precisionScale === null ? "decimal" : `decimal(${type.precisionScale.precision},${type.precisionScale.scale})`; }
      case "ExprTypeString": { if (this.options.dialect === "mysql") return castContext ? `CHAR(${type.size ?? 255})` : `${type.isText ? "text" : `varchar(${type.size ?? 255})`}${type.isUnicode ? " character set utf8mb4" : ""}`; if (this.options.dialect === "postgresql") return type.isText ? "text" : `character varying${type.size === null ? "" : `(${type.size})`}`; const base = type.isText ? (type.isUnicode ? "ntext" : "text") : type.isUnicode ? "nvarchar" : "varchar"; return type.isText ? this.quote(base) : `${this.quote(base)}(${type.size ?? "MAX"})`; }
      case "ExprTypeByte": return "tinyint";
      case "ExprTypeByteArray": return this.options.dialect === "postgresql" ? "bytea" : `varbinary(${type.size ?? "MAX"})`;
      case "ExprTypeFixSizeByteArray": return this.options.dialect === "postgresql" ? "bytea" : `binary(${type.size})`;
      case "ExprTypeFixSizeString": return this.options.dialect === "postgresql" ? `character(${type.size})` : `${this.options.dialect === "tsql" ? this.quote(type.isUnicode ? "nchar" : "char") : "char"}(${type.size})`;
    }
  }
  private over(node: Extract<Expr, { readonly kind: "ExprOver" }>): string { return `OVER(${node.partitions === null ? "" : `PARTITION BY ${node.partitions.map((item) => this.value(item)).join(",")}${node.orderBy === null ? "" : " "}`}${node.orderBy === null ? "" : `ORDER BY ${node.orderBy.orderList.map((item) => `${this.value(item.value)}${item.descendant ? " DESC" : ""}`).join(",")}`})`; }
  private stringAgg(node: Extract<Expr, { readonly kind: "ExprStringAgg" }>): string {
    const expression = this.value(node.expression); const separatorNode = node.separator.kind === "ExprParameter" && node.separator.replacedValue !== null ? node.separator.replacedValue : node.separator;
    if (this.options.dialect === "mysql" && (separatorNode.kind !== "ExprStringLiteral" || separatorNode.value === null)) throw new Error("MySQL STRING_AGG separator must be a non-null string literal.");
    const separator = this.value(separatorNode); const order = node.orderBy === null ? "" : node.orderBy.orderList.map((item) => `${this.value(item.value)}${item.descendant ? " DESC" : ""}`).join(",");
    if (this.options.dialect === "tsql") return `STRING_AGG(${expression},${separator})${order === "" ? "" : ` WITHIN GROUP (ORDER BY ${order})`}`;
    if (this.options.dialect === "postgresql") return `STRING_AGG(${expression},${separator}${order === "" ? "" : ` ORDER BY ${order}`})`;
    if (this.options.dialect === "mysql") return `GROUP_CONCAT(${expression}${order === "" ? "" : ` ORDER BY ${order}`} SEPARATOR ${separator})`;
    return `GROUP_CONCAT(${expression},${separator}${order === "" ? "" : ` ORDER BY ${order}`})`;
  }
  private portableScalar(node: Extract<Expr, { readonly kind: "ExprPortableScalarFunction" }>): string {
    const args = node.arguments?.map((item) => this.value(item)) ?? []; const call = (name: string, values = args) => `${name}(${values.join(",")})`;
    switch (node.portableFunction) {
      case "Len": return call(this.options.dialect === "tsql" ? "LEN" : this.options.dialect === "sqlite" ? "LENGTH" : "CHAR_LENGTH");
      case "DataLen": return this.options.dialect === "tsql" ? call("DATALENGTH") : this.options.dialect === "postgresql" || this.options.dialect === "mysql" ? call("OCTET_LENGTH") : `LENGTH(CAST(${args[0]} AS BLOB))`;
      case "IndexOf": return this.options.dialect === "tsql" ? call("CHARINDEX") : this.options.dialect === "postgresql" ? call("STRPOS", [args[1]!, args[0]!]) : this.options.dialect === "mysql" ? call("LOCATE") : call("INSTR", [args[1]!, args[0]!]);
      case "Left": return this.options.dialect === "sqlite" ? `SUBSTR(${args[0]},1,MAX(${args[1]},0))` : call("LEFT");
      case "Right": return this.options.dialect === "sqlite" ? `CASE WHEN ${args[1]}<=0 THEN '' ELSE SUBSTR(${args[0]},-(${args[1]})) END` : call("RIGHT");
      case "Repeat": return this.options.dialect === "tsql" ? call("REPLICATE") : this.options.dialect === "sqlite" ? `CASE WHEN ${args[1]}<=0 THEN '' ELSE REPLACE(HEX(ZEROBLOB(${args[1]})),'00',${args[0]}) END` : call("REPEAT");
      case "Substring": return call(this.options.dialect === "sqlite" ? "SUBSTR" : "SUBSTRING");
      case "Ceiling": { if (this.options.dialect === "postgresql") return call("CEIL"); if (this.options.dialect === "sqlite") return `CASE WHEN ${args[0]}<=CAST(${args[0]} AS INTEGER) THEN CAST(${args[0]} AS INTEGER) ELSE CAST(${args[0]} AS INTEGER)+1 END`; return call("CEILING"); }
      case "Floor": return this.options.dialect === "sqlite" ? `CASE WHEN ${args[0]}>=CAST(${args[0]} AS INTEGER) THEN CAST(${args[0]} AS INTEGER) ELSE CAST(${args[0]} AS INTEGER)-1 END` : call("FLOOR");
      case "Year": case "Month": case "Day": { const part = node.portableFunction.toUpperCase(); if (this.options.dialect === "postgresql") return `EXTRACT(${part} FROM ${args[0]})`; if (this.options.dialect === "sqlite") return `CAST(STRFTIME('${part === "YEAR" ? "%Y" : part === "MONTH" ? "%m" : "%d"}',${args[0]}) AS INTEGER)`; return call(part); }
      default: return call(node.portableFunction.toUpperCase());
    }
  }
  private limit(node: Expr): string { return this.options.dialect !== "tsql" && node.kind === "ExprQuerySpecification" && node.top !== null ? ` LIMIT ${this.value(node.top)}` : ""; }
  private dateAdd(node: Extract<Expr, { readonly kind: "ExprDateAdd" }>): string {
    const date = this.value(node.date); const unit = node.datePart.toUpperCase(); const number = node.number;
    if (this.options.dialect === "tsql") { const part = ({ Day: "d", Hour: "hh", Millisecond: "ms", Minute: "mi", Month: "m", Second: "s", Week: "wk", Year: "yy" } as const)[node.datePart]; return `DATEADD(${part},${number},${date})`; }
    if (this.options.dialect === "postgresql") { const unit = ({ Day: "d", Hour: "h", Millisecond: "milliseconds", Minute: "m", Month: "month", Second: "s", Week: "weeks", Year: "y" } as const)[node.datePart]; return `${date}${number < 0 ? "-" : "+"}INTERVAL'${Math.abs(number)}${unit}'`; }
    if (this.options.dialect === "mysql") return `DATE_ADD(${date},INTERVAL ${number} ${unit})`;
    if (node.datePart === "Month" || node.datePart === "Year") { const months = node.datePart === "Year" ? number * 12 : number; const signed = months >= 0 ? `+${months}` : String(months); const next = months + 1 >= 0 ? `+${months + 1}` : String(months + 1); return `DATETIME(printf('%s-%02d',STRFTIME('%Y-%m',DATE(${date},'start of month','${signed} months')),MIN(CAST(STRFTIME('%d',${date}) AS INTEGER),CAST(STRFTIME('%d',DATE(${date},'start of month','${next} months','-1 day')) AS INTEGER)))||SUBSTR(STRFTIME('%Y-%m-%d %H:%M:%f',${date}),11))`; }
    return `DATETIME(${date},'${number >= 0 ? "+" : ""}${number} ${node.datePart.toLowerCase()}s')`;
  }
  private dateDiff(node: Extract<Expr, { readonly kind: "ExprDateDiff" }>): string {
    const start = this.value(node.startDate); const end = this.value(node.endDate); const unit = node.datePart.toUpperCase();
    if (this.options.dialect === "tsql") return `DATEDIFF(${unit},${start},${end})`;
    if (this.options.dialect === "postgresql") { const pgDate = (value: ExprValue, sql: string) => value.kind === "ExprStringLiteral" ? `CAST(${sql} AS timestamp)` : sql; return `CAST(DATE_PART('${unit}',DATE_TRUNC('${unit}',${pgDate(node.endDate, end)})-DATE_TRUNC('${unit}',${pgDate(node.startDate, start)})) AS int4)`; }
    if (this.options.dialect === "mysql") return node.datePart === "Day" ? `DATEDIFF(${end},${start})` : `TIMESTAMPDIFF(${unit},${start},${end})`;
    if (node.datePart === "Day") return `CAST((CAST(STRFTIME('%s',DATE(${end})) AS INTEGER)-CAST(STRFTIME('%s',DATE(${start})) AS INTEGER))/86400 AS INTEGER)`;
    return `CAST((JULIANDAY(${end})-JULIANDAY(${start})) AS INTEGER)`;
  }
  private jsonOutputName(path: string): string { const names = [...path.matchAll(/\.(?:"((?:[^"\\]|\\.)*)"|([\p{L}_][\p{L}\p{N}_]*))/gu)].map(match => (match[1] ?? match[2]!).split('\\"').join('"').split("\\\\").join("\\")); return names.join("."); }
  private queryAsJson(node: Extract<Expr, { readonly kind: "ExprQueryAsJson" }>): string {
    if (this.options.dialect === "tsql") {
      if (node.query.kind === "ExprQuerySpecification") { const names = node.query.selectList.filter((item): item is Extract<IExprSelecting, { readonly kind: "ExprJsonOutputColumn" }> => item.kind === "ExprJsonOutputColumn").map(item => this.jsonOutputName(item.jsonPath)); if (new Set(names).size !== names.length) throw new Error("FOR JSON output paths must be unique."); }
      const previous = this.renderingJsonQuery; this.renderingJsonQuery = true; let rendered: string; try { rendered = this.render(node.query); } finally { this.renderingJsonQuery = previous; }
      const query = `${rendered} FOR JSON PATH${node.includeNullValues ? ", INCLUDE_NULL_VALUES" : ""}`;
      if (!node.withoutArrayWrapper) return query;
      return `SELECT CASE WHEN JSON_QUERY(J1.Json,'$[0]') IS NULL THEN NULL WHEN JSON_QUERY(J1.Json,'$[1]') IS NOT NULL THEN SUBSTRING(J1.Json,1,-1) ELSE JSON_QUERY(J1.Json,'$[0]') END Json FROM (SELECT (${query}) Json) J1`;
    }
    type JsonBranch = { value?: string; children: Map<string, JsonBranch> };
    const root: JsonBranch = { children: new Map() }; const projections: string[] = []; let temporary = 0;
    if (node.query.kind === "ExprQuerySpecification") for (const item of node.query.selectList) {
      const name = item.kind === "ExprJsonOutputColumn" ? this.jsonOutputName(item.jsonPath) : item.kind === "ExprAliasedSelecting" ? item.alias.name : item.kind === "ExprAliasedColumn" ? item.alias?.name ?? item.column.columnName.name : null;
      if (name === null) continue;
      const parts = name.split("."); const projected = parts.length === 1 ? name : `__sq_json_${temporary++}`;
      let expression: string;
      if (item.kind === "ExprJsonOutputColumn") expression = this.selecting(item.value);
      else if (item.kind === "ExprAliasedSelecting") expression = this.selecting(item.value);
      else if (item.kind === "ExprAliasedColumn") expression = this.render(item.column);
      else continue;
      projections.push(`${expression} ${this.quote(projected)}`);
      let branch = root; for (const part of parts) { let child = branch.children.get(part); if (child === undefined) { child = { children: new Map() }; branch.children.set(part, child); } branch = child; } branch.value = `J0.${this.quote(projected)}`;
    }
    const object = (branch: JsonBranch): string => { const entries = [...branch.children].flatMap(([name, child]) => [`'${name.split("'").join("''")}'`, child.value ?? object(child)]); if (this.options.dialect === "postgresql") return `jsonb_build_object(${entries.join(",")})`; if (this.options.dialect === "mysql") return `JSON_OBJECT(${entries.join(",")})`; return `json_object(${entries.join(",")})`; };
    const source = node.query.kind === "ExprQuerySpecification" ? `SELECT ${projections.join(",")}${node.query.from === null ? "" : ` FROM ${this.tableSource(node.query.from)}`}${node.query.where === null ? "" : ` WHERE ${this.boolean(node.query.where)}`}` : this.render(node.query);
    const jsonObject = object(root);
    if (node.withoutArrayWrapper) {
      if (this.options.dialect === "postgresql") return `SELECT (SELECT ${node.includeNullValues ? jsonObject : `jsonb_strip_nulls(${jsonObject})`} FROM (${source}) J0) Json`;
      if (this.options.dialect === "mysql") return `SELECT (SELECT ${node.includeNullValues ? jsonObject : `JSON_MERGE_PATCH(JSON_OBJECT(),${jsonObject})`} FROM (${source}) J0) Json`;
      const value = node.includeNullValues ? jsonObject : `json_patch('{}',${jsonObject})`; return `SELECT CASE COUNT(*) WHEN 0 THEN NULL WHEN 1 THEN json_extract(json_group_array(json(${value})),'$[0]') ELSE json('!') END Json FROM (${source}) J0`;
    }
    if (this.options.dialect === "postgresql") return `SELECT COALESCE(jsonb_agg(${node.includeNullValues ? jsonObject : `jsonb_strip_nulls(${jsonObject})` }),'[]'::jsonb) Json FROM (${source}) J0`;
    if (this.options.dialect === "mysql") return `SELECT COALESCE(JSON_ARRAYAGG(${node.includeNullValues ? jsonObject : `JSON_MERGE_PATCH(JSON_OBJECT(),${jsonObject})`}),JSON_ARRAY()) Json FROM (${source}) J0`;
    return `SELECT COALESCE(json_group_array(json(${node.includeNullValues ? jsonObject : `json_patch('{}',${jsonObject})`})),'[]') Json FROM (${source}) J0`;
  }
  private jsonValue(node: Extract<Expr, { readonly kind: "ExprJsonValue" }>): string {
    const document = this.value(node.document); const path = `'${node.path.split("'").join("''")}'`;
    if (this.options.dialect === "tsql") {
      if (node.returningType === null) return `JSON_VALUE(${document},${path})`;
      const segments = [...node.path.matchAll(/\.(?:"((?:[^"\\]|\\.)*)"|([\p{L}_][\p{L}\p{N}_]*))|\[(\d+)\]/gu)].map(match => match[1] ?? match[2] ?? match[3]!);
      if (segments.length > 1) { const key = segments.at(-1)!; const parent = `$.${segments.slice(0, -1).map(item => `"${item.split('"').join('\\"')}"`).join(".")}`; const typeFilter = node.returningType.kind === "ExprTypeBoolean" ? " AND [type]=3" : ""; return `(SELECT TRY_CONVERT(${this.openJsonType(node.returningType)},[value]) FROM OPENJSON(${document},'${parent}') WHERE [key]='${key.split("'").join("''")}'${typeFilter})`; }
      return `(SELECT TRY_CONVERT(${this.openJsonType(node.returningType)},[value]) FROM OPENJSON(${document},${path}))`;
    }
    if (this.options.dialect === "postgresql") { const value = `jsonb_path_query_first(CAST(${document} AS jsonb),${path})`; return node.returningType === null ? `CASE WHEN jsonb_typeof(${value})='string' THEN CAST(${value} #>> '{}' AS character varying) END` : `CAST(${value} #>> '{}' AS ${this.pgJsonType(node.returningType)})`; }
    if (this.options.dialect === "mysql") { const value = `JSON_EXTRACT(${document},${path})`; return node.returningType === null ? `CASE WHEN JSON_TYPE(${value})='STRING' THEN JSON_UNQUOTE(${value}) END` : `CAST(JSON_UNQUOTE(${value}) AS ${this.openJsonType(node.returningType)})`; }
    return node.returningType === null ? `CASE WHEN json_type(${document},${path}) IN ('text') THEN json_extract(${document},${path}) END` : `CAST(json_extract(${document},${path}) AS ${node.returningType.kind === "ExprTypeInt32" || node.returningType.kind === "ExprTypeInt16" || node.returningType.kind === "ExprTypeInt64" ? "INTEGER" : node.returningType.kind === "ExprTypeDecimal" || node.returningType.kind === "ExprTypeDouble" ? "REAL" : "TEXT"})`;
  }
  private jsonQuery(node: Extract<Expr, { readonly kind: "ExprJsonQuery" }>): string {
    const document = this.value(node.document); const path = `'${node.path.split("'").join("''")}'`;
    if (this.options.dialect === "tsql") return `JSON_QUERY(${document},${path})`;
    if (this.options.dialect === "postgresql") { const value = `jsonb_path_query_first(CAST(${document} AS jsonb),${path})`; return `CASE WHEN jsonb_typeof(${value}) IN ('object','array') THEN ${value} END`; }
    if (this.options.dialect === "mysql") { const value = `JSON_EXTRACT(${document},${path})`; return `CASE WHEN JSON_TYPE(${value}) IN ('OBJECT','ARRAY') THEN ${value} END`; }
    return `CASE WHEN json_type(${document},${path}) IN ('object','array') THEN json(json_extract(${document},${path})) END`;
  }
  private jsonTable(node: Extract<Expr, { readonly kind: "ExprJsonTable" }>): string {
    const document = this.value(node.document); const path = `'${node.path.split("'").join("''")}'`; const alias = this.columnSource(node.alias);
    if (this.options.dialect === "tsql") return `OPENJSON(${document},${path}) WITH (${node.columns.map((column) => column.kind === "ExprJsonTableQueryColumn" ? `${this.quote(column.name.name)} nvarchar(max) '${column.path}' AS JSON` : column.kind === "ExprJsonTableOrdinalColumn" ? `${this.quote(column.name.name)} int '$.__ordinal'` : `${this.quote(column.name.name)} ${this.openJsonType(column.sqlType)} '${column.path}'`).join(",")}) ${alias}`;
    if (this.options.dialect === "postgresql") return `(SELECT ${node.columns.map((column) => column.kind === "ExprJsonTableValueColumn" ? `CAST(jsonb_path_query_first(J.value,'${column.path}')#>>'{}' AS ${this.pgJsonType(column.sqlType)}) ${this.quote(column.name.name)}` : column.kind === "ExprJsonTableQueryColumn" ? `jsonb_path_query_first(J.value,'${column.path}') ${this.quote(column.name.name)}` : `J.ordinal ${this.quote(column.name.name)}`).join(",")} FROM jsonb_array_elements(jsonb_path_query_first(CAST(${document} AS jsonb),${path})) WITH ORDINALITY J(value,ordinal)) ${alias}`;
    if (this.options.dialect === "mysql") return `(SELECT ${node.columns.map((column) => `J.${this.quote(column.name.name)} ${this.quote(column.name.name)}`).join(",")} FROM JSON_TABLE(JSON_EXTRACT(${document},${path}),'$[*]' COLUMNS(${node.columns.map((column) => `${this.quote(column.name.name)} ${column.kind === "ExprJsonTableValueColumn" ? this.openJsonType(column.sqlType) : column.kind === "ExprJsonTableQueryColumn" ? "JSON" : "FOR ORDINALITY"}${column.kind === "ExprJsonTableOrdinalColumn" ? "" : ` PATH '${column.path}' NULL ON EMPTY NULL ON ERROR`}`).join(",")})) J) ${alias}`;
    return `(SELECT ${node.columns.map((column) => column.kind === "ExprJsonTableValueColumn" ? `CASE WHEN json_type(J.value,'${column.path}') IN ('integer') THEN CAST(json_extract(J.value,'${column.path}') AS INTEGER) END ${this.quote(column.name.name)}` : column.kind === "ExprJsonTableQueryColumn" ? `CASE WHEN json_type(J.value,'${column.path}') IN ('object','array') THEN json(json_extract(J.value,'${column.path}')) END ${this.quote(column.name.name)}` : `J.key ${this.quote(column.name.name)}`).join(",")} FROM json_each(${document},${path}) J) ${alias}`;
  }
  private openJsonType(type: ExprType): string { switch (type.kind) { case "ExprTypeInt16": return "smallint"; case "ExprTypeInt32": return "int"; case "ExprTypeInt64": return "bigint"; case "ExprTypeBoolean": return "bit"; case "ExprTypeDouble": return "float"; case "ExprTypeDecimal": return type.precisionScale === null ? "decimal(38,18)" : `decimal(${type.precisionScale.precision},${type.precisionScale.scale})`; case "ExprTypeString": return `${type.isUnicode ? "nvarchar" : "varchar"}(${type.size ?? "max"})`; default: throw new Error(`OPENJSON does not support ${type.kind}.`); } }
  private pgJsonType(type: ExprType): string { switch (type.kind) { case "ExprTypeInt16": return "int2"; case "ExprTypeInt32": return "int4"; case "ExprTypeInt64": return "int8"; case "ExprTypeBoolean": return "boolean"; case "ExprTypeDouble": return "float8"; case "ExprTypeDecimal": return "numeric"; default: return "character varying"; } }
  private mergeMatched(node: Extract<Expr, { readonly kind: "ExprMergeMatchedDelete" | "ExprMergeMatchedUpdate" }>): string { return node.kind === "ExprMergeMatchedDelete" ? "DELETE" : `UPDATE SET ${node.set.map((item) => `${this.render(item.column)}=${this.render(item.value)}`).join(",")}`; }
  private pgMerge(node: Extract<Expr, { readonly kind: "ExprMerge" }>): string {
    if (node.source.kind !== "ExprDerivedTableQuery" && node.source.kind !== "ExprDerivedTableValues") throw new Error("PostgreSQL MERGE polyfill requires a derived source.");
    const source = node.source;
    const query = source.kind === "ExprDerivedTableQuery" ? (source.query.kind === "ExprSelectOffsetFetch" ? source.query.selectQuery : source.query) : null;
    if (query !== null && query.kind !== "ExprQuerySpecification") throw new Error("PostgreSQL MERGE polyfill requires named source projections.");
    const names = source.kind === "ExprDerivedTableValues" ? source.columns.map((item) => item.name) : query!.selectList.map((item, index) => item.kind === "ExprAliasedSelecting" ? item.alias.name : item.kind === "ExprAliasedColumn" ? item.alias?.name ?? item.column.columnName.name : item.kind === "ExprColumn" ? item.columnName.name : `Expr${index + 1}`);
    const sourceName = "__sqexpress_merge_source"; const sourceAlias = source.alias.alias.kind === "ExprAlias" ? source.alias.alias.name : "s";
    const target = this.tableName(node.targetTable.fullName); const targetAlias = node.targetTable.alias === null ? "" : ` ${this.columnSource(node.targetTable.alias)}`; const on = this.boolean(node.on);
    const sourceSql = source.kind === "ExprDerivedTableValues" ? `VALUES ${source.values.items.map((row) => `(${row.items.map((item) => this.value(item)).join(",")})`).join(",")}` : this.render(source.query);
    const ctes: string[] = [`${this.quote(sourceName)}(${names.map((name) => this.quote(name)).join(",")}) AS(${sourceSql})`]; const counts: string[] = [];
    if (node.whenMatched?.kind === "ExprMergeMatchedUpdate") { const name = "__sqexpress_merge_matched"; ctes.push(`${this.quote(name)} AS(UPDATE ${target}${targetAlias} SET ${node.whenMatched.set.map((item) => `${this.quote(item.column.columnName.name)}=${this.render(item.value)}`).join(",")} FROM ${this.quote(sourceName)} ${this.quote(sourceAlias)} WHERE ${on} RETURNING 1)`); counts.push(`(SELECT COUNT(*) FROM ${this.quote(name)})`); }
    if (node.whenNotMatchedByTarget?.kind === "ExprExprMergeNotMatchedInsert") { const name = "__sqexpress_merge_not_matched_by_target"; ctes.push(`${this.quote(name)} AS(INSERT INTO ${target}(${node.whenNotMatchedByTarget.columns.map((column) => this.quote(column.name)).join(",")}) SELECT ${node.whenNotMatchedByTarget.values.map((value) => this.render(value)).join(",")} FROM ${this.quote(sourceName)} ${this.quote(sourceAlias)} WHERE NOT EXISTS(SELECT 1 FROM ${target}${targetAlias} WHERE ${on}) RETURNING 1)`); counts.push(`(SELECT COUNT(*) FROM ${this.quote(name)})`); }
    return `WITH ${ctes.join(",")} SELECT ${counts.join(",")}`;
  }
  private mysqlMerge(node: Extract<Expr, { readonly kind: "ExprMerge" }>): string {
    if (node.source.kind !== "ExprDerivedTableQuery" && node.source.kind !== "ExprDerivedTableValues") throw new Error("MySQL MERGE polyfill requires a derived source.");
    const source = node.source; const query = source.kind === "ExprDerivedTableQuery" ? (source.query.kind === "ExprSelectOffsetFetch" ? source.query.selectQuery : source.query) : null;
    if (query !== null && query.kind !== "ExprQuerySpecification") throw new Error("MySQL MERGE polyfill requires named source projections.");
    const projected = source.kind === "ExprDerivedTableValues" ? source.columns.map((column, index) => ({ name: column.name, value: source.values.items[0]!.items[index]! as IExprSelecting })) : query!.selectList.map((item) => item.kind === "ExprAliasedSelecting" ? { name: item.alias.name, value: item.value } : null);
    if (projected.some((item) => item === null) || node.on.kind !== "ExprBooleanEq") throw new Error("MySQL MERGE polyfill requires named projections and an equality key.");
    const temp = "tmpMergeDataSource"; const sourceAlias = source.alias.alias.kind === "ExprAlias" ? source.alias.alias.name : "s"; const target = this.tableName(node.targetTable.fullName); const targetAlias = node.targetTable.alias?.alias.kind === "ExprAlias" ? node.targetTable.alias.alias.name : "t";
    const key = node.on.right.kind === "ExprColumn" ? node.on.right.columnName.name : projected[0]!.name;
    const type = (value: IExprSelecting): string => value.kind === "ExprInt32Literal" ? "int" : value.kind === "ExprStringLiteral" ? `varchar(${Math.max(1, value.value?.length ?? 1)}) character set utf8mb4` : "text";
    const columns = projected.map((item) => `${this.quote(item!.name)} ${type(item!.value)}`);
    const create = `CREATE TEMPORARY TABLE ${this.quote(temp)}(${columns.join(",")},CONSTRAINT PRIMARY KEY (${this.quote(key)}));`;
    const insertSource = `INSERT INTO ${this.quote(temp)}(${projected.map((item) => this.quote(item!.name)).join(",")}) ${source.kind === "ExprDerivedTableValues" ? `VALUES ${source.values.items.map((row) => `(${row.items.map((item) => this.value(item)).join(",")})`).join(",")}` : this.render(source.query)};`;
    const matched = node.whenMatched?.kind === "ExprMergeMatchedUpdate" ? `UPDATE ${target} ${this.quote(targetAlias)} JOIN ${this.quote(temp)} ${this.quote(sourceAlias)} ON ${this.boolean(node.on)} SET ${node.whenMatched.set.map((item) => `${this.render(item.column)}=${this.render(item.value)}`).join(",")};DELETE ${this.quote(sourceAlias)} FROM ${this.quote(temp)} ${this.quote(sourceAlias)} JOIN ${target} ${this.quote(targetAlias)} ON ${this.boolean(node.on)};` : "";
    const notMatched = node.whenNotMatchedByTarget?.kind === "ExprExprMergeNotMatchedInsert" ? `INSERT INTO ${target}(${node.whenNotMatchedByTarget.columns.map((column) => this.quote(column.name)).join(",")}) SELECT ${node.whenNotMatchedByTarget.values.map((value) => this.render(value)).join(",")} FROM ${this.quote(temp)} ${this.quote(sourceAlias)};` : "";
    return `${create}${insertSource}${matched}${notMatched}DROP TABLE ${this.quote(temp)};`;
  }
  private sqliteMerge(node: Extract<Expr, { readonly kind: "ExprMerge" }>): string {
    if (node.source.kind !== "ExprDerivedTableQuery" && node.source.kind !== "ExprDerivedTableValues") throw new Error("SQLite MERGE polyfill requires a derived source.");
    const source = node.source; const query = source.kind === "ExprDerivedTableQuery" ? (source.query.kind === "ExprSelectOffsetFetch" ? source.query.selectQuery : source.query) : null;
    if ((query !== null && query.kind !== "ExprQuerySpecification") || node.on.kind !== "ExprBooleanEq") throw new Error("SQLite MERGE polyfill requires named projections and an equality key.");
    const projected = source.kind === "ExprDerivedTableValues" ? source.columns.map((column, index) => ({ name: column.name, value: source.values.items[0]!.items[index]! as IExprSelecting })) : query!.selectList.map((item) => item.kind === "ExprAliasedSelecting" ? { name: item.alias.name, value: item.value } : null);
    if (projected.some((item) => item === null)) throw new Error("SQLite MERGE polyfill requires named source projections.");
    const temp = "tmpMergeDataSource"; const originalSourceAlias = source.alias.alias.kind === "ExprAlias" ? source.alias.alias.name : "s"; const sourceAlias = /^A\d+$/.test(originalSourceAlias) ? "A0" : originalSourceAlias; const target = this.tableName(node.targetTable.fullName); const targetAlias = node.targetTable.alias?.alias.kind === "ExprAlias" ? node.targetTable.alias.alias.name : "t";
    const key = node.on.right.kind === "ExprColumn" ? node.on.right.columnName.name : projected[0]!.name;
    const type = (value: IExprSelecting): string => value.kind === "ExprInt32Literal" ? "int4" : value.kind === "ExprStringLiteral" ? `character varying(${Math.max(1, value.value?.length ?? 1)})` : "text";
    const create = `CREATE TEMP TABLE ${this.quote(temp)}(${projected.map((item) => `${this.quote(item!.name)} ${type(item!.value)}`).join(",")},CONSTRAINT ${this.quote(`PK_${temp}`)} PRIMARY KEY (${this.quote(key)}));`;
    const insertSource = `INSERT INTO ${this.quote(temp)}(${projected.map((item) => this.quote(item!.name)).join(",")}) ${source.kind === "ExprDerivedTableValues" ? `VALUES ${source.values.items.map((row) => `(${row.items.map((item) => this.value(item)).join(",")})`).join(",")}` : this.render(source.query)};`;
    const previousRemap = this.columnSourceRemap; const targetName = node.targetTable.fullName.kind === "ExprTableFullName" ? node.targetTable.fullName.tableName.name : node.targetTable.fullName.name; const sourceRemap = new Map([[originalSourceAlias.toLocaleLowerCase("en-US"), sourceAlias]]); this.columnSourceRemap = new Map([...sourceRemap, [targetAlias.toLocaleLowerCase("en-US"), targetName]]);
    const updateOn = this.boolean(node.on);
    const matched = node.whenMatched?.kind === "ExprMergeMatchedUpdate" ? `UPDATE ${target} SET ${node.whenMatched.set.map((item) => `${this.quote(item.column.columnName.name)}=${this.render(item.value)}`).join(",")} FROM ${this.quote(temp)} ${this.quote(sourceAlias)} WHERE ${updateOn}${node.whenMatched.and === null ? "" : ` AND ${this.boolean(node.whenMatched.and)}`};` : node.whenMatched?.kind === "ExprMergeMatchedDelete" ? `DELETE FROM ${target} WHERE EXISTS(SELECT 1 FROM ${this.quote(temp)} ${this.quote(sourceAlias)} WHERE ${updateOn}${node.whenMatched.and === null ? "" : ` AND ${this.boolean(node.whenMatched.and)}`});` : "";
    const sourceMissing = node.whenNotMatchedBySource?.kind === "ExprMergeMatchedDelete" ? `DELETE FROM ${target} WHERE NOT EXISTS(SELECT 1 FROM ${this.quote(temp)} ${this.quote(sourceAlias)} WHERE ${updateOn})${node.whenNotMatchedBySource.and === null ? "" : ` AND ${this.boolean(node.whenNotMatchedBySource.and)}`};` : node.whenNotMatchedBySource?.kind === "ExprMergeMatchedUpdate" ? `UPDATE ${target} SET ${node.whenNotMatchedBySource.set.map(item => `${this.quote(item.column.columnName.name)}=${this.render(item.value)}`).join(",")} WHERE NOT EXISTS(SELECT 1 FROM ${this.quote(temp)} ${this.quote(sourceAlias)} WHERE ${updateOn})${node.whenNotMatchedBySource.and === null ? "" : ` AND ${this.boolean(node.whenNotMatchedBySource.and)}`};` : "";
    this.columnSourceRemap = sourceRemap; const insertOn = this.boolean(node.on);
    const notMatched = node.whenNotMatchedByTarget?.kind === "ExprExprMergeNotMatchedInsert" ? `INSERT INTO ${target}(${node.whenNotMatchedByTarget.columns.map((column) => this.quote(column.name)).join(",")}) SELECT ${node.whenNotMatchedByTarget.values.map((value) => this.render(value)).join(",")} FROM ${this.quote(temp)} ${this.quote(sourceAlias)} WHERE NOT EXISTS(SELECT 1 FROM ${target} ${this.quote(targetAlias)} WHERE ${insertOn})${node.whenNotMatchedByTarget.and === null ? "" : ` AND ${this.boolean(node.whenNotMatchedByTarget.and)}`};` : "";
    this.columnSourceRemap = previousRemap;
    return `${create}${insertSource}${matched}${notMatched}${sourceMissing}DROP TABLE ${this.quote(temp)};`;
  }
  private quote(value: string): string {
    if (this.options.avoidNameQuoting) return value;
    if (this.options.dialect === "tsql") return `[${value.split("]").join("]]" )}]`;
    if (this.options.dialect === "mysql") return `\`${value.split("`").join("``")}\``;
    return `"${value.split('"').join('""')}"`;
  }
}
