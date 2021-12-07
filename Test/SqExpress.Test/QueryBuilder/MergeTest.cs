using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SqExpress.Test.QueryBuilder
{
    [TestFixture]
    public class MergeTest
    {
        [Test]
        public void FullTest()
        {
            const int usersCount = 3;

            var data = new List<UserData>(usersCount);

            for (int i = 0; i < usersCount; i++)
            {
                data.Add(new UserData
                {
                    UserId = i%2 == 0 ? 0 : i,
                    FirstName = "First" + i,
                    LastName = "Last" + i,
                    EMail = $"user{i}@company.com",
                    RegDate = new DateTime(2020,01,02)
                });
            }

            DateTime utcNow = new DateTime(2020, 10, 03, 10, 17, 12, 131);

            var recordIndex = CustomColumnFactory.Int32("Index");
            var inserted = CustomColumnFactory.Int32("InsertedUserId");
            var deleted = CustomColumnFactory.Int32("DeletedUserId");
            var action = CustomColumnFactory.String("Action");

            var mergeOutput = SqQueryBuilder
                .MergeDataInto(Tables.User(), data)
                .MapDataKeys(s => s.Set(s.Target.UserId, s.Source.UserId))
                .MapData(s => s
                    .Set(s.Target.FirstName, s.Source.FirstName)
                    .Set(s.Target.LastName, s.Source.LastName)
                    .Set(s.Target.Email, s.Source.EMail)
                    .Set(s.Target.RegDate, s.Source.RegDate))
                .MapExtraData(s => s.Set(recordIndex, s.Index))
                .AndOn((t, s) => t.UserId.WithSource(s) != 0)
                .WhenMatchedThenUpdate()
                .AlsoSet(s =>
                    s.Set(s.Target.Version, s.Target.Version+1)
                    .Set(s.Target.Modified, utcNow))
                .WhenNotMatchedByTargetThenInsert()
                .ExcludeKeys()
                .Exclude(t => new[] { t.Email.ColumnName, t.LastName.ColumnName })
                .AlsoInsert(s => s
                    .Set(s.Target.LastName, "Fake")
                    .Set(s.Target.Created, utcNow)
                    .Set(s.Target.Modified, utcNow)
                    .Set(s.Target.Version, 1))
                .WhenNotMatchedBySourceThenDelete()
                .Output((t, s, m) => m
                    .Inserted(t.UserId.As(inserted))
                    .Inserted(t.UserId.As(deleted))
                    .Column(recordIndex.WithSource(s))
                    .Action(action))
                .Done();

            var actual = mergeOutput?.ToSql();

            var expected = "MERGE [dbo].[user] [A0] USING (" +
                           "VALUES (0,'First0','Last0','user0@company.com','2020-01-02',0)," +
                           "(1,'First1','Last1','user1@company.com','2020-01-02',1)," +
                           "(0,'First2','Last2','user2@company.com','2020-01-02',2)" +
                           ")[A1]([UserId],[FirstName],[LastName],[Email],[RegDate],[Index]) " +
                           "ON [A0].[UserId]=[A1].[UserId] AND [A1].[UserId]!=0 " +
                           "WHEN MATCHED THEN UPDATE SET " +
                           "[A0].[FirstName]=[A1].[FirstName]," +
                           "[A0].[LastName]=[A1].[LastName]," +
                           "[A0].[Email]=[A1].[Email]," +
                           "[A0].[RegDate]=[A1].[RegDate]," +
                           "[A0].[Version]=[A0].[Version]+1," +
                           "[A0].[Modified]='2020-10-03T10:17:12.131' " +
                           "WHEN NOT MATCHED THEN INSERT" +
                           "([FirstName],[RegDate],[LastName],[Created],[Modified],[Version]) " +
                           "VALUES([A1].[FirstName],[A1].[RegDate],'Fake','2020-10-03T10:17:12.131','2020-10-03T10:17:12.131',1) " +
                           "WHEN NOT MATCHED BY SOURCE THEN  DELETE " +
                           "OUTPUT INSERTED.[UserId] [InsertedUserId],INSERTED.[UserId] [DeletedUserId],[A1].[Index],$ACTION [Action];";

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestMatched_ThenDelete()
        {
            var data = new[]
            {
                new UserData
                {
                    UserId = 1,
                    FirstName = "First",
                    LastName = "Last",
                    EMail = "user1@company.com",
                    RegDate = new DateTime(2020, 01, 02)
                }
            };

            var merge = SqQueryBuilder.MergeDataInto(Tables.User("T"), data)
                .MapDataKeys(s => s.Set(s.Target.UserId, s.Source.UserId))
                .MapData(s => s.Set(s.Target.FirstName, s.Source.FirstName).Set(s.Target.LastName, s.Source.LastName))
                .WhenMatchedThenDelete()
                .Done();

            var sql = merge?.ToSql();
            var expected = "MERGE [dbo].[user] [T] " +
                           "USING (VALUES (1,'First','Last'))[A0]([UserId],[FirstName],[LastName]) " +
                           "ON [T].[UserId]=[A0].[UserId] WHEN MATCHED THEN  DELETE;";

            Assert.AreEqual(expected, sql);
        }

        [Test]
        public void TestNotMatched_ThenInsertDefaults()
        {
            var data = new[]
            {
                new UserData
                {
                    UserId = 1,
                    FirstName = "First",
                    LastName = "Last",
                    EMail = "user1@company.com",
                    RegDate = new DateTime(2020, 01, 02)
                }
            };

            var merge = SqQueryBuilder.MergeDataInto(Tables.User("T"), data)
                .MapDataKeys(s => s.Set(s.Target.UserId, s.Source.UserId))
                .MapData(s => s.Set(s.Target.FirstName, s.Source.FirstName).Set(s.Target.LastName, s.Source.LastName))
                .WhenNotMatchedByTargetThenInsertDefaults((t,s)=>t.UserId.WithSource(s) != 7)
                .Done();

            var sql = merge?.ToSql();
            var expected = "MERGE [dbo].[user] [T] " +
                           "USING (VALUES (1,'First','Last'))[A0]([UserId],[FirstName],[LastName]) " +
                           "ON [T].[UserId]=[A0].[UserId] WHEN NOT MATCHED AND [A0].[UserId]!=7 THEN INSERT DEFAULT VALUES;";

            Assert.AreEqual(expected, sql);
        }

        [Test]
        public void TestNotMatched_ThenExclude()
        {
            var data = new[]
            {
                new UserData
                {
                    UserId = 1,
                    FirstName = "First",
                    LastName = "Last",
                    EMail = "user1@company.com",
                    RegDate = new DateTime(2020, 01, 02)
                }
            };

            var merge = SqQueryBuilder.MergeDataInto(Tables.User("T"), data)
                .MapDataKeys(s => s.Set(s.Target.UserId, s.Source.UserId))
                .MapData(s => s.Set(s.Target.FirstName, s.Source.FirstName).Set(s.Target.LastName, s.Source.LastName))
                .WhenNotMatchedByTargetThenInsert((t,s)=>t.UserId.WithSource(s) != 8)
                .Exclude(t=>t.LastName)
                .Done();

            var sql = merge?.ToSql();
            var expected = "MERGE [dbo].[user] [T] " +
                           "USING (VALUES (1,'First','Last'))[A0]([UserId],[FirstName],[LastName]) " +
                           "ON [T].[UserId]=[A0].[UserId] WHEN NOT MATCHED AND [A0].[UserId]!=8 THEN INSERT([UserId],[FirstName]) VALUES([A0].[UserId],[A0].[FirstName]);";

            Assert.AreEqual(expected, sql);
        }

        [Test]
        public void TestNotMatchedBySourceMatched_ThenUpdate()
        {
            var data = new[]
            {
                new UserData
                {
                    UserId = 1,
                    FirstName = "First",
                    LastName = "Last",
                    EMail = "user1@company.com",
                    RegDate = new DateTime(2020, 01, 02)
                }
            };

            var merge = SqQueryBuilder.MergeDataInto(Tables.User("T"), data)
                .MapDataKeys(s => s.Set(s.Target.UserId, s.Source.UserId))
                .MapData(s => s.Set(s.Target.FirstName, s.Source.FirstName).Set(s.Target.LastName, s.Source.LastName))
                .WhenNotMatchedBySourceThenUpdate(t=>t.UserId == 7)
                .Set(s=>s.Set(s.Target.Version, -1))
                .Done();

            var sql = merge?.ToSql();
            var expected = "MERGE [dbo].[user] [T] " +
                           "USING (VALUES (1,'First','Last'))[A0]([UserId],[FirstName],[LastName]) " +
                           "ON [T].[UserId]=[A0].[UserId] WHEN NOT MATCHED BY SOURCE AND [T].[UserId]=7 THEN UPDATE SET [T].[Version]=-1;";

            Assert.AreEqual(expected, sql);
        }         
        
        [Test]
        public void TestUpdateDefault()
        {
            var data = new[]
            {
                new UserData
                {
                    UserId = 1,
                    FirstName = "First",
                    LastName = "Last",
                    EMail = "user1@company.com",
                    RegDate = new DateTime(2020, 01, 02)
                }
            };

            var merge = SqQueryBuilder.MergeDataInto(Tables.User("T"), data)
                .MapDataKeys(s => s.Set(s.Target.UserId, s.Source.UserId))
                .MapData(s => s
                    .Set(s.Target.FirstName, s.Source.FirstName)
                    .Set(s.Target.LastName, s.Source.LastName))
                .WhenMatchedThenUpdate()
                .AlsoSet(s => s
                    .Set(s.Target.Version, -1)
                    .SetDefault(s.Target.RegDate))
                .WhenNotMatchedByTargetThenInsert()
                .ExcludeKeys()
                .AlsoInsert(s=>s.SetDefault(s.Target.RegDate))
                .Done();

            var sql = merge.ToSql();
            var expected = "MERGE [dbo].[user] [T] USING (VALUES (1,'First','Last'))[A0]([UserId],[FirstName],[LastName]) " +
                           "ON [T].[UserId]=[A0].[UserId] " +
                           "WHEN MATCHED THEN UPDATE SET [T].[FirstName]=[A0].[FirstName],[T].[LastName]=[A0].[LastName],[T].[Version]=-1,[T].[RegDate]=DEFAULT " +
                           "WHEN NOT MATCHED THEN INSERT([FirstName],[LastName],[RegDate]) VALUES([A0].[FirstName],[A0].[LastName],DEFAULT);";

            Assert.AreEqual(expected, sql);
        }        
        
        [Test]
        public void EmptyData_Null()
        {
            var data = new UserData[0];

            Assert.Throws<SqExpressException>(() =>
            {
                SqQueryBuilder.MergeDataInto(Tables.User(), data)
                    .MapDataKeys(s => s.Set(s.Target.FirstName, s.Source.FirstName))
                    .MapData(s =>
                        s.Set(s.Target.FirstName, s.Source.FirstName).Set(s.Target.LastName, s.Source.LastName))
                    .WhenMatchedThenDelete()
                    .Done();

            });
        }
        
        [Test]
        public void KeysOnly_NoWhenMatch()
        {
            var data = new []{new { Id1 = 1, Id2 = 2 }, new { Id1 = 3, Id2 = 4 } };

            var merge = SqQueryBuilder.MergeDataInto(Tables.CustomerOrder(), data)
                .MapDataKeys(s => s.Set(s.Target.CustomerId, s.Source.Id1).Set(s.Target.OrderId, s.Source.Id2))
                .WhenNotMatchedByTargetThenInsert()
                .WhenNotMatchedBySourceThenDelete()
                .Done();

            var sql = merge.ToSql();
            const string expected = "MERGE [dbo].[CustomerOrder] [A0] USING (VALUES (1,2),(3,4))[A1]([CustomerId],[OrderId]) " +
                                    "ON [A0].[CustomerId]=[A1].[CustomerId] AND [A0].[OrderId]=[A1].[OrderId] " +
                                    "WHEN NOT MATCHED THEN INSERT([CustomerId],[OrderId]) VALUES([A1].[CustomerId],[A1].[OrderId]) " +
                                    "WHEN NOT MATCHED BY SOURCE THEN  DELETE;";

            Assert.AreEqual(expected, sql);
        }

        [Test]
        public void KeysOnly_WhenMatchedThenDelete()
        {
            var data = new []{new { Id1 = 1, Id2 = 2 }, new { Id1 = 3, Id2 = 4 } };

            var merge = SqQueryBuilder.MergeDataInto(Tables.CustomerOrder(), data)
                .MapDataKeys(s => s.Set(s.Target.CustomerId, s.Source.Id1).Set(s.Target.OrderId, s.Source.Id2))
                .WhenMatchedThenDelete()
                .WhenNotMatchedByTargetThenInsert()
                .WhenNotMatchedBySourceThenDelete()
                .Done();

            var sql = merge.ToSql();
            const string expected = "MERGE [dbo].[CustomerOrder] [A0] USING (VALUES (1,2),(3,4))[A1]([CustomerId],[OrderId]) " +
                                    "ON [A0].[CustomerId]=[A1].[CustomerId] AND [A0].[OrderId]=[A1].[OrderId] " +
                                    "WHEN MATCHED THEN  DELETE " +
                                    "WHEN NOT MATCHED THEN INSERT([CustomerId],[OrderId]) VALUES([A1].[CustomerId],[A1].[OrderId]) " +
                                    "WHEN NOT MATCHED BY SOURCE THEN  DELETE;";

            Assert.AreEqual(expected, sql);
        }

        [Test]
        public void KeysOnly_WhenMatchedThenUpdate_AlsoSet()
        {
            var data = new []{new { Id1 = 1, Id2 = 2 }, new { Id1 = 3, Id2 = 4 } };

            var merge = SqQueryBuilder.MergeDataInto(Tables.CustomerOrder(), data)
                .MapDataKeys(s => s.Set(s.Target.CustomerId, s.Source.Id1).Set(s.Target.OrderId, s.Source.Id2))
                .WhenMatchedThenUpdate().AlsoSet(s=>s.Set(s.Target.CustomerId, 0))
                .WhenNotMatchedByTargetThenInsert()
                .WhenNotMatchedBySourceThenDelete()
                .Done();

            var sql = merge.ToSql();
            const string expected = "MERGE [dbo].[CustomerOrder] [A0] USING (VALUES (1,2),(3,4))[A1]([CustomerId],[OrderId]) " +
                                    "ON [A0].[CustomerId]=[A1].[CustomerId] AND [A0].[OrderId]=[A1].[OrderId] " +
                                    "WHEN MATCHED THEN UPDATE SET [A0].[CustomerId]=0 " +
                                    "WHEN NOT MATCHED THEN INSERT([CustomerId],[OrderId]) VALUES([A1].[CustomerId],[A1].[OrderId]) " +
                                    "WHEN NOT MATCHED BY SOURCE THEN  DELETE;";

            Assert.AreEqual(expected, sql);
        }

        [Test]
        public void KeysOnly_WhenMatchedThenUpdate_Error()
        {
            var data = new []{new { Id1 = 1, Id2 = 2 }, new { Id1 = 3, Id2 = 4 } };
            Assert.Throws<SqExpressException>(() =>
                SqQueryBuilder.MergeDataInto(Tables.CustomerOrder(), data)
                    .MapDataKeys(s => s.Set(s.Target.CustomerId, s.Source.Id1).Set(s.Target.OrderId, s.Source.Id2))
                    .WhenMatchedThenUpdate()
                    .WhenNotMatchedByTargetThenInsert()
                    .WhenNotMatchedBySourceThenDelete()
                    .Done());
        }
    }
}