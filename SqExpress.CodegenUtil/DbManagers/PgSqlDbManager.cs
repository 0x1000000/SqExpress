﻿using System;
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
    internal class PgSqlDbManager : DbManager
    {
        private readonly string _databaseName;

        private PgSqlDbManager(NpgsqlConnection connection, ISqDatabase database, string databaseName) : base(connection, database)
        {
            this._databaseName = databaseName;
        }

        public static PgSqlDbManager Create(string connectionString)
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

        public override async Task<IReadOnlyList<TableModel>> SelectTables()
        {
            var tColumns = new MsSqlIsColumns();
            var tTables = new MsSqlIsTables();

            var filter = tTables.TableName == tColumns.TableName
                         & tTables.TableSchema == tColumns.TableSchema
                         & tTables.TableType == "BASE TABLE"
                         & tTables.TableCatalog == this._databaseName;

            var tablesDic = await SqQueryBuilder.Select(tColumns.AllColumns())
                .From(tColumns)
                .Where(SqQueryBuilder.Exists(SqQueryBuilder.SelectOne().From(tTables).Where(filter)))
                .Query(this.Database, new Dictionary<TableNameModel, List<ColumnModel>>(),
                    (acc, r) => AggregateColumns(MsSqlIsColumnsDto.FromRecord(r, tColumns), false, acc));

            return tablesDic.Select(kv => new TableModel(kv.Key, kv.Value)).ToList();
        }
    }
}