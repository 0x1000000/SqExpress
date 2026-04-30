using System.Diagnostics;

namespace SqExpress.CodeGenUtil.Ef
{
    internal static class ProcessRunner
    {
        public static int Run(string fileName, string arguments, string workingDirectory, out string output)
        {
            var exitCode = Run(fileName, arguments, workingDirectory, out var standardOutput, out var standardError);
            output = standardOutput + standardError;
            return exitCode;
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
