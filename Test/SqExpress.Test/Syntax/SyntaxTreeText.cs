using System;
using System.Text;
using NUnit.Framework;
using SqExpress.SyntaxExplorer;

namespace SqExpress.Test.Syntax
{
    [TestFixture]
    public class SyntaxTreeText
    {
        [Test]
        public void Test()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();

            var e = SqQueryBuilder.Select(tUser.UserId, tUser.FirstName, tCustomer.CustomerId)
                .From(tUser)
                .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                .Done();

            string expected = "0ExprQuerySpecification,1Int32TableColumn,2ExprTableAlias,3ExprAliasGuid,2ExprColumnName," +
                              "1StringTableColumn,2ExprTableAlias,3ExprAliasGuid,2ExprColumnName," +
                              "1Int32TableColumn,2ExprTableAlias,3ExprAliasGuid,2ExprColumnName," +
                              "1ExprJoinedTable,2User,3ExprTableFullName,4ExprSchemaName,4ExprTableName," +
                              "3ExprTableAlias,4ExprAliasGuid,2Customer,3ExprTableFullName,4ExprSchemaName," +
                              "4ExprTableName,3ExprTableAlias,4ExprAliasGuid,2ExprBooleanEq," +
                              "3NullableInt32TableColumn,4ExprTableAlias," +
                              "5ExprAliasGuid,4ExprColumnName,3Int32TableColumn," +
                              "4ExprTableAlias,5ExprAliasGuid,4ExprColumnName,";

            StringBuilder builder = new StringBuilder();

            e.SyntaxTree().WalkThrough((expr, tier) =>
            {
                builder.Append(tier);
                builder.Append(expr.GetType().Name);
                builder.Append(',');
                return VisitorResult<int>.Continue(tier+1);
            }, 0);

            Assert.AreEqual(expected, builder.ToString());
        }
    }
}