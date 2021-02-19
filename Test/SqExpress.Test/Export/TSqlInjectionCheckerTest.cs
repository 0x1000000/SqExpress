using System.Text;
using NUnit.Framework;
using SqExpress.SqlExport.Internal;

namespace SqExpress.Test.Export
{
    [TestFixture]
    public class TSqlInjectionCheckerTest
    {
        [Test]
        public void AppendStringEscape_Basic()
        {
            Assert.AreEqual("", AppendStringEscape(""));
            Assert.AreEqual(@"''", AppendStringEscape(@"'"));
            Assert.AreEqual(@"z''", AppendStringEscape(@"z'"));
            Assert.AreEqual(@"''z", AppendStringEscape(@"'z"));
            Assert.AreEqual(@"z''z", AppendStringEscape(@"z'z"));
            Assert.AreEqual(@"z''z''", AppendStringEscape(@"z'z'"));
            Assert.AreEqual(@"''z''z''", AppendStringEscape(@"'z'z'"));

            Assert.AreEqual(@"''''", AppendStringEscape(@"''"));
            Assert.AreEqual(@"z''''", AppendStringEscape(@"z''"));
            Assert.AreEqual(@"''''z", AppendStringEscape(@"''z"));
            Assert.AreEqual(@"z''''z", AppendStringEscape(@"z''z"));
            Assert.AreEqual(@"z''''z''''", AppendStringEscape(@"z''z''"));
            Assert.AreEqual(@"''''z''''z''''", AppendStringEscape(@"''z''z''"));

            Assert.AreEqual(@"xz''", AppendStringEscape(@"xz'"));
            Assert.AreEqual(@"''xz", AppendStringEscape(@"'xz"));
            Assert.AreEqual(@"xz''xz", AppendStringEscape(@"xz'xz"));
            Assert.AreEqual(@"xz''xz''", AppendStringEscape(@"xz'xz'"));
            Assert.AreEqual(@"''xz''xz''", AppendStringEscape(@"'xz'xz'"));

            Assert.AreEqual(@"xz''''", AppendStringEscape(@"xz''"));
            Assert.AreEqual(@"''''xz", AppendStringEscape(@"''xz"));
            Assert.AreEqual(@"xz''''xz", AppendStringEscape(@"xz''xz"));
            Assert.AreEqual(@"xz''''xz''''", AppendStringEscape(@"xz''xz''"));
            Assert.AreEqual(@"''''xz''''xz''''", AppendStringEscape(@"''xz''xz''"));
        }

        private static string AppendStringEscape(string original)
        {
            var sql = SqQueryBuilder.Literal(original).ToSql();
            return sql.Substring(1, sql.Length-2);
        }

        [Test]
        public void Test()
        {
            Assert.AreEqual("''\\\\''", AppendStringEscapeMySql("'\\'"));
        }

        private static string AppendStringEscapeMySql(string original)
        {
            var sql = SqQueryBuilder.Literal(original).ToMySql();
            return sql.Substring(1, sql.Length - 2);
        }
    }
}