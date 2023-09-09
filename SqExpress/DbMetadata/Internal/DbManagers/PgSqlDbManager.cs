using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.DbMetadata.Internal.DbManagers
{
    internal class PgSqlDbManager : DbStrategyBase
    {
        private readonly string _databaseName;

        private PgSqlDbManager(ISqDatabase database, string databaseName) : base(database)
        {
            _databaseName = databaseName;
        }

        public static DbManager Create(string connectionString)
        {
            throw new SqExpressException("Pg Sql is not yet supported");
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