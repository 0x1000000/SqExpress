using System;
using NUnit.Framework;
using SqExpress.Syntax.Select;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.Syntax
{
    [TestFixture]
    public class ModificationTest
    {
        [Test]
        public void WithInnerJoinTest()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();


            var originalExpr = Select(tUser.FirstName).From(tUser).Where(tUser.UserId.In(1, 2)).OrderBy(tUser.FirstName).Done();
            var newExpr = originalExpr.WithInnerJoin(tCustomer, tCustomer.UserId == tUser.UserId);

            var expected = "SELECT [A0].[FirstName] FROM [dbo].[user] [A0] JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId] WHERE [A0].[UserId] IN(1,2) ORDER BY [A0].[FirstName]";
            Assert.AreEqual(expected, newExpr.ToSql());
        }

        [Test]
        public void WithLeftJoinTest()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();


            var originalExpr = Select(tUser.FirstName).From(tUser).Where(tUser.UserId.In(1, 2)).OrderBy(tUser.FirstName).Done();
            var newExpr = originalExpr.WithLeftJoin(tCustomer, tCustomer.UserId == tUser.UserId);

            var expected = "SELECT [A0].[FirstName] FROM [dbo].[user] [A0] LEFT JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId] WHERE [A0].[UserId] IN(1,2) ORDER BY [A0].[FirstName]";
            Assert.AreEqual(expected, newExpr.ToSql());
        }

        [Test]
        public void WithFullJoinTest()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();


            var originalExpr = Select(tUser.FirstName).From(tUser).Where(tUser.UserId.In(1, 2)).OrderBy(tUser.FirstName).Done();
            var newExpr = originalExpr.WithFullJoin(tCustomer, tCustomer.UserId == tUser.UserId);

            var expected = "SELECT [A0].[FirstName] FROM [dbo].[user] [A0] FULL JOIN [dbo].[Customer] [A1] ON [A1].[UserId]=[A0].[UserId] WHERE [A0].[UserId] IN(1,2) ORDER BY [A0].[FirstName]";
            Assert.AreEqual(expected, newExpr.ToSql());
        }

        [Test]
        public void WithCrossJoinTest()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();


            var originalExpr = Select(tUser.FirstName).From(tUser).Where(tUser.UserId.In(1, 2)).OrderBy(tUser.FirstName).Done();
            var newExpr = originalExpr.WithCrossJoin(tCustomer);

            var expected = "SELECT [A0].[FirstName] FROM [dbo].[user] [A0] CROSS JOIN [dbo].[Customer] [A1] WHERE [A0].[UserId] IN(1,2) ORDER BY [A0].[FirstName]";
            Assert.AreEqual(expected, newExpr.ToSql());
        }

        [Test]
        public void WithJoin_Error()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();

            var originalExpr = Select(tUser.UserId).From(tUser).Union(Select(tCustomer.UserId).From(tCustomer)).OrderBy(tUser.FirstName.WithSource(null)).Done();

            Assert.That(() => originalExpr.WithInnerJoin(tCustomer, tCustomer.UserId == tUser.UserId),
                Throws.TypeOf<SqExpressException>().With.Message.EqualTo("Join can be done only with a query specification"));
        }

        [Test]
        public void AddOrderBy_Basic()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();

            var originalExpr = Select(tUser.UserId).From(tUser).Union(Select(tCustomer.UserId).From(tCustomer)).Done();

            var withOrderBy = originalExpr.AddOrderBy(tUser.FirstName);

            var expected = "SELECT [A0].[UserId] FROM [dbo].[user] [A0] UNION SELECT [A1].[UserId] FROM [dbo].[Customer] [A1] ORDER BY [A0].[FirstName]";
            Assert.AreEqual(expected, withOrderBy.ToSql());

            ExprOrderBy orderBy = tUser.FirstName;

            withOrderBy = originalExpr.AddOrderBy(orderBy);

            expected = "SELECT [A0].[UserId] FROM [dbo].[user] [A0] UNION SELECT [A1].[UserId] FROM [dbo].[Customer] [A1] ORDER BY [A0].[FirstName]";
            Assert.AreEqual(expected, withOrderBy.ToSql());
        }

        [Test]
        public void AddOffsetFetch_OffsetOnly()
        {
            var tUser = Tables.User();

            var originalExpr = Select(tUser.UserId).From(tUser).OrderBy(tUser.FirstName, tUser.LastName).Done();

            var withOrderBy = originalExpr.AddOffsetFetch(2, null);

            var expected = "SELECT [A0].[UserId] FROM [dbo].[user] [A0] ORDER BY [A0].[FirstName],[A0].[LastName] OFFSET 2 ROW";
            Assert.AreEqual(expected, withOrderBy.ToSql());
        }

        [Test]
        public void AddOffsetFetch_Basic()
        {
            var tUser = Tables.User();

            var originalExpr = Select(tUser.UserId).From(tUser).Done().AddOrderBy(new ExprOrderByItem[]{ tUser.FirstName, tUser.LastName });

            var withOrderBy = originalExpr.AddOffsetFetch(1, 2);

            var expected = "SELECT [A0].[UserId] FROM [dbo].[user] [A0] ORDER BY [A0].[FirstName],[A0].[LastName] OFFSET 1 ROW FETCH NEXT 2 ROW ONLY";
            Assert.AreEqual(expected, withOrderBy.ToSql());
        }
    }
}