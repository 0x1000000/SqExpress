import { readFile, writeFile } from "node:fs/promises";

const path = new URL("../test/fixtures/cases.json", import.meta.url);
const cases = JSON.parse(await readFile(path, "utf8"));
const prefix = "SqExpress.Test.SqlParser.";
const additions = [
  [
    "TSqlParserUnicodeLiteralTest.UnicodePrefixedStringLiteralParses",
    "SELECT [u].[UserId] FROM [dbo].[Users] [u] WHERE [u].[Name]=N'A'",
  ],
  [
    "TSqlParserEdgeBehaviorTest.DerivedTableGroupByQuery_WithSingleQuotedInnerAlias_Parses",
    `SELECT CustomerName.CustomerName, COUNT(1) As OrdersNum FROM (SELECT C.CustomerId, CASE WHEN U.UserId IS NOT NULL THEN U.FirstName + ' ' + U.LastName ELSE Co.CompanyName END AS 'CustomerName' FROM Customer C LEFT JOIN [User] U ON U.UserId = C.UserId LEFT JOIN [Company] Co ON Co.CompanyId = C.CompanyId) AS CustomerName INNER JOIN ItOrder Ord ON Ord.CustomerId = CustomerName.CustomerId GROUP BY CustomerName.CustomerName`,
  ],
  [
    "TSqlParserTableExtractionTest.ExtractTables_CteAndSubQuery_InfersTypesAndSkipsCtePseudoTable",
    "wItH C aS(SeLeCt o.UserId,o.Amount FrOm dbo.Orders o WhErE o.Title LiKe 'A%') SeLeCt u.UserId,u.Name,c.Amount fRoM dbo.Users u JoIn (SeLeCt x.UserId,x.Amount FrOm C x) c On c.UserId=u.UserId wHeRe u.CreatedAt>='2025-01-01' aNd u.IsActive=1",
  ],
  [
    "TSqlParserTableExtractionTest.ExtractTables_Merge_DetectsTargetAndSourceWithTypes",
    "mErGe dbo.Users t uSiNg (sElEcT s.UserId,s.Name,s.Balance FrOm dbo.UsersStaging s) src oN t.UserId=src.UserId wHeN mAtChEd tHeN uPdAtE sEt t.Name=src.Name,t.UpdatedAt='2025-01-02' wHeN nOt mAtChEd tHeN iNsErT(UserId,Name,Balance) vAlUeS(src.UserId,src.Name,src.Balance);",
  ],
  [
    "TSqlParserTableExtractionTest.ExtractTables_Update_DetectsJoinedTablesAndTypeHints",
    "uPdAtE u SeT u.Name='X',u.TotalAmount=12.5 fRoM dbo.Users u jOiN dbo.Orders o On o.UserId=u.UserId wHeRe o.Title LiKe 'A%' aNd u.IsDeleted iS nUlL;",
  ],
  [
    "TSqlParserTableExtractionTest.ExtractTables_Delete_DetectsExistsSubquerySource",
    "dElEtE u fRoM dbo.Users u wHeRe u.Email='a@b.com' aNd eXiStS(sElEcT 1 FrOm dbo.Orders o wHeRe o.UserId=u.UserId aNd o.Amount>1.25);",
  ],
];
const byId = new Map(cases.map((item) => [item.id, item]));
for (const [name, sql] of additions)
  byId.set(`${prefix}${name}#1`, { id: `${prefix}${name}#1`, sql, defaultSchema: "dbo" });
await writeFile(path, `${JSON.stringify([...byId.values()], null, 2)}\n`);
console.log(`Imported ${additions.length} final ordinary parser fixtures.`);
