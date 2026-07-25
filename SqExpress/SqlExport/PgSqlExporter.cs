using SqExpress.SqlExport.Internal;
using SqExpress.SqlExport.Statement.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;
using System.Collections.Generic;

namespace SqExpress.SqlExport
{
    /// <summary>
    /// Renders SqExpress expression trees and statements using PostgreSQL syntax.
    /// </summary>
    public class PgSqlExporter : ISqlExporterInternal
    {
        /// <summary>Gets an exporter using <see cref="SqlBuilderOptions.Default"/>.</summary>
        public static readonly PgSqlExporter Default = new PgSqlExporter(SqlBuilderOptions.Default);

        private readonly SqlBuilderOptions _builderOptions;

        /// <summary>Creates a PostgreSQL exporter.</summary>
        /// <param name="builderOptions">Options controlling schema mapping and identifier quoting.</param>
        public PgSqlExporter(SqlBuilderOptions builderOptions)
        {
            this._builderOptions = builderOptions;
        }

        /// <inheritdoc/>
        public string ToSql(IExpr expr)
        {
            return ((ISqlExporterInternal)this).ToSql(expr, out _);
        }

        /// <inheritdoc/>
        public string ToSql(IStatement statement)
        {
            var builder = new PgSqlStatementBuilder(this._builderOptions, null);
            statement.Accept(builder);
            return builder.Build();
        }

        string ISqlExporterInternal.ToSql(IExpr expr, out IReadOnlyList<DbParameterValue>? parameters)
        {
            var sqlExporter = new PgSqlBuilder(this._builderOptions);
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
