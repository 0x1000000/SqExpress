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
            StringBuilder builder = new StringBuilder();
            SqlInjectionChecker.AppendStringEscapeSingleQuote(builder, original);
            return builder.ToString();
        }

        private static string AppendStringEscape2(string original)
        {
            StringBuilder builder = new StringBuilder();
            SqlInjectionChecker.AppendStringEscapeSingleQuoteAndBackslash(builder, original);
            return builder.ToString();
        }

        [Test]
        public void Test()
        {
            Assert.AreEqual("''\\\\''", AppendStringEscape2("'\\'"));
        }
    }
}