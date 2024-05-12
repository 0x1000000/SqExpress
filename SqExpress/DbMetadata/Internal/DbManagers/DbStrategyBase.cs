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

        public abstract Task<DbRawModels> LoadRawModels();

        public abstract ColumnType GetColType(ColumnRawModel raw);

        public abstract string DefaultSchemaName { get; }

        public abstract DefaultValue? ParseDefaultValue(string? rawColumnDefaultValue, ColumnType columnType);

        public void Dispose()
        {
            Database.Dispose();
        }
    }
}