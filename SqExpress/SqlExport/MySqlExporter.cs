using System;
using SqExpress.SqlExport.Internal;
using SqExpress.SqlExport.Statement.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;
using System.Collections.Generic;

namespace SqExpress.SqlExport
{
    /// <summary>
    /// Renders SqExpress expression trees and statements using a selected MySQL-compatible dialect.
    /// </summary>
    public class MySqlExporter : ISqlExporterInternal
    {
        /// <summary>Gets an exporter configured for MariaDB.</summary>
        public static readonly MySqlExporter MariaDbDefault = new MySqlExporter(MySqlExporterOptions.MariaDbDefault);

        /// <summary>Gets an exporter configured for Oracle MySQL.</summary>
        public static readonly MySqlExporter OracleDefault = new MySqlExporter(MySqlExporterOptions.OracleDefault);

        /// <summary>Gets the legacy default exporter, which targets MariaDB.</summary>
        [System.Obsolete("Use MySqlExporter.MariaDbDefault or MySqlExporter.OracleDefault explicitly.")]
        public static readonly MySqlExporter Default = MariaDbDefault;

        private readonly SqlBuilderOptions _builderOptions;

        /// <summary>Gets the MySQL-compatible dialect rendered by this exporter.</summary>
        public MySqlFlavor Flavor { get; }

        /// <summary>Creates an exporter from complete MySQL-specific options.</summary>
        /// <param name="options">The exporter configuration.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
        public MySqlExporter(MySqlExporterOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this._builderOptions = options.BuilderOptions;
            this.Flavor = options.Flavor;
        }

        /// <summary>Creates a MariaDB exporter with the specified common rendering options.</summary>
        /// <param name="builderOptions">Options controlling schema mapping and identifier quoting.</param>
        public MySqlExporter(SqlBuilderOptions builderOptions)
            : this(new MySqlExporterOptions(builderOptions, MySqlFlavor.MariaDb))
        {
        }

        /// <summary>Creates an exporter for the specified MySQL-compatible dialect.</summary>
        /// <param name="builderOptions">Options controlling schema mapping and identifier quoting.</param>
        /// <param name="flavor">The dialect to render.</param>
        public MySqlExporter(SqlBuilderOptions builderOptions, MySqlFlavor flavor)
            : this(new MySqlExporterOptions(builderOptions, flavor))
        {
        }

        /// <inheritdoc/>
        public string ToSql(IExpr expr)
        {
            return ((ISqlExporterInternal)this).ToSql(expr, out _);
        }

        /// <inheritdoc/>
        public string ToSql(IStatement statement)
        {
            var builder = new MySqlStatementBuilder(this._builderOptions, this.Flavor, null);
            statement.Accept(builder);
            return builder.Build();
        }

        string ISqlExporterInternal.ToSql(IExpr expr, out IReadOnlyList<DbParameterValue>? parameters)
        {
            var sqlExporter = new MySqlBuilder(this._builderOptions, this.Flavor);
            if (expr.Accept(sqlExporter, null))
            {
                var sql = sqlExporter.ToString();
                parameters = sqlExporter.ParameterValues;
                return sql;
            }

            throw new SqExpressException("Could not build Sql");
        }

        int ISqlExporterInternal.ParametersLimit => 65535;
    }
}
