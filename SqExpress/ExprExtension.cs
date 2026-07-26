using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using SqExpress.DataAccess;
using SqExpress.QueryBuilders;
using SqExpress.QueryBuilders.Select;
using SqExpress.QueryBuilders.Update;
using SqExpress.SqlExport;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;
using SqExpress.SyntaxTreeOperations;
using SqExpress.SyntaxTreeOperations.ExportImport;
using SqExpress.SyntaxTreeOperations.ExportImport.Internal;
using SqExpress.SyntaxTreeOperations.Internal;

namespace SqExpress
{
    /// <summary>
    /// Provides execution, export, binding, parameterization, and syntax-tree operations for SqExpress expressions.
    /// </summary>
    /// <remarks>
    /// These extensions are the normal bridge from a completed syntax tree to an <see cref="ISqDatabase"/>, an
    /// <see cref="ISqlExporter"/>, or a syntax-tree traversal operation.
    /// </remarks>
    public static class ExprExtension
    {
        //Sync handler
        /// <summary>Executes a query and folds rows synchronously into an accumulator while command execution remains asynchronous.</summary>
        /// <typeparam name="TAgg">The accumulator and final result type.</typeparam>
        /// <param name="query">The completed query syntax tree.</param>
        /// <param name="database">The database wrapper that exports and executes the query.</param>
        /// <param name="seed">The initial accumulator.</param>
        /// <param name="aggregator">Processes the current row before its reader advances and returns the next accumulator.</param>
        /// <param name="cancellationToken">Requests cancellation of execution and result reading.</param>
        /// <returns>A task containing the final accumulator.</returns>
        public static Task<TAgg> Query<TAgg>(this IExprQuery query, ISqDatabase database, TAgg seed, Func<TAgg,ISqDataRecordReader, TAgg> aggregator, CancellationToken cancellationToken = default) 
            => database.Query(query, seed, aggregator, cancellationToken);

        /// <summary>Completes a fluent query and folds its rows synchronously into an accumulator.</summary>
        /// <typeparam name="TAgg">The accumulator and final result type.</typeparam>
        /// <param name="query">The final fluent query stage.</param>
        /// <param name="database">The database wrapper that exports and executes the query.</param>
        /// <param name="seed">The initial accumulator.</param>
        /// <param name="aggregator">Processes the current row before its reader advances and returns the next accumulator.</param>
        /// <param name="cancellationToken">Requests cancellation of execution and result reading.</param>
        /// <returns>A task containing the final accumulator.</returns>
        public static Task<TAgg> Query<TAgg>(this IExprQueryFinal query, ISqDatabase database, TAgg seed, Func<TAgg,ISqDataRecordReader, TAgg> aggregator, CancellationToken cancellationToken = default) 
            => database.Query(query.Done(), seed, aggregator, cancellationToken);

        /// <summary>Executes a query and invokes a synchronous callback once for every returned row.</summary>
        /// <param name="query">The completed query syntax tree.</param>
        /// <param name="database">The database wrapper that exports and executes the query.</param>
        /// <param name="handler">Processes the current row; the supplied reader is valid only during this callback.</param>
        /// <param name="cancellationToken">Requests cancellation of execution and result reading.</param>
        /// <returns>A task that completes after all rows have been handled.</returns>
        public static Task Query(this IExprQuery query, ISqDatabase database, Action<ISqDataRecordReader> handler, CancellationToken cancellationToken = default) 
            => database.Query(query, handler, cancellationToken: cancellationToken);

        /// <summary>Completes a fluent query and invokes a synchronous callback for every returned row.</summary>
        /// <param name="query">The final fluent query stage.</param>
        /// <param name="database">The database wrapper that exports and executes the query.</param>
        /// <param name="handler">Processes the current row; the supplied reader is valid only during this callback.</param>
        /// <param name="cancellationToken">Requests cancellation of execution and result reading.</param>
        /// <returns>A task that completes after all rows have been handled.</returns>
        public static Task Query(this IExprQueryFinal query, ISqDatabase database, Action<ISqDataRecordReader> handler, CancellationToken cancellationToken = default) 
            => database.Query(query.Done(), handler, cancellationToken: cancellationToken);

        //Async handler
#if !NETSTANDARD
        /// <summary>Streams query rows asynchronously without materializing the complete result set.</summary>
        /// <remarks>The provider-backed reader is valid only for the current asynchronous iteration.</remarks>
        /// <param name="query">The completed query syntax tree.</param>
        /// <param name="database">The database wrapper that exports and executes the query.</param>
        /// <param name="cancellationToken">Requests cancellation of execution and enumeration.</param>
        /// <returns>A lazy asynchronous sequence of row readers.</returns>
        public static IAsyncEnumerable<ISqDataRecordReader> Query(this IExprQuery query, ISqDatabase database, CancellationToken cancellationToken = default)
            => database.Query(query, cancellationToken);

        /// <summary>Completes a fluent query and streams its rows asynchronously without materializing them.</summary>
        /// <remarks>The provider-backed reader is valid only for the current asynchronous iteration.</remarks>
        /// <param name="query">The final fluent query stage.</param>
        /// <param name="database">The database wrapper that exports and executes the query.</param>
        /// <param name="cancellationToken">Requests cancellation of execution and enumeration.</param>
        /// <returns>A lazy asynchronous sequence of row readers.</returns>
        public static IAsyncEnumerable<ISqDataRecordReader> Query(this IExprQueryFinal query, ISqDatabase database, CancellationToken cancellationToken = default)
            => database.Query(query.Done(), cancellationToken);
#endif

        /// <summary>Executes a query and awaits an asynchronous accumulator callback for every returned row.</summary>
        /// <typeparam name="TAgg">The accumulator and final result type.</typeparam>
        /// <param name="query">The completed query syntax tree.</param>
        /// <param name="database">The database wrapper that exports and executes the query.</param>
        /// <param name="seed">The initial accumulator.</param>
        /// <param name="aggregator">Asynchronously processes the current row and returns the next accumulator.</param>
        /// <param name="cancellationToken">Requests cancellation of execution and result reading.</param>
        /// <returns>A task containing the final accumulator after every callback has completed.</returns>
        public static Task<TAgg> Query<TAgg>(this IExprQuery query, ISqDatabase database, TAgg seed, Func<TAgg,ISqDataRecordReader, Task<TAgg>> aggregator, CancellationToken cancellationToken = default) 
            => database.Query(query, seed, aggregator, cancellationToken);

        /// <summary>Completes a fluent query and asynchronously folds its rows into an accumulator.</summary>
        /// <typeparam name="TAgg">The accumulator and final result type.</typeparam>
        /// <param name="query">The final fluent query stage.</param>
        /// <param name="database">The database wrapper that exports and executes the query.</param>
        /// <param name="seed">The initial accumulator.</param>
        /// <param name="aggregator">Asynchronously processes the current row and returns the next accumulator.</param>
        /// <param name="cancellationToken">Requests cancellation of execution and result reading.</param>
        /// <returns>A task containing the final accumulator after every callback has completed.</returns>
        public static Task<TAgg> Query<TAgg>(this IExprQueryFinal query, ISqDatabase database, TAgg seed, Func<TAgg,ISqDataRecordReader, Task<TAgg>> aggregator, CancellationToken cancellationToken = default) 
            => database.Query(query.Done(), seed, aggregator, cancellationToken);

        /// <summary>Executes a query and awaits an asynchronous callback for every returned row.</summary>
        /// <param name="query">The completed query syntax tree.</param>
        /// <param name="database">The database wrapper that exports and executes the query.</param>
        /// <param name="handler">Asynchronously processes the current row before its reader advances.</param>
        /// <param name="cancellationToken">Requests cancellation of execution and result reading.</param>
        /// <returns>A task that completes after all row callbacks finish.</returns>
        public static Task Query(this IExprQuery query, ISqDatabase database, Func<ISqDataRecordReader, Task> handler, CancellationToken cancellationToken = default) 
            => database.Query(query, handler, cancellationToken: cancellationToken);

        /// <summary>Completes a fluent query and awaits an asynchronous callback for every returned row.</summary>
        /// <param name="query">The final fluent query stage.</param>
        /// <param name="database">The database wrapper that exports and executes the query.</param>
        /// <param name="handler">Asynchronously processes the current row before its reader advances.</param>
        /// <param name="cancellationToken">Requests cancellation of execution and result reading.</param>
        /// <returns>A task that completes after all row callbacks finish.</returns>
        public static Task Query(this IExprQueryFinal query, ISqDatabase database, Func<ISqDataRecordReader, Task> handler, CancellationToken cancellationToken = default) 
            => database.Query(query.Done(), handler, cancellationToken: cancellationToken);

        /// <summary>
        /// Executes a query asynchronously and maps every returned row into an application value.
        /// </summary>
        /// <typeparam name="T">The result element type produced by the row mapper.</typeparam>
        /// <param name="query">The completed query syntax tree.</param>
        /// <param name="database">The database wrapper that exports and executes the query.</param>
        /// <param name="factory">Maps the current data-record reader to one result element; it is invoked before the reader advances.</param>
        /// <param name="cancellationToken">Requests cancellation of command execution and result reading.</param>
        /// <returns>A task whose result contains mapped rows in provider return order.</returns>
        public static Task<List<T>> QueryList<T>(this IExprQuery query, ISqDatabase database, Func<ISqDataRecordReader, T> factory, CancellationToken cancellationToken = default) 
            => database.QueryList(query, factory, cancellationToken: cancellationToken);

        /// <summary>Completes a fluent query and maps every returned row into a materialized list.</summary>
        /// <typeparam name="T">The mapped result element type.</typeparam>
        /// <param name="query">The final fluent query stage.</param>
        /// <param name="database">The database wrapper that executes the query.</param>
        /// <param name="factory">Maps the current row before its reader advances.</param>
        /// <param name="cancellationToken">Requests cancellation of execution and result reading.</param>
        /// <returns>A task containing mapped rows in provider return order.</returns>
        public static Task<List<T>> QueryList<T>(this IExprQueryFinal query, ISqDatabase database, Func<ISqDataRecordReader, T> factory, CancellationToken cancellationToken = default) 
            => database.QueryList(query.Done(), factory, cancellationToken: cancellationToken);

        /// <summary>Executes a query and builds a dictionary from selected rows.</summary>
        /// <typeparam name="TKey">The non-null dictionary key type.</typeparam>
        /// <typeparam name="TValue">The dictionary value type.</typeparam>
        /// <param name="query">The completed query syntax tree.</param>
        /// <param name="database">The database wrapper that executes the query.</param>
        /// <param name="keyFactory">Creates a key from the current row.</param>
        /// <param name="valueFactory">Creates a value from the same current row.</param>
        /// <param name="keyDuplicationHandler">Optional policy for combining or replacing duplicate keys; default behavior is defined by the database extensions.</param>
        /// <param name="predicate">Optional filter applied to each mapped key/value pair before insertion.</param>
        /// <param name="cancellationToken">Requests cancellation of execution and reading.</param>
        /// <returns>A task containing the constructed dictionary.</returns>
        public static Task<Dictionary<TKey, TValue>> QueryDictionary<TKey, TValue>(
            this IExprQuery query, 
            ISqDatabase database, 
            Func<ISqDataRecordReader, TKey> keyFactory,
            Func<ISqDataRecordReader, TValue> valueFactory, 
            SqDatabaseExtensions.KeyDuplicationHandler<TKey, TValue>? keyDuplicationHandler = null,
            Func<TKey, TValue, bool>? predicate = null,
            CancellationToken cancellationToken = default)
        where TKey : notnull
            => database.QueryDictionary(query, keyFactory, valueFactory, keyDuplicationHandler, predicate, cancellationToken: cancellationToken);

        /// <summary>Completes a fluent query and builds a dictionary from selected rows.</summary>
        /// <typeparam name="TKey">The non-null dictionary key type.</typeparam>
        /// <typeparam name="TValue">The dictionary value type.</typeparam>
        /// <param name="query">The final fluent query stage.</param>
        /// <param name="database">The database wrapper that executes the query.</param>
        /// <param name="keyFactory">Creates a key from the current row.</param>
        /// <param name="valueFactory">Creates a value from the same current row.</param>
        /// <param name="keyDuplicationHandler">Optional duplicate-key policy.</param>
        /// <param name="predicate">Optional filter applied before insertion.</param>
        /// <returns>A task containing the constructed dictionary.</returns>
        public static Task<Dictionary<TKey, TValue>> QueryDictionary<TKey, TValue>(
            this IExprQueryFinal query, 
            ISqDatabase database, 
            Func<ISqDataRecordReader, TKey> keyFactory,
            Func<ISqDataRecordReader, TValue> valueFactory, 
            SqDatabaseExtensions.KeyDuplicationHandler<TKey, TValue>? keyDuplicationHandler = null,
            Func<TKey, TValue, bool>? predicate = null)
        where TKey : notnull
            => database.QueryDictionary(query.Done(), keyFactory, valueFactory, keyDuplicationHandler, predicate);

        /// <summary>Completes and executes an offset/fetch builder, returning page items and the total unpaged row count.</summary>
        /// <remarks>The total count is obtained by adding a windowed <c>COUNT</c> projection to the query.</remarks>
        /// <typeparam name="T">The mapped page-item type.</typeparam>
        /// <param name="builder">The final offset/fetch builder stage.</param>
        /// <param name="database">The database wrapper that executes the query.</param>
        /// <param name="reader">Maps each returned row to a page item.</param>
        /// <param name="cancellationToken">Requests cancellation of execution and reading.</param>
        /// <returns>A task containing items, effective offset, and total row count.</returns>
        public static Task<DataPage<T>> QueryPage<T>(this ISelectOffsetFetchBuilderFinal builder, ISqDatabase database, Func<ISqDataRecordReader, T> reader, CancellationToken cancellationToken = default)
            => builder.Done().QueryPage(database, reader, cancellationToken: cancellationToken);

        /// <summary>Executes a completed offset/fetch query and returns page items together with the total unpaged row count.</summary>
        /// <remarks>The query is copied with an additional windowed count column; the supplied syntax tree is not mutated.</remarks>
        /// <typeparam name="T">The mapped page-item type.</typeparam>
        /// <param name="query">The completed paginated query.</param>
        /// <param name="database">The database wrapper that executes the query.</param>
        /// <param name="reader">Maps each returned row to a page item.</param>
        /// <param name="cancellationToken">Requests cancellation of execution and reading.</param>
        /// <returns>A task containing items, effective offset, and total row count.</returns>
        public static async Task<DataPage<T>> QueryPage<T>(this ExprSelectOffsetFetch query, ISqDatabase database, Func<ISqDataRecordReader, T> reader, CancellationToken cancellationToken = default)
        {
            var countColumn = CustomColumnFactory.Int32("$count$");

            var selectQuery = (ExprQuerySpecification)query.SelectQuery;

            query = query.WithSelectQuery(
                selectQuery.WithSelectList(selectQuery.SelectList.Combine(SqQueryBuilder.CountOne().Over().As(countColumn))));

            var res = await query.Query(database,
                new KeyValuePair<List<T>, int?>(new List<T>(), null),
                (acc, r) =>
                {
                    acc.Key.Add(reader(r));
                    var total = acc.Value ?? countColumn.Read(r);
                    return new KeyValuePair<List<T>, int?>(acc.Key, total);
                });

            var offsetLiteral = query.OrderBy.OffsetFetch.Offset as ExprInt32Literal;

            return new DataPage<T>(res.Key, offsetLiteral?.Value ?? 0, res.Value ?? 0);
        }

        /// <summary>
        /// Executes a query and returns the provider's scalar result from the first column of the first record.
        /// </summary>
        /// <remarks>
        /// The underlying provider determines how SQL <c>NULL</c> is represented; it may return
        /// <see cref="DBNull.Value"/> rather than a C# <see langword="null"/>.
        /// </remarks>
        /// <param name="query">The completed query to execute.</param>
        /// <param name="database">The database wrapper that exports and executes the query.</param>
        /// <param name="cancellationToken">Requests cancellation of command execution.</param>
        /// <returns>A task containing the provider scalar value, or the provider's no-result/null representation.</returns>
        public static Task<object?> QueryScalar(this IExprQuery query, ISqDatabase database, CancellationToken cancellationToken = default)
            => database.QueryScalar(query, cancellationToken);

        /// <summary>Completes a fluent query and returns the provider scalar value from its first row and first column.</summary>
        /// <remarks>SQL <c>NULL</c> may be returned as <see cref="DBNull.Value"/> depending on the provider.</remarks>
        /// <param name="query">The final fluent query stage.</param>
        /// <param name="database">The database wrapper that executes the query.</param>
        /// <param name="cancellationToken">Requests cancellation of execution.</param>
        /// <returns>A task containing the provider scalar result.</returns>
        public static Task<object?> QueryScalar(this IExprQueryFinal query, ISqDatabase database, CancellationToken cancellationToken = default)
            => database.QueryScalar(query.Done(), cancellationToken);

        /// <summary>
        /// Executes a completed statement and discards the provider's affected-row count.
        /// </summary>
        /// <param name="query">The insert, update, delete, merge, DDL, or combined statement to execute.</param>
        /// <param name="database">The database wrapper that exports and executes the statement.</param>
        /// <param name="cancellationToken">Requests cancellation of command execution.</param>
        /// <returns>A task that completes when the provider command finishes.</returns>
        public static Task Exec(this IExprExec query, ISqDatabase database, CancellationToken cancellationToken = default)
            => database.Exec(query, cancellationToken);

        /// <summary>Completes and executes a fluent non-query statement.</summary>
        /// <param name="query">The final insert, update, delete, merge, or DDL builder stage.</param>
        /// <param name="database">The database wrapper that exports and executes the statement.</param>
        /// <param name="cancellationToken">Requests cancellation of execution.</param>
        /// <returns>A task that completes when the command finishes.</returns>
        public static Task Exec(this IExprExecFinal query, ISqDatabase database, CancellationToken cancellationToken = default)
            => database.Exec(query.Done(), cancellationToken);

        /// <summary>Completes and executes a mapped set-based update.</summary>
        /// <param name="builder">The final mapped-update stage.</param>
        /// <param name="database">The database wrapper that exports and executes the update.</param>
        /// <param name="cancellationToken">Requests cancellation of execution.</param>
        /// <returns>A task that completes when the update finishes.</returns>
        public static Task Exec(this IUpdateDataBuilderFinal builder, ISqDatabase database, CancellationToken cancellationToken = default)
            => database.Exec(builder.Done(), cancellationToken);

        /// <summary>
        /// Exports an expression to SQL using the selected database dialect.
        /// </summary>
        /// <example>
        /// <code>
        /// var sql = SqQueryBuilder.SelectOne().Done().ToSql(TSqlExporter.Default);
        /// </code>
        /// </example>
        /// <param name="expr">The syntax tree to render.</param>
        /// <param name="exporter">The exporter whose dialect, quoting, and polyfill rules should be applied.</param>
        /// <returns>The generated SQL text. This overload does not execute it.</returns>
        public static string ToSql(this IExpr expr, ISqlExporter exporter) 
            => exporter.ToSql(expr);

        /// <summary>Completes a fluent non-query and renders it using the selected database dialect.</summary>
        /// <param name="expr">The final non-query builder stage.</param>
        /// <param name="exporter">The exporter supplying dialect syntax, quoting, and portable-function translations.</param>
        /// <returns>The generated SQL text; no database command is executed.</returns>
        public static string ToSql(this IExprExecFinal expr, ISqlExporter exporter)
            => expr.Done().ToSql(exporter);

        /// <summary>Completes a fluent query and renders it using the selected database dialect.</summary>
        /// <param name="expr">The final query builder stage.</param>
        /// <param name="exporter">The exporter supplying dialect syntax, quoting, and portable-function translations.</param>
        /// <returns>The generated SQL text; no database command is executed.</returns>
        public static string ToSql(this IExprQueryFinal expr, ISqlExporter exporter)
            => expr.Done().ToSql(exporter);

        /// <summary>Renders a general or combined statement using the selected database dialect.</summary>
        /// <param name="expr">The statement tree to render.</param>
        /// <param name="exporter">The exporter supplying dialect syntax and statement support.</param>
        /// <returns>The generated SQL text; no database command is executed.</returns>
        public static string ToSql(this IStatement expr, ISqlExporter exporter)
            => exporter.ToSql(expr);

        /// <summary>
        /// Resolves table and column references in an expression against the supplied table descriptors.
        /// </summary>
        /// <exception cref="SqExpressException">
        /// Binding produced one or more errors. Use <see cref="TryBindTables{T}(T, IReadOnlyList{TableBase}, out T, out IReadOnlyList{TableBindingDiagnostic}, out IReadOnlyList{TableBindingDiagnostic})"/>
        /// to receive diagnostics instead.
        /// </exception>
        /// <typeparam name="T">The concrete root expression type, preserved by the binding operation.</typeparam>
        /// <param name="expression">The parsed or dynamically constructed syntax tree containing unresolved references.</param>
        /// <param name="tables">The visible table descriptor catalog used to resolve physical columns and tables.</param>
        /// <returns>A rewritten expression whose resolvable references point to supplied descriptors.</returns>
        public static T BindTables<T>(this T expression, IReadOnlyList<TableBase> tables) where T : IExpr
            => BindTables(expression, tables, new TableBindingOptions(), out _);

        /// <summary>Resolves table and column references with caller-selected diagnostic severity policy.</summary>
        /// <typeparam name="T">The concrete root expression type preserved by rewriting.</typeparam>
        /// <param name="expression">The syntax tree containing references to bind.</param>
        /// <param name="tables">The visible descriptor catalog.</param>
        /// <param name="options">Controls which binding conditions are warnings or errors.</param>
        /// <returns>A rewritten expression whose resolvable references point to supplied descriptors.</returns>
        /// <exception cref="SqExpressException">One or more error-severity diagnostics were produced.</exception>
        public static T BindTables<T>(this T expression, IReadOnlyList<TableBase> tables, TableBindingOptions options) where T : IExpr
            => BindTables(expression, tables, options, out _);

        /// <summary>Resolves table and column references with the default severity policy and returns recoverable diagnostics.</summary>
        /// <typeparam name="T">The concrete root expression type preserved by rewriting.</typeparam>
        /// <param name="expression">The syntax tree containing references to bind.</param>
        /// <param name="tables">The visible descriptor catalog.</param>
        /// <param name="warnings">Receives recoverable binding diagnostics.</param>
        /// <returns>The bound expression.</returns>
        /// <exception cref="SqExpressException">One or more error-severity diagnostics were produced.</exception>
        public static T BindTables<T>(this T expression, IReadOnlyList<TableBase> tables, out IReadOnlyList<TableBindingDiagnostic> warnings) where T : IExpr
            => BindTables(expression, tables, new TableBindingOptions(), out warnings);

        /// <summary>Resolves references with an explicit severity policy while returning recoverable diagnostics.</summary>
        /// <typeparam name="T">The concrete root expression type preserved by rewriting.</typeparam>
        /// <param name="expression">The syntax tree containing references to bind.</param>
        /// <param name="tables">The visible descriptor catalog.</param>
        /// <param name="options">Controls which binding conditions are warnings or errors.</param>
        /// <param name="warnings">Receives warning-severity diagnostics.</param>
        /// <returns>The bound expression.</returns>
        /// <exception cref="SqExpressException">One or more error-severity diagnostics were produced.</exception>
        public static T BindTables<T>(
            this T expression,
            IReadOnlyList<TableBase> tables,
            TableBindingOptions options,
            out IReadOnlyList<TableBindingDiagnostic> warnings) where T : IExpr
        {
            var result = TableBinder.Bind(expression, tables, options, out warnings, out var errors);
            if (errors.Count > 0)
            {
                throw new SqExpressException(string.Join(Environment.NewLine, errors.Select(e => e.Message)));
            }
            return result;
        }

        /// <summary>
        /// Attempts fail-closed table/column resolution without throwing for binding diagnostics.
        /// </summary>
        /// <typeparam name="T">The concrete root expression type, preserved by the binding operation.</typeparam>
        /// <param name="expression">The syntax tree containing references to resolve.</param>
        /// <param name="tables">The visible table descriptor catalog.</param>
        /// <param name="boundExpression">Receives the rewritten expression, including successful partial resolutions when errors occur.</param>
        /// <param name="warnings">Receives recoverable diagnostics according to the default severity policy.</param>
        /// <param name="errors">Receives diagnostics that prevented successful binding.</param>
        /// <returns><see langword="true"/> when no error-severity diagnostics were produced.</returns>
        public static bool TryBindTables<T>(
            this T expression,
            IReadOnlyList<TableBase> tables,
            out T boundExpression,
            out IReadOnlyList<TableBindingDiagnostic> warnings,
            out IReadOnlyList<TableBindingDiagnostic> errors) where T : IExpr
            => TryBindTables(expression, tables, new TableBindingOptions(), out boundExpression, out warnings, out errors);

        /// <summary>Attempts table/column binding with an explicit diagnostic policy without throwing for binding errors.</summary>
        /// <typeparam name="T">The concrete root expression type preserved by rewriting.</typeparam>
        /// <param name="expression">The syntax tree containing references to bind.</param>
        /// <param name="tables">The visible descriptor catalog.</param>
        /// <param name="options">Controls diagnostic severities.</param>
        /// <param name="boundExpression">Receives the rewritten expression, including successful partial resolutions.</param>
        /// <param name="warnings">Receives warning-severity diagnostics.</param>
        /// <param name="errors">Receives error-severity diagnostics.</param>
        /// <returns><see langword="true"/> when no error diagnostics were produced.</returns>
        public static bool TryBindTables<T>(
            this T expression,
            IReadOnlyList<TableBase> tables,
            TableBindingOptions options,
            out T boundExpression,
            out IReadOnlyList<TableBindingDiagnostic> warnings,
            out IReadOnlyList<TableBindingDiagnostic> errors) where T : IExpr
        {
            boundExpression = TableBinder.Bind(expression, tables, options, out warnings, out errors);
            return errors.Count == 0;
        }

        /// <summary>Replaces every named parser parameter with caller-supplied typed values.</summary>
        /// <remarks>
        /// Parameter names are normalized by removing leading <c>@</c> characters and compared ordinally.
        /// List values expand only when the parameter is an item of <c>IN (...)</c>. Every named parameter present
        /// in the expression must have a supplied value; duplicate normalized names and list use elsewhere fail.
        /// The input syntax tree is not mutated.
        /// </remarks>
        /// <typeparam name="T">The concrete root expression type preserved by rewriting.</typeparam>
        /// <param name="expr">The parsed or constructed expression containing named parameter nodes.</param>
        /// <param name="values">Replacement values keyed by parameter name, with or without leading <c>@</c>.</param>
        /// <returns>A rewritten expression with scalar parameters substituted and <c>IN</c> lists expanded.</returns>
        /// <exception cref="SqExpressException">A parameter is missing, duplicated after normalization, empty, or used with a list outside <c>IN (...)</c>.</exception>
        public static T WithParams<T>(this T expr, IReadOnlyDictionary<string, ParamValue> values) where T : IExpr
        {
            if (values.Count == 0)
            {
                return expr;
            }

            var normalizedValues = NormalizeParamDictionary(values);

            var result = expr.SyntaxTree()
                .ModifyDescendants(e =>
                    {
                        if (e is ExprInValues inValues)
                        {
                            return ReplaceInValues(inValues, normalizedValues);
                        }

                        if (e is ExprParameter parameter)
                        {
                            var tagName = parameter.TagName;
                            if (tagName != null && tagName.Length > 0 && normalizedValues.TryGetValue(tagName, out var value))
                            {
                                if (value.IsSingle)
                                {
                                    return value.AsSingle;
                                }

                                return e;
                            }

                            if (tagName != null && tagName.Length > 0)
                            {
                                throw new SqExpressException($"Could not find parameter {tagName}");
                            }
                        }

                        return e;
                    }
                );

            EnsureNoListParamsOutsideIn(result, normalizedValues);
            return result;
        }

        /// <summary>Validates that a general parsed expression is a row-returning query.</summary>
        /// <param name="expr">The expression returned by a parser or another API with a general <see cref="IExpr"/> result.</param>
        /// <returns>The same expression viewed as a query.</returns>
        /// <exception cref="SqExpressException">The expression is not query-shaped.</exception>
        public static IExprQuery AsQuery(this IExpr expr)
            => EnsureQuery(expr);

        /// <summary>Validates that a general parsed expression is an executable non-query statement.</summary>
        /// <param name="expr">The expression returned by a parser or another API with a general <see cref="IExpr"/> result.</param>
        /// <returns>The same expression viewed as a non-query statement.</returns>
        /// <exception cref="SqExpressException">The expression is a query or otherwise not executable as a non-query.</exception>
        public static IExprExec AsNonQuery(this IExpr expr)
            => EnsureNonQuery(expr);

#if NET8_0_OR_GREATER

        /// <summary>Replaces one named parser parameter with a scalar value or an <c>IN</c>-list expansion.</summary>
        /// <typeparam name="T">The concrete root expression type preserved by rewriting.</typeparam>
        /// <param name="expr">The expression containing the named parameter.</param>
        /// <param name="paramName">The parameter name, with or without leading <c>@</c>.</param>
        /// <param name="paramValue">The scalar replacement or non-empty list used inside <c>IN (...)</c>.</param>
        /// <returns>A rewritten expression; the original tree is unchanged.</returns>
        /// <exception cref="SqExpressException">The name is invalid, a required parameter is missing, or a list is used outside <c>IN (...)</c>.</exception>
        public static T WithParams<T>(this T expr, string paramName, ParamValue paramValue) where T : IExpr 
            => WithParams(expr, [(paramName, paramValue)]);

        /// <summary>Replaces named parser parameters from a tuple span without requiring a dictionary allocation for small sets.</summary>
        /// <remarks>Names are normalized by removing leading <c>@</c>; list replacements are valid only inside <c>IN (...)</c>.</remarks>
        /// <typeparam name="T">The concrete root expression type preserved by rewriting.</typeparam>
        /// <param name="expr">The expression containing named parameters.</param>
        /// <param name="values">Parameter-name/replacement pairs. Normalized names must be unique.</param>
        /// <returns>A rewritten expression; an empty span returns the original instance.</returns>
        /// <exception cref="SqExpressException">A name is invalid/duplicated, a required value is absent, or a list is used outside <c>IN (...)</c>.</exception>
        public static T WithParams<T>(this T expr, params ReadOnlySpan<(string paramName, ParamValue paramExprValue)> values) where T: IExpr
        {
            if (values.Length == 0)
            {
                return expr;
            }

            if (values.Length <= 4)
            {
                string? n0 = null;
                string? n1 = null;
                string? n2 = null;
                string? n3 = null;
                ParamValue? v0 = null;
                ParamValue? v1 = null;
                ParamValue? v2 = null;
                ParamValue? v3 = null;

                for (int i = 0; i < values.Length; i++)
                {
                    var (paramName, paramExprValue) = values[i];
                    paramName = NormalizeParamName(paramName);

                    if ((n0 != null && StringComparer.Ordinal.Equals(paramName, n0))
                        || (n1 != null && StringComparer.Ordinal.Equals(paramName, n1))
                        || (n2 != null && StringComparer.Ordinal.Equals(paramName, n2))
                        || (n3 != null && StringComparer.Ordinal.Equals(paramName, n3)))
                    {
                        throw new SqExpressException($"Duplicate parameter name '{paramName}'");
                    }

                    switch (i)
                    {
                        case 0:
                            n0 = paramName;
                            v0 = paramExprValue;
                            break;
                        case 1:
                            n1 = paramName;
                            v1 = paramExprValue;
                            break;
                        case 2:
                            n2 = paramName;
                            v2 = paramExprValue;
                            break;
                        case 3:
                            n3 = paramName;
                            v3 = paramExprValue;
                            break;
                    }
                }

                var result = expr.SyntaxTree()
                    .ModifyDescendants(e =>
                        {
                            if (e is ExprInValues inValues)
                            {
                                return ReplaceInValues(inValues, n0, v0, n1, v1, n2, v2, n3, v3);
                            }

                            if (e is ExprParameter parameter)
                            {
                                var tagName = parameter.TagName;
                                if (tagName is { Length: > 0 })
                                {
                                    if (n0 != null && StringComparer.Ordinal.Equals(tagName, n0) && v0.HasValue)
                                    {
                                        return v0.Value.IsSingle ? v0.Value.AsSingle : e;
                                    }
                                    if (n1 != null && StringComparer.Ordinal.Equals(tagName, n1) && v1.HasValue)
                                    {
                                        return v1.Value.IsSingle ? v1.Value.AsSingle : e;
                                    }
                                    if (n2 != null && StringComparer.Ordinal.Equals(tagName, n2) && v2.HasValue)
                                    {
                                        return v2.Value.IsSingle ? v2.Value.AsSingle : e;
                                    }
                                    if (n3 != null && StringComparer.Ordinal.Equals(tagName, n3) && v3.HasValue)
                                    {
                                        return v3.Value.IsSingle ? v3.Value.AsSingle : e;
                                    }

                                    throw new SqExpressException($"Could not find parameter {tagName}");
                                }
                            }

                            return e;
                        }
                    );
                EnsureNoListParamsOutsideIn(result, n0, v0, n1, v1, n2, v2, n3, v3);
                return result;
            }

            var dictionary = new Dictionary<string, ParamValue>(values.Length, StringComparer.Ordinal);
            for (int i = 0; i < values.Length; i++)
            {
                var (paramName, paramExprValue) = values[i];
                paramName = NormalizeParamName(paramName);

                if (!dictionary.TryAdd(paramName, paramExprValue))
                {
                    throw new SqExpressException($"Duplicate parameter name '{paramName}'");
                }
            }

            return expr.WithParams(dictionary);
        }
#endif

        private static ExprInValues ReplaceInValues(ExprInValues inValues, IReadOnlyDictionary<string, ParamValue> values)
        {
            List<ExprValue>? newItems = null;

            for (var index = 0; index < inValues.Items.Count; index++)
            {
                var item = inValues.Items[index];
                if (item is ExprParameter { TagName: { Length: > 0 } tagName } && values.TryGetValue(tagName, out var value))
                {
                    newItems ??= new List<ExprValue>(inValues.Items.Count);
                    if (newItems.Count == 0 && index > 0)
                    {
                        for (var j = 0; j < index; j++)
                        {
                            newItems.Add(inValues.Items[j]);
                        }
                    }

                    if (value.IsSingle)
                    {
                        newItems.Add(value.AsSingle);
                    }
                    else
                    {
                        foreach (var listValue in value.AsList)
                        {
                            newItems.Add(listValue);
                        }
                    }
                }
                else if (newItems != null)
                {
                    newItems.Add(item);
                }
            }

            return newItems != null ? new ExprInValues(inValues.TestExpression, newItems) : inValues;
        }

#if NET8_0_OR_GREATER
        private static ExprInValues ReplaceInValues(
            ExprInValues inValues,
            string? n0,
            ParamValue? v0,
            string? n1,
            ParamValue? v1,
            string? n2,
            ParamValue? v2,
            string? n3,
            ParamValue? v3)
        {
            List<ExprValue>? newItems = null;

            for (var index = 0; index < inValues.Items.Count; index++)
            {
                var item = inValues.Items[index];
                ParamValue? replacement = null;

                if (item is ExprParameter { TagName: { Length: > 0 } tagName })
                {
                    if (n0 != null && StringComparer.Ordinal.Equals(tagName, n0))
                    {
                        replacement = v0;
                    }
                    else if (n1 != null && StringComparer.Ordinal.Equals(tagName, n1))
                    {
                        replacement = v1;
                    }
                    else if (n2 != null && StringComparer.Ordinal.Equals(tagName, n2))
                    {
                        replacement = v2;
                    }
                    else if (n3 != null && StringComparer.Ordinal.Equals(tagName, n3))
                    {
                        replacement = v3;
                    }
                }

                if (replacement.HasValue)
                {
                    newItems ??= new List<ExprValue>(inValues.Items.Count);
                    if (newItems.Count == 0 && index > 0)
                    {
                        for (var j = 0; j < index; j++)
                        {
                            newItems.Add(inValues.Items[j]);
                        }
                    }

                    if (replacement.Value.IsSingle)
                    {
                        newItems.Add(replacement.Value.AsSingle);
                    }
                    else
                    {
                        foreach (var listValue in replacement.Value.AsList)
                        {
                            newItems.Add(listValue);
                        }
                    }
                }
                else if (newItems != null)
                {
                    newItems.Add(item);
                }
            }

            return newItems != null ? new ExprInValues(inValues.TestExpression, newItems) : inValues;
        }
#endif

        private static void EnsureNoListParamsOutsideIn<T>(T expr, IReadOnlyDictionary<string, ParamValue> values) where T : IExpr
        {
            foreach (var parameter in expr.SyntaxTree().DescendantsAndSelf())
            {
                if (parameter is ExprParameter { TagName: { Length: > 0 } tagName }
                    && values.TryGetValue(tagName, out var value)
                    && value.IsList)
                {
                    throw new SqExpressException($"List parameter {tagName} can be used only in IN(...)");
                }
            }
        }

        private static IReadOnlyDictionary<string, ParamValue> NormalizeParamDictionary(IReadOnlyDictionary<string, ParamValue> values)
        {
            var result = new Dictionary<string, ParamValue>(values.Count, StringComparer.Ordinal);

            foreach (var pair in values)
            {
                var paramName = NormalizeParamName(pair.Key);
                if (result.ContainsKey(paramName))
                {
                    throw new SqExpressException($"Duplicate parameter name '{paramName}'");
                }

                result.Add(paramName, pair.Value);
            }

            return result;
        }

        private static string NormalizeParamName(string? paramName)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                throw new SqExpressException("Parameter name cannot be null or empty");
            }

            var notNullParamName = paramName!;

            var index = 0;
            while (index < notNullParamName.Length && notNullParamName[index] == '@')
            {
                index++;
            }

            if (index == notNullParamName.Length)
            {
                throw new SqExpressException("Parameter name cannot be null or empty");
            }

            return index == 0 ? notNullParamName : notNullParamName.Substring(index);
        }

        private static IExprQuery EnsureQuery(IExpr expr)
        {
            if (expr is IExprQuery query)
            {
                return query;
            }

            throw new SqExpressException(
                $"Expression '{expr.GetType().Name}' is not a query. Use {nameof(AsNonQuery)}() for INSERT/UPDATE/DELETE/MERGE statements.");
        }

        private static IExprExec EnsureNonQuery(IExpr expr)
        {
            if (expr is IExprExec exec)
            {
                return exec;
            }

            throw new SqExpressException(
                $"Expression '{expr.GetType().Name}' is not a non-query statement. Use {nameof(AsQuery)}() for SELECT statements.");
        }

#if NET8_0_OR_GREATER
        private static void EnsureNoListParamsOutsideIn<T>(
            T expr,
            string? n0,
            ParamValue? v0,
            string? n1,
            ParamValue? v1,
            string? n2,
            ParamValue? v2,
            string? n3,
            ParamValue? v3) where T : IExpr
        {
            foreach (var parameter in expr.SyntaxTree().DescendantsAndSelf())
            {
                if (parameter is not ExprParameter { TagName: { Length: > 0 } tagName })
                {
                    continue;
                }

                if (n0 != null && StringComparer.Ordinal.Equals(tagName, n0) && v0.HasValue && v0.Value.IsList)
                {
                    throw new SqExpressException($"List parameter {tagName} can be used only in IN(...)");
                }

                if (n1 != null && StringComparer.Ordinal.Equals(tagName, n1) && v1.HasValue && v1.Value.IsList)
                {
                    throw new SqExpressException($"List parameter {tagName} can be used only in IN(...)");
                }

                if (n2 != null && StringComparer.Ordinal.Equals(tagName, n2) && v2.HasValue && v2.Value.IsList)
                {
                    throw new SqExpressException($"List parameter {tagName} can be used only in IN(...)");
                }

                if (n3 != null && StringComparer.Ordinal.Equals(tagName, n3) && v3.HasValue && v3.Value.IsList)
                {
                    throw new SqExpressException($"List parameter {tagName} can be used only in IN(...)");
                }
            }
        }
#endif

        /// <summary>Executes a general or combined statement through the database's statement pipeline.</summary>
        /// <param name="expr">The statement tree, which may contain multiple commands.</param>
        /// <param name="database">The database wrapper that exports and executes the statement.</param>
        /// <param name="cancellationToken">Requests cancellation of execution.</param>
        /// <returns>A task that completes after all statement commands finish.</returns>
        public static Task Exec(this IStatement expr, ISqDatabase database, CancellationToken cancellationToken = default)
            => database.Statement(expr, cancellationToken);

        /// <summary>Opens traversal, search, serialization, and immutable modification operations for an expression tree.</summary>
        /// <typeparam name="TExpr">The concrete root type preserved by descendant-only modifications.</typeparam>
        /// <param name="expr">The root expression.</param>
        /// <returns>A lightweight operation facade around the supplied root.</returns>
        public static SyntaxTreeActions<TExpr> SyntaxTree<TExpr>(this TExpr expr) where TExpr : IExpr
        {
            return new SyntaxTreeActions<TExpr>(expr);
        }

        /// <summary>Provides custom traversal, search, export, and immutable rewriting operations for one expression root.</summary>
        /// <typeparam name="TExpr">The concrete root expression type.</typeparam>
        /// <remarks>
        /// Traversals are depth-first and follow the structural property order defined by SqExpress.
        /// Modification methods rebuild changed branches; they do not mutate the original syntax tree.
        /// For custom node-specific traversal classes, prefer <see cref="ExprVisitorBase"/>. Implement
        /// <see cref="IExprVisitor{TRes,TArg}"/> directly only when each visit must return a value or receive an argument.
        /// </remarks>
        public readonly struct SyntaxTreeActions<TExpr> where TExpr : IExpr
        {
            private readonly TExpr _expr;

            internal SyntaxTreeActions(TExpr expr)
            {
                this._expr = expr;
            }

            /// <summary>Walks the tree with the full structural callback interface and caller-supplied context.</summary>
            /// <typeparam name="TCtx">The context propagated through visitor results and property callbacks.</typeparam>
            /// <param name="walkerVisitor">Receives node, property, collection, and scalar callbacks.</param>
            /// <param name="context">The initial traversal context.</param>
            public void WalkThrough<TCtx>(IWalkerVisitor<TCtx> walkerVisitor, TCtx context)
            {
                this._expr.Accept(new ExprWalker<TCtx>(walkerVisitor), new WalkerContext<TCtx>(null, context));
            }

            /// <summary>Walks expression nodes with a delegate and returns the final propagated context.</summary>
            /// <typeparam name="TCtx">The traversal context type.</typeparam>
            /// <param name="walker">Returns the next context and whether to continue, skip descendants, or stop.</param>
            /// <param name="context">The initial context.</param>
            /// <returns>The context held when traversal completes or stops.</returns>
            public TCtx WalkThrough<TCtx>(Func<IExpr, TCtx, VisitorResult<TCtx>> walker, TCtx context)
            {
                var walkerVisitor = new DefaultWalkerVisitor<TCtx>(walker, context);
                this._expr.Accept(new ExprWalker<TCtx>(walkerVisitor), new WalkerContext<TCtx>(null, context));
                return walkerVisitor.CurrentCtx;
            }

            /// <summary>Walks the tree with structural callbacks that also receive each node's parent.</summary>
            /// <typeparam name="TCtx">The context propagated through traversal.</typeparam>
            /// <param name="walkerVisitor">The parent-aware structural visitor.</param>
            /// <param name="context">The initial traversal context.</param>
            public void WalkThroughWithParent<TCtx>(IWalkerVisitorWithParent<TCtx> walkerVisitor, TCtx context)
            {
                this._expr.Accept(new ExprWalker<TCtx>(walkerVisitor), new WalkerContext<TCtx>(null, context));
            }

            /// <summary>Walks expression nodes with their parents and returns the final propagated context.</summary>
            /// <typeparam name="TCtx">The traversal context type.</typeparam>
            /// <param name="walker">Receives the current node, its parent or <see langword="null"/> for the root, and current context.</param>
            /// <param name="context">The initial context.</param>
            /// <returns>The context held when traversal completes or stops.</returns>
            public TCtx WalkThroughWithParent<TCtx>(Func<IExpr, IExpr?, TCtx, VisitorResult<TCtx>> walker, TCtx context)
            {
                var walkerVisitor = new DefaultParentWalkerVisitorWithParent<TCtx>(walker, context);
                this._expr.Accept(new ExprWalker<TCtx>(walkerVisitor), new WalkerContext<TCtx>(null, context));
                return walkerVisitor.CurrentCtx;
            }

            /// <summary>Enumerates every expression below the root in depth-first structural order.</summary>
            /// <returns>A lazy sequence that excludes the root expression.</returns>
            public IEnumerable<IExpr> Descendants()
            {
                return ExprWalkerPull.GetEnumerable(this._expr, false);
            }

            /// <summary>Enumerates the root and all descendants in depth-first structural order.</summary>
            /// <returns>A lazy sequence beginning with the root expression.</returns>
            public IEnumerable<IExpr> DescendantsAndSelf()
            {
                return ExprWalkerPull.GetEnumerable(this._expr, true);
            }

            /// <summary>Finds the first node of a requested type in root-first depth-first order.</summary>
            /// <typeparam name="TExprNode">The expression-node type to locate.</typeparam>
            /// <param name="filter">An optional additional predicate applied to matching node types.</param>
            /// <returns>The first matching node, or <see langword="null"/> when none is found.</returns>
            public TExprNode? FirstOrDefault<TExprNode>(Predicate<TExprNode>? filter = null) where TExprNode : class, IExpr
            {
                TExprNode? result = null;
                this._expr.Accept(new ExprWalker<object?>(new DefaultWalkerVisitor<object?>((e, c) =>
                {
                    if (e is TExprNode te && (filter == null || filter.Invoke(te)))
                    {
                        result = te;
                        return VisitorResult<object?>.Stop(c);
                    }
                    return VisitorResult<object?>.Continue(c);
                })), new WalkerContext<object?>(null, null));
                return result;
            }

            /// <summary>Rewrites the root and descendants using a callback invoked for every expression node.</summary>
            /// <remarks>Returning the original node leaves it unchanged; returning <see langword="null"/> removes it only where the AST property permits null.</remarks>
            /// <param name="modifier">Returns the replacement for each visited node.</param>
            /// <returns>The rewritten root, which may be a different type or <see langword="null"/>.</returns>
            public IExpr? Modify(Func<IExpr, IExpr?> modifier)
            {
                return this._expr.Accept(new ExprModifier(), modifier);
            }

            /// <summary>Rewrites only nodes assignable to a requested expression type, including the root when it matches.</summary>
            /// <typeparam name="TExprNode">The node type passed to the modifier.</typeparam>
            /// <param name="modifier">Returns a replacement for each matching node.</param>
            /// <returns>The rewritten root, which may be a different type or <see langword="null"/>.</returns>
            public IExpr? Modify<TExprNode>(Func<TExprNode, IExpr?> modifier) where TExprNode: IExpr
            {
                return this._expr.Accept(new ExprModifier(),
                    e =>
                    {
                        if (e is TExprNode te)
                        {
                            return modifier(te);
                        }
                        return e;
                    });
            }

            /// <summary>Rewrites descendants while guaranteeing that the root itself is not passed to the modifier.</summary>
            /// <param name="modifier">Returns a replacement for every descendant expression.</param>
            /// <returns>The rewritten expression with the same compile-time root type.</returns>
            public TExpr ModifyDescendants(Func<IExpr, IExpr?> modifier)
            {
                var thisExpr = this._expr;
                return (TExpr)thisExpr.Accept(new ExprModifier(),
                    e =>
                    {
                        if (!ReferenceEquals(e, thisExpr))
                        {
                            return modifier(e);
                        }
                        return e;
                    })!;
            }

            /// <summary>Rewrites matching descendants while excluding the root even when it has the requested type.</summary>
            /// <typeparam name="TExprNode">The descendant node type passed to the modifier.</typeparam>
            /// <param name="modifier">Returns a replacement for every matching descendant.</param>
            /// <returns>The rewritten expression with the same compile-time root type.</returns>
            public TExpr ModifyDescendants<TExprNode>(Func<TExprNode, IExpr?> modifier) where TExprNode : IExpr
            {
                var thisExpr = this._expr;
                return (TExpr)thisExpr.Accept(new ExprModifier(),
                    e =>
                    {
                        if (!ReferenceEquals(e, thisExpr) && e is TExprNode te)
                        {
                            return modifier(te);
                        }
                        return e;
                    })!;
            }

            /// <summary>Serializes the complete structural tree to a flat list of caller-selected plain items.</summary>
            /// <typeparam name="T">The plain item representation.</typeparam>
            /// <param name="plainItemFactory">Creates items for node/property traversal events.</param>
            /// <returns>The serialized items in traversal order.</returns>
            public IReadOnlyList<T> ExportToPlainList<T>(PlainItemFactory<T> plainItemFactory) where T : IPlainItem
            {
                var walkerVisitor = new ExprPlainWriter<T>(plainItemFactory);
                WalkThrough(walkerVisitor, 0);
                return walkerVisitor.Result;
            }

            /// <summary>Writes the complete expression-tree structure to an existing XML writer.</summary>
            /// <param name="xmlWriter">The configured writer that receives nodes, properties, and scalar values; ownership remains with the caller.</param>
            public void ExportToXml(XmlWriter xmlWriter)
            {
                var walkerVisitor = new ExprXmlWriter();
                WalkThrough(walkerVisitor, xmlWriter);
            }
#if !NETSTANDARD
            /// <summary>Writes the complete expression-tree structure to an existing UTF-8 JSON writer.</summary>
            /// <param name="jsonWriter">The configured writer that receives nodes, properties, and scalar values; ownership remains with the caller.</param>
            public void ExportToJson(System.Text.Json.Utf8JsonWriter jsonWriter)
            {
                var walkerVisitor = new ExprJsonWriter();
                WalkThrough(walkerVisitor, jsonWriter);
            }
#endif

            internal IExpr ParametrizeLiterals(int? limit, out int numOfParams, out int numOfSkips)
            {
                var counter = 0;
                var skipsCounter = 0;

                var res = this._expr.SyntaxTree()
                    .Modify(e =>
                        {
                            if (e is ExprLiteral v)
                            {
                                if (limit.HasValue && counter < limit.Value)
                                {
                                    counter++;
                                    return new ExprParameter(v, null);
                                }
                                skipsCounter++;
                            }

                            return e;
                        }
                    );
                numOfParams = counter;
                numOfSkips = skipsCounter;
                return res!;
            }
        }
    }
}
