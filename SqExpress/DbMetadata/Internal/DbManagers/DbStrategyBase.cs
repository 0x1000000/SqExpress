using System.Collections.Generic;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.DbMetadata.Internal.DbManagers
{
    internal abstract class DbStrategyBase : IDbStrategy
    {
        protected readonly ISqDatabase Database;

        protected DbStrategyBase(ISqDatabase database)
        {
            Database = database;
        }

        public abstract Task<List<ColumnRawModel>> LoadColumns();

        public abstract Task<LoadIndexesResult> LoadIndexes();

        public abstract Task<Dictionary<ColumnRef, List<ColumnRef>>> LoadForeignKeys();

        public abstract ColumnType GetColType(ColumnRawModel raw);

        public abstract string DefaultSchemaName { get; }

        public abstract DefaultValue? ParseDefaultValue(string? rawColumnDefaultValue);

        public void Dispose()
        {
            Database.Dispose();
        }
    }
}