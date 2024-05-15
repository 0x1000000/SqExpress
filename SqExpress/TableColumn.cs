using SqExpress.Meta;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress
{
    public abstract class TableColumn : ExprColumn
    {
        protected TableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ExprType sqlType, bool isNullable, ColumnMeta? columnMeta) : base(source, columnName)
        {
            this.Table = table;
            this.SqlType = sqlType;
            this.IsNullable = isNullable;
            this.ColumnMeta = columnMeta;
        }

        public TableColumn WithSource(IExprColumnSource? source) => this.WithSourceInternal(source);

        public TableColumn WithColumnName(ExprColumnName columnName) => this.WithColumnNameInternal(columnName);

        public TableColumn WithTable(ExprTable table) => this.WithTableInternal(table);

        public TableColumn WithColumnMeta(ColumnMeta? columnMeta) => this.WithColumnMetaInternal(columnMeta);

        public abstract TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor);

        protected abstract TableColumn WithSourceInternal(IExprColumnSource? source);

        protected abstract TableColumn WithColumnNameInternal(ExprColumnName columnName);

        protected abstract TableColumn WithTableInternal(ExprTable table);

        protected abstract TableColumn WithColumnMetaInternal(ColumnMeta? columnMeta);

        public ExprTable Table { get; }

        public ExprType SqlType { get; }

        public bool IsNullable { get; }

        public ColumnMeta? ColumnMeta { get; }

        public abstract string? ReadAsString(ISqDataRecordReader recordReader);

        public abstract ExprLiteral FromString(string? value);
    }
}