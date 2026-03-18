using System;
using SqExpress.SqlExport.Internal;
using SqExpress.SqlExport.Statement.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;
using System.Collections.Generic;

namespace SqExpress.SqlExport
{
    public class MySqlExporter : ISqlExporterInternal
    {
        public static readonly MySqlExporter MariaDbDefault = new MySqlExporter(MySqlExporterOptions.MariaDbDefault);

        public static readonly MySqlExporter OracleDefault = new MySqlExporter(MySqlExporterOptions.OracleDefault);

        [System.Obsolete("Use MySqlExporter.MariaDbDefault or MySqlExporter.OracleDefault explicitly.")]
        public static readonly MySqlExporter Default = MariaDbDefault;

        private readonly SqlBuilderOptions _builderOptions;

        public MySqlFlavor Flavor { get; }

        public MySqlExporter(MySqlExporterOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this._builderOptions = options.BuilderOptions;
            this.Flavor = options.Flavor;
        }

        public MySqlExporter(SqlBuilderOptions builderOptions)
            : this(new MySqlExporterOptions(builderOptions, MySqlFlavor.MariaDb))
        {
        }

        public MySqlExporter(SqlBuilderOptions builderOptions, MySqlFlavor flavor)
            : this(new MySqlExporterOptions(builderOptions, flavor))
        {
        }

        public string ToSql(IExpr expr)
        {
            return ((ISqlExporterInternal)this).ToSql(expr, out _);
        }

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
