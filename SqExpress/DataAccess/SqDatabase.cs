using SqExpress.DataAccess.Internal;
using SqExpress.DbMetadata;
using SqExpress.DbMetadata.Internal;
using SqExpress.DbMetadata.Internal.DbManagers;
using SqExpress.DbMetadata.Internal.DbManagers.MsSql;
using SqExpress.DbMetadata.Internal.DbManagers.MySql;
using SqExpress.DbMetadata.Internal.DbManagers.PgSql;
using SqExpress.DbMetadata.Internal.DbManagers.Sqlite;
using SqExpress.DbMetadata.Internal.Model;
using SqExpress.SqlExport;
using SqExpress.StatementSyntax;
using SqExpress.Syntax;
using SqExpress.SyntaxTreeOperations.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SqExpress.SqlExport.Internal;

namespace SqExpress.DataAccess
{
    /// <summary>Defines execution, transaction, and metadata operations for SqExpress syntax trees.</summary>
    /// <remarks>
    /// Implementations export expressions with their configured SQL dialect and parameterization policy. Query
    /// record readers are valid only during the callback or asynchronous enumeration step in which they are supplied.
    /// </remarks>
    public interface ISqDatabase : IDisposable
#if !NETSTANDARD
        , IAsyncDisposable
#endif
    {
        /// <summary>Begins a new transaction using the provider's default isolation level and makes it current for subsequent commands.</summary>
        /// <returns>An owned transaction that commits or rolls back this database's current transaction.</returns>
        ISqTransaction BeginTransaction();

        /// <summary>Begins a transaction when none exists, otherwise returns a non-owning proxy for the current transaction.</summary>
        /// <param name="isNewTransaction">Receives whether the returned transaction owns a newly started provider transaction.</param>
        /// <returns>The owned transaction or a proxy whose commit/rollback does not finish the outer transaction.</returns>
        ISqTransaction BeginTransactionOrUseExisting(out bool isNewTransaction);

        /// <summary>Begins a new current transaction at the requested provider isolation level.</summary>
        /// <param name="isolationLevel">The ADO.NET isolation level passed to the provider.</param>
        /// <returns>An owned database transaction.</returns>
        ISqTransaction BeginTransaction(IsolationLevel isolationLevel);

        /// <summary>Begins a transaction at the requested isolation level or returns a proxy for an existing transaction.</summary>
        /// <param name="isolationLevel">The isolation level used only when a new provider transaction is required.</param>
        /// <param name="isNewTransaction">Receives whether a new provider transaction was started.</param>
        /// <returns>An owned transaction or a non-owning proxy.</returns>
        ISqTransaction BeginTransactionOrUseExisting(IsolationLevel isolationLevel, out bool isNewTransaction);

        /// <summary>Executes a query asynchronously while synchronously folding each returned row into an accumulator.</summary>
        /// <typeparam name="TAgg">The accumulator/result type.</typeparam>
        /// <param name="query">The completed query syntax tree.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="aggregator">Called for each row before the reader advances; returns the next accumulator.</param>
        /// <param name="cancellationToken">Requests cancellation of command execution and reading.</param>
        /// <returns>A task containing the final accumulator.</returns>
        Task<TAgg> Query<TAgg>(IExprQuery query, TAgg seed, Func<TAgg, ISqDataRecordReader, TAgg> aggregator, CancellationToken cancellationToken = default);

        /// <summary>Executes a query and awaits an asynchronous accumulator callback for each returned row.</summary>
        /// <typeparam name="TAgg">The accumulator/result type.</typeparam>
        /// <param name="query">The completed query syntax tree.</param>
        /// <param name="seed">The initial accumulator value.</param>
        /// <param name="aggregator">Asynchronously processes the current row and returns the next accumulator.</param>
        /// <param name="cancellationToken">Requests cancellation of command execution and reading.</param>
        /// <returns>A task containing the final accumulator after every callback completes.</returns>
        Task<TAgg> Query<TAgg>(IExprQuery query, TAgg seed, Func<TAgg, ISqDataRecordReader, Task<TAgg>> aggregator, CancellationToken cancellationToken = default);

#if !NETSTANDARD
        /// <summary>Asynchronously begins a default-isolation transaction or returns a proxy for the current transaction.</summary>
        /// <returns>The transaction and a flag indicating whether it owns a newly started provider transaction.</returns>
        ValueTask<(ISqTransaction transaction, bool isNewTransaction)> BeginTransactionOrUseExistingAsync();

        /// <summary>Asynchronously begins or reuses a transaction, applying the isolation level only when starting one.</summary>
        /// <param name="isolationLevel">The requested provider isolation level for a new transaction.</param>
        /// <returns>The transaction and a flag indicating whether it is newly owned.</returns>
        ValueTask<(ISqTransaction transaction, bool isNewTransaction)> BeginTransactionOrUseExistingAsync(IsolationLevel isolationLevel);

        /// <summary>Asynchronously begins a new current transaction using the provider default isolation level.</summary>
        /// <returns>The owned transaction.</returns>
        ValueTask<ISqTransaction> BeginTransactionAsync();

        /// <summary>Asynchronously begins a new current transaction with the requested provider isolation level.</summary>
        /// <param name="isolationLevel">The ADO.NET isolation level passed to the provider.</param>
        /// <returns>The owned transaction.</returns>
        ValueTask<ISqTransaction> BeginTransactionAsync(IsolationLevel isolationLevel);

        /// <summary>Executes a query as a lazy asynchronous stream whose reader is valid only for the current iteration.</summary>
        /// <param name="query">The completed query syntax tree.</param>
        /// <param name="cancellationToken">Requests cancellation of execution and enumeration.</param>
        /// <returns>An asynchronous sequence of provider-backed row readers.</returns>
        IAsyncEnumerable<ISqDataRecordReader> Query(IExprQuery query, CancellationToken cancellationToken = default);
#endif

        /// <summary>Executes a query and returns the provider scalar result from its first row and column.</summary>
        /// <remarks>SQL null may be represented by <see cref="DBNull.Value"/>.</remarks>
        /// <param name="query">The completed query syntax tree.</param>
        /// <param name="cancellationToken">Requests cancellation of command execution.</param>
        /// <returns>A task containing the provider scalar result or its no-result/null representation.</returns>
        Task<object?> QueryScalar(IExprQuery query, CancellationToken cancellationToken = default);

        /// <summary>Exports and executes a non-query expression, discarding the provider affected-row count.</summary>
        /// <param name="statement">The completed insert, update, delete, merge, or DDL expression.</param>
        /// <param name="cancellationToken">Requests cancellation of command execution.</param>
        /// <returns>A task that completes when execution finishes.</returns>
        Task Exec(IExprExec statement, CancellationToken cancellationToken = default);

        /// <summary>Exports and executes a general or combined statement using the configured connection and transaction.</summary>
        /// <param name="statement">The statement tree, potentially containing multiple commands.</param>
        /// <param name="cancellationToken">Requests cancellation of command execution.</param>
        /// <returns>A task that completes when every statement finishes.</returns>
        Task Statement(IStatement statement, CancellationToken cancellationToken = default);

        /// <summary>Reads provider catalog metadata into SqExpress table models and rejects unmapped database column types.</summary>
        /// <param name="cancellationToken">Requests cancellation of metadata queries.</param>
        /// <returns>A task containing the discovered database tables and columns.</returns>
        Task<IReadOnlyList<SqTable>> GetTables(CancellationToken cancellationToken = default);

        /// <summary>Reads provider catalog metadata with caller-selected handling of database types SqExpress cannot map.</summary>
        /// <param name="skipUnknownColumnTypes">Whether to omit unsupported columns instead of failing metadata discovery.</param>
        /// <param name="cancellationToken">Requests cancellation of metadata queries.</param>
        /// <returns>A task containing the discovered tables and all retained columns.</returns>
        Task<IReadOnlyList<SqTable>> GetTables(bool skipUnknownColumnTypes, CancellationToken cancellationToken = default);
    }

    /// <summary>Represents a database transaction owned or proxied by an <see cref="ISqDatabase"/>.</summary>
    /// <remarks>A reused transaction proxy does not commit or roll back the owning outer transaction.</remarks>
    public interface ISqTransaction : IDisposable
#if !NETSTANDARD
        , IAsyncDisposable
#endif
    {
        /// <summary>Commits an owned transaction.</summary>
        void Commit();

        /// <summary>Rolls back an owned transaction.</summary>
        void Rollback();

#if !NETSTANDARD
        /// <summary>Asynchronously commits the provider transaction when this instance owns it; a reused proxy leaves the outer transaction active.</summary>
        /// <returns>A value task that completes when the provider commit finishes.</returns>
        ValueTask CommitAsync();

        /// <summary>Asynchronously rolls back the provider transaction when this instance owns it; a reused proxy leaves the outer transaction active.</summary>
        /// <returns>A value task that completes when the provider rollback finishes.</returns>
        ValueTask RollbackAsync();
#endif
    }

    /// <summary>Default SqExpress database implementation over an ADO.NET connection.</summary>
    /// <typeparam name="TConnection">The concrete provider connection type.</typeparam>
    /// <remarks>
    /// A closed connection is opened on demand and restored to its original closed state when the database is
    /// disposed. A connection supplied already open remains open. Set <c>disposeConnection</c> when this instance
    /// should also dispose the supplied connection.
    /// </remarks>
    public class SqDatabase<TConnection> : ISqDatabase where TConnection : DbConnection
    {
        private readonly TConnection _connection;

        private readonly bool _wasClosed;

        private readonly Func<TConnection, string, DbCommand> _commandFactory;

        private readonly ISqlExporter _sqlExporter;

        private readonly SemaphoreSlim _tranSyncSemaphore = new SemaphoreSlim(1, 1);

        private readonly bool _disposeConnection;

        private readonly ParametrizationMode _parametrizationMode;

        private SqTransaction? _currentTransaction;

        private int _isDisposed;

        /// <summary>Configures SqExpress execution over an existing provider connection and a matching SQL dialect exporter.</summary>
        /// <param name="connection">The provider connection used for commands and transactions; it is opened on demand when initially closed.</param>
        /// <param name="commandFactory">Creates a provider command for this connection and generated SQL text.</param>
        /// <param name="sqlExporter">Renders SqExpress syntax in the dialect accepted by the connection's database.</param>
        /// <param name="parametrizationMode">Controls which literal values are replaced with provider command parameters.</param>
        /// <param name="disposeConnection">Whether disposing this database wrapper also disposes the supplied connection.</param>
        public SqDatabase(
            TConnection connection,
            Func<TConnection, string, DbCommand> commandFactory,
            ISqlExporter sqlExporter,
            ParametrizationMode parametrizationMode,
            bool disposeConnection = false)
        {
            this._connection = connection;
            this._commandFactory = commandFactory;
            this._sqlExporter = sqlExporter;
            this._disposeConnection = disposeConnection;
            this._parametrizationMode = parametrizationMode;
            this._wasClosed = this._connection.State == ConnectionState.Closed;
        }


        /// <summary>Configures legacy non-parameterized execution; prefer the overload that requires an explicit parameterization mode.</summary>
        /// <param name="connection">The provider connection used for commands and transactions.</param>
        /// <param name="commandFactory">Creates a provider command for this connection and generated SQL text.</param>
        /// <param name="sqlExporter">Renders syntax in the database's SQL dialect.</param>
        /// <param name="disposeConnection">Whether disposing this wrapper also disposes the supplied connection.</param>
        [Obsolete("Specify parametrization mode")]
        public SqDatabase(
            TConnection connection, 
            Func<TConnection, string, DbCommand> commandFactory, 
            ISqlExporter sqlExporter, 
            bool disposeConnection=false)
        {
            this._connection = connection;
            this._commandFactory = commandFactory;
            this._sqlExporter = sqlExporter;
            this._disposeConnection = disposeConnection;
            this._parametrizationMode = ParametrizationMode.None;
            this._wasClosed = this._connection.State == ConnectionState.Closed;
        }

        public ISqTransaction BeginTransaction()
            => this.BeginTransaction(IsolationLevel.Unspecified);

        public ISqTransaction BeginTransactionOrUseExisting(out bool isNewTransaction)
            => this.BeginTransactionOrUseExisting(IsolationLevel.Unspecified, out isNewTransaction);

        public ISqTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            this.CheckDisposed();
            this._tranSyncSemaphore.Wait();
            try
            {
                if (this._currentTransaction != null)
                {
                    throw new SqExpressException("There is an already running transaction associated with this connection");
                }

                this._currentTransaction = new SqTransaction(this, isolationLevel);
                return this._currentTransaction;
            }
            finally
            {
                this._tranSyncSemaphore.Release();
            }
        }

#if !NETSTANDARD
        public ValueTask<ISqTransaction> BeginTransactionAsync()
            => this.BeginTransactionAsync(IsolationLevel.Unspecified);

        public async ValueTask<ISqTransaction> BeginTransactionAsync(IsolationLevel isolationLevel)
        {
            this.CheckDisposed();
            await this._tranSyncSemaphore.WaitAsync();
            try
            {
                if (this._currentTransaction != null)
                {
                    throw new SqExpressException("There is an already running transaction associated with this connection");
                }

                this._currentTransaction = new SqTransaction(this, isolationLevel);
                return this._currentTransaction;
            }
            finally
            {
                this._tranSyncSemaphore.Release();
            }
        }
#endif

        public ISqTransaction BeginTransactionOrUseExisting(IsolationLevel isolationLevel, out bool isNewTransaction)
        {
            this._tranSyncSemaphore.Wait();
            try
            {
                if (this._currentTransaction != null)
                {
                    isNewTransaction = false;
                    return new SqTransactionProxy(this);
                }
                isNewTransaction = true;
                this._currentTransaction = new SqTransaction(this, isolationLevel);
                return this._currentTransaction;
            }
            finally
            {
                this._tranSyncSemaphore.Release();
            }
        }
#if !NETSTANDARD
        public ValueTask<(ISqTransaction transaction, bool isNewTransaction)> BeginTransactionOrUseExistingAsync()
            => this.BeginTransactionOrUseExistingAsync(IsolationLevel.Unspecified);

        public async ValueTask<(ISqTransaction transaction, bool isNewTransaction)> BeginTransactionOrUseExistingAsync(IsolationLevel isolationLevel)
        {
            await this._tranSyncSemaphore.WaitAsync();
            try
            {
                if (this._currentTransaction != null)
                {
                    return (new SqTransactionProxy(this), false);
                }
                this._currentTransaction = new SqTransaction(this, isolationLevel);
                return (this._currentTransaction, true);
            }
            finally
            {
                this._tranSyncSemaphore.Release();
            }
        }
#endif

        public async Task<object?> QueryScalar(IExprQuery query, CancellationToken cancellationToken = default)
        {
            this.CheckDisposed();

            var command = await this.CreateCommand(query, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            object? result;
            try
            {
                result = await command.ExecuteScalarAsync(cancellationToken);
            }
            catch (Exception e)
            {
                throw new SqDatabaseCommandException(command.CommandText, e.Message, e);
            }
            return result;
        }

        public async Task<TAgg> Query<TAgg>(IExprQuery query, TAgg seed, Func<TAgg, ISqDataRecordReader, TAgg> aggregator, CancellationToken cancellationToken = default)
        {
            this.CheckDisposed();
            var result = seed;

            var command = await this.CreateCommand(query, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            DbDataReader? reader;
            try
            {
                reader = await command.ExecuteReaderAsync(cancellationToken);
            }
            catch (Exception e)
            {
                throw new SqDatabaseCommandException(command.CommandText, e.Message, e);
            }

#if !NETSTANDARD
            {
                await using (reader)
#else
            if (reader != null)
            {
                using (reader)
#endif
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var proxy = new DbReaderProxy(reader);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        result = aggregator(result, proxy);
                    }
                }
            }
            return result;
        }

        public async Task<TAgg> Query<TAgg>(IExprQuery query, TAgg seed, Func<TAgg, ISqDataRecordReader, Task<TAgg>> aggregator, CancellationToken cancellationToken = default)
        {
            this.CheckDisposed();
            var result = seed;

            var command = await this.CreateCommand(query, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            DbDataReader? reader;
            try
            {
                reader = await command.ExecuteReaderAsync(cancellationToken);
            }
            catch (Exception e)
            {
                throw new SqDatabaseCommandException(command.CommandText, e.Message, e);
            }

#if !NETSTANDARD
            {
                await using (reader)
#else
            if (reader != null)
            {
                using (reader)
#endif
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var proxy = new DbReaderProxy(reader);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        result = await aggregator(result, proxy);
                    }
                }
            }
            return result;
        }

#if !NETSTANDARD
        public async IAsyncEnumerable<ISqDataRecordReader> Query(IExprQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            this.CheckDisposed();

            var command = await this.CreateCommand(query, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            DbDataReader? reader;
            try
            {
                reader = await command.ExecuteReaderAsync(cancellationToken);
            }
            catch (Exception e)
            {
                throw new SqDatabaseCommandException(command.CommandText, e.Message, e);
            }

            await using (reader)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var proxy = new DbReaderProxy(reader);
                while (await reader.ReadAsync(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return proxy;
                }
            }
        }
#endif

        public async Task Exec(IExprExec statement, CancellationToken cancellationToken = default)
        {
            this.CheckDisposed();

            var command = await this.CreateCommand(statement, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception e)
            {
                throw new SqDatabaseCommandException(command.CommandText, e.Message, e);
            }
        }

        public async Task Statement(IStatement statement, CancellationToken cancellationToken = default)
        {
            this.CheckDisposed();

            var command = await this.CreateCommand(null, cancellationToken, statement);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception e)
            {
                throw new SqDatabaseCommandException(command.CommandText, e.Message, e);
            }
        }

        public Task<IReadOnlyList<SqTable>> GetTables(CancellationToken cancellationToken = default)
            => this.GetTables(skipUnknownColumnTypes: false, cancellationToken);

        public async Task<IReadOnlyList<SqTable>> GetTables(bool skipUnknownColumnTypes, CancellationToken cancellationToken = default)
        {
            this.CheckDisposed();

            if (string.IsNullOrEmpty(this._connection.Database) && this._sqlExporter is not SqliteExporter)
            {
                throw new SqExpressException("Connection should include a database name");
            }

            var databaseName = string.IsNullOrEmpty(this._connection.Database) ? "main" : this._connection.Database;

            IDbStrategy dbStrategy = this._sqlExporter switch
            {
                TSqlExporter => new MsSqlDbStrategy(this, databaseName),
                PgSqlExporter => new PgSqlDbStrategy(this, databaseName),
                MySqlExporter e => new MySqlDbStrategy(this, databaseName, e.Flavor),
                SqliteExporter => new SqliteDbStrategy(this, databaseName, this._connection),
                _ => throw new SqExpressException("Unknown sqlExporter")
            };

            var dbManager = new DbManager(dbStrategy, this._connection, new DbManagerOptions(""));

            IReadOnlyList<TableModel> tableModels;
            try
            {
                tableModels = await dbManager.SelectTables(skipUnknownColumnTypes);
            }
            catch (Exception e)
            {
                throw new SqExpressException("Could not read database metadata", e);
            }

            return DbModelMapper.ToSqDbTables(tableModels, skipUnknownColumnTypes);

        }

        public void Dispose()
        {
            if (Interlocked.Increment(ref this._isDisposed) != 1)
            {
                return;
            }

            try
            {
                this._tranSyncSemaphore.Wait();
                try
                {
                    if (this._currentTransaction != null)
                    {
                        this._currentTransaction.DbTransaction?.Dispose();
                        this._currentTransaction = null;
                    }
                }
                finally
                {
                    this._tranSyncSemaphore.Release();
                }
            }
            finally
            {
                if (!this._disposeConnection)
                {
                    if (this._wasClosed && this._connection.State == ConnectionState.Open)
                    {
                        this._connection.Close();
                    }
                }
                else
                {
                    this._connection.Dispose();
                }
            }
        }

#if !NETSTANDARD
        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Increment(ref this._isDisposed) != 1)
            {
                return;
            }

            try
            {
                await this._tranSyncSemaphore.WaitAsync();
                try
                {
                    if (this._currentTransaction != null)
                    {
                        if (this._currentTransaction.DbTransaction != null)
                        {
                            await this._currentTransaction.DbTransaction.DisposeAsync();
                        }
                        this._currentTransaction = null;
                    }
                }
                finally
                {
                    this._tranSyncSemaphore.Release();
                }
            }
            finally
            {
                if (!this._disposeConnection)
                {
                    if (this._wasClosed && this._connection.State == ConnectionState.Open)
                    {
                        await this._connection.CloseAsync();
                    }
                }
                else
                {
                    await this._connection.DisposeAsync();
                }
            }
        }
#endif

        private void CheckDisposed()
        {
            if (this._isDisposed > 0)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        private async Task<DbCommand> CreateCommand(IExpr? expr, CancellationToken cancellationToken, IStatement? statement = null)
        {
            IReadOnlyList<DbParameterValue>? parameters = null;
            string sql;
            if (expr != null)
            {
                if (this._sqlExporter is ISqlExporterInternal iInternal)
                {
                    expr = this.Parametrize(expr, iInternal.ParametersLimit);
                    sql = iInternal.ToSql(expr, out parameters);
                }
                else
                {
                    sql = this._sqlExporter.ToSql(expr);
                }
                    
            }
            else if(statement != null)
            {
                sql = this._sqlExporter.ToSql(statement);
            }
            else
            {
                throw new InvalidOperationException("Either expr or statement should be provided");
            }

            DbCommand command;
            await this._tranSyncSemaphore.WaitAsync(cancellationToken);
            try
            {
                //Opening the connection is also thread safe
                if (this._wasClosed && this._connection.State == ConnectionState.Closed)
                {
                    await this._connection.OpenAsync(cancellationToken);
                }

                command = this._commandFactory.Invoke(this._connection, sql);

                if (parameters?.Count > 0)
                {
                    foreach (var parameter in parameters)
                    {
                        var p = command.CreateParameter();
                        p.Value = parameter.Value;
                        p.DbType = parameter.Type;
                        p.ParameterName = parameter.Name;
                        command.Parameters.Add(p);
                    }
                }

                if (command.Transaction != null && this._currentTransaction != null)
                {
                    throw new SqDatabaseCommandException(sql, "Command factory provided a command with already set transaction", null);
                }

                if (this._currentTransaction != null)
                {
#if NETSTANDARD
                    command.Transaction = this._currentTransaction.StartTransactionIfNecessary();
#else
                    command.Transaction = await this._currentTransaction.StartTransactionIfNecessaryAsync();
#endif
                }
            }
            finally
            {
                this._tranSyncSemaphore.Release();
            }

            return command;
        }

        private IExpr Parametrize(IExpr expr, int limit)
        {
            if (this._parametrizationMode == ParametrizationMode.None)
            {
                return expr;
            }

            expr = expr.SyntaxTree().ParametrizeLiterals(limit, out var numOfParams, out var skips);

            if (skips > 0 && this._parametrizationMode == ParametrizationMode.ThrowOnLimit)
            {
                throw new SqExpressException($"Number of parameters ({numOfParams + skips}) in the expression exceeds the limit {limit}");
            }

            return expr;
        }

        private void ReleaseTransaction()
        {
            this._tranSyncSemaphore.Wait();
            try
            {
                if (this._currentTransaction == null)
                {
                    throw new SqExpressException("Could not find any running transaction associated with this connection");
                }
                this._currentTransaction.DbTransaction?.Dispose();
                this._currentTransaction = null;
            }
            finally
            {
                this._tranSyncSemaphore.Release();
            }
        }
#if !NETSTANDARD
        private async ValueTask ReleaseTransactionAsync()
        {
            await this._tranSyncSemaphore.WaitAsync();
            try
            {
                if (this._currentTransaction == null)
                {
                    throw new SqExpressException("Could not find any running transaction associated with this connection");
                }

                if (this._currentTransaction.DbTransaction != null)
                {
                    await this._currentTransaction.DbTransaction.DisposeAsync();
                }
                this._currentTransaction = null;
            }
            finally
            {
                this._tranSyncSemaphore.Release();
            }
        }
#endif

        private class SqTransaction : ISqTransaction
        {
            private readonly SqDatabase<TConnection> _host;

            private readonly IsolationLevel _isolationLevel;

            public DbTransaction? DbTransaction;

            public SqTransaction(SqDatabase<TConnection> host, IsolationLevel isolationLevel)
            {
                this._host = host;
                this._isolationLevel = isolationLevel;
            }

#if NETSTANDARD
            public DbTransaction StartTransactionIfNecessary()
            {
                //This method is thread safe since it is called under lock
                if (this.DbTransaction == null)
                {
                    if (this._host._connection.State != ConnectionState.Open)
                    {
                        throw new SqExpressException("Connection should be opened");
                    }

                    this.DbTransaction = this._host._connection.BeginTransaction(this._isolationLevel);
                }
                return this.DbTransaction;

            }
#endif

            public void Commit()
            {
                if (this.DbTransaction == null)
                {
                    throw new SqExpressException("Could not commit not started transaction");
                }
                this.DbTransaction.Commit();
            }

            public void Rollback()
            {
                if (this.DbTransaction == null)
                {
                    throw new SqExpressException("Could not rollback not started transaction");
                }
                this.DbTransaction.Rollback();
            }

            public void Dispose()
            {
                this._host.ReleaseTransaction();
            }

#if !NETSTANDARD
            public async ValueTask<DbTransaction> StartTransactionIfNecessaryAsync()
            {
                //This method is thread safe since it is called under lock
                if (this.DbTransaction == null)
                {
                    if (this._host._connection.State != ConnectionState.Open)
                    {
                        throw new SqExpressException("Connection should be opened");
                    }

                    this.DbTransaction = await this._host._connection.BeginTransactionAsync(this._isolationLevel);
                }
                return this.DbTransaction;
            }

            public async ValueTask CommitAsync()
            {
                if (this.DbTransaction == null)
                {
                    throw new SqExpressException("Could not commit not started transaction");
                }
                await this.DbTransaction.CommitAsync();
            }

            public async ValueTask RollbackAsync()
            {
                if (this.DbTransaction == null)
                {
                    throw new SqExpressException("Could not rollback not started transaction");
                }
                await this.DbTransaction.RollbackAsync();
            }

            public async ValueTask DisposeAsync()
            {
                await this._host.ReleaseTransactionAsync();
            }
#endif
        }

        private class SqTransactionProxy : ISqTransaction
        {
            private readonly SqDatabase<TConnection> _host;

            public SqTransactionProxy(SqDatabase<TConnection> host)
            {
                this._host = host;
            }

            public void Dispose() => this.ThrowIfDisposed();

            public void Commit() => this.ThrowIfDisposed();

            public void Rollback() => this.ThrowIfDisposed();

#if !NETSTANDARD
            public ValueTask CommitAsync()
            {
                this.ThrowIfDisposed();
                return ValueTask.CompletedTask;
            }

            public ValueTask RollbackAsync()
            {
                this.ThrowIfDisposed();
                return ValueTask.CompletedTask;
            }

            public ValueTask DisposeAsync()
            {
                this.ThrowIfDisposed();
                return ValueTask.CompletedTask;
            }
#endif

            private void ThrowIfDisposed()
            {
                if (this._host._currentTransaction == null)
                {
                    throw new SqExpressException("Could not dispose already disposed transaction");
                }
            }
        }
    }
}
