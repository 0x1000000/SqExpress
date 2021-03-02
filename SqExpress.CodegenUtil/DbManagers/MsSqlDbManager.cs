using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.CodeGenUtil.Model;
using SqExpress.CodeGenUtil.Tables.MsSql;
using SqExpress.DataAccess;
using SqExpress.SqlExport;
using SqExpress.Syntax.Boolean;
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
                var database = new SqDatabase<SqlConnection>(connection, ConnectionFactory, new TSqlExporter(SqlBuilderOptions.Default));

                return new MsSqlDbManager(connection, database, connection.Database);
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

        public override async Task<IReadOnlyList<TableModel>> SelectTables()
        {
            var columnsRaw = await this.LoadColumns();
            var pk = await this.LoadIndexes();
            var fk = await this.LoadForeignKeys();

            var acc = new Dictionary<TableRef, Dictionary<ColumnRef, ColumnModel>>();

            foreach (var rawColumn in columnsRaw)
            {
                var table = rawColumn.DbName.Table;
                if(!acc.TryGetValue(table, out var colList))
                {
                    colList = new Dictionary<ColumnRef, ColumnModel>();
                    acc.Add(table, colList);
                }

                var colModel = BuildColumnModel(
                    rawColumn, 
                    pk.TryGetValue(table, out var pkCols) ? pkCols : null, 
                    fk.TryGetValue(rawColumn.DbName, out var fkList) ? fkList : null);

                colList.Add(colModel.DbName, colModel);
            }

            var sortedTables = SortTablesByForeignKeys(acc: acc);

            return sortedTables.Select(t =>
                    new TableModel(ToTableCrlName(t),
                        t,
                        acc[t]
                            .Select(p => p.Value)
                            .OrderBy(c => c.PkIndex ?? 10000)
                            .ThenBy(c => c.Name)
                            .ToList()))
                .ToList();
        }

        private static ColumnModel BuildColumnModel(TableColumnRawModel rawColumn, List<ColumnRef>? pkCols, List<ColumnRef>? fkList)
        {
            string clrName = ToColCrlName(rawColumn.DbName);

            var pkIndex = pkCols?.IndexOf(rawColumn.DbName);

            if (pkIndex < 0)
            {
                pkIndex = null;
            }

            return new ColumnModel(
                name: clrName,
                dbName: rawColumn.DbName,
                columnType: GetColType(raw: rawColumn),
                pkIndex: pkIndex,
                identity: rawColumn.Identity,
                defaultValue: rawColumn.DefaultValue,
                fk: fkList);
        }

        private Task<List<TableColumnRawModel>> LoadColumns()
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
                    tColumns.DataType,
                    tColumns.ColumnDefault,
                    tColumns.IsNullable,
                    tColumns.CharacterMaximumLength,
                    tColumns.NumericPrecision,
                    tColumns.NumericScale,
                    funcIsIdentity.As(cIsIdentity))
                .From(tColumns)
                .OrderBy(tColumns.OrdinalPosition).QueryList(this.Database,
                    r => new TableColumnRawModel(
                        dbName: new ColumnRef(schema: tColumns.TableSchema.Read(recordReader: r),
                            tableName: tColumns.TableName.Read(recordReader: r),
                            name: tColumns.ColumnName.Read(recordReader: r)),
                        identity: cIsIdentity.Read(recordReader: r) == 1,
                        nullable: tColumns.IsNullable.Read(recordReader: r) == "YES",
                        typeName: tColumns.DataType.Read(recordReader: r) ?? "",
                        defaultValue: tColumns.ColumnDefault.Read(recordReader: r),
                        size: tColumns.CharacterMaximumLength.Read(recordReader: r),
                        precision: tColumns.NumericPrecision.Read(recordReader: r),
                        scale: tColumns.NumericScale.Read(recordReader: r)));

        }

        private Task<Dictionary<TableRef, List<ColumnRef>>> LoadIndexes()
        {

            var tSysSchemas = new MsSqlSysSchemas();
            var tSysTables = new MsSqlSysTables();
            var tSysColumns = new MsSqlSysColumns();
            var tSysIndexes = new MsSqlSysIndexes();
            var tSysIndexColumns = new MsSqlSysIndexColumns();

            var rSchemaName = CustomColumnFactory.String("RefSchemaName");
            var rTableName = CustomColumnFactory.String("RefTableName");
            var rColumnName = CustomColumnFactory.String("RefColumnName");

            return Select(
                    tSysSchemas.Name.As(rSchemaName), 
                    tSysTables.Name.As(rTableName), 
                    tSysColumns.Name.As(rColumnName),
                    tSysIndexes.IsPrimaryKey)
                .From(tSysIndexes)
                .InnerJoin(tSysTables, on: tSysTables.ObjectId == tSysIndexes.ObjectId)
                .InnerJoin(tSysSchemas, on: tSysSchemas.SchemaId == tSysTables.SchemaId)
                .InnerJoin(tSysIndexColumns, on: tSysIndexColumns.IndexId == tSysIndexes.IndexId & tSysIndexColumns.ObjectId == tSysTables.ObjectId)
                .InnerJoin(tSysColumns, on: tSysColumns.ColumnId == tSysIndexColumns.ColumnId & tSysColumns.ObjectId == tSysTables.ObjectId)
                .Where(tSysIndexes.IsPrimaryKey == true)
                .OrderBy(tSysIndexColumns.KeyOrdinal)
                .Query(this.Database,
                    new Dictionary<TableRef, List<ColumnRef>>(),
                    (acc, r) =>
                    {
                        var tableName = new TableRef(
                            schema: rSchemaName.Read(recordReader: r),
                            name: rTableName.Read(recordReader: r));

                        var columnName = new ColumnRef(
                            schema: tableName.Schema,
                            tableName: tableName.Name,
                            name: rColumnName.Read(recordReader: r));

                        bool isPk = tSysIndexes.IsPrimaryKey.Read(r);

                        if (!acc.TryGetValue(tableName, out var list))
                        {
                            list = new List<ColumnRef>();
                            acc.Add(tableName, list);
                        }

                        list.Add(columnName);

                        return acc;
                    });
        }

        private Task<Dictionary<ColumnRef, List<ColumnRef>>> LoadForeignKeys()
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

        private ExprBoolean JoinTables(IMsSqlTableColumns t1, IMsSqlTableColumns t2)
        {
            return t1.TableCatalog == t2.TableCatalog & t1.TableSchema == t2.TableSchema & t1.TableName == t2.TableName;
        }

        private static string ToColCrlName(ColumnRef columnRef)
        {
            return columnRef.Name;
        }

        private static string ToTableCrlName(TableRef tableRef)
        {
            return tableRef.Name;
        }

        private static IReadOnlyList<TableRef> SortTablesByForeignKeys(Dictionary<TableRef, Dictionary<ColumnRef, ColumnModel>> acc)
        {
            var tableGraph = new Dictionary<TableRef, int>();
            var maxValue = 0;

            foreach (var pair in acc)
            {
                CountTable(pair.Key, pair.Value, 1);
            }

            return acc
                .Keys
                .OrderByDescending(k => tableGraph.TryGetValue(k, out var value) ? value : maxValue)
                .ThenBy(k => k)
                .ToList();

            void CountTable(TableRef table, Dictionary<ColumnRef, ColumnModel> columns, int value)
            {
                var parentTables = columns.Values.Where(c => c.Fk != null)
                    .SelectMany(c => c.Fk)
                    .Select(f => f.Table)
                    .Distinct()
                    .Where(pt=> !pt.Equals(table))//Self ref
                    .ToList();

                bool hasParents = false;
                foreach (var parentTable in parentTables)
                {
                    if (tableGraph.TryGetValue(parentTable, out int oldValue))
                    {
                        if (value >= 1000)
                        {
                            throw new SqExpressCodeGenException("Cycle in tables");
                        }

                        if (oldValue < value)
                        {
                            tableGraph[parentTable] = value;
                        }
                    }
                    else
                    {
                        tableGraph.Add(parentTable, value);
                    }

                    if (maxValue < value)
                    {
                        maxValue = value;
                    }

                    CountTable(parentTable, acc[parentTable], value + 1);
                    hasParents = true;
                }

                if (hasParents && !tableGraph.ContainsKey(columns.Keys.First().Table))
                {
                    tableGraph.Add(table, 0);
                }
            }
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

        private static ColumnType GetColType(TableColumnRawModel raw)
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
                    return new StringColumnType(isNullable: raw.Nullable, size: raw.Size, isFixed: true, isUnicode: false, isText: false);
                case "varchar":
                    return new StringColumnType(isNullable: raw.Nullable, size: raw.Size, isFixed: false, isUnicode: false, isText: false);
                case "text":
                    return new StringColumnType(isNullable: raw.Nullable, size: raw.Size, isFixed: false, isUnicode: false, isText: true);
                case "nchar":
                    return new StringColumnType(isNullable: raw.Nullable, size: raw.Size, isFixed: true, isUnicode: true, isText: false);
                case "nvarchar":
                    return new StringColumnType(isNullable: raw.Nullable, size: raw.Size, isFixed: false, isUnicode: true, isText: false);
                case "ntext":
                    return new StringColumnType(isNullable: raw.Nullable, size: raw.Size, isFixed: false, isUnicode: true, isText: true);
                case "binary":
                    return new ByteArrayColumnType(isNullable: raw.Nullable, size: raw.Size, isFixed: true);
                case "varbinary":
                    return new ByteArrayColumnType(isNullable: raw.Nullable, size: raw.Size, isFixed: false);
                case "image":
                    return new ByteArrayColumnType(isNullable: raw.Nullable, size: raw.Size, isFixed: false);
                case "uniqueidentifier":
                    return new GuidColumnType(isNullable: raw.Nullable);
                case "xml":
                    return new XmlColumnType(isNullable: raw.Nullable);
                default:
                    throw new SqExpressCodeGenException(
                        $"Not supported column type \"{raw.TypeName}\" for {raw.DbName.Schema}.{raw.DbName.TableName}.{raw.DbName.Name}");
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