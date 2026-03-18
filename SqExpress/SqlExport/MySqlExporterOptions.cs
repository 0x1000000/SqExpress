using System;

namespace SqExpress.SqlExport
{
    public sealed class MySqlExporterOptions
    {
        public static readonly MySqlExporterOptions MariaDbDefault =
            new MySqlExporterOptions(SqlBuilderOptions.Default, MySqlFlavor.MariaDb);

        public static readonly MySqlExporterOptions OracleDefault =
            new MySqlExporterOptions(SqlBuilderOptions.Default, MySqlFlavor.Oracle);

        public MySqlExporterOptions(SqlBuilderOptions builderOptions, MySqlFlavor flavor)
        {
            this.BuilderOptions = builderOptions ?? throw new ArgumentNullException(nameof(builderOptions));
            this.Flavor = flavor;
        }

        public SqlBuilderOptions BuilderOptions { get; }

        public MySqlFlavor Flavor { get; }

        public MySqlExporterOptions WithBuilderOptions(SqlBuilderOptions builderOptions)
            => new MySqlExporterOptions(builderOptions ?? throw new ArgumentNullException(nameof(builderOptions)), this.Flavor);

        public MySqlExporterOptions WithFlavor(MySqlFlavor flavor)
            => new MySqlExporterOptions(this.BuilderOptions, flavor);
    }
}
