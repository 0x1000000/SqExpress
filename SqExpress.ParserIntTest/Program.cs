using System.Text;
using Microsoft.Data.SqlClient;
using SqExpress.SqlParser;
using SqExpress.Syntax;

namespace SqExpress.ParserIntTest
{
    internal static class Program
    {
        private const string MsSqlConnectionString = "Data Source=(local);Initial Catalog=TestDatabase;Integrated Security=True;TrustServerCertificate=True";
        private static readonly string DefaultArtifactPath =
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "parser-mismatches.txt"));

        static async Task<int> Main(string[] args)
        {
            await using var connection = new SqlConnection(MsSqlConnectionString);
            try
            {
                await connection.OpenAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine($"DB-CONNECTION-FAILED: {e}");
                return 1;
            }

            var command = ParseCommand(args);
            return command.Mode switch
            {
                CommandMode.Single => await RunSingleAsync(connection, command),
                _ => await RunInteractiveAsync(connection, command.ArtifactPath)
            };
        }

        private static async Task<int> RunInteractiveAsync(SqlConnection connection, string artifactPath)
        {
            Console.WriteLine("Interactive mode. Type SQL statement or /exit.");
            Console.WriteLine("Use --session for the first 20-case dry run or --sql/--expect for batch mode.");

            while (true)
            {
                Console.WriteLine("------------");
                Console.WriteLine("Type SQL statement:");
                var sql = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(sql))
                {
                    continue;
                }

                if (string.Equals(sql, "\\exit", StringComparison.InvariantCultureIgnoreCase)
                    || string.Equals(sql, "/exit", StringComparison.InvariantCultureIgnoreCase))
                {
                    return 0;
                }

                bool isValidExpected = AskIsValid();
                var testCase = new SqlCase("interactive", isValidExpected, sql.Trim(), "interactive");
                var result = await EvaluateCaseAsync(connection, testCase);
                PrintCaseResult(result);

                if (ShouldAppendToArtifact(result))
                {
                    await AppendArtifactAsync(artifactPath, result);
                }
            }
        }

        private static async Task<int> RunSingleAsync(SqlConnection connection, Command command)
        {
            var testCase = new SqlCase(
                command.Label ?? "single",
                command.ExpectValid!.Value,
                command.Sql!,
                command.Label ?? "single");

            var result = await EvaluateCaseAsync(connection, testCase);
            PrintCaseResult(result);

            if (ShouldAppendToArtifact(result))
            {
                await AppendArtifactAsync(command.ArtifactPath, result);
            }

            return result.Verdict is Verdict.AgreementValid or Verdict.AgreementInvalid ? 0 : 2;
        }

        private static async Task<CaseResult> EvaluateCaseAsync(SqlConnection connection, SqlCase testCase)
        {
            var sqError = CheckWithSqExpress(testCase.Sql);
            var dbError = await CheckWithDatabase(testCase.Sql, connection);
            bool sqValid = string.IsNullOrEmpty(sqError);
            bool dbValid = string.IsNullOrEmpty(dbError);

            var verdict = ResolveVerdict(testCase.ExpectValid, sqValid, dbValid);

            return new CaseResult(
                testCase,
                verdict,
                sqError,
                dbError);
        }

        private static Verdict ResolveVerdict(bool expectValid, bool sqValid, bool dbValid)
        {
            if (sqValid == dbValid)
            {
                if (expectValid == sqValid)
                {
                    return sqValid ? Verdict.AgreementValid : Verdict.AgreementInvalid;
                }

                return Verdict.ExpectationWrong;
            }

            return Verdict.Mismatch;
        }

        private static void PrintCaseResult(CaseResult result)
        {
            Console.WriteLine("------------");
            Console.WriteLine($"CASE id={result.TestCase.Id} source={result.TestCase.Source} expect={(result.TestCase.ExpectValid ? "valid" : "invalid")} verdict={FormatVerdict(result.Verdict)}");
            Console.WriteLine($"SQL: {result.TestCase.Sql}");
            Console.WriteLine($"SqExpress: {FormatEngineResult(result.SqError)}");
            Console.WriteLine($"MSSQL: {FormatEngineResult(result.DbError)}");
        }

        private static string FormatVerdict(Verdict verdict)
            => verdict switch
            {
                Verdict.AgreementValid => "agreement-valid",
                Verdict.AgreementInvalid => "agreement-invalid",
                Verdict.Mismatch => "mismatch",
                Verdict.ExpectationWrong => "expectation-wrong",
                _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, null)
            };

        private static string FormatEngineResult(string? error)
            => string.IsNullOrEmpty(error) ? "valid" : error.Replace(Environment.NewLine, " | ");

        private static bool ShouldAppendToArtifact(CaseResult result)
            => result.Verdict is Verdict.Mismatch or Verdict.ExpectationWrong;

        private static async Task AppendArtifactAsync(string artifactPath, CaseResult result)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(artifactPath)!);

            var lines = new List<string>
            {
                "============",
                $"Timestamp: {DateTimeOffset.Now:O}",
                $"Id: {result.TestCase.Id}",
                $"Source: {result.TestCase.Source}",
                $"Expected: {(result.TestCase.ExpectValid ? "valid" : "invalid")}",
                $"Verdict: {FormatVerdict(result.Verdict)}",
                $"SqExpress: {FormatEngineResult(result.SqError)}",
                $"MSSQL: {FormatEngineResult(result.DbError)}",
                "SQL:",
                result.TestCase.Sql,
                string.Empty
            };

            await File.AppendAllLinesAsync(artifactPath, lines, Encoding.UTF8);
        }

        private static string? CheckWithSqExpress(string sql)
        {
            return SqTSqlParser.TryParse(sql, out IExpr? _, out var error)
                ? null
                : error;
        }

        private static async Task<string?> CheckWithDatabase(string sql, SqlConnection connection)
        {
            string? executionError = null;
            string? cleanupError = null;

            try
            {
                await using (var commandOn = new SqlCommand("SET PARSEONLY ON", connection))
                {
                    await commandOn.ExecuteNonQueryAsync();
                }

                await using (var validateCommand = new SqlCommand(sql, connection))
                {
                    await using var reader = await validateCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                    }
                }

            }
            catch (Exception exception) when (FindSqlException(exception) is { } e)
            {
                executionError = e.Message;
            }
            finally
            {
                try
                {
                    await using var commandOff = new SqlCommand("SET PARSEONLY OFF", connection);
                    await commandOff.ExecuteNonQueryAsync();
                }
                catch (Exception exception) when (FindSqlException(exception) is { } e)
                {
                    cleanupError = e.Message;
                }
            }

            return executionError ?? cleanupError;

            static SqlException? FindSqlException(Exception exception)
            {
                if (exception is SqlException sqlException)
                {
                    return sqlException;
                }

                return exception.InnerException == null
                    ? null
                    : FindSqlException(exception.InnerException);
            }
        }

        private static bool AskIsValid()
        {
            while (true)
            {
                Console.WriteLine("Is it valid SQL? (Y/n)");
                var response = Console.ReadLine();

                if (string.Equals(response?.Trim(), "Y", StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(response?.Trim(), "n", StringComparison.InvariantCultureIgnoreCase))
                {
                    return false;
                }
            }
        }

        private static Command ParseCommand(string[] args)
        {
            string? sql = null;
            bool? expectValid = null;
            string? label = null;
            string artifactPath = DefaultArtifactPath;
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--sql":
                        sql = RequireNextValue(args, ref i, "--sql");
                        break;
                    case "--expect":
                        expectValid = ParseExpectation(RequireNextValue(args, ref i, "--expect"));
                        break;
                    case "--label":
                        label = RequireNextValue(args, ref i, "--label");
                        break;
                    case "--artifact":
                        artifactPath = ResolveArtifactPath(RequireNextValue(args, ref i, "--artifact"));
                        break;
                    case "--help":
                    case "-h":
                        PrintUsage();
                        Environment.Exit(0);
                        break;
                    default:
                        throw new ArgumentException($"Unknown argument: {args[i]}");
                }
            }

            if (!string.IsNullOrWhiteSpace(sql) || expectValid.HasValue)
            {
                if (string.IsNullOrWhiteSpace(sql) || !expectValid.HasValue)
                {
                    throw new ArgumentException("Batch mode requires both --sql and --expect valid|invalid.");
                }

                return new Command(CommandMode.Single, sql.Trim(), expectValid, label, artifactPath);
            }

            return new Command(CommandMode.Interactive, null, null, null, artifactPath);
        }

        private static string ResolveArtifactPath(string path)
            => Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path));

        private static string RequireNextValue(string[] args, ref int i, string argumentName)
        {
            if (i + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value after {argumentName}.");
            }

            i++;
            return args[i];
        }

        private static bool ParseExpectation(string value)
            => value.ToLowerInvariant() switch
            {
                "valid" => true,
                "invalid" => false,
                _ => throw new ArgumentException($"Unknown expectation value '{value}'. Use valid or invalid.")
            };

        private static void PrintUsage()
        {
            Console.WriteLine("SqExpress.ParserIntTest usage:");
            Console.WriteLine("  --sql \"SELECT 1\" --expect valid|invalid [--label name] [--artifact path]");
            Console.WriteLine("Without arguments the tool runs in interactive mode.");
        }

        private enum CommandMode
        {
            Interactive,
            Single
        }

        private enum Verdict
        {
            AgreementValid,
            AgreementInvalid,
            Mismatch,
            ExpectationWrong
        }

        private sealed record Command(CommandMode Mode, string? Sql, bool? ExpectValid, string? Label, string ArtifactPath);
        private sealed record SqlCase(string Id, bool ExpectValid, string Sql, string Source);
        private sealed record CaseResult(SqlCase TestCase, Verdict Verdict, string? SqError, string? DbError);
    }
}
