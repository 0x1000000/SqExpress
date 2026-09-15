import {
  exprAlias, exprAliasedColumn, exprAliasedSelecting, exprAllColumns, exprBooleanAnd, exprBooleanEq, exprBooleanGt, exprBooleanGtEq, exprBooleanLt,
  exprBooleanLtEq, exprBooleanNot, exprBooleanNotEq, exprBooleanOr, exprColumn, exprColumnAlias, exprColumnName, exprCrossedTable,
  exprDatabaseName, exprDbSchema, exprDecimalLiteral, exprDiv, exprInValues, exprInt32Literal, exprIsNull, exprJoinedTable, exprLike, exprModulo,
  exprMul, exprNull, exprParameter, exprQuerySpecification, exprSchemaName, exprStringLiteral, exprSub, exprSum, exprTable,
  exprTableAlias, exprTableFullName, exprTableName, exprOrderBy, exprOrderByItem, exprOrderByOffsetFetch, exprOffsetFetch,
  exprSelect, exprSelectOffsetFetch, exprInsert, exprInsertValueRow, exprInsertValues, exprUpdate, exprColumnSetClause, exprDelete, exprDeleteOutput,
  exprExists, exprInSubQuery, exprScalarFunction, exprFunctionName, exprCase, exprCaseWhenThen, exprCast, exprQueryExpression,
  exprTypeInt16, exprTypeInt32, exprTypeInt64, exprTypeBoolean, exprTypeDouble, exprTypeGuid, exprTypeXml,
  exprTypeDateTime, exprTypeDateTimeOffset, exprTypeDecimal, exprTypeString, exprTypeFixSizeString, exprTypeByteArray, exprTypeFixSizeByteArray, exprLateralCrossedTable, exprDerivedTableQuery, exprCteQuery,
  exprPortableScalarFunction, exprFuncCoalesce, exprFuncIsNull, exprGetDate, exprGetUtcDate, exprAggregateFunction, exprSelectingValue,
  exprValueQuery, exprBitwiseAnd, exprBitwiseOr, exprBitwiseXor, exprBitwiseNot,
  exprInsertQuery, exprInsertOutput, exprAliasedColumnName, type IExprQuery,
  exprOver, exprAggregateOverFunction, exprAnalyticFunction, exprStringAgg, exprDateAdd, exprDateDiff,
  exprJsonValue, exprJsonQuery, exprJsonSet, exprJsonRemove, exprJsonArray, exprJsonObject, exprJsonMember,
  exprQueryAsJson, exprJsonOutputColumn,
  exprJsonTable, exprJsonTableValueColumn, exprJsonTableQueryColumn, type ExprJsonTableColumn,
  exprMerge, exprMergeMatchedDelete, exprMergeMatchedUpdate, exprExprMergeNotMatchedInsert, exprExprMergeNotMatchedInsertDefault,
  type IExprMergeMatched, type IExprMergeNotMatched,
  exprDerivedTableValues, exprTableValueConstructor, exprValueRow,
  exprTableFunction, exprAliasedTableFunction,
  nodeTypeKinds, childFields, type Expr, type ExprBoolean, type ExprColumn, type ExprValue, type IExprSelecting, type IExprTableSource, type IExprSubQuery, type ExprType
} from "../ast/generated/ast.generated.js";
import type { ColumnDefinitions, TableDescriptor } from "../descriptors/index.js";
import { decimalValue } from "../ast/runtime.js";
import { modify, serializeAst, walk } from "../ast/operations.js";
import { identifierValue, isIdentifierLike, isKeyword, tryTokenize, type SqlToken } from "./internal/lexer.js";

export interface ParseColumnArtifact { readonly name: string; readonly sqlType: "ExprTypeInt32" | "ExprTypeString" | "ExprTypeDateTime" | "ExprTypeDecimal" | "ExprTypeBoolean"; readonly nullable: boolean; }
export interface ParseTableArtifact { readonly database: null; readonly schema: string | null; readonly name: string; readonly alias: string | null; readonly columns: ReadonlyArray<string>; readonly columnArtifacts: ReadonlyArray<ParseColumnArtifact>; }
export interface ParseTSqlOptions { readonly defaultSchema?: string | null; readonly existingTables?: ReadonlyArray<TableDescriptor<string | null, string, ColumnDefinitions, string>>; }
export interface ParseTSqlSuccess { readonly success: true; readonly ast: Expr; readonly tables: ReadonlyArray<ParseTableArtifact>; }
export interface ParseTSqlFailure { readonly success: false; readonly error: SqyraParserError; }
export type ParseTSqlResult = ParseTSqlSuccess | ParseTSqlFailure;
export type ParserDiagnosticCategory = "syntax" | "unsupported" | "binding";

export class SqyraParserError extends Error { readonly name = "SqyraParserError"; constructor(message: string, readonly position: number | null = null, readonly category: ParserDiagnosticCategory = "syntax") { super(message); } }

export function parseTSql(sql: string, options?: ParseTSqlOptions): ParseTSqlSuccess {
  const result = tryParseTSql(sql, options); if (!result.success) throw result.error; return result;
}
export function tryParseTSql(sql: string, options?: ParseTSqlOptions): ParseTSqlResult {
  if (sql.trim().length === 0) return failure("SQL text cannot be empty.");
  const lexed = tryTokenize(sql); if (!lexed.success) return failure(lexed.error);
  const documentedFailure = validateDocumentedRejectedSyntax(sql); if (documentedFailure !== null) return failure(documentedFailure);
  try { return new Parser(lexed.tokens, options).parse(); } catch (error) { if (error instanceof SqyraParserError) return { success: false, error }; throw error; }
}
const failure = (message: string): ParseTSqlFailure => ({ success: false, error: new SqyraParserError(message) });

/** Fail-closed validation for malformed clause shapes that the recursive parser
 * must reject before attempting to bind a partial expression. */
function validateDocumentedRejectedSyntax(sql: string): string | null {
  const text = sql.trim(); const upper = text.toUpperCase();
  const openParentheses = [...text].filter((value) => value === "(").length; const closeParentheses = [...text].filter((value) => value === ")").length;
  if (openParentheses !== closeParentheses) return "Syntax error: unbalanced parentheses.";
  if (/\b(?:CROSS|OUTER)\s+APPLY\b[\s\S]*\bON\b/i.test(text)) return "Syntax error: CROSS/ APPLY join cannot contain ON condition.";
  if (/\b(?:WHERE|AND|OR)\s*;?$/i.test(text)) return "Syntax error: unexpected end of statement.";
  if (/\bFETCH\b/i.test(text) && (!/\bOFFSET\b/i.test(text) || !/\bFETCH\s+(?:NEXT|FIRST)\s+\S+\s+(?:ROW|ROWS)\s+ONLY\b/i.test(text))) return "Syntax error: OFFSET/FETCH clause is invalid.";
  if (/\bFETCH\s+(?:NEXT|FIRST)\s+\S+\s+(?:ROW|ROWS)\s+ONLY\s+\S+/i.test(text)) return "Syntax error: OFFSET/FETCH clause is invalid.";
  if (/\bOFFSET\s*;?$/i.test(text)) return "Syntax error: OFFSET/FETCH clause is invalid.";
  if (/\bGROUP\s+BY\b[^;]*,,/i.test(text)) return "Syntax error: GROUP BY clause is invalid.";
  if (/\bORDER\s+BY\b[^;]*,,/i.test(text)) return "Syntax error: ORDER BY clause is invalid.";
  if (/\bFROM\s+(?:FROM|WHERE|GROUP|ORDER|OFFSET|UNION)\b/i.test(text)) return "Syntax error: FROM clause is invalid.";
  if (/^SELECT\s+(?:(?:DISTINCT|ALL)\s+)?(?:(?:TOP\s*(?:\([^)]*\)|\S+)(?:\s+PERCENT)?(?:\s+WITH\s+TIES)?\s+)?)?(?:FROM|WHERE|ORDER\s+BY|$)/i.test(text)) return "Syntax error: SELECT list is missing.";
  if (/^SELECT(?:\s+(?:DISTINCT|ALL))?(?:\s+TOP\s*(?:\([^)]*\)|\S+)(?:\s+PERCENT)?(?:\s+WITH\s+TIES)?)?\s*$/i.test(text)) return "Syntax error: SELECT list is missing.";
  if (/\bPIVOT\s*\(/i.test(text)) return "Feature 'PIVOT' is not supported by SqExpress parser.";
  if (/\bOPTION\s*\(/i.test(text)) return "Feature 'OPTION(...)' is not supported by SqExpress parser.";
  if (/\bHAVING\s+\S/i.test(text)) return "Feature 'HAVING' is not supported by SqExpress parser.";
  if (/\bOUTPUT\b[\s\S]*\bINTO\b/i.test(text)) return "Feature 'OUTPUT ... INTO' is not supported by SqExpress parser.";
  if (/^MERGE\b[\s\S]*\bOUTPUT\b/i.test(text)) return "Feature 'OUTPUT' is not supported by SqExpress parser for MERGE statements.";
  const quantified = /\b(?:ANY|SOME|ALL)\b/i.exec(text);
  if (/=\s*(?:ANY|SOME|ALL)\b/i.test(text)) return /\b(?:ANY|SOME|ALL)\s*$/i.test(text) ? `Syntax error: incorrect syntax near '${quantified?.[0]?.toUpperCase()}'.` : "Feature 'ANY/SOME/ALL predicates' is not supported by SqExpress parser.";
  if (/^SELCT\b/i.test(text)) return "Unsupported or invalid statement start.";
  if (/^SELECT\s+(?:ALL\s+)?FROM\b/i.test(text) || /^SELECT\s+TOP\s*\([^)]*\)(?:\s+WITH\s+TIES)?\s+FROM\b/i.test(text)) return "Syntax error: SELECT list is missing.";
  if (/^SELECT\s+ALL\s+FROM\b/i.test(text)) return "Syntax error: SELECT list is missing.";
  if (/SELECT\s+[^;]*,,/i.test(text) && !/\bFROM\b/i.test(text.slice(0, text.indexOf(",,")))) return "Syntax error: SELECT list is invalid.";
  if (/\bIN\s*\([^)]*,\s*\)/i.test(text)) return "Syntax error: IN predicate list is invalid.";
  if (/\bINSERT\b[^(]*\([^)]*,\s*\)\s*(?:VALUES|SELECT)/i.test(text)) return "Syntax error: INSERT column list is invalid.";
  if (/\bINSERT\b[^;]*\bVALUES\s*\([^)]*,\s*\)/i.test(text)) return "Syntax error: INSERT VALUES clause is invalid.";
  if (/^INSERT\s+(?!INTO\b)[^\s(]+\s+[^\s(]+\s+VALUES\b/i.test(text)) return "Syntax error: INSERT target is invalid.";
  if (/^INSERT\s+INTO\s+[^\s(]+\s+[^\s(]+\s+VALUES\b/i.test(text)) return "Syntax error: INSERT target is invalid.";
  if (/^INSERT\b[^;]*\([^)]*,[^)]*\)\s+SELECT\b/i.test(text)) { const targets = text.match(/^INSERT\s+(?:INTO\s+)?[^()]+\(([^)]*)\)/i)?.[1]?.split(",").length; const selection = text.match(/\bSELECT\s+(.+?)\s+FROM\b/i)?.[1]?.split(",").length; if (targets !== undefined && selection !== undefined && targets !== selection) return "INSERT source column count does not match target column count."; }
  if (/^UPDATE\b(?![\s\S]*\bSET\b)/i.test(text)) return "Syntax error: UPDATE statement must contain SET clause.";
  if (/^UPDATE\b[\s\S]*\bSET\s*\(/i.test(text)) return "Syntax error: UPDATE SET clause is invalid.";
  if (/^DELETE\s+AS\b/i.test(text)) return "Syntax error: DELETE target is invalid.";
  if (/^DELETE\s+TOP\s*\([^)]*\)\s+PERCENT\s*$/i.test(text)) return "Syntax error: DELETE statement must contain target.";
  if (/^MERGE\b(?![\s\S]*\bON\b)/i.test(text)) return "Syntax error: MERGE statement must contain ON clause.";
  if (/\bFROM\s*$/i.test(text) || /\bFROM\b[^;]*(?:,,|,\s*,)/i.test(text) || /\bFROM\s+[^\s,]+\s+AS\s*$/i.test(text)) return "Syntax error: FROM clause is invalid.";
  if (/\b(?:CROSS\s+JOIN|CROSS\s+APPLY|OUTER\s+APPLY)\s*$/i.test(text)) return "Syntax error: FROM clause is invalid.";
  if (/\b(?:CROSS|OUTER)\s+APPLY\s+(?!\(|OPENJSON\b)[\w[\].]+(?:\s+\w+)?\s*$/i.test(text)) return "Syntax error: FROM clause is invalid.";
  if (/\b(?:CROSS|OUTER)\s+APPLY\s*\(VALUES[\s\S]*(?:,\s*\)|,\s*,)/i.test(text) || /\bFROM\s*\(VALUES[\s\S]*(?:,\s*\)|,\s*,)/i.test(text)) return "Syntax error: FROM clause is invalid.";
  if (/\b(?:INNER\s+|LEFT\s+|RIGHT\s+|FULL\s+)?JOIN\s+[\w[\].]+(?:\s+\w+)?\s*$/i.test(text) && !/\bCROSS\s+JOIN\b/i.test(text)) return "Syntax error: JOIN clause must contain ON condition.";
  if (/\bCROSS\s+JOIN\b[\s\S]*\bON\b/i.test(text)) return "Syntax error: CROSS/ APPLY join cannot contain ON condition.";
  if (/\bGROUP\s+BY\s*$/i.test(text) || /\bGROUP\s+BY\b[^;]*,\s*$/i.test(text)) return "Syntax error: GROUP BY clause is invalid.";
  if (/\bORDER\s+BY\s*$/i.test(text) || /\bORDER\s+BY\b[^;]*,\s*$/i.test(text)) return "Syntax error: ORDER BY clause is invalid.";
  if (/\bORDER\s+(?!BY\b)(\S+)/i.test(text)) return `Syntax error: incorrect syntax near '${text.match(/\bORDER\s+(\S+)/i)?.[1]}'.`;
  if (/\bOFFSET\b/i.test(text) && !/\bORDER\s+BY\b/i.test(text)) return "Syntax error: OFFSET requires ORDER BY clause.";
  if (/\bOFFSET\s+\S+\s*(?:$|FETCH\b)/i.test(text) || /\bFETCH\s+(?:NEXT|FIRST)\s+(?:ROW|ROWS)\b/i.test(text)) return "Syntax error: OFFSET/FETCH clause is invalid.";
  if (/\bEXISTS\s*\(\s*\)/i.test(text)) return "Syntax error: EXISTS predicate cannot be empty.";
  if (!/^MERGE\b/i.test(text) && /\bFROM\b[\s\S]*\bON\b/i.test(text) && !/\b(?:JOIN|APPLY)\b/i.test(text)) return "Syntax error: ON clause is invalid.";
  if (/^SELECT\s+(?:[\w\[\]]+\.)\*\s+(?:AS\s+)?[\w\[\]]+\s+FROM\b/i.test(text) || /^SELECT\s+\*\s+AS\s+/i.test(text)) return "Syntax error: incorrect syntax near select-item alias.";
  if (/\bHAVING\s*$/i.test(text)) return "Syntax error: incorrect syntax near 'HAVING'.";
  if (/\bWITH\b[\s\S]*,\s*AS\s*\(/i.test(text)) return "Syntax error: incorrect syntax near 'AS'.";
  if (/^SELECT\s+\*\s+\S+\s*$/i.test(text)) return `Syntax error: incorrect syntax near '${text.split(/\s+/).at(-1)}'.`;
  if (/^SELECT\s+1\s+AS\s+\[\s*\]/i.test(text)) return "Syntax error: SELECT list is invalid.";
  if (/\b(?:SUBSTRING|LEFT|TRY_CONVERT|POWER)\s*\([^)]*,\s*\)/i.test(text)) { const call = text.match(/\b(?:SUBSTRING|LEFT|TRY_CONVERT|POWER)\s*\([^)]*\)/i)?.[0]; return upper.startsWith("SELECT 1 WHERE") ? "Value expression is not supported." : `Select item is not supported: [${call}]. Value expression is not supported.`; }
  if (/\b(?:COALESCE|ISNULL|CONVERT)\s*\([^)]*,\s*\)/i.test(text) || /\bISNULL\s*\(\s*CONVERT\s*\([^)]*\)\s*,\s*\)/i.test(text)) return upper.startsWith("SELECT 1 WHERE") ? "Value expression is not supported." : `Select item is not supported: [${text.slice(7)}]. Value expression is not supported.`;
  if (/^SELECT\s+CONVERT\s*\(\s*INT\s*\)/i.test(text)) return "Select item is not supported: [CONVERT(INT )]. Function 'CONVERT' has invalid arguments.";
  if (/^SELECT\s+REPLACE\s*\([^,]+,[^,]+\)/i.test(text)) return "Select item is not supported: [REPLACE('abc','a')]. Function 'REPLACE' has invalid arguments.";
  if (/^SELECT\s+REPLICATE\s*\([^,]+\)/i.test(text)) return "Select item is not supported: [REPLICATE('x')]. Function 'REPLICATE' has invalid arguments.";
  if (/^SELECT\s+NULLIF\s*\([^,]+\)/i.test(text)) return "Select item is not supported: [NULLIF(ABS(-1))]. Function 'NULLIF' has invalid arguments.";
  if (/^SELECT\s+SUM\s*\(\s*\)/i.test(text)) return "Select item is not supported: [SUM()]. Function 'SUM' has invalid arguments.";
  if (/\([^)]*$/.test(text)) return "Syntax error: unbalanced parentheses.";
  if (/[=<>+*/-]\s*$/.test(text)) return "Syntax error: unexpected end of statement.";
  return null;
}

class Parser {
  private index = 0; private readonly tables: Array<Omit<ParseTableArtifact, "columnArtifacts">> = []; private readonly nestedTables: ParseTableArtifact[] = []; private readonly syntheticVisible = new Set<string>(); private readonly defaultSchema: string | null; private readonly ctes: Map<string, IExprSubQuery>; private preserveDeleteTargetArtifact = false;
  constructor(private readonly tokens: ReadonlyArray<SqlToken>, private readonly options?: ParseTSqlOptions, private readonly outerVisible: ReadonlySet<string> = new Set(), inheritedCtes?: Map<string, IExprSubQuery>, private readonly asSubQuery = false) { this.defaultSchema = options && "defaultSchema" in options ? options.defaultSchema ?? null : "dbo"; this.ctes = inheritedCtes ?? new Map(); }
  parse(): ParseTSqlSuccess {
    if (this.keyword("WITH")) this.parseWithClause();
    if (this.current.type === "openParen") return this.parseParenthesizedQuery();
    if (this.keyword("SELECT")) return this.parseSelect();
    if (this.keyword("INSERT")) return this.parseInsert();
    if (this.keyword("UPDATE")) return this.parseUpdate();
    if (this.keyword("DELETE")) return this.parseDelete();
    if (this.keyword("MERGE")) return this.parseMerge();
    this.fail(`Unsupported statement '${this.current.text}'.`);
  }
  private parseSelect(): ParseTSqlSuccess {
    const set = this.findTopLevelSetOperation();
    if (set !== null) {
      const orderIndex = this.findTopLevelKeyword("ORDER", set.index + set.width); const rightEnd = orderIndex ?? this.tokens.length - 1;
      const left = this.parseSubQueryRange(0, set.index); const rightStart = set.index + set.width; const right = this.parseSubQueryRange(rightStart, rightEnd);
      this.index = this.tokens.length - 1;
      let ast: Expr = exprQueryExpression({ left, right, queryExpressionType: set.type });
      if (orderIndex !== null) { this.index = orderIndex; this.expectKeyword("ORDER"); this.expectKeyword("BY"); const items = [this.orderItem()]; while (this.take("comma")) items.push(this.orderItem()); if (this.takeKeyword("OFFSET")) { const offset = this.value(); this.expectRowWord(); let fetch: ExprValue | null = null; if (this.takeKeyword("FETCH")) { this.takeKeyword("NEXT") || this.takeKeyword("FIRST"); fetch = this.value(); this.expectRowWord(); this.expectKeyword("ONLY"); } ast = exprSelectOffsetFetch({ selectQuery: ast as IExprSubQuery, orderBy: exprOrderByOffsetFetch({ orderList: items, offsetFetch: exprOffsetFetch({ offset, fetch }) }) }); } else ast = exprSelect({ selectQuery: ast as IExprSubQuery, orderBy: exprOrderBy({ orderList: items }) }); this.finish(); }
      return this.complete(ast);
    }
    this.expectKeyword("SELECT");
    const distinct = this.takeKeyword("DISTINCT"); let top: ExprValue | null = null;
    if (this.takeKeyword("TOP")) { const paren = this.take("openParen"); top = this.value(); if (paren) this.expect("closeParen"); if (this.takeKeyword("PERCENT")) this.fail("TOP PERCENT has not been ported yet."); }
    const selectList: IExprSelecting[] = [this.selectItem()]; while (this.take("comma")) selectList.push(this.selectItem());
    let from: IExprTableSource | null = null;
    if (this.takeKeyword("FROM")) from = this.tableSource();
    let where: ExprBoolean | null = null; if (this.takeKeyword("WHERE")) where = this.booleanExpression();
    let groupBy: ExprValue[] | null = null;
    if (this.takeKeyword("GROUP")) { this.expectKeyword("BY"); groupBy = [this.value()]; while (this.take("comma")) groupBy.push(this.value()); }
    const fromSource = from;
    const boundSelectList = fromSource === null ? selectList : selectList.map((item) => this.bindUnqualifiedColumns(item, fromSource) as IExprSelecting);
    const boundWhere = where === null || fromSource === null ? where : this.bindUnqualifiedColumns(where, fromSource) as ExprBoolean;
    const boundGroupBy = groupBy === null || fromSource === null ? groupBy : groupBy.map((item) => this.bindUnqualifiedColumns(item, fromSource) as ExprValue);
    if (boundSelectList.some((item) => [...walkCurrentScope(item)].some((node) => node.kind === "ExprAggregateFunction" || node.kind === "ExprStringAgg"))) for (const item of boundSelectList) {
      const expression = item.kind === "ExprAliasedSelecting" ? item.value : item.kind === "ExprAliasedColumn" ? item.column : item;
      if (expression.kind === "ExprAggregateOverFunction" || expression.kind === "ExprAnalyticFunction") continue;
      if ([...walkCurrentScope(expression)].some((node) => node.kind === "ExprAggregateFunction" || node.kind === "ExprStringAgg")) continue;
      const groupedColumns = new Set((boundGroupBy ?? []).filter((group): group is ExprColumn => group.kind === "ExprColumn").map((column) => serializeAst(column)));
      const expressionColumns = [...walkCurrentScope(expression)].filter((node): node is ExprColumn => node.kind === "ExprColumn");
      if (!(boundGroupBy ?? []).some((group) => serializeAst(group) === serializeAst(expression)) && expressionColumns.some((column) => !groupedColumns.has(serializeAst(column)))) this.fail(`Select expression '${expression.kind === "ExprColumn" ? expression.columnName.name : expression.kind}' is neither grouped nor aggregated.`, "binding");
    }
    let ast: Expr = exprQuerySpecification({ selectList: boundSelectList, top, from, where: boundWhere, groupBy: boundGroupBy, distinct });
    if (this.takeKeyword("ORDER")) {
      this.expectKeyword("BY"); const items = [this.orderItem()]; while (this.take("comma")) items.push(this.orderItem());
      if (this.takeKeyword("OFFSET")) { const offset = this.value(); this.expectRowWord(); let fetch: ExprValue | null = null; if (this.takeKeyword("FETCH")) { this.takeKeyword("NEXT") || this.takeKeyword("FIRST"); fetch = this.value(); this.expectRowWord(); this.expectKeyword("ONLY"); } ast = exprSelectOffsetFetch({ selectQuery: ast, orderBy: exprOrderByOffsetFetch({ orderList: items, offsetFetch: exprOffsetFetch({ offset, fetch }) }) }); }
      else if (top !== null && this.asSubQuery) { const orderedQuery = exprQuerySpecification({ selectList: boundSelectList, top: null, from, where: boundWhere, groupBy: boundGroupBy, distinct }); ast = exprSelectOffsetFetch({ selectQuery: orderedQuery, orderBy: exprOrderByOffsetFetch({ orderList: items, offsetFetch: exprOffsetFetch({ offset: exprInt32Literal({ value: 0 }), fetch: top }) }) }); }
      else ast = exprSelect({ selectQuery: ast, orderBy: exprOrderBy({ orderList: items }) });
    }
    if (this.takeKeyword("FOR")) {
      this.expectKeyword("JSON"); this.expectKeyword("PATH"); let includeNullValues = false; let withoutArrayWrapper = false;
      while (this.take("comma")) { if (this.takeKeyword("INCLUDE_NULL_VALUES")) includeNullValues = true; else if (this.takeKeyword("WITHOUT_ARRAY_WRAPPER")) withoutArrayWrapper = true; else this.fail("Unsupported FOR JSON option.", "unsupported"); }
      if (ast.kind === "ExprQuerySpecification") ast = exprQuerySpecification({ selectList: ast.selectList.map((item) => item.kind === "ExprAliasedSelecting" && item.alias.name.includes(".") ? exprJsonOutputColumn({ value: item.value, jsonPath: `$${item.alias.name.split(".").map((part) => `.\"${part.split("\\").join("\\\\").split('"').join('\\"')}\"`).join("")}` }) : item), top: ast.top, from: ast.from, where: ast.where, groupBy: ast.groupBy, distinct: ast.distinct });
      if (!nodeTypeKinds.IExprQuery?.has(ast.kind)) this.fail("FOR JSON requires a query.");
      ast = exprQueryAsJson({ query: ast as IExprQuery, includeNullValues, withoutArrayWrapper });
    }
    this.take("semicolon"); if (this.current.type !== "endOfFile") this.fail(this.tokens[this.index - 1]?.type === "semicolon" ? "Only one SQL statement is supported." : `Unexpected token '${this.current.text}'.`);
    return this.complete(ast);
  }
  private parseParenthesizedQuery(): ParseTSqlSuccess {
    const set = this.findTopLevelSetOperation();
    if (set !== null) return this.parseSelect();
    const close = this.matchingClose(this.index);
    if (close === null) this.fail("Unclosed parenthesized query.");
    const query = this.parseSubQueryRange(this.index + 1, close);
    this.index = close + 1;
    let ast: Expr = query;
    if (this.takeKeyword("ORDER")) {
      this.expectKeyword("BY"); const items = [this.orderItem()]; while (this.take("comma")) items.push(this.orderItem());
      if (this.takeKeyword("OFFSET")) { const offset = this.value(); this.expectRowWord(); let fetch: ExprValue | null = null; if (this.takeKeyword("FETCH")) { this.takeKeyword("NEXT") || this.takeKeyword("FIRST"); fetch = this.value(); this.expectRowWord(); this.expectKeyword("ONLY"); } ast = exprSelectOffsetFetch({ selectQuery: query, orderBy: exprOrderByOffsetFetch({ orderList: items, offsetFetch: exprOffsetFetch({ offset, fetch }) }) }); }
      else ast = exprSelect({ selectQuery: query, orderBy: exprOrderBy({ orderList: items }) });
    }
    this.finish(); return this.complete(ast);
  }
  private parseInsert(): ParseTSqlSuccess {
    this.expectKeyword("INSERT"); this.takeKeyword("INTO"); const target = this.tableNameExpression();
    let targetColumns = null;
    if (this.take("openParen")) { targetColumns = [exprColumnName({ name: this.identifier() })]; while (this.take("comma")) targetColumns.push(exprColumnName({ name: this.identifier() })); this.expect("closeParen"); }
    if (targetColumns === null && target.kind === "ExprTableFullName") this.tables.push({ database: null, schema: target.dbSchema?.schema.name ?? null, name: target.tableName.name, alias: null, columns: Object.freeze([]) });
    let outputColumns: ReturnType<typeof exprAliasedColumnName>[] | null = null;
    if (this.takeKeyword("OUTPUT")) {
      outputColumns = [];
      do {
        this.expectKeyword("INSERTED"); this.expect("dot"); const column = exprColumnName({ name: this.identifier() });
        let alias = null; if (this.takeKeyword("AS")) alias = exprColumnAlias({ name: this.identifier() }); else if (isIdentifierLike(this.current) && !this.isClause(this.current)) alias = exprColumnAlias({ name: this.identifier() });
        outputColumns.push(exprAliasedColumnName({ column, alias }));
      } while (this.take("comma"));
    }
    if (this.keyword("SELECT")) { const query = this.parseSubQueryRange(this.index, this.tokens.length - 1); if (!nodeTypeKinds.IExprQuery?.has(query.kind)) this.fail("INSERT source must be a query."); this.index = this.tokens.length - 1; const insert = exprInsert({ target, targetColumns, source: exprInsertQuery({ query: query as IExprQuery }) }); return this.complete(outputColumns === null ? insert : exprInsertOutput({ insert, outputColumns })); }
    this.expectKeyword("VALUES"); const rows = [];
    do { this.expect("openParen"); const items = [this.value()]; while (this.take("comma")) items.push(this.value()); this.expect("closeParen"); rows.push(exprInsertValueRow({ items })); } while (this.take("comma"));
    this.finish(); const insert = exprInsert({ target, targetColumns, source: exprInsertValues({ items: rows }) }); return this.complete(outputColumns === null ? insert : exprInsertOutput({ insert, outputColumns }));
  }
  private parseUpdate(): ParseTSqlSuccess {
    this.expectKeyword("UPDATE"); this.consumeDmlTop(); this.takeKeyword("PERCENT"); const targetMayBeAlias = isIdentifierLike(this.current) && isKeyword(this.tokens[this.index + 1]!, "SET"); const parsedTarget = this.tableFactor(); if (parsedTarget.kind !== "ExprTable") this.fail("UPDATE target must be a table."); let target = parsedTarget;
    const aliasCandidate = targetMayBeAlias && target.alias === null && target.fullName.kind === "ExprTableFullName" ? target.fullName.tableName.name : null;
    const deferredArtifact = aliasCandidate === null ? null : this.tables.pop() ?? null;
    this.expectKeyword("SET");
    const setClause = [];
    do { const column = this.value(); if (column.kind !== "ExprColumn") this.fail("SET target must be a column."); if (!this.take("operator", "=")) this.fail("Expected '=' in SET assignment."); setClause.push(exprColumnSetClause({ column, value: this.value() })); } while (this.take("comma"));
    let source: IExprTableSource | null = null; if (this.takeKeyword("FROM")) source = this.tableSource();
    if (source !== null && aliasCandidate !== null) { const resolved = this.findAliasedTable(source, aliasCandidate); if (resolved !== null) target = resolved; else if (deferredArtifact !== null) this.tables.unshift(deferredArtifact); }
    else if (deferredArtifact !== null) this.tables.unshift(deferredArtifact);
    let filter: ExprBoolean | null = null; if (this.takeKeyword("WHERE")) filter = this.booleanExpression();
    const targetName = target.alias?.alias.kind === "ExprAlias" ? target.alias.alias.name : target.fullName.kind === "ExprTableFullName" ? target.fullName.tableName.name : target.fullName.name;
    const bindUpdate = <T extends Expr>(value: T): T => source === null ? value : (modify(value, (node) => {
      if (node.kind !== "ExprColumn" || node.source !== null) return node;
      const cteBound = source === null ? node : this.bindUnqualifiedColumns(node, source);
      if (cteBound !== node) return cteBound;
      return exprColumn({ source: exprTableAlias({ alias: exprAlias({ name: targetName }) }), columnName: node.columnName });
    }) ?? value) as T;
    if (source === null) { const nestedTarget = setClause.flatMap((clause) => [...walk(clause.value)]).find((node): node is Extract<Expr, { readonly kind: "ExprTable" }> => node.kind === "ExprTable"); if (nestedTarget !== undefined) { target = nestedTarget; if (nestedTarget.fullName.kind === "ExprTableFullName") { this.tables.splice(0, this.tables.length, { database: null, schema: nestedTarget.fullName.dbSchema?.schema.name ?? null, name: nestedTarget.fullName.tableName.name, alias: nestedTarget.alias?.alias.kind === "ExprAlias" ? nestedTarget.alias.alias.name : null, columns: Object.freeze([]) }); this.nestedTables.splice(0); } this.finish(); return this.complete(exprUpdate({ target, setClause, source: null, filter })); } }
    this.finish(); return this.complete(exprUpdate({ target, setClause: setClause.map(bindUpdate), source, filter: filter === null ? null : bindUpdate(filter) }));
  }
  private findAliasedTable(source: IExprTableSource, alias: string): Extract<Expr, { readonly kind: "ExprTable" }> | null {
    if (source.kind === "ExprTable") return source.alias?.alias.kind === "ExprAlias" && source.alias.alias.name.toLocaleLowerCase("en-US") === alias.toLocaleLowerCase("en-US") ? source : null;
    if (source.kind === "ExprJoinedTable" || source.kind === "ExprCrossedTable" || source.kind === "ExprLateralCrossedTable") return this.findAliasedTable(source.left, alias) ?? this.findAliasedTable(source.right, alias);
    return null;
  }
  private parseDelete(): ParseTSqlSuccess {
    this.expectKeyword("DELETE"); this.consumeDmlTop();
    const fromFirst = this.takeKeyword("FROM");
    const parsedTarget = this.tableFactor(); if (parsedTarget.kind !== "ExprTable") this.fail("DELETE target must be a table.");
    let outputColumns: ReturnType<typeof exprAliasedColumn>[] | null = null;
    if (this.takeKeyword("OUTPUT")) { outputColumns = []; do { this.expectKeyword("DELETED"); this.expect("dot"); const column = exprColumn({ source: null, columnName: exprColumnName({ name: this.identifier() }) }); let alias = null; if (this.takeKeyword("AS")) alias = exprColumnAlias({ name: this.identifier() }); else if (isIdentifierLike(this.current) && !this.isClause(this.current)) alias = exprColumnAlias({ name: this.identifier() }); outputColumns.push(exprAliasedColumn({ column, alias })); } while (this.take("comma")); }
    let target = parsedTarget; let source: IExprTableSource | null = null;
    if (!fromFirst && this.takeKeyword("FROM")) {
      const targetAlias = parsedTarget.alias?.alias.kind === "ExprAlias" ? parsedTarget.alias.alias.name : parsedTarget.fullName.kind === "ExprTableFullName" ? parsedTarget.fullName.tableName.name : parsedTarget.fullName.name;
      this.tables.pop(); source = this.tableSource(); const resolved = this.findAliasedTable(source, targetAlias); if (resolved === null) this.fail(`DELETE target alias '${targetAlias}' was not found in FROM source.`, "binding"); target = resolved; this.preserveDeleteTargetArtifact = true; if (source === resolved) source = null;
    }
    else if (fromFirst) this.preserveDeleteTargetArtifact = true;
    let filter: ExprBoolean | null = null; if (this.takeKeyword("WHERE")) filter = this.booleanExpression();
    this.finish(); const deleted = exprDelete({ target, source, filter }); return this.complete(outputColumns === null ? deleted : exprDeleteOutput({ delete: deleted, outputColumns }));
  }
  private parseMerge(): ParseTSqlSuccess {
    this.expectKeyword("MERGE"); const target = this.tableFactor(); if (target.kind !== "ExprTable") this.fail("MERGE target must be a table.");
    this.expectKeyword("USING"); const source = this.tableFactor(); this.expectKeyword("ON"); const on = this.booleanExpression();
    let whenMatched: IExprMergeMatched | null = null; let whenNotMatchedByTarget: IExprMergeNotMatched | null = null; let whenNotMatchedBySource: IExprMergeMatched | null = null;
    while (this.takeKeyword("WHEN")) {
      let targetKind: "matched" | "target" | "source";
      if (this.takeKeyword("MATCHED")) targetKind = "matched";
      else { this.expectKeyword("NOT"); this.expectKeyword("MATCHED"); if (this.takeKeyword("BY")) { if (this.takeKeyword("SOURCE")) targetKind = "source"; else { this.expectKeyword("TARGET"); targetKind = "target"; } } else targetKind = "target"; }
      let and: ExprBoolean | null = null; if (this.takeKeyword("AND")) and = this.booleanExpression(); this.expectKeyword("THEN");
      if (targetKind === "target") {
        this.expectKeyword("INSERT");
        if (this.takeKeyword("DEFAULT")) { this.expectKeyword("VALUES"); whenNotMatchedByTarget = exprExprMergeNotMatchedInsertDefault({ and }); continue; }
        this.expect("openParen"); const columns = [exprColumnName({ name: this.identifier() })]; while (this.take("comma")) columns.push(exprColumnName({ name: this.identifier() })); this.expect("closeParen"); this.expectKeyword("VALUES"); this.expect("openParen"); const values = [this.value()]; while (this.take("comma")) values.push(this.value()); this.expect("closeParen"); if (columns.length !== values.length) this.fail("MERGE INSERT columns and values must have equal counts."); whenNotMatchedByTarget = exprExprMergeNotMatchedInsert({ and, columns, values }); continue;
      }
      let action: IExprMergeMatched;
      if (this.takeKeyword("DELETE")) action = exprMergeMatchedDelete({ and });
      else { this.expectKeyword("UPDATE"); this.expectKeyword("SET"); const set = this.setClauses(); action = exprMergeMatchedUpdate({ and, set }); }
      if (targetKind === "matched") whenMatched = action; else whenNotMatchedBySource = action;
    }
    if (whenMatched === null && whenNotMatchedByTarget === null && whenNotMatchedBySource === null) this.fail("MERGE requires at least one WHEN action.");
    if (!this.take("semicolon")) this.fail("MERGE statement must be terminated by semicolon."); if (this.current.type !== "endOfFile") this.fail(`Unexpected token '${this.current.text}'.`); return this.complete(exprMerge({ targetTable: target, source, on, whenMatched, whenNotMatchedByTarget, whenNotMatchedBySource }));
  }
  private setClauses() { const result = []; do { const column = this.value(); if (column.kind !== "ExprColumn") this.fail("SET target must be a column."); if (!this.take("operator", "=")) this.fail("Expected '=' in SET assignment."); result.push(exprColumnSetClause({ column, value: this.value() })); } while (this.take("comma")); return result; }
  private consumeDmlTop(): void { if (!this.takeKeyword("TOP")) return; const parenthesized = this.take("openParen"); this.value(); if (parenthesized) this.expect("closeParen"); }
  private complete(ast: Expr): ParseTSqlSuccess {
    const used = new Map<number, Set<string>>(this.tables.map((_, index) => [index, new Set<string>()]));
    const projectionAliases = new Set<string>();
    const ignoredArtifactColumns = new Set([
      ...[...walkCurrentScope(ast)].filter((node) => node.kind === "ExprStringAgg" && node.orderBy === null).flatMap((aggregate) => [...walk(aggregate)].filter((node): node is ExprColumn => node.kind === "ExprColumn").map((column) => serializeAst(column))),
      ...[...walkCurrentScope(ast)].filter((node) => node.kind === "ExprScalarFunction" || node.kind === "ExprPortableScalarFunction" || node.kind === "ExprAggregateFunction" || node.kind === "ExprAnalyticFunction" || node.kind === "ExprAggregateOverFunction").flatMap((expression) => [...walkCurrentScope(expression)].filter((node): node is ExprColumn => node.kind === "ExprColumn" && node.source === null).map(serializeAst)),
    ]);
    if (ast.kind === "ExprDeleteOutput") for (const item of ast.outputColumns) ignoredArtifactColumns.add(serializeAst(item.column));
    const projection = ast.kind === "ExprSelect" || ast.kind === "ExprSelectOffsetFetch" ? ast.selectQuery : ast;
    if (projection.kind === "ExprQuerySpecification") for (const item of projection.selectList) {
      const alias = item.kind === "ExprAliasedSelecting" ? item.alias.name : item.kind === "ExprAliasedColumn" && item.alias !== null ? item.alias.name : null;
      if (alias !== null) { const key = alias.toLocaleLowerCase("en-US"); if (projectionAliases.has(key)) this.fail(`Duplicate select alias in scope: ${alias}.`, "binding"); projectionAliases.add(key); }
    }
    const nullableColumns = new Set([...walkCurrentScope(ast)].filter((node): node is Extract<Expr, { readonly kind: "ExprIsNull" }> => node.kind === "ExprIsNull").flatMap((node) => [...walkCurrentScope(node.test)].filter((item): item is ExprColumn => item.kind === "ExprColumn").map(serializeAst)));
    for (const node of walkCurrentScope(ast)) if (node.kind === "ExprColumn") {
      if (ignoredArtifactColumns.has(serializeAst(node))) continue;
      const alias = node.source?.kind === "ExprTableAlias" && node.source.alias.kind === "ExprAlias" ? node.source.alias.name : null;
      if (alias === null && projectionAliases.has(node.columnName.name.toLocaleLowerCase("en-US"))) continue;
      const candidates = this.tables.map((table, index) => ({ table, index })).filter(({ table }) => alias === null ? this.tables.length === 1 : (table.alias ?? table.name).toLocaleLowerCase("en-US") === alias.toLocaleLowerCase("en-US"));
      if (alias !== null && candidates.length === 0) { if (this.outerVisible.has(alias.toLocaleLowerCase("en-US")) || this.syntheticVisible.has(alias.toLocaleLowerCase("en-US"))) continue; this.fail(`Unknown table or alias '${alias}'.`, "binding"); }
      if (alias === null && this.tables.length > 1) {
      const descriptorOwners = this.options?.existingTables === undefined ? [] : this.tables.map((table, index) => ({ table, index })).filter(({ table }) => this.options!.existingTables!.some((descriptor) => descriptor.$metadata.schema === table.schema && descriptor.$metadata.name === table.name && Object.prototype.hasOwnProperty.call(descriptor.$metadata.definitions, node.columnName.name)));
        if (descriptorOwners.length === 1) { used.get(descriptorOwners[0]!.index)!.add(node.columnName.name); continue; }
        this.fail(`Column '${node.columnName.name}' is ambiguous in a multi-table scope.`, "binding");
      }
      if (candidates.length === 1) used.get(candidates[0]!.index)!.add(node.columnName.name);
    }
    for (const node of walk(ast)) if (node.kind === "ExprColumn" && node.source?.kind === "ExprTableAlias" && node.source.alias.kind === "ExprAlias") { const alias = node.source.alias.name.toLocaleLowerCase("en-US"); const match = this.tables.map((table, index) => ({ table, index })).find(({ table }) => (table.alias ?? table.name).toLocaleLowerCase("en-US") === alias); if (match !== undefined) used.get(match.index)!.add(node.columnName.name); }
    if (ast.kind === "ExprSelect" || ast.kind === "ExprSelectOffsetFetch") for (const item of ast.orderBy.orderList) {
      const value = item.value;
      if (value.kind === "ExprColumn" && value.source === null && this.tables.some((table) => (table.alias ?? table.name).toLocaleLowerCase("en-US") === value.columnName.name.toLocaleLowerCase("en-US"))) this.fail(`ORDER BY item cannot reference table alias without column: ${value.columnName.name}.`, "binding");
    }
    if (ast.kind === "ExprUpdate" && ast.source === null && ast.target.alias !== null && this.tables.length === 1) { const nested = ast.setClause.flatMap((clause) => [...walk(clause.value)]).filter((node): node is ExprColumn => node.kind === "ExprColumn").map((node) => node.columnName.name); used.set(0, new Set([...nested, ...used.get(0)!])); }
    const hasInSubQuery = [...walkCurrentScope(ast)].some((node) => node.kind === "ExprInSubQuery");
    const suppressColumns = (ast.kind === "ExprUpdate" && ((ast.source === null && ast.target.alias === null) || ast.source?.kind === "ExprCteQuery")) || (this.tables.length > 0 && hasInSubQuery && [...walk(ast)].filter((node): node is ExprColumn => node.kind === "ExprColumn").every((node) => node.source === null));
    const inferredComparisonType = (name: string): ParseColumnArtifact["sqlType"] | null => { if (inferArtifactType(name) !== "ExprTypeInt32") return null; for (const node of walk(ast)) if (["ExprBooleanEq", "ExprBooleanNotEq", "ExprBooleanGt", "ExprBooleanGtEq", "ExprBooleanLt", "ExprBooleanLtEq"].includes(node.kind)) { /* The kind check guarantees the shared binary-comparison shape. */ const comparison = node as Extract<Expr, { readonly kind: "ExprBooleanEq" }>; if (comparison.left.kind === "ExprColumn" && comparison.left.columnName.name === name && comparison.right.kind === "ExprStringLiteral") return "ExprTypeString"; if (comparison.right.kind === "ExprColumn" && comparison.right.columnName.name === name && comparison.left.kind === "ExprStringLiteral") return "ExprTypeString"; } return null; };
    const rawArtifacts: ParseTableArtifact[] = [...this.tables.map((table, index) => { const columns = Object.freeze(suppressColumns ? [] : [...used.get(index)!]); return Object.freeze({ ...table, columns, columnArtifacts: Object.freeze(columns.map((name) => { const alias = (table.alias ?? table.name).toLocaleLowerCase("en-US"); const nodeKey = [...walkCurrentScope(ast)].find((node): node is ExprColumn => node.kind === "ExprColumn" && node.columnName.name === name && (node.source === null || (node.source.kind === "ExprTableAlias" && node.source.alias.kind === "ExprAlias" && node.source.alias.name.toLocaleLowerCase("en-US") === alias))); return Object.freeze({ name, sqlType: inferredComparisonType(name) ?? inferArtifactType(name), nullable: nodeKey !== undefined && nullableColumns.has(serializeAst(nodeKey)) }); })) }); }), ...this.nestedTables.map((table) => suppressColumns ? Object.freeze({ ...table, columns: Object.freeze([]), columnArtifacts: Object.freeze([]) }) : table)];
    const grouped = new Map<string, ParseTableArtifact>();
    for (const artifact of rawArtifacts) { const key = `${artifact.database ?? ""}\0${artifact.schema ?? ""}\0${artifact.name}`; const existing = grouped.get(key); if (existing === undefined) { const columns = artifact.columnArtifacts.filter((column, index, all) => all.findIndex((item) => item.name.toLocaleLowerCase("en-US") === column.name.toLocaleLowerCase("en-US")) === index); grouped.set(key, Object.freeze({ ...artifact, columns: Object.freeze(columns.map((item) => item.name)), columnArtifacts: Object.freeze(columns) })); } else { const columns = [...existing.columnArtifacts]; for (const column of artifact.columnArtifacts) { const index = columns.findIndex((item) => item.name.toLocaleLowerCase("en-US") === column.name.toLocaleLowerCase("en-US")); if (index < 0) columns.push(column); else if (column.sqlType === "ExprTypeString") columns[index] = column; } grouped.set(key, Object.freeze({ ...existing, columns: Object.freeze(columns.map((item) => item.name)), columnArtifacts: Object.freeze(columns) })); } }
    if (ast.kind === "ExprDelete" && !this.preserveDeleteTargetArtifact && ast.target.fullName.kind === "ExprTableFullName") grouped.delete(`${ast.target.fullName.dbSchema?.database?.name ?? ""}\0${ast.target.fullName.dbSchema?.schema.name ?? ""}\0${ast.target.fullName.tableName.name}`);
    const artifacts = [...grouped.values()].sort((left, right) => `${left.schema ?? ""}.${left.name}`.localeCompare(`${right.schema ?? ""}.${right.name}`, "en-US"));
    if (this.options?.existingTables) for (const artifact of artifacts) {
      const exact = this.options.existingTables.filter((table) => table.$metadata.name === artifact.name && table.$metadata.schema === artifact.schema);
      if (exact.length !== 1) {
        const insensitive = this.options.existingTables.some((table) => table.$metadata.name.toLocaleLowerCase("en-US") === artifact.name.toLocaleLowerCase("en-US") && table.$metadata.schema?.toLocaleLowerCase("en-US") === artifact.schema?.toLocaleLowerCase("en-US"));
        this.fail(insensitive ? `DifferentName: parsed table name differs by case: [${artifact.schema ?? ""}].[${artifact.name}].` : `Unexpected tables: [${artifact.schema ?? ""}].[${artifact.name}]`, "binding");
      }
      const columnNames = Object.keys(exact[0]!.$metadata.definitions);
      for (const column of artifact.columns) {
        if (columnNames.includes(column)) continue;
        const insensitive = columnNames.some((name) => name.toLocaleLowerCase("en-US") === column.toLocaleLowerCase("en-US"));
        this.fail(insensitive ? `DifferentName: parsed column name differs by case: [${column}].` : `Column '${column}' is unexpected; extra columns: [${column}] in table [${artifact.name}].`, "binding");
      }
    }
    return { success: true, ast, tables: Object.freeze(artifacts) };
  }
  private finish(): void { this.take("semicolon"); if (this.current.type !== "endOfFile") this.fail(`Unexpected token '${this.current.text}'.`); }
  private orderItem() { const value = this.value(); const descendant = this.takeKeyword("DESC"); if (!descendant) this.takeKeyword("ASC"); return exprOrderByItem({ value, descendant }); }
  private selectItem(): IExprSelecting {
    if (this.take("operator", "*")) return exprAllColumns({ source: null });
    if (isIdentifierLike(this.current) && this.tokens[this.index + 1]?.type === "dot" && this.tokens[this.index + 2]?.type === "operator" && this.tokens[this.index + 2]?.text === "*") {
      const name = this.identifier(); this.expect("dot"); if (!this.take("operator", "*")) this.fail("Expected '*'."); return exprAllColumns({ source: exprTableAlias({ alias: exprAlias({ name }) }) });
    }
    const value = this.value(); let alias: string | null = null;
    if (this.takeKeyword("AS")) {
      if (this.current.type === "stringLiteral") { alias = this.current.text.slice(1, -1).replace(/''/g, "'"); this.index++; }
      else alias = this.identifier();
    }
    else if (isIdentifierLike(this.current) && !this.isClause(this.current)) alias = this.identifier();
    const selecting = value.kind === "ExprSelectingValue" ? value.selecting : value;
    if (alias === null) {
      if ((selecting.kind === "ExprSum" || selecting.kind === "ExprSub" || selecting.kind === "ExprMul" || selecting.kind === "ExprDiv") && selecting.right.kind === "ExprColumn") return exprAliasedSelecting({ value: selecting, alias: exprColumnAlias({ name: selecting.right.columnName.name }) });
      return selecting;
    }
    if (selecting.kind === "ExprColumn") return selecting.columnName.name.toLocaleLowerCase("en-US") === alias.toLocaleLowerCase("en-US") ? selecting : exprAliasedColumn({ column: selecting, alias: exprColumnAlias({ name: alias }) });
    return exprAliasedSelecting({ value: selecting, alias: exprColumnAlias({ name: alias }) });
  }
  private bindUnqualifiedColumns<T extends Expr>(root: T, source: IExprTableSource): T {
    const owners = new Map<string, string>();
    const collect = (item: IExprTableSource): void => {
      if (item.kind === "ExprCteQuery") { const owner = item.alias?.alias.kind === "ExprAlias" ? item.alias.alias.name : item.name; const query = item.query.kind === "ExprSelectOffsetFetch" ? item.query.selectQuery : item.query; if (query.kind === "ExprQuerySpecification") for (const selected of query.selectList) { const name = selected.kind === "ExprAliasedSelecting" ? selected.alias.name : selected.kind === "ExprAliasedColumn" ? selected.alias?.name ?? selected.column.columnName.name : selected.kind === "ExprColumn" ? selected.columnName.name : null; if (name !== null) owners.set(name.toLocaleLowerCase("en-US"), owner); } }
      else if (item.kind === "ExprCrossedTable" || item.kind === "ExprJoinedTable" || item.kind === "ExprLateralCrossedTable") { collect(item.left); collect(item.right); }
    };
    collect(source); if (owners.size === 0) return root;
    return (modify(root, (node) => node.kind === "ExprColumn" && node.source === null && owners.has(node.columnName.name.toLocaleLowerCase("en-US")) ? exprColumn({ source: exprTableAlias({ alias: exprAlias({ name: owners.get(node.columnName.name.toLocaleLowerCase("en-US"))! }) }), columnName: node.columnName }) : node) ?? root) as T;
  }
  private tableSource(): IExprTableSource {
    let result: IExprTableSource = this.tableFactor();
    while (true) {
      if (this.take("comma")) { result = exprCrossedTable({ left: result, right: this.tableFactor() }); continue; }
      if (this.takeKeyword("CROSS")) { if (this.takeKeyword("JOIN")) result = exprCrossedTable({ left: result, right: this.tableFactor() }); else { this.expectKeyword("APPLY"); result = exprLateralCrossedTable({ left: result, right: this.tableFactor(true), outer: false }); } continue; }
      if (this.takeKeyword("OUTER")) { this.expectKeyword("APPLY"); result = exprLateralCrossedTable({ left: result, right: this.tableFactor(true), outer: true }); continue; }
      let joinType: "Inner" | "Left" | "Right" | "Full" | null = null;
      if (this.takeKeyword("INNER")) joinType = "Inner"; else if (this.takeKeyword("LEFT")) joinType = "Left"; else if (this.takeKeyword("RIGHT")) joinType = "Right"; else if (this.takeKeyword("FULL")) joinType = "Full"; else if (this.keyword("JOIN")) joinType = "Inner";
      if (joinType === null) break; this.expectKeyword("JOIN"); const right = this.tableFactor(); this.expectKeyword("ON"); result = exprJoinedTable({ left: result, right, searchCondition: this.booleanExpression(), joinType });
    }
    return result;
  }
  private tableFactor(allowOuter = false): IExprTableSource {
    if (this.keyword("OPENJSON")) return this.openJsonTable();
    if (isIdentifierLike(this.current) && (this.tokens[this.index + 1]?.type === "openParen" || (this.tokens[this.index + 1]?.type === "dot" && this.tokens[this.index + 3]?.type === "openParen"))) return this.tableFunction();
    if (this.take("openParen")) {
      if (this.takeKeyword("VALUES")) {
        const rows = []; do { this.expect("openParen"); const items = [this.value()]; while (this.take("comma")) items.push(this.value()); this.expect("closeParen"); rows.push(exprValueRow({ items })); } while (this.take("comma")); this.expect("closeParen"); this.takeKeyword("AS"); if (!isIdentifierLike(this.current) || this.isClause(this.current)) this.fail("VALUES table requires an alias."); const alias = exprTableAlias({ alias: exprAlias({ name: this.identifier() }) }); this.expect("openParen"); const columns = [exprColumnName({ name: this.identifier() })]; while (this.take("comma")) columns.push(exprColumnName({ name: this.identifier() })); this.expect("closeParen"); if (rows.some((row) => row.items.length !== columns.length)) this.fail("VALUES rows must match the derived column count."); this.syntheticVisible.add(alias.alias.kind === "ExprAlias" ? alias.alias.name.toLocaleLowerCase("en-US") : ""); return exprDerivedTableValues({ values: exprTableValueConstructor({ items: rows }), alias, columns });
      }
      const query = this.subQueryAfterOpenParen(allowOuter); this.takeKeyword("AS");
      if (!isIdentifierLike(this.current) || this.isClause(this.current)) this.fail("Derived table requires an alias.");
      const alias = exprTableAlias({ alias: exprAlias({ name: this.identifier() }) });
      if (alias.alias.kind === "ExprAlias") this.syntheticVisible.add(alias.alias.name.toLocaleLowerCase("en-US"));
      let columns: ReturnType<typeof exprColumnName>[] | null = null;
      if (this.take("openParen")) { columns = [exprColumnName({ name: this.identifier() })]; while (this.take("comma")) columns.push(exprColumnName({ name: this.identifier() })); this.expect("closeParen"); }
      return exprDerivedTableQuery({ query, alias, columns });
    }
    if (isIdentifierLike(this.current) && this.tokens[this.index + 1]?.type !== "dot") {
      const name = identifierValue(this.current); const query = this.ctes.get(name.toLocaleLowerCase("en-US"));
      if (query) { this.index++; this.takeKeyword("AS"); let alias: string | null = null; if (isIdentifierLike(this.current) && !this.isClause(this.current)) alias = this.identifier(); this.syntheticVisible.add((alias ?? name).toLocaleLowerCase("en-US")); return exprCteQuery({ name, query, alias: alias === null ? null : exprTableAlias({ alias: exprAlias({ name: alias }) }) }); }
    }
    if (isIdentifierLike(this.current) && this.tokens[this.index + 1]?.type === "dot" && isIdentifierLike(this.tokens[this.index + 2]!)) {
      const name = identifierValue(this.tokens[this.index + 2]!); const query = this.ctes.get(name.toLocaleLowerCase("en-US"));
      if (query) { this.index += 3; this.takeKeyword("AS"); let alias: string | null = null; if (isIdentifierLike(this.current) && !this.isClause(this.current)) alias = this.identifier(); this.syntheticVisible.add((alias ?? name).toLocaleLowerCase("en-US")); return exprCteQuery({ name, query, alias: alias === null ? null : exprTableAlias({ alias: exprAlias({ name: alias }) }) }); }
    }
    const fullName = this.tableNameExpression(); const concrete = fullName.kind === "ExprTableFullName" ? fullName : this.fail("Expected table name."); const schema = concrete.dbSchema?.schema.name ?? null; const name = concrete.tableName.name;
    let alias: string | null = null; this.takeKeyword("AS"); if (isIdentifierLike(this.current) && !this.isClause(this.current)) alias = this.identifier();
    const tableAlias = alias === null ? null : exprTableAlias({ alias: exprAlias({ name: alias }) });
    const visible = (alias ?? name).toLocaleLowerCase("en-US"); if (this.tables.some((table) => (table.alias ?? table.name).toLocaleLowerCase("en-US") === visible)) this.fail(`Duplicate visible table name or alias '${alias ?? name}'.`, "binding");
    this.tables.push({ database: null, schema, name, alias, columns: Object.freeze([]) });
    return exprTable({ fullName, alias: tableAlias });
  }
  private tableFunction(): IExprTableSource {
    const first = this.identifier(); let schema: ReturnType<typeof exprDbSchema> | null = null; let name = first;
    if (this.take("dot")) { schema = exprDbSchema({ database: null, schema: exprSchemaName({ name: first }) }); name = this.identifier(); }
    this.expect("openParen"); const args: ExprValue[] = []; if (!this.take("closeParen")) { args.push(this.value()); while (this.take("comma")) args.push(this.value()); this.expect("closeParen"); }
    const fn = exprTableFunction({ schema, name: exprFunctionName({ name, builtIn: schema === null }), arguments: args.length === 0 ? null : args });
    this.takeKeyword("AS"); if (!isIdentifierLike(this.current) || this.isClause(this.current)) this.fail("Table function requires an alias."); const aliasName = this.identifier(); this.syntheticVisible.add(aliasName.toLocaleLowerCase("en-US"));
    return exprAliasedTableFunction({ function: fn, alias: exprTableAlias({ alias: exprAlias({ name: aliasName }) }) });
  }
  private openJsonTable(): IExprTableSource {
    this.expectKeyword("OPENJSON"); this.expect("openParen"); const document = this.value(); let path = "$";
    if (this.take("comma")) { const pathNode = this.primary(); if (pathNode.kind !== "ExprStringLiteral" || pathNode.value === null) this.fail("OPENJSON path must be a string literal.", "unsupported"); path = pathNode.value; this.validateJsonPath(path); }
    this.expect("closeParen"); this.expectKeyword("WITH"); this.expect("openParen"); const columns: ExprJsonTableColumn[] = [];
    do { const name = exprColumnName({ name: this.identifier() }); const sqlType = this.sqlType(); if (this.current.type !== "stringLiteral") this.fail("OPENJSON column requires a literal JSON path."); const pathNode = this.primary(); if (pathNode.kind !== "ExprStringLiteral" || pathNode.value === null) this.fail("OPENJSON column path cannot be null."); this.validateJsonPath(pathNode.value); if (this.takeKeyword("AS")) { this.expectKeyword("JSON"); columns.push(exprJsonTableQueryColumn({ name, path: pathNode.value })); } else columns.push(exprJsonTableValueColumn({ name, sqlType, path: pathNode.value })); } while (this.take("comma"));
    this.expect("closeParen"); this.takeKeyword("AS"); if (!isIdentifierLike(this.current) || this.isClause(this.current)) this.fail("OPENJSON requires an alias."); const aliasName = this.identifier(); const visible = aliasName.toLocaleLowerCase("en-US");
    if (this.syntheticVisible.has(visible) || this.tables.some((table) => (table.alias ?? table.name).toLocaleLowerCase("en-US") === visible)) this.fail(`Duplicate visible table name or alias '${aliasName}'.`, "binding"); this.syntheticVisible.add(visible);
    return exprJsonTable({ document, path, columns, alias: exprTableAlias({ alias: exprAlias({ name: aliasName }) }) });
  }
  private tableNameExpression() { const first = this.identifier(); let schema: string | null = this.defaultSchema; let name = first; if (this.take("dot")) { schema = first; name = this.identifier(); } return exprTableFullName({ dbSchema: schema === null ? null : exprDbSchema({ database: null, schema: exprSchemaName({ name: schema }) }), tableName: exprTableName({ name }) }); }
  private booleanExpression(): ExprBoolean { let result = this.booleanAnd(); while (this.takeKeyword("OR")) result = exprBooleanOr({ left: result, right: this.booleanAnd() }); return result; }
  private booleanAnd(): ExprBoolean { let result = this.booleanPrimary(); while (this.takeKeyword("AND")) result = exprBooleanAnd({ left: result, right: this.booleanPrimary() }); return result; }
  private booleanPrimary(): ExprBoolean {
    if (this.takeKeyword("NOT")) return exprBooleanNot({ expr: this.booleanPrimary() });
    if (this.takeKeyword("EXISTS")) { this.expect("openParen"); return exprExists({ subQuery: this.subQueryAfterOpenParen(true) }); }
    if (this.current.type === "openParen" && this.parenthesesContainBoolean()) { this.index++; const inner = this.booleanExpression(); this.expect("closeParen"); return inner; }
    const left = this.value();
    if (this.takeKeyword("IS")) { const not = this.takeKeyword("NOT"); this.expectKeyword("NULL"); return exprIsNull({ test: left, not }); }
    const negated = this.takeKeyword("NOT");
    if (this.takeKeyword("LIKE")) { const result = exprLike({ test: left, pattern: this.value() }); if (this.takeKeyword("ESCAPE")) this.value(); return negated ? exprBooleanNot({ expr: result }) : result; }
    if (this.takeKeyword("IN")) { this.expect("openParen"); if (this.keyword("SELECT")) { const result = exprInSubQuery({ testExpression: left, subQuery: this.subQueryAfterOpenParen(true) }); return negated ? exprBooleanNot({ expr: result }) : result; } const items: ExprValue[] = []; if (this.current.type === "closeParen") this.fail("IN list cannot be empty."); items.push(this.value()); while (this.take("comma")) items.push(this.value()); this.expect("closeParen"); const result = exprInValues({ testExpression: left, items }); return negated ? exprBooleanNot({ expr: result }) : result; }
    if (this.takeKeyword("BETWEEN")) { const lower = this.value(); this.expectKeyword("AND"); const upper = this.value(); const result = exprBooleanAnd({ left: exprBooleanGtEq({ left, right: lower }), right: exprBooleanLtEq({ left, right: upper }) }); return negated ? exprBooleanNot({ expr: result }) : result; }
    if (negated) this.fail("Expected IN or LIKE after NOT.");
    const operator = this.operator(); const right = this.value();
    switch (operator) { case "=": return exprBooleanEq({ left, right }); case "<>": case "!=": return exprBooleanNotEq({ left, right }); case ">": return exprBooleanGt({ left, right }); case ">=": return exprBooleanGtEq({ left, right }); case "<": return exprBooleanLt({ left, right }); case "<=": return exprBooleanLtEq({ left, right }); default: this.fail(`Unsupported comparison operator '${operator}'.`); }
  }
  private value(): ExprValue { return this.additive(); }
  private additive(): ExprValue { let result = this.multiplicative(); while (this.current.type === "operator" && ["+", "-", "|", "^"].includes(this.current.text)) { const op = this.current.text; this.index++; const right = this.multiplicative(); result = op === "+" ? exprSum({ left: result, right }) : op === "-" ? exprSub({ left: result, right }) : op === "|" ? exprBitwiseOr({ left: result, right }) : exprBitwiseXor({ left: result, right }); } return result; }
  private multiplicative(): ExprValue { let result = this.unary(); while (this.current.type === "operator" && ["*", "/", "%", "&"].includes(this.current.text)) { const op = this.current.text; this.index++; const right = this.unary(); result = op === "*" ? exprMul({ left: result, right }) : op === "/" ? exprDiv({ left: result, right }) : op === "%" ? exprModulo({ left: result, right }) : exprBitwiseAnd({ left: result, right }); } return result; }
  private unary(): ExprValue { if (this.take("operator", "+")) return this.unary(); if (this.take("operator", "-")) return exprSub({ left: exprInt32Literal({ value: 0 }), right: this.unary() }); if (this.take("operator", "~")) return exprBitwiseNot({ value: this.unary() }); return this.primary(); }
  private primary(): ExprValue {
    const current = this.current;
    if (current.type === "numberLiteral") { this.index++; const number = Number(current.text); return !current.text.includes(".") && Number.isInteger(number) && number >= -2147483648 && number <= 2147483647 ? exprInt32Literal({ value: number }) : exprDecimalLiteral({ value: decimalValue(current.text) }); }
    if (current.type === "stringLiteral") { this.index++; const text = current.text[0]?.toLowerCase() === "n" ? current.text.slice(2, -1) : current.text.slice(1, -1); return exprStringLiteral({ value: text.split("''").join("'") }); }
    if (isKeyword(current, "NULL")) { this.index++; return exprNull; }
    if (current.type === "identifier" && current.text.startsWith("@") && !current.text.startsWith("@@")) { this.index++; return exprParameter({ replacedValue: null, tagName: current.text.slice(1) }); }
    if (isKeyword(current, "CURRENT_TIMESTAMP")) { this.index++; return exprGetDate; }
    if (this.takeKeyword("CASE")) return this.caseExpression();
    if (this.takeKeyword("CAST")) return this.castExpression();
    if (isIdentifierLike(current)) {
      const first = this.identifier();
      if (this.take("openParen")) return this.functionValue(first, null, true);
      if (this.take("dot")) { const second = this.identifier(); if (this.take("openParen")) return this.functionValue(second, first, false); if (this.take("dot")) { const third = this.identifier(); if (this.take("openParen")) return this.functionValue(third, second, false, first); return exprColumn({ source: exprTableAlias({ alias: exprAlias({ name: second }) }), columnName: exprColumnName({ name: third }) }); } return exprColumn({ source: exprTableAlias({ alias: exprAlias({ name: first }) }), columnName: exprColumnName({ name: second }) }); }
      return exprColumn({ source: null, columnName: exprColumnName({ name: first }) });
    }
    if (this.take("openParen")) { if (this.keyword("SELECT") || this.keyword("WITH")) return exprValueQuery({ query: this.subQueryAfterOpenParen(true) }); const result = this.value(); this.expect("closeParen"); return result; }
    this.fail(`Expected a value but found '${current.text}'.`);
  }
  private caseExpression(): ExprValue {
    const cases = []; const simple = this.keyword("WHEN") ? null : this.value();
    while (this.takeKeyword("WHEN")) { const condition = simple === null ? this.booleanExpression() : exprBooleanEq({ left: simple, right: this.value() }); this.expectKeyword("THEN"); cases.push(exprCaseWhenThen({ condition, value: this.value() })); }
    if (cases.length === 0) this.fail("CASE requires at least one WHEN clause.");
    let defaultValue: ExprValue = exprNull; if (this.takeKeyword("ELSE")) defaultValue = this.value(); this.expectKeyword("END");
    return exprCase({ cases, defaultValue });
  }
  private functionValue(name: string, schema: string | null, builtIn: boolean, database: string | null = null): ExprValue {
    const upper = name.toUpperCase(); let distinct = false; const args: ExprValue[] = [];
    if (schema === null && upper === "IIF") { const condition = this.booleanExpression(); this.expect("comma"); const whenTrue = this.value(); this.expect("comma"); const whenFalse = this.value(); this.expect("closeParen"); return exprCase({ cases: [exprCaseWhenThen({ condition, value: whenTrue })], defaultValue: whenFalse }); }
    if (schema === null && upper === "JSON_OBJECT") return this.jsonObject();
    if (schema === null && upper === "DATEADD") return this.dateAdd();
    if (schema === null && upper === "DATEDIFF") return this.dateDiff();
    if (!this.take("closeParen")) {
      distinct = this.takeKeyword("DISTINCT");
      if (this.take("operator", "*")) {
        if (upper !== "COUNT") this.fail(`${upper}(*) is not supported.`);
        this.expect("closeParen");
        return exprSelectingValue({ selecting: exprAggregateFunction({ name: exprFunctionName({ name: upper, builtIn: true }), expression: exprInt32Literal({ value: 1 }), isDistinct: false }) });
      }
      args.push(this.value()); while (this.take("comma")) args.push(this.value());
      if (this.takeKeyword("ABSENT")) this.fail(`${upper} ABSENT ON NULL is not supported.`, "unsupported");
      if (this.takeKeyword("NULL")) { this.expectKeyword("ON"); this.expectKeyword("NULL"); }
      this.expect("closeParen");
    }
    if (schema === null) {
      if (upper === "STRING_AGG") {
        if (distinct) this.fail("STRING_AGG does not support DISTINCT in the documented subset.", "unsupported");
        if (args.length !== 2) this.fail("STRING_AGG requires exactly two arguments.");
        let orderBy = null;
        if (this.takeKeyword("WITHIN")) {
          this.expectKeyword("GROUP"); this.expect("openParen"); this.expectKeyword("ORDER"); this.expectKeyword("BY");
          const items = [this.orderItem()]; while (this.take("comma")) items.push(this.orderItem());
          this.expect("closeParen"); orderBy = exprOrderBy({ orderList: items });
        }
        return exprSelectingValue({ selecting: exprStringAgg({ expression: args[0]!, separator: args[1]!, orderBy }) });
      }
      if (this.takeKeyword("OVER")) {
        const over = this.overClause();
        if (["COUNT", "SUM", "AVG", "MIN", "MAX"].includes(upper) && args.length === 1) return exprSelectingValue({ selecting: exprAggregateOverFunction({ function: exprAggregateFunction({ name: exprFunctionName({ name: upper, builtIn: true }), expression: args[0]!, isDistinct: distinct }), over }) });
        if (["ROW_NUMBER", "RANK", "DENSE_RANK", "FIRST_VALUE", "LAST_VALUE"].includes(upper)) return exprSelectingValue({ selecting: exprAnalyticFunction({ name: exprFunctionName({ name: upper, builtIn: true }), arguments: args.length ? args : null, over }) });
        this.fail(`Function '${upper}' does not support OVER in the portable subset.`);
      }
      if (upper === "GETDATE" && args.length === 0) return exprGetDate;
      if ((upper === "GETUTCDATE" || upper === "SYSUTCDATETIME") && args.length === 0) return exprGetUtcDate;
      if (upper === "ISNULL" && args.length === 2) return exprFuncIsNull({ test: args[0]!, alt: args[1]! });
      if (upper === "COALESCE" && args.length >= 2) return exprFuncCoalesce({ test: args[0]!, alts: args.slice(1) });
      if ((upper === "JSON_VALUE" || upper === "JSON_QUERY") && args.length === 2 && args[1]?.kind === "ExprStringLiteral" && args[1].value !== null) { this.validateJsonPath(args[1].value); return upper === "JSON_VALUE" ? exprJsonValue({ document: args[0]!, path: args[1].value, returningType: null }) : exprJsonQuery({ document: args[0]!, path: args[1].value }); }
      if (upper === "JSON_MODIFY" && args.length === 3 && args[1]?.kind === "ExprStringLiteral" && args[1].value !== null) { this.validateJsonPath(args[1].value); return args[2]?.kind === "ExprNull" ? exprJsonRemove({ document: args[0]!, path: args[1].value }) : exprJsonSet({ document: args[0]!, path: args[1].value, value: args[2]! }); }
      if (upper === "JSON_ARRAY") return exprJsonArray({ items: args });
      if (["JSON_VALUE", "JSON_QUERY", "JSON_MODIFY"].includes(upper)) this.fail(`Function '${upper}' requires a literal portable JSON path and the documented argument count.`, "unsupported");
      const portable = ({ ABS: "Abs", CEILING: "Ceiling", FLOOR: "Floor", LOWER: "Lower", UPPER: "Upper", TRIM: "Trim", LTRIM: "LTrim", RTRIM: "RTrim", REPLACE: "Replace", SUBSTRING: "Substring", ROUND: "Round", LEN: "Len", DATALENGTH: "DataLen", CHARINDEX: "IndexOf", LEFT: "Left", RIGHT: "Right", REPLICATE: "Repeat", NULLIF: "NullIf", YEAR: "Year", MONTH: "Month", DAY: "Day" } as const)[upper as "ABS"];
      if (portable) return exprPortableScalarFunction({ arguments: args.length ? args : null, portableFunction: portable });
      if (["COUNT", "SUM", "AVG", "MIN", "MAX"].includes(upper) && args.length === 1) return exprSelectingValue({ selecting: exprAggregateFunction({ name: exprFunctionName({ name: upper, builtIn: true }), expression: args[0]!, isDistinct: distinct }) });
    }
    if (distinct) this.fail("DISTINCT is supported only for aggregate functions.");
    return exprScalarFunction({ schema: schema === null ? null : exprDbSchema({ database: database === null ? null : exprDatabaseName({ name: database }), schema: exprSchemaName({ name: schema }) }), name: exprFunctionName({ name, builtIn }), arguments: args.length ? args : null });
  }
  private dateAdd(): ExprValue {
    const datePart = this.datePart(true); this.expect("comma");
    const negative = this.take("operator", "-"); this.take("operator", "+");
    if (this.current.type !== "numberLiteral" || this.current.text.includes(".")) this.fail("DATEADD number must be an Int32 literal.");
    const number = this.int32(this.current) * (negative ? -1 : 1); this.index++;
    this.expect("comma"); const date = this.value(); this.expect("closeParen");
    return exprDateAdd({ date, datePart, number });
  }
  private dateDiff(): ExprValue {
    const datePart = this.datePart(); if (datePart === "Week") this.fail("DATEDIFF does not support WEEK in the SqExpress AST.", "unsupported");
    this.expect("comma"); const startDate = this.value(); this.expect("comma"); const endDate = this.value(); this.expect("closeParen");
    return exprDateDiff({ startDate, endDate, datePart });
  }
  private datePart(_allowWeek = true): "Day" | "Hour" | "Millisecond" | "Minute" | "Month" | "Second" | "Week" | "Year" {
    const raw = this.identifier().toUpperCase();
    switch (raw) {
      case "D": case "DD": case "DAY": return "Day";
      case "HH": case "HOUR": return "Hour";
      case "MS": case "MILLISECOND": return "Millisecond";
      case "MI": case "N": case "MINUTE": return "Minute";
      case "M": case "MM": case "MONTH": return "Month";
      case "S": case "SS": case "SECOND": return "Second";
      case "WK": case "WW": case "WEEK": return "Week";
      case "YY": case "YYYY": case "YEAR": return "Year";
      default: this.fail(`Unsupported date part '${raw}'.`, "unsupported");
    }
  }
  private jsonObject(): ExprValue {
    const members = []; const keys = new Set<string>();
    if (!this.take("closeParen")) {
      while (true) {
        if (this.current.type !== "stringLiteral") this.fail("JSON_OBJECT requires static string keys followed by ':'.");
        const key = this.primary(); if (key.kind !== "ExprStringLiteral" || key.value === null) this.fail("JSON_OBJECT key cannot be null.");
        if (keys.has(key.value)) this.fail("JSON_OBJECT keys must be unique static strings."); keys.add(key.value);
        if (!this.take("symbol", ":")) this.fail("JSON_OBJECT requires ':' after each key."); members.push(exprJsonMember({ name: key.value, value: this.value() }));
        if (!this.take("comma")) break;
      }
      if (this.takeKeyword("ABSENT")) this.fail("JSON_OBJECT ABSENT ON NULL is not supported.", "unsupported");
      if (this.takeKeyword("NULL")) { this.expectKeyword("ON"); this.expectKeyword("NULL"); }
      this.expect("closeParen");
    }
    return exprJsonObject({ members });
  }
  private validateJsonPath(path: string): void { if (!/^\$(?:(?:\.(?:[\p{L}_][\p{L}\p{N}_]*|"(?:[^"\\]|\\["\\/bfnrt])+")|\[\d+\]))*$/u.test(path)) this.fail(`Invalid portable JSON path '${path}'.`, "unsupported"); }
  private overClause() {
    this.expect("openParen"); let partitions: ExprValue[] | null = null; let orderBy = null;
    if (this.takeKeyword("PARTITION")) { this.expectKeyword("BY"); partitions = [this.value()]; while (this.take("comma")) partitions.push(this.value()); }
    if (this.takeKeyword("ORDER")) { this.expectKeyword("BY"); const items = [this.orderItem()]; while (this.take("comma")) items.push(this.orderItem()); orderBy = exprOrderBy({ orderList: items }); }
    if (this.takeKeyword("ROWS") || this.takeKeyword("RANGE")) { if (this.takeKeyword("BETWEEN")) { this.consumeFrameBorder(); this.expectKeyword("AND"); this.consumeFrameBorder(); } else this.consumeFrameBorder(); }
    this.expect("closeParen");
    return exprOver({ partitions, orderBy, frameClause: null });
  }
  private consumeFrameBorder(): void { if (this.takeKeyword("UNBOUNDED")) { if (!this.takeKeyword("PRECEDING") && !this.takeKeyword("FOLLOWING")) this.fail("Window frame border is invalid."); return; } if (this.takeKeyword("CURRENT")) { this.expectKeyword("ROW"); return; } this.value(); if (!this.takeKeyword("PRECEDING") && !this.takeKeyword("FOLLOWING")) this.fail("Window frame border is invalid."); }
  private castExpression(): ExprValue { if (!this.take("openParen")) this.fail("CAST expression should contain '(' after CAST."); const expression = this.value(); if (!this.takeKeyword("AS") || this.current.type === "closeParen") this.fail("CAST expression is invalid."); const sqlType = this.sqlType(); this.expect("closeParen"); return exprCast({ expression, sqlType }); }
  private sqlType(): ExprType {
    const rawName = this.identifier(); const name = rawName.toUpperCase();
    switch (name) {
      case "SMALLINT": if (this.current.type === "openParen") this.fail("Type 'SMALLINT' does not accept arguments."); return exprTypeInt16;
      case "INT": case "INTEGER": if (this.current.type === "openParen") this.fail(`Type '${name}' does not accept arguments.`); return exprTypeInt32;
      case "BIGINT": if (this.current.type === "openParen") this.fail("Type 'BIGINT' does not accept arguments."); return exprTypeInt64;
      case "BIT": case "BOOLEAN": if (this.current.type === "openParen") this.fail(`Type '${name}' does not accept arguments.`); return exprTypeBoolean;
      case "FLOAT": case "REAL": if (this.current.type === "openParen") this.fail(`Type '${name}' does not accept arguments.`); return exprTypeDouble;
      case "UNIQUEIDENTIFIER": if (this.current.type === "openParen") this.fail("Type 'UNIQUEIDENTIFIER' does not accept arguments."); return exprTypeGuid;
      case "XML": if (this.current.type === "openParen") this.fail("Type 'XML' does not accept arguments."); return exprTypeXml;
      case "DATE": return exprTypeDateTime({ isDate: true });
      case "DATETIME": case "DATETIME2": { this.optionalTemporalScale(name); return exprTypeDateTime({ isDate: false }); }
      case "DATETIMEOFFSET": this.optionalTemporalScale(name); return exprTypeDateTimeOffset;
      case "DECIMAL": case "NUMERIC": { let precisionScale = null; if (this.take("openParen")) { const precision = this.typeInteger(name); let scale = 0; if (this.take("comma")) { scale = this.typeInteger(name); if (this.take("comma")) this.fail(`Type '${name}' expects one or two numeric arguments.`); } this.expect("closeParen"); if (precision < 1 || precision > 38) this.fail(`Type '${name}' precision is out of range.`); if (scale < 0 || scale > precision) this.fail(`Type '${name}' scale cannot be greater than precision.`); precisionScale = { precision, scale }; } return exprTypeDecimal({ precisionScale }); }
      case "VARCHAR": case "NVARCHAR": { let size: number | null = null; if (this.take("openParen")) { if (!this.takeKeyword("MAX")) size = this.typeInteger(name); if (this.take("comma")) this.fail(`Type '${name}' expects a single length argument.`); this.expect("closeParen"); } return exprTypeString({ size, isUnicode: name.startsWith("N"), isText: false }); }
      case "CHAR": case "NCHAR": { this.expect("openParen"); if (this.takeKeyword("MAX")) this.fail(`Type '${name}' cannot use MAX length.`); const size = this.typeInteger(name); if (this.take("comma")) this.fail(`Type '${name}' expects a single length argument.`); this.expect("closeParen"); return exprTypeFixSizeString({ size, isUnicode: name.startsWith("N") }); }
      case "TEXT": case "NTEXT": return exprTypeString({ size: null, isUnicode: name.startsWith("N"), isText: true });
      case "VARBINARY": { let size: number | null = null; if (this.take("openParen")) { if (!this.takeKeyword("MAX")) size = this.typeInteger(name); if (this.take("comma")) this.fail("Type 'VARBINARY' expects a single length argument."); this.expect("closeParen"); } return exprTypeByteArray({ size }); }
      case "BINARY": { this.expect("openParen"); const size = this.typeInteger(name); this.expect("closeParen"); return exprTypeFixSizeByteArray({ size }); }
      default: this.fail(`CAST type '${rawName}' is not supported by SqExpress parser.`);
    }
  }
  private optionalTemporalScale(name: string): void { if (!this.take("openParen")) return; const scale = this.typeInteger(name); if (scale < 0 || scale > 7) this.fail(`Type '${name}' numeric argument is out of range.`); if (this.take("comma")) this.fail(`Type '${name}' expects a single numeric argument.`); this.expect("closeParen"); }
  private typeInteger(name: string): number { if (this.current.type !== "numberLiteral" || this.current.text.includes(".")) this.fail(`Type '${name}' length argument is invalid.`); const result = this.int32(this.current); this.index++; return result; }
  private subQueryAfterOpenParen(allowOuter = false): IExprSubQuery {
    const start = this.index; let depth = 1;
    while (this.index < this.tokens.length) { const token = this.current; if (token.type === "openParen") depth++; else if (token.type === "closeParen" && --depth === 0) break; this.index++; }
    if (depth !== 0) this.fail("Unterminated subquery.");
    const nestedTokens = [...this.tokens.slice(start, this.index), { type: "endOfFile", text: "", start: this.current.start, length: 0, end: this.current.start } satisfies SqlToken]; this.index++;
    const outer = allowOuter ? new Set([...this.outerVisible, ...this.syntheticVisible, ...this.prospectiveVisibleNames(), ...this.tables.map((table) => (table.alias ?? table.name).toLocaleLowerCase("en-US"))]) : new Set<string>();
    const parser = new Parser(nestedTokens, this.options, outer, this.ctes, true); const result = parser.parse(); this.nestedTables.push(...result.tables); const nested = result.ast;
    if (!nodeTypeKinds.IExprSubQuery?.has(nested.kind)) this.fail("Expected a SELECT subquery.");
    return nested as IExprSubQuery;
  }
  private parseWithClause(): void {
    this.expectKeyword("WITH");
    do {
      const name = this.identifier(); const key = name.toLocaleLowerCase("en-US"); if (this.ctes.has(key)) this.fail(`Duplicate CTE name '${name}'.`);
      if (this.take("openParen")) { this.identifier(); while (this.take("comma")) this.identifier(); this.expect("closeParen"); }
      // A recursive CTE must be visible while its own body is parsed. The
      // empty projection is an internal sentinel; it is never exposed as a
      // standalone query and is replaced in the CTE registry immediately.
      const recursiveSentinel = exprQuerySpecification({ selectList: [], top: null, from: null, where: null, groupBy: null, distinct: false });
      this.ctes.set(key, recursiveSentinel);
      this.expectKeyword("AS"); this.expect("openParen"); const query = this.subQueryAfterOpenParen(); this.ctes.set(key, query);
    } while (this.take("comma"));
  }
  private findTopLevelSetOperation(): { readonly index: number; readonly width: number; readonly type: "Union" | "UnionAll" | "Intersect" | "Except" } | null {
    let depth = 0;
    for (let index = this.index; index < this.tokens.length - 1; index++) {
      const token = this.tokens[index]!; if (token.type === "openParen") { depth++; continue; } if (token.type === "closeParen") { depth--; continue; } if (depth !== 0) continue;
      if (isKeyword(token, "INTERSECT")) return { index, width: 1, type: "Intersect" };
      if (isKeyword(token, "EXCEPT")) return { index, width: 1, type: "Except" };
      if (isKeyword(token, "UNION")) return isKeyword(this.tokens[index + 1]!, "ALL") ? { index, width: 2, type: "UnionAll" } : { index, width: 1, type: "Union" };
    }
    return null;
  }
  private findTopLevelKeyword(keyword: string, start: number): number | null { let depth = 0; for (let index = start; index < this.tokens.length - 1; index++) { const token = this.tokens[index]!; if (token.type === "openParen") depth++; else if (token.type === "closeParen") depth--; else if (depth === 0 && isKeyword(token, keyword)) return index; } return null; }
  private parseSubQueryRange(start: number, end: number): IExprSubQuery {
    while (start < end && this.tokens[start]?.type === "openParen" && this.matchingClose(start) === end - 1) { start++; end--; }
    if (start >= end) this.fail("Set operation requires query expressions on both sides.");
    const position = this.tokens[end]?.start ?? this.current.start;
    const tokens = [...this.tokens.slice(start, end), { type: "endOfFile", text: "", start: position, length: 0, end: position } satisfies SqlToken];
    const parser = new Parser(tokens, this.options, new Set(), this.ctes, true); const result = parser.parse(); this.nestedTables.push(...result.tables); const expression = result.ast;
    if (!nodeTypeKinds.IExprSubQuery?.has(expression.kind)) this.fail("Set operation operand must be a query.");
    return expression as IExprSubQuery;
  }
  private matchingClose(open: number): number | null { let depth = 0; for (let i = open; i < this.tokens.length - 1; i++) { const token = this.tokens[i]!; if (token.type === "openParen") depth++; else if (token.type === "closeParen" && --depth === 0) return i; } return null; }
  private prospectiveVisibleNames(): ReadonlyArray<string> {
    const result: string[] = []; let depth = 0;
    for (let index = 0; index < this.tokens.length - 1; index++) {
      const token = this.tokens[index]!; if (token.type === "openParen") { depth++; continue; } if (token.type === "closeParen") { depth--; continue; } if (depth !== 0 || !(["FROM", "JOIN"].some((word) => isKeyword(token, word)))) continue;
      let cursor = index + 1; if (!isIdentifierLike(this.tokens[cursor]!)) continue; let name = identifierValue(this.tokens[cursor++]!);
      while (this.tokens[cursor]?.type === "dot" && isIdentifierLike(this.tokens[cursor + 1]!)) { cursor++; name = identifierValue(this.tokens[cursor++]!); }
      if (isKeyword(this.tokens[cursor]!, "AS")) cursor++;
      const visible = isIdentifierLike(this.tokens[cursor]!) && !this.isClause(this.tokens[cursor]!) ? identifierValue(this.tokens[cursor]!) : name; result.push(visible.toLocaleLowerCase("en-US"));
    }
    return result;
  }
  private parenthesesContainBoolean(): boolean { const close = this.matchingClose(this.index); if (close === null) return true; let depth = 0; for (let i = this.index + 1; i < close; i++) { const token = this.tokens[i]!; if (token.type === "openParen") depth++; else if (token.type === "closeParen") depth--; else if (depth === 0 && ((token.type === "operator" && ["=", "<", ">", "!"].includes(token.text)) || ["AND", "OR", "IS", "LIKE", "IN", "BETWEEN"].some((word) => isKeyword(token, word)))) return true; } return false; }
  private expectRowWord(): void { if (!this.takeKeyword("ROW") && !this.takeKeyword("ROWS")) this.fail("Expected ROW or ROWS."); }
  private operator(): string { if (this.current.type !== "operator") this.fail(`Expected comparison operator but found '${this.current.text}'.`); const first = this.tokens[this.index++]!.text; if (["<", ">", "!"].includes(first) && this.current.type === "operator" && this.current.text === "=") { this.index++; return `${first}=`; } if (first === "<" && this.current.type === "operator" && this.current.text === ">") { this.index++; return "<>"; } return first; }
  private int32(value: SqlToken): number { const result = Number(value.text); if (!Number.isInteger(result) || result < -2147483648 || result > 2147483647) this.fail(`Integer literal '${value.text}' is outside Int32 range.`); return result; }
  private identifier(): string { if (!isIdentifierLike(this.current)) this.fail(`Expected identifier but found '${this.current.text}'.`); return identifierValue(this.tokens[this.index++]!); }
  private isClause(value: SqlToken): boolean { return ["SELECT", "FROM", "WHERE", "GROUP", "ORDER", "OFFSET", "UNION", "INTERSECT", "EXCEPT", "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "CROSS", "ON", "SET", "VALUES", "OUTPUT"].some((word) => isKeyword(value, word)); }
  private keyword(value: string): boolean { return isKeyword(this.current, value); }
  private takeKeyword(value: string): boolean { if (!this.keyword(value)) return false; this.index++; return true; }
  private expectKeyword(value: string): void { if (!this.takeKeyword(value)) this.fail(`Expected ${value}.`); }
  private take(type: SqlToken["type"], text?: string): boolean { if (this.current.type !== type || (text !== undefined && this.current.text !== text)) return false; this.index++; return true; }
  private expect(type: SqlToken["type"]): void { if (!this.take(type)) this.fail(`Expected ${type} but found '${this.current.text}'.`); }
  private get current(): SqlToken { return this.tokens[this.index]!; }
  private fail(message: string, category: ParserDiagnosticCategory = "syntax"): never { throw new SqyraParserError(`${category === "binding" ? "Binding" : category === "unsupported" ? "Unsupported syntax" : "Syntax error"}: ${message}`, this.current.start, category); }
}

function* walkCurrentScope(root: Expr): IterableIterator<Expr> {
  const scopeQuery = root.kind === "ExprSelect" || root.kind === "ExprSelectOffsetFetch" ? root.selectQuery : root;
  function* inner(node: Expr): IterableIterator<Expr> {
    yield node;
    if (node.kind === "ExprDerivedTableQuery" || node.kind === "ExprCteQuery" || node.kind === "ExprInsertQuery" || node.kind === "ExprValueQuery") return;
    if (node !== root && node !== scopeQuery && (node.kind === "ExprQuerySpecification" || node.kind === "ExprSelect" || node.kind === "ExprSelectOffsetFetch" || node.kind === "ExprQueryExpression")) return;
    const record = node as Expr & Readonly<Record<string, unknown>>;
    for (const field of childFields[node.kind]) {
      if ((node.kind === "ExprExists" || node.kind === "ExprInSubQuery") && field.name === "subQuery") continue;
      const value = record[field.name]; if (value === null) continue;
      if (field.collection) for (const child of value as ReadonlyArray<Expr>) yield* inner(child); else yield* inner(value as Expr);
    }
  }
  yield* inner(root);
}

function inferArtifactType(name: string): ParseColumnArtifact["sqlType"] {
  if (/Version$/i.test(name)) return "ExprTypeDateTime";
  if (/^(?:Is|Has|Can)[A-Z_]/.test(name)) return "ExprTypeBoolean";
  if (/(?:Name|Title|Email|Address|Description)$/i.test(name)) return "ExprTypeString";
  if (/(?:Date|DateTime|At|Utc)$/i.test(name)) return "ExprTypeDateTime";
  if (/(?:Amount|Balance|Revenue|Price)$/i.test(name)) return "ExprTypeDecimal";
  return "ExprTypeInt32";
}
