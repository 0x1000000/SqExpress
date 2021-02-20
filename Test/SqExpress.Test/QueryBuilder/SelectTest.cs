using System;
using NUnit.Framework;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.QueryBuilder
{
    [TestFixture]
    public class SelectTest
    {
        [Test]
        public void BasicTest()
        {
            var u = Tables.User();
            var u2 = Tables.User("U2");
            var u3 = Tables.User();
            var u4 = Tables.User("U4");

            var actual = Select(u.UserId, u4.FirstName)
                .From(u)
                .InnerJoin(u2, @on: u2.UserId == u.UserId)
                .LeftJoin(u3, @on: u3.UserId == u2.UserId)
                .FullJoin(u4, @on: u4.UserId == u3.UserId)
                .Where(u3.UserId == 5)
                .Done()
                .ToSql();

            var expected = "SELECT [A0].[UserId],[U4].[FirstName] " +
                           "FROM [dbo].[user] [A0] " +
                           "JOIN [dbo].[user] [U2] ON [U2].[UserId]=[A0].[UserId] " +
                           "LEFT JOIN [dbo].[user] [A1] ON [A1].[UserId]=[U2].[UserId] " +
                           "FULL JOIN [dbo].[user] [U4] ON [U4].[UserId]=[A1].[UserId] " +
                           "WHERE [A1].[UserId]=5";

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestDbName()
        {
            var u = new User("SomeDB", Alias.Empty);

            var e = SelectOne().From(u).Done();

            Assert.AreEqual("SELECT 1 FROM [SomeDB].[dbo].[user]", e.ToSql());
            Assert.AreEqual("SELECT 1 FROM \"SomeDB\".\"public\".\"user\"", e.ToPgSql());
            Assert.AreEqual("SELECT 1 FROM `SomeDB`.`user`", e.ToMySql());
        }

        [Test]
        public void TopTest()
        {
            var actual = SelectDistinct(Literal(2)).Done().ToSql();
            Assert.AreEqual("SELECT DISTINCT 2", actual);

            actual = SelectTopDistinct(Literal(3), Literal(2)).Done().ToSql();
            Assert.AreEqual("SELECT DISTINCT TOP 3 2", actual);

            actual = SelectTopDistinct(4, Literal(2)).Done().ToSql();
            Assert.AreEqual("SELECT DISTINCT TOP 4 2", actual);

            actual = SelectTop(5, Literal(2)).Done().ToSql();
            Assert.AreEqual("SELECT TOP 5 2", actual);

            actual = SelectTop(Literal(6), Literal(2)).Done().ToSql();
            Assert.AreEqual("SELECT TOP 6 2", actual);

            actual = SelectDistinct(2).Done().ToSql();
            Assert.AreEqual("SELECT DISTINCT 2", actual);

            actual = SelectTopDistinct(Literal(3), 2).Done().ToSql();
            Assert.AreEqual("SELECT DISTINCT TOP 3 2", actual);

            actual = SelectTopDistinct(4, 2).Done().ToSql();
            Assert.AreEqual("SELECT DISTINCT TOP 4 2", actual);

            actual = SelectTop(5, 2).Done().ToSql();
            Assert.AreEqual("SELECT TOP 5 2", actual);

            actual = SelectTop(Literal(6), 2, "Hi").Done().ToSql();
            Assert.AreEqual("SELECT TOP 6 2,'Hi'", actual);

            actual = SelectTop(Literal(9) % 7, 2, AllColumns()).Done().ToSql();
            Assert.AreEqual("SELECT TOP 9%7 2,*", actual);

            actual = SelectDistinct(2, AllColumns()).Done().ToSql();
            Assert.AreEqual("SELECT DISTINCT 2,*", actual);

            actual = SelectTopDistinct(Literal(3), 2, AllColumns()).Done().ToSql();
            Assert.AreEqual("SELECT DISTINCT TOP 3 2,*", actual);

            actual = SelectTopDistinct(4, 2, AllColumns()).Done().ToSql();
            Assert.AreEqual("SELECT DISTINCT TOP 4 2,*", actual);

        }

        [Test]
        public void Set_SimpleText()
        {
            var s1 = Select(Literal(1));
            var s2 = Select(Literal(2));

            var actual = s1.UnionAll(s2).Done().ToSql();
            Assert.AreEqual("SELECT 1 UNION ALL SELECT 2", actual);

            actual = s1.Union(s2).Done().ToSql();
            Assert.AreEqual("SELECT 1 UNION SELECT 2", actual);

            actual = s1.Except(s2).Done().ToSql();
            Assert.AreEqual("SELECT 1 EXCEPT SELECT 2", actual);

            actual = s1.Intersect(s2).Done().ToSql();
            Assert.AreEqual("SELECT 1 INTERSECT SELECT 2", actual);
        }

        [Test]
        public void Set_CombinationTest()
        {
            var s1 = Select(Literal(1));
            var s2 = Select(Literal(2));
            var s3 = Select(Literal(4));

            var union1 = s1.UnionAll(s2).UnionAll(s3);
            var union2 = s1.UnionAll(s2).UnionAll(s3);

            var actual = union1.Except(union2).Done().ToSql();

            Assert.AreEqual("SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 4 EXCEPT (SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 4)", actual);
        }

        [Test]
        public void Where_NullSupport()
        {
            var user = Tables.User(Alias.Empty);

            var actual = Select(user.UserId)
                .From(user)
                .Where(null)
                .OrderBy(user.UserId)
                .Done()
                .ToSql();

            Assert.AreEqual("SELECT [UserId] FROM [dbo].[user] ORDER BY [UserId]", actual);
        }

        [Test]
        public void GroupByTest()
        {
            var u = Tables.User(Alias.Empty);

            var actual = Select(u.FirstName).From(u).GroupBy(u.FirstName).Done().ToSql();
            var expected = "SELECT [FirstName] FROM [dbo].[user] GROUP BY [FirstName]";
            Assert.AreEqual(expected, actual);

            actual = Select(u.FirstName, u.LastName, u.Email).From(u).GroupBy(u.FirstName, u.LastName, u.Email).Done().ToSql();
            expected = "SELECT [FirstName],[LastName],[Email] FROM [dbo].[user] GROUP BY [FirstName],[LastName],[Email]";
            Assert.AreEqual(expected, actual);
        }


        [Test]
        public void OrderBy_basic()
        {
            var u = Tables.User(Alias.Empty);

            var actual = Select(u.UserId, u.FirstName)
                .From(u)
                .OrderBy(u.FirstName, Desc(u.LastName))
                .Done()
                .ToSql();

            var expected = "SELECT [UserId],[FirstName] FROM [dbo].[user] ORDER BY [FirstName],[LastName] DESC";

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void OrderBy_Join()
        {
            var u = Tables.User();
            var u2 = Tables.User();

            var actual = Select(u.UserId)
                .From(u)
                .InnerJoin(u2, @on: u.UserId == u2.UserId)
                .OrderBy(u.FirstName)
                .Done()
                .ToSql();

            var expected = "SELECT [A0].[UserId] FROM [dbo].[user] [A0] JOIN [dbo].[user] [A1] ON [A0].[UserId]=[A1].[UserId] ORDER BY [A0].[FirstName]";

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void OrderBy_Union()
        {
            var u = Tables.User();
            var u2 = Tables.User();

            var actual = Select(u.UserId).From(u).UnionAll(Select(u2.UserId).From(u2))
                .OrderBy(u.UserId)
                .Done()
                .ToSql();

            var expected = "SELECT [A0].[UserId] FROM [dbo].[user] [A0] UNION ALL SELECT [A1].[UserId] FROM [dbo].[user] [A1] ORDER BY [A0].[UserId]";

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void OrderBy_Offset()
        {
            var u = Tables.User(Alias.Empty);

            var actual = Select(u.UserId, u.FirstName)
                .From(u)
                .OrderBy(u.FirstName, Desc(u.LastName))
                .Offset(5)
                .Done()
                .ToSql();

            var expected = "SELECT [UserId],[FirstName] FROM [dbo].[user] ORDER BY [FirstName],[LastName] DESC OFFSET 5 ROW";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void OrderBy_OffsetFetch()
        {
            var u = Tables.User(Alias.Empty);

            var actual = Select(u.UserId, u.FirstName)
                .From(u)
                .OrderBy(u.FirstName, Desc(u.LastName))
                .OffsetFetch(5, 50)
                .Done()
                .ToSql();

            var expected = "SELECT [UserId],[FirstName] FROM [dbo].[user] ORDER BY [FirstName],[LastName] DESC OFFSET 5 ROW FETCH NEXT 50 ROW ONLY";

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TopVsLimit()
        {
            var u = Tables.User();
            var u2 = Tables.User();

            IExprQuery expr = SelectTop(7, u.UserId).From(u).Done();
            var actual = expr.ToSql();

            Assert.AreEqual("SELECT TOP 7 [A0].[UserId] FROM [dbo].[user] [A0]", actual);
            actual = expr.ToPgSql();
            Assert.AreEqual("SELECT \"A0\".\"UserId\" FROM \"public\".\"user\" \"A0\" LIMIT 7", actual);

            //OrderBy OFFSET
            expr = SelectTop(7, u.UserId).From(u).OrderBy(u.FirstName).Done();
            actual = expr.ToSql();
            Assert.AreEqual("SELECT TOP 7 [A0].[UserId] FROM [dbo].[user] [A0] ORDER BY [A0].[FirstName]", actual);
            actual = expr.ToPgSql();
            Assert.AreEqual("SELECT \"A0\".\"UserId\" FROM \"public\".\"user\" \"A0\" ORDER BY \"A0\".\"FirstName\" LIMIT 7", actual);
            actual = expr.ToMySql();
            Assert.AreEqual("SELECT `A0`.`UserId` FROM `user` `A0` ORDER BY `A0`.`FirstName` LIMIT 7", actual);

            //OrderBy OFFSET
            expr = SelectTop(7, u.UserId).From(u).OrderBy(u.FirstName).Offset(5).Done();
            actual = expr.ToSql();
            Assert.AreEqual("SELECT TOP 7 [A0].[UserId] FROM [dbo].[user] [A0] ORDER BY [A0].[FirstName] OFFSET 5 ROW", actual);
            actual = expr.ToPgSql();
            Assert.AreEqual("SELECT \"A0\".\"UserId\" FROM \"public\".\"user\" \"A0\" ORDER BY \"A0\".\"FirstName\" LIMIT 7 OFFSET 5 ROW", actual);
            actual = expr.ToMySql();
            Assert.AreEqual("SELECT `A0`.`UserId` FROM `user` `A0` ORDER BY `A0`.`FirstName` LIMIT 7 OFFSET 5", actual);

            //Join
            expr = SelectTop(7, u.UserId).From(u).InnerJoin(u2, on: u2.UserId == u.UserId).Done();

            actual = expr.ToSql();
            Assert.AreEqual("SELECT TOP 7 [A0].[UserId] FROM [dbo].[user] [A0] JOIN [dbo].[user] [A1] ON [A1].[UserId]=[A0].[UserId]", actual);
            actual = expr.ToPgSql();
            Assert.AreEqual("SELECT \"A0\".\"UserId\" FROM \"public\".\"user\" \"A0\" JOIN \"public\".\"user\" \"A1\" ON \"A1\".\"UserId\"=\"A0\".\"UserId\" LIMIT 7", actual);

            //Except
            expr = Select(u.UserId, CountOneOver()).From(u).Except(SelectTop(7, u2.UserId, CountOneOver()).From(u2)).Done();
            actual = expr.ToSql();
            Assert.AreEqual("SELECT [A0].[UserId],COUNT(1)OVER() FROM [dbo].[user] [A0] EXCEPT SELECT TOP 7 [A1].[UserId],COUNT(1)OVER() FROM [dbo].[user] [A1]", actual);
            actual = expr.ToPgSql();
            Assert.AreEqual("SELECT \"A0\".\"UserId\",COUNT(1)OVER() FROM \"public\".\"user\" \"A0\" EXCEPT (SELECT \"A1\".\"UserId\",COUNT(1)OVER() FROM \"public\".\"user\" \"A1\" LIMIT 7)", actual);
        }
    }
}