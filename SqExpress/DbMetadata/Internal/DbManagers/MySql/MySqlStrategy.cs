using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.DbMetadata.Internal.DbManagers.MySql.Tables.InformationSchema;
using SqExpress.DbMetadata.Internal.Model;
using SqExpress.SqlExport;
using SqExpress.Syntax.Boolean;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.DbMetadata.Internal.DbManagers.MySql
{
    internal class MySqlDbStrategy : DbStrategyBase
    {
        private readonly string _databaseName;

        internal MySqlDbStrategy(ISqDatabase database, string databaseName) : base(database)
        {
            _databaseName = databaseName;
        }

        public static DbManager Create(DbManagerOptions options, DbConnection connection)
        {
            try
            {
                var database = new SqDatabase<DbConnection>(
                    connection,
                    CommandFactory,
                    MySqlExporter.Default
                );

                return new DbManager(new MySqlDbStrategy(database, connection.Database), connection, options);
            }
            catch
            {
                connection.Dispose();
                throw;
            }

            static DbCommand CommandFactory(DbConnection connection, string sql)
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                return cmd;
            }
        }

        public override string DefaultSchemaName => "";

        public override async Task<DbRawModels> LoadRawModels()
        {
            var columns = await LoadColumns();

            var (foreignKeys, fkNames, pk) = await LoadConstrains();

            //Indexes
            var tStatistics = new MySqlStatistics();
            var indexes = await Select(
                    tStatistics.TableSchema,
                    tStatistics.TableName,
                    tStatistics.NonUnique,
                    tStatistics.IndexName,
                    tStatistics.ColumnName,
                    tStatistics.Collation
                )
                .From(tStatistics)
                .Where(GetTableFilter(tStatistics))
                .OrderBy(tStatistics.TableName, tStatistics.IndexName, tStatistics.SeqInIndex)
                .Query(
                    Database,
                    LoadIndexesResult.Empty(),
                    (acc, r) =>
                    {
                        var tableSchema = tStatistics.TableSchema.Read(r);
                        var tableName = tStatistics.TableName.Read(r);
                        var nonUniqueNum = tStatistics.NonUnique.Read(r);
                        var indexName = tStatistics.IndexName.Read(r);
                        var columnName = tStatistics.ColumnName.Read(r);
                        var collation = tStatistics.Collation.Read(r);

                        var tableRef = new TableRef(tableSchema, tableName);

                        if (fkNames.TryGetValue(tableRef, out var fkNameList) && fkNameList.Any(
                                x => string.Equals(
                                    x,
                                    indexName,
                                    StringComparison.CurrentCultureIgnoreCase
                                )
                            ))
                        {
                            return acc;
                        }

                        var columnRef = new ColumnRef(tableSchema, tableName, columnName);


                        var isPk = pk.TryGetValue(tableRef, out var pkName) && string.Equals(
                            pkName,
                            indexName,
                            StringComparison.InvariantCultureIgnoreCase
                        );
                        var isDescending = string.Equals(collation, "D", StringComparison.InvariantCultureIgnoreCase);
                        var isUnique = nonUniqueNum == 0;
                        var isClustered = false;

                        if (isPk)
                        {
                            if (!acc.Pks.TryGetValue(tableRef, out var list))
                            {
                                list = new PrimaryKeyModel(new List<IndexColumnModel>(), indexName);
                                acc.Pks.Add(tableRef, list);
                            }

                            list.Columns.Add(new IndexColumnModel(isDescending, columnRef));
                        }
                        else
                        {
                            if (!acc.Indexes.TryGetValue(tableRef, out var indexes))
                            {
                                indexes = new List<IndexModel>();
                                acc.Indexes.Add(tableRef, indexes);
                            }

                            var index = indexes.FirstOrDefault(i => i.Name == indexName);
                            if (index == null)
                            {
                                index = new IndexModel(new List<IndexColumnModel>(), indexName, isUnique, isClustered);
                                indexes.Add(index);
                            }

                            index.Columns.Add(new IndexColumnModel(isDescending, columnRef));
                        }

                        return acc;
                    }
                );


            return new DbRawModels(columns, indexes, foreignKeys);
        }

        public override ColumnType GetColType(ColumnRawModel raw)
        {
            switch (raw.TypeName.ToLowerInvariant())
            {
                //Numeric Data Types
                case "tinyint":
                case "boolean":
                case "int1":
                    return new ByteColumnType(raw.Nullable);
                case "smallint":
                case "int2":
                    return new Int16ColumnType(raw.Nullable);
                case "int3":
                case "int4":
                case "mediumint":
                case "integer":
                case "int":
                    return new Int32ColumnType(raw.Nullable);
                case "int8":
                case "bigint":
                    return new Int64ColumnType(raw.Nullable);
                case "decimal":
                case "dec":
                case "numeric":
                case "fixed":
                case "number":
                    return new DecimalColumnType(raw.Nullable, Ensure(raw.Precision, "Precision"), Ensure(raw.Scale, "Scale"));
                case "float":
                    return new DoubleColumnType(raw.Nullable);
                case "double":
                case "double prevision":
                    return new DoubleColumnType(raw.Nullable);
                case "bit":
                    return new BooleanColumnType(raw.Nullable);

                //Date and Time Data Types
                case "date":
                    return new DateTimeColumnType(raw.Nullable, true);
                case "time":
                    return new Int64ColumnType(raw.Nullable);
                case "datetime":
                    return new DateTimeColumnType(raw.Nullable, false);
                case "timestamp":
                    return new DateTimeColumnType(raw.Nullable, false);

                case "char":
                    return new StringColumnType(
                        isNullable: raw.Nullable,
                        size: CheckSize(raw.Size),
                        isFixed: true,
                        isUnicode: IsUnicode(raw).Item1,
                        isText: false
                    );
                case "enum":
                case "varchar":
                    var (isUnicode, maxLen) = IsUnicode(raw);
                    var size = CheckSize(raw.Size);
                    return new StringColumnType(
                        isNullable: raw.Nullable,
                        size: size == maxLen ? null : size,
                        isFixed: false,
                        isUnicode: isUnicode,
                        isText: false
                    );
                case "tinytext":
                case "text":
                case "text character":
                case "long varchar":
                case "mediumtext":
                case "longtext":
                case "long":
                    return new StringColumnType(
                        isNullable: raw.Nullable,
                        size: null,
                        isFixed: false,
                        isUnicode: IsUnicode(raw).Item1,
                        isText: true
                    );
                case "binary":
                case "char byte":
                    return CheckSize(raw.Size) == 16
                        ? new GuidColumnType(raw.Nullable)
                        : new ByteArrayColumnType(isNullable: raw.Nullable, size: CheckSize(raw.Size), isFixed: true);
                case "varbinary":
                case "blob":
                case "longblob":
                case "mediumblob":
                    return new ByteArrayColumnType(isNullable: raw.Nullable, size: CheckSize(raw.Size), isFixed: false);
                default:
                    throw new SqExpressException(
                        $"Not supported column type \"{raw.TypeName}\" for {raw.DbName.Schema}.{raw.DbName.TableName}.{raw.DbName.Name}"
                    );
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

                throw new SqExpressException(
                    $"\"{name}\" should have a value for {raw.DbName.Schema}.{raw.DbName.TableName}.{raw.DbName.Name}"
                );
            }

            static (bool, int?) IsUnicode(ColumnRawModel raw)
            {
                if (raw.Extra is string s)
                {
                    if (s.IndexOf("utf8mb3", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        return (true, 21844);
                    }

                    if (s.IndexOf("utf8mb4", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        return (true, 16383);
                    }
                }

                return (false, null);
            }
        }

        public override DefaultValue? ParseDefaultValue(string? rawColumnDefaultValue, ColumnType columnType)
        {
            if (rawColumnDefaultValue == null)
            {
                return null;
            }

            if (rawColumnDefaultValue.Equals("NULL", StringComparison.InvariantCultureIgnoreCase))
            {
                return columnType.IsNullable ? null : new DefaultValue(DefaultValueType.Null, null);
            }

            if (rawColumnDefaultValue.Equals("utc_timestamp()", StringComparison.InvariantCultureIgnoreCase))
            {
                return new DefaultValue(DefaultValueType.GetUtcDate, null);
            }

            if (int.TryParse(rawColumnDefaultValue, out _))
            {
                return new DefaultValue(DefaultValueType.Integer, rawColumnDefaultValue);
            }

            if (columnType is BooleanColumnType)
            {
                if (rawColumnDefaultValue == "b'0'")
                {
                    return new DefaultValue(DefaultValueType.Bool, "0");
                }

                if (rawColumnDefaultValue == "b'1'")
                {
                    return new DefaultValue(DefaultValueType.Bool, "1");
                }
            }

            return new DefaultValue(DefaultValueType.Raw, rawColumnDefaultValue);
        }

        private Task<List<ColumnRawModel>> LoadColumns()
        {
            var tColumns = new MySqlColumns();

            return Select(
                    tColumns.TableSchema,
                    tColumns.TableName,
                    tColumns.ColumnName,
                    tColumns.OrdinalPosition,
                    tColumns.DataType,
                    tColumns.ColumnDefault,
                    tColumns.IsNullable,
                    tColumns.Extra,
                    tColumns.CharacterMaximumLength,
                    tColumns.NumericPrecision,
                    tColumns.NumericScale,
                    tColumns.CharacterSetName
                )
                .From(tColumns)
                .Where(GetTableFilter(tColumns))
                .OrderBy(tColumns.OrdinalPosition)
                .Done()
                .QueryList(
                    Database,
                    r => new ColumnRawModel(
                        dbName: new ColumnRef(
                            schema: tColumns.TableSchema.Read(recordReader: r),
                            tableName: tColumns.TableName.Read(recordReader: r),
                            name: tColumns.ColumnName.Read(recordReader: r)
                        ),
                        ordinalPosition: (int)tColumns.OrdinalPosition.Read(r),
                        nullable: tColumns.IsNullable.Read(recordReader: r) == "YES",
                        identity: (tColumns.Extra.Read(recordReader: r)
                            ?.IndexOf("auto_increment", StringComparison.InvariantCultureIgnoreCase) ?? -1) >= 0,
                        typeName: tColumns.DataType.Read(recordReader: r) ?? "",
                        defaultValue: tColumns.ColumnDefault.Read(recordReader: r),
                        size: (int?)tColumns.CharacterMaximumLength.Read(recordReader: r),
                        precision: (int?)tColumns.NumericPrecision.Read(recordReader: r),
                        scale: (int?)tColumns.NumericScale.Read(recordReader: r),
                        extra: tColumns.CharacterSetName.Read(r)
                    )
                );
        }

        private Task<(Dictionary<ColumnRef, List<ColumnRef>> Fks, Dictionary<TableRef, List<string>> FkName,
                Dictionary<TableRef, string> Pks)>
            LoadConstrains()
        {
            var tTableConstraints = new MySqlTableConstraints();
            var tColumnUsage = new MySqlKeyColumnUsage();

            return Select(
                    tTableConstraints.ConstraintType,
                    tColumnUsage.ConstraintName,
                    tColumnUsage.TableSchema,
                    tColumnUsage.TableName,
                    tColumnUsage.ColumnName,
                    tColumnUsage.ReferencedTableSchema,
                    tColumnUsage.ReferencedTableName,
                    tColumnUsage.ReferencedColumnName
                )
                .From(tColumnUsage)
                .InnerJoin(
                    tTableConstraints,
                    on:
                    tTableConstraints.ConstraintCatalog == tColumnUsage.TableCatalog
                    & tTableConstraints.TableSchema == tColumnUsage.TableSchema
                    & tTableConstraints.TableName == tColumnUsage.TableName
                    & tTableConstraints.ConstraintName == tColumnUsage.ConstraintName
                )
                .Where(GetTableFilter(tColumnUsage) & tTableConstraints.ConstraintType.In("PRIMARY KEY", "FOREIGN KEY"))
                .OrderBy(tColumnUsage.OrdinalPosition)
                .Query(
                    Database,
                    (Fks: new Dictionary<ColumnRef, List<ColumnRef>>(),
                        FkName: new Dictionary<TableRef, List<string>>(),
                        Pks: new Dictionary<TableRef, string>()),
                    (acc, r) =>
                    {
                        var tableSchema = tColumnUsage.TableSchema.Read(r);
                        var tableName = tColumnUsage.TableName.Read(r);

                        var columnName = new ColumnRef(
                            schema: tableSchema,
                            tableName: tableName,
                            name: tColumnUsage.ColumnName.Read(r)
                        );

                        var refTableName = new TableRef(tableSchema, tableName);
                        var constraintType = tTableConstraints.ConstraintType.Read(r);
                        var constraintName = tColumnUsage.ConstraintName.Read(r);

                        if (constraintType == "FOREIGN KEY")
                        {
                            var refColumnName = new ColumnRef(
                                schema: ReadNullable(tColumnUsage.ReferencedTableSchema, r),
                                tableName: ReadNullable(tColumnUsage.ReferencedTableName, r),
                                name: ReadNullable(tColumnUsage.ReferencedColumnName, r)
                            );

                            if (!acc.Fks.TryGetValue(columnName, out var colList))
                            {
                                colList = new List<ColumnRef>();
                                acc.Fks.Add(columnName, colList);
                            }

                            colList.Add(refColumnName);

                            if (!acc.FkName.TryGetValue(refTableName, out var fkList))
                            {
                                fkList = new List<string>();
                                acc.FkName.Add(refTableName, fkList);
                            }

                            fkList.Add(constraintName);
                        }
                        else if (constraintType == "PRIMARY KEY")
                        {
                            if (acc.Pks.TryGetValue(refTableName, out var existingPk))
                            {
                                if (existingPk != constraintName)
                                {
                                    throw new SqExpressException(
                                        $"Table '{refTableName.Name}' already has the primary key '{existingPk}' and another one ('{constraintName}') was not expcted"
                                    );
                                }
                            }
                            else
                            {
                                acc.Pks.Add(refTableName, constraintName);
                            }
                        }
                        else
                        {
                            throw new SqExpressException($"Unexpected constraint type: '{constraintType}'");
                        }

                        return acc;

                        static string ReadNullable(NullableStringTableColumn column, ISqDataRecordReader r)
                        {
                            return column.Read(r) ??
                                   throw new SqExpressException($"{column.ColumnName.Name} is expected to contain a value");
                        }
                    }
                );
        }

        private ExprBoolean GetTableFilter(IMySqlTableColumns tColumns)
        {
            return tColumns.TableSchema == _databaseName;
        }
    }
}
