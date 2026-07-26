using System;
using System.Collections.Generic;
using SqExpress.QueryBuilders;
using SqExpress.QueryBuilders.Case;
using SqExpress.QueryBuilders.Delete;
using SqExpress.QueryBuilders.Insert;
using SqExpress.QueryBuilders.Insert.Internal;
using SqExpress.QueryBuilders.Merge;
using SqExpress.QueryBuilders.Merge.Internal;
using SqExpress.QueryBuilders.Update;
using SqExpress.QueryBuilders.Update.Internal;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress
{
    /// <summary>
    /// Provides the preferred, SQL-shaped entry points for constructing SqExpress syntax trees.
    /// </summary>
    /// <remarks>
    /// Import this type with <c>using static SqExpress.SqQueryBuilder;</c> to compose expressions in a style that
    /// follows SQL clause order. Builder methods return staged interfaces so that only valid next clauses are exposed.
    /// </remarks>
    /// <example>
    /// <code>
    /// using static SqExpress.SqQueryBuilder;
    ///
    /// var query = Select("Hello World!").Done();
    /// var sql = query.ToSql(TSqlExporter.Default);
    /// </code>
    /// </example>
    public static partial class SqQueryBuilder
    {
        /// <summary>
        /// Gets the SQL <c>NULL</c> value expression.
        /// </summary>
        public static ExprNull Null => ExprNull.Instance;

        /// <summary>
        /// Gets the SQL <c>DEFAULT</c> value expression, for use where the target dialect permits it.
        /// </summary>
        public static ExprDefault Default => ExprDefault.Instance;

        /// <summary>
        /// Creates a value expression whose text is emitted without SQL escaping or parameterization.
        /// </summary>
        /// <remarks>
        /// Use only for SQL text that is trusted and valid for the selected exporter. Prefer typed expressions and
        /// parameter values for data originating outside the application.
        /// </remarks>
        /// <param name="unsafeValueExpr">Trusted SQL text to insert verbatim as a value expression.</param>
        /// <returns>An AST node whose contents bypass normal identifier and value escaping.</returns>
        public static ExprUnsafeValue UnsafeValue(string unsafeValueExpr) => new ExprUnsafeValue(unsafeValueExpr);

        /// <summary>
        /// Creates an <c>IS NULL</c> predicate for a value expression.
        /// </summary>
        /// <param name="value">The expression whose SQL null state is tested.</param>
        /// <returns>A null-test predicate; it does not use SQL equality semantics.</returns>
        public static ExprIsNull IsNull(ExprValue value) => new ExprIsNull(value, not: false);

        /// <summary>
        /// Creates an <c>IS NOT NULL</c> predicate for a value expression.
        /// </summary>
        /// <param name="value">The expression whose SQL null state is tested.</param>
        /// <returns>A negated null-test predicate.</returns>
        public static ExprIsNull IsNotNull(ExprValue value) => new ExprIsNull(value, not: true);

        /// <summary>
        /// Creates a SQL <c>LIKE</c> predicate using a literal pattern.
        /// </summary>
        /// <remarks>Pattern wildcards and escaping follow the target database's <c>LIKE</c> rules.</remarks>
        /// <param name="test">The string-compatible expression to test.</param>
        /// <param name="pattern">The SQL pattern, including any <c>%</c> or <c>_</c> wildcards.</param>
        /// <returns>A pattern-matching Boolean expression.</returns>
        public static ExprLike Like(ExprValue test, string pattern) => new ExprLike(test, pattern);

        /// <summary>
        /// Gets a selector for creating SQL type expressions used by casts and table declarations.
        /// </summary>
        public static SqlTypeSelector SqlType => new SqlTypeSelector();

        /// <summary>
        /// Starts construction of a searched SQL <c>CASE</c> expression.
        /// </summary>
        /// <returns>The stage that accepts the first <c>WHEN</c> condition.</returns>
        public static CaseWhen Case() => new CaseWhen();

        /// <summary>
        /// Selects a concrete SqExpress SQL type expression.
        /// </summary>
        public struct SqlTypeSelector
        {
            /// <summary>Gets the portable logical type, rendered using the selected database's Boolean-compatible type.</summary>
            public ExprTypeBoolean Boolean => ExprTypeBoolean.Instance;
            /// <summary>Gets the portable unsigned-byte type, rendered using the selected database's closest supported type.</summary>
            public ExprTypeByte Byte => ExprTypeByte.Instance;
            /// <summary>Gets the portable 16-bit signed integer type.</summary>
            public ExprTypeInt16 Int16 => ExprTypeInt16.Instance;
            /// <summary>Gets the portable 32-bit signed integer type.</summary>
            public ExprTypeInt32 Int32 => ExprTypeInt32.Instance;
            /// <summary>Gets the portable 64-bit signed integer type.</summary>
            public ExprTypeInt64 Int64 => ExprTypeInt64.Instance;
            /// <summary>Creates an exact numeric type whose dialect-specific name and optional precision are rendered by the exporter.</summary>
            /// <param name="precisionScale">Optional total precision and fractional scale; <see langword="null"/> requests the dialect default.</param>
            /// <returns>A decimal SQL type expression.</returns>
            public ExprTypeDecimal Decimal(DecimalPrecisionScale? precisionScale = null) => new ExprTypeDecimal(precisionScale);
            /// <summary>Gets the portable double-precision floating-point type.</summary>
            public ExprTypeDouble Double => ExprTypeDouble.Instance;
            /// <summary>Creates either a date-only or date-and-time type in the selected SQL dialect.</summary>
            /// <param name="isDate"><see langword="true"/> for a date-only type; <see langword="false"/> for a date-and-time type.</param>
            /// <returns>A temporal SQL type expression.</returns>
            public ExprTypeDateTime DateTime(bool isDate = false) => new ExprTypeDateTime(isDate);
            /// <summary>Gets the portable date, time, and UTC-offset type.</summary>
            public ExprTypeDateTimeOffset DateTimeOffset => ExprTypeDateTimeOffset.Instance;
            /// <summary>Gets the portable GUID/UUID type, rendered using the selected database's native representation.</summary>
            public ExprTypeGuid Guid => ExprTypeGuid.Instance;
            /// <summary>Creates a character type whose length and storage family are chosen by the selected SQL exporter.</summary>
            /// <param name="size">Maximum character count, or <see langword="null"/> for the dialect's unsized/default form.</param>
            /// <param name="isUnicode">Whether to request Unicode-capable storage when the dialect distinguishes it.</param>
            /// <param name="isText">Whether to request the dialect's large-text form instead of a regular varying character type.</param>
            /// <returns>A string SQL type expression.</returns>
            public ExprTypeString String(int? size=null, bool isUnicode=true, bool isText = false) =>new ExprTypeString(size, isUnicode, isText);
            /// <summary>Creates a fixed-length binary type in the selected SQL dialect.</summary>
            /// <param name="size">The required number of bytes.</param>
            /// <returns>A fixed-length binary SQL type expression.</returns>
            public ExprTypeFixSizeByteArray ByteArrayFixedSize(int size) => new ExprTypeFixSizeByteArray(size);
            /// <summary>Creates a variable-length binary type in the selected SQL dialect.</summary>
            /// <param name="size">Maximum byte count, or <see langword="null"/> for the dialect's unsized/default form.</param>
            /// <returns>A variable-length binary SQL type expression.</returns>
            public ExprTypeByteArray ByteArray(int? size) => new ExprTypeByteArray(size);

        }

        /// <summary>
        /// Creates a SQL conversion whose concrete type spelling is supplied by the selected exporter.
        /// </summary>
        /// <param name="expression">The value expression to convert.</param>
        /// <param name="asType">The portable target SQL type.</param>
        /// <returns>A <c>CAST</c> expression.</returns>
        public static ExprCast Cast(ExprValue expression, ExprType asType) 
            => new ExprCast(expression, asType);

        /// <summary>
        /// Creates a SQL conversion for a selectable expression such as an aggregate or analytic result.
        /// </summary>
        /// <param name="expression">The selectable expression to convert.</param>
        /// <param name="asType">The portable target SQL type.</param>
        /// <returns>A <c>CAST</c> expression.</returns>
        public static ExprCast Cast(IExprSelecting expression, ExprType asType) 
            => new ExprCast(expression, asType);

        /// <summary>
        /// Creates an unqualified column reference for dynamic queries that do not use a table descriptor.
        /// </summary>
        /// <param name="columnName">The database column name; the exporter applies identifier quoting.</param>
        /// <returns>An unqualified column expression.</returns>
        public static ExprColumn Column(string columnName) 
            => new ExprColumn(null, columnName);

        /// <summary>
        /// Creates a column reference qualified by a table, alias, or other column source.
        /// </summary>
        /// <param name="source">The table or alias used to qualify the column.</param>
        /// <param name="columnName">The database column name; the exporter applies identifier quoting.</param>
        /// <returns>A qualified column expression.</returns>
        public static ExprColumn Column(IExprColumnSource source, string columnName) 
            => new ExprColumn(source, columnName);

        /// <summary>
        /// Creates a table alias, generating one automatically when no alias is supplied.
        /// </summary>
        /// <param name="alias">An explicit alias, <see cref="Alias.Auto"/>, or the default value to request an automatically generated alias.</param>
        /// <returns>A table-alias AST node suitable for qualifying columns.</returns>
        public static ExprTableAlias TableAlias(Alias alias = default)
            => new ExprTableAlias(alias.BuildAliasExpression() ?? Alias.Auto.BuildAliasExpression()!);

        /// <summary>
        /// Creates an <c>EXISTS</c> predicate from a completed subquery.
        /// </summary>
        /// <param name="subQuery">The final fluent subquery stage to place inside <c>EXISTS</c>.</param>
        /// <returns>A Boolean expression that is true when the subquery yields at least one row.</returns>
        public static ExprExists Exists(IExprSubQueryFinal subQuery) 
            => new ExprExists(subQuery.Done());

        /// <summary>
        /// Starts a bulk-style insert builder that maps application objects to target columns and SQL values.
        /// </summary>
        /// <typeparam name="TTable">The concrete table descriptor type.</typeparam>
        /// <typeparam name="TItem">The application record type.</typeparam>
        /// <param name="table">The target table descriptor.</param>
        /// <param name="data">The records to map into inserted rows.</param>
        /// <returns>The stage that defines the per-record column mapping.</returns>
        public static IInsertDataBuilderMapData<TTable, TItem> InsertDataInto<TTable, TItem>(TTable table, IEnumerable<TItem> data)
            where TTable : ExprTable 
            =>
            new InsertDataBuilder<TTable, TItem>(table, data);

        /// <summary>Starts an <c>INSERT</c> whose source can be a values list or a query.</summary>
        /// <param name="table">The target table.</param>
        /// <param name="column1">The first required target column name.</param>
        /// <param name="rest">Additional target column names, in value/source projection order.</param>
        /// <returns>An insert builder that accepts values or a source query.</returns>
        public static InsertBuilder InsertInto(ExprTable table, ExprColumnName column1, params ExprColumnName[] rest)
            => new InsertBuilder(table, Helpers.Combine(column1, rest));

        /// <summary>Starts an <c>INSERT</c> using a prebuilt, non-empty target-column list.</summary>
        /// <param name="table">The target table.</param>
        /// <param name="columns">Target column names, ordered to match each values row or source projection.</param>
        /// <returns>An insert builder that accepts values or a source query.</returns>
        public static InsertBuilder InsertInto(ExprTable table, IReadOnlyList<ExprColumnName> columns)
            => new InsertBuilder(table, columns.AssertNotEmpty(nameof(columns)));

        /// <summary>Starts an <c>INSERT</c> using column references from a table descriptor.</summary>
        /// <param name="table">The target table.</param>
        /// <param name="columns">Target columns, ordered to match each values row or source projection.</param>
        /// <returns>An insert builder that accepts values or a source query.</returns>
        public static InsertBuilder InsertInto(ExprTable table, IReadOnlyList<ExprColumn> columns)
            => new InsertBuilder(table, columns.AssertNotEmpty(nameof(columns)).SelectToReadOnlyList(x=>x.ColumnName));

        /// <summary>Starts an <c>INSERT</c> that explicitly supplies identity/generated column values where the dialect supports it.</summary>
        /// <param name="table">The target table.</param>
        /// <param name="column1">The first required target column name.</param>
        /// <param name="rest">Additional target column names, in value/source projection order.</param>
        /// <returns>An identity-insert builder that accepts values or a source query.</returns>
        public static IdentityInsertBuilder IdentityInsertInto(ExprTable table, ExprColumnName column1, params ExprColumnName[] rest)
            => new IdentityInsertBuilder(table, Helpers.Combine(column1, rest));

        /// <summary>Starts an identity-value <c>INSERT</c> using a prebuilt, non-empty target-column list.</summary>
        /// <param name="table">The target table.</param>
        /// <param name="columns">Target column names, including any explicitly assigned identity column.</param>
        /// <returns>An identity-insert builder that accepts values or a source query.</returns>
        public static IdentityInsertBuilder IdentityInsertInto(ExprTable table, IReadOnlyList<ExprColumnName> columns)
            => new IdentityInsertBuilder(table, columns.AssertNotEmpty(nameof(columns)));

        /// <summary>Starts an identity-value <c>INSERT</c> using column references from a table descriptor.</summary>
        /// <param name="table">The target table.</param>
        /// <param name="columns">Target columns, including any explicitly assigned identity column.</param>
        /// <returns>An identity-insert builder that accepts values or a source query.</returns>
        public static IdentityInsertBuilder IdentityInsertInto(ExprTable table, IReadOnlyList<ExprColumn> columns)
            => new IdentityInsertBuilder(table, columns.AssertNotEmpty(nameof(columns)).SelectToReadOnlyList(x => x.ColumnName));

        /// <summary>
        /// Starts an <c>UPDATE</c> statement for the target table.
        /// </summary>
        /// <example>
        /// Given a table descriptor named <c>users</c>:
        /// <code>
        /// var statement = Update(users)
        ///     .Set(users.FirstName, "Alice")
        ///     .Where(users.UserId == 1);
        /// </code>
        /// </example>
        /// <param name="target">The table to update and the source of columns accepted by subsequent <c>Set</c> calls.</param>
        /// <returns>The stage that collects one or more assignments.</returns>
        public static UpdateBuilder Update(ExprTable target)
            => new UpdateBuilder(target, new List<ExprColumnSetClause>());

        /// <summary>
        /// Starts a set-based update that maps application records to a generated source value table and matches them to target rows.
        /// </summary>
        /// <typeparam name="TTable">The concrete table descriptor type.</typeparam>
        /// <typeparam name="TItem">The application record type.</typeparam>
        /// <param name="table">The target table descriptor.</param>
        /// <param name="data">The records used to identify and update target rows.</param>
        /// <returns>The stage that defines the source mapping and match/update rules.</returns>
        public static IUpdateDataBuilderMapDataInitial<TTable, TItem> UpdateData<TTable, TItem>(TTable table, IEnumerable<TItem> data)
            where TTable : ExprTable
            => new UpdateDataBuilder<TTable,TItem>(table, data, new ExprAliasGuid(Guid.NewGuid()));

        /// <summary>
        /// Starts a set-based <c>MERGE</c> that maps application records to a generated source value table.
        /// </summary>
        /// <typeparam name="TTable">The concrete target table descriptor type.</typeparam>
        /// <typeparam name="TItem">The application record type.</typeparam>
        /// <param name="table">The table to merge into.</param>
        /// <param name="data">The records to expose as the merge source.</param>
        /// <returns>The stage that defines source columns, matching, and matched/unmatched actions.</returns>
        public static IMergeDataBuilderMapDataInitial<TTable, TItem> MergeDataInto<TTable, TItem>(TTable table, IEnumerable<TItem> data)
            where TTable : ExprTable
            => new MergeDataBuilder<TTable, TItem>(table, data, new ExprAliasGuid(Guid.NewGuid()));

        /// <summary>
        /// Starts a <c>MERGE</c> statement between a target table and source table expression.
        /// </summary>
        /// <example>
        /// Given a target descriptor named <c>users</c> and a compatible <c>source</c> table expression:
        /// <code>
        /// var statement = MergeInto(users, source)
        ///     .On(users.UserId == source.Column(users.UserId))
        ///     .WhenMatched()
        ///         .ThenUpdate()
        ///         .Set(users.FirstName, source.Column(users.FirstName))
        ///     .Done();
        /// </code>
        /// </example>
        /// <param name="target">The table modified by matched and unmatched actions.</param>
        /// <param name="source">The table expression matched against the target.</param>
        /// <returns>The stage that requires the merge match condition.</returns>
        public static IMergeBuilderCondition MergeInto(ExprTable target, IExprTableSource source)
            => new MergeBuilder(target, source);

        /// <summary>
        /// Starts a <c>DELETE</c> builder that requires an explicit filter or an explicit choice to delete all rows.
        /// </summary>
        /// <param name="target">The table whose rows may be deleted.</param>
        /// <returns>The initial delete stage.</returns>
        public static DeleteBuilder Delete(ExprTable target) 
            => new DeleteBuilder(target: target);
    }
}
