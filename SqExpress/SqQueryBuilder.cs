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
        public static ExprUnsafeValue UnsafeValue(string unsafeValueExpr) => new ExprUnsafeValue(unsafeValueExpr);

        /// <summary>
        /// Creates an <c>IS NULL</c> predicate for a value expression.
        /// </summary>
        public static ExprIsNull IsNull(ExprValue value) => new ExprIsNull(value, not: false);

        /// <summary>
        /// Creates an <c>IS NOT NULL</c> predicate for a value expression.
        /// </summary>
        public static ExprIsNull IsNotNull(ExprValue value) => new ExprIsNull(value, not: true);

        /// <summary>
        /// Creates a SQL <c>LIKE</c> predicate.
        /// </summary>
        public static ExprLike Like(ExprValue test, string pattern) => new ExprLike(test, pattern);

        /// <summary>
        /// Gets a selector for creating SQL type expressions used by casts and table declarations.
        /// </summary>
        public static SqlTypeSelector SqlType => new SqlTypeSelector();

        /// <summary>
        /// Starts construction of a SQL <c>CASE</c> expression.
        /// </summary>
        public static CaseWhen Case() => new CaseWhen();

        /// <summary>
        /// Selects a concrete SqExpress SQL type expression.
        /// </summary>
        public struct SqlTypeSelector
        {
            /// <summary>Gets the SQL Boolean type.</summary>
            public ExprTypeBoolean Boolean => ExprTypeBoolean.Instance;
            /// <summary>Gets the SQL byte type.</summary>
            public ExprTypeByte Byte => ExprTypeByte.Instance;
            /// <summary>Gets the SQL 16-bit integer type.</summary>
            public ExprTypeInt16 Int16 => ExprTypeInt16.Instance;
            /// <summary>Gets the SQL 32-bit integer type.</summary>
            public ExprTypeInt32 Int32 => ExprTypeInt32.Instance;
            /// <summary>Gets the SQL 64-bit integer type.</summary>
            public ExprTypeInt64 Int64 => ExprTypeInt64.Instance;
            /// <summary>Creates a SQL decimal type with optional precision and scale.</summary>
            public ExprTypeDecimal Decimal(DecimalPrecisionScale? precisionScale = null) => new ExprTypeDecimal(precisionScale);
            /// <summary>Gets the SQL double-precision type.</summary>
            public ExprTypeDouble Double => ExprTypeDouble.Instance;
            /// <summary>Creates a SQL date or date-time type.</summary>
            public ExprTypeDateTime DateTime(bool isDate = false) => new ExprTypeDateTime(isDate);
            /// <summary>Gets the SQL date-time-offset type.</summary>
            public ExprTypeDateTimeOffset DateTimeOffset => ExprTypeDateTimeOffset.Instance;
            /// <summary>Gets the SQL GUID type.</summary>
            public ExprTypeGuid Guid => ExprTypeGuid.Instance;
            /// <summary>Creates a SQL string type with optional size, Unicode, and large-text settings.</summary>
            public ExprTypeString String(int? size=null, bool isUnicode=true, bool isText = false) =>new ExprTypeString(size, isUnicode, isText);
            /// <summary>Creates a fixed-size SQL binary type.</summary>
            public ExprTypeFixSizeByteArray ByteArrayFixedSize(int size) => new ExprTypeFixSizeByteArray(size);
            /// <summary>Creates a variable-size SQL binary type.</summary>
            public ExprTypeByteArray ByteArray(int? size) => new ExprTypeByteArray(size);

        }

        /// <summary>
        /// Casts a value expression to the specified SQL type.
        /// </summary>
        public static ExprCast Cast(ExprValue expression, ExprType asType) 
            => new ExprCast(expression, asType);

        /// <summary>
        /// Casts a selecting expression to the specified SQL type.
        /// </summary>
        public static ExprCast Cast(IExprSelecting expression, ExprType asType) 
            => new ExprCast(expression, asType);

        /// <summary>
        /// Creates an unqualified column reference.
        /// </summary>
        public static ExprColumn Column(string columnName) 
            => new ExprColumn(null, columnName);

        /// <summary>
        /// Creates a column reference qualified by a table, alias, or other column source.
        /// </summary>
        public static ExprColumn Column(IExprColumnSource source, string columnName) 
            => new ExprColumn(source, columnName);

        /// <summary>
        /// Creates a table alias, generating one automatically when no alias is supplied.
        /// </summary>
        public static ExprTableAlias TableAlias(Alias alias = default)
            => new ExprTableAlias(alias.BuildAliasExpression() ?? Alias.Auto.BuildAliasExpression()!);

        /// <summary>
        /// Creates an <c>EXISTS</c> predicate from a completed subquery.
        /// </summary>
        public static ExprExists Exists(IExprSubQueryFinal subQuery) 
            => new ExprExists(subQuery.Done());

        /// <summary>
        /// Starts a mapped insert operation for a sequence of application data.
        /// </summary>
        public static IInsertDataBuilderMapData<TTable, TItem> InsertDataInto<TTable, TItem>(TTable table, IEnumerable<TItem> data)
            where TTable : ExprTable 
            =>
            new InsertDataBuilder<TTable, TItem>(table, data);

        /// <summary>Starts an insert into one or more named target columns.</summary>
        public static InsertBuilder InsertInto(ExprTable table, ExprColumnName column1, params ExprColumnName[] rest)
            => new InsertBuilder(table, Helpers.Combine(column1, rest));

        /// <summary>Starts an insert into a non-empty list of named target columns.</summary>
        public static InsertBuilder InsertInto(ExprTable table, IReadOnlyList<ExprColumnName> columns)
            => new InsertBuilder(table, columns.AssertNotEmpty(nameof(columns)));

        /// <summary>Starts an insert using a non-empty list of target column references.</summary>
        public static InsertBuilder InsertInto(ExprTable table, IReadOnlyList<ExprColumn> columns)
            => new InsertBuilder(table, columns.AssertNotEmpty(nameof(columns)).SelectToReadOnlyList(x=>x.ColumnName));

        /// <summary>Starts an identity insert into one or more named target columns.</summary>
        public static IdentityInsertBuilder IdentityInsertInto(ExprTable table, ExprColumnName column1, params ExprColumnName[] rest)
            => new IdentityInsertBuilder(table, Helpers.Combine(column1, rest));

        /// <summary>Starts an identity insert into a non-empty list of named target columns.</summary>
        public static IdentityInsertBuilder IdentityInsertInto(ExprTable table, IReadOnlyList<ExprColumnName> columns)
            => new IdentityInsertBuilder(table, columns.AssertNotEmpty(nameof(columns)));

        /// <summary>Starts an identity insert using a non-empty list of target column references.</summary>
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
        public static UpdateBuilder Update(ExprTable target)
            => new UpdateBuilder(target, new List<ExprColumnSetClause>());

        /// <summary>
        /// Starts an update operation that maps a sequence of application data to target-table rows.
        /// </summary>
        public static IUpdateDataBuilderMapDataInitial<TTable, TItem> UpdateData<TTable, TItem>(TTable table, IEnumerable<TItem> data)
            where TTable : ExprTable
            => new UpdateDataBuilder<TTable,TItem>(table, data, new ExprAliasGuid(Guid.NewGuid()));

        /// <summary>
        /// Starts a mapped <c>MERGE</c> operation for a sequence of application data.
        /// </summary>
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
        public static IMergeBuilderCondition MergeInto(ExprTable target, IExprTableSource source)
            => new MergeBuilder(target, source);

        /// <summary>
        /// Starts a <c>DELETE</c> statement for the target table.
        /// </summary>
        public static DeleteBuilder Delete(ExprTable target) 
            => new DeleteBuilder(target: target);
    }
}
