#if NET
using System;
using System.IO;
using NUnit.Framework;
using SqExpress.CodeGenUtil;
using SqExpress.CodeGenUtil.Ef;

namespace SqExpress.Test.CodeGenUtil
{
    [TestFixture]
    [NonParallelizable]
    public class CodeGenErrorReportingTest
    {
        [Test]
        public void ExpectedCliFailure_IsWrittenToStandardErrorWithOperationContext()
        {
            var originalOut = Console.Out;
            var originalError = Console.Error;
            using var output = new StringWriter();
            using var error = new StringWriter();
            try
            {
                Console.SetOut(output);
                Console.SetError(error);

                var exitCode = Program.Main(new[]
                {
                    "gentables",
                    "ef",
                    Path.Combine(TestContext.CurrentContext.TestDirectory, "missing.csproj"),
                    "-o",
                    ".",
                    "-n",
                    "Test.Tables",
                    "-v",
                    "quiet"
                });

                Assert.That(exitCode, Is.EqualTo(1));
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }

            Assert.That(output.ToString(), Is.Empty);
            Assert.That(error.ToString(), Does.StartWith("SqExpress EF table generation failed:"));
            Assert.That(error.ToString(), Does.Contain("Could not find EF project"));
        }

        [TestCase("", "", "")]
        [TestCase("normal output", "", "Standard output:\r\nnormal output")]
        [TestCase("", "failure", "Standard error:\r\nfailure")]
        [TestCase("normal output", "failure", "Standard output:\r\nnormal output\r\nStandard error:\r\nfailure")]
        public void CapturedProcessOutput_LabelsStreams(string standardOutput, string standardError, string expected)
        {
            var actual = ProcessRunner.FormatCapturedOutput(standardOutput, standardError)
                .Replace(Environment.NewLine, "\r\n");

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void BuildTargets_CaptureExitCodesAndReportContext()
        {
            var targets = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "SqExpress", "SqExpress.targets"));

            Assert.That(targets, Does.Contain("PropertyName=\"SqModelGenExitCode\""));
            Assert.That(targets, Does.Contain("SqExpress model generation failed with exit code $(SqModelGenExitCode)"));
            Assert.That(targets, Does.Contain("PropertyName=\"_SqEfTablesGenExitCode\""));
            Assert.That(targets, Does.Contain("ItemName=\"_SqEfTablesGenConsoleOutput\""));
            Assert.That(targets, Does.Contain("@(_SqEfTablesGenConsoleOutput, ' ')"));
            Assert.That(targets, Does.Contain("Text=\"$(SqEfTablesGenErrorText)\""));
            Assert.That(targets, Does.Contain("IgnoreExitCode=\"true\""));
        }

        [Test]
        public void PmcWrapper_ForwardsStandardErrorAsTerminatingError()
        {
            var module = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "SqExpress", "PsTools", "SqExpressTools.psm1"));

            Assert.That(module, Does.Contain("Write-Error -Message $errorText -ErrorAction Stop"));
            Assert.That(module, Does.Contain("without error output"));
        }

        [Test]
        public void TableGenerationProperties_UseCanonicalNamesWithLegacyFallbacks()
        {
            var root = GetRepositoryRoot();
            var props = File.ReadAllText(Path.Combine(root, "SqExpress", "SqExpress.props"));
            var targets = File.ReadAllText(Path.Combine(root, "SqExpress", "SqExpress.targets"));
            var module = File.ReadAllText(Path.Combine(root, "SqExpress", "PsTools", "SqExpressTools.psm1"));

            foreach (var suffix in new[]
                     {
                         "Output", "Namespace", "TableClassPrefix", "UseTableDeclarationAttributes",
                         "SkipUnknownColumnTypes", "SplitTablesBySchema", "CleanOutput", "Include", "Exclude"
                     })
            {
                Assert.That(props, Does.Contain($"<SqTablesGen{suffix}"));
                Assert.That(props, Does.Contain($"<SqTablseGen{suffix}"));
                Assert.That(targets.IndexOf($"$(SqTablesGen{suffix})", StringComparison.Ordinal),
                    Is.LessThan(targets.IndexOf($"$(SqTablseGen{suffix})", StringComparison.Ordinal)));
            }

            Assert.That(module, Does.Contain("GetCurrentProjectProperty (\"SqTablesGen\" + $propertySuffix)"));
            Assert.That(module, Does.Contain("GetCurrentProjectProperty (\"SqTablseGen\" + $propertySuffix)"));
            Assert.That(module, Does.Contain("GetTableGenProperty \"UseTableDeclarationAttributes\""));
            Assert.That(module, Does.Contain("GetTableGenProperty \"SkipUnknownColumnTypes\""));
            Assert.That(module, Does.Contain("GetTableGenProperty \"SplitTablesBySchema\""));
            Assert.That(module, Does.Contain("$PSBoundParameters.ContainsKey('UseTableDeclarationAttributes')"));
            Assert.That(module, Does.Contain("$PSBoundParameters.ContainsKey('Include')"));
        }

        [Test]
        public void EfProperties_ContainOnlyEfSpecificPublicSettings()
        {
            var props = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "SqExpress", "SqExpress.props"));

            Assert.That(props, Does.Contain("<SqEfTablesGenEnable>")
                .And.Contain("<SqEfTablesGenProject>")
                .And.Contain("<SqEfTablesGenDbContext>")
                .And.Contain("<SqEfTablesGenFramework>"));
            Assert.That(props, Does.Not.Contain("<SqEfTablesGenOutput>")
                .And.Not.Contain("<SqEfTablesGenNamespace>")
                .And.Not.Contain("<SqEfTablesGenCleanOutput>")
                .And.Not.Contain("<SqEfTablesGenInclude>"));
        }

        [Test]
        public void PmcEfGeneration_UsesEfProjectDefaultsWhenArgumentsAreOmitted()
        {
            var module = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "SqExpress", "PsTools", "SqExpressTools.psm1"));

            Assert.That(module, Does.Contain("!$PSBoundParameters.ContainsKey('ConnectionString')"));
            Assert.That(module, Does.Contain("GetCurrentProjectProperty \"SqEfTablesGenProject\""));
            Assert.That(module, Does.Contain("!$PSBoundParameters.ContainsKey('DbContext')"));
            Assert.That(module, Does.Contain("GetCurrentProjectProperty \"SqEfTablesGenDbContext\""));
            Assert.That(module, Does.Contain("!$PSBoundParameters.ContainsKey('Framework')"));
            Assert.That(module, Does.Contain("GetCurrentProjectProperty \"SqEfTablesGenFramework\""));
        }

        private static string GetRepositoryRoot()
            => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
    }
}
#endif
