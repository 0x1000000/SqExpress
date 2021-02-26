using CommandLine;

namespace SqExpress.CodeGenUtil
{
    [Verb("gentabdesc", HelpText = "Generate table descriptor classes.")]
    public class GenTabDescOptions
    {
        public GenTabDescOptions(ConnectionType connectionType, string connectionString)
        {
            this.ConnectionType = connectionType;
            this.ConnectionString = connectionString;
        }

        [Value(1, MetaName = "CONNECTION_TYPE", Required = true, HelpText = "Connection Type: \"mssql\" or \"mysql\" or \"pgsql\".")]
        public ConnectionType ConnectionType { get; }

        [Value(2, MetaName = "CONNECTION_STRING", Required = true, HelpText = "Database connection string.")]
        public string ConnectionString { get; }
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
}