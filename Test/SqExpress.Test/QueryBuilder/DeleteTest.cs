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
            var tableA = Tables.User();

            var deleteAll = Delete(table).All();
            var deleteWhere = Delete(table).Where(table.UserId.In(1, 2));

            var deleteAllA = Delete(tableA).All();
            var deleteWhereA = Delete(tableA).Where(tableA.UserId.In(1, 2));

            Assert.AreEqual("DELETE [dbo].[user]", deleteAll.ToSql());
            Assert.AreEqual("DELETE FROM `user`", deleteAll.ToMySql());
            Assert.AreEqual("DELETE FROM \"public\".\"user\"", deleteAll.ToPgSql());

            Assert.AreEqual("DELETE [dbo].[user] WHERE [UserId] IN(1,2)", deleteWhere.ToSql());
            Assert.AreEqual("DELETE FROM `user` WHERE `UserId` IN(1,2)", deleteWhere.ToMySql());
            Assert.AreEqual("DELETE FROM \"public\".\"user\" WHERE \"UserId\" IN(1,2)", deleteWhere.ToPgSql());

            Assert.AreEqual("DELETE [A0] FROM [dbo].[user] [A0]", deleteAllA.ToSql());
            Assert.AreEqual("DELETE FROM `user`", deleteAllA.ToMySql());
            Assert.AreEqual("DELETE FROM \"public\".\"user\" \"A0\"", deleteAllA.ToPgSql());

            Assert.AreEqual("DELETE [A0] FROM [dbo].[user] [A0] WHERE [A0].[UserId] IN(1,2)", deleteWhereA.ToSql());
            Assert.AreEqual("DELETE FROM `user` WHERE `UserId` IN(1,2)", deleteWhereA.ToMySql());
            Assert.AreEqual("DELETE FROM \"public\".\"user\" \"A0\" WHERE \"A0\".\"UserId\" IN(1,2)", deleteWhereA.ToPgSql());
        }

        [Test]
        public void DeleteFromTest()
        {
            var tUserSinAlias = Tables.User(Alias.Empty);
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();

            var exprDeleteSinAlias = Delete(tUserSinAlias).From(tUserSinAlias).All();
            Assert.AreEqual("DELETE [dbo].[user] FROM [dbo].[user]", exprDeleteSinAlias.ToSql());
            Assert.AreEqual("DELETE FROM \"public\".\"user\"", exprDeleteSinAlias.ToPgSql());
            Assert.AreEqual("DELETE `user` FROM `user`", exprDeleteSinAlias.ToMySql());

            var exprDeleteConAlias = Delete(tUser).From(tUser).All();
            Assert.AreEqual("DELETE [A0] FROM [dbo].[user] [A0]", exprDeleteConAlias.ToSql());
            Assert.AreEqual("DELETE `user` FROM `user`", exprDeleteSinAlias.ToMySql());


            //Join

            var deleteInnerJoin = Delete(tUser)
                .From(tUser)
                .InnerJoin(tCustomer, @on: tCustomer.UserId == tUser.UserId)
                .All();

            var actual = deleteInnerJoin.ToSql();
            var expected = "DELETE [A0] FROM [dbo].[user] [A0] JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId]";
            Assert.AreEqual(expected, actual);

            actual = deleteInnerJoin.ToMySql();
            expected = "DELETE `A0` FROM `user` `A0` JOIN `Customer` `A1` ON `A1`.`UserId`=`A0`.`UserId`";
            Assert.AreEqual(expected, actual);

            actual = Delete(tUser).From(tUser).LeftJoin(tCustomer, on: tCustomer.UserId == tUser.UserId).All().ToSql();
            expected =
                "DELETE [A0] FROM [dbo].[user] [A0] LEFT JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId]";
            Assert.AreEqual(expected, actual);

            actual = Delete(tUser).From(tUser).FullJoin(tCustomer, on: tCustomer.UserId == tUser.UserId).All().ToSql();
            expected =
                "DELETE [A0] FROM [dbo].[user] [A0] FULL JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId]";
            Assert.AreEqual(expected, actual);

            actual = Delete(tUser).From(tUser).CrossJoin(tCustomer).All().ToSql();
            expected = "DELETE [A0] FROM [dbo].[user] [A0] CROSS JOIN [dbo].[Customer] [A1]";
            Assert.AreEqual(expected, actual);

            var tUser2 = Tables.User();
            actual = Delete(tUser)
                .From(tUser)
                .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                .InnerJoin(tUser2, on: tUser.UserId == tUser2.UserId)
                .All()
                .ToSql();

            expected =
                "DELETE [A0] FROM [dbo].[user] [A0] JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId] JOIN [dbo].[user] [A2] ON [A0].[UserId]=[A2].[UserId]";

            Assert.AreEqual(expected, actual);

            //Join Where
            actual = Delete(tUser)
                .From(tUser)
                .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
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
                Delete(tUser).From(tUser).InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId).All().Output(tUser.UserId).ToPgSql());
        }

        [Test]
        public void Delete_NullWhere()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();

            var expr = Delete(tUser)
                .From(tUser)
                .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                .Where(null);

            var actualTSql = expr.ToSql();
            var actualPgSql = expr.ToPgSql();

            Assert.AreEqual("DELETE [A0] FROM [dbo].[user] [A0] JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId]", actualTSql);
            Assert.AreEqual("DELETE FROM \"public\".\"user\" \"A0\" USING \"public\".\"Customer\" \"A1\" WHERE \"A1\".\"UserId\"=\"A0\".\"UserId\"", actualPgSql);
        }
    }
}