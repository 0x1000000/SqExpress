using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SqExpress.Test.QueryBuilder
{
    [TestFixture]
    public class InsertTest
    {
        [Test]
        public void Test()
        {
            const int usersCount = 3;

            var data = new List<UserData>(usersCount);

            for (int i = 0; i < usersCount; i++)
            {
                data.Add(new UserData
                {
                    UserId = i % 2 == 0 ? 0 : i,
                    FirstName = "First" + i,
                    LastName = "Last" + i,
                    EMail = $"user{i}@company.com",
                    RegDate = new DateTime(2020, 01, 02)
                });
            }

            var tbl = Tables.User();

            var expr = SqQueryBuilder
                .InsertDataInto(tbl, data)
                .MapData(i => i
                    .Set(i.Target.FirstName, i.Source.FirstName)
                    .Set(i.Target.LastName, i.Source.LastName)
                    .Set(i.Target.Email, i.Source.EMail)
                    .Set(i.Target.RegDate, i.Source.RegDate))
                .Done();

            var actual = expr.ToSql();

            var expected = "INSERT INTO [dbo].[user]([FirstName],[LastName],[Email],[RegDate]) VALUES ('First0','Last0','user0@company.com','2020-01-02'),('First1','Last1','user1@company.com','2020-01-02'),('First2','Last2','user2@company.com','2020-01-02')";
            Assert.AreEqual(actual, expected);
        }

        [Test]
        public void TestExtra()
        {
            const int usersCount = 3;

            var data = new List<UserData>(usersCount);

            for (int i = 0; i < usersCount; i++)
            {
                data.Add(new UserData
                {
                    UserId = i % 2 == 0 ? 0 : i,
                    FirstName = "First" + i,
                    LastName = "Last" + i,
                    EMail = $"user{i}@company.com",
                    RegDate = new DateTime(2020, 01, 02)
                });
            }

            var tbl = Tables.User();

            var expr = SqQueryBuilder
                .InsertDataInto(tbl, data)
                .MapData(i => i
                    .Set(i.Target.FirstName, i.Source.FirstName)
                    .Set(i.Target.LastName, i.Source.LastName)
                    .Set(i.Target.Email, i.Source.EMail)
                    .Set(i.Target.RegDate, i.Source.RegDate))
                .AlsoInsert(s=>s.Set(s.Target.Version, 5).Set(s.Target.Created, new DateTime(2020,01,02)))
                .Output(tbl.UserId)
                .Done();

            var actual = expr.ToSql();

            var expected = "INSERT INTO [dbo].[user]([FirstName],[LastName],[Email],[RegDate],[Version],[Created]) OUTPUT INSERTED.[UserId] SELECT [FirstName],[LastName],[Email],[RegDate],5,'2020-01-02' FROM (VALUES ('First0','Last0','user0@company.com','2020-01-02'),('First1','Last1','user1@company.com','2020-01-02'),('First2','Last2','user2@company.com','2020-01-02'))[A0]([FirstName],[LastName],[Email],[RegDate])";
            Assert.AreEqual(actual, expected);
        }
    }
}