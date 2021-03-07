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
using SqExpress.CodeGenUtil.Model;

namespace SqExpress.CodeGenUtil
{
    public class Program
    {
        public static void Main2(string[] args)
        {

            Console.WriteLine(SyntaxFactory.ClassDeclaration("TableWsusers").ToFullString());

            var syntaxFile = CSharpSyntaxTree.ParseText("class TableWsusers{T[] Build()=> new T[]{C.A(),C.B()};}").GetRoot();
            WalkSyntaxNodeOrToken(syntaxFile, 0);
            static void WalkSyntaxNodeOrToken(SyntaxNodeOrToken node, int deep)
            {
                //if (!node.IsToken)
                {
                    string? typeName = "";
                    Console.Write(new string(' ', deep * 2));
                    Console.Write(node.Kind());
                    if (node.IsToken)
                    {
                        Console.Write(" (token)");
                        typeName = node.AsToken().GetType().Name;
                    }

                    if (node.IsNode)
                    {
                        Console.Write(" (node)");
                        typeName = node.AsNode()?.GetType().Name;
                    }



                    Console.WriteLine($" {typeName}");
                }

                foreach (var syntaxNode in node.ChildNodesAndTokens())
                {
                    WalkSyntaxNodeOrToken(syntaxNode, deep + 1);
                }
            }
        }

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
                Console.WriteLine("Command line parser exception: ");
                Console.WriteLine(e);
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
                Console.WriteLine(sqExpressCodeGenException.Message);
                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unhandled Exception: ");
                Console.WriteLine(e);
                return 1;
            }
        }

        public static async Task RunGenTabDescOptions(GenTabDescOptions options)
        {
            string directory = options.OutputDir;
            if (string.IsNullOrEmpty(directory))
            {
                directory = Directory.GetCurrentDirectory();
            }
            else if (!Path.IsPathFullyQualified(directory))
            {
                directory = Path.GetFullPath(directory, Directory.GetCurrentDirectory());
            }

            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
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

            IReadOnlyDictionary<TableRef, ClassDeclarationSyntax> existingCode = ExistingCodeDiscoverer.Discover(directory);

            var sqlManager = CreateDbManager(options);


            var connectionTest = await sqlManager.TryOpenConnection();
            if (!string.IsNullOrEmpty(connectionTest))
            {
                throw new SqExpressCodeGenException(connectionTest);
            }

            var tables = await sqlManager.SelectTables();

            IReadOnlyDictionary<TableRef, TableModel> tableMap = tables.ToDictionary(t => t.DbName);

            var tableClassGenerator = new TableClassGenerator(tableMap, options.Namespace, existingCode);

            foreach (var table in tables)
            {
                string filePath = Path.Combine(directory, $"{table.Name}.cs");
                await File.WriteAllTextAsync(filePath, tableClassGenerator.Generate(table).ToFullString());
            }

            var allTablePath = Path.Combine(directory, "AllTables.cs");

            await File.WriteAllTextAsync(allTablePath, TableListClassGenerator.Generate(tables, options.Namespace, options.TableClassPrefix).ToFullString());

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
