using System;

namespace SqExpress.SqlExport
{
    /// <summary>
    /// Configures <see cref="MySqlExporter"/>, including its MySQL-compatible dialect.
    /// </summary>
    public sealed class MySqlExporterOptions
    {
        /// <summary>
        /// Gets the shared default configuration for MariaDB.
        /// </summary>
        public static readonly MySqlExporterOptions MariaDbDefault =
            new MySqlExporterOptions(SqlBuilderOptions.Default, MySqlFlavor.MariaDb);

        /// <summary>
        /// Gets the shared default configuration for Oracle MySQL.
        /// </summary>
        public static readonly MySqlExporterOptions OracleDefault =
            new MySqlExporterOptions(SqlBuilderOptions.Default, MySqlFlavor.Oracle);

        /// <summary>
        /// Creates MySQL exporter options.
        /// </summary>
        /// <param name="builderOptions">Common SQL rendering options.</param>
        /// <param name="flavor">The MySQL-compatible dialect to render.</param>
        /// <exception cref="ArgumentNullException"><paramref name="builderOptions"/> is <see langword="null"/>.</exception>
        public MySqlExporterOptions(SqlBuilderOptions builderOptions, MySqlFlavor flavor)
        {
            this.BuilderOptions = builderOptions ?? throw new ArgumentNullException(nameof(builderOptions));
            this.Flavor = flavor;
        }

        /// <summary>
        /// Gets the common SQL rendering options.
        /// </summary>
        public SqlBuilderOptions BuilderOptions { get; }

        /// <summary>
        /// Gets the MySQL-compatible dialect to render.
        /// </summary>
        public MySqlFlavor Flavor { get; }

        /// <summary>
        /// Returns a copy using the specified common rendering options.
        /// </summary>
        /// <param name="builderOptions">The replacement rendering options.</param>
        /// <returns>A new options instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="builderOptions"/> is <see langword="null"/>.</exception>
        public MySqlExporterOptions WithBuilderOptions(SqlBuilderOptions builderOptions)
            => new MySqlExporterOptions(builderOptions ?? throw new ArgumentNullException(nameof(builderOptions)), this.Flavor);

        /// <summary>
        /// Returns a copy targeting the specified MySQL-compatible dialect.
        /// </summary>
        /// <param name="flavor">The replacement dialect.</param>
        /// <returns>A new options instance.</returns>
        public MySqlExporterOptions WithFlavor(MySqlFlavor flavor)
            => new MySqlExporterOptions(this.BuilderOptions, flavor);
    }
}
