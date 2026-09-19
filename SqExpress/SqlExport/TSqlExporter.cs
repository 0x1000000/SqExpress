using System.Collections.Generic;
using SqExpress.SqlExport.Internal;
using SqExpress.SqlExport.Statement.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;

namespace SqExpress.SqlExport;

/// <summary>Renders SqExpress expression trees and statements using Microsoft SQL Server T-SQL syntax.</summary>
public class TSqlExporter : ISqlExporterInternal
{
    /// <summary>Gets a reusable T-SQL exporter with default identifier quoting and no schema remapping.</summary>
    public static readonly TSqlExporter Default = new TSqlExporter(SqlBuilderOptions.Default);

    private readonly SqlBuilderOptions _builderOptions;

    /// <summary>Creates a T-SQL renderer with caller-selected identifier and schema handling.</summary>
    /// <param name="builderOptions">Options controlling schema mapping and identifier quoting.</param>
    public TSqlExporter(SqlBuilderOptions builderOptions) => this._builderOptions = builderOptions;

    /// <summary>Returns a new T-SQL exporter using the specified builder options.</summary>
    /// <param name="options">The replacement builder options.</param>
    /// <returns>A new exporter instance.</returns>
    public TSqlExporter WithOptions(SqlBuilderOptions options) => new TSqlExporter(options);

    /// <summary>Returns a new T-SQL exporter using the specified formatting profile.</summary>
    /// <param name="profile">The formatting profile, or <see langword="null"/> for unformatted SQL.</param>
    /// <returns>A new exporter instance.</returns>
    public TSqlExporter WithFormatting(SqlFormattingProfile? profile)
        => new TSqlExporter(this._builderOptions.WithFormatting(profile));

    /// <inheritdoc/>
    public string ToSql(IExpr expr) => ((ISqlExporterInternal)this).ToSql(expr, out _);

    /// <inheritdoc/>
    public string ToSql(IStatement statement)
    {
        var builder = new TSqlStatementBuilder(this._builderOptions.WithFormatting(null));
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
