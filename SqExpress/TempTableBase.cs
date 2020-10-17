using SqExpress.Syntax.Names;

namespace SqExpress
{
    public class TempTableBase : TableBase
    {
        public TempTableBase(string name, Alias alias = default) 
            : base(new ExprTempTableName(name), BuildTableAlias(alias) )
        {

        }
    }
}