using SqExpress.Syntax.Names;

namespace SqExpress
{
    /// <summary>Base class for strongly typed temporary-table descriptors.</summary>
    public class TempTableBase : TableBase
    {
        /// <summary>Initializes a temporary-table descriptor with an optional alias.</summary>
        public TempTableBase(string name, Alias alias = default) 
            : base(new ExprTempTableName(name), BuildTableAlias(alias) )
        {
        }
    }
}
