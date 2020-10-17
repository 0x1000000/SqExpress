using System;
using NUnit.Framework;
using SqExpress.Syntax.Names;

namespace SqExpress.Test.Syntax.Meta
{
    [TestFixture]
    public class EqualityTest
    {
        [Test]
        public void ColumnName_CaseInsensitive()
        {
            var c1 = new ExprColumnName("FirstName");
            var c1eq1 = new ExprColumnName("fiRstNamE");

            var c2difName = new ExprColumnName("FirstName2");

            var eqI = ExprNameEqualityComparer.CaseInsensitive;
            Assert.AreNotEqual(c1, c1eq1);
            Assert.That(c1, Is.EqualTo(c1eq1).Using(eqI));
            Assert.AreEqual(eqI.GetHashCode(c1), eqI.GetHashCode(c1eq1));
            Assert.AreNotEqual(c1.GetHashCode(), c1eq1.GetHashCode());

            Assert.That(c1, Is.Not.EqualTo(c2difName).Using(eqI));
            Assert.AreNotEqual(eqI.GetHashCode(c1), eqI.GetHashCode(c2difName));
        }

        [Test]
        public void ColumnName_CaseSensitive()
        {
            var c1 = new ExprColumnName("FirstName");
            var c1eq1 = new ExprColumnName("FirstName");
            var c1eq2 = new ExprColumnName("fiRstNamE");

            var c2difName = new ExprColumnName("FirstName2");

            var eqI = ExprNameEqualityComparer.CaseSensitive;
            Assert.AreEqual(c1, c1eq1);
            Assert.AreNotEqual(c1, c1eq2);

            Assert.That(c1, Is.EqualTo(c1eq1).Using(eqI));
            Assert.That(c1, Is.Not.EqualTo(c1eq2).Using(eqI));

            Assert.AreEqual(eqI.GetHashCode(c1), eqI.GetHashCode(c1eq1));
            Assert.AreEqual(c1.GetHashCode(), c1eq1.GetHashCode());
            Assert.AreNotEqual(c1.GetHashCode(), c1eq2.GetHashCode());

            Assert.That(c1, Is.Not.EqualTo(c2difName).Using(eqI));
            Assert.AreNotEqual(eqI.GetHashCode(c1), eqI.GetHashCode(c2difName));
        }
    }
}