using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.CodeGenUtil.Model;
using SqExpress.CodeGenUtil.Tables.MySQL;
using SqExpress.DataAccess;
using SqExpress.SqlExport;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.CodeGenUtil.DbManagers
{
    internal class MySqlDbManager : DbStrategyBase
    {
        private readonly string _databaseName;

        public MySqlDbManager(ISqDatabase database, string databaseName) : base(database)
        {
            this._databaseName = databaseName;
        }

        public static DbManager Create(string connectionString)
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

        public override Task<List<ColumnRawModel>> LoadColumns()
        {
            throw new NotImplementedException();
        }

        public override Task<LoadIndexesResult> LoadIndexes()
        {
            throw new NotImplementedException();
        }

        public override string DefaultSchemaName => "";

        public override DefaultValue? ParseDefaultValue(string? rawColumnDefaultValue)
        {
            throw new NotImplementedException();
        }

        public override Task<Dictionary<ColumnRef, List<ColumnRef>>> LoadForeignKeys()
        {
            throw new NotImplementedException();
        }

        public override ColumnType GetColType(ColumnRawModel raw)
        {
            throw new NotImplementedException();
        }
    }
}