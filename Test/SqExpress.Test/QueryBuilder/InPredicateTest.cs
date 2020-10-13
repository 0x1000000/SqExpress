using System;
using NUnit.Framework;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.QueryBuilder
{
    [TestFixture]
    public class InPredicateTest
    {
        [Test]
        public void InSubQueryTest()
        {
            var tbl = Tables.User(Alias.Empty);

            var expr = Select(tbl.FirstName)
                .From(tbl)
                .Where(tbl.UserId >0 & !tbl.UserId.In(Select(Literal(1)).UnionAll(Select(Literal(2)))))
                .Done();

            var actual = expr.ToSql();

            Assert.AreEqual("SELECT [FirstName] FROM [dbo].[user] WHERE [UserId]>0 AND NOT [UserId] IN(SELECT 1 UNION ALL SELECT 2)", actual);
        }

        [Test]
        public void InValuesQueryTest()
        {
            var tbl = Tables.User(Alias.Empty);

            var expr = Select(tbl.FirstName)
                .From(tbl)
                .Where(tbl.UserId.In(Literal(1),Literal(2)))
                .Done();

            var actual = expr.ToSql();

            Assert.AreEqual("SELECT [FirstName] FROM [dbo].[user] WHERE [UserId] IN(1,2)", actual);
        }

        [Test]
        public void InValuesTest()
        {
            var a = CustomColumnFactory.Int32("a");

            Assert.AreEqual("[a] IN(1)", a.In(Literal(1)).ToSql());
            Assert.AreEqual("[a] IN(1,2,3)", a.In(Literal(1), Literal(2), Literal(3)).ToSql());
            Assert.AreEqual("[a] IN(1,2)", a.In(new []{ Literal(1), Literal(2) }).ToSql());

            Assert.AreEqual("[a] IN(1)", a.In(1).ToSql());
            Assert.AreEqual("[a] IN(1,2,3)", a.In(1,2,3).ToSql());
            Assert.AreEqual("[a] IN(1,2)", a.In(new []{1, 2}).ToSql());

            Assert.AreEqual("[a] IN('1')", a.In("1").ToSql());
            Assert.AreEqual("[a] IN('1','2','3')", a.In("1","2","3").ToSql());
            Assert.AreEqual("[a] IN('1','2')", a.In(new []{"1", "2"}).ToSql());

            var g1 = Guid.Parse("F46E2EC5-E08F-4CB9-8FD5-62DAC1A90C85");
            var g2 = Guid.Parse("FE716966-74D9-4449-83CE-16371698E8D0");
            var g3 = Guid.Parse("9614F808-E9EA-4BFE-8432-3711EB7E235C");

            Assert.AreEqual("[a] IN('f46e2ec5-e08f-4cb9-8fd5-62dac1a90c85')", a.In(g1).ToSql());
            Assert.AreEqual("[a] IN('f46e2ec5-e08f-4cb9-8fd5-62dac1a90c85','fe716966-74d9-4449-83ce-16371698e8d0','9614f808-e9ea-4bfe-8432-3711eb7e235c')", a.In(g1,g2,g3).ToSql());
            Assert.AreEqual("[a] IN('f46e2ec5-e08f-4cb9-8fd5-62dac1a90c85','fe716966-74d9-4449-83ce-16371698e8d0')", a.In(new []{g1, g2}).ToSql());
        }
    }
}