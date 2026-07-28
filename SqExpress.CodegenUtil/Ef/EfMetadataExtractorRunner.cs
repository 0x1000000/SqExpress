using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SqExpress.CodeGenUtil.Ef
{
    internal static class EfMetadataExtractorRunner
    {
        internal delegate int ProcessExecutor(string fileName, string arguments, string workingDirectory, out string output);

        public static async Task<EfMetadataDocument> Extract(string projectPath, string? dbContextTypeName, string? framework)
        {
            var fullProjectPath = Path.GetFullPath(projectPath, Directory.GetCurrentDirectory());
            if (!File.Exists(fullProjectPath) || !string.Equals(Path.GetExtension(fullProjectPath), ".csproj", StringComparison.OrdinalIgnoreCase))
            {
                throw new SqExpressCodeGenException($"Could not find EF project \"{projectPath}\".");
            }

            var projectDirectory = Path.GetDirectoryName(fullProjectPath)!;
            var targetFramework = ResolveTargetFramework(fullProjectPath, framework);
            var assemblyName = GetProjectProperty(fullProjectPath, "AssemblyName", targetFramework);
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                assemblyName = Path.GetFileNameWithoutExtension(fullProjectPath);
            }

            var extractorDirectory = Path.Combine(
                projectDirectory,
                "obj",
                "SqExpress",
                "EfMetadataExtractor",
                ComputeHash(fullProjectPath + "|" + targetFramework + "|" + dbContextTypeName));
            Directory.CreateDirectory(extractorDirectory);

            var extractorProjectPath = Path.Combine(extractorDirectory, "SqExpress.EfMetadataExtractor.csproj");
            var extractorProgramPath = Path.Combine(extractorDirectory, "Program.cs");

            await WriteAllTextIfChangedAsync(
                extractorProjectPath,
                CreateExtractorProject(fullProjectPath, targetFramework));
            await WriteAllTextIfChangedAsync(extractorProgramPath, ExtractorProgramSource);

            var outputExcluded = BuildExtractor(
                extractorProjectPath,
                targetFramework,
                extractorDirectory,
                ProcessRunner.Run);

            var runProperties = DisableGenerationProperty + (outputExcluded ? " " + ExcludeOutputProperty : "");
            var args =
                $"run --project {ProcessRunner.Quote(extractorProjectPath)} --framework {ProcessRunner.Quote(targetFramework)} --no-restore --no-build {runProperties} -- " +
                $"--target-assembly {ProcessRunner.Quote(assemblyName)} " +
                (string.IsNullOrWhiteSpace(dbContextTypeName) ? "" : $" --db-context {ProcessRunner.Quote(dbContextTypeName!)}");

            var runExitCode = ProcessRunner.Run("dotnet", args, projectDirectory, out var metadataJson, out var runError);
            if (runExitCode != 0)
            {
                var extractorError = runError.Trim();
                if (extractorError.Length > 0 && extractorError.IndexOfAny(new[] { '\r', '\n' }) < 0)
                {
                    throw new SqExpressCodeGenException($"EF metadata extractor execution failed: {extractorError}");
                }

                throw new SqExpressCodeGenException(
                    $"EF metadata extractor execution failed.{Environment.NewLine}" +
                    ProcessRunner.FormatCapturedOutput(metadataJson, runError));
            }

            if (string.IsNullOrWhiteSpace(metadataJson))
            {
                throw new SqExpressCodeGenException($"EF metadata extractor did not return metadata JSON.{Environment.NewLine}{runError}");
            }

            return JsonSerializer.Deserialize<EfMetadataDocument>(metadataJson)
                   ?? throw new SqExpressCodeGenException("EF metadata extractor returned an empty metadata document.");
        }

        internal static bool BuildExtractor(
            string extractorProjectPath,
            string targetFramework,
            string extractorDirectory,
            ProcessExecutor processRunner)
        {
            var buildArgs =
                $"build {ProcessRunner.Quote(extractorProjectPath)} --framework {ProcessRunner.Quote(targetFramework)} --nologo {DisableGenerationProperty}";

            var buildExitCode = processRunner("dotnet", buildArgs, extractorDirectory, out var buildOutput);
            if (buildExitCode == 0)
            {
                return false;
            }

            var fallbackBuildArgs = buildArgs + " " + ExcludeOutputProperty;
            var fallbackExitCode = processRunner("dotnet", fallbackBuildArgs, extractorDirectory, out var fallbackOutput);
            if (fallbackExitCode != 0)
            {
                throw new SqExpressCodeGenException(
                    $"Could not build EF metadata extractor.{Environment.NewLine}" +
                    $"Normal build (existing table descriptors included):{Environment.NewLine}{buildOutput}{Environment.NewLine}" +
                    $"Fallback build (generated table output excluded):{Environment.NewLine}{fallbackOutput}");
            }

            return true;
        }

        private static string ResolveTargetFramework(string projectPath, string? framework)
        {
            if (!string.IsNullOrWhiteSpace(framework))
            {
                return framework!;
            }

            var targetFramework = GetProjectProperty(projectPath, "TargetFramework", null);
            if (!string.IsNullOrWhiteSpace(targetFramework))
            {
                return targetFramework;
            }

            var targetFrameworks = GetProjectProperty(projectPath, "TargetFrameworks", null)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Trim())
                .Where(i => i.Length > 0)
                .ToArray();

            return targetFrameworks.Length switch
            {
                1 => targetFrameworks[0],
                0 => throw new SqExpressCodeGenException($"Could not resolve TargetFramework for EF project \"{projectPath}\"."),
                _ => throw new SqExpressCodeGenException(
                    $"EF project \"{projectPath}\" targets multiple frameworks ({string.Join(", ", targetFrameworks)}). Specify --framework.")
            };
        }

        private static string GetProjectProperty(string projectPath, string propertyName, string? framework)
        {
            var args =
                $"msbuild {ProcessRunner.Quote(projectPath)} -nologo -getProperty:{propertyName} " +
                "/p:SqEfTablesGenEnable=false" +
                (string.IsNullOrWhiteSpace(framework) ? "" : $" /p:TargetFramework={ProcessRunner.Quote(framework!)}");

            var exitCode = ProcessRunner.Run("dotnet", args, Path.GetDirectoryName(projectPath)!, out var output);
            if (exitCode != 0)
            {
                throw new SqExpressCodeGenException(
                    $"Could not read MSBuild property {propertyName} from \"{projectPath}\".{Environment.NewLine}{output}");
            }

            return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim() ?? "";
        }

        private static string ComputeHash(string value)
        {
            using var sha256 = SHA256.Create();
            return string.Concat(sha256.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(b => b.ToString("x2"))).Substring(0, 16);
        }

        private static async Task WriteAllTextIfChangedAsync(string path, string text)
        {
            if (File.Exists(path))
            {
                var existing = await File.ReadAllTextAsync(path);
                if (string.Equals(existing, text, StringComparison.Ordinal))
                {
                    return;
                }
            }

            await File.WriteAllTextAsync(path, text, Encoding.UTF8);
        }

        internal static string CreateExtractorProject(string targetProjectPath, string targetFramework)
        {
            return $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>{EscapeXml(targetFramework)}</TargetFramework>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""{EscapeXml(targetProjectPath)}"" ReferenceOutputAssembly=""true"" SetTargetFramework=""TargetFramework={EscapeXml(targetFramework)}"" Properties=""SqEfTablesGenEnable=false"" />
  </ItemGroup>
</Project>
";
        }

        private static string EscapeXml(string value)
            => value
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");

        private const string DisableGenerationProperty = "/p:SqEfTablesGenEnable=false";
        private const string ExcludeOutputProperty = "/p:SqEfTablesGenExcludeOutputFromCompile=true";

        internal static string ExtractorProgramSource { get; } = LoadExtractorProgramSource();

        private static string LoadExtractorProgramSource()
        {
            const string resourceName = "SqExpress.CodeGenUtil.Ef.Extractor.Program.cs";
            using var stream = typeof(EfMetadataExtractorRunner).Assembly.GetManifestResourceStream(resourceName)
                               ?? throw new SqExpressCodeGenException($"Could not find embedded EF metadata extractor source \"{resourceName}\".");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
