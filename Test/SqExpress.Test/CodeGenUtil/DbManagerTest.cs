#if !NETFRAMEWORK
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using NUnit.Framework;
using SqExpress.CodeGenUtil;
using SqExpress.CodeGenUtil.DbManagers;
using SqExpress.CodeGenUtil.Model;

namespace SqExpress.Test.CodeGenUtil
{
    [TestFixture]
    public class DbManagerTest : IDbStrategy
    {
        private readonly IDbStrategy _msSqlDbStrategy = new MsSqlDbManager(null!, null!);

        [Test]
        public async Task SelectTables_BasicTest()
        {
            using var dbManager = new DbManager(this, new SqlConnection("Initial Catalog=TestDatabase;"), new GenTabDescOptions(ConnectionType.MsSql, "fake", "Tab", "", "MyTables"));

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
            Assert.AreEqual(2, tableA.Columns.Count);

            var tableAColumnId = tableA.Columns[0];
            Assert.AreEqual("Id", tableAColumnId.Name);
        }

        public void Dispose()
        {
        }

        Task<List<ColumnRawModel>> IDbStrategy.LoadColumns()
        {
            List<ColumnRawModel> columns = new List<ColumnRawModel>
            {
                new ColumnRawModel(new ColumnRef("dbo","TableZ", "Id"), 1, true, false, "int", "((0))", null, null, null),
                new ColumnRawModel(new ColumnRef("dbo","TableZ", "ValueA"), 2, false, false, "nvarchar", "(N'')", 255, null, null),
                new ColumnRawModel(new ColumnRef("dbo","TableZ", "Value_A"), 3, false, true, "decimal", null, null, 2, 6),
                new ColumnRawModel(new ColumnRef("dbo","TableA", "Id"), 4, true, false, "int", "((0))", null, null, null),
                new ColumnRawModel(new ColumnRef("dbo","TableA", "Value"), 5, false, false, "datetime", "(getutcdate())", null, null, null)
            };

            return Task.FromResult(columns);
        }

        Task<LoadIndexesResult> IDbStrategy.LoadIndexes()
        {
            Dictionary<TableRef, PrimaryKeyModel> pks = new Dictionary<TableRef, PrimaryKeyModel>();
            Dictionary<TableRef, List<IndexModel>> inds = new Dictionary<TableRef, List<IndexModel>>();

            pks.Add(new TableRef("dbo", "TableA"), new PrimaryKeyModel(new List<IndexColumnModel> { new IndexColumnModel(false, new ColumnRef("dbo", "TableA", "Id")) }, "PK_TableA"));
            pks.Add(new TableRef("dbo", "TableZ"), new PrimaryKeyModel(new List<IndexColumnModel> { new IndexColumnModel(false, new ColumnRef("dbo", "TableZ", "Id")) }, "PK_TableZ"));

            inds.Add(new TableRef("dbo", "TableA"),
                new List<IndexModel>
                {
                    new IndexModel(new List<IndexColumnModel> {new IndexColumnModel(true, new ColumnRef("dbo", "TableA", "Value"))},
                        "IX_TableA_Value",
                        true,
                        false)
                });

            LoadIndexesResult result = new LoadIndexesResult(pks, inds);

            return Task.FromResult(result);
        }

        Task<Dictionary<ColumnRef, List<ColumnRef>>> IDbStrategy.LoadForeignKeys()
        {
            Dictionary<ColumnRef, List<ColumnRef>> result = new Dictionary<ColumnRef, List<ColumnRef>>();

            result.Add(new ColumnRef("dbo", "TableA", "Id"), new List<ColumnRef> { new ColumnRef("dbo", "TableZ", "Id") });

            return Task.FromResult(result);
        }

        ColumnType IDbStrategy.GetColType(ColumnRawModel raw)
        {
            return this._msSqlDbStrategy.GetColType(raw);
        }

        public string DefaultSchemaName => "dbo";

        DefaultValue? IDbStrategy.ParseDefaultValue(string rawColumnDefaultValue)
        {
            return this._msSqlDbStrategy.ParseDefaultValue(rawColumnDefaultValue);
        }
    }
}
#endif