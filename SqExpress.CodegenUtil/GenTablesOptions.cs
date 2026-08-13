using System;
using CommandLine;

using System.Collections.Generic;
using System.Linq;

namespace SqExpress.CodeGenUtil
{
    [Verb("gentables", HelpText = "Generate table descriptor classes.")]
    public class GenTablesOptions
    {
        public GenTablesOptions(ConnectionType connectionType, string source, string tableClassPrefix, string outputDir, string @namespace, Verbosity verbosity, bool useTableDeclarationAttributes = false, bool skipUnknownColumnTypes = false, string dbContext = "", string framework = "", bool splitTablesBySchema = false, bool cleanOutput = false, IEnumerable<string>? include = null, IEnumerable<string>? exclude = null)
        {
            this.ConnectionType = connectionType;
            this.Source = source;
            this.TableClassPrefix = tableClassPrefix;
            this.OutputDir = outputDir;
            this.Namespace = @namespace;
            this.Verbosity = verbosity;
            this.UseTableDeclarationAttributes = useTableDeclarationAttributes;
            this.SkipUnknownColumnTypes = skipUnknownColumnTypes;
            this.DbContext = dbContext;
            this.Framework = framework;
            this.SplitTablesBySchema = splitTablesBySchema;
            this.CleanOutput = cleanOutput;
            this.Include = include?.ToArray() ?? Array.Empty<string>();
            this.Exclude = exclude?.ToArray() ?? Array.Empty<string>();
        }

        [Value(1, MetaName = "CONNECTION_TYPE", Required = true, HelpText = "Connection Type: \"mssql\" or \"mysql\" or \"pgsql\" or \"ef\".")]
        public ConnectionType ConnectionType { get; }

        [Value(2, MetaName = "CONNECTION_STRING_OR_EF_PROJECT", Required = false, HelpText = "Database connection string, or EF .csproj path for \"ef\".")]
        public string Source { get; }

        [Option("table-class-prefix", Required = false, Default = "Table", HelpText = "Prefix for table descriptor class names.")]
        public string TableClassPrefix { get; }

        [Option('o',"output-dir", Required = false, Default = "", HelpText = "Path to a directory where cs files will be written.")]
        public string OutputDir { get; }

        [Option('n',"namespace", Required = false, Default = "MyCompany.MyApp.Tables", HelpText = "Default namespace for newly crated files.")]
        public string Namespace { get; }

        [Option('v',"verbosity", Required = false, Default = Verbosity.Minimal, HelpText = "Allowed values are quiet, minimal, normal, detailed, and diagnostic. The default is minimal")]
        public Verbosity Verbosity { get; }

        [Option("use-table-declaration-attributes", Required = false, Default = false, HelpText = "Generate attribute-based table declaration partial classes instead of direct TableBase descriptors.")]
        public bool UseTableDeclarationAttributes { get; }

        [Option("skip-unknown-column-types", Required = false, Default = false, HelpText = "Skip unsupported database column types and generate table descriptors from the remaining supported columns.")]
        public bool SkipUnknownColumnTypes { get; }

        [Option("db-context", Required = false, Default = "", HelpText = "EF DbContext type name for \"ef\" mode when it cannot be inferred.")]
        public string DbContext { get; }

        [Option("framework", Required = false, Default = "", HelpText = "Target framework for \"ef\" mode when the EF project targets multiple frameworks.")]
        public string Framework { get; }

        [Option("split-tables-by-schema", Required = false, Default = false, HelpText = "Generate table files in schema-specific folders and namespaces.")]
        public bool SplitTablesBySchema { get; }

        [Option("clean-output", Required = false, Default = false, HelpText = "Remove obsolete table descriptors from the output directory.")]
        public bool CleanOutput { get; }

        [Option("include", Required = false, Separator = ';', HelpText = "Include tables matching a semicolon-separated list of wildcard patterns.")]
        public IEnumerable<string> Include { get; }

        [Option("exclude", Required = false, Separator = ';', HelpText = "Exclude tables matching a semicolon-separated list of wildcard patterns.")]
        public IEnumerable<string> Exclude { get; }
    }

    public enum ConnectionType
    {
        MsSql = 1,
        MySql = 2,
        PgSql = 3,
        Ef = 4
    }

    public enum Verbosity
    {
        Quiet = 1,
        Minimal = 2,
        Normal = 3,
        Detailed = 4
    }
}
