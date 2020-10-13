using System;
using NUnit.Framework;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.QueryBuilder
{
    [TestFixture]
    public class DeleteTest
    {
        [Test]
        public void DeleteFromTableTest()
        {
            var table = Tables.User(Alias.Empty);

            Assert.AreEqual("DELETE [dbo].[user]", Delete(table).All().ToSql());
            Assert.AreEqual("DELETE [dbo].[user] WHERE [UserId] IN(1,2)",
                Delete(table).Where(table.UserId.In(1, 2)).ToSql());

            table = Tables.User();
            Assert.AreEqual("DELETE [A0] FROM [dbo].[user] [A0]", Delete(table).All().ToSql());
            Assert.AreEqual("DELETE [A0] FROM [dbo].[user] [A0] WHERE [A0].[UserId] IN(1,2)",
                Delete(table).Where(table.UserId.In(1, 2)).ToSql());
        }

        [Test]
        public void DeleteFromTest()
        {
            var tUserSinAlias = Tables.User(Alias.Empty);
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();

            Assert.AreEqual("DELETE [dbo].[user] FROM [dbo].[user]",
                Delete(tUserSinAlias).From(tUserSinAlias).All().ToSql());
            Assert.AreEqual("DELETE [A0] FROM [dbo].[user] [A0]", Delete(tUser).From(tUser).All().ToSql());


            //Join

            var actual = Delete(tUser)
                .From(tUser)
                .InnerJoin(tCustomer, @on: tCustomer.UserId == tUser.UserId)
                .All()
                .ToSql();
            var expected =
                "DELETE [A0] FROM [dbo].[user] [A0] JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId]";
            Assert.AreEqual(expected, actual);

            actual = Delete(tUser).From(tUser).LeftJoin(tCustomer, @on: tCustomer.UserId == tUser.UserId).All().ToSql();
            expected =
                "DELETE [A0] FROM [dbo].[user] [A0] LEFT JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId]";
            Assert.AreEqual(expected, actual);

            actual = Delete(tUser).From(tUser).FullJoin(tCustomer, @on: tCustomer.UserId == tUser.UserId).All().ToSql();
            expected =
                "DELETE [A0] FROM [dbo].[user] [A0] FULL JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId]";
            Assert.AreEqual(expected, actual);

            actual = Delete(tUser).From(tUser).CrossJoin(tCustomer).All().ToSql();
            expected = "DELETE [A0] FROM [dbo].[user] [A0] CROSS JOIN [dbo].[Customer] [A1]";
            Assert.AreEqual(expected, actual);

            var tUser2 = Tables.User();
            actual = Delete(tUser)
                .From(tUser)
                .InnerJoin(tCustomer, @on: tCustomer.UserId == tUser.UserId)
                .InnerJoin(tUser2, @on: tUser.UserId == tUser2.UserId)
                .All()
                .ToSql();
            expected =
                "DELETE [A0] FROM [dbo].[user] [A0] JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId] JOIN [dbo].[user] [A2] ON [A0].[UserId]=[A2].[UserId]";
            Assert.AreEqual(expected, actual);

            //Join Where
            actual = Delete(tUser)
                .From(tUser)
                .InnerJoin(tCustomer, @on: tCustomer.UserId == tUser.UserId)
                .Where(tUser.UserId.In(7))
                .ToSql();
            expected =
                "DELETE [A0] FROM [dbo].[user] [A0] JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId] WHERE [A0].[UserId] IN(7)";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void DeleteOutputTest()
        {
            var tUserSinAlias = Tables.User(Alias.Empty);
            var tUser = Tables.User();

            Assert.AreEqual("DELETE [dbo].[user] OUTPUT DELETED.[UserId] FROM [dbo].[user]",
                Delete(tUserSinAlias).From(tUserSinAlias).All().Output(tUserSinAlias.UserId).ToSql());

            Assert.AreEqual("DELETE [A0] OUTPUT DELETED.[UserId] FROM [dbo].[user] [A0]",
                Delete(tUser).From(tUser).All().Output(tUser.UserId).ToSql());
        }

        [Test]
        public void DeleteOutputPgTest()
        {
            var tUserSinAlias = Tables.User(Alias.Empty);
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();

            Assert.AreEqual("DELETE FROM \"public\".\"user\" RETURNING \"UserId\"",
                Delete(tUserSinAlias).From(tUserSinAlias).All().Output(tUserSinAlias.UserId).ToPgSql());

            Assert.AreEqual("DELETE FROM \"public\".\"user\" \"A0\" RETURNING \"A0\".\"UserId\"",
                Delete(tUser).From(tUser).All().Output(tUser.UserId).ToPgSql());

            Assert.AreEqual("DELETE FROM \"public\".\"user\" \"A0\" USING \"public\".\"Customer\" \"A1\" WHERE \"A1\".\"UserId\"=\"A0\".\"UserId\" RETURNING \"A0\".\"UserId\"",
                Delete(tUser).From(tUser).InnerJoin(tCustomer, @on: tCustomer.UserId == tUser.UserId).All().Output(tUser.UserId).ToPgSql());
        }
    }
}