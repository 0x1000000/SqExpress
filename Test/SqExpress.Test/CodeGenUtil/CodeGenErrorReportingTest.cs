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

        private static string GetRepositoryRoot()
            => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
    }
}
#endif
