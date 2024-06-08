using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using NUnit.Framework;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;
using SqExpress.SyntaxTreeOperations;
using SqExpress.SyntaxTreeOperations.ExportImport.Internal;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.Syntax
{
    [TestFixture]
    public class SyntaxTreeOperationsTest
    {
        [Test]
        public void WalkThroughTest()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();

            var e = Select(tUser.UserId, tUser.FirstName, tCustomer.CustomerId)
                .From(tUser)
                .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                .Done();

            string expected = "0ExprQuerySpecification,1Int32TableColumn,2ExprTableAlias,3ExprAliasGuid," +
                              "2ExprColumnName,1StringTableColumn,2ExprTableAlias,3ExprAliasGuid," +
                              "2ExprColumnName,1Int32TableColumn,2ExprTableAlias,3ExprAliasGuid,2ExprColumnName," +
                              "1ExprJoinedTable,2User,3ExprTableFullName,4ExprDbSchema,5ExprSchemaName," +
                              "4ExprTableName,3ExprTableAlias,4ExprAliasGuid,2Customer,3ExprTableFullName," +
                              "4ExprDbSchema,5ExprSchemaName,4ExprTableName,3ExprTableAlias,4ExprAliasGuid," +
                              "2ExprBooleanEq,3NullableInt32TableColumn,4ExprTableAlias,5ExprAliasGuid," +
                              "4ExprColumnName,3Int32TableColumn,4ExprTableAlias,5ExprAliasGuid,4ExprColumnName,";

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
        public void WalkThroughParentTest()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();

            var e = Select(tUser.UserId, tUser.FirstName, tCustomer.CustomerId)
                .From(tUser)
                .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                .Done();

            string expected = "ExprQuerySpecification((none)),Int32TableColumn(ExprQuerySpecification),ExprTableAlias(Int32TableColumn),ExprAliasGuid(ExprTableAlias),ExprColumnName(Int32TableColumn),StringTableColumn(ExprQuerySpecification),ExprTableAlias(StringTableColumn),ExprAliasGuid(ExprTableAlias),ExprColumnName(StringTableColumn),Int32TableColumn(ExprQuerySpecification),ExprTableAlias(Int32TableColumn),ExprAliasGuid(ExprTableAlias),ExprColumnName(Int32TableColumn),ExprJoinedTable(ExprQuerySpecification),User(ExprJoinedTable),ExprTableFullName(User),ExprDbSchema(ExprTableFullName),ExprSchemaName(ExprDbSchema),ExprTableName(ExprTableFullName),ExprTableAlias(User),ExprAliasGuid(ExprTableAlias),Customer(ExprJoinedTable),ExprTableFullName(Customer),ExprDbSchema(ExprTableFullName),ExprSchemaName(ExprDbSchema),ExprTableName(ExprTableFullName),ExprTableAlias(Customer),ExprAliasGuid(ExprTableAlias),ExprBooleanEq(ExprJoinedTable),NullableInt32TableColumn(ExprBooleanEq),ExprTableAlias(NullableInt32TableColumn),ExprAliasGuid(ExprTableAlias),ExprColumnName(NullableInt32TableColumn),Int32TableColumn(ExprBooleanEq),ExprTableAlias(Int32TableColumn),ExprAliasGuid(ExprTableAlias),ExprColumnName(Int32TableColumn),";

            StringBuilder builder = new StringBuilder();

            e.SyntaxTree().WalkThroughWithParent((expr, parent, ctx) =>
            {
                if (parent != null)
                {
                    var children = parent.SyntaxTree()
                        .WalkThrough((e, list) =>
                            {
                                if (e != parent)
                                {
                                    list.Add(e);
                                    return VisitorResult<List<IExpr>>.StopNode(list);
                                }
                                return VisitorResult<List<IExpr>>.Continue(list);
                            },
                            new List<IExpr>());
                    Assert.IsTrue(children.Contains(expr));
                }

                builder.Append(expr.GetType().Name);
                builder.Append($"({parent?.GetType().Name ?? "(none)"})");
                builder.Append(',');
                return VisitorResult<IExpr>.Continue(expr);
            }, (IExpr)e);

            Assert.AreEqual(expected, builder.ToString());
        }


        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void Descendants_Test(bool self)
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();

            var examples = new List<IExpr>();

            //1
            IExpr ex = Select(tUser.UserId, tUser.FirstName, tCustomer.CustomerId)
                .From(tUser)
                .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                .Done();
            examples.Add(ex);

            //2
            ex = Literal(2);
            examples.Add(ex);

            //3
            ex = Literal(2).As(tUser.UserId);
            examples.Add(ex);

            //4
            ex = Update(tUser)
                    .Set(tUser.UserId, "A")
                    .From(tUser)
                    .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                    .Where(tCustomer.UserId.In(1, 3, 5));
            examples.Add(ex);

            foreach (var e in examples)
            {

                var set = new List<IExpr>();

                e.SyntaxTree()
                    .WalkThrough((expr, tier) =>
                        {
                            if (tier != 0 || self)
                            {
                                set.Add(expr);
                            }

                            return VisitorResult<int>.Continue(tier + 1);
                        },
                        0);

                var list = self ? e.SyntaxTree().DescendantsAndSelf().ToList() : e.SyntaxTree().Descendants().ToList();
                CollectionAssert.AreEqual(set, list);
            }
        }

        [Test]
        public void FindTest()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();

            var e = Select(tUser.UserId, tUser.FirstName, tCustomer.CustomerId)
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

            IExpr e = Select(tUser.UserId, tUser.FirstName, tCustomer.CustomerId)
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
                    })!;

            //After
            Assert.AreEqual("SELECT [A0].[UserNewId],[A0].[FirstName],[A1].[CustomerId] " +
                            "FROM [dbo].[user] [A0] JOIN [dbo].[Customer] [A1] " +
                            "ON [A1].[UserNewId]=[A0].[UserNewId]", e.ToSql());
        }

#if NET
        [Test]
        public void TestExportImportJson()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();

            var selectExpr = Select(tUser.UserId, tUser.FirstName, tCustomer.CustomerId, Cast(Literal(12.8m), SqlType.Decimal((10,2))).As("Salary"))
                .From(tUser)
                .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                .Where(tUser.Version == 5)
                .OrderBy(tUser.FirstName)
                .OffsetFetch(100, 5)
                .Done();

            using MemoryStream writer = new MemoryStream();
            selectExpr
                .SyntaxTree()
                .ExportToJson(new System.Text.Json.Utf8JsonWriter(writer));

            var jsonText = Encoding.UTF8.GetString(writer.ToArray());

            var doc = System.Text.Json.JsonDocument.Parse(jsonText);

            var deserialized = ExprDeserializer.DeserializeFormJson(doc.RootElement);

            Assert.AreEqual(selectExpr.ToSql(), deserialized.ToSql());
        }
#endif

        [Test]
        public void TestExportImportPlain()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();

            var selectExpr = Select(tUser.UserId, tUser.FirstName, tCustomer.CustomerId, Cast(Literal(12.8m), SqlType.Decimal(new DecimalPrecisionScale(10, 2))).As("Salary"))
                .From(tUser)
                .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                .Where(tUser.Version == 5 & tUser.RegDate > new DateTime(2020, 10, 18, 1,2,3,400) & tUser.RegDate <= new DateTime(2021,01,01))
                .OrderBy(tUser.FirstName)
                .OffsetFetch(100, 5)
                .Done();


            var items = selectExpr.SyntaxTree().ExportToPlainList(PlainItem.Create!);

            var res = ExprDeserializer.DeserializeFormPlainList(items);

            Assert.AreEqual(selectExpr.ToSql(), res.ToSql());
        }

        [Test]
        public void TestExportImportXml()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();

            var selectExpr = Select(tUser.UserId, tUser.FirstName, tCustomer.CustomerId, Cast(Literal(12.8m), SqlType.Decimal(new DecimalPrecisionScale(10, 2))).As("Salary"))
                .From(tUser)
                .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                .Where(tUser.Version == 5 & tUser.RegDate > new DateTime(2020, 10, 18, 1,2,3,400) & tUser.RegDate <= new DateTime(2021,01,01))
                .OrderBy(tUser.FirstName)
                .OffsetFetch(100, 5)
                .Done();

            var sb = new StringBuilder();
            
            using XmlWriter writer = XmlWriter.Create(sb);
            selectExpr.SyntaxTree().ExportToXml(writer);

            var doc = new XmlDocument();
            doc.LoadXml(sb.ToString());

            var res = ExprDeserializer.DeserializeFormXml(doc.DocumentElement!);
            Assert.AreEqual(selectExpr.ToSql(), res.ToSql());
        }
    }
}