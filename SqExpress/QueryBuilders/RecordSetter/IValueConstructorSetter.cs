using SqExpress.Syntax.Names;
using SqExpress.Syntax.Value;

namespace SqExpress.QueryBuilders.RecordSetter;

public delegate void ValueConstructorMapping<in TItem>(IValueConstructorSetter<TItem> setter);

public interface IValueConstructorSetter : IUpdateSetterLiteral<IValueConstructorSetter, ExprColumnName>
{
}

public interface IValueConstructorSetter<out TItem> : IValueConstructorSetter
{
    public TItem Item { get; }

    public int Index { get; }
}