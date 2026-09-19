using System.Collections.Generic;
using SqExpress.SqlExport.Internal;
using SqExpress.SqlExport.Statement.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;

namespace SqExpress.SqlExport;

/// <summary>
/// Renders SqExpress expression trees and statements using SQLite syntax.
/// </summary>
/// <remarks>
/// Portable operations are translated to SQLite-native syntax or compatible expressions where supported,
/// including SQLite's storage/type conventions. Exporting does not open or execute against a database.
/// </remarks>
public class SqliteExporter : ISqlExporterInternal
{
    /// <summary>Gets a reusable SQLite exporter with default quoting and schema behavior.</summary>
    public static readonly SqliteExporter Default = new SqliteExporter(SqlBuilderOptions.Default);

    private readonly SqlBuilderOptions _builderOptions;

    /// <summary>Creates a SQLite renderer with caller-selected identifier and schema handling.</summary>
    /// <param name="builderOptions">Options controlling schema mapping and identifier quoting.</param>
    public SqliteExporter(SqlBuilderOptions builderOptions)
    {
        this._builderOptions = builderOptions;
    }

    /// <summary>Returns a new SQLite exporter using the specified builder options.</summary>
    /// <param name="options">The replacement builder options.</param>
    /// <returns>A new exporter instance.</returns>
    public SqliteExporter WithOptions(SqlBuilderOptions options) => new SqliteExporter(options);

    /// <summary>Returns a new SQLite exporter using the specified formatting profile.</summary>
    /// <param name="profile">The formatting profile, or <see langword="null"/> for unformatted SQL.</param>
    /// <returns>A new exporter instance.</returns>
    public SqliteExporter WithFormatting(SqlFormattingProfile? profile)
        => new SqliteExporter(this._builderOptions.WithFormatting(profile));

    /// <inheritdoc/>
    public string ToSql(IExpr expr)
    {
        return ((ISqlExporterInternal)this).ToSql(expr, out _);
    }

    /// <inheritdoc/>
    public string ToSql(IStatement statement)
    {
        var builder = new SqliteStatementBuilder(this._builderOptions.WithFormatting(null));
        statement.Accept(builder);
        return builder.Build();
    }

    string ISqlExporterInternal.ToSql(IExpr expr, out IReadOnlyList<DbParameterValue>? parameters)
    {
        var sqlExporter = new SqliteBuilder(this._builderOptions);
        if (expr.Accept(sqlExporter, null))
        {
            var sql = sqlExporter.ToString();
            parameters = sqlExporter.ParameterValues;
            return sql;
        }

        throw new SqExpressException("Could not build Sql");
    }

    int ISqlExporterInternal.ParametersLimit => 32766;
}
