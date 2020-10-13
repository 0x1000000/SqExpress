using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select.SelectItems;

namespace SqExpress.QueryBuilders.Merge
{
    public interface IOutputSetter<out TNext>
    {
        TNext Inserted(ExprColumn column);
        TNext Inserted(ExprAliasedColumn column);

        TNext Deleted(ExprColumn column);
        TNext Deleted(ExprAliasedColumn column);

        TNext Column(ExprColumn column);
        TNext Column(ExprAliasedColumn column);

        TNext Action(ExprColumnAlias? alias = null);
    }

    public interface IOutputSetterNext : IOutputSetter<IOutputSetterNext>
    {
    }
}