using System.Collections.Generic;
using SqExpress.SqlExport.Internal;
using SqExpress.SqlExport.Statement.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;

namespace SqExpress.SqlExport
{
    /// <summary>
    /// Renders SqExpress expression trees and statements using Microsoft SQL Server T-SQL syntax.
    /// </summary>
    public class TSqlExporter : ISqlExporterInternal
    {
        /// <summary>Gets an exporter using <see cref="SqlBuilderOptions.Default"/>.</summary>
        public static readonly TSqlExporter Default = new TSqlExporter(SqlBuilderOptions.Default);

        private readonly SqlBuilderOptions _builderOptions;

        /// <summary>Creates a T-SQL exporter.</summary>
        /// <param name="builderOptions">Options controlling schema mapping and identifier quoting.</param>
        public TSqlExporter(SqlBuilderOptions builderOptions)
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
            var builder = new TSqlStatementBuilder(this._builderOptions, null);
            statement.Accept(builder);
            return builder.Build();
        }

        string ISqlExporterInternal.ToSql(IExpr expr, out IReadOnlyList<DbParameterValue>? parameters)
        {
            var sqlExporter = new TSqlBuilder(this._builderOptions);
            if (expr.Accept(sqlExporter, null))
            {
                var sql = sqlExporter.ToString();
                parameters = sqlExporter.ParameterValues;
                return sql;
            }

            throw new SqExpressException("Could not build Sql");
        }

        int ISqlExporterInternal.ParametersLimit => 2000;
    }
}
