#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SqExpress.DataAccess;
using SqExpress.QueryBuilders.Select;
using SqExpress.SqlParser;
using SqExpress.Syntax;
using SqExpress.Syntax.Internal;
using ExprValue = SqExpress.Syntax.Value.ExprValue;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test
{
    [TestFixture]
    public class ExprExtensionTest
    {
        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public async Task QueryDictionaryTest(bool predicate)
        {
            var testData = new Tuple<int, string>[]
            {
                new Tuple<int, string>(1, "v1"),
                new Tuple<int, string>(2, "v2"),
                new Tuple<int, string>(3, "v3"),
            };

            var query = SqQueryBuilder.SelectOne();

            TestSqDatabase database = new TestSqDatabase(BuildQueryDelegate(query: query, testData: testData));


            Func<int, string, bool>? p = null;
            if (predicate)
            {
                p = (k, v) => k != 3 && v != "v3";
            }

            var result = await query.QueryDictionary(database, r => r.GetInt32("K"), r => r.GetString("V"), predicate: p);

            var count = predicate ? 2 : 3;
            Assert.AreEqual(count, result.Count);
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(testData[i].Item2, result[testData[i].Item1]);
            }
        }

        [Test]
        public async Task QueryDictionaryTest_KeyDuplication()
        {
            var testData = new Tuple<int, string>[]
            {
                new Tuple<int, string>(1, "v1"),
                new Tuple<int, string>(2, "v2"),
                new Tuple<int, string>(2, "v3"),
            };

            var query = SqQueryBuilder.SelectOne();

            TestSqDatabase database = new TestSqDatabase(queryImplementation: BuildQueryDelegate(query, testData));


            var result = await query.QueryDictionary(database, r => r.GetInt32("K"), r => r.GetString("V"), KeyDuplicationHandler);

            var count = 3;
            Assert.AreEqual(count, result.Count);
            for (int i = 0; i < count; i++)
            {
                var item1 = testData[i].Item2 == "v3" ? 3 : testData[i].Item1;
                Assert.AreEqual(testData[i].Item2, result[item1]);
            }

            static void KeyDuplicationHandler(int key, string oldValue, string newValue, Dictionary<int, string> dictionary)
            {
                Assert.AreEqual(2, key);
                Assert.AreEqual("v2", oldValue);
                Assert.AreEqual("v3", newValue);

                dictionary.Add(3, newValue);
            }
        }

        [Test]
        public void QueryDictionaryTest_KeyDuplication_Fail()
        {
            var testData = new Tuple<int, string>[]
            {
                new Tuple<int, string>(1, "v1"),
                new Tuple<int, string>(2, "v2"),
                new Tuple<int, string>(2, "v3"),
            };

            var query = SqQueryBuilder.SelectOne();

            TestSqDatabase database = new TestSqDatabase(queryImplementation: BuildQueryDelegate(query, testData));

            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await query.QueryDictionary(database, r => r.GetInt32("K"), r => r.GetString("V"));
            });

        }

        [Test]
        public void WithParams_Dictionary_ReplacesParameters()
        {
            const string sql = "SELECT @id [Id], @name [Name]";
            Assert.That(SqTSqlParser.TryParse(sql, out IExpr? expr, out var error), Is.True, error);

            var replaced = expr!.WithParams(new Dictionary<string, ExprValue>
            {
                {"id", Literal(1)},
                {"name", Literal("John")}
            });

            Assert.That(replaced.SyntaxTree().Descendants().OfType<ExprParameter>().Any(), Is.False);
            var exported = replaced.ToSql();
            Assert.That(exported, Does.Not.Contain("@id"));
            Assert.That(exported, Does.Not.Contain("@name"));
            Assert.That(exported, Does.Contain("'John'"));
        }

        [Test]
        public void WithParams_Dictionary_ThrowsWhenParameterMissing()
        {
            const string sql = "SELECT @id [Id], @name [Name]";
            Assert.That(SqTSqlParser.TryParse(sql, out IExpr? expr, out var error), Is.True, error);

            var ex = Assert.Throws<SqExpressException>(() => expr!.WithParams(new Dictionary<string, ExprValue>
            {
                {"id", Literal(1)}
            }));

            Assert.That(ex!.Message, Does.Contain("Could not find parameter name"));
        }

        [Test]
        public void WithParams_Dictionary_UsesExactNameMatching()
        {
            const string sql = "SELECT @id [Id]";
            Assert.That(SqTSqlParser.TryParse(sql, out IExpr? expr, out var error), Is.True, error);

            var ex = Assert.Throws<SqExpressException>(() => expr!.WithParams(new Dictionary<string, ExprValue>
            {
                {"@id", Literal(1)}
            }));

            Assert.That(ex!.Message, Does.Contain("Could not find parameter id"));
        }

#if NET8_0_OR_GREATER
        [Test]
        public void WithParams_Span_ReplacesParameters()
        {
            const string sql = "SELECT @id [Id], @name [Name]";
            Assert.That(SqTSqlParser.TryParse(sql, out IExpr? expr, out var error), Is.True, error);

            var replaced = expr!.WithParams(
                ("id", 1),
                ("name", "John"));

            Assert.That(replaced.SyntaxTree().Descendants().OfType<ExprParameter>().Any(), Is.False);
            var exported = replaced.ToSql();
            Assert.That(exported, Does.Not.Contain("@id"));
            Assert.That(exported, Does.Not.Contain("@name"));
            Assert.That(exported, Does.Contain("'John'"));
        }

        [Test]
        public void WithParams_Span_ThrowsOnDuplicateName()
        {
            const string sql = "SELECT @id [Id]";
            Assert.That(SqTSqlParser.TryParse(sql, out IExpr? expr, out var error), Is.True, error);

            var ex = Assert.Throws<SqExpressException>(() => expr!.WithParams(
                ("id", 1),
                ("id", 2)));

            Assert.That(ex!.Message, Does.Contain("Duplicate parameter name 'id'"));
        }

        [Test]
        public void WithParams_Span_ThrowsOnEmptyName()
        {
            const string sql = "SELECT @id [Id]";
            Assert.That(SqTSqlParser.TryParse(sql, out IExpr? expr, out var error), Is.True, error);

            var ex = Assert.Throws<SqExpressException>(() => expr!.WithParams(
                ("", 1)));

            Assert.That(ex!.Message, Does.Contain("Parameter name cannot be null or empty"));
        }
#endif

        private static TestSqDatabase.QueryDelegate<object> BuildQueryDelegate(IQuerySpecificationBuilderInitial? query, Tuple<int, string>[] testData)
        {
            Task<object> QueryImplementation(IExprQuery q, object seed, Func<object, ISqDataRecordReader, object> aggregator)
            {
                Assert.AreEqual(query?.Done().ToSql(), q.ToSql());

                var acc = (Dictionary<int, string>)seed;

                foreach (var tuple in testData)
                {
                    var reader = new TestSqDataRecordReader(getByColName: colName =>
                    {
                        switch (colName)
                        {
                            case "K": return tuple.Item1;
                            case "V": return tuple.Item2;
                            default: throw new Exception($"Unknown column: \"{colName}\"");
                        }
                    });

                    acc = (Dictionary<int, string>)aggregator(acc, reader);
                }

                return Task.FromResult((object)acc);
            }

            return QueryImplementation;
        }

    }
}
