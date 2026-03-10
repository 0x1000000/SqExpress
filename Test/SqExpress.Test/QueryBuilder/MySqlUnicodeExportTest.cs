using NUnit.Framework;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.QueryBuilder
{
    [TestFixture]
    public class MySqlUnicodeExportTest
    {
        [Test]
        public void UnicodeVarchar_PreservesExplicitSizeAndUsesUtf8Mb4()
        {
            var sql = SqlType.String(20000, isUnicode: true).ToMySql();

            Assert.AreEqual("varchar(20000) character set utf8mb4", sql);
        }
    }
}
