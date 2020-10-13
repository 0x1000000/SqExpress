using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using SqExpress.DataAccess.Internal;
using SqExpress.SqlExport;
using SqExpress.StatementSyntax;

namespace SqExpress.DataAccess
{
    public interface ISqDatabase : IDisposable
    {
        Task<TAgg> Query<TAgg>(IExprQuery query, TAgg seed, Func<TAgg, ISqDataRecordReader, TAgg> aggregator);

        Task<object> QueryScalar(IExprQuery query);

        Task Exec(IExprExec statement);

        Task Statement(IStatement statement);
    }

    public class SqDatabase<TConnection> : ISqDatabase where TConnection : DbConnection
    {
        private readonly TConnection _connection;

        private readonly bool _wasClosed;

        private readonly Func<TConnection, string, DbCommand> _commandFactory;

        private readonly ISqlExporter _sqlExporter;

        public SqDatabase(TConnection connection, Func<TConnection, string, DbCommand> commandFactory, ISqlExporter sqlExporter)
        {
            this._connection = connection;
            this._commandFactory = commandFactory;
            this._sqlExporter = sqlExporter;
            this._wasClosed = this._connection.State == ConnectionState.Closed;
        }

        public async Task<object> QueryScalar(IExprQuery query)
        {
            var sql = this._sqlExporter.ToSql(query);
            var connection = await this.GetOpenedConnection();
            var command = this._commandFactory.Invoke(connection, sql);
            object reader;
            try
            {
                reader = await command.ExecuteScalarAsync();
            }
            catch (Exception e)
            {
                throw new SqDatabaseCommandException(sql, e.Message, e);
            }
            return reader;
        }

        public async Task<TAgg> Query<TAgg>(IExprQuery query, TAgg seed, Func<TAgg, ISqDataRecordReader, TAgg> aggregator)
        {
            var result = seed;
            var sql = this._sqlExporter.ToSql(query);

            var connection = await this.GetOpenedConnection();
            var command = this._commandFactory.Invoke(connection, sql);
            DbDataReader? reader;
            try
            {
                reader = await command.ExecuteReaderAsync();
            }
            catch (Exception e)
            {
                throw new SqDatabaseCommandException(sql, e.Message, e);
            }
            if (reader != null)
            {
                using (reader)
                {
                    var proxy = new DbReaderProxy(reader);
                    while (await reader.ReadAsync())
                    {
                        result = aggregator(result, proxy);
                    }
                }
            }
            return result;
        }

        public async Task Exec(IExprExec statement)
        {
            var sql = this._sqlExporter.ToSql(statement);

            var connection = await this.GetOpenedConnection();
            var command = this._commandFactory.Invoke(connection, sql);
            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                throw new SqDatabaseCommandException(sql, e.Message, e);
            }
        }

        public async Task Statement(IStatement statement)
        {
            var sql = this._sqlExporter.ToSql(statement);
            var connection = await this.GetOpenedConnection();
            var command = this._commandFactory.Invoke(connection, sql);
            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception e)
            {
                throw new SqDatabaseCommandException(sql, e.Message, e);
            }
        }

        private async Task<TConnection> GetOpenedConnection()
        {
            if (_wasClosed && this._connection.State == ConnectionState.Closed)
            {
                await this._connection.OpenAsync();
            }
            return this._connection;
        }

        public void Dispose()
        {
            if (this._wasClosed && this._connection.State == ConnectionState.Open)
            {
                this._connection.Close();
            }
        }
    }
}