using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SqExpress.CodeGenUtil.Model;
using SqExpress.CodeGenUtil.Tables.MsSql;
using SqExpress.DataAccess;
using SqExpress.SqlExport;
using SqExpress.Syntax.Boolean;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.CodeGenUtil.DbManagers
{
    internal class MsSqlDbManager : DbStrategyBase
    {
        private readonly string _databaseName;

        public MsSqlDbManager(ISqDatabase database, string databaseName) : base(database)
        {
            this._databaseName = databaseName;
        }

        public static DbManager Create(GenTablesOptions options)
        {
            SqlConnection connection;
            try
            {
                connection = new SqlConnection(options.ConnectionString);
            }
            catch (ArgumentException e)
            {
                throw new SqExpressCodeGenException($"MsSQL connection string has incorrect format \"{options.ConnectionString}\"", e);
            }

            if (string.IsNullOrEmpty(connection.Database))
            {
                throw new SqExpressCodeGenException("MsSQL connection string has to contain \"database\" attribute");
            }
            try
            {
                var database = new SqDatabase<SqlConnection>(connection, ConnectionFactory, new TSqlExporter(SqlBuilderOptions.Default));

                return new DbManager(new MsSqlDbManager(database, connection.Database), connection, options);
            }
            catch
            {
                connection.Dispose();
                throw;
            }

            static SqlCommand ConnectionFactory(SqlConnection connection, string sql)
            {
                //System.Console.WriteLine(sql);
                return new SqlCommand(sql, connection) {Transaction = null};
            }
        }

        public override Task<List<ColumnRawModel>> LoadColumns()
        {
            var tColumns = new MsSqlIsColumns();

            var funcIsIdentity = ScalarFunctionSys(
                "COLUMNPROPERTY",
                ScalarFunctionSys("OBJECT_ID", tColumns.TableSchema + "." + tColumns.TableName),
                tColumns.ColumnName,
                "IsIdentity");

            var cIsIdentity = CustomColumnFactory.Int32("IsIdentity");

            return Select(
                    tColumns.TableSchema,
                    tColumns.TableName,
                    tColumns.ColumnName,
                    tColumns.OrdinalPosition,
                    tColumns.DataType,
                    tColumns.ColumnDefault,
                    tColumns.IsNullable,
                    tColumns.CharacterMaximumLength,
                    tColumns.NumericPrecision,
                    tColumns.NumericScale,
                    funcIsIdentity.As(cIsIdentity))
                .From(tColumns)
                .Where(GetTableFilter(tColumns))
                .OrderBy(tColumns.OrdinalPosition).QueryList(this.Database,
                    r => new ColumnRawModel(
                        dbName: new ColumnRef(schema: tColumns.TableSchema.Read(recordReader: r),
                            tableName: tColumns.TableName.Read(recordReader: r),
                            name: tColumns.ColumnName.Read(recordReader: r)),
                        ordinalPosition: tColumns.OrdinalPosition.Read(r),
                        identity: cIsIdentity.Read(recordReader: r) == 1,
                        nullable: tColumns.IsNullable.Read(recordReader: r) == "YES",
                        typeName: tColumns.DataType.Read(recordReader: r) ?? "",
                        defaultValue: tColumns.ColumnDefault.Read(recordReader: r),
                        size: tColumns.CharacterMaximumLength.Read(recordReader: r),
                        precision: tColumns.NumericPrecision.Read(recordReader: r),
                        scale: tColumns.NumericScale.Read(recordReader: r)));
        }

        public override Task<LoadIndexesResult> LoadIndexes()
        {
            var tSysSchemas = new MsSqlSysSchemas();
            var tSysTables = new MsSqlSysTables();
            var tSysColumns = new MsSqlSysColumns();
            var tSysIndexes = new MsSqlSysIndexes();
            var tSysIndexColumns = new MsSqlSysIndexColumns();

            var rSchemaName = CustomColumnFactory.String("RefSchemaName");
            var rTableName = CustomColumnFactory.String("RefTableName");
            var rColumnName = CustomColumnFactory.String("RefColumnName");
            var rIndexName = CustomColumnFactory.String("RefIndexName");

            return Select(
                    tSysSchemas.Name.As(rSchemaName), 
                    tSysTables.Name.As(rTableName), 
                    tSysColumns.Name.As(rColumnName),
                    tSysIndexes.Name.As(rIndexName),
                    tSysIndexes.IsPrimaryKey,
                    tSysIndexes.Type,
                    tSysIndexes.IsUnique,
                    tSysIndexColumns.IsDescendingKey)
                .From(tSysIndexes)
                .InnerJoin(tSysTables, on: tSysTables.ObjectId == tSysIndexes.ObjectId)
                .InnerJoin(tSysSchemas, on: tSysSchemas.SchemaId == tSysTables.SchemaId)
                .InnerJoin(tSysIndexColumns, on: tSysIndexColumns.IndexId == tSysIndexes.IndexId & tSysIndexColumns.ObjectId == tSysTables.ObjectId)
                .InnerJoin(tSysColumns, on: tSysColumns.ColumnId == tSysIndexColumns.ColumnId & tSysColumns.ObjectId == tSysTables.ObjectId)
                .Where(tSysIndexColumns.IsIncludedColumn == false)
                .OrderBy(tSysIndexColumns.KeyOrdinal)
                .Query(this.Database,
                    LoadIndexesResult.Empty(),
                    (acc, r) =>
                    {
                        var tableName = new TableRef(
                            schema: rSchemaName.Read(recordReader: r),
                            name: rTableName.Read(recordReader: r));

                        var columnName = new ColumnRef(
                            schema: tableName.Schema,
                            tableName: tableName.Name,
                            name: rColumnName.Read(recordReader: r));

                        string indexName = rIndexName.Read(r);

                        bool isPk = tSysIndexes.IsPrimaryKey.Read(r);
                        bool isDescending = tSysIndexColumns.IsDescendingKey.Read(r);
                        bool isUnique = tSysIndexes.IsUnique.Read(r);
                        bool isClustered = tSysIndexes.Type.Read(r) == 1;

                        if (isPk)
                        {
                            if (!acc.Pks.TryGetValue(tableName, out var list))
                            {
                                list = new PrimaryKeyModel(new List<IndexColumnModel>(), indexName);
                                acc.Pks.Add(tableName, list);
                            }
                            list.Columns.Add(new IndexColumnModel(isDescending, columnName));
                        }
                        else
                        {
                            if (!acc.Indexes.TryGetValue(tableName, out var indexes))
                            {
                                indexes = new List<IndexModel>();
                                acc.Indexes.Add(tableName, indexes);
                            }

                            var index = indexes.FirstOrDefault(i => i.Name == indexName);
                            if (index == null)
                            {
                                index = new IndexModel(new List<IndexColumnModel>(), indexName, isUnique, isClustered);
                                indexes.Add(index);
                            }

                            index.Columns.Add(new IndexColumnModel(isDescending, columnName));
                        }

                        return acc;
                    });
        }

        public override string DefaultSchemaName => "dbo";

        public override DefaultValue? ParseDefaultValue(string? rawColumnDefaultValue)
        {
            if (string.IsNullOrEmpty(rawColumnDefaultValue))
            {
                return null;
            }

            if (TryGetDefaultValue(rawColumnDefaultValue, IntDefRegEx, DefaultValueType.Integer, out var result))
            {
                return result;
            }
            if (TryGetDefaultValue(rawColumnDefaultValue, StringDefRegEx, DefaultValueType.String, out result))
            {
                return result;
            }

            if (string.Equals(rawColumnDefaultValue, "(getutcdate())", StringComparison.InvariantCultureIgnoreCase))
            {
                return new DefaultValue(DefaultValueType.GetUtcDate, null);
            }
            if (string.Equals(rawColumnDefaultValue, "(NULL)", StringComparison.InvariantCultureIgnoreCase))
            {
                return new DefaultValue(DefaultValueType.Null, null);
            }

            return new DefaultValue(DefaultValueType.Raw, rawColumnDefaultValue);

            static bool TryGetDefaultValue(string value, Regex regex, DefaultValueType defaultValueType, out DefaultValue? result)
            {
                var m = regex.Match(value);
                if (m.Success)
                {
                    result = new DefaultValue(defaultValueType, m.Result("$1"));
                    return true;
                }
                result = null;
                return false;
            }
        }

        private static readonly Regex IntDefRegEx = new Regex("^\\(\\((-{0,1}\\d+)\\)\\)$");
        private static readonly Regex StringDefRegEx = new Regex("^\\([N]{0,1}'((?:[^']|'')*)'\\)$");

        public override Task<Dictionary<ColumnRef, List<ColumnRef>>> LoadForeignKeys()
        {

            var tConstraints = new MsSqlReferentialConstraints();
            var tKeys = new MsSqlKeyColumnUsage();

            var tSysSchemas = new MsSqlSysSchemas();
            var tSysTables = new MsSqlSysTables();
            var tSysColumns = new MsSqlSysColumns();
            var tSysIndexes = new MsSqlSysIndexes();
            var tSysIndexColumns = new MsSqlSysIndexColumns();

            var rSchema = CustomColumnFactory.String("RefSchemaName");
            var rName = CustomColumnFactory.String("RefTableName");
            var rColumnName = CustomColumnFactory.String("RefColumnName");


            return Select(
                    tKeys.TableSchema, 
                    tKeys.TableName, 
                    tKeys.ColumnName,
                    tSysSchemas.Name.As(rSchema),
                    tSysTables.Name.As(rName),
                    tSysColumns.Name.As(rColumnName))
                .From(tConstraints)
                .InnerJoin(tKeys, on: tKeys.ConstraintCatalog == tConstraints.ConstraintCatalog & tKeys.ConstraintSchema == tConstraints.ConstraintSchema & tKeys.ConstraintName == tConstraints.ConstraintName)
                .InnerJoin(tSysIndexes, on: tSysIndexes.Name == tConstraints.UniqueConstraintName)
                .InnerJoin(tSysTables, on: tSysTables.ObjectId == tSysIndexes.ObjectId)
                .InnerJoin(tSysSchemas, on: tSysSchemas.SchemaId == tSysTables.SchemaId & tSysSchemas.Name == tConstraints.UniqueConstraintSchema)
                .InnerJoin(tSysIndexColumns, on: tSysIndexColumns.IndexId == tSysIndexes.IndexId & tSysIndexColumns.ObjectId == tSysTables.ObjectId)
                .InnerJoin(tSysColumns, on: tSysColumns.ColumnId == tSysIndexColumns.ColumnId & tSysColumns.ObjectId == tSysTables.ObjectId)

                .Where(GetTableFilter(tKeys) & tKeys.OrdinalPosition == tSysIndexColumns.KeyOrdinal)
                .OrderBy(tKeys.OrdinalPosition)
                .Query(this.Database,
                    new Dictionary<ColumnRef, List<ColumnRef>>(),
                    (acc, r) =>
                    {
                        var columnName = new ColumnRef(
                            schema: tKeys.TableSchema.Read(r),
                            tableName: tKeys.TableName.Read(r),
                            name: tKeys.ColumnName.Read(r));

                        var refColumnName = new ColumnRef(
                            schema: rSchema.Read(r),
                            tableName: rName.Read(r),
                            name: rColumnName.Read(r));

                        if(!acc.TryGetValue(columnName, out var colList))
                        {
                            colList = new List<ColumnRef>();
                            acc.Add(columnName, colList);
                        }

                        colList.Add(refColumnName);

                        return acc;
                    });
        }

        private ExprBoolean GetTableFilter(IMsSqlTableColumns tColumns)
        {
            var tTables = new MsSqlTables();

            var filter = tTables.TableName == tColumns.TableName
                         & tTables.TableSchema == tColumns.TableSchema
                         & tTables.TableType == "BASE TABLE"
                         & tTables.TableCatalog == this._databaseName;

            return Exists(SelectOne().From(tTables).Where(filter));
        }

        public override ColumnType GetColType(ColumnRawModel raw)
        {
            switch (raw.TypeName.ToLowerInvariant())
            {
                case "bigint":
                    return new Int64ColumnType(raw.Nullable);
                case "numeric":
                    return new DecimalColumnType(raw.Nullable, Ensure(raw.Precision, "Precision"), Ensure(raw.Scale, "Scale"));
                case "bit":
                    return new BooleanColumnType(raw.Nullable);
                case "smallint":
                    return new Int16ColumnType(raw.Nullable);
                case "decimal":
                    return new DecimalColumnType(raw.Nullable, Ensure(raw.Precision, "Precision"), Ensure(raw.Scale, "Scale"));
                case "smallmoney":
                    return new DecimalColumnType(raw.Nullable, Ensure(raw.Precision, "Precision"), Ensure(raw.Scale, "Scale"));
                case "int":
                    return new Int32ColumnType(raw.Nullable);
                case "tinyint":
                    return new ByteColumnType(raw.Nullable);
                case "money":
                    return new DecimalColumnType(raw.Nullable, Ensure(raw.Precision, "Precision"), Ensure(raw.Scale, "Scale"));
                case "float":
                    return new DoubleColumnType(raw.Nullable);
                case "real":
                    return new DoubleColumnType(raw.Nullable);
                case "date":
                    return new DateTimeColumnType(raw.Nullable, true);
                case "datetimeoffset":
                    return new DateTimeColumnType(raw.Nullable, false);
                case "datetime2":
                    return new DateTimeColumnType(raw.Nullable, false);
                case "smalldatetime":
                    return new DateTimeColumnType(raw.Nullable, false);
                case "datetime":
                    return new DateTimeColumnType(raw.Nullable, false);
                case "timestamp":
                    return new DateTimeColumnType(raw.Nullable, false);
                case "time":
                    return new Int64ColumnType(raw.Nullable);
                case "char":
                    return new StringColumnType(isNullable: raw.Nullable, size: CheckSize(raw.Size), isFixed: true, isUnicode: false, isText: false);
                case "varchar":
                    return new StringColumnType(isNullable: raw.Nullable, size: CheckSize(raw.Size), isFixed: false, isUnicode: false, isText: false);
                case "text":
                    return new StringColumnType(isNullable: raw.Nullable, size: CheckSize(raw.Size), isFixed: false, isUnicode: false, isText: true);
                case "nchar":
                    return new StringColumnType(isNullable: raw.Nullable, size: CheckSize(raw.Size), isFixed: true, isUnicode: true, isText: false);
                case "nvarchar":
                    return new StringColumnType(isNullable: raw.Nullable, size: CheckSize(raw.Size), isFixed: false, isUnicode: true, isText: false);
                case "ntext":
                    return new StringColumnType(isNullable: raw.Nullable, size: CheckSize(raw.Size), isFixed: false, isUnicode: true, isText: true);
                case "binary":
                    return new ByteArrayColumnType(isNullable: raw.Nullable, size: CheckSize(raw.Size), isFixed: true);
                case "varbinary":
                    return new ByteArrayColumnType(isNullable: raw.Nullable, size: CheckSize(raw.Size), isFixed: false);
                case "image":
                    return new ByteArrayColumnType(isNullable: raw.Nullable, size: CheckSize(raw.Size), isFixed: false);
                case "uniqueidentifier":
                    return new GuidColumnType(isNullable: raw.Nullable);
                case "xml":
                    return new XmlColumnType(isNullable: raw.Nullable);
                default:
                    throw new SqExpressCodeGenException(
                        $"Not supported column type \"{raw.TypeName}\" for {raw.DbName.Schema}.{raw.DbName.TableName}.{raw.DbName.Name}");
            }

            int? CheckSize(int? size)
            {
                if (size.HasValue && size.Value <= 0)
                {
                    return null;
                }

                return size;
            }

            int Ensure(int? value, string name)
            {
                if (value.HasValue)
                {
                    return value.Value;
                }

                throw new SqExpressCodeGenException(
                    $"\"{name}\" should have a value for {raw.DbName.Schema}.{raw.DbName.TableName}.{raw.DbName.Name}");
            }
        }
    }
}