#if !NETFRAMEWORK
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using NUnit.Framework;
using SqExpress.CodeGenUtil;
using SqExpress.CodeGenUtil.DbManagers;
using SqExpress.CodeGenUtil.Model;

namespace SqExpress.Test.CodeGenUtil
{
    [TestFixture]
    public class StringHelpersTest
    {
        [Test]
        public void DeSnake_BasicTest()
        {
            Assert.AreEqual("", StringHelper.DeSnake(""));
            Assert.AreEqual("Abc", StringHelper.DeSnake("abc"));
            Assert.AreEqual("ABc", StringHelper.DeSnake("a_bc"));
            Assert.AreEqual("ABc", StringHelper.DeSnake("a_bc_"));
            Assert.AreEqual("ABC3", StringHelper.DeSnake("a_b_c_3"));
            Assert.AreEqual("StringHelpersTestOk", StringHelper.DeSnake("string_Helpers_testOk"));
            Assert.AreEqual("D3aBc", StringHelper.DeSnake("3a_bc"));
            Assert.AreEqual("D3abc", StringHelper.DeSnake("3abc"));
            Assert.AreEqual("D3aBc", StringHelper.DeSnake("++3a_bc"));
            Assert.AreEqual("W3ABc", StringHelper.DeSnake("+W+3~a_bc"));
            Assert.AreEqual("Parent0", StringHelper.DeSnake("Parent0"));
        }

        [Test]
        public void DeSnake_BasicTest2()
        {
            Assert.AreEqual("NoNo5", StringHelper.AddNumberUntilUnique("NoNo5", "No", s=> s == "NoNo5"));
            Assert.AreEqual("NoNo5", StringHelper.AddNumberUntilUnique("No", "No", s=> s == "NoNo5"));
            Assert.AreEqual("NoNo5", StringHelper.AddNumberUntilUnique("NoNo2", "No", s=> s == "NoNo5"));
            Assert.AreEqual("No2", StringHelper.AddNumberUntilUnique("No", "", s=> s == "No2"));
        }
    }
}
#endif
