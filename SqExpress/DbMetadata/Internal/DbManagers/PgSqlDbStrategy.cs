using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.DbMetadata.Internal.Model;
using SqExpress.DbMetadata.Internal.Tables.PgSql;
using SqExpress.SqlExport;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Select;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.DbMetadata.Internal.DbManagers
{
    internal class PgSqlDbStrategy : DbStrategyBase
    {
        private readonly string _databaseName;

        internal PgSqlDbStrategy(ISqDatabase database, string databaseName) : base(database)
        {
            _databaseName = databaseName;
        }

        public static DbManager Create(DbManagerOptions options, DbConnection connection)
        {
            try
            {
                var database = new SqDatabase<DbConnection>(connection, ConnectionFactory, new PgSqlExporter(SqlBuilderOptions.Default));

                return new DbManager(new PgSqlDbStrategy(database, connection.Database), connection, options);
            }
            catch
            {
                connection.Dispose();
                throw;
            }

            static DbCommand ConnectionFactory(DbConnection connection, string sql)
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                return cmd;
            }
        }

        public override Task<List<ColumnRawModel>> LoadColumns()
        {
            var tColumns = new PgSqlIsColumns();

            return Select(
                    tColumns.TableSchema,
                    tColumns.TableName,
                    tColumns.ColumnName,
                    tColumns.OrdinalPosition,
                    tColumns.DataType,
                    tColumns.ColumnDefault,
                    tColumns.IsNullable,
                    tColumns.IsIdentity,
                    tColumns.CharacterMaximumLength,
                    tColumns.NumericPrecision,
                    tColumns.NumericScale
                )
                .From(tColumns)
                .Where(this.GetTableFilter(tColumns))
                .OrderBy(tColumns.OrdinalPosition)
                .QueryList(this.Database,
                    r => new ColumnRawModel(
                        dbName: new ColumnRef(schema: tColumns.TableSchema.Read(recordReader: r),
                            tableName: tColumns.TableName.Read(recordReader: r),
                            name: tColumns.ColumnName.Read(recordReader: r)),
                        ordinalPosition: tColumns.OrdinalPosition.Read(r),
                        nullable: tColumns.IsNullable.Read(recordReader: r) == "YES",
                        identity: tColumns.IsIdentity.Read(recordReader: r) == "YES",
                        typeName: tColumns.DataType.Read(recordReader: r) ?? "",
                        defaultValue: tColumns.ColumnDefault.Read(recordReader: r),
                        size: tColumns.CharacterMaximumLength.Read(recordReader: r),
                        precision: tColumns.NumericPrecision.Read(recordReader: r),
                        scale: tColumns.NumericScale.Read(recordReader: r)));
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

        private ExprBoolean GetTableFilter(IPgSqlTableColumns tColumns)
        {
            var tTables = new PgSqlTables();

            var filter = tTables.TableName == tColumns.TableName
                         & tTables.TableSchema == tColumns.TableSchema
                         & tTables.TableType == "BASE TABLE"
                         & tTables.TableCatalog == _databaseName
                         & tTables.TableSchema != "pg_catalog"
                         & tTables.TableSchema != "information_schema";

            return Exists(SelectOne().From(tTables).Where(filter));
        }
    }
}