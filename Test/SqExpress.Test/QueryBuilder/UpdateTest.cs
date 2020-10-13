using System;
using NUnit.Framework;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.QueryBuilder
{
    [TestFixture]
    public class UpdateTest
    {
        [Test]
        public void UpdateTableTest()
        {
            var tUserSinAlias = Tables.User(Alias.Empty);

            var tUser = Tables.User();

            Assert.AreEqual("UPDATE [dbo].[user] SET [FirstName]='First',[LastName]='Last'"
                , Update(tUserSinAlias).Set(tUserSinAlias.FirstName, "First").Set(tUserSinAlias.LastName, "Last").All().ToSql());

            Assert.AreEqual("UPDATE [dbo].[user] SET [FirstName]='First',[LastName]='Last' WHERE [UserId] IN(1)"
                , Update(tUserSinAlias).Set(tUserSinAlias.FirstName, "First").Set(tUserSinAlias.LastName, "Last").Where(tUserSinAlias.UserId.In(1)).ToSql());

            Assert.AreEqual("UPDATE [A0] SET [A0].[FirstName]='First',[A0].[LastName]='Last' FROM [dbo].[user] [A0] WHERE [UserId] IN(1)"
                , Update(tUser).Set(tUser.FirstName, "First").Set(tUser.LastName, "Last").Where(tUserSinAlias.UserId.In(1)).ToSql());
        }

        [Test]
        public void UpdateFromTest()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();

            var actual = Update(tUser)
                .Set(tUser.FirstName, "First")
                .Set(tUser.LastName, "Last")
                .From(tUser)
                .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                .CrossJoin(tUser)
                .Where(tCustomer.CustomerId.In(1))
                .ToSql();

            var expected = "UPDATE [A0] SET [A0].[FirstName]='First',[A0].[LastName]='Last' " +
                           "FROM [dbo].[user] [A0] " +
                           "JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId] " +
                           "CROSS JOIN [dbo].[user] [A0] " +
                           "WHERE [A1].[CustomerId] IN(1)";

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void UpdateFromTestPg()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();
            var tCustomerOrder = Tables.CustomerOrder();

            var actual = Update(tUser)
                .Set(tUser.FirstName, "First")
                .Set(tUser.LastName, tUser.LastName + "(i)")
                .Set(tUser.RegDate, Default)
                .From(tUser)
                .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                .CrossJoin(tCustomerOrder)
                .Where(tCustomer.CustomerId.In(1))
                .ToPgSql();

            var expected = "UPDATE \"public\".\"user\" \"A0\" SET \"FirstName\"='First',\"LastName\"=\"A0\".\"LastName\"||'(i)',\"RegDate\"=DEFAULT FROM \"public\".\"Customer\" \"A1\",\"public\".\"CustomerOrder\" \"A2\" WHERE \"A1\".\"UserId\"=\"A0\".\"UserId\" AND \"A1\".\"CustomerId\" IN(1)";

            Assert.AreEqual(expected, actual);
        }
    }
}