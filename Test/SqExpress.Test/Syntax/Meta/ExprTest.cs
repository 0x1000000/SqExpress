using NUnit.Framework;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Output;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;

namespace SqExpress.Test.Syntax.Meta
{
    [TestFixture]
    public class ExprTest
    {
        private static string?[] ExtractSelectingOutputNames(IExprQuery query)
        {
            var selectings = query.ExtractSelecting();
            var result = new string?[selectings.Count];
            for (var i = 0; i < selectings.Count; i++)
            {
                result[i] = selectings[i] is IExprNamedSelecting named ? named.OutputName : null;
            }

            return result;
        }

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

        [Test]
        public void ExprQuerySpecification_GetOutputColumnNames_ReturnsProjectedNames()
        {
            var query = SqQueryBuilder.Select(
                    SqQueryBuilder.Literal(1).As("Id"),
                    SqQueryBuilder.Literal("A").As("Name"))
                .Done();

            var outputNames = query.GetOutputColumnNames();

            Assert.That(outputNames, Is.EqualTo(new string?[] { "Id", "Name" }));
            Assert.That(ExtractSelectingOutputNames(query), Is.EqualTo(new string?[] { "Id", "Name" }));
        }

        [Test]
        public void ExprQuerySpecification_GetOutputColumnNames_ReturnsNullForUnnamedItems()
        {
            var query = SqQueryBuilder.Select(
                    SqQueryBuilder.Literal(1),
                    SqQueryBuilder.Literal("A"))
                .Done();

            var outputNames = query.GetOutputColumnNames();

            Assert.That(outputNames, Is.EqualTo(new string?[] { null, null }));
            Assert.That(ExtractSelectingOutputNames(query), Is.EqualTo(new string?[] { null, null }));
        }

        [Test]
        public void ExprQuerySpecification_GetOutputColumnNames_SupportsMixedNamedAndUnnamedItems()
        {
            var query = SqQueryBuilder.Select(
                    SqQueryBuilder.Literal(1).As("Id"),
                    SqQueryBuilder.Literal("A"))
                .Done();

            var outputNames = query.GetOutputColumnNames();

            Assert.That(outputNames, Is.EqualTo(new string?[] { "Id", null }));
            Assert.That(ExtractSelectingOutputNames(query), Is.EqualTo(new string?[] { "Id", null }));
        }

        [Test]
        public void ExprSelect_GetOutputColumnNames_DelegatesToSubQuery()
        {
            var query = new ExprSelect(
                SqQueryBuilder.Select(
                        SqQueryBuilder.Literal(1).As("Id"),
                        SqQueryBuilder.Literal("A").As("Name"))
                    .Done(),
                new ExprOrderBy(System.Array.Empty<ExprOrderByItem>()));

            var outputNames = query.GetOutputColumnNames();

            Assert.That(outputNames, Is.EqualTo(new string?[] { "Id", "Name" }));
            Assert.That(ExtractSelectingOutputNames(query), Is.EqualTo(new string?[] { "Id", "Name" }));
        }

        [Test]
        public void ExprSelectOffsetFetch_GetOutputColumnNames_DelegatesToSubQuery()
        {
            var query = new ExprSelectOffsetFetch(
                SqQueryBuilder.Select(
                        SqQueryBuilder.Literal(1).As("Id"),
                        SqQueryBuilder.Literal("A").As("Name"))
                    .Done(),
                new ExprOrderByOffsetFetch(
                    new[] { new ExprOrderByItem(SqQueryBuilder.Literal(1), false) },
                    new ExprOffsetFetch(SqQueryBuilder.Literal(0), SqQueryBuilder.Literal(10))));

            var outputNames = query.GetOutputColumnNames();

            Assert.That(outputNames, Is.EqualTo(new string?[] { "Id", "Name" }));
            Assert.That(ExtractSelectingOutputNames(query), Is.EqualTo(new string?[] { "Id", "Name" }));
        }

        [Test]
        public void ExprQueryExpression_GetOutputColumnNames_UsesLeftSideProjection()
        {
            var left = SqQueryBuilder.Select(
                    SqQueryBuilder.Literal(1).As("LeftId"),
                    SqQueryBuilder.Literal("A").As("LeftName"))
                .Done();
            var right = SqQueryBuilder.Select(
                    SqQueryBuilder.Literal(2).As("RightId"),
                    SqQueryBuilder.Literal("B").As("RightName"))
                .Done();
            var query = new ExprQueryExpression(left, right, ExprQueryExpressionType.UnionAll);

            var outputNames = query.GetOutputColumnNames();

            Assert.That(outputNames, Is.EqualTo(new string?[] { "LeftId", "LeftName" }));
            Assert.That(ExtractSelectingOutputNames(query), Is.EqualTo(new string?[] { "LeftId", "LeftName" }));
        }

        [Test]
        public void ExprInsertOutput_GetOutputColumnNames_ReturnsOutputAliases()
        {
            var insert = new ExprInsert(
                new ExprTableFullName(new ExprDbSchema(null, new ExprSchemaName("dbo")), new ExprTableName("Users")),
                new[] { new ExprColumnName("Id"), new ExprColumnName("Name") },
                new ExprInsertValues(new[]
                {
                    new ExprInsertValueRow(new IExprAssigning[] { SqQueryBuilder.Literal(1), SqQueryBuilder.Literal("A") })
                }));
            var query = new ExprInsertOutput(
                insert,
                new[]
                {
                    new ExprAliasedColumnName(new ExprColumnName("Id"), new ExprColumnAlias("InsertedId")),
                    new ExprAliasedColumnName(new ExprColumnName("Name"), new ExprColumnAlias("InsertedName"))
                });

            var outputNames = query.GetOutputColumnNames();

            Assert.That(outputNames, Is.EqualTo(new string?[] { "InsertedId", "InsertedName" }));
            Assert.That(ExtractSelectingOutputNames(query), Is.EqualTo(new string?[] { "InsertedId", "InsertedName" }));
        }

        [Test]
        public void ExprDeleteOutput_GetOutputColumnNames_ReturnsOutputAliases()
        {
            var delete = new ExprDelete(
                new ExprTable(new ExprTableFullName(new ExprDbSchema(null, new ExprSchemaName("dbo")), new ExprTableName("Users")), null),
                null,
                null);
            var query = new ExprDeleteOutput(
                delete,
                new[]
                {
                    new ExprAliasedColumn(new ExprColumn(null, new ExprColumnName("Id")), new ExprColumnAlias("DeletedId")),
                    new ExprAliasedColumn(new ExprColumn(null, new ExprColumnName("Name")), new ExprColumnAlias("DeletedName"))
                });

            var outputNames = query.GetOutputColumnNames();

            Assert.That(outputNames, Is.EqualTo(new string?[] { "DeletedId", "DeletedName" }));
            Assert.That(ExtractSelectingOutputNames(query), Is.EqualTo(new string?[] { "DeletedId", "DeletedName" }));
        }

        [Test]
        public void ExprMergeOutput_GetOutputColumnNames_ReturnsOutputNames()
        {
            var target = new ExprTable(new ExprTableFullName(new ExprDbSchema(null, new ExprSchemaName("dbo")), new ExprTableName("Users")), null);
            var source = new ExprTable(new ExprTableFullName(new ExprDbSchema(null, new ExprSchemaName("dbo")), new ExprTableName("UsersSource")), null);
            var query = new ExprMergeOutput(
                target,
                source,
                new ExprBooleanEq(new ExprColumn(null, new ExprColumnName("Id")), new ExprColumn(null, new ExprColumnName("Id"))),
                null,
                null,
                null,
                new ExprOutput(new IExprOutputColumn[]
                {
                    new ExprOutputColumnInserted(new ExprAliasedColumnName(new ExprColumnName("Id"), new ExprColumnAlias("InsertedId"))),
                    new ExprOutputAction(new ExprColumnAlias("Action"))
                }));

            var outputNames = query.GetOutputColumnNames();

            Assert.That(outputNames, Is.EqualTo(new string?[] { "InsertedId", "Action" }));
            Assert.That(ExtractSelectingOutputNames(query), Is.EqualTo(new string?[] { "InsertedId", "Action" }));
        }

        [Test]
        public void ExprMergeOutput_GetOutputColumnNames_ReturnsNullForUnnamedAction()
        {
            var target = new ExprTable(new ExprTableFullName(new ExprDbSchema(null, new ExprSchemaName("dbo")), new ExprTableName("Users")), null);
            var source = new ExprTable(new ExprTableFullName(new ExprDbSchema(null, new ExprSchemaName("dbo")), new ExprTableName("UsersSource")), null);
            var query = new ExprMergeOutput(
                target,
                source,
                new ExprBooleanEq(new ExprColumn(null, new ExprColumnName("Id")), new ExprColumn(null, new ExprColumnName("Id"))),
                null,
                null,
                null,
                new ExprOutput(new IExprOutputColumn[]
                {
                    new ExprOutputColumnInserted(new ExprAliasedColumnName(new ExprColumnName("Id"), new ExprColumnAlias("InsertedId"))),
                    new ExprOutputAction(null)
                }));

            var outputNames = query.GetOutputColumnNames();

            Assert.That(outputNames, Is.EqualTo(new string?[] { "InsertedId", null }));
            Assert.That(query.ExtractSelecting().Count, Is.EqualTo(2));
            Assert.That(query.ExtractSelecting()[1], Is.TypeOf<ExprAliasedColumnName>());
            Assert.That(((ExprAliasedColumnName)query.ExtractSelecting()[1]).Column.Name, Is.EqualTo("$action"));
        }

        [Test]
        public void ExprQueryList_GetOutputColumnNames_DelegatesToContainedQuery()
        {
            var query = SqQueryBuilder.Select(
                    SqQueryBuilder.Literal(1).As("Id"),
                    SqQueryBuilder.Literal("A").As("Name"))
                .Done();
            var list = new ExprQueryList(new IExprComplete[] { query });

            var outputNames = list.GetOutputColumnNames();

            Assert.That(outputNames, Is.EqualTo(new string?[] { "Id", "Name" }));
            Assert.That(ExtractSelectingOutputNames(list), Is.EqualTo(new string?[] { "Id", "Name" }));
        }

        [Test]
        public void ExprQueryList_GetOutputColumnNames_DelegatesWhenQueryIsNotFirst()
        {
            var query = SqQueryBuilder.Select(
                    SqQueryBuilder.Literal(1).As("Id"),
                    SqQueryBuilder.Literal("A").As("Name"))
                .Done();
            var list = new ExprQueryList(new IExprComplete[]
            {
                new ExprDelete(
                    new ExprTable(new ExprTableFullName(new ExprDbSchema(null, new ExprSchemaName("dbo")), new ExprTableName("Users")), null),
                    null,
                    null),
                query
            });

            var outputNames = list.GetOutputColumnNames();

            Assert.That(outputNames, Is.EqualTo(new string?[] { "Id", "Name" }));
            Assert.That(ExtractSelectingOutputNames(list), Is.EqualTo(new string?[] { "Id", "Name" }));
        }

        [Test]
        public void ExprQueryList_GetOutputColumnNames_WhenNoQuery_ReturnsEmpty()
        {
            var list = new ExprQueryList(new IExprComplete[]
            {
                new ExprDelete(
                    new ExprTable(new ExprTableFullName(new ExprDbSchema(null, new ExprSchemaName("dbo")), new ExprTableName("Users")), null),
                    null,
                    null)
            });

            var outputNames = list.GetOutputColumnNames();

            Assert.That(outputNames, Is.Empty);
            Assert.That(list.ExtractSelecting(), Is.Empty);
        }
    }
}
