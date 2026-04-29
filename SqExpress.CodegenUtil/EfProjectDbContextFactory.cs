using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace SqExpress.CodeGenUtil
{
    internal sealed class EfProjectDbContextFactory : IDisposable
    {
        private readonly ProjectAssemblyLoadContext _loadContext;
        private readonly object _dbContext;

        private EfProjectDbContextFactory(ProjectAssemblyLoadContext loadContext, object dbContext)
        {
            this._loadContext = loadContext;
            this._dbContext = dbContext;
        }

        public string? ProviderName => GetProviderName(this._dbContext);

        public object Model => GetModel(this._dbContext);

        public static EfProjectDbContextFactory Create(string sourcePath, string? dbContextTypeName)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                throw new SqExpressCodeGenException("EF project or assembly path cannot be empty.");
            }

            var assemblyPath = ResolveAssemblyPath(sourcePath);
            var loadContext = new ProjectAssemblyLoadContext(assemblyPath);

            try
            {
                var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
                var dbContext = CreateDbContext(assembly, dbContextTypeName);
                return new EfProjectDbContextFactory(loadContext, dbContext);
            }
            catch
            {
                loadContext.Unload();
                throw;
            }
        }

        private static string ResolveAssemblyPath(string sourcePath)
        {
            var fullPath = Path.GetFullPath(sourcePath, Directory.GetCurrentDirectory());
            if (File.Exists(fullPath) && string.Equals(Path.GetExtension(fullPath), ".dll", StringComparison.OrdinalIgnoreCase))
            {
                return fullPath;
            }

            if (File.Exists(fullPath) && string.Equals(Path.GetExtension(fullPath), ".csproj", StringComparison.OrdinalIgnoreCase))
            {
                BuildProject(fullPath);
                return GetProjectTargetPath(fullPath);
            }

            throw new SqExpressCodeGenException($"Could not find EF project or assembly \"{sourcePath}\".");
        }

        private static void BuildProject(string projectPath)
        {
            var exitCode = RunDotNet(
                $"build \"{projectPath}\" --nologo /p:SqEfTablesGenEnable=false /p:SqEfTablesGenExcludeOutputFromCompile=true",
                Path.GetDirectoryName(projectPath)!,
                out var output);

            if (exitCode != 0)
            {
                throw new SqExpressCodeGenException($"Could not build EF project \"{projectPath}\".{Environment.NewLine}{output}");
            }
        }

        private static string GetProjectTargetPath(string projectPath)
        {
            var exitCode = RunDotNet(
                $"msbuild \"{projectPath}\" -nologo -getProperty:TargetPath /p:SqEfTablesGenEnable=false /p:SqEfTablesGenExcludeOutputFromCompile=true",
                Path.GetDirectoryName(projectPath)!,
                out var output);

            if (exitCode == 0)
            {
                var targetPath = output
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .LastOrDefault();
                if (!string.IsNullOrWhiteSpace(targetPath) && File.Exists(targetPath))
                {
                    return targetPath;
                }
            }

            throw new SqExpressCodeGenException($"Could not resolve build output for EF project \"{projectPath}\".{Environment.NewLine}{output}");
        }

        private static int RunDotNet(string arguments, string workingDirectory, out string output)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo) ?? throw new SqExpressCodeGenException("Could not start dotnet.");
            output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode;
        }

        private static object CreateDbContext(Assembly assembly, string? dbContextTypeName)
        {
            var dbContextTypes = assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && IsDbContextType(t))
                .ToList();

            var selectedContextType = SelectDbContextType(dbContextTypes, dbContextTypeName);
            var factoryTypes = assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && t.GetInterfaces().Any(IsDesignTimeFactoryInterface))
                .ToList();

            var matchingFactoryTypes = selectedContextType == null
                ? factoryTypes
                : factoryTypes
                    .Where(t => t.GetInterfaces().Any(i => IsDesignTimeFactoryInterface(i) && i.GetGenericArguments()[0] == selectedContextType))
                    .ToList();

            if (matchingFactoryTypes.Count == 1)
            {
                return CreateFromDesignTimeFactory(matchingFactoryTypes[0]);
            }

            if (matchingFactoryTypes.Count > 1)
            {
                throw new SqExpressCodeGenException(
                    "Found multiple EF design-time DbContext factories. Specify --db-context.");
            }

            selectedContextType ??= SelectSingleDbContextType(dbContextTypes);

            if (Activator.CreateInstance(selectedContextType) is { } result)
            {
                return result;
            }

            throw new SqExpressCodeGenException(
                $"Could not create EF DbContext \"{selectedContextType.FullName}\". Add IDesignTimeDbContextFactory<{selectedContextType.Name}> or a parameterless constructor.");
        }

        private static Type? SelectDbContextType(IReadOnlyList<Type> dbContextTypes, string? dbContextTypeName)
        {
            if (string.IsNullOrWhiteSpace(dbContextTypeName))
            {
                return null;
            }

            var matches = dbContextTypes
                .Where(t => string.Equals(t.FullName, dbContextTypeName, StringComparison.Ordinal) ||
                            string.Equals(t.Name, dbContextTypeName, StringComparison.Ordinal))
                .ToList();

            if (matches.Count == 1)
            {
                return matches[0];
            }

            throw new SqExpressCodeGenException(
                matches.Count == 0
                    ? $"Could not find EF DbContext \"{dbContextTypeName}\"."
                    : $"Found multiple EF DbContexts named \"{dbContextTypeName}\". Use a fully-qualified type name.");
        }

        private static Type SelectSingleDbContextType(IReadOnlyList<Type> dbContextTypes)
        {
            if (dbContextTypes.Count == 1)
            {
                return dbContextTypes[0];
            }

            if (dbContextTypes.Count == 0)
            {
                throw new SqExpressCodeGenException("Could not find any EF DbContext in the target project.");
            }

            throw new SqExpressCodeGenException("Found multiple EF DbContexts. Specify --db-context.");
        }

        private static bool IsDesignTimeFactoryInterface(Type type)
            => type.IsGenericType &&
               string.Equals(
                   type.GetGenericTypeDefinition().FullName,
                   "Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory`1",
                   StringComparison.Ordinal);

        private static object CreateFromDesignTimeFactory(Type factoryType)
        {
            var factory = Activator.CreateInstance(factoryType)
                ?? throw new SqExpressCodeGenException($"Could not create EF design-time factory \"{factoryType.FullName}\".");

            var factoryInterface = factoryType.GetInterfaces().Single(IsDesignTimeFactoryInterface);
            var method = factoryInterface.GetMethod("CreateDbContext")
                ?? throw new SqExpressCodeGenException($"Could not find CreateDbContext on \"{factoryType.FullName}\".");

            var dbContext = method.Invoke(factory, new object[] { Array.Empty<string>() })
                ?? throw new SqExpressCodeGenException($"Factory \"{factoryType.FullName}\" did not return a DbContext.");

            if (!IsDbContextType(dbContext.GetType()))
            {
                throw new SqExpressCodeGenException($"Factory \"{factoryType.FullName}\" did not return a DbContext.");
            }

            return dbContext;
        }

        private static bool IsDbContextType(Type type)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (string.Equals(current.FullName, "Microsoft.EntityFrameworkCore.DbContext", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static object GetDatabaseFacade(object dbContext)
        {
            return dbContext.GetType().GetProperty("Database", BindingFlags.Public | BindingFlags.Instance)?.GetValue(dbContext)
                   ?? throw new SqExpressCodeGenException($"Could not read Database from EF DbContext \"{dbContext.GetType().FullName}\".");
        }

        private static object GetModel(object dbContext)
        {
            var designTimeModel = TryGetDesignTimeModel(dbContext);
            if (designTimeModel != null)
            {
                return designTimeModel;
            }

            return dbContext.GetType().GetProperty("Model", BindingFlags.Public | BindingFlags.Instance)?.GetValue(dbContext)
                   ?? throw new SqExpressCodeGenException($"Could not read Model from EF DbContext \"{dbContext.GetType().FullName}\".");
        }

        private static object? TryGetDesignTimeModel(object dbContext)
        {
            var efAssembly = dbContext.GetType().BaseType?.Assembly;
            var infrastructureType = efAssembly?.GetType("Microsoft.EntityFrameworkCore.Infrastructure.IInfrastructure`1")
                ?.MakeGenericType(typeof(IServiceProvider));
            if (infrastructureType == null || !infrastructureType.IsInstanceOfType(dbContext))
            {
                return null;
            }

            var serviceProvider = infrastructureType.GetProperty("Instance")?.GetValue(dbContext) as IServiceProvider;
            var designTimeModelType = efAssembly?.GetType("Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel");
            if (serviceProvider == null || designTimeModelType == null)
            {
                return null;
            }

            var designTimeModel = serviceProvider.GetService(designTimeModelType);
            return designTimeModel?.GetType().GetProperty("Model")?.GetValue(designTimeModel);
        }

        private static string? GetProviderName(object dbContext)
        {
            var database = GetDatabaseFacade(dbContext);
            return database.GetType().GetProperty("ProviderName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(database) as string;
        }

        public void Dispose()
        {
            if (this._dbContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
            this._loadContext.Unload();
        }

        private sealed class ProjectAssemblyLoadContext : AssemblyLoadContext
        {
            private readonly AssemblyDependencyResolver _resolver;

            public ProjectAssemblyLoadContext(string mainAssemblyPath) : base(isCollectible: true)
            {
                this._resolver = new AssemblyDependencyResolver(mainAssemblyPath);
            }

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                var assemblyPath = this._resolver.ResolveAssemblyToPath(assemblyName);
                return assemblyPath == null ? null : this.LoadFromAssemblyPath(assemblyPath);
            }
        }
    }
}
