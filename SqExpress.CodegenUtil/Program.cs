﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MySqlConnector;
using Npgsql;
using SqExpress.CodeGenUtil.CodeGen;
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
                        (GenTablesOptions opts) => Run(opts, RunGenTablesOptions),
                        (GenModelsOptions opts) => Run(opts, RunGenModelsOptions),
                        errs => 1);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Command line parser exception: ");
                Console.Error.WriteLine(e);
                return 1;
            }
        }

        private static int Run<TOpts>(TOpts opts, Func<TOpts,Task> task)
        {
            try
            {
                task(opts).Wait();
                return 0;
            }
            catch (SqExpressCodeGenException e)
            {
                Console.WriteLine(e.Message);
                return 1;
            }
            catch (AggregateException e) when (e.InnerException is SqExpressCodeGenException sqExpressCodeGenException)
            {
                Console.Error.WriteLine(sqExpressCodeGenException.Message);
                return 1;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Unhandled Exception: ");
                Console.Error.WriteLine(e);
                return 1;
            }
        }

        public static async Task RunGenTablesOptions(GenTablesOptions options)
        {
            ILogger logger = new DefaultLogger(Console.Out, options.Verbosity);

            logger.LogMinimal("Table proxy classes generation is running...");

            string directory = EnsureDirectory(options.OutputDir, logger, "Output", true);

            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                throw new SqExpressCodeGenException("Connection string cannot be empty");
            }
            logger.LogNormal("Checking existing code...");
            IReadOnlyDictionary<TableRef, ClassDeclarationSyntax> existingCode = ExistingCodeExplorer.FindTableDescriptors(directory, DefaultFileSystem.Instance);
            if(logger.IsNormalOrHigher) logger.LogNormal(existingCode.Count > 0
                ? $"Found {existingCode.Count} already existing table descriptor classes."
                : "No table descriptor classes found.");

            var sqlManager = CreateDbManager(options);

            logger.LogNormal("Connecting to database...");

            var connectionTest = await sqlManager.TryOpenConnection();
            if (!string.IsNullOrEmpty(connectionTest))
            {
                throw new SqExpressCodeGenException(connectionTest);
            }

            logger.LogNormal("Success!");

            var tables = await sqlManager.SelectTables();

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

            var tableClassGenerator = new TableClassGenerator(tableMap, options.Namespace, existingCode);

            foreach (var table in tables)
            {
                string filePath = Path.Combine(directory, $"{table.Name}.cs");

                if(logger.IsDetailed) logger.LogDetailed($"{table.DbName} to \"{filePath}\".");

                var text = tableClassGenerator.Generate(table, out var existing).ToFullString();
                await File.WriteAllTextAsync(filePath, text);

                if (logger.IsDetailed) logger.LogDetailed(existing ? "Existing file updated." : "New file created.");
            }

            var allTablePath = Path.Combine(directory, "AllTables.cs");

            if (logger.IsDetailed) logger.LogDetailed($"AllTables to \"{allTablePath}\".");

            await File.WriteAllTextAsync(allTablePath, TableListClassGenerator.Generate(allTablePath, tables, options.Namespace, options.TableClassPrefix, DefaultFileSystem.Instance).ToFullString());

            logger.LogMinimal("Table proxy classes generation successfully completed!");
        }

        private static async Task RunGenModelsOptions(GenModelsOptions options)
        {
            ILogger logger = new DefaultLogger(Console.Out, options.Verbosity);

            logger.LogMinimal("Model classes generation is running...");

            string inDirectory = EnsureDirectory(options.InputDir, logger, "Input", false);
            string outDirectory = EnsureDirectory(options.OutputDir, logger, "Output", true);

            var analysis = ExistingCodeExplorer
                .EnumerateTableDescriptorsModelAttributes(inDirectory, DefaultFileSystem.Instance)
                .ParseAttribute(options.NullRefTypes)
                .CreateAnalysis();

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
                await File.WriteAllTextAsync(path, ModelClassGenerator.Generate(meta, options.Namespace, path, options.RwClasses, options.NullRefTypes, options.ModelType, DefaultFileSystem.Instance, out var existing).ToFullString());
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
                        connection = new SqlConnection(options.ConnectionString);
                    }
                    catch (ArgumentException e)
                    {
                        throw new SqExpressCodeGenException($"MsSQL connection string has incorrect format \"{options.ConnectionString}\"", e);
                    }

                    if (string.IsNullOrEmpty(connection.Database))
                    {
                        throw new SqExpressCodeGenException("MsSQL connection string has to contain \"database\" attribute");
                    }
                    return MsSqlDbStrategy.Create(new DbManagerOptions(options.TableClassPrefix), connection);
                case ConnectionType.MySql:
                    try
                    {
                        connection = new MySqlConnection(options.ConnectionString);
                    }
                    catch (ArgumentException e)
                    {
                        throw new SqExpressCodeGenException($"MySQL connection string has incorrect format \"{options.ConnectionString}\"", e);
                    }

                    if (string.IsNullOrEmpty(connection.Database))
                    {
                        throw new SqExpressCodeGenException("MySQL connection string has to contain \"database\" attribute");
                    }
                    return MySqlDbStrategy.Create(new DbManagerOptions(options.TableClassPrefix), connection);
                case ConnectionType.PgSql:
                    try
                    {
                        connection = new NpgsqlConnection(options.ConnectionString);
                    }
                    catch (ArgumentException e)
                    {
                        throw new SqExpressCodeGenException($"PgSQL connection string has incorrect format \"{options.ConnectionString}\"", e);
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
