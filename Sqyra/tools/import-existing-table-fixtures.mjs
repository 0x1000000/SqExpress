import { readFile, writeFile } from "node:fs/promises";

const path = new URL("../test/fixtures/cases.json", import.meta.url);
const cases = JSON.parse(await readFile(path, "utf8"));
const p = "SqExpress.Test.SqlParser.TSqlParserExistingTablesTest.";
const table = (schema, name, columns) => ({
  schema,
  name,
  columns: columns.map((column) =>
    typeof column === "string" ? { name: column, type: "int32" } : column,
  ),
});
const users = (columns = ["Id"], schema = "dbo", name = "Users") => table(schema, name, columns);
const orders = (columns = ["OrderId", "UserId"], schema = "dbo", name = "Orders") =>
  table(schema, name, columns);
const join =
  "SELECT [u].[Id],[o].[OrderId] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[Id]";
const ownedJoin = "SELECT u.Id,o.OrderId FROM dbo.Users u JOIN dbo.Orders o ON o.UserId=u.Id";
const entries = [
  ["TryParse_WithExistingTables_WhenExactMatch_ReturnsTrue", join, [users(), orders()]],
  [
    "TryParse_WithExistingTables_WhenParserFails_ReturnsParseError",
    "SELECT FROM [dbo].[Users]",
    [users()],
    "SELECT list is missing",
  ],
  [
    "TryParse_WithExistingTables_WhenProvidedTablesContainExtraEntries_StillReturnsTrue",
    "SELECT [u].[Id] FROM [dbo].[Users] [u]",
    [users(), orders(["OrderId"])],
  ],
  [
    "TryParse_WithExistingTables_WhenUnexpectedTableParsed_ReturnsMismatchError",
    join,
    [users()],
    "Unexpected tables: [dbo].[Orders]",
  ],
  [
    "TryParse_WithExistingTables_WhenExpectedHasMoreColumns_StillReturnsTrue",
    "SELECT [u].[Id],[u].[Name] FROM [dbo].[Users] [u]",
    [users(["Id", { name: "Name", type: "string" }, { name: "Email", type: "string" }])],
  ],
  [
    "TryParse_WithExistingTables_WhenUnexpectedColumnParsed_ReturnsMismatchError",
    "SELECT [u].[Id],[u].[Name] FROM [dbo].[Users] [u]",
    [users()],
    "extra columns: [Name]",
  ],
  [
    "TryParse_WithExistingTables_WhenWildcardQueryReferencesKnownColumn_ReturnsTrue",
    "SELECT * FROM [dbo].[Users] WHERE [UserId] = @userId",
    [users(["UserId", { name: "Name", type: "string" }, { name: "IsActive", type: "boolean" }])],
  ],
  [
    "TryParse_WithExistingTables_WhenWildcardQueryReferencesUnknownColumn_ReturnsMismatchError",
    "SELECT * FROM [dbo].[Users] WHERE [UserKey] = @userId",
    [users(["UserId", { name: "Name", type: "string" }])],
    "extra columns: [UserKey]",
  ],
  [
    "TryParse_WithExistingTables_WhenColumnTypeDiffers_StillReturnsTrue",
    join,
    [users([{ name: "Id", type: "string" }]), orders()],
  ],
  [
    "TryParse_WithExistingTables_WhenColumnNullabilityDiffers_StillReturnsTrue",
    `${join} WHERE [u].[Id] IS NULL`,
    [users(), orders()],
  ],
  [
    "TryParse_WithExistingTables_WhenIndexAndMetaDiffer_StillReturnsTrue",
    "SELECT [u].[Id] FROM [dbo].[Users] [u]",
    [users()],
  ],
  [
    "TryParse_WithExistingTables_WhenSimpleUpdateWithoutFrom_UsesUpdateTargetTableForValidation",
    "UPDATE [dbo].[Users] SET [Name]='X' WHERE [Id]=1",
    [users(["Id", { name: "Name", type: "string" }])],
  ],
  [
    "TryParse_WithExistingTables_WhenCustomDefaultSchemaMatchesUnqualifiedTable_ReturnsTrue",
    "SELECT [u].[Id] FROM [Users] [u]",
    [users(["Id"], "sales")],
    null,
    "sales",
  ],
  [
    "TryParse_WithExistingTables_WhenDefaultSchemaIsNull_MatchesSchemaLessTable",
    "SELECT [u].[Id] FROM [Users] [u]",
    [users(["Id"], null)],
    null,
    null,
  ],
  [
    "TryParse_WithExistingTables_WhenSchemaMatchesNonDbo_ReturnsTrue",
    "SELECT [u].[Id],[o].[OrderId] FROM [sales].[Users] [u] JOIN [sales].[Orders] [o] ON [o].[UserId]=[u].[Id]",
    [users(["Id"], "sales"), orders(undefined, "sales")],
  ],
  [
    "TryParse_WithExistingTables_WhenUnqualifiedProjectionColumnsResolveUniquelyInJoin_ReturnsTrue",
    "SELECT TOP 1 SalesOrderId, SUM(Amount) AS TotalSales FROM ops.Payment p JOIN ops.Invoice i ON p.InvoiceId = i.InvoiceId WHERE i.InvoiceDate >= DATEADD(month, -1, GETDATE()) GROUP BY SalesOrderId ORDER BY TotalSales DESC",
    [
      table("ops", "Payment", ["InvoiceId", { name: "Amount", type: "decimal" }]),
      table("ops", "Invoice", [
        "InvoiceId",
        "SalesOrderId",
        { name: "InvoiceDate", type: "dateTime" },
      ]),
    ],
  ],
  [
    "TryParse_WithExistingTables_WhenNamesDifferOnlyByCase_ReturnsMismatchError",
    "SELECT [U].[ID],[O].[ORDERID] FROM [DBO].[USERS] [U] JOIN [DBO].[ORDERS] [O] ON [O].[USERID]=[U].[ID]",
    [users(["id"], "dbo", "users"), orders(["orderid", "userid"], "dbo", "orders")],
    "DifferentName",
  ],
  ["TryParse_WithExistingTables_WhenSqlHasNoTablesAndExpectedIsEmpty_ReturnsTrue", "SELECT 1", []],
  [
    "TryParse_WithExistingTables_WhenSqlHasNoTablesButExpectedNotEmpty_ReturnsTrue",
    "SELECT 1",
    [users()],
  ],
  ["TryParse_WithExistingTables_WhenArgumentIsNull_SkipsValidationAndReturnsTrue", join, null],
  ["Parse_WhenExistingTablesAreNotProvided_SkipsValidationAndReturnsExpression", join, null],
  [
    "Parse_WithExistingTables_WhenParserFails_ThrowsSqExpressTSqlParserException",
    "SELECT FROM [dbo].[Users]",
    [users()],
    "SELECT list is missing",
  ],
  [
    "Parse_WithExistingTables_WhenParsedTableIsMissing_ThrowsSqExpressTSqlParserException",
    join,
    [users()],
    "Unexpected tables: [dbo].[Orders]",
  ],
  [
    "Parse_WithExistingTables_EmitsOwnedSqTablesAndReusesTheirColumns",
    ownedJoin,
    [users(), orders()],
  ],
  [
    "Parse_WithExistingTables_RepeatedAliasesOwnIndependentColumnSets",
    "SELECT a.Id,b.Id FROM dbo.Users a JOIN dbo.Users b ON a.Id=b.Id",
    [users()],
  ],
  [
    "Parse_WithExistingTables_NormalizesUnqualifiedColumnToOwnedAliasColumn",
    "SELECT Id FROM dbo.Users u",
    [users()],
  ],
  ["Parse_WithoutExistingTables_KeepsNeutralNodes", "SELECT u.Id FROM dbo.Users u", null],
  [
    "Parse_WithExistingTables_CrossApplyBindsPhysicalColumnsAcrossCorrelatedScopes",
    "SELECT u.Id,x.OrderId FROM dbo.Users u CROSS APPLY (SELECT o.OrderId FROM dbo.Orders o WHERE o.UserId=u.Id) x",
    [users(), orders()],
  ],
];
const byId = new Map(cases.map((item) => [item.id, item]));
for (const [
  name,
  sql,
  existingTables,
  expectedErrorPart = null,
  defaultSchema = "dbo",
] of entries) {
  byId.set(`${p}${name}#1`, {
    id: `${p}${name}#1`,
    sql,
    defaultSchema,
    expectedErrorPart,
    existingTables,
    compareSemantic: existingTables === null,
  });
}
await writeFile(path, `${JSON.stringify([...byId.values()], null, 2)}\n`);
console.log(`Imported ${entries.length} descriptor-aware parser fixtures.`);
