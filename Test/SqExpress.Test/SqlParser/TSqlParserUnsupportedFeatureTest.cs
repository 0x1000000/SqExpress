using System.Collections.Generic;
using NUnit.Framework;
using SqExpress.SqlParser;
using SqExpress.Syntax;

namespace SqExpress.Test.SqlParser
{
    public class TSqlParserUnsupportedFeatureTest
    {
        [TestCaseSource(nameof(UnsupportedCases))]
        public void UnsupportedFeaturesReturnErrors(string sql, string expectedError)
        {

            var ok = TSqlParser.TryParse(sql, out IExpr? _, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Is.EqualTo(expectedError));
        }

        private static IEnumerable<TestCaseData> UnsupportedCases()
        {
            yield return new TestCaseData(
                    "SELECT [u].[UserId] FROM [dbo].[Users] [u] PIVOT (MAX([u].[UserId]) FOR [u].[UserId] IN([1],[2])) [p]",
                    "Feature 'PIVOT' is not supported by SqExpress parser.")
                .SetName("Unsupported_Select_Pivot");

            yield return new TestCaseData(
                    "SELECT [u].[UserId] FROM [dbo].[Users] [u] FOR JSON PATH",
                    "Feature 'FOR JSON/XML' is not supported by SqExpress parser.")
                .SetName("Unsupported_Select_ForJson");

            yield return new TestCaseData(
                    "SELECT [u].[UserId] FROM [dbo].[Users] [u] OPTION(RECOMPILE)",
                    "Feature 'OPTION(...)' is not supported by SqExpress parser.")
                .SetName("Unsupported_Select_OptionHint");

            yield return new TestCaseData(
                    "UPDATE [u] SET [u].[Name]='X' OUTPUT INSERTED.[UserId] INTO [dbo].[Audit]([UserId]) FROM [dbo].[Users] [u]",
                    "Feature 'OUTPUT ... INTO' is not supported by SqExpress parser.")
                .SetName("Unsupported_Update_OutputInto");

            yield return new TestCaseData(
                    "DELETE [u] OUTPUT DELETED.[UserId] INTO [dbo].[Audit]([UserId]) FROM [dbo].[Users] [u]",
                    "Feature 'OUTPUT ... INTO' is not supported by SqExpress parser.")
                .SetName("Unsupported_Delete_OutputInto");

            yield return new TestCaseData(
                    "MERGE [dbo].[Users] [t] USING [dbo].[UsersStaging] [s] ON [t].[UserId]=[s].[UserId] WHEN MATCHED THEN UPDATE SET [t].[Name]=[s].[Name] OUTPUT INSERTED.[UserId] INTO [dbo].[Audit]([UserId]);",
                    "Feature 'OUTPUT ... INTO' is not supported by SqExpress parser.")
                .SetName("Unsupported_Merge_OutputInto");
        }
    }
}


