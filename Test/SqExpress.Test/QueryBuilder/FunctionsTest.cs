using System;
using NUnit.Framework;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.QueryBuilder
{
    [TestFixture]
    public class FunctionsTest
    {
        [Test]
        public void AggBasicTest()
        {
            var userTable = Tables.User(Alias.Empty);

            Assert.AreEqual("SELECT COUNT(1) FROM [dbo].[user]", Select(CountOne()).From(userTable).Done().ToSql());
            Assert.AreEqual("SELECT COUNT([UserId]) FROM [dbo].[user]", Select(Count(userTable.UserId)).From(userTable).Done().ToSql());
            Assert.AreEqual("SELECT COUNT(DISTINCT [UserId]) FROM [dbo].[user]", Select(CountDistinct(userTable.UserId)).From(userTable).Done().ToSql());
            Assert.AreEqual("SELECT COUNT([UserId])OVER() FROM [dbo].[user]", Select(CountOver(userTable.UserId)).From(userTable).Done().ToSql());

            Assert.AreEqual("SELECT MIN([UserId]) FROM [dbo].[user]", Select(Min(userTable.UserId)).From(userTable).Done().ToSql());
            Assert.AreEqual("SELECT MIN(DISTINCT [UserId]) FROM [dbo].[user]", Select(MinDistinct(userTable.UserId)).From(userTable).Done().ToSql());
            Assert.AreEqual("SELECT MIN(DISTINCT [UserId]) FROM [dbo].[user]", Select(MinDistinct(userTable.UserId)).From(userTable).Done().ToSql());
            Assert.AreEqual("SELECT MIN(DISTINCT [UserId])OVER(ORDER BY [Version]) FROM [dbo].[user]", Select(MinDistinct(userTable.UserId).OverOrderBy(userTable.Version)).From(userTable).Done().ToSql());
            Assert.AreEqual("SELECT MIN(DISTINCT [UserId])OVER(PARTITION BY [Version] ORDER BY [Version]) FROM [dbo].[user]", Select(MinDistinct(userTable.UserId).OverPartitionBy(userTable.Version).OrderBy(userTable.Version)).From(userTable).Done().ToSql());
            Assert.AreEqual("SELECT MIN(DISTINCT [UserId])OVER(PARTITION BY [Version]) FROM [dbo].[user]", Select(MinDistinct(userTable.UserId).OverPartitionBy(userTable.Version).NoOrderBy()).From(userTable).Done().ToSql());



            Assert.AreEqual("SELECT MAX([UserId]) FROM [dbo].[user]", Select(Max(userTable.UserId)).From(userTable).Done().ToSql());
            Assert.AreEqual("SELECT MAX(DISTINCT [UserId]) FROM [dbo].[user]", Select(MaxDistinct(userTable.UserId)).From(userTable).Done().ToSql());
            Assert.AreEqual("SELECT MAX(DISTINCT [UserId])OVER() FROM [dbo].[user]", Select(MaxDistinct(userTable.UserId).Over()).From(userTable).Done().ToSql());

            Assert.AreEqual("SELECT SUM([UserId]) FROM [dbo].[user]", Select(Sum(userTable.UserId)).From(userTable).Done().ToSql());
            Assert.AreEqual("SELECT SUM(DISTINCT [UserId]) FROM [dbo].[user]", Select(SumDistinct(userTable.UserId)).From(userTable).Done().ToSql());
        }

        [Test]
        public void CaseWhenThenTest()
        {
            var userTable = Tables.User();

            var actual = Select(Case()
                    .When(userTable.FirstName == "John")
                    .Then("J")
                    .When(userTable.FirstName == "Bob")
                    .Then(false)
                    .Else(5)
                    .As("Result"))
                .From(userTable)
                .Done()
                .ToSql();

            Assert.AreEqual("SELECT CASE WHEN [A0].[FirstName]='John' THEN 'J' WHEN [A0].[FirstName]='Bob' THEN CAST(0 AS bit) ELSE 5 END [Result] FROM [dbo].[user] [A0]", actual);
        }

        [Test]
        public void ScalarFunctionTest()
        {
            Assert.AreEqual("COUNT()", ScalarFunctionSys("COUNT").ToSql());
            Assert.AreEqual("COUNT(1)", ScalarFunctionSys("COUNT", 1).ToSql());
            Assert.AreEqual("COUNT(1,'5')", ScalarFunctionSys("COUNT", 1, "5").ToSql());
            Assert.AreEqual("COUNT(1,'5','2020-10-19')", ScalarFunctionSys("COUNT", 1, "5", new DateTime(2020, 10, 19)).ToSql());

            Assert.AreEqual("[dbo].[m]]yFun'c]()", ScalarFunctionCustom("dbo", "m]yFun'c").ToSql());
            Assert.AreEqual("[dbo].[m]]yFun'c](1)", ScalarFunctionCustom("dbo", "m]yFun'c", 1).ToSql());
            Assert.AreEqual("[dbo].[m]]yFun'c](1,'5')", ScalarFunctionCustom("dbo", "m]yFun'c", 1, "5").ToSql());
            Assert.AreEqual("[dbo].[m]]yFun'c](1,'5','2020-10-19')", ScalarFunctionCustom("dbo", "m]yFun'c", 1, "5", new DateTime(2020,10,19)).ToSql());

            Assert.AreEqual("[db1].[dbo].[m]]yFun'c]()", ScalarFunctionDbCustom("db1", "dbo", "m]yFun'c").ToSql());
            Assert.AreEqual("[db1].[dbo].[m]]yFun'c](1)", ScalarFunctionDbCustom("db1", "dbo", "m]yFun'c", 1).ToSql());
            Assert.AreEqual("[db1].[dbo].[m]]yFun'c](1,'5')", ScalarFunctionDbCustom("db1", "dbo", "m]yFun'c", 1, "5").ToSql());
            Assert.AreEqual("[db1].[dbo].[m]]yFun'c](1,'5','2020-10-19')", ScalarFunctionDbCustom("db1", "dbo", "m]yFun'c", 1, "5", new DateTime(2020,10,19)).ToSql());
        }

        [Test]
        public void UnsafeValueTest()
        {
            Assert.AreEqual("SELECT 'Wh' + 'at ever'", Select(UnsafeValue("'Wh' + 'at ever'")).Done().ToSql());
        }
    }
}