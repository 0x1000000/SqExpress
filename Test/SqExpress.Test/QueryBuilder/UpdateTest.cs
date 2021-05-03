using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

        [Test]
        public void UpdateDataIn_Test()
        {

            const int usersCount = 3;

            var data = new List<UserData>(usersCount);

            for (int i = 0; i < usersCount; i++)
            {
                data.Add(new UserData
                {
                    UserId = i+1,
                    FirstName = "First" + i,
                    LastName = "Last" + i,
                    EMail = $"user{i}@company.com",
                    RegDate = new DateTime(2020, 01, 02)
                });
            }

            var tbl = Tables.User();

            var expr = UpdateData(tbl, data)
                .MapDataKeys(s => s.Set(s.Target.UserId, s.Source.UserId))
                .MapData(s => s.Set(s.Target.FirstName, s.Source.FirstName))
                .AlsoSet(s => s.Set(s.Target.Modified, GetUtcDate()))
                .Done();

            var sql = expr.ToMySql();

            Regex r = new Regex("t[\\d|\\w]{32}");

            sql = r.Replace(sql, "tmpTableName");

            Assert.AreEqual(sql, "CREATE TEMPORARY TABLE `tmpTableName`(`UserId` int,`FirstName` varchar(6) character set utf8,CONSTRAINT PRIMARY KEY (`UserId`));INSERT INTO `tmpTableName`(`UserId`,`FirstName`) VALUES (1,'First0'),(2,'First1'),(3,'First2');UPDATE `user` `A0`,`tmpTableName` `A1` SET `A0`.`FirstName`=`A1`.`FirstName`,`A0`.`Modified`=UTC_TIMESTAMP() WHERE `A0`.`UserId`=`A1`.`UserId`;DROP TABLE `tmpTableName`;");

            sql = expr.ToSql();

            Assert.AreEqual(sql, "UPDATE [A0] SET [A0].[FirstName]=[A1].[FirstName],[A0].[Modified]=GETUTCDATE() FROM [dbo].[user] [A0] JOIN (VALUES (1,'First0'),(2,'First1'),(3,'First2'))[A1]([UserId],[FirstName]) ON [A0].[UserId]=[A1].[UserId]");
        }
    }
}