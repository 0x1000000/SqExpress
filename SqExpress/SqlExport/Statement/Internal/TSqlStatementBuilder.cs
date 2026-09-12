using System;
using SqExpress.SqlExport.Internal;
using SqExpress.StatementSyntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Names;

namespace SqExpress.SqlExport.Statement.Internal;

internal class TSqlStatementBuilder : SqlStatementBuilderBase
{
    private readonly TSqlBuilder _exprBuilder;

    public TSqlStatementBuilder(SqlBuilderOptions? options) : base(options)
    {
        this._exprBuilder = new TSqlBuilder(this.Options, this.FormattingWriter);
    }

    internal TSqlStatementBuilder(SqlBuilderOptions? options, SqlFormattingWriter formattingWriter)
        : base(options, formattingWriter)
    {
        this._exprBuilder = new TSqlBuilder(this.Options, formattingWriter);
    }

    public override void VisitCreateTable(StatementCreateTable statementCreateTable)
    {
        this.AppendTable(statementCreateTable);
    }

    protected override void AppendColumn(TableColumn column)
    {
        column.ColumnName.Accept(this.ExprBuilder, null);
        this.FormattingWriter.Append(' ');

        column.SqlType.Accept(this.ExprBuilder, null);

        if (!column.IsNullable)
        {
            this.FormattingWriter.Append(" NOT NULL");
        }

        if (column.ColumnMeta != null)
        {
            if (column.ColumnMeta.IsIdentity)
            {
                this.FormattingWriter.Append("  IDENTITY (1, 1)");
            }

            if (!ReferenceEquals(column.ColumnMeta.ColumnDefaultValue, null))
            {
                this.FormattingWriter.Append(" DEFAULT (");
                column.ColumnMeta.ColumnDefaultValue.Accept(this.ExprBuilder, null);
                this.FormattingWriter.Append(')');
            }
        }
    }

    protected override void AppendTempKeyword(IExprTableFullName tableName)
    {
    }

    protected override void AppendIndexesInside(TableBase table)
    {
        foreach (var tableIndex in table.Indexes)
        {
            this.FormattingWriter.Append(",INDEX ");
            this.AppendName(this.BuildIndexName(table.FullName, tableIndex));
            if (tableIndex.Unique)
            {
                this.FormattingWriter.Append(" UNIQUE");
            }
            if (tableIndex.Clustered)
            {
                this.FormattingWriter.Append(" CLUSTERED");
            }

            this.AppendIndexColumnList(tableIndex: tableIndex);
        }
    }

    protected override void AppendIndexesOutside(TableBase table)
    {
        //All indexes are created inside CREATE TABLE
    }

    protected override bool IsNamedPk() => true;

    public override void VisitDropTable(StatementDropTable statementDropTable)
    {
        if (!statementDropTable.IfExists)
        {
            this.FormattingWriter.Append("DROP TABLE ");
            statementDropTable.Table.Accept(this.ExprBuilder, null);
        }
        else
        {
            StatementIfExists ifExists = statementDropTable.Table switch
            {
                ExprTableFullName t => new StatementIfTableExists(
                    t,
                    StatementList.Combine(new StatementDropTable(statementDropTable.Table, false)), null),
                ExprTempTableName tempTable  => new StatementIfTempTableExists(
                    tempTable,
                    StatementList.Combine(new StatementDropTable(statementDropTable.Table, false)), null),
                _ => throw new ArgumentOutOfRangeException()
            };

            ifExists.Accept(this);
        }
    }

    public override void VisitIf(StatementIf statementIf)
    {
        this.FormattingWriter.Append("IF ");
        statementIf.Condition.Accept(this.ExprBuilder, null);

        bool ifBlock = statementIf.Statements.Count() > 1;
        if (ifBlock)
        {
            this.FormattingWriter.Append("BEGIN");
        }
        this.FormattingWriter.Append(' ');
        statementIf.Statements.Accept(this);
        if (ifBlock)
        {
            this.FormattingWriter.Append(" END");
        }

        if (statementIf.ElseStatements != null)
        {
            this.FormattingWriter.Append(" ELSE ");
            ifBlock = statementIf.ElseStatements.Count() > 1;
            if (ifBlock)
            {
                this.FormattingWriter.Append("BEGIN ");
            }
            statementIf.ElseStatements.Accept(this);
            this.FormattingWriter.Append(" END");
        }
    }

    public override void VisitIfTableExists(StatementIfTableExists statementIfExists)
    {
        if (statementIfExists.ExprTable.DbSchema == null)
        {
            throw new SqExpressException("Table schema name is mandatory to check the table existence");
        }

        string? databaseName = statementIfExists.ExprTable.DbSchema.Database?.Name;

        var tbl = new InformationSchemaTables(databaseName, Alias.Empty);

        ExprBoolean condition = tbl.TableSchema == this.Options.MapSchema(statementIfExists.ExprTable.DbSchema.Schema.Name) 
                                & tbl.TableName == statementIfExists.ExprTable.TableName.Name;

        var test = SqQueryBuilder.SelectTopOne()
            .From(tbl)
            .Where(condition);

        new StatementIf(SqQueryBuilder.Exists(test), statementIfExists.Statements, statementIfExists.ElseStatements).Accept(this);
    }

    public override void VisitIfTempTableExists(StatementIfTempTableExists statementIfTempTableExists)
    {
        var tableName =  TSqlExporter.Default.ToSql(statementIfTempTableExists.Table);

        tableName = "tempdb.." + tableName;

        var condition = SqQueryBuilder.IsNotNull(new ExprScalarFunction(null,
            new ExprFunctionName(true, "OBJECT_ID"),
            [SqQueryBuilder.Literal(tableName)]
        ));

        new StatementIf(condition, statementIfTempTableExists.Statements, statementIfTempTableExists.ElseStatements).Accept(this);
    }

    public string Build() => this.FormattingWriter.ToString();

    protected override SqlBuilderBase ExprBuilder => this._exprBuilder;
}