using System;
using System.Collections.Generic;
using System.Linq;
using SqExpress.SqlExport.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax.Names;

namespace SqExpress.SqlExport.Statement.Internal;

internal class SqliteStatementBuilder : SqlStatementBuilderBase
{
    private readonly SqliteBuilder _exprBuilder;

    public SqliteStatementBuilder(SqlBuilderOptions? options) : base(options)
    {
        this._exprBuilder = new SqliteBuilder(this.Options, this.FormattingWriter);
    }

    internal SqliteStatementBuilder(SqlBuilderOptions? options, SqlFormattingWriter formattingWriter)
        : base(options, formattingWriter)
    {
        this._exprBuilder = new SqliteBuilder(this.Options, formattingWriter);
    }

    public string Build() => this.FormattingWriter.ToString();

    protected override void AppendColumn(TableColumn column)
    {
        column.ColumnName.Accept(this.ExprBuilder, null);

        if (column.ColumnMeta?.IsIdentity == true && column.ColumnMeta.PrimaryKeyAutoIncrementIsAllowed())
        {
            this.FormattingWriter.Append(" INTEGER PRIMARY KEY AUTOINCREMENT");
            return;
        }

        this.FormattingWriter.Append(' ');
        column.SqlType.Accept(this.ExprBuilder, null);

        if (!column.IsNullable)
        {
            this.FormattingWriter.Append(" NOT NULL");
        }

        if (column.ColumnMeta != null && !ReferenceEquals(column.ColumnMeta.ColumnDefaultValue, null))
        {
            this.FormattingWriter.Append(" DEFAULT (");
            column.ColumnMeta.ColumnDefaultValue.Accept(this.ExprBuilder, null);
            this.FormattingWriter.Append(')');
        }
    }

    protected override void AppendTempKeyword(IExprTableFullName tableName)
    {
        if (tableName is ExprTempTableName)
        {
            this.FormattingWriter.Append("TEMP ");
        }
    }

    protected override void AppendIndexesInside(TableBase table)
    {
    }

    protected override void AppendIndexesOutside(TableBase table)
    {
        foreach (var tableIndex in table.Indexes)
        {
            if (tableIndex.Columns.Count == 1 && tableIndex.Columns[0].Column.ColumnMeta?.IsIdentity == true)
            {
                continue;
            }

            this.FormattingWriter.Append("CREATE ");
            if (tableIndex.Unique)
            {
                this.FormattingWriter.Append("UNIQUE ");
            }

            this.FormattingWriter.Append("INDEX ");
            this.AppendName(this.BuildIndexName(table.FullName, tableIndex));
            this.FormattingWriter.Append(" ON ");
            table.FullName.Accept(this.ExprBuilder, null);
            this.AppendIndexColumnList(tableIndex);
            this.FormattingWriter.Append(';');
        }
    }

    protected override bool IsNamedPk() => false;

    public override void VisitCreateTable(StatementCreateTable statementCreateTable)
    {
        var table = statementCreateTable.Table;

        this.FormattingWriter.Append("CREATE ");
        this.AppendTempKeyword(table.FullName);
        this.FormattingWriter.Append("TABLE ");
        table.FullName.Accept(this.ExprBuilder, null);
        this.FormattingWriter.Append('(');

        ColumnAnalysis analysis = ColumnAnalysis.Build();

        for (int i = 0; i < table.Columns.Count; i++)
        {
            if (i != 0)
            {
                this.FormattingWriter.Append(',');
            }

            var column = table.Columns[i];
            analysis.Analyze(column);
            this.AppendColumn(column);
        }

        var inlineIdentityPk = new HashSet<string>(
            table.Columns
                .Where(c => c.ColumnMeta?.IsIdentity == true && c.ColumnMeta.IsPrimaryKey)
                .Select(c => c.ColumnName.Name),
            StringComparer.Ordinal);

        var remainingPk = analysis.Pk
            .Where(c => !inlineIdentityPk.Contains(c.Name))
            .ToList();
        if (remainingPk.Count > 0)
        {
            this.FormattingWriter.Append(",PRIMARY KEY ");
            this.ExprBuilder.AcceptListComaSeparatedPar('(', remainingPk, ')', null);
        }

        foreach (var analysisFk in analysis.Fks)
        {
            var foreignTable = analysisFk.Key;
            var pairList = analysisFk.Value;

            this.FormattingWriter.Append(",FOREIGN KEY ");
            this.ExprBuilder.AcceptListComaSeparatedPar('(', pairList.Select(p => p.Internal).ToList(), ')', null);
            this.FormattingWriter.Append(" REFERENCES ");
            foreignTable.Accept(this.ExprBuilder, null);
            this.ExprBuilder.AcceptListComaSeparatedPar('(', pairList.Select(p => p.External).ToList(), ')', null);
        }

        this.FormattingWriter.Append(')');
        this.FormattingWriter.Append(';');

        this.AppendIndexesOutside(table);
    }

    public override void VisitDropTable(StatementDropTable statementDropTable)
    {
        this.FormattingWriter.Append("DROP TABLE ");
        if (statementDropTable.IfExists)
        {
            this.FormattingWriter.Append("IF EXISTS ");
        }

        statementDropTable.Table.Accept(this.ExprBuilder, null);
        this.FormattingWriter.Append(';');
    }

    public override void VisitIf(StatementIf statementIf)
    {
        throw new NotSupportedException("Not supported");
    }

    public override void VisitIfTableExists(StatementIfTableExists statementIfExists)
    {
        throw new NotSupportedException("Not supported");
    }

    public override void VisitIfTempTableExists(StatementIfTempTableExists statementIfTempTableExists)
    {
        throw new NotSupportedException("Not supported");
    }

    protected override SqlBuilderBase ExprBuilder => this._exprBuilder;
}

internal static class SqliteColumnMetaExtensions
{
    public static bool PrimaryKeyAutoIncrementIsAllowed(this ColumnMeta columnMeta)
    {
        return columnMeta.IsPrimaryKey;
    }
}