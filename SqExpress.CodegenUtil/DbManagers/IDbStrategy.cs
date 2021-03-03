using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SqExpress.CodeGenUtil.Model;

namespace SqExpress.CodeGenUtil.DbManagers
{
    internal interface IDbStrategy : IDisposable
    {
        Task<List<TableColumnRawModel>> LoadColumns();

        Task<LoadIndexesResult> LoadIndexes();

        Task<Dictionary<ColumnRef, List<ColumnRef>>> LoadForeignKeys();

        ColumnType GetColType(TableColumnRawModel raw);

        string DefaultSchemaName { get; }

        DefaultValue? ParseDefaultValue(string? rawColumnDefaultValue);
    }

    internal readonly struct LoadIndexesResult
    {
        public readonly Dictionary<TableRef, PrimaryKey> Pks;
        public readonly Dictionary<TableRef, List<Model.Index>> Indexes;

        public LoadIndexesResult(Dictionary<TableRef, PrimaryKey> pks, Dictionary<TableRef, List<Model.Index>> indexes)
        {
            this.Pks = pks;
            this.Indexes = indexes;
        }

        public static LoadIndexesResult Empty() => new LoadIndexesResult(
            new Dictionary<TableRef, PrimaryKey>(),
            new Dictionary<TableRef, List<Model.Index>>());
    }
}