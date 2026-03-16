using System;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.SqlParser;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios;

public class ScParserTypedParams : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        var allTables = AllTables.BuildAllTableList(context.Dialect);
        var tUser = AllTables.GetItUser(context.Dialect);

        var sample = (await SelectTop(1, tUser.UserId, tUser.LastName)
                .From(tUser)
                .OrderBy(tUser.UserId)
                .QueryList(context.Database, r => (UserId: tUser.UserId.Read(r), LastName: tUser.LastName.Read(r))))
            .SingleOrDefault();

        if (sample == default)
        {
            throw new Exception("Could not find a sample user for parser typed params scenario");
        }

        var originalLastName = sample.LastName;
        var updatedLastName = BuildUpdatedLastName(originalLastName);

        try
        {
            var parsedQuery = SqTSqlParser.Parse(
                    "SELECT U.LastName FROM dbo.ItUser U WHERE U.UserId = @userId",
                    allTables)
                .WithParamsAsQuery(("userId", sample.UserId));

            var parsedValueBefore = Convert.ToString(await parsedQuery.QueryScalar(context.Database)) ?? string.Empty;
            if (parsedValueBefore != originalLastName)
            {
                throw new Exception($"Expected LastName '{originalLastName}' before update but got '{parsedValueBefore}'");
            }

            await SqTSqlParser.Parse(
                    "UPDATE dbo.ItUser SET LastName = @lastName WHERE UserId = @userId",
                    allTables)
                .WithParamsAsNonQuery(
                    ("userId", sample.UserId),
                    ("lastName", updatedLastName))
                .Exec(context.Database);

            var parsedValueAfter = Convert.ToString(await parsedQuery.QueryScalar(context.Database)) ?? string.Empty;
            if (parsedValueAfter != updatedLastName)
            {
                throw new Exception($"Expected LastName '{updatedLastName}' after update but got '{parsedValueAfter}'");
            }

            context.WriteLine($"Parser typed params verified for user {sample.UserId}");
        }
        finally
        {
            await SqTSqlParser.Parse(
                    "UPDATE dbo.ItUser SET LastName = @lastName WHERE UserId = @userId",
                    allTables)
                .WithParamsAsNonQuery(
                    ("userId", sample.UserId),
                    ("lastName", originalLastName))
                .Exec(context.Database);
        }
    }

    private static string BuildUpdatedLastName(string originalLastName)
    {
        const string suffix = "_SQX";
        if (originalLastName.Length + suffix.Length <= 255)
        {
            return originalLastName + suffix;
        }

        return originalLastName.Substring(0, 255 - suffix.Length) + suffix;
    }
}
