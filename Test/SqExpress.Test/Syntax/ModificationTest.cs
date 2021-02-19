using NUnit.Framework;
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
    }
}