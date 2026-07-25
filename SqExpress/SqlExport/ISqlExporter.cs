using SqExpress.StatementSyntax;
using SqExpress.Syntax;
using System.Collections.Generic;
using SqExpress.SqlExport.Internal;

namespace SqExpress.SqlExport
{
    /// <summary>
    /// Converts SqExpress expression trees and statements into SQL text for a specific database dialect.
    /// </summary>
    /// <remarks>
    /// Exporters do not execute SQL. Use the exporter matching the target database, such as
    /// <see cref="TSqlExporter"/>, <see cref="PgSqlExporter"/>, <see cref="MySqlExporter"/>,
    /// or <see cref="SqliteExporter"/>.
    /// </remarks>
    public interface ISqlExporter
    {
        /// <summary>
        /// Renders an expression tree as SQL.
        /// </summary>
        /// <param name="expr">The expression to render.</param>
        /// <returns>The SQL representation of <paramref name="expr"/>.</returns>
        string ToSql(IExpr expr);

        /// <summary>
        /// Renders a statement as SQL.
        /// </summary>
        /// <param name="statement">The statement to render.</param>
        /// <returns>The SQL representation of <paramref name="statement"/>.</returns>
        string ToSql(IStatement statement);
    }

    internal interface ISqlExporterInternal : ISqlExporter
    {
        internal string ToSql(IExpr expr, out IReadOnlyList<DbParameterValue>? parameters);

        int ParametersLimit { get; }
    }
}
