using NUnit.Framework;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.QueryBuilder
{
    [TestFixture]
    public class ExistsPredicateTest
    {
        [Test]
        public void Test()
        {
            var tbl = Tables.User();

            var expr = Select(tbl.FirstName)
                .From(tbl)
                .Where(tbl.UserId >0 & ExistsIn<User>(tblSub => tblSub.UserId == tbl.UserId))
                .Done();

            var actual = expr.ToSql();

            var expected = "SELECT [A0].[FirstName] FROM [dbo].[user] [A0] WHERE [A0].[UserId]>0 AND EXISTS(SELECT 1 FROM [dbo].[user] [A1] WHERE [A1].[UserId]=[A0].[UserId])";

            Assert.AreEqual(expected, actual);
        }
    }
}