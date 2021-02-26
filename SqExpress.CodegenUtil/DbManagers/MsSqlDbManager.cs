using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.CodeGenUtil.Model;
using SqExpress.CodeGenUtil.Tables.MsSql;
using SqExpress.DataAccess;
using SqExpress.SqlExport;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.CodeGenUtil.DbManagers
{
    internal class MsSqlDbManager : DbManager
    {
        private readonly string _databaseName;

        private MsSqlDbManager(SqlConnection connection, ISqDatabase database, string databaseName) : base(connection, database)
        {
            this._databaseName = databaseName;
        }

        public static MsSqlDbManager Create(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            if (string.IsNullOrEmpty(connection.Database))
            {
                throw new SqExpressCodeGenException("MsSQL connection string has to contain \"database\" attribute");
            }
            try
            {
                var database = new SqDatabase<SqlConnection>(connection, (conn, sql) =>
                {
                    //Console.WriteLine(sql);
                    return new SqlCommand(sql, conn) { Transaction = null };
                }, new TSqlExporter(SqlBuilderOptions.Default));

                return new MsSqlDbManager(connection, database, connection.Database);
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        public override async Task<IReadOnlyList<TableModel>> SelectTables()
        {
            var tColumns = new MsSqlIsColumns();
            var tTables = new MsSqlIsTables();

            var filter = tTables.TableName == tColumns.TableName
                         & tTables.TableSchema == tColumns.TableSchema
                         & tTables.TableType == "BASE TABLE"
                         & tTables.TableCatalog == this._databaseName;

            var tablesDic = await Select(tColumns.AllColumns())
                .From(tColumns)
                .Where(Exists(SelectOne().From(tTables).Where(filter)))
                .Query(this.Database, new Dictionary<TableNameModel, List<ColumnModel>>(),
                    (acc, r) => AggregateColumns(MsSqlIsColumnsDto.FromRecord(r, tColumns), false, acc));

            return tablesDic.Select(kv => new TableModel(kv.Key, kv.Value)).ToList();
        }
    }
}