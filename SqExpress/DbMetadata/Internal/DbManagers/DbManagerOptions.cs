namespace SqExpress.DbMetadata.Internal.DbManagers;

internal class DbManagerOptions
{
    public string TableClassPrefix { get; }

    public DbManagerOptions(string tableClassPrefix)
    {
        TableClassPrefix = tableClassPrefix;
    }
}