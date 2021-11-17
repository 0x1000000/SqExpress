using System;
using System.Collections.Generic;
using NUnit.Framework;
using SqExpress.SyntaxTreeOperations;
using SqExpress.Test.Syntax;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.QueryBuilder
{
    [TestFixture]
    public class InsertTest
    {
        [Test]
        public void InsertDataTest()
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

            var expr = InsertDataInto(tbl, data)
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
        public void InsertDataWithExtraTest()
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

            var expr = InsertDataInto(tbl, data)
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

            //Serialization
            var list = expr.SyntaxTree().ExportToPlainList(PlainItem.Create);
            var after = ExprDeserializer.DeserializeFormPlainList(list);

            Assert.AreEqual(expected, after.ToSql());

            //My SQL
            expected = "INSERT INTO `user`(`FirstName`,`LastName`,`Email`,`RegDate`,`Version`,`Created`) SELECT `A0`.*,5,'2020-01-02' FROM (VALUES ('First0','Last0','user0@company.com','2020-01-02'),('First1','Last1','user1@company.com','2020-01-02'),('First2','Last2','user2@company.com','2020-01-02'))`A0`  RETURNING `UserId`";
            Assert.AreEqual(expected,after.ToMySql());

        }

        [Test]
        public void IdentityInsertDataWithExtraTest()
        {
            const int usersCount = 1;

            var data = new List<UserData>(usersCount);

            for (int i = 0; i < usersCount; i++)
            {
                data.Add(new UserData
                {
                    UserId = i,
                    FirstName = "First" + i,
                });
            }

            var tbl = Tables.User();

            var expr = InsertDataInto(tbl, data)
                .MapData(i => i
                    .Set(i.Target.UserId, i.Source.UserId)
                    .Set(i.Target.FirstName, i.Source.FirstName))
                .IdentityInsert()
                .Done();

            string expected = "INSERT INTO \"public\".\"user\"(\"UserId\",\"FirstName\") OVERRIDING SYSTEM VALUE VALUES (0,'First0');select setval(pg_get_serial_sequence('\"public\".\"user\"','UserId'),(SELECT MAX(\"UserId\") FROM \"public\".\"user\"));";
            Assert.AreEqual(expected, expr.ToPgSql());
        }

        [Test]
        public void InsertExprTest()
        {
            var tbl = Tables.User();

            var actual = InsertInto(tbl, tbl.FirstName, tbl.LastName, tbl.Modified, tbl.Version)
                .From(Select(tbl.FirstName, tbl.LastName, GetUtcDate(), Literal(1)+1).From(tbl))
                .ToSql();
            var expected = "INSERT INTO [dbo].[user]([FirstName],[LastName],[Modified],[Version]) " +
                           "SELECT [A0].[FirstName],[A0].[LastName],GETUTCDATE(),1+1 FROM [dbo].[user] [A0]";

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Test()
        {
            const int usersCount = 1;

            var data = new List<UserData>(usersCount);

            for (int i = 0; i < usersCount; i++)
            {
                data.Add(new UserData
                {
                    FirstName = "First" + i,
                    LastName = "Last" + i,
                });
            }

            var tbl = Tables.User();

            var actualTSql = InsertDataInto(tbl, data)
                .MapData(s => s.Set(s.Target.FirstName, s.Source.FirstName).Set(s.Target.LastName, s.Source.LastName))
                .CheckExistenceBy(tbl.FirstName, tbl.LastName)
                .Output(tbl.UserId)
                .Done()
                .ToSql();

            Assert.AreEqual("INSERT INTO [dbo].[user]([FirstName],[LastName]) OUTPUT INSERTED.[UserId] SELECT [FirstName],[LastName] FROM (VALUES ('First0','Last0'))[A0]([FirstName],[LastName]) WHERE NOT EXISTS(SELECT 1 FROM [dbo].[user] [A1] WHERE [A1].[FirstName]=[A0].[FirstName] AND [A1].[LastName]=[A0].[LastName])", actualTSql);

            var actualMySql = InsertDataInto(tbl, data)
                .MapData(s => s.Set(s.Target.UserId, s.Source.UserId).Set(s.Target.FirstName, s.Source.FirstName).Set(s.Target.LastName, s.Source.LastName))
                .CheckExistenceBy(tbl.FirstName, tbl.LastName)
                .Done()
                .ToMySql();

            Assert.AreEqual("INSERT INTO `user`(`UserId`,`FirstName`,`LastName`) WITH CTE_Derived_Table_0(`UserId`,`FirstName`,`LastName`) AS(VALUES (0,'First0','Last0')) SELECT `UserId`,`FirstName`,`LastName` FROM `CTE_Derived_Table_0` `A0` WHERE NOT EXISTS(SELECT 1 FROM `user` `A1` WHERE `A1`.`FirstName`=`A0`.`FirstName` AND `A1`.`LastName`=`A0`.`LastName`)", actualMySql);

            var actualMyOutputSql = InsertDataInto(tbl, data)
                .MapData(s => s.Set(s.Target.FirstName, s.Source.FirstName).Set(s.Target.LastName, s.Source.LastName))
                .CheckExistenceBy(tbl.FirstName, tbl.LastName)
                .Output(tbl.UserId)
                .Done()
                .ToMySql();

            Assert.AreEqual("INSERT INTO `user`(`FirstName`,`LastName`) WITH CTE_Derived_Table_0(`FirstName`,`LastName`) AS(VALUES ('First0','Last0')) SELECT `FirstName`,`LastName` FROM `CTE_Derived_Table_0` `A0` WHERE NOT EXISTS(SELECT 1 FROM `user` `A1` WHERE `A1`.`FirstName`=`A0`.`FirstName` AND `A1`.`LastName`=`A0`.`LastName`)  RETURNING `UserId`", actualMyOutputSql);
        }
    }
}