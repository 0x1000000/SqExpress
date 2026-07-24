using System.Diagnostics;

namespace SqExpress.CodeGenUtil.Ef
{
    internal static class ProcessRunner
    {
        public static int Run(string fileName, string arguments, string workingDirectory, out string output)
        {
            var exitCode = Run(fileName, arguments, workingDirectory, out var standardOutput, out var standardError);
            output = FormatCapturedOutput(standardOutput, standardError);
            return exitCode;
        }

        internal static string FormatCapturedOutput(string standardOutput, string standardError)
        {
            var output = standardOutput.Trim();
            var error = standardError.Trim();
            if (output.Length == 0)
            {
                return error.Length == 0 ? string.Empty : "Standard error:" + System.Environment.NewLine + error;
            }
            if (error.Length == 0)
            {
                return "Standard output:" + System.Environment.NewLine + output;
            }
            return "Standard output:" + System.Environment.NewLine + output + System.Environment.NewLine +
                   "Standard error:" + System.Environment.NewLine + error;
        }

        public static int Run(string fileName, string arguments, string workingDirectory, out string standardOutputText, out string standardErrorText)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo) ?? throw new SqExpressCodeGenException($"Could not start {fileName}.");
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            standardOutputText = standardOutput.GetAwaiter().GetResult();
            standardErrorText = standardError.GetAwaiter().GetResult();
            return process.ExitCode;
        }

        public static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
    }
}
