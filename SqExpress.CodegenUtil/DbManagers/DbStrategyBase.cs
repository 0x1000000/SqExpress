using System.Collections.Generic;
using System.Threading.Tasks;
using SqExpress.CodeGenUtil.Model;
using SqExpress.DataAccess;

namespace SqExpress.CodeGenUtil.DbManagers
{
    internal abstract class DbStrategyBase : IDbStrategy
    {
        protected readonly ISqDatabase Database;

        protected DbStrategyBase(ISqDatabase database)
        {
            this.Database = database;
        }

        public abstract Task<List<TableColumnRawModel>> LoadColumns();

        public abstract Task<LoadIndexesResult> LoadIndexes();

        public abstract Task<Dictionary<ColumnRef, List<ColumnRef>>> LoadForeignKeys();

        public abstract ColumnType GetColType(TableColumnRawModel raw);

        public abstract string DefaultSchemaName { get; }

        public abstract DefaultValue? ParseDefaultValue(string? rawColumnDefaultValue);

        public void Dispose()
        {
            this.Database.Dispose();
        }
    }
}