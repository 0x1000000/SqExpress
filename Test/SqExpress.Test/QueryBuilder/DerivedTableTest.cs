using System;
using System.Collections.Generic;
using NUnit.Framework;
using SqExpress.Syntax.Value;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.QueryBuilder
{
    [TestFixture]
    public class DerivedTableTest
    {
        [Test]
        public void Test()
        {
            var user = Tables.User();
            var subQuery = new SubQuery(user);

            var actual = Select(user.UserId, subQuery.Count)
                .From(user)
                .InnerJoin(subQuery, @on: subQuery.UserId == user.UserId).Done()
                .ToSql();

            var expected = "SELECT [A0].[UserId],[A1].[Count] " +
                           "FROM [dbo].[user] [A0] " +
                           "JOIN (SELECT [A2].[UserId],17 FROM [dbo].[user] [A2])" +
                           "[A1]([UserId],[Count]) " +
                           "ON [A1].[UserId]=[A0].[UserId]";

            Assert.AreEqual(expected, actual);
        }        
        
        [Test]
        public void Test_AllNamed()
        {
            var user = Tables.User();
            var subQuery = new SubQueryAllNamed(user);

            var actual = Select(user.UserId, subQuery.Count)
                .From(user)
                .InnerJoin(subQuery, @on: subQuery.UserId == user.UserId).Done()
                .ToSql();

            var expected = "SELECT [A0].[UserId],[A1].[Count] " +
                           "FROM [dbo].[user] [A0] " +
                           "JOIN (SELECT [A2].[UserId],17 [Count] FROM [dbo].[user] [A2])" +
                           "[A1] " +
                           "ON [A1].[UserId]=[A0].[UserId]";

            Assert.AreEqual(expected, actual);
        }
        
        [Test]
        public void Test_ReNamed()
        {
            var user = Tables.User();
            var subQuery = new SubQueryReNamed();

            var actual = Select(user.UserId, subQuery.Count)
                .From(user)
                .InnerJoin(subQuery, @on: subQuery.UserId == user.UserId).Done()
                .ToSql();

            var expected = "SELECT [A0].[UserId],[A1].[Count] " +
                           "FROM [dbo].[user] [A0] " +
                           "JOIN (SELECT [A2].[UserId],17 [Count] FROM [dbo].[user] [A2])" +
                           "[A1]([OtherUserId],[Count]) " +
                           "ON [A1].[OtherUserId]=[A0].[UserId]";

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Test_ErrorNotMatched()
        {
            Assert.Throws<SqExpressException>(() =>
                {
                    var user = Tables.User();
                    var subQuery = new SubQueryError();

                    Select(user.UserId)
                        .From(user)
                        .InnerJoin(subQuery, @on: subQuery.UserId == user.UserId)
                        .Done()
                        .ToSql();
                },
                "Number of declared columns does not match to number of selected columns in the derived table sub query");
        }


        [Test]
        public void DerivedValues_NullColl()
        {
            var values = Values(new[]
            {
                Row(1, (string?)null, (DateTime?) null, (int?)null),
                Row(2, (string?)null, (DateTime?) null, (int?)0),
                Row(3, (string?)null, (DateTime?) null, (int?)null)
            });

            Console.WriteLine(values.ToSql());

            static IReadOnlyList<ExprValue> Row(params ExprValue[] values) => values;
        }

        [Test]
        public void EmptyModifyTest()
        {
            var original = new SubQuery(new User());

            var modified = original.SyntaxTree().Modify(e => e);

            Assert.AreSame(original, modified);
        }

        private class SubQuery : DerivedTableBase
        {
            public Int32CustomColumn UserId { get; }

            public Int32CustomColumn Count { get; }

            public SubQuery(User userTable)
            {
                this.UserId = userTable.UserId.AddToDerivedTable(this);
                this.Count = this.CreateInt32Column("Count");
            }

            protected override IExprSubQuery CreateQuery()
            {
                var user = Tables.User();

                return Select(user.UserId, Literal(17))
                    .From(user)
                    .Done();
            }
        }

        private class SubQueryAllNamed : DerivedTableBase
        {
            public Int32CustomColumn UserId { get; }

            public Int32CustomColumn Count { get; }

            public SubQueryAllNamed(User userTable)
            {
                this.UserId = userTable.UserId.AddToDerivedTable(this);
                this.Count = this.CreateInt32Column("Count");
            }

            protected override IExprSubQuery CreateQuery()
            {
                var user = Tables.User();

                return Select(user.UserId, Literal(17).As(this.Count))
                    .From(user)
                    .Done();
            }
        }

        private class SubQueryReNamed : DerivedTableBase
        {
            public Int32CustomColumn UserId { get; }

            public Int32CustomColumn Count { get; }

            public SubQueryReNamed()
            {
                this.UserId = this.CreateInt32Column("OtherUserId");
                this.Count = this.CreateInt32Column("Count");
            }

            protected override IExprSubQuery CreateQuery()
            {
                var user = Tables.User();

                return Select(user.UserId, Literal(17).As(this.Count))
                    .From(user)
                    .Done();
            }
        }

        private class SubQueryError : DerivedTableBase
        {
            public Int32CustomColumn UserId { get; }

            public SubQueryError()
            {
                this.UserId = this.CreateInt32Column("OtherUserId");
            }

            protected override IExprSubQuery CreateQuery()
            {
                var user = Tables.User();

                return Select(user.UserId, Literal(17))
                    .From(user)
                    .Done();
            }
        }
    }
}