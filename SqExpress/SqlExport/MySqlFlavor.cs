namespace SqExpress.SqlExport
{
    /// <summary>
    /// Identifies the MySQL-compatible dialect rendered by <see cref="MySqlExporter"/>.
    /// </summary>
    public enum MySqlFlavor
    {
        /// <summary>
        /// Render SQL using MariaDB-compatible syntax.
        /// </summary>
        MariaDb,

        /// <summary>
        /// Render SQL using Oracle MySQL-compatible syntax.
        /// </summary>
        Oracle
    }
}
