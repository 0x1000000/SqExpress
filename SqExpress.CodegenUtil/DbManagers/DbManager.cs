using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using SqExpress.CodeGenUtil.Model;
using SqExpress.DataAccess;

namespace SqExpress.CodeGenUtil.DbManagers
{
    internal abstract class DbManager : IDisposable
    {
        private readonly DbConnection _connection;

        protected readonly ISqDatabase Database;

        protected DbManager(DbConnection connection, ISqDatabase database)
        {
            this._connection = connection;
            this.Database = database;
        }

        public async Task<string?> TryOpenConnection()
        {
            try
            {
                await this._connection.OpenAsync();
                await this._connection.CloseAsync();
                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public abstract Task<IReadOnlyList<TableModel>> SelectTables();

        public void Dispose()
        {
            try
            {
                this.Database.Dispose();
            }
            finally
            {
                this._connection.Dispose();
            }
        }
    }
}