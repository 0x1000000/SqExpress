#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using SqExpress.DataAccess;
using SqExpress.QueryBuilders.Select;

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