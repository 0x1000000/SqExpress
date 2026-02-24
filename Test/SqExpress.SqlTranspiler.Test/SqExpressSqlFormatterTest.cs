using NUnit.Framework;

namespace SqExpress.SqlTranspiler.Test
{
    [TestFixture]
    public class SqExpressSqlFormatterTest
    {
        [Test]
        public void Format_BasicSelect_FormatsAndKeepsSqlValid()
        {
            var formatter = new SqExpressSqlFormatter();
            var transpiler = new SqExpressSqlTranspiler();

            var formatted = formatter.Format("select u.UserId,u.Name from dbo.Users u where u.IsActive=1 order by u.Name desc");

            Assert.That(formatted, Does.StartWith("SELECT "));
            Assert.That(formatted, Does.Contain("\r\nFROM "));
            Assert.That(formatted, Does.Contain("\r\nWHERE "));
            Assert.That(formatted, Does.Contain("\r\nORDER BY "));
            Assert.DoesNotThrow(() => transpiler.Transpile(formatted));
        }

        [Test]
        public void Format_InvalidSql_Throws()
        {
            var formatter = new SqExpressSqlFormatter();

            var ex = Assert.Throws<SqExpressSqlTranspilerException>(() => formatter.Format("SELECT FROM"));

            Assert.That(ex?.Message, Does.Contain("Could not parse SQL"));
        }

        [Test]
        public void Format_SeveralStatements_FormatsWholeScript()
        {
            var formatter = new SqExpressSqlFormatter();

            var formatted = formatter.Format("select 1 as A; select 2 as B");

            Assert.That(formatted, Does.Contain("SELECT 1 AS A"));
            Assert.That(formatted, Does.Contain("SELECT 2 AS B"));
        }
    }
}
