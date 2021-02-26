using System;
using System.Threading.Tasks;
using CommandLine;
using SqExpress.CodeGenUtil.DbManagers;

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
            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                throw new SqExpressCodeGenException("Connection string cannot be empty");
            }

            //using var sqlManager = MySqlDbManager.Create("server=127.0.0.1;uid=test;pwd=test;database=test");
            //using var sqlManager = PgSqlDbManager.Create("Host=localhost;Port=5432;Username=postgres;Password=test;Database=test");
            var sqlManager = CreateDbManager(options);


            var connectionTest = await sqlManager.TryOpenConnection();
            if (!string.IsNullOrEmpty(connectionTest))
            {
                throw new SqExpressCodeGenException(connectionTest);
            }


            var tables = await sqlManager.SelectTables();

            foreach (var table in tables)
            {
                if (!string.IsNullOrEmpty(table.NameModel.Schema))
                {
                    Console.WriteLine(table.NameModel.Schema + "." + table.NameModel.Name);
                }
                else
                {
                    Console.WriteLine(table.NameModel.Name);
                }

                foreach (var column in table.Columns)
                {
                    Console.Write("--");
                    Console.Write(column.Name);
                    Console.Write(": ");
                    Console.Write(column.SqlType);
                    Console.WriteLine();
                }
            }
        }

        private static DbManager CreateDbManager(GenTabDescOptions options)
        {
            switch (options.ConnectionType)
            {
                case ConnectionType.MsSql:
                    return MsSqlDbManager.Create(options.ConnectionString);
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
