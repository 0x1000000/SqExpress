using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using MySqlConnector;
using Npgsql;
using SqExpress.CodeGen.Shared;
using SqExpress.CodeGenUtil.Ef;
using SqExpress.CodeGenUtil.Logger;
using SqExpress.DbMetadata.Internal.DbManagers;
using SqExpress.DbMetadata.Internal.DbManagers.MsSql;
using SqExpress.DbMetadata.Internal.DbManagers.MySql;
using SqExpress.DbMetadata.Internal.DbManagers.PgSql;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.CodeGenUtil
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var parser = new Parser(with =>
                {
                    with.CaseInsensitiveEnumValues = true;
                    with.CaseSensitive = false;
                    with.AutoHelp = true;
                    with.AutoVersion = true;
                    with.HelpWriter = Console.Error;
                });

                return parser.ParseArguments<GenTablesOptions, GenModelsOptions>(args)
                    .MapResult(
                        (GenTablesOptions opts) => Run(opts, RunGenTablesOptions, GetOperationName(opts)),
                        (GenModelsOptions opts) => Run(opts, RunGenModelsOptions, "model generation"),
                        errs => 1);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Command line parser exception: ");
                Console.Error.WriteLine(e);
                return 1;
            }
        }

        private static int Run<TOpts>(TOpts opts, Func<TOpts,Task> task, string operationName)
        {
            try
            {
                task(opts).GetAwaiter().GetResult();
                return 0;
            }
            catch (SqExpressCodeGenException e)
            {
                Console.Error.WriteLine($"SqExpress {operationName} failed: {e.Message}");
                return 1;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unhandled Exception: ");
                Console.Error.WriteLine(e);
                return 1;
            }
        }

        private static string GetOperationName(GenTablesOptions options)
            => options.ConnectionType == ConnectionType.Ef
                ? "EF table generation"
                : $"{options.ConnectionType} table generation";

        public static async Task RunGenTablesOptions(GenTablesOptions options)
        {
            ILogger logger = new DefaultLogger(Console.Out, options.Verbosity);

            logger.LogMinimal("Table proxy classes generation is running...");

            string directory = EnsureDirectory(options.OutputDir, logger, "Output", true);

            if (string.IsNullOrWhiteSpace(options.Source))
            {
                throw new SqExpressCodeGenException(
                    options.ConnectionType == ConnectionType.Ef
                        ? "EF project path cannot be empty"
                        : "Connection string cannot be empty");
            }
            var existingCode = (IReadOnlyDictionary<TableRef, Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>)
                new Dictionary<TableRef, Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>();
            if (!options.UseTableDeclarationAttributes)
            {
                logger.LogNormal("Checking existing code...");
                existingCode = CodeGenLegacySqModelSupport.FindTableDescriptors(directory, DefaultFileSystem.Instance);
                if(logger.IsNormalOrHigher) logger.LogNormal(existingCode.Count > 0
                    ? $"Found {existingCode.Count} already existing table descriptor classes."
                    : "No table descriptor classes found.");
            }

            IReadOnlyList<TableModel> tables;
            if (options.ConnectionType == ConnectionType.Ef)
            {
                logger.LogNormal("Reading EF model metadata...");
                var efProjectPath = ValidateEfProjectPath(options.Source);
                var metadata = await EfMetadataExtractorRunner.Extract(
                    efProjectPath,
                    options.DbContext,
                    string.IsNullOrWhiteSpace(options.Framework) ? null : options.Framework);
                tables = EfMetadataTableReader.SelectTables(metadata, options.TableClassPrefix, options.SkipUnknownColumnTypes);
            }
            else
            {
                using var sqlManager = CreateDbManager(options);

                logger.LogNormal("Connecting to database...");

                var connectionTest = await sqlManager.TryOpenConnection();
                if (!string.IsNullOrEmpty(connectionTest))
                {
                    throw new SqExpressCodeGenException(connectionTest);
                }

                logger.LogNormal("Success!");

                tables = await sqlManager.SelectTables(options.SkipUnknownColumnTypes);
            }

            if(logger.IsNormalOrHigher)
            {
                logger.LogNormal(tables.Count > 0
                    ? $"Found {tables.Count} tables."
                    : "No tables found in the database.");

                if (logger.IsDetailed)
                {
                    foreach (var tableModel in tables)
                    {
                        Console.WriteLine($"{tableModel.DbName} ({tableModel.Name})");
                        foreach (var tableModelColumn in tableModel.Columns)
                        {
                            Console.WriteLine($"- {tableModelColumn.DbName.Name} {tableModelColumn.ColumnType.GetType().Name}{(tableModelColumn.Pk.HasValue ? " (PK)":null)}{(tableModelColumn.Fk != null ? $" (FK: {string.Join(';', tableModelColumn.Fk.Select(f=>f.ToString()))})" : null)}");
                        }
                    }
                }
            }

            logger.LogNormal("Code generation...");
            IReadOnlyDictionary<TableRef, TableModel> tableMap = tables.ToDictionary(t => t.DbName);
            var layout = TableGenerationLayout.Create(
                tables,
                directory,
                options.Namespace,
                options.SplitTablesBySchema);
            var tableNamespaces = layout.Entries.ToDictionary(static pair => pair.Key, static pair => pair.Value.Namespace);

            foreach (var table in tables)
            {
                var layoutEntry = layout.Entries[table.DbName];
                string filePath = layoutEntry.FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                if(logger.IsDetailed) logger.LogDetailed($"{table.DbName} to \"{filePath}\".");

                string text;
                bool existing;
                if (options.UseTableDeclarationAttributes)
                {
                    text = CodeGenTableDescriptorSupport.GenerateTableDeclaration(
                        table,
                        tableMap,
                        layoutEntry.Namespace,
                        options.SkipUnknownColumnTypes,
                        filePath,
                        DefaultFileSystem.Instance,
                        out existing,
                        tableNamespaces).ToFullString();
                }
                else
                {
                    text = CodeGenTableDescriptorSupport.GenerateTableDescriptor(
                        table,
                        tableMap,
                        layoutEntry.Namespace,
                        existingCode,
                        options.SkipUnknownColumnTypes,
                        out existing,
                        tableNamespaces).ToFullString();
                }
                await WriteAllTextIfChangedAsync(filePath, text);

                if (logger.IsDetailed) logger.LogDetailed(existing ? "Existing file updated." : "New file created.");
            }

            var allTablePath = Path.Combine(directory, "AllTables.cs");

            if (logger.IsDetailed) logger.LogDetailed($"AllTables to \"{allTablePath}\".");

            await WriteAllTextIfChangedAsync(
                allTablePath,
                CodeGenAllTablesSupport.Generate(
                    allTablePath,
                    tables,
                    options.Namespace,
                    options.TableClassPrefix,
                    DefaultFileSystem.Instance,
                    options.SplitTablesBySchema ? tableNamespaces : null,
                    options.SplitTablesBySchema
                        ? layout.Entries.ToDictionary(static pair => pair.Key, static pair => pair.Value.SchemaSegment!)
                        : null).ToFullString());

            if (options.CleanOutput)
            {
                var removedFiles = TableOutputCleaner.Clean(directory, layout.Entries);
                foreach (var removedFile in removedFiles)
                {
                    logger.LogNormal($"Removed obsolete table descriptor file \"{removedFile}\".");
                }
            }

            logger.LogMinimal("Table proxy classes generation successfully completed!");
        }

        private static async Task RunGenModelsOptions(GenModelsOptions options)
        {
            ILogger logger = new DefaultLogger(Console.Out, options.Verbosity);

            logger.LogMinimal("Model classes generation is running...");

            string inDirectory = EnsureDirectory(options.InputDir, logger, "Input", false);
            string outDirectory = EnsureDirectory(options.OutputDir, logger, "Output", true);

            var analysis = CodeGenLegacySqModelSupport.AnalyzeLegacySqModels(inDirectory, DefaultFileSystem.Instance, options.NullRefTypes);

            if (analysis.Count < 1)
            {
                logger.LogNormal("No model attributes detected in the input directory.");
            }
            else
            {
                logger.LogNormal($"Found {analysis.Count} models in the input directory.");
            }

            if (logger.IsDetailed)
            {
                foreach (var model in analysis)
                {
                    logger.LogDetailed(model.Name);
                    foreach (var property in model.Properties)
                    {
                        logger.LogDetailed(
                            $" -{property.Type} {property.Name}");
                        foreach (var col in property.Column)
                        {
                            logger.LogDetailed(
                                $"   ={(property.CastType != null ? $"({property.CastType})" : null)}{col.TableRef.TableTypeName}.{col.ColumnName}");
                        }
                    }
                }
            }

            logger.LogNormal("Code generation...");

            foreach (var meta in analysis)
            {
                string path = Path.Combine(outDirectory, $"{meta.Name}.cs");
                if (logger.IsDetailed) logger.LogDetailed(path);
                await WriteAllTextIfChangedAsync(
                    path,
                    CodeGenModelSupport.Generate(
                        meta,
                        options.Namespace,
                        path,
                        options.RwClasses,
                        options.NullRefTypes,
                        options.ModelType == ModelType.Record ? CodeGenModelType.Record : CodeGenModelType.ImmutableClass,
                        DefaultFileSystem.Instance,
                        out var existing).ToFullString());
                if (logger.IsDetailed) logger.LogDetailed(existing ? "Existing file updated." : "New file created.");
            }

            if (options.CleanOutput)
            {
                var modelFiles = analysis.Select(meta => $"{meta.Name}.cs").ToHashSet(StringComparer.InvariantCultureIgnoreCase);

                var toRemove = Directory.EnumerateFiles(outDirectory).Where(p=> !modelFiles.Contains(Path.GetFileName(p))).ToList();

                foreach (var delPath in toRemove)
                {
                    File.Delete(delPath);
                    if(logger.IsNormalOrHigher) logger.LogNormal($"File {Path.GetFileName(delPath)} has been removed since it does not contain any model class");
                }

            }


            logger.LogMinimal("Model classes generation successfully completed!");
        }

        private static DbManager CreateDbManager(GenTablesOptions options)
        {
            DbConnection connection;
            switch (options.ConnectionType)
            {
                case ConnectionType.MsSql:
                    try
                    {
                        connection = new SqlConnection(options.Source);
                    }
                    catch (ArgumentException e)
                    {
                        throw new SqExpressCodeGenException($"MsSQL connection string has incorrect format \"{options.Source}\"", e);
                    }

                    if (string.IsNullOrEmpty(connection.Database))
                    {
                        throw new SqExpressCodeGenException("MsSQL connection string has to contain \"database\" attribute");
                    }
                    return MsSqlDbStrategy.Create(new DbManagerOptions(options.TableClassPrefix), connection);
                case ConnectionType.MySql:
                    try
                    {
                        connection = new MySqlConnection(options.Source);
                    }
                    catch (ArgumentException e)
                    {
                        throw new SqExpressCodeGenException($"MySQL connection string has incorrect format \"{options.Source}\"", e);
                    }

                    if (string.IsNullOrEmpty(connection.Database))
                    {
                        throw new SqExpressCodeGenException("MySQL connection string has to contain \"database\" attribute");
                    }
                    return MySqlDbStrategy.Create(new DbManagerOptions(options.TableClassPrefix), connection);
                case ConnectionType.PgSql:
                    try
                    {
                        connection = new NpgsqlConnection(options.Source);
                    }
                    catch (ArgumentException e)
                    {
                        throw new SqExpressCodeGenException($"PgSQL connection string has incorrect format \"{options.Source}\"", e);
                    }

                    if (string.IsNullOrEmpty(connection.Database))
                    {
                        throw new SqExpressCodeGenException("PgSQL connection string has to contain \"database\" attribute");
                    }
                    return PgSqlDbStrategy.Create(new DbManagerOptions(options.TableClassPrefix), connection);
                default:
                    throw new SqExpressCodeGenException("Unknown connection type: " + options.ConnectionType);
            }
        }

        private static string ValidateEfProjectPath(string source)
        {
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(source, Directory.GetCurrentDirectory());
            }
            catch (Exception e) when (e is ArgumentException || e is NotSupportedException || e is PathTooLongException)
            {
                throw new SqExpressCodeGenException($"EF project path \"{source}\" has invalid format.", e);
            }

            if (!string.Equals(Path.GetExtension(fullPath), ".csproj", StringComparison.OrdinalIgnoreCase))
            {
                throw new SqExpressCodeGenException(
                    $"EF table generation expects a .csproj path. Received \"{source}\". Pass the EF project file so SqExpress can run metadata extraction in the project's target framework.");
            }

            if (!File.Exists(fullPath))
            {
                throw new SqExpressCodeGenException($"Could not find EF project \"{source}\".");
            }

            return fullPath;
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

            await File.WriteAllTextAsync(path, text);
        }

        private static string EnsureDirectory(string directory, ILogger logger, string dirAlias, bool create)
        {
            if (string.IsNullOrEmpty(directory))
            {
                directory = Directory.GetCurrentDirectory();
                logger.LogDetailed(
                    $"{dirAlias} directory was not specified, so the current directory \"{directory}\" is used as an output one.");
            }
            else if (!Path.IsPathFullyQualified(directory))
            {
                directory = Path.GetFullPath(directory, Directory.GetCurrentDirectory());
                logger.LogDetailed($"{dirAlias} directory is converted to fully qualified \"{directory}\".");
            }


            if (!Directory.Exists(directory))
            {
                if (create)
                {
                    try
                    {
                        Directory.CreateDirectory(directory);
                        logger.LogDetailed($"Directory \"{directory}\" was created.");
                    }
                    catch (Exception e)
                    {
                        throw new SqExpressCodeGenException($"Could not create directory: \"{directory}\".", e);
                    }
                }
                else
                {
                    throw new SqExpressCodeGenException($"\"{directory}\" directory does not exist.");
                }
            }

            return directory;
        }
    }
}
