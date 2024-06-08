using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SqExpress.DataAccess.Internal;
using SqExpress.DbMetadata;
using SqExpress.DbMetadata.Internal;
using SqExpress.DbMetadata.Internal.DbManagers;
using SqExpress.DbMetadata.Internal.DbManagers.MsSql;
using SqExpress.DbMetadata.Internal.DbManagers.MySql;
using SqExpress.DbMetadata.Internal.DbManagers.PgSql;
using SqExpress.DbMetadata.Internal.Model;
using SqExpress.SqlExport;
using SqExpress.StatementSyntax;

namespace SqExpress.DataAccess
{
    public interface ISqDatabase : IDisposable
#if !NETSTANDARD
        , IAsyncDisposable
#endif
    {
        ISqTransaction BeginTransaction();

        ISqTransaction BeginTransactionOrUseExisting(out bool isNewTransaction);

        ISqTransaction BeginTransaction(IsolationLevel isolationLevel);

        ISqTransaction BeginTransactionOrUseExisting(IsolationLevel isolationLevel, out bool isNewTransaction);

        Task<TAgg> Query<TAgg>(IExprQuery query, TAgg seed, Func<TAgg, ISqDataRecordReader, TAgg> aggregator, CancellationToken cancellationToken = default);

        Task<TAgg> Query<TAgg>(IExprQuery query, TAgg seed, Func<TAgg, ISqDataRecordReader, Task<TAgg>> aggregator, CancellationToken cancellationToken = default);

#if !NETSTANDARD
        ValueTask<(ISqTransaction transaction, bool isNewTransaction)> BeginTransactionOrUseExistingAsync();

        ValueTask<(ISqTransaction transaction, bool isNewTransaction)> BeginTransactionOrUseExistingAsync(IsolationLevel isolationLevel);

        ValueTask<ISqTransaction> BeginTransactionAsync(IsolationLevel isolationLevel);

        IAsyncEnumerable<ISqDataRecordReader> Query(IExprQuery query, CancellationToken cancellationToken = default);
#endif

        Task<object?> QueryScalar(IExprQuery query, CancellationToken cancellationToken = default);

        Task Exec(IExprExec statement, CancellationToken cancellationToken = default);

        Task Statement(IStatement statement, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SqTable>> GetTables(CancellationToken cancellationToken = default);
    }

    public interface ISqTransaction : IDisposable
#if !NETSTANDARD
        , IAsyncDisposable
#endif
    {
        void Commit();

        void Rollback();

#if !NETSTANDARD
        ValueTask CommitAsync();

        ValueTask RollbackAsync();
#endif
    }

    public class SqDatabase<TConnection> : ISqDatabase where TConnection : DbConnection
    {
        private readonly TConnection _connection;

        private readonly bool _wasClosed;

        private readonly Func<TConnection, string, DbCommand> _commandFactory;

        private readonly ISqlExporter _sqlExporter;

        private readonly SemaphoreSlim _tranSyncSemaphore = new SemaphoreSlim(1, 1);

        private readonly bool _disposeConnection;

        private SqTransaction? _currentTransaction;

        private int _isDisposed;

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
                    return (new SqTransactionProxy(this), false);
                else
                {
                    this._currentTransaction = new SqTransaction(this, isolationLevel);
                    return (this._currentTransaction, true);
                }
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
            var sql = this._sqlExporter.ToSql(query);

            var command = await this.CreateCommand(sql, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            object? result;
            try
            {
                result = await command.ExecuteScalarAsync(cancellationToken);
            }
            catch (Exception e)
            {
                throw new SqDatabaseCommandException(sql, e.Message, e);
            }
            return result;
        }

        public async Task<TAgg> Query<TAgg>(IExprQuery query, TAgg seed, Func<TAgg, ISqDataRecordReader, TAgg> aggregator, CancellationToken cancellationToken = default)
        {
            this.CheckDisposed();
            var result = seed;
            var sql = this._sqlExporter.ToSql(query);

            var command = await this.CreateCommand(sql, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            DbDataReader? reader;
            try
            {
                reader = await command.ExecuteReaderAsync(cancellationToken);
            }
            catch (Exception e)
            {
                throw new SqDatabaseCommandException(sql, e.Message, e);
            }

            if (reader != null)
            {
                using (reader)
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
            var sql = this._sqlExporter.ToSql(query);

            var command = await this.CreateCommand(sql, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            DbDataReader? reader;
            try
            {
                reader = await command.ExecuteReaderAsync(cancellationToken);
            }
            catch (Exception e)
            {
                throw new SqDatabaseCommandException(sql, e.Message, e);
            }

            if (reader != null)
            {
                using (reader)
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
            var sql = this._sqlExporter.ToSql(query);

            var command = await this.CreateCommand(sql, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            DbDataReader? reader;
            try
            {
                reader = await command.ExecuteReaderAsync(cancellationToken);
            }
            catch (Exception e)
            {
                throw new SqDatabaseCommandException(sql, e.Message, e);
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
            var sql = this._sqlExporter.ToSql(statement);

            var command = await this.CreateCommand(sql, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception e)
            {
                throw new SqDatabaseCommandException(sql, e.Message, e);
            }
        }

        public async Task Statement(IStatement statement, CancellationToken cancellationToken = default)
        {
            this.CheckDisposed();
            var sql = this._sqlExporter.ToSql(statement);

            var command = await this.CreateCommand(sql, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception e)
            {
                throw new SqDatabaseCommandException(sql, e.Message, e);
            }
        }

        public async Task<IReadOnlyList<SqTable>> GetTables(CancellationToken cancellationToken = default)
        {
            this.CheckDisposed();

            if (string.IsNullOrEmpty(this._connection.Database))
            {
                throw new SqExpressException("Connection should include a database name");
            }

            IDbStrategy dbStrategy = this._sqlExporter switch
            {
                TSqlExporter => new MsSqlDbStrategy(this, this._connection.Database),
                PgSqlExporter => new PgSqlDbStrategy(this, this._connection.Database),
                MySqlExporter => new MySqlDbStrategy(this, this._connection.Database),
                _ => throw new SqExpressException("Unknown sqlExporter")
            };

            var dbManager = new DbManager(dbStrategy, this._connection, new DbManagerOptions(""));

            IReadOnlyList<TableModel> tableModels;
            try
            {
                tableModels = await dbManager.SelectTables();
            }
            catch (Exception e)
            {
                throw new SqExpressException("Could not read database metadata", e);
            }

            return DbModelMapper.ToSqDbTables(tableModels);

        }

        public void Dispose()
        {
            if (Interlocked.Increment(ref this._isDisposed) != 1)
            {
                return;
            }

            try
            {
                //lock (this._tranSync)
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

        private async Task<DbCommand> CreateCommand(string sql, CancellationToken cancellationToken)
        {
            var connection = await this.GetOpenedConnection(cancellationToken);
            var command = this._commandFactory.Invoke(connection, sql);

            await this._tranSyncSemaphore.WaitAsync(cancellationToken);
            try
            {
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

        private async Task<TConnection> GetOpenedConnection(CancellationToken cancellationToken)
        {
            if (this._wasClosed && this._connection.State == ConnectionState.Closed)
            {
                await this._connection.OpenAsync(cancellationToken);
            }
            return this._connection;
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
                return this.DbTransaction ??= this._host._connection.BeginTransaction(this._isolationLevel);
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
                return this.DbTransaction ??= await this._host._connection.BeginTransactionAsync(this._isolationLevel);
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