using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SqExpress.CodeGenUtil.Model;
using SqExpress.CodeGenUtil.Tables;
using SqExpress.DataAccess;

namespace SqExpress.CodeGenUtil.DbManagers
{
    internal abstract class DbManager : IDisposable
    {
        protected readonly DbConnection _connection;
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

        protected static Dictionary<TableNameModel, List<ColumnModel>> AggregateColumns(IIsColumnsDto dto, bool emptySchema, Dictionary<TableNameModel, List<ColumnModel>> acc)
        {
            var tableName = new TableNameModel(Name: dto.TableName, Schema: emptySchema ? "" : dto.TableSchema);

            if (!acc.TryGetValue(tableName, out var columnList))
            {
                columnList = new List<ColumnModel>();
                acc.Add(tableName, columnList);
            }

            var dtoDataType = dto.DataType ??
                              throw new SqExpressCodeGenException("Data Type is empty for column: " + dto.ColumnName);
            columnList.Add(new ColumnModel(dto.ColumnName, dtoDataType));

            return acc;
        }
    }
}