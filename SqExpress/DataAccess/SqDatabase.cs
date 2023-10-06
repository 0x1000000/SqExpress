using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using SqExpress.DataAccess.Internal;
using SqExpress.DbMetadata;
using SqExpress.DbMetadata.Internal;
using SqExpress.DbMetadata.Internal.DbManagers;
using SqExpress.DbMetadata.Internal.Model;
using SqExpress.SqlExport;
using SqExpress.StatementSyntax;

namespace SqExpress.DataAccess
{
    public interface ISqDatabase : IDisposable
    {
        ISqTransaction BeginTransaction();

        ISqTransaction BeginTransactionOrUseExisting(out bool isNewTransaction);

        ISqTransaction BeginTransaction(IsolationLevel isolationLevel);

        ISqTransaction BeginTransactionOrUseExisting(IsolationLevel isolationLevel, out bool isNewTransaction);

        Task<TAgg> Query<TAgg>(IExprQuery query, TAgg seed, Func<TAgg, ISqDataRecordReader, TAgg> aggregator, CancellationToken cancellationToken = default);

        Task<TAgg> Query<TAgg>(IExprQuery query, TAgg seed, Func<TAgg, ISqDataRecordReader, Task<TAgg>> aggregator, CancellationToken cancellationToken = default);

        Task<object?> QueryScalar(IExprQuery query, CancellationToken cancellationToken = default);

        Task Exec(IExprExec statement, CancellationToken cancellationToken = default);

        Task Statement(IStatement statement, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SqTable>> GetTables(CancellationToken cancellationToken = default);
    }

    public interface ISqTransaction : IDisposable
    {
        void Commit();

        void Rollback();
    }

    public class SqDatabase<TConnection> : ISqDatabase where TConnection : DbConnection
    {
        private readonly TConnection _connection;

        private readonly bool _wasClosed;

        private readonly Func<TConnection, string, DbCommand> _commandFactory;

        private readonly ISqlExporter _sqlExporter;

        private readonly object _tranSync = new object();

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

        public ISqTransaction BeginTransaction() => this.BeginTransaction(IsolationLevel.Unspecified);

        public ISqTransaction BeginTransactionOrUseExisting(out bool isNewTransaction)
            => BeginTransactionOrUseExisting(IsolationLevel.Unspecified, out isNewTransaction);

        public ISqTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            this.CheckDisposed();
            lock (this._tranSync)
            {
                if (this._currentTransaction != null)
                {
                    throw new SqExpressException("There is an already running transaction associated with this connection");
                }
                this._currentTransaction = new SqTransaction(this, isolationLevel);
                return this._currentTransaction;
            }
        }

        public ISqTransaction BeginTransactionOrUseExisting(IsolationLevel isolationLevel, out bool isNewTransaction)
        {
            lock (this._tranSync)
            {
                if (this._currentTransaction != null)
                {
                    isNewTransaction = false;
                    return new SqTransactionProxy(this);
                }
                isNewTransaction = true;
                return this.BeginTransaction(isolationLevel);
            }
        }

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

            cancellationToken.ThrowIfCancellationRequested();

            if (reader != null)
            {
                using (reader)
                {
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

            if (cancellationToken.IsCancellationRequested)
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (reader != null)
            {
                using (reader)
                {
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

            if (this._sqlExporter is not TSqlExporter)
            {
                throw new SqExpressException("Only MS SQL is currently supported");
            }

            if (string.IsNullOrEmpty(this._connection.Database))
            {
                throw new SqExpressException("Connection should include a database name");
            }


            var dbManager = new DbManager(new MsSqlDbManager(this, this._connection.Database), this._connection, new DbManagerOptions(""));

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
                lock (this._tranSync)
                {
                    if (this._currentTransaction != null)
                    {
                        this._currentTransaction.DbTransaction?.Dispose();
                        this._currentTransaction = null;
                    }
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

        private void CheckDisposed()
        {
            if (this._isDisposed > 0)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private async Task<DbCommand> CreateCommand(string sql, CancellationToken cancellationToken)
        {
            var connection = await this.GetOpenedConnection(cancellationToken);
            var command = this._commandFactory.Invoke(connection, sql);

            lock (this._tranSync)
            {
                if (command.Transaction != null && this._currentTransaction != null)
                {
                    throw new SqDatabaseCommandException(sql, "Command factory provided a command with already set transaction", null);
                }

                if (this._currentTransaction != null)
                {
                    command.Transaction = this._currentTransaction.StartTransactionIfNecessary();
                }
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
            lock (this._tranSync)
            {
                if (this._currentTransaction == null)
                {
                    throw new SqExpressException("Could not find any running transaction associated with this connection");
                }
                this._currentTransaction.DbTransaction?.Dispose();
                this._currentTransaction = null;
            }
        }

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

            public DbTransaction StartTransactionIfNecessary()
            {
                //This method is thread safe since it is called under lock
                return this.DbTransaction ??= this._host._connection.BeginTransaction(this._isolationLevel);
            }

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
        }

        private class SqTransactionProxy : ISqTransaction
        {
            private readonly SqDatabase<TConnection> _host;

            public SqTransactionProxy(SqDatabase<TConnection> host)
            {
                this._host = host;
            }

            public void Dispose()
            {
                if (this._host._currentTransaction == null)
                {
                    throw new SqExpressException("Could not dispose already disposed transaction");
                }
            }

            public void Commit()
            {
                if (this._host._currentTransaction == null)
                {
                    throw new SqExpressException("Could not commit already disposed transaction");
                }
            }

            public void Rollback()
            {
                if (this._host._currentTransaction == null)
                {
                    throw new SqExpressException("Could not rollback already disposed transaction");
                }
            }
        }
    }
}