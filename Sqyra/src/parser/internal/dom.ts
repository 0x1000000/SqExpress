export type SqlDomStatementKind = "unknown" | "select" | "insert" | "update" | "delete" | "merge";
export interface SqlDomStatement {
  readonly kind: SqlDomStatementKind;
  readonly rawSql: string;
  readonly normalizedSql: string;
  readonly withClause: SqlDomWithClause | null;
  readonly topLevelSelect: SqlDomSelectClause | null;
  readonly tableReferences: ReadonlyArray<SqlDomTableReference>;
  readonly columnReferences: ReadonlyArray<SqlDomColumnReference>;
  readonly forJson: boolean;
  readonly forJsonWithoutArrayWrapper: boolean;
  readonly forJsonIncludeNullValues: boolean;
}
export interface SqlDomWithClause {
  readonly ctes: ReadonlyArray<SqlDomCte>;
}
export interface SqlDomCte {
  readonly name: string;
  readonly querySql: string;
}
export interface SqlDomSelectClause {
  readonly items: ReadonlyArray<SqlDomSelectItem>;
  readonly hasValidSelectListSyntax: boolean;
  readonly from: SqlDomTableSource | null;
  readonly hasFromClause: boolean;
  readonly whereSql: string | null;
  readonly groupBySql: string | null;
  readonly hasHavingClause: boolean;
  readonly havingSql: string | null;
  readonly orderBySql: string | null;
  readonly offsetFetchSql: string | null;
  readonly isDistinct: boolean;
  readonly topSql: string | null;
  readonly hasSetOperation: boolean;
}
export interface SqlDomSelectItem {
  readonly sql: string;
  readonly alias: string | null;
}
export type SqlDomTableSource =
  | SqlDomNamedTableSource
  | SqlDomDerivedTableSource
  | SqlDomValuesTableSource
  | SqlDomFunctionTableSource
  | SqlDomJoinedTableSource;
export interface SqlDomNamedTableSource {
  readonly kind: "named";
  readonly schema: string | null;
  readonly table: string;
  readonly alias: string | null;
}
export interface SqlDomDerivedTableSource {
  readonly kind: "derived";
  readonly sql: string;
  readonly alias: string | null;
}
export interface SqlDomValuesTableSource {
  readonly kind: "values";
  readonly sql: string;
  readonly alias: string | null;
  readonly columnAliases: ReadonlyArray<string>;
}
export interface SqlDomFunctionTableSource {
  readonly kind: "function";
  readonly name: string;
  readonly argumentsSql: string;
  readonly alias: string | null;
  readonly withSql: string | null;
}
export type SqlDomJoinType =
  "inner" | "left" | "right" | "full" | "cross" | "crossApply" | "outerApply";
export interface SqlDomJoinedTableSource {
  readonly kind: "joined";
  readonly left: SqlDomTableSource;
  readonly right: SqlDomTableSource;
  readonly joinType: SqlDomJoinType;
  readonly onSql: string | null;
}
export interface SqlDomTableReference {
  readonly schema: string | null;
  readonly table: string;
  readonly alias: string | null;
}
export interface SqlDomColumnReference {
  readonly sourceAlias: string;
  readonly columnName: string;
}
