using NUnit.Framework;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.Syntax
{
    [TestFixture]
    public class ArithmeticTest
    {
        [Test]
        public void TestNum()
        {
            var user = Tables.User();

            var e = ((user.Version + 2)*3 - 2)/3;

            Assert.AreEqual("(([A0].[Version]+2)*3-2)/3", e.ToSql());
        }

        [Test]
        public void TestString()
        {
            var user = Tables.User(Alias.Empty);

            var e = Select(user.FirstName + ", " + user.LastName + Cast(GetUtcDate(), SqlType.String())).From(user).Done();

            Assert.AreEqual("SELECT [FirstName]+', '+[LastName]+CAST(GETUTCDATE() AS [nvarchar](MAX)) FROM [dbo].[user]", e.ToSql());
            Assert.AreEqual("SELECT \"FirstName\"||', '||\"LastName\"||CAST(now() at time zone 'utc' AS character varying) FROM \"public\".\"user\"",e.ToPgSql());
        }

        [Test]
        public void TestSpace()
        {
            var user = Tables.User(Alias.Empty);

            var e = user.FirstName + " " + user.LastName;

            Assert.AreEqual("[FirstName]+' '+[LastName]", e.ToSql());
        }
    }
}