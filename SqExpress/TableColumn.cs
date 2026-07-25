using SqExpress.Meta;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress
{
    /// <summary>Base class for a typed column owned by a table descriptor.</summary>
    /// <remarks>
    /// A table column combines its SQL expression identity with SQL type, nullability, owning table, and optional
    /// schema metadata. Transformation methods return a new column instance and do not mutate the descriptor column.
    /// Concrete column types provide strongly typed record-reading and string conversion operations.
    /// </remarks>
    public abstract class TableColumn : TypedColumn
    {
        /// <summary>Initializes a table column from its expression identity, SQL type, nullability, and metadata.</summary>
        protected TableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ExprType sqlType, bool isNullable, ColumnMeta? columnMeta)
            : base(source, columnName, sqlType, isNullable)
        {
            this.Table = table;
            this.ColumnMeta = columnMeta;
        }

        /// <summary>Returns a copy qualified by the specified column source.</summary>
        public TableColumn WithSource(IExprColumnSource? source) => this.WithSourceInternal(source);

        /// <summary>Returns a copy with a different column name.</summary>
        public TableColumn WithColumnName(ExprColumnName columnName) => this.WithColumnNameInternal(columnName);

        /// <summary>Returns a copy associated with a different owning table expression.</summary>
        public TableColumn WithTable(ExprTable table) => this.WithTableInternal(table);

        /// <summary>Returns a copy with different optional schema metadata.</summary>
        public TableColumn WithColumnMeta(ColumnMeta? columnMeta) => this.WithColumnMetaInternal(columnMeta);

        /// <summary>Dispatches this column to the matching strongly typed visitor method.</summary>
        public abstract TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor);

        /// <summary>Creates the concrete column copy used by <see cref="WithSource"/>.</summary>
        protected abstract TableColumn WithSourceInternal(IExprColumnSource? source);

        /// <summary>Creates the concrete column copy used by <see cref="WithColumnName"/>.</summary>
        protected abstract TableColumn WithColumnNameInternal(ExprColumnName columnName);

        /// <summary>Creates the concrete column copy used by <see cref="WithTable"/>.</summary>
        protected abstract TableColumn WithTableInternal(ExprTable table);

        /// <summary>Creates the concrete column copy used by <see cref="WithColumnMeta"/>.</summary>
        protected abstract TableColumn WithColumnMetaInternal(ColumnMeta? columnMeta);

        /// <summary>Gets the table expression that owns this column.</summary>
        public ExprTable Table { get; }

        /// <summary>Gets optional schema metadata such as keys, defaults, or references.</summary>
        public ColumnMeta? ColumnMeta { get; }

        /// <summary>Reads the column from a record and converts it to its invariant string representation.</summary>
        /// <remarks>Non-nullable column implementations throw when the database value is null.</remarks>
        public abstract string? ReadAsString(ISqDataRecordReader recordReader);

        /// <summary>Parses a string representation into a literal appropriate for this column's CLR and SQL type.</summary>
        /// <remarks>Nullable columns accept <see langword="null"/>; non-nullable columns reject it.</remarks>
        public abstract ExprLiteral FromString(string? value);
    }
}
