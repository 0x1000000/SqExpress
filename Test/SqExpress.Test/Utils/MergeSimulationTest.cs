﻿using System;
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

            Assert.AreEqual("CREATE TEMPORARY TABLE `tmpMergeDataSource`(`UserId` int,`FirstName` varchar(3) character set utf8,`LastName` varchar(3) character set utf8,CONSTRAINT PRIMARY KEY (`UserId`));INSERT INTO `tmpMergeDataSource`(`UserId`,`FirstName`,`LastName`) VALUES (1,'FN1','LN1'),(2,'FN2','LN2');UPDATE `user` `A0`,`tmpMergeDataSource` `A1` SET `A0`.`FirstName`=`A1`.`FirstName`,`A0`.`LastName`=`A1`.`LastName`,`A0`.`Version`=`A0`.`Version`+1 WHERE `A0`.`UserId`=`A1`.`UserId`;INSERT INTO `user`(`UserId`,`FirstName`,`LastName`) SELECT `A1`.`UserId`,`A1`.`FirstName`,`A1`.`LastName` FROM `tmpMergeDataSource` `A1` WHERE NOT EXISTS(SELECT 1 FROM `user` `A0` WHERE `A0`.`UserId`=`A1`.`UserId`) AND `A1`.`UserId`!=2;UPDATE `user` `A0` SET `A0`.`Version`=`A0`.`Version`+2 WHERE NOT EXISTS(SELECT 1 FROM `tmpMergeDataSource` `A1` WHERE `A0`.`UserId`=`A1`.`UserId`) AND `A0`.`Version`!=0;DROP TABLE `tmpMergeDataSource`;", actualSlq);
        }
    }
}