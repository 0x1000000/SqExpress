using System;
using System.IO;
using System.Text;
using System.Xml;
using NUnit.Framework;
using SqExpress.Syntax;
using SqExpress.Syntax.Select;
using SqExpress.SyntaxTreeOperations;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.QueryBuilder
{
    [TestFixture]
    public class CteTest
    {
        [Test]
        public void TestSimple()
        {
            var cte = new SimpleCte();

            var expr = Select(cte.Num).From(cte).Done();

            var sql = SerDeSer(expr);
            Console.WriteLine(sql);
        }

        [Test]
        public void TestSimpleRecursive()
        {
            var cte = new SimpleRecursiveCte();
            var expr = Select(Column("Num").WithSource(cte.Alias)).From(cte).Done();

            string actual = SerDeSer(expr);

            var expected =
                "WITH [SimpleRecursiveCte] AS(SELECT 1 [Num] UNION ALL SELECT [A1].[Num]+1 FROM [SimpleRecursiveCte] [A1] WHERE [A1].[Num]<10)SELECT [A0].[Num] FROM [SimpleRecursiveCte] [A0]";

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestInsert()
        {
            var cte = new SimpleRecursiveCte();
            var table = new TargetTable();

            var expr = InsertInto(table, table.Val).From(Select(cte.Num).From(cte));

            var expected =
                "WITH [SimpleRecursiveCte] AS(SELECT 1 [Num] UNION ALL SELECT [A1].[Num]+1 FROM [SimpleRecursiveCte] [A1] WHERE [A1].[Num]<10)INSERT INTO [dbo].[TargetTable]([Val]) SELECT [A0].[Num] FROM [SimpleRecursiveCte] [A0]";

            Assert.AreEqual(expected, SerDeSer(expr));

            var expectedMySql =
                "INSERT INTO `TargetTable`(`Val`) WITH RECURSIVE `SimpleRecursiveCte` AS(SELECT 1 `Num` UNION ALL SELECT `A0`.`Num`+1 FROM `SimpleRecursiveCte` `A0` WHERE `A0`.`Num`<10) SELECT `A1`.`Num` FROM `SimpleRecursiveCte` `A1`";

            Assert.AreEqual(expectedMySql, expr.ToMySql());

            var expectedPgSql =
                "WITH RECURSIVE \"SimpleRecursiveCte\" AS(SELECT 1 \"Num\" UNION ALL SELECT \"A1\".\"Num\"+1 FROM \"SimpleRecursiveCte\" \"A1\" WHERE \"A1\".\"Num\"<10)INSERT INTO \"public\".\"TargetTable\"(\"Val\") SELECT \"A0\".\"Num\" FROM \"SimpleRecursiveCte\" \"A0\"";

            Assert.AreEqual(expectedPgSql, expr.ToPgSql());
        }

        [Test]
        public void TestUpdate()
        {
            var cte = new RefSimpleRecursiveWithOriginalCte();
            var table = new TargetTable();

            var expr = Update(table).Set(table.Val, cte.Num).From(table).InnerJoin(cte, on: cte.Num == table.Val).All();

            var expected =
                "WITH [SimpleRecursiveCte] AS(SELECT 1 [Num] UNION ALL SELECT [A2].[Num]+1 FROM [SimpleRecursiveCte] [A2] WHERE [A2].[Num]<10),[RefSimpleRecursiveWithOriginalCte] AS(SELECT [A3].[Num] [OriginalNum],[A3].[Num]*10 [Num] FROM [SimpleRecursiveCte] [A3] WHERE [A3].[Num]<10)UPDATE [A0] SET [A0].[Val]=[A1].[Num] FROM [dbo].[TargetTable] [A0] JOIN [RefSimpleRecursiveWithOriginalCte] [A1] ON [A1].[Num]=[A0].[Val]";

            Assert.AreEqual(expected, SerDeSer(expr));

            var expectedMySql =
                "UPDATE `TargetTable` `A0` JOIN (WITH RECURSIVE `SimpleRecursiveCte` AS(SELECT 1 `Num` UNION ALL SELECT `A1`.`Num`+1 FROM `SimpleRecursiveCte` `A1` WHERE `A1`.`Num`<10),`RefSimpleRecursiveWithOriginalCte` AS(SELECT `A2`.`Num` `OriginalNum`,`A2`.`Num`*10 `Num` FROM `SimpleRecursiveCte` `A2` WHERE `A2`.`Num`<10) SELECT * FROM `RefSimpleRecursiveWithOriginalCte` `A3`)`A3` ON `A3`.`Num`=`A0`.`Val` SET `A0`.`Val`=`A3`.`Num`";

            Assert.AreEqual(expectedMySql, expr.ToMySql());
        }

        [Test]
        public void TestRefSimpleRecursive()
        {
            var cte = new RefSimpleRecursiveCte();

            var expr = Select(Column("Num").WithSource(cte.Alias)).From(cte).Done();

            string actual = SerDeSer(expr);

            const string expected =
                "WITH [SimpleRecursiveCte] AS(SELECT 1 [Num] UNION ALL SELECT [A1].[Num]+1 FROM [SimpleRecursiveCte] [A1] WHERE [A1].[Num]<10),[RefSimpleRecursiveCte] AS(SELECT [A2].[Num]*10 [Num] FROM [SimpleRecursiveCte] [A2] WHERE [A2].[Num]<10)SELECT [A0].[Num] FROM [RefSimpleRecursiveCte] [A0]";

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestUnionOfOneCte()
        {
            var cte = new RefSimpleRecursiveCte();
            var cte2 = new RefSimpleRecursiveCte();

            var expr = Select(cte.Num).From(cte).UnionAll(Select(cte2.Num).From(cte2)).Done();

            var expected =
                "WITH [SimpleRecursiveCte] AS(SELECT 1 [Num] UNION ALL SELECT [A2].[Num]+1 FROM [SimpleRecursiveCte] [A2] WHERE [A2].[Num]<10),[RefSimpleRecursiveCte] AS(SELECT [A3].[Num]*10 [Num] FROM [SimpleRecursiveCte] [A3] WHERE [A3].[Num]<10)SELECT [A0].[Num] FROM [RefSimpleRecursiveCte] [A0] UNION ALL SELECT [A1].[Num] FROM [RefSimpleRecursiveCte] [A1]";

            Assert.AreEqual(expected, SerDeSer(expr));
        }
        [Test]
        public void TestUnionOfDifferentCte()
        {
            var cte = new RefSimpleRecursiveCte();
            var cte2 = new SimpleCte();

            var expr = Select(cte.Num).From(cte).UnionAll(Select(cte2.Num).From(cte2)).Done();

            var expected =
                "WITH [SimpleRecursiveCte] AS(SELECT 1 [Num] UNION ALL SELECT [A2].[Num]+1 FROM [SimpleRecursiveCte] [A2] WHERE [A2].[Num]<10),[RefSimpleRecursiveCte] AS(SELECT [A3].[Num]*10 [Num] FROM [SimpleRecursiveCte] [A3] WHERE [A3].[Num]<10),[SimpleCte] AS(SELECT 1 [Num])SELECT [A0].[Num] FROM [RefSimpleRecursiveCte] [A0] UNION ALL SELECT [A1].[Num] FROM [SimpleCte] [A1]";

            Assert.AreEqual(expected, SerDeSer(expr));

            var expectedMySql =
                "WITH RECURSIVE `SimpleRecursiveCte` AS(SELECT 1 `Num` UNION ALL SELECT `A0`.`Num`+1 FROM `SimpleRecursiveCte` `A0` WHERE `A0`.`Num`<10),`RefSimpleRecursiveCte` AS(SELECT `A1`.`Num`*10 `Num` FROM `SimpleRecursiveCte` `A1` WHERE `A1`.`Num`<10),`SimpleCte` AS(SELECT 1 `Num`) SELECT `A2`.`Num` FROM `RefSimpleRecursiveCte` `A2` UNION ALL SELECT `A3`.`Num` FROM `SimpleCte` `A3`";

            Assert.AreEqual(expectedMySql, expr.ToMySql());
        }

        [Test]
        public void TestJoinCte()
        {
            var cte = new JoinCte();

            var expr = Select(cte.C1, cte.C2).From(cte).Done();

            const string expectedTSql =
                "WITH [SimpleRecursiveCte] AS(SELECT 1 [Num] UNION ALL SELECT [A1].[Num]+1 FROM [SimpleRecursiveCte] [A1] WHERE [A1].[Num]<10),[RefSimpleRecursiveCte] AS(SELECT [A2].[Num]*10 [Num] FROM [SimpleRecursiveCte] [A2] WHERE [A2].[Num]<10),[JoinCte] AS(SELECT [A3].[Num] [C1],[A4].[Num] [C2] FROM [SimpleRecursiveCte] [A3] CROSS JOIN [RefSimpleRecursiveCte] [A4])SELECT [A0].[C1],[A0].[C2] FROM [JoinCte] [A0]";

            Assert.AreEqual(expectedTSql, SerDeSer(expr));

            var expectedPgSql = 
                "WITH RECURSIVE \"SimpleRecursiveCte\" AS(SELECT 1 \"Num\" UNION ALL SELECT \"A1\".\"Num\"+1 FROM \"SimpleRecursiveCte\" \"A1\" WHERE \"A1\".\"Num\"<10),\"RefSimpleRecursiveCte\" AS(SELECT \"A2\".\"Num\"*10 \"Num\" FROM \"SimpleRecursiveCte\" \"A2\" WHERE \"A2\".\"Num\"<10),\"JoinCte\" AS(SELECT \"A3\".\"Num\" \"C1\",\"A4\".\"Num\" \"C2\" FROM \"SimpleRecursiveCte\" \"A3\" CROSS JOIN \"RefSimpleRecursiveCte\" \"A4\")SELECT \"A0\".\"C1\",\"A0\".\"C2\" FROM \"JoinCte\" \"A0\"";
            Assert.AreEqual(expectedPgSql, expr.ToPgSql());

            var expectedMySql =
                "WITH RECURSIVE `SimpleRecursiveCte` AS(SELECT 1 `Num` UNION ALL SELECT `A0`.`Num`+1 FROM `SimpleRecursiveCte` `A0` WHERE `A0`.`Num`<10),`RefSimpleRecursiveCte` AS(SELECT `A1`.`Num`*10 `Num` FROM `SimpleRecursiveCte` `A1` WHERE `A1`.`Num`<10),`JoinCte` AS(SELECT `A2`.`Num` `C1`,`A3`.`Num` `C2` FROM `SimpleRecursiveCte` `A2` CROSS JOIN `RefSimpleRecursiveCte` `A3`) SELECT `A4`.`C1`,`A4`.`C2` FROM `JoinCte` `A4`";

            Assert.AreEqual(expectedMySql, expr.ToMySql());
        }

        [Test]
        public void TestSimpleFromRecursive()
        {
            var cte = new SimpleFromRecursive();

            var expr = Select(cte.Num).From(cte).OrderBy(cte.Num).OffsetFetch(2, 3).Done();

            var expectedTSql =
                "WITH [SimpleCte] AS(SELECT 1 [Num]),[SimpleFromRecursive] AS(SELECT [A1].[Num] FROM [SimpleCte] [A1] UNION ALL SELECT [A2].[Num]+1 FROM [SimpleFromRecursive] [A2] WHERE [A2].[Num]<10)SELECT [A0].[Num] FROM [SimpleFromRecursive] [A0] ORDER BY [A0].[Num] OFFSET 2 ROW FETCH NEXT 3 ROW ONLY";

            Assert.AreEqual(expectedTSql, SerDeSer(expr));
        }

        private static string SerDeSer(IExpr expr)
        {
            var initialSql = expr.ToSql();
#if NET
            using MemoryStream writer = new MemoryStream();
            expr
                .SyntaxTree()
                .ExportToJson(new System.Text.Json.Utf8JsonWriter(writer));

            var jsonText = Encoding.UTF8.GetString(writer.ToArray());

            var doc = System.Text.Json.JsonDocument.Parse(jsonText);

            var restoredExpr = ExprDeserializer.DeserializeFormJson(doc.RootElement);
#else
            var sb = new StringBuilder();
            using var writer = XmlWriter.Create(sb);
            expr.SyntaxTree().ExportToXml(writer);

            var doc = new XmlDocument();
            doc.LoadXml(sb.ToString());

            var restoredExpr = ExprDeserializer.DeserializeFormXml(doc.DocumentElement!);
#endif

            var restoredSql = restoredExpr.ToSql();

            Assert.AreEqual(initialSql, restoredSql);

            return initialSql;
        }

        [Test]
        public void TestRecursiveCteEmptyModifier()
        {
            var cte = new SimpleRecursiveCte();

            var cte2 = cte.SyntaxTree().Modify(e => e);

            Assert.AreSame(cte, cte2);
        }

        [Test]
        public void TestModify()
        {
            var cte = new RefSimpleRecursiveCte();

            var expr = Select(cte.Num).From(cte).Done();

            var updated = expr.SyntaxTree().Modify<ExprCte>(e => e.Name == nameof(SimpleRecursiveCte) ? e.WithName("Modified") : e)!;

            var expected =
                "WITH [Modified] AS(SELECT 1 [Num] UNION ALL SELECT [A1].[Num]+1 FROM [Modified] [A1] WHERE [A1].[Num]<10),[RefSimpleRecursiveCte] AS(SELECT [A2].[Num]*10 [Num] FROM [Modified] [A2] WHERE [A2].[Num]<10)SELECT [A0].[Num] FROM [RefSimpleRecursiveCte] [A0]";

            Assert.AreEqual(expected, SerDeSer(updated));
        }

        class SimpleCte : CteBase
        {
            public SimpleCte(Alias alias = default) : base(nameof(SimpleCte), alias)
            {
                this.Num = this.CreateInt32Column("Num");
            }

            public Int32CustomColumn Num { get; }

            public override IExprSubQuery CreateQuery()
            {
                return Select(Literal(1).As(Num)).Done();
            }
        }

        class SimpleRecursiveCte : CteBase
        {
            public SimpleRecursiveCte(Alias alias = default) : base(nameof(SimpleRecursiveCte), alias)
            {
                this.Num = this.CreateInt32Column("Num");
            }

            public Int32CustomColumn Num { get; }

            public override IExprSubQuery CreateQuery()
            {
                var next = new SimpleRecursiveCte();

                return Select(Literal(1).As(Num))
                    .UnionAll(
                        Select(next.Num+1)
                            .From(next)
                            .Where(next.Num < 10))
                    .Done();
            }
        }

        class RefSimpleRecursiveCte : CteBase
        {
            public RefSimpleRecursiveCte(Alias alias = default) : base(nameof(RefSimpleRecursiveCte), alias)
            {
                this.Num = this.CreateInt32Column("Num");
            }

            public Int32CustomColumn Num { get; }

            public override IExprSubQuery CreateQuery()
            {
                var simpleRecursiveCte = new SimpleRecursiveCte();

                return Select((simpleRecursiveCte.Num*10).As(Num)).From(simpleRecursiveCte).Where(simpleRecursiveCte.Num < 10).Done();
            }
        }

        class RefSimpleRecursiveWithOriginalCte : CteBase
        {
            public RefSimpleRecursiveWithOriginalCte(Alias alias = default) : base(nameof(RefSimpleRecursiveWithOriginalCte), alias)
            {
                this.Num = this.CreateInt32Column("Num");
                this.OriginalNum = this.CreateInt32Column("OriginalNum");
            }

            public Int32CustomColumn Num { get; }

            public Int32CustomColumn OriginalNum { get; }

            public override IExprSubQuery CreateQuery()
            {
                var simpleRecursiveCte = new SimpleRecursiveCte();

                return Select(
                        simpleRecursiveCte.Num.As(this.OriginalNum),
                        (simpleRecursiveCte.Num * 10).As(Num))
                    .From(simpleRecursiveCte)
                    .Where(simpleRecursiveCte.Num < 10)
                    .Done();
            }
        }


        class JoinCte : CteBase
        {
            public JoinCte(Alias alias = default) : base(nameof(JoinCte), alias)
            {
                this.C1 = this.CreateInt32Column("C1");
                this.C2 = this.CreateInt32Column("C2");
            }

            public Int32CustomColumn C1 { get; }

            public Int32CustomColumn C2 { get; }

            public override IExprSubQuery CreateQuery()
            {
                var simpleRecursiveCte = new SimpleRecursiveCte();
                var refSimpleRecursiveCte = new RefSimpleRecursiveCte();

                return Select(simpleRecursiveCte.Num.As(this.C1), refSimpleRecursiveCte.Num.As(this.C2))
                    .From(simpleRecursiveCte)
                    .CrossJoin(refSimpleRecursiveCte)
                    .Done();
            }
        }

        class SimpleFromRecursive : CteBase
        {
            public SimpleFromRecursive(Alias alias = default) : base(nameof(SimpleFromRecursive), alias)
            {
                this.Num = this.CreateInt32Column("Num");
            }

            public Int32CustomColumn Num { get; }

            public override IExprSubQuery CreateQuery()
            {
                var simple = new SimpleCte();

                var next = new SimpleFromRecursive();

                return Select(simple.Num).From(simple)
                    .UnionAll(
                        Select(next.Num + 1)
                            .From(next)
                            .Where(next.Num < 10))
                    .Done();
            }
        }

        class TargetTable : TableBase
        {
            public TargetTable(Alias alias = default) : base("dbo", "TargetTable", alias)
            {
                this.Val = this.CreateInt32Column("Val");
            }

            public Int32TableColumn Val { get; }
        }
    }
}