using System.Collections.Generic;
using SqExpress.SqlExport.Internal;
using SqExpress.SqlExport.Statement.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;

namespace SqExpress.SqlExport;

/// <summary>Renders SqExpress expression trees and statements using PostgreSQL syntax.</summary>
public class PgSqlExporter : ISqlExporterInternal
{
    /// <summary>Gets a reusable PostgreSQL exporter with default quoting and schema behavior.</summary>
    public static readonly PgSqlExporter Default = new PgSqlExporter(SqlBuilderOptions.Default);

    private readonly SqlBuilderOptions _builderOptions;

    /// <summary>Creates a PostgreSQL renderer with caller-selected identifier and schema handling.</summary>
    /// <param name="builderOptions">Options controlling schema mapping and identifier quoting.</param>
    public PgSqlExporter(SqlBuilderOptions builderOptions) => this._builderOptions = builderOptions;

    /// <summary>Returns a new PostgreSQL exporter using the specified builder options.</summary>
    /// <param name="options">The replacement builder options.</param>
    /// <returns>A new exporter instance.</returns>
    public PgSqlExporter WithOptions(SqlBuilderOptions options) => new PgSqlExporter(options);

    /// <summary>Returns a new PostgreSQL exporter using the specified formatting profile.</summary>
    /// <param name="profile">The formatting profile, or <see langword="null"/> for unformatted SQL.</param>
    /// <returns>A new exporter instance.</returns>
    public PgSqlExporter WithFormatting(SqlFormattingProfile? profile)
        => new PgSqlExporter(this._builderOptions.WithFormatting(profile));

    /// <inheritdoc/>
    public string ToSql(IExpr expr) => ((ISqlExporterInternal)this).ToSql(expr, out _);

    /// <inheritdoc/>
    public string ToSql(IStatement statement)
    {
        var builder = new PgSqlStatementBuilder(this._builderOptions.WithFormatting(null));
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
