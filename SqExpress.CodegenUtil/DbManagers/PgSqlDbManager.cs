using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using SqExpress.CodeGenUtil.Model;
using SqExpress.CodeGenUtil.Tables.MsSql;
using SqExpress.DataAccess;
using SqExpress.SqlExport;

namespace SqExpress.CodeGenUtil.DbManagers
{
    internal class PgSqlDbManager : DbStrategyBase
    {
        private readonly string _databaseName;

        private PgSqlDbManager(ISqDatabase database, string databaseName) : base(database)
        {
            this._databaseName = databaseName;
        }

        public static DbManager Create(string connectionString)
        {
            throw new SqExpressCodeGenException("Pg Sql is not yet supported");
            //var connection = new NpgsqlConnection(connectionString);
            //if (string.IsNullOrEmpty(connection.Database))
            //{
            //    throw new SqExpressCodeGenException("PgSQL connection string has to contain \"database\" attribute");
            //}
            //try
            //{
            //    var database = new SqDatabase<NpgsqlConnection>(connection, (conn, sql) =>
            //    {
            //        Console.WriteLine(sql);
            //        return new NpgsqlCommand(sql, conn) { Transaction = null };
            //    }, new PgSqlExporter(SqlBuilderOptions.Default));

            //    return new PgSqlDbManager(connection, database, connection.Database);
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

        public override string DefaultSchemaName => "public";

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