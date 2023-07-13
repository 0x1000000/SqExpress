using System;
using SqExpress.Syntax.Value;
using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Utils;

namespace SqExpress.QueryBuilders.RecordSetter.Internal;

internal class ValueConstructorSetter<TItem> : IValueConstructorSetter<TItem>
{
    private readonly List<ExprColumnName> _columns = new List<ExprColumnName>();

    private int? _capacity;

    private List<ExprValue>? _record;

    public int Index { get; private set; } = -1;

    public TItem Item { get; private set; }

    public IReadOnlyList<ExprValue>? Record => this._record;

    public IReadOnlyList<ExprColumnName> Columns => this._columns;

    public ValueConstructorSetter(TItem defaultItem)
    {
        this.Item = defaultItem;
    }

    public void NextItem(TItem item, int? length)
    {
        this.Index++;
        this.Item = item;
        this._capacity = length;
        this._record = length.HasValue ? new List<ExprValue>(length.Value) : new List<ExprValue>();
    }

    public void EnsureRecordLength()
    {
        if (this._capacity.HasValue)
        {
            if (this._record == null || this._record.Count != this._capacity.Value)
            {
                throw new SqExpressException($"Number of columns on {this.Index + 1} iteration is less than number of columns on the first one");
            }
        }
    }

    private ValueConstructorSetter<TItem> SetGeneric(ExprColumnName column, ExprLiteral value)
    {
        var record = this._record.AssertFatalNotNull(nameof(this._record));
        if (this._capacity.HasValue && record.Count == this._capacity)
        {
            throw new SqExpressException($"Number of columns on {this.Index + 1} iteration exceeds number of columns on the first one");
        }
        record.Add(value);
        if (!this._capacity.HasValue)
        {
            this._columns.Add(column);
        }
        return this;
    }

    public IValueConstructorSetter Set(ExprColumnName col, int? value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, int value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, string value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, Guid? value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, Guid value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, DateTime? value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, DateTime value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, DateTimeOffset? value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, DateTimeOffset value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, bool? value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, bool value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, byte? value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, byte value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, short? value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, short value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, long? value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, long value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, decimal? value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, decimal value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, double? value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
    public IValueConstructorSetter Set(ExprColumnName col, double value) => this.SetGeneric(col, SqQueryBuilder.Literal(value));
}