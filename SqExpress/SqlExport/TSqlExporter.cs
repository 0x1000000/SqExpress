﻿using SqExpress.SqlExport.Internal;
using SqExpress.SqlExport.Statement.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;

namespace SqExpress.SqlExport
{
    public class TSqlExporter : ISqlExporter
    {
        public static readonly TSqlExporter Default = new TSqlExporter(SqlBuilderOptions.Default);

        private readonly SqlBuilderOptions _builderOptions;

        public TSqlExporter(SqlBuilderOptions builderOptions)
        {
            this._builderOptions = builderOptions;
        }

        public string ToSql(IExpr expr)
        {
            var sqlExporter = new TSqlBuilder(this._builderOptions);
            if (expr.Accept(sqlExporter, null))
            {
                return sqlExporter.ToString();
            }

            throw new SqExpressException("Could not build Sql");
        }

        public string ToSql(IStatement statement)
        {
            var builder = new TSqlStatementBuilder(this._builderOptions, null);
            statement.Accept(builder);
            return builder.Build();
        }
    }
}