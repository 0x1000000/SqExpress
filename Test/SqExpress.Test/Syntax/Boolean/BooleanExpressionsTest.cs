using System;
using NUnit.Framework;
using SqExpress.Syntax.Names;

namespace SqExpress.Test.Syntax.Boolean
{
    [TestFixture]
    public class BooleanExpressionsTest
    {
        [Test]
        public void BasicTest()
        {
            var c1 = new ExprColumn(null, new ExprColumnName("C1"));
            var c2 = new ExprColumn(null, new ExprColumnName("C2"));

            Assert.AreEqual("[C1]", c1.ToSql());
            Assert.AreEqual("[C1]=5", (c1 == 5).ToSql());
            Assert.AreEqual("[C1]>=5 AND [C2]!='6' OR [C2]!='7''n''7'", (c1 >= 5 & c2 != "6" | c2 != "7'n'7").ToSql());
            Assert.AreEqual("[C1]<5 AND([C2]<=6 OR [C2]='7''n''7' AND [C2]>'2020-02-21')", (c1 < 5 & (c2 <= 6 | c2 == "7'n'7" & c2 > new DateTime(2020,02,21))).ToSql());
        }


        [Test]
        public void NotTest_Predicate()
        {
            var c1 = new ExprColumn(null, new ExprColumnName("C1"));

            var a = (!(c1 == 7)).ToSql();

            Assert.AreEqual("NOT [C1]=7", (!(c1 == 7)).ToSql());
            Assert.AreEqual("NOT [C1]=7 AND NOT [C1]=3", (!(c1 == 7) & !(c1==3)).ToSql());
            Assert.AreEqual("NOT [C1]=7 AND NOT [C1]=3 OR NOT [C1]=3", ((!(c1 == 7) & !(c1==3)) | !(c1 == 3)).ToSql());
            Assert.AreEqual("NOT [C1]=7 AND NOT [C1]=3 OR NOT [C1]=3", (!(c1 == 7) & !(c1==3) | !(c1 == 3)).ToSql());
            Assert.AreEqual("NOT(NOT [C1]=7 AND NOT [C1]=3) OR NOT [C1]=3", (!(!(c1 == 7) & !(c1 == 3)) | !(c1 == 3)).ToSql());
        }
    }
}