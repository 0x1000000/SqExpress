import { bitwiseScenario } from "./bitwise.js";
import { createTablesScenario } from "./create-tables.js";
import { jsonMutationConstructionScenario } from "./json-mutation-construction.js";
import { jsonReadScenario } from "./json-read.js";
import { jsonTableScenario } from "./json-table.js";
import { forJsonScenario } from "./for-json.js";
import { likeScenario } from "./like.js";
import { deleteCustomersTopUserScenario } from "./delete-customers-top-user.js";
import { updateUsersScenario } from "./update-users.js";
import { selectPrefixedModelsScenario } from "./select-prefixed-models.js";
import { groupByExpressionScenario } from "./group-by-expression.js";
import { selectValueScenario } from "./select-value.js";
import { stringAggScenario } from "./string-agg.js";
import { parametrizationTypesScenario } from "./parametrization-types.js";
import { insertUsersScenario } from "./insert-users.js";
import { insertCompaniesScenario } from "./insert-companies.js";
import { transactionsScenario } from "./transactions.js";
import { transactionsAsyncScenario } from "./transactions-async.js";
import { parametrizationLimitBoundaryScenario } from "./parametrization-limit-boundary.js";
import { selectLogicScenario } from "./select-logic.js";
import { sqlInjectionsScenario } from "./sql-injections.js";
import { parserTypedParamsScenario } from "./parser-typed-params.js";
import { parserParamsExprValuesScenario } from "./parser-params-expr-values.js";
import { selectTopScenario } from "./select-top.js";
import { selectSetsScenario } from "./select-sets.js";
import { cteCrossScenario } from "./cte-cross.js";
import { portableScalarFunctionsScenario } from "./portable-scalar-functions.js";
import { dateDiffScenario } from "./date-diff.js";
import { allColumnTypesScenario } from "./all-column-types.js";
import { allColumnTypesExportImportScenario } from "./all-column-types-export-import.js";
import { pgMergeIdentityPolyfillScenario } from "./pg-merge-identity-polyfill.js";
import { tempTablesScenario } from "./temp-tables.js";
import { treeClosureScenario } from "./tree-closure.js";
import { analyticFunctionsOrdersScenario } from "./analytic-functions-orders.js";
import { createOrdersScenario } from "./create-orders.js";
import { cancellationScenario } from "./cancellation.js";
import { forJsonNestedBooksScenario } from "./for-json-nested-books.js";
import { cteScenario } from "./cte.js";
import { mergeScenario } from "./merge.js";
import { mergeExprScenario } from "./merge-expr.js";
import { mergeExprEdgeCasesScenario } from "./merge-expr-edge-cases.js";
import { transactionsDeadlockScenario } from "./transactions-deadlock.js";
import { createDynamicTableScenario } from "./create-dynamic-table.js";
import type { Scenario } from "./types.js";

// Preserves the BuildScenario chain in Test/SqExpress.IntTest/Program.cs.
// Two transaction invocations with distinct connection settings are exercised
// inside each transaction scenario module.
export const scenarios: ReadonlyArray<Scenario> = Object.freeze([
  createTablesScenario,
  insertUsersScenario,
  sqlInjectionsScenario,
  likeScenario,
  deleteCustomersTopUserScenario,
  insertCompaniesScenario,
  updateUsersScenario,
  selectPrefixedModelsScenario,
  allColumnTypesScenario,
  allColumnTypesExportImportScenario,
  selectLogicScenario,
  selectTopScenario,
  selectSetsScenario,
  tempTablesScenario,
  groupByExpressionScenario,
  stringAggScenario,
  selectValueScenario,
  createOrdersScenario,
  analyticFunctionsOrdersScenario,
  transactionsScenario,
  transactionsAsyncScenario,
  transactionsDeadlockScenario,
  mergeScenario,
  pgMergeIdentityPolyfillScenario,
  parametrizationTypesScenario,
  parserParamsExprValuesScenario,
  parserTypedParamsScenario,
  parametrizationLimitBoundaryScenario,
  mergeExprScenario,
  mergeExprEdgeCasesScenario,
  cancellationScenario,
  cteScenario,
  cteCrossScenario,
  treeClosureScenario,
  bitwiseScenario,
  jsonReadScenario,
  jsonMutationConstructionScenario,
  jsonTableScenario,
  forJsonScenario,
  forJsonNestedBooksScenario,
  dateDiffScenario,
  createDynamicTableScenario,
  portableScalarFunctionsScenario,
]);
