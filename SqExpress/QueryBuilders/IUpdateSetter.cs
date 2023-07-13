using System;
using SqExpress.Syntax.Value;

namespace SqExpress.QueryBuilders;

public interface IUpdateSetter<out TRes, in TCol> : IUpdateSetterLiteral<TRes, TCol>
{
    public TRes Set(TCol col, IExprAssigning value);
}

public interface IUpdateSetterLiteral<out TRes, in TCol>
{
    public TRes Set(TCol col, int? value);
    public TRes Set(TCol col, int value);
    public TRes Set(TCol col, string value);
    public TRes Set(TCol col, Guid? value);
    public TRes Set(TCol col, Guid value);
    public TRes Set(TCol col, DateTime? value);
    public TRes Set(TCol col, DateTime value);
    public TRes Set(TCol col, DateTimeOffset? value);
    public TRes Set(TCol col, DateTimeOffset value);
    public TRes Set(TCol col, bool? value);
    public TRes Set(TCol col, bool value);
    public TRes Set(TCol col, byte? value);
    public TRes Set(TCol col, byte value);
    public TRes Set(TCol col, short? value);
    public TRes Set(TCol col, short value);
    public TRes Set(TCol col, long? value);
    public TRes Set(TCol col, long value);
    public TRes Set(TCol col, decimal? value);
    public TRes Set(TCol col, decimal value);
    public TRes Set(TCol col, double? value);
    public TRes Set(TCol col, double value);
}