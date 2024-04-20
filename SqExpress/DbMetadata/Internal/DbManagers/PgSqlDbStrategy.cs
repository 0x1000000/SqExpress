using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.DbMetadata.Internal.Model;
using SqExpress.DbMetadata.Internal.Tables.PgSql.InformationSchema;
using SqExpress.DbMetadata.Internal.Tables.PgSql.PgSchema;
using SqExpress.SqlExport;
using SqExpress.Syntax.Boolean;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.DbMetadata.Internal.DbManagers
{
    internal class PgSqlDbStrategy : DbStrategyBase
    {
        private readonly string _databaseName;

        internal PgSqlDbStrategy(ISqDatabase database, string databaseName) : base(database)
        {
            this._databaseName = databaseName;
        }

        public static DbManager Create(DbManagerOptions options, DbConnection connection)
        {
            try
            {
                var database = new SqDatabase<DbConnection>(
                    connection,
                    ConnectionFactory,
                    new PgSqlExporter(SqlBuilderOptions.Default)
                );

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
            var tColumns = new PgSqlColumns();

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
                .QueryList(
                    this.Database,
                    r => new ColumnRawModel(
                        dbName: new ColumnRef(
                            schema: tColumns.TableSchema.Read(recordReader: r),
                            tableName: tColumns.TableName.Read(recordReader: r),
                            name: tColumns.ColumnName.Read(recordReader: r)
                        ),
                        ordinalPosition: tColumns.OrdinalPosition.Read(r),
                        nullable: tColumns.IsNullable.Read(recordReader: r) == "YES",
                        identity: tColumns.IsIdentity.Read(recordReader: r) == "YES",
                        typeName: tColumns.DataType.Read(recordReader: r) ?? "",
                        defaultValue: tColumns.ColumnDefault.Read(recordReader: r),
                        size: tColumns.CharacterMaximumLength.Read(recordReader: r),
                        precision: tColumns.NumericPrecision.Read(recordReader: r),
                        scale: tColumns.NumericScale.Read(recordReader: r)
                    )
                );
        }

        public override Task<LoadIndexesResult> LoadIndexes()
        {
            var tIndex = new PgIndexWithAtt();
            var tClassC = new PgClass();
            var tClassI = new PgClass();
            var tNamespace = new PgNamespace();

            return Select(
                    tNamespace.NspName,
                    tClassC.RelName,
                    tClassI.RelName.As("indexname"),
                    tIndex.IndIsPrimary,
                    tIndex.IndIsUnique,
                    tIndex.IndIsClustered,
                    UnsafeValue($"{tIndex.IndOption.ColumnName.Name}[{tIndex.AttNum.ColumnName.Name} - 1]").As("options"),
                    ScalarFunctionCustom("pg_catalog", "pg_get_indexdef", tIndex.IndExRelId, tIndex.AttNum, true).As("attdef")
                )
                .From(tIndex)
                .InnerJoin(tClassC, on: tClassC.Oid == tIndex.IndRelId)
                .InnerJoin(tClassI, on: tClassI.Oid == tIndex.IndExRelId)
                .LeftJoin(tNamespace, on: tNamespace.Oid == tClassC.RelNamespace)
                .Where(
                    tNamespace.NspName != "pg_catalog" &
                    (tClassC.RelKind == "r" | tClassC.RelKind == "m" | tClassC.RelKind == "p") &
                    (tClassI.RelKind == "i" | tClassI.RelKind == "I")
                )
                .OrderBy(Asc(tClassI.RelName), Asc(tIndex.AttNum))
                .Query(
                    this.Database,
                    LoadIndexesResult.Empty(),
                    (acc, r) =>
                    {
                        var tableName = new TableRef(
                            schema: tNamespace.NspName.Read(recordReader: r),
                            name: tClassC.RelName.Read(recordReader: r)
                        );

                        var columnName = new ColumnRef(
                            schema: tableName.Schema,
                            tableName: tableName.Name,
                            name: r.GetString("attdef").Trim('"')
                        );

                        var indexName = r.GetString("indexname");
                        var options = r.GetInt32("options");

                        var isDescending = options == 1 || options == 3;
                        var isUnique = tIndex.IndIsUnique.Read(r);
                        var isClustered = tIndex.IndIsClustered.Read(r);
                        var isPk = tIndex.IndIsPrimary.Read(r) && isUnique;

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
                    }
                );
        }

        public override string DefaultSchemaName => "public";

        public override DefaultValue? ParseDefaultValue(string? rawColumnDefaultValue, ColumnType columnType)
        {
            if (string.IsNullOrEmpty(rawColumnDefaultValue))
            {
                return null;
            }
#if NETSTANDARD
            if (rawColumnDefaultValue!.Contains("now()"))
#else
            if (rawColumnDefaultValue.Contains("now()", StringComparison.InvariantCultureIgnoreCase))
#endif
            {
                return new DefaultValue(DefaultValueType.GetUtcDate, null);
            }

            return columnType.Accept(DefaultValueParser.Instance, rawColumnDefaultValue) ??
                   new DefaultValue(DefaultValueType.Raw, rawColumnDefaultValue);
        }

        public override Task<Dictionary<ColumnRef, List<ColumnRef>>> LoadForeignKeys()
        {
            var tTableConstraints = new PgSqlTableConstraints();
            var tKeyColumnUsage = new PgSqlKeyColumnUsage();
            var tConstraintColumnUsage = new PgSqlConstraintColumnUsage();

            return Select(
                    tTableConstraints.TableSchema,
                    tTableConstraints.ConstraintName,
                    tTableConstraints.TableName,
                    tKeyColumnUsage.ColumnName,
                    tConstraintColumnUsage.TableSchema.As("foreign_table_schema"),
                    tConstraintColumnUsage.TableName.As("foreign_table_name"),
                    tConstraintColumnUsage.ColumnName.As("foreign_column_name")
                )
                .From(tTableConstraints)
                .InnerJoin(
                    tKeyColumnUsage,
                    on: tTableConstraints.ConstraintName == tKeyColumnUsage.ConstraintName &
                        tTableConstraints.TableSchema == tKeyColumnUsage.TableSchema
                )
                .InnerJoin(tConstraintColumnUsage, on: tConstraintColumnUsage.ConstraintName == tTableConstraints.ConstraintName)
                .Where(tTableConstraints.ConstraintType == "FOREIGN KEY")
                .OrderBy(tKeyColumnUsage.OrdinalPosition)
                .Query(
                    this.Database,
                    new Dictionary<ColumnRef, List<ColumnRef>>(),
                    (acc, r) =>
                    {
                        var columnName = new ColumnRef(
                            schema: tTableConstraints.TableSchema.Read(r),
                            tableName: tTableConstraints.TableName.Read(r),
                            name: tKeyColumnUsage.ColumnName.Read(r)
                        );

                        var refColumnName = new ColumnRef(
                            schema: r.GetString("foreign_table_schema"),
                            tableName: r.GetString("foreign_table_name"),
                            name: r.GetString("foreign_column_name")
                        );

                        if (!acc.TryGetValue(columnName, out var colList))
                        {
                            colList = new List<ColumnRef>();
                            acc.Add(columnName, colList);
                        }

                        colList.Add(refColumnName);

                        return acc;
                    }
                );
        }

        public override ColumnType GetColType(ColumnRawModel raw)
        {
            var type = raw.TypeName.ToLowerInvariant();
            if (type.StartsWith("timestamp"))
            {
                type = "timestamp";
            }

            switch (type)
            {
                case "bit":
                case "boolean":
                    return new BooleanColumnType(raw.Nullable);
                case "smallint":
                    return new Int16ColumnType(raw.Nullable);
                case "interval":
                case "serial":
                case "int":
                case "integer":
                    return new Int32ColumnType(raw.Nullable);
                case "bigserial":
                case "bigint":
                    return new Int64ColumnType(raw.Nullable);
                case "numeric":
                    return new DecimalColumnType(raw.Nullable, Ensure(raw.Precision, "Precision"), Ensure(raw.Scale, "Scale"));
                case "decimal":
                    return new DecimalColumnType(raw.Nullable, Ensure(raw.Precision, "Precision"), Ensure(raw.Scale, "Scale"));
                case "money":
                    return new DecimalColumnType(raw.Nullable, Ensure(raw.Precision, "Precision"), Ensure(raw.Scale, "Scale"));
                case "double precision":
                case "real":
                    return new DoubleColumnType(raw.Nullable);
                case "date":
                    return new DateTimeColumnType(raw.Nullable, true);
                case "time":
                    return new Int64ColumnType(raw.Nullable);
                case "timestamp":
                    return new DateTimeColumnType(raw.Nullable, false);
                case "bytea":
                    return new ByteArrayColumnType(isNullable: raw.Nullable, size: CheckSize(raw.Size), isFixed: false);
                case "text":
                case "bpchar":
                    return CheckSize(raw.Size).HasValue
                        ? new StringColumnType(
                            isNullable: raw.Nullable,
                            size: raw.Size!.Value,
                            isFixed: false,
                            isUnicode: true,
                            isText: false
                        )
                        : new StringColumnType(
                            isNullable: raw.Nullable,
                            size: null,
                            isFixed: false,
                            isUnicode: true,
                            isText: true
                        );
                case "varchar":
                case "character varying":
                    return new StringColumnType(
                        isNullable: raw.Nullable,
                        size: CheckSize(raw.Size),
                        isFixed: false,
                        isUnicode: true,
                        isText: false
                    );
                case "char":
                case "character":
                    return new StringColumnType(
                        isNullable: raw.Nullable,
                        size: CheckSize(raw.Size),
                        isFixed: true,
                        isUnicode: true,
                        isText: false
                    );
                case "uuid":
                    return new GuidColumnType(isNullable: raw.Nullable);
                case "xml":
                    return new XmlColumnType(isNullable: raw.Nullable);
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
        }

        private ExprBoolean GetTableFilter(IPgSqlTableColumns tColumns)
        {
            var tTables = new PgSqlTables();

            var filter = tTables.TableName == tColumns.TableName
                         & tTables.TableSchema == tColumns.TableSchema
                         & tTables.TableType == "BASE TABLE"
                         & tTables.TableCatalog == this._databaseName
                         & tTables.TableSchema != "pg_catalog"
                         & tTables.TableSchema != "information_schema";

            return Exists(SelectOne().From(tTables).Where(filter));
        }

        private class PgIndexWithAtt : DerivedTableBase
        {
            private readonly PgIndex _tIndex = new();

            public Int32CustomColumn IndRelId { get; }

            public Int32CustomColumn IndExRelId { get; }

            public Int32CustomColumn IndOption { get; }

            public BooleanCustomColumn IndIsUnique { get; }

            public BooleanCustomColumn IndIsPrimary { get; }

            public BooleanCustomColumn IndIsClustered { get; }

            public Int32CustomColumn AttNum { get; }

            public PgIndexWithAtt(Alias alias = default) : base(alias)
            {
                this.IndRelId = this.CreateInt32Column(this._tIndex.IndRelId.ColumnName.Name);
                this.IndExRelId = this.CreateInt32Column(this._tIndex.IndExRelId.ColumnName.Name);
                this.IndOption = this.CreateInt32Column(this._tIndex.IndOption.ColumnName.Name);
                this.IndIsUnique = this.CreateBooleanColumn(this._tIndex.IndIsUnique.ColumnName.Name);
                this.IndIsPrimary = this.CreateBooleanColumn(this._tIndex.IndIsPrimary.ColumnName.Name);
                this.IndIsClustered = this.CreateBooleanColumn(this._tIndex.IndIsClustered.ColumnName.Name);
                this.AttNum = this.CreateInt32Column("attnum");
            }

            protected override IExprSubQuery CreateQuery()
            {
                return Select(
                        this._tIndex.IndRelId,
                        this._tIndex.IndExRelId,
                        this._tIndex.IndOption,
                        this._tIndex.IndIsPrimary,
                        this._tIndex.IndIsUnique,
                        this._tIndex.IndIsClustered,
                        ScalarFunctionCustom(
                                "pg_catalog",
                                "unnest",
                                ScalarFunctionSys(
                                    "ARRAY",
                                    ValueQuery(
                                        Select(
                                            ScalarFunctionCustom("pg_catalog", "generate_series", 1, this._tIndex.IndNkeysAtts)
                                                .As("n")
                                        )
                                    )
                                )
                            )
                            .As(this.AttNum)
                    )
                    .From(this._tIndex)
                    .Done();
            }
        }

        private class DefaultValueParser : IColumnTypeVisitor<DefaultValue?, string>
        {
            public static readonly DefaultValueParser Instance = new DefaultValueParser();

            private DefaultValueParser()
            {
            }

            public DefaultValue? VisitBooleanColumnType(BooleanColumnType booleanColumnType, string defaultValueRaw)
            {
                return null;
            }

            public DefaultValue? VisitByteColumnType(ByteColumnType byteColumnType, string defaultValueRaw)
            {
                return Byte.TryParse(defaultValueRaw, out _) ? new DefaultValue(DefaultValueType.Integer, defaultValueRaw) : null;
            }

            public DefaultValue? VisitByteArrayColumnType(ByteArrayColumnType byteArrayColumnType, string defaultValueRaw)
            {
                return null;
            }

            public DefaultValue? VisitInt16ColumnType(Int16ColumnType int16ColumnType, string defaultValueRaw)
            {
                return Int32.TryParse(defaultValueRaw, out _)
                    ? new DefaultValue(DefaultValueType.Integer, defaultValueRaw)
                    : null;
            }

            public DefaultValue? VisitInt32ColumnType(Int32ColumnType int32ColumnType, string defaultValueRaw)
            {
                return Int32.TryParse(defaultValueRaw, out _)
                    ? new DefaultValue(DefaultValueType.Integer, defaultValueRaw)
                    : null;
            }

            public DefaultValue? VisitInt64ColumnType(Int64ColumnType int64ColumnType, string defaultValueRaw)
            {
                return Int32.TryParse(defaultValueRaw, out _)
                    ? new DefaultValue(DefaultValueType.Integer, defaultValueRaw)
                    : null;
            }

            public DefaultValue? VisitDoubleColumnType(DoubleColumnType doubleColumnType, string defaultValueRaw)
            {
                return Int32.TryParse(defaultValueRaw, out _)
                    ? new DefaultValue(DefaultValueType.Integer, defaultValueRaw)
                    : null;
            }

            public DefaultValue? VisitDecimalColumnType(DecimalColumnType decimalColumnType, string defaultValueRaw)
            {
                return Int32.TryParse(defaultValueRaw, out _)
                    ? new DefaultValue(DefaultValueType.Integer, defaultValueRaw)
                    : null;
            }

            public DefaultValue? VisitDateTimeColumnType(DateTimeColumnType dateTimeColumnType, string defaultValueRaw)
            {
                return null;
            }

            public DefaultValue? VisitDateTimeOffsetColumnType(
                DateTimeOffsetColumnType dateTimeColumnType,
                string defaultValueRaw)
            {
                return null;
            }

            public DefaultValue? VisitStringColumnType(StringColumnType stringColumnType, string defaultValueRaw)
            {
                return new DefaultValue(DefaultValueType.String, defaultValueRaw);
            }

            public DefaultValue? VisitGuidColumnType(GuidColumnType guidColumnType, string defaultValueRaw)
            {
                return null;
            }

            public DefaultValue? VisitXmlColumnType(XmlColumnType xmlColumnType, string defaultValueRaw)
            {
                return null;
            }
        }
    }
}
