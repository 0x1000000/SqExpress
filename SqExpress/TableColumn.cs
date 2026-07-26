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
        /// <summary>Initializes the shared query identity and schema metadata of a concrete typed table column.</summary>
        /// <param name="source">The qualifier used in SQL references, or <see langword="null"/> for an unqualified column.</param>
        /// <param name="columnName">The physical database column name.</param>
        /// <param name="table">The descriptor that owns the column for schema and relationship metadata.</param>
        /// <param name="sqlType">The portable SQL type rendered by the selected exporter.</param>
        /// <param name="isNullable">Whether database null is valid for this column.</param>
        /// <param name="columnMeta">Optional key, identity, default, or foreign-key metadata.</param>
        protected TableColumn(IExprColumnSource? source, ExprColumnName columnName, ExprTable table, ExprType sqlType, bool isNullable, ColumnMeta? columnMeta)
            : base(source, columnName, sqlType, isNullable)
        {
            this.Table = table;
            this.ColumnMeta = columnMeta;
        }

        /// <summary>Rebinds SQL qualification while preserving the column's name, type, table ownership, and metadata.</summary>
        /// <param name="source">The new qualifier, or <see langword="null"/> for an unqualified reference.</param>
        /// <returns>A new column of the same concrete type.</returns>
        public TableColumn WithSource(IExprColumnSource? source) => this.WithSourceInternal(source);

        /// <summary>Changes the physical/output name while preserving type, qualification, ownership, and metadata.</summary>
        /// <param name="columnName">The replacement column name.</param>
        /// <returns>A new column of the same concrete type.</returns>
        public TableColumn WithColumnName(ExprColumnName columnName) => this.WithColumnNameInternal(columnName);

        /// <summary>Changes schema ownership without changing the SQL qualifier used by the expression.</summary>
        /// <param name="table">The replacement owning table.</param>
        /// <returns>A new column of the same concrete type.</returns>
        public TableColumn WithTable(ExprTable table) => this.WithTableInternal(table);

        /// <summary>Changes key/default/identity/reference metadata without altering the column expression.</summary>
        /// <param name="columnMeta">Replacement metadata, or <see langword="null"/> to remove it.</param>
        /// <returns>A new column of the same concrete type.</returns>
        public TableColumn WithColumnMeta(ColumnMeta? columnMeta) => this.WithColumnMetaInternal(columnMeta);

        /// <summary>Dispatches to the visitor overload for the concrete CLR/nullability column type.</summary>
        /// <typeparam name="TRes">The result produced by the visitor.</typeparam>
        /// <param name="visitor">The table-column visitor.</param>
        /// <returns>The value returned by the matching visitor method.</returns>
        public abstract TRes Accept<TRes>(ITableColumnVisitor<TRes> visitor);

        /// <summary>Implements polymorphic source rebinding for the non-generic base API.</summary>
        /// <param name="source">The replacement qualifier.</param>
        /// <returns>A same-type column copy.</returns>
        protected abstract TableColumn WithSourceInternal(IExprColumnSource? source);

        /// <summary>Implements polymorphic name replacement for the non-generic base API.</summary>
        /// <param name="columnName">The replacement name.</param>
        /// <returns>A same-type column copy.</returns>
        protected abstract TableColumn WithColumnNameInternal(ExprColumnName columnName);

        /// <summary>Implements polymorphic table-ownership replacement for the non-generic base API.</summary>
        /// <param name="table">The replacement owner.</param>
        /// <returns>A same-type column copy.</returns>
        protected abstract TableColumn WithTableInternal(ExprTable table);

        /// <summary>Implements polymorphic schema-metadata replacement for the non-generic base API.</summary>
        /// <param name="columnMeta">The replacement metadata.</param>
        /// <returns>A same-type column copy.</returns>
        protected abstract TableColumn WithColumnMetaInternal(ColumnMeta? columnMeta);

        /// <summary>Gets the table expression that owns this column.</summary>
        public ExprTable Table { get; }

        /// <summary>Gets optional schema metadata such as keys, defaults, or references.</summary>
        public ColumnMeta? ColumnMeta { get; }

        /// <summary>Reads the column from a record and converts it to its invariant string representation.</summary>
        /// <remarks>Non-nullable column implementations throw when the database value is null.</remarks>
        /// <param name="recordReader">The current result row; lookup uses this column's database name.</param>
        /// <returns>The invariant serialized value, or <see langword="null"/> for a nullable database null.</returns>
        public abstract string? ReadAsString(ISqDataRecordReader recordReader);

        /// <summary>Parses a string representation into a literal appropriate for this column's CLR and SQL type.</summary>
        /// <remarks>Nullable columns accept <see langword="null"/>; non-nullable columns reject it.</remarks>
        /// <param name="value">The invariant serialized value, or <see langword="null"/> where the column permits it.</param>
        /// <returns>A typed SqExpress literal suitable for assignment to the column.</returns>
        public abstract ExprLiteral FromString(string? value);
    }
}
