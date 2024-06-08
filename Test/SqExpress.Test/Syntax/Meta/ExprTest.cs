using NUnit.Framework;
using SqExpress.Syntax.Names;

namespace SqExpress.Test.Syntax.Meta
{
    [TestFixture]
    public class ExprTest
    {
        [Test]
        public void ExprTable_Test()
        {
            ExprTable t = new ExprTable(new ExprTableFullName(new ExprDbSchema(new ExprDatabaseName("test"), new ExprSchemaName("dbo")), new ExprTableName("User")), null);

            Assert.AreEqual("[test].[dbo].[User]", t.ToSql());
            Assert.AreEqual("\"test\".\"public\".\"User\"", t.ToPgSql());

            t = t.SyntaxTree().ModifyDescendants<ExprTableFullName>(i => new ExprTableFullName(new ExprDbSchema(null, i.DbSchema!.Schema), i.TableName));

            Assert.AreEqual("[dbo].[User]", t.ToSql());
            Assert.AreEqual("\"public\".\"User\"", t.ToPgSql());

            t = t.SyntaxTree().ModifyDescendants<ExprTableFullName>(i => new ExprTableFullName(null, i.TableName));

            Assert.AreEqual("[User]", t.ToSql());
            Assert.AreEqual("\"User\"", t.ToPgSql());
        }

    }
}