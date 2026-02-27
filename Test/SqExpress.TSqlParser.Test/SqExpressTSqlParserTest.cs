using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using SqExpress.DbMetadata;
using SqExpress.SqlExport;
using SqExpress.Syntax;
using SqExpress.Syntax.Names;

namespace SqExpress.TSqlParser.Test
{
    public class SqExpressTSqlParserTest
    {
        [Test]
        public void BasicTest()
        {
            var parser = new SqExpressTSqlParser();

            var inputSql = "SELECT U.Id, U.Name, U2.Email, U2.Address FROM Users U CROSS JOIN Users U2";
            var outputSql = "SELECT [U].[Id],[U].[Name],[U2].[Email],[U2].[Address] FROM [dbo].[Users] [U] CROSS JOIN [dbo].[Users] [U2]";

            if (parser.TryParseScript(inputSql, out IExpr? expr, out var tables, out var errors))
            {
                var refs = expr.SyntaxTree().Descendants().OfType<ExprTable>().ToList();
                Assert.That(refs.Count, Is.EqualTo(2));
                Assert.That(refs.Select(i => (i.Alias?.Alias as ExprAlias)?.Name).OrderBy(i => i).ToArray(), Is.EqualTo(new[] { "U", "U2" }));
                Assert.That(expr.SyntaxTree().Descendants().OfType<SqTable>().Any(), Is.False);

                Assert.That(tables, Is.Not.Null);
                Assert.That(tables!.Count, Is.EqualTo(1));
                var sqTable = tables.Single();
                Assert.That(sqTable.FullName.TableName, Is.EqualTo("Users"));
                Assert.That(sqTable.Columns.Count, Is.EqualTo(4));
                Assert.That(sqTable.Columns[0].ColumnName.Name, Is.EqualTo("Id"));
                Assert.That(sqTable.Columns[1].ColumnName.Name, Is.EqualTo("Name"));
                Assert.That(sqTable.Columns[2].ColumnName.Name, Is.EqualTo("Email"));
                Assert.That(sqTable.Columns[3].ColumnName.Name, Is.EqualTo("Address"));

                var actualSql = expr.ToSql(TSqlExporter.Default);

                Assert.That(actualSql, Is.EqualTo(outputSql));
            }
            else
            {
                Assert.Fail(errors);
            }
        }

        [Test]
        public void BasicTest2()
        {
            var parser = new SqExpressTSqlParser();

            var inputSql = "UPDATE u SET u.[Name]=[o].[Title],[u].[IsActive]=1 FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] WHERE [o].[Title] LIKE 'A%'";
            var outputSql = "UPDATE [u] SET [u].[Name]=[o].[Title],[u].[IsActive]=1 FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] WHERE [o].[Title] LIKE 'A%'";

            if (parser.TryParseScript(inputSql, out IExpr? expr, out var error))
            {
                var actualSql = expr.ToSql(TSqlExporter.Default);

                Assert.That(actualSql, Is.EqualTo(outputSql));
            }
            else
            {
                Assert.Fail(error);
            }
        }

        [Test]
        public void BasicTest3()
        {
            var parser = new SqExpressTSqlParser();

            var inputSql = "WITH R AS(SELECT 1 UNION ALL SELECT [r].[N]+1 FROM [R] [r] WHERE [r].[N]<3)SELECT [u].[UserId],ISNULL([u].[Name],'NA') [UserName],LEN([u].[Name]) [NameLen],ROW_NUMBER()OVER(ORDER BY [u].[UserId]) [Rn],(SELECT COUNT(1) FROM (SELECT [l2].[UserId] FROM (SELECT [o3].[UserId] FROM [dbo].[Orders] [o3] WHERE [o3].[Amount]>0)[l2])[l1]) [NestedCnt],(SELECT COUNT(1) FROM [R] [rr] WHERE [rr].[N]<=2) [DepthCnt] FROM [dbo].[Users] [u] OUTER APPLY (SELECT [o].[OrderId] FROM [dbo].[Orders] [o] ORDER BY [o].[OrderId] DESC OFFSET 0 ROW FETCH NEXT 1 ROW ONLY)[oa] WHERE [u].[UserId] IN(SELECT [x].[UserId] FROM (SELECT [y].[UserId] FROM (SELECT [o2].[UserId] FROM [dbo].[Orders] [o2] WHERE [o2].[Amount]>0)[y])[x]) AND EXISTS(SELECT 1 FROM [R] [rx] WHERE [rx].[N]=1) ORDER BY [u].[UserId] OFFSET 0 ROW FETCH NEXT 20 ROW ONLY";
            var outputSql = "WITH [R] AS(SELECT 1 UNION ALL SELECT [r].[N]+1 FROM [R] [r] WHERE [r].[N]<3)SELECT [u].[UserId],ISNULL([u].[Name],'NA') [UserName],LEN([u].[Name]) [NameLen],ROW_NUMBER()OVER(ORDER BY [u].[UserId]) [Rn],(SELECT COUNT(1) FROM (SELECT [l2].[UserId] FROM (SELECT [o3].[UserId] FROM [dbo].[Orders] [o3] WHERE [o3].[Amount]>0)[l2])[l1]) [NestedCnt],(SELECT COUNT(1) FROM [R] [rr] WHERE [rr].[N]<=2) [DepthCnt] FROM [dbo].[Users] [u] OUTER APPLY (SELECT [o].[OrderId] FROM [dbo].[Orders] [o] ORDER BY [o].[OrderId] DESC OFFSET 0 ROW FETCH NEXT 1 ROW ONLY)[oa] WHERE [u].[UserId] IN(SELECT [x].[UserId] FROM (SELECT [y].[UserId] FROM (SELECT [o2].[UserId] FROM [dbo].[Orders] [o2] WHERE [o2].[Amount]>0)[y])[x]) AND EXISTS(SELECT 1 FROM [R] [rx] WHERE [rx].[N]=1) ORDER BY [u].[UserId] OFFSET 0 ROW FETCH NEXT 20 ROW ONLY";

            if (parser.TryParseScript(inputSql, out IExpr? expr, out var error))
            {
                var actualSql = expr.ToSql(TSqlExporter.Default);

                Assert.That(actualSql, Is.EqualTo(outputSql));
            }
            else
            {
                Assert.Fail(error);
            }
        }

        [Test]
        public void TableArtifactsComplexMultiRefTest()
        {
            var parser = new SqExpressTSqlParser();
            var inputSql =
                "SELECT [u].[UserId],[o].[OrderId],[u2].[Name] FROM [dbo].[Users] [u] JOIN [dbo].[Orders] [o] ON [o].[UserId]=[u].[UserId] LEFT JOIN [dbo].[Users] [u2] ON [u2].[UserId]=[o].[UserId] WHERE [u2].[IsActive]=1";

            if (parser.TryParseScript(inputSql, out IExpr? expr, out var tables, out var error))
            {
                Assert.That(expr.SyntaxTree().Descendants().OfType<SqTable>().Any(), Is.False);

                Assert.That(tables, Is.Not.Null);
                Assert.That(tables!.Count, Is.EqualTo(2));

                var users = tables.Single(i => i.FullName.TableName == "Users");
                var orders = tables.Single(i => i.FullName.TableName == "Orders");

                Assert.That(users.Columns.Select(i => i.ColumnName.Name).ToArray(), Is.EquivalentTo(new[] { "UserId", "Name", "IsActive" }));
                Assert.That(orders.Columns.Select(i => i.ColumnName.Name).ToArray(), Is.EquivalentTo(new[] { "OrderId", "UserId" }));
            }
            else
            {
                Assert.Fail(error);
            }
        }
    }
}
