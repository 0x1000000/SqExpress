using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.CodeGenUtil.CodeGen;
using SqExpress.CodeGenUtil.DbManagers;
using SqExpress.CodeGenUtil.Logger;
using SqExpress.CodeGenUtil.Model;

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

                return parser.ParseArguments<GenTabDescOptions, GenDtoOptions>(args)
                    .MapResult(
                        (GenTabDescOptions opts) => Run(opts, RunGenTabDescOptions),
                        (GenDtoOptions opts) => 0,
                        errs => 1);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Command line parser exception: ");
                Console.Error.WriteLine(e.Message);
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

        public static async Task RunGenTabDescOptions(GenTabDescOptions options)
        {
            ILogger logger = new DefaultLogger(Console.Out, options.Verbosity);

            logger.LogMinimal("Table proxy classes generation is running...");

            string directory = options.OutputDir;
            if (string.IsNullOrEmpty(directory))
            {
                directory = Directory.GetCurrentDirectory();
                logger.LogDetailed($"Output di0rectory was not specified, so the current directory \"{directory}\" is used as an output one.");
            }
            else if (!Path.IsPathFullyQualified(directory))
            {
                directory = Path.GetFullPath(directory, Directory.GetCurrentDirectory());
                logger.LogDetailed($"Output directory is converted to fully qualified \"{directory}\".");
            }

            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                    logger.LogDetailed($"Directory \"${directory}\" was created.");
                }
                catch (Exception e)
                {
                    throw new SqExpressCodeGenException($"Could not create directory: \"{directory}\".", e);
                }
            }

            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                throw new SqExpressCodeGenException("Connection string cannot be empty");
            }
            logger.LogNormal("Checking existing code...");
            IReadOnlyDictionary<TableRef, ClassDeclarationSyntax> existingCode = ExistingTablesCodeDiscoverer.Discover(directory);
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

            await File.WriteAllTextAsync(allTablePath, TableListClassGenerator.Generate(allTablePath, tables, options.Namespace, options.TableClassPrefix).ToFullString());

            logger.LogMinimal("Table proxy classes generation successfully completed!");
        }

        private static DbManager CreateDbManager(GenTabDescOptions options)
        {
            switch (options.ConnectionType)
            {
                case ConnectionType.MsSql:
                    return MsSqlDbManager.Create(options);
                case ConnectionType.MySql:
                    return MySqlDbManager.Create(options.ConnectionString);
                case ConnectionType.PgSql:
                    return PgSqlDbManager.Create(options.ConnectionString);
                default:
                    throw new SqExpressCodeGenException("Unknown connection type: " + options.ConnectionType);
            }
        }
    }
}
