using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace SqExpress.SqlTranspiler.Test
{
    [TestFixture]
    public class BlazorShowcaseSamplesTest
    {
        private static readonly IReadOnlyDictionary<string, string> ExpectedUnsupportedReasons =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["having-gross-sales"] = "HAVING is not supported",
                ["datepart-rollup"] = "GROUP BY supports only columns"
            };

        [Test]
        public void ShowcaseCatalog_ContainsTwentySamples()
        {
            Assert.That(GetShowcaseSamples().Count, Is.EqualTo(20));
        }

        [TestCaseSource(nameof(GetShowcaseSampleCases))]
        public void ShowcaseSample_HasExpectedTranspileOutcome(ShowcaseSampleProxy sample)
        {
            var transpiler = new SqExpressSqlTranspiler();

            if (ExpectedUnsupportedReasons.TryGetValue(sample.Id, out var expectedReason))
            {
                var ex = Assert.Throws<SqExpressSqlTranspilerException>(() => transpiler.Transpile(sample.SqlText));
                Assert.That(ex!.Message, Does.Contain(expectedReason));
                return;
            }

            var result = transpiler.Transpile(sample.SqlText);
            Assert.That(result.QueryCSharpCode, Is.Not.Empty, sample.Title);
            Assert.That(result.DeclarationsCSharpCode, Is.Not.Empty, sample.Title);

            if (sample.Id == "windowed-order-share")
            {
                Assert.That(result.QueryCSharpCode, Does.Contain("(Sum(r.Revenue).Over().AsValue() - r.Revenue).As(\"RemainingRevenue\")"));
            }
        }

        private static IReadOnlyList<ShowcaseSampleProxy> GetShowcaseSamples()
        {
            var assembly = Assembly.Load("SqExpress.SqlTranspiler.Blazor");
            var catalogType = assembly.GetType("SqExpress.SqlTranspiler.Blazor.Pages.ShowcaseCatalog", throwOnError: true)!;
            var allField = catalogType.GetField("All", BindingFlags.Public | BindingFlags.Static)!;
            var samples = (IEnumerable)allField.GetValue(null)!;

            return samples
                .Cast<object>()
                .Select(sample =>
                {
                    var sampleType = sample.GetType();
                    return new ShowcaseSampleProxy(
                        (string)sampleType.GetProperty("Id")!.GetValue(sample)!,
                        (string)sampleType.GetProperty("Title")!.GetValue(sample)!,
                        (string)sampleType.GetProperty("SqlText")!.GetValue(sample)!);
                })
                .ToList();
        }

        private static IEnumerable<TestCaseData> GetShowcaseSampleCases()
        {
            return GetShowcaseSamples().Select(i => new TestCaseData(i).SetName(i.Title));
        }

        public sealed class ShowcaseSampleProxy
        {
            public ShowcaseSampleProxy(string id, string title, string sqlText)
            {
                this.Id = id;
                this.Title = title;
                this.SqlText = sqlText;
            }

            public string Id { get; }

            public string Title { get; }

            public string SqlText { get; }
        }
    }
}
