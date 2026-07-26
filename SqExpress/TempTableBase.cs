using SqExpress.Syntax.Names;

namespace SqExpress
{
    /// <summary>Base class for strongly typed temporary-table descriptors.</summary>
    public class TempTableBase : TableBase
    {
        /// <summary>Initializes a strongly typed temporary-table descriptor whose physical name is rendered using target-dialect rules.</summary>
        /// <param name="name">The logical temporary-table name, without adding dialect-specific quoting.</param>
        /// <param name="alias">An optional query alias; the default leaves the temporary table unaliased.</param>
        public TempTableBase(string name, Alias alias = default) 
            : base(new ExprTempTableName(name), BuildTableAlias(alias) )
        {
        }
    }
}
