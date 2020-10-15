using System;
using System.Text;
using NUnit.Framework;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Value;
using SqExpress.SyntaxExplorer;

namespace SqExpress.Test.Syntax
{
    [TestFixture]
    public class SyntaxTreeExploringTest
    {
        [Test]
        public void WalkThroughTest()
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

        [Test]
        public void FindTest()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();

            var e = SqQueryBuilder.Select(tUser.UserId, tUser.FirstName, tCustomer.CustomerId)
                .From(tUser)
                .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                .Where(tUser.Version == 5)
                .Done();

            var versionCol =e.SyntaxTree().FirstOrDefault<ExprColumnName>(cn=>cn.Name == tUser.Version.ColumnName.Name);

            Assert.NotNull(versionCol);
            Assert.AreEqual(tUser.Version.ColumnName, versionCol);
        }



        [Test]
        public void ModifyTest()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();

            IExpr e = SqQueryBuilder.Select(tUser.UserId, tUser.FirstName, tCustomer.CustomerId)
                .From(tUser)
                .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId & tUser.Version == 1)
                .Where(tUser.UserId.In(1))
                .Done();

            //Before
            Assert.AreEqual("SELECT [A0].[UserId],[A0].[FirstName],[A1].[CustomerId] " +
                            "FROM [dbo].[user] [A0] JOIN [dbo].[Customer] [A1] " +
                            "ON [A1].[UserId]=[A0].[UserId] AND [A0].[Version]=1 " +
                            "WHERE [A0].[UserId] IN(1)", e.ToSql());

            e = e.SyntaxTree()
                .Modify(subE =>
                    {
                        if (subE is ExprIn)
                        {
                            return null;
                        }
                        if (subE is ExprBooleanAnd and && and.Right is ExprBooleanEq eq && eq.Right is ExprInt32Literal)
                        {
                            return and.Left;
                        }
                        if (subE is ExprColumnName c && c.Name == "UserId")
                        {
                            return new ExprColumnName("UserNewId");
                        }
                        return subE;
                    });

            //After
            Assert.AreEqual("SELECT [A0].[UserNewId],[A0].[FirstName],[A1].[CustomerId] " +
                            "FROM [dbo].[user] [A0] JOIN [dbo].[Customer] [A1] " +
                            "ON [A1].[UserNewId]=[A0].[UserNewId]", e.ToSql());
        }
    }
}