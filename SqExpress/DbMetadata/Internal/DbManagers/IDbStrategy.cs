using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.DbMetadata.Internal.DbManagers
{
    internal interface IDbStrategy : IDisposable
    {
        Task<List<ColumnRawModel>> LoadColumns();

        Task<LoadIndexesResult> LoadIndexes();

        Task<Dictionary<ColumnRef, List<ColumnRef>>> LoadForeignKeys();

        ColumnType GetColType(ColumnRawModel raw);

        string DefaultSchemaName { get; }

        DefaultValue? ParseDefaultValue(string? rawColumnDefaultValue, ColumnType columnType);
    }

    internal readonly struct LoadIndexesResult
    {
        public readonly Dictionary<TableRef, PrimaryKeyModel> Pks;
        public readonly Dictionary<TableRef, List<Model.IndexModel>> Indexes;

        public LoadIndexesResult(Dictionary<TableRef, PrimaryKeyModel> pks, Dictionary<TableRef, List<Model.IndexModel>> indexes)
        {
            Pks = pks;
            Indexes = indexes;
        }

        public static LoadIndexesResult Empty() => new LoadIndexesResult(
            new Dictionary<TableRef, PrimaryKeyModel>(),
            new Dictionary<TableRef, List<Model.IndexModel>>());
    }
}