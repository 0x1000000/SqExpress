using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SqExpress.CodeGenUtil.Model;
using SqExpress.CodeGenUtil.Tables.MySQL;
using SqExpress.DataAccess;
using SqExpress.SqlExport;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.CodeGenUtil.DbManagers
{
    internal class MySqlDbManager : DbManager
    {
        private readonly string _databaseName;

        private MySqlDbManager(MySqlConnection connection, ISqDatabase database, string databaseName) : base(connection, database)
        {
            this._databaseName = databaseName;
        }

        public static MySqlDbManager Create(string connectionString)
        {
            throw new SqExpressCodeGenException("MySql is not yet supported");

            //var connection = new MySqlConnection(connectionString);
            //if (string.IsNullOrEmpty(connection.Database))
            //{
            //    throw new SqExpressCodeGenException("MySQL connection string has to contain \"database\" attribute");
            //}
            //try
            //{
            //    var database = new SqDatabase<MySqlConnection>(connection, (conn, sql) =>
            //    {
            //        //Console.WriteLine(sql);
            //        return new MySqlCommand(sql, conn) { Transaction = null };
            //    }, new MySqlExporter(SqlBuilderOptions.Default));

            //    return new MySqlDbManager(connection, database, connection.Database);
            //}
            //catch
            //{
            //    connection.Dispose();
            //    throw;
            //}
        }

        public override async Task<IReadOnlyList<TableModel>> SelectTables()
        {
            var tColumns = new MySqlISColumns();
            var tTables = new MySqlIsTables();

            var filter = tTables.TableName == tColumns.TableName
                         & tTables.TableSchema == tColumns.TableSchema
                         & tTables.TableType == "BASE TABLE"
                         & tTables.TableSchema == this._databaseName;

            var tablesDic = await Select(tColumns.AllColumns())
                .From(tColumns)
                .Where(Exists(SelectOne().From(tTables).Where(filter)))
                .Query(this.Database, new Dictionary<TableNameModel, List<ColumnModel>>(),
                    (acc, r) => AggregateColumns(MySqlIsColumnsDto.FromRecord(r, tColumns), true, acc));

            return tablesDic.Select(kv => new TableModel(kv.Key, kv.Value)).ToList();
        }
    }
}