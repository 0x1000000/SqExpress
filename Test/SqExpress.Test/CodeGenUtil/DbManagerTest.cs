#if NET
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SqExpress.CodeGenUtil;
using SqExpress.DbMetadata.Internal.DbManagers;
using SqExpress.DbMetadata.Internal.DbManagers.MsSql;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.Test.CodeGenUtil;

[TestFixture]
public class DbManagerTest : IDbStrategy
{
    private readonly IDbStrategy _msSqlDbStrategy = new MsSqlDbStrategy(null!, null!);

    [Test]
    public async Task SelectTables_BasicTest()
    {
        var genTablesOptions = new GenTablesOptions(ConnectionType.MsSql, "fake", "Tab", "", "MyTables", Verbosity.Quiet);
        using var dbManager = new DbManager(this, new SqlConnection("Initial Catalog=TestDatabase;"), new DbManagerOptions(genTablesOptions.TableClassPrefix));

        var tables = await dbManager.SelectTables();

        Assert.AreEqual(2, tables.Count);
        var tableZ = tables[0];

        Assert.AreEqual("TabTableZ", tableZ.Name);//TableZ goes first since is references TableA
        Assert.AreEqual(3, tableZ.Columns.Count);

        var tableZColumnId = tableZ.Columns[0];
        Assert.AreEqual("Id", tableZColumnId.Name);
        Assert.AreEqual(typeof(Int32ColumnType), tableZColumnId.ColumnType.GetType());
        Assert.AreEqual(0, tableZColumnId.Pk?.Index);
        Assert.AreEqual(true, tableZColumnId.Identity);
        Assert.AreEqual(DefaultValueType.Integer, tableZColumnId.DefaultValue?.Type);
        Assert.AreEqual("0", tableZColumnId.DefaultValue?.RawValue);

        var tableZColumnValueA = tableZ.Columns[1];
        Assert.AreEqual("ValueA", tableZColumnValueA.Name);
        Assert.AreEqual(typeof(StringColumnType), tableZColumnValueA.ColumnType.GetType());
        Assert.AreEqual(255, ((StringColumnType)tableZColumnValueA.ColumnType).Size);
        Assert.AreEqual(DefaultValueType.String, tableZColumnValueA.DefaultValue?.Type);
        Assert.AreEqual("", tableZColumnValueA.DefaultValue?.RawValue);

        var tableZColumnValueA2 = tableZ.Columns[2];
        Assert.AreEqual("ValueANo2", tableZColumnValueA2.Name);
        Assert.AreEqual(typeof(DecimalColumnType), tableZColumnValueA2.ColumnType.GetType());
        Assert.AreEqual(6, ((DecimalColumnType)tableZColumnValueA2.ColumnType).Scale);

        var tableA = tables[1];
        Assert.AreEqual("TabTableA", tableA.Name);
        Assert.AreEqual(3, tableA.Columns.Count);

        var tableAColumnId = tableA.Columns[0];
        Assert.AreEqual("Id", tableAColumnId.Name);

        var tableAIsActive = tableA.Columns[2];
        Assert.AreEqual("IsActive", tableAIsActive.Name);
        Assert.AreEqual("1", tableAIsActive.DefaultValue?.RawValue);
    }

    [Test]
    public async Task SelectTables_SkipUnknownColumnTypes_DropsUnsupportedColumns()
    {
        using var dbManager = new DbManager(new UnsupportedTypeDbStrategy(), new SqlConnection("Initial Catalog=TestDatabase;"), new DbManagerOptions("Tab"));

        Assert.ThrowsAsync<SqExpressException>(async () => await dbManager.SelectTables());

        var tables = await dbManager.SelectTables(skipUnknownColumnTypes: true);

        Assert.AreEqual(1, tables.Count);
        Assert.AreEqual(1, tables[0].Columns.Count);
        Assert.AreEqual("Id", tables[0].Columns[0].Name);
        Assert.AreEqual(0, tables[0].Indexes.Count);
    }

    [Test]
    public async Task SelectTables_CycleKeepsExternalDependenciesOrdered()
    {
        var strategy = new GraphDbStrategy(
            ["Parent", "CycleA", "CycleB", "Child", "Isolated"],
            [("CycleA", "CycleB"), ("CycleB", "CycleA"),
                ("CycleA", "Parent"), ("Child", "CycleA")]);
        using var dbManager = new DbManager(strategy, new SqlConnection("Initial Catalog=TestDatabase;"), new DbManagerOptions(""));

        var tables = await dbManager.SelectTables();
        var names = tables.Select(t => t.DbName.Name).ToList();

        Assert.That(names.IndexOf("Parent"), Is.LessThan(names.IndexOf("CycleA")));
        Assert.That(names.IndexOf("CycleA"), Is.LessThan(names.IndexOf("CycleB")));
        Assert.That(names.IndexOf("CycleB"), Is.LessThan(names.IndexOf("Child")));
        Assert.That(tables.Single(t => t.DbName.Name == "CycleA").Columns[0].Fk?.Count, Is.EqualTo(2));
        Assert.That(tables.Single(t => t.DbName.Name == "CycleB").Columns[0].Fk?.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task SelectTables_DeepForeignKeyChainDoesNotThrow()
    {
        var names = Enumerable.Range(0, 1001).Select(i => $"T{i:0000}").ToArray();
        var edges = Enumerable.Range(1, names.Length - 1)
            .Select(i => (Child: names[i], Parent: names[i - 1])).ToArray();
        using var dbManager = new DbManager(new GraphDbStrategy(names, edges),
            new SqlConnection("Initial Catalog=TestDatabase;"), new DbManagerOptions(""));

        var tables = await dbManager.SelectTables();

        Assert.That(tables.Select(t => t.DbName.Name), Is.EqualTo(names));
    }

    [Test]
    public void ParseDefaultValue_SysUtcDateTime_IsRecognizedAsUtcNow()
    {
        var parsed = ((IDbStrategy)this).ParseDefaultValue("(sysutcdatetime())", new DateTimeColumnType(isNullable: false, isDate: false));

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed?.Type, Is.EqualTo(DefaultValueType.GetUtcDate));
        Assert.That(parsed?.RawValue, Is.Null);
    }

    public void Dispose()
    {
    }


    async Task<DbRawModels> IDbStrategy.LoadRawModels(bool includeViews)
    {
        return new DbRawModels(await this.LoadColumns(), await this.LoadIndexes(), await this.LoadForeignKeys());
    }


    private Task<List<ColumnRawModel>> LoadColumns()
    {
        List<ColumnRawModel> columns =
        [
            new ColumnRawModel(
                new ColumnRef("dbo", "TableZ", "Id"),
                1,
                true,
                false,
                "int",
                "((0))",
                null,
                null,
                null,
                null
            ),
            new ColumnRawModel(
                new ColumnRef("dbo", "TableZ", "ValueA"),
                2,
                false,
                false,
                "nvarchar",
                "(N'')",
                255,
                null,
                null,
                null
            ),
            new ColumnRawModel(
                new ColumnRef("dbo", "TableZ", "Value_A"),
                3,
                false,
                true,
                "decimal",
                null,
                null,
                2,
                6,
                null
            ),
            new ColumnRawModel(
                new ColumnRef("dbo", "TableA", "Id"),
                4,
                true,
                false,
                "int",
                "((0))",
                null,
                null,
                null,
                null
            ),
            new ColumnRawModel(
                new ColumnRef("dbo", "TableA", "Value"),
                5,
                false,
                false,
                "datetime",
                "(getutcdate())",
                null,
                null,
                null,
                null
            ),
            new ColumnRawModel(
                new ColumnRef("dbo", "TableA", "IsActive"),
                6,
                false,
                false,
                "bit",
                "((1))",
                null,
                null,
                null,
                null
            )
        ];

        return Task.FromResult(columns);
    }

    private Task<LoadIndexesResult> LoadIndexes()
    {
        Dictionary<TableRef, PrimaryKeyModel> pks = new Dictionary<TableRef, PrimaryKeyModel>();
        Dictionary<TableRef, List<IndexModel>> inds = new Dictionary<TableRef, List<IndexModel>>();

        pks.Add(new TableRef("dbo", "TableA"), new PrimaryKeyModel(
            [new IndexColumnModel(false, new ColumnRef("dbo", "TableA", "Id"))], "PK_TableA"));
        pks.Add(new TableRef("dbo", "TableZ"), new PrimaryKeyModel(
            [new IndexColumnModel(false, new ColumnRef("dbo", "TableZ", "Id"))], "PK_TableZ"));

        inds.Add(new TableRef("dbo", "TableA"),
            [
                new IndexModel(
                    [new IndexColumnModel(true, new ColumnRef("dbo", "TableA", "Value"))],
                    "IX_TableA_Value",
                    true,
                    false
                )
            ]
        );

        LoadIndexesResult result = new LoadIndexesResult(pks, inds);

        return Task.FromResult(result);
    }

    private Task<Dictionary<ColumnRef, List<ForeignKeyModel>>> LoadForeignKeys()
    {
        Dictionary<ColumnRef, List<ForeignKeyModel>> result = new Dictionary<ColumnRef, List<ForeignKeyModel>>();

        result.Add(new ColumnRef("dbo", "TableA", "Id"), [new ForeignKeyModel(new ColumnRef("dbo", "TableZ", "Id"), ForeignKeyDeleteAction.NoAction)]);

        return Task.FromResult(result);
    }

    ColumnType? IDbStrategy.TryGetColType(ColumnRawModel raw)
    {
        return this._msSqlDbStrategy.TryGetColType(raw);
    }

    public string DefaultSchemaName => "dbo";

    DefaultValue? IDbStrategy.ParseDefaultValue(string? rawColumnDefaultValue, ColumnType columnType)
    {
        return this._msSqlDbStrategy.ParseDefaultValue(rawColumnDefaultValue, columnType);
    }

    private sealed class UnsupportedTypeDbStrategy : IDbStrategy
    {
        private readonly IDbStrategy _msSqlDbStrategy = new MsSqlDbStrategy(null!, null!);

        public void Dispose()
        {
        }

        public string DefaultSchemaName => "dbo";

        public Task<DbRawModels> LoadRawModels(bool includeViews)
        {
            return Task.FromResult(new DbRawModels(
                [
                    new ColumnRawModel(
                        new ColumnRef("dbo", "TableUnsupported", "Id"),
                        1,
                        false,
                        false,
                        "int",
                        null,
                        null,
                        null,
                        null,
                        null
                    ),
                    new ColumnRawModel(
                        new ColumnRef("dbo", "TableUnsupported", "UnsupportedValue"),
                        2,
                        false,
                        false,
                        "unsupported",
                        null,
                        null,
                        null,
                        null,
                        null
                    )
                ],
                new LoadIndexesResult(
                    new Dictionary<TableRef, PrimaryKeyModel>
                    {
                        [new TableRef("dbo", "TableUnsupported")] = new PrimaryKeyModel(
                            [new IndexColumnModel(false, new ColumnRef("dbo", "TableUnsupported", "Id"))],
                            "PK_TableUnsupported")
                    },
                    new Dictionary<TableRef, List<IndexModel>>
                    {
                        [new TableRef("dbo", "TableUnsupported")] =
                        [
                            new IndexModel(
                                [
                                    new IndexColumnModel(
                                        false,
                                        new ColumnRef("dbo", "TableUnsupported", "UnsupportedValue")
                                    )
                                ],
                                "IX_TableUnsupported_UnsupportedValue",
                                false,
                                false
                            )
                        ]
                    }),
                new Dictionary<ColumnRef, List<ForeignKeyModel>>()));
        }

        public ColumnType? TryGetColType(ColumnRawModel raw)
        {
            if (raw.TypeName == "unsupported")
            {
                return null;
            }

            return this._msSqlDbStrategy.TryGetColType(raw);
        }

        public DefaultValue? ParseDefaultValue(string? rawColumnDefaultValue, ColumnType columnType)
        {
            return this._msSqlDbStrategy.ParseDefaultValue(rawColumnDefaultValue, columnType);
        }
    }

    private sealed class GraphDbStrategy : IDbStrategy
    {
        private readonly IReadOnlyList<string> _tables;
        private readonly IReadOnlyList<(string Child, string Parent)> _edges;

        public GraphDbStrategy(IReadOnlyList<string> tables, IReadOnlyList<(string Child, string Parent)> edges)
        {
            this._tables = tables;
            this._edges = edges;
        }

        public string DefaultSchemaName => "dbo";

        public Task<DbRawModels> LoadRawModels(bool includeViews)
        {
            var columns = this._tables.Select((name, index) => new ColumnRawModel(
                new ColumnRef("dbo", name, "Id"), index + 1, false, false, "int",
                null, null, null, null, null)).ToList();
            var foreignKeys = new Dictionary<ColumnRef, List<ForeignKeyModel>>();
            foreach (var (child, parent) in this._edges)
            {
                var column = new ColumnRef("dbo", child, "Id");
                if (!foreignKeys.TryGetValue(column, out var targets))
                {
                    targets = new List<ForeignKeyModel>();
                    foreignKeys.Add(column, targets);
                }

                targets.Add(new ForeignKeyModel(new ColumnRef("dbo", parent, "Id"), ForeignKeyDeleteAction.NoAction));
            }

            return Task.FromResult(new DbRawModels(columns, LoadIndexesResult.Empty(), foreignKeys));
        }

        public ColumnType? TryGetColType(ColumnRawModel raw) => new Int32ColumnType(false);

        public DefaultValue? ParseDefaultValue(string? rawColumnDefaultValue, ColumnType columnType) => null;

        public void Dispose()
        {
        }
    }
}
#endif
