using System;
using NUnit.Framework;
using SqExpress.Syntax.Value;
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

        [Test]
        public void Bitwise()
        {
            ExprValue e = Literal(21) & Literal(10) + Literal(11);
            Assert.AreEqual("21&(10+11)", e.ToSql());

            e = Literal(21) & ~Literal(10) + Literal(11);
            Assert.AreEqual("21&(~10+11)", e.ToSql());

            e = Literal(21) & ~(Literal(10) + Literal(11));
            Assert.AreEqual("21&~(10+11)", e.ToSql());

            e = Literal(5) & Literal(2) | Literal(3);
            Assert.AreEqual("(5&2)|3", e.ToSql());

            e = Literal(3) | Literal(5) & Literal(2);
            Assert.AreEqual("3|(5&2)", e.ToSql());

            e = Literal(1) ^ Literal(2) ^ ~~Literal(3) ^ Literal(4);
            Assert.AreEqual("1^2^~~3^4", e.ToSql());
        }
    }
}