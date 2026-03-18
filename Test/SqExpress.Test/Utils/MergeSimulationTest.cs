using System;
using NUnit.Framework;
using SqExpress.Utils;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.Utils
{
    [TestFixture]
    public class MergeSimulationTest
    {
        [Test]
        public void Basic()
        {
            var tUser = Tables.User(Alias.Auto);

            var data = new[]
            {
                new { Id = 1, FirstName = "FN1", LastName = "LN1" },
                new { Id = 2, FirstName = "FN2", LastName = "LN2" }
            };

            var mergeExpr = MergeDataInto(tUser, data)
                .MapDataKeys(s => s
                    .Set(s.Target.UserId, s.Source.Id))
                .MapData(s => s
                    .Set(s.Target.FirstName, s.Source.FirstName)
                    .Set(s.Target.LastName, s.Source.LastName))
                .WhenMatchedThenUpdate()
                .AlsoSet(s=>s.Set(tUser.Version, tUser.Version+1))
                .WhenNotMatchedByTargetThenInsert((_, s)=> tUser.UserId.WithSource(s) != 2)
                .WhenNotMatchedBySourceThenUpdate(t=> t.Version != 0)
                    .Set(s => s.Set(tUser.Version, tUser.Version + 2))
                .Done();

            var actualSlq = mergeExpr.ToMySql();

            var expected = "CREATE TEMPORARY TABLE `tmpMergeDataSource`(`UserId` int NOT NULL,`FirstName` varchar(255) NOT NULL,`LastName` varchar(255) NOT NULL);INSERT INTO `tmpMergeDataSource`(`UserId`,`FirstName`,`LastName`) VALUES (1,'FN1','LN1'),(2,'FN2','LN2');UPDATE `user` `A0` JOIN `tmpMergeDataSource` `A1` ON `A0`.`UserId`=`A1`.`UserId` SET `A0`.`FirstName`=`A1`.`FirstName`,`A0`.`LastName`=`A1`.`LastName`,`A0`.`Version`=`A0`.`Version`+1;INSERT INTO `user`(`UserId`,`FirstName`,`LastName`) SELECT `A1`.`UserId`,`A1`.`FirstName`,`A1`.`LastName` FROM `tmpMergeDataSource` `A1` WHERE NOT EXISTS(SELECT 1 FROM `user` `A0` WHERE `A0`.`UserId`=`A1`.`UserId`) AND `A1`.`UserId`!=2;UPDATE `user` `A0` SET `A0`.`Version`=`A0`.`Version`+2 WHERE NOT EXISTS(SELECT 1 FROM `tmpMergeDataSource` `A1` WHERE `A0`.`UserId`=`A1`.`UserId`) AND `A0`.`Version`!=0;DROP TABLE `tmpMergeDataSource`;";

            Assert.AreEqual(expected, actualSlq);
        }

        [Test]
        public void DerivedQuerySource()
        {
            var target = Tables.User("T");
            var sourceTable = Tables.User("U");
            var source = Select(
                    sourceTable.UserId.As("UserId"),
                    sourceTable.FirstName.As("FirstName"),
                    Literal("N/A").As("LastName"),
                    Literal(5).As("Marker"))
                .From(sourceTable)
                .Where(sourceTable.UserId < 10)
                .Done()
                .As(TableAlias("S"));

            var mergeExpr = MergeInto(target, source)
                .On(target.UserId == source.Column("UserId"))
                .WhenMatched()
                    .ThenUpdate()
                        .Set(target.LastName, source.Column("LastName"))
                .WhenNotMatchedByTarget()
                    .ThenInsert()
                        .Set(target.UserId, source.Column("UserId"))
                        .Set(target.FirstName, source.Column("FirstName"))
                        .Set(target.LastName, source.Column("LastName"))
                .Done();

            var sql = mergeExpr.ToMySql();

            Assert.That(sql, Does.StartWith("CREATE TEMPORARY TABLE `tmpMergeDataSource`(`UserId` int"));
            Assert.That(sql, Does.Contain("`FirstName` varchar"));
            Assert.That(sql, Does.Contain("`LastName` varchar(255) NOT NULL"));
            Assert.That(sql, Does.Contain("`Marker` int"));
            Assert.That(sql, Does.Contain("INSERT INTO `tmpMergeDataSource`(`UserId`,`FirstName`,`LastName`,`Marker`) SELECT `U`.`UserId` `UserId`,`U`.`FirstName` `FirstName`,'N/A' `LastName`,5 `Marker` FROM `user` `U` WHERE `U`.`UserId`<10;"));
            Assert.That(sql, Does.Contain("JOIN `tmpMergeDataSource` `S` ON `T`.`UserId`=`S`.`UserId`"));
            Assert.That(sql, Does.Contain("INSERT INTO `user`(`UserId`,`FirstName`,`LastName`) SELECT `S`.`UserId`,`S`.`FirstName`,`S`.`LastName` FROM `tmpMergeDataSource` `S`"));
        }
    }
}
