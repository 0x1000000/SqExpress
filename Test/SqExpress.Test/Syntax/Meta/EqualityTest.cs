using System;
using NUnit.Framework;
using SqExpress.Syntax.Names;

namespace SqExpress.Test.Syntax.Meta
{
    [TestFixture]
    public class EqualityTest
    {
        [Test]
        public void Column_Alias()
        {
            var c1 = new ExprColumn(new ExprTableAlias(new ExprAlias("T")), new ExprColumnName("FirstName"));
            var c1eq1 = new ExprColumn(new ExprTableAlias(new ExprAlias("T")), new ExprColumnName("fiRstNamE"));
            var c1eq2 = new ExprColumn(new ExprTableAlias(new ExprAlias("t")), new ExprColumnName("fiRstNamE"));

            var c2difName = new ExprColumn(new ExprTableAlias(new ExprAlias("T")), new ExprColumnName("FirstName2"));
            var c2difAlias = new ExprColumn(new ExprTableAlias(new ExprAlias("T2")), new ExprColumnName("FirstName"));

            Assert.AreEqual(c1, c1eq1);
            Assert.AreEqual(c1.GetHashCode(), c1eq1.GetHashCode());
            Assert.AreEqual(c1, c1eq2);
            Assert.AreEqual(c1.GetHashCode(), c1eq2.GetHashCode());

            Assert.AreNotEqual(c1, c2difName);
            Assert.AreNotEqual(c1, c2difAlias);
        }

        [Test]
        public void Column_AliasAuto()
        {
            var a1 = new ExprAliasGuid(Guid.Parse("C98CB577-6922-411B-8A2C-6E1F39D9E9E7"));
            var a2 = new ExprAliasGuid(Guid.Parse("5D7A203E-6A2E-4BD3-BB8B-112B0AD3C04A"));

            var c1 = new ExprColumn(new ExprTableAlias(a1), new ExprColumnName("FirstName"));
            var c1eq1 = new ExprColumn(new ExprTableAlias(a1), new ExprColumnName("fiRstNamE"));

            var c2difName = new ExprColumn(new ExprTableAlias(a1), new ExprColumnName("FirstName2"));
            var c2difAlias = new ExprColumn(new ExprTableAlias(a2), new ExprColumnName("FirstName"));

            Assert.AreEqual(c1, c1eq1);
            Assert.AreEqual(c1.GetHashCode(), c1eq1.GetHashCode());

            Assert.AreNotEqual(c1, c2difName);
            Assert.AreNotEqual(c1, c2difAlias);
        }
    }
}