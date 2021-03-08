using CommandLine;

namespace SqExpress.CodeGenUtil
{
    [Verb("gentables", HelpText = "Generate table descriptor classes.")]
    public class GenTabDescOptions
    {
        public GenTabDescOptions(ConnectionType connectionType, string connectionString, string tableClassPrefix, string outputDir, string @namespace, Verbosity verbosity)
        {
            this.ConnectionType = connectionType;
            this.ConnectionString = connectionString;
            this.TableClassPrefix = tableClassPrefix;
            this.OutputDir = outputDir;
            this.Namespace = @namespace;
            this.Verbosity = verbosity;
        }

        [Value(1, MetaName = "CONNECTION_TYPE", Required = true, HelpText = "Connection Type: \"mssql\" or \"mysql\" or \"pgsql\".")]
        public ConnectionType ConnectionType { get; }

        [Value(2, MetaName = "CONNECTION_STRING", Required = true, HelpText = "Database connection string.")]
        public string ConnectionString { get; }

        [Option("table-class-prefix", Required = false, Default = "Table", HelpText = "Prefix for table descriptor class names.")]
        public string TableClassPrefix { get; }

        [Option('o',"output-dir", Required = false, Default = "", HelpText = "Path to a directory where cs files will be written.")]
        public string OutputDir { get; }

        [Option('n',"namespace", Required = false, Default = "MyCompany.MyApp.Tables", HelpText = "Default namespace for newly crated files.")]
        public string Namespace { get; }

        [Option('v',"verbosity", Required = false, Default = Verbosity.Minimal, HelpText = "Allowed values are quiet, minimal, normal, detailed, and diagnostic. The default is minimal")]
        public Verbosity Verbosity { get; }
    }

    [Verb("gendto", HelpText = "Generate DTO.")]
    public class GenDtoOptions
    {
    }

    public enum ConnectionType
    {
        MsSql = 1,
        MySql = 2,
        PgSql = 3
    }

    public enum Verbosity
    {
        Quite = 1,
        Minimal = 2,
        Normal = 3,
        Detailed = 4
    }
}