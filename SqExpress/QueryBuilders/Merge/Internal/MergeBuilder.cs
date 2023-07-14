using System;
using System.Collections.Generic;
using System.Data.Common;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Output;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;

namespace SqExpress.QueryBuilders.Merge.Internal;

internal class MergeBuilder : IMergeBuilderCondition, IMergeMatchedBuilder, IMergeMatchedThenBuilder, IMergeNotMatchedByTargetThenBuilder, IMergeNotMatchedBySourceBuilder, IMergeMatchedBySourceThenBuilder, IMergeMatchedThenUpdateBuilder, IMergeNotMatchedByTargetInsertBuilder, IMergeMatchedBySourceThenUpdateBuilder, IOutputDoneFirst, IOutputDone
{
    private readonly ExprTable _target;

    private readonly IExprTableSource _source;

    private ExprBoolean? _on;

    private ExprBoolean? _matchedAnd;

    private bool _matchedDelete;

    private List<ExprColumnSetClause>? _matchedUpdate;

    private ExprBoolean? _notMatchedTargetAnd;

    private bool _notMatchedInsertDefaults;

    private (List<ExprColumnName>, List<IExprAssigning>)? _notMatchedInsert;

    private ExprBoolean? _notMatchedSourceAnd;

    private bool _notMatchedDelete;

    private List<ExprColumnSetClause>? _notMatchedUpdate;

    private List<IExprOutputColumn>? _output;

    public MergeBuilder(ExprTable target, IExprTableSource source)
    {
        this._target = target;
        this._source = source;
    }

    public ExprMerge Done()
    {
        return new ExprMerge(
            this._target,
            this._source,
            this._on ?? throw new SqExpressException("Joining condition has already been set"),
            GetWhenMatched(),
            GetWhenNotMatched(),
            GetWhenNotMatchedBySource());

        IExprMergeMatched? GetWhenMatched()
        {
            if (this._matchedDelete)
            {
                return new ExprMergeMatchedDelete(this._matchedAnd);
            }
            if (this._matchedUpdate != null && this._matchedUpdate.Count > 0)
            {
                return new ExprMergeMatchedUpdate(this._matchedAnd, this._matchedUpdate);
            }
            return null;
        }

        IExprMergeNotMatched? GetWhenNotMatched()
        {
            if (this._notMatchedInsertDefaults)
            {
                return new ExprExprMergeNotMatchedInsertDefault(this._notMatchedTargetAnd);
            }

            if (this._notMatchedInsert != null && this._notMatchedInsert.Value.Item1.Count > 0)
            {
                return new ExprExprMergeNotMatchedInsert(this._notMatchedTargetAnd, this._notMatchedInsert.Value.Item1, this._notMatchedInsert.Value.Item2);
            }

            return null;
        }

        IExprMergeMatched? GetWhenNotMatchedBySource()
        {
            if (this._notMatchedDelete)
            {
                return new ExprMergeMatchedDelete(this._notMatchedSourceAnd);
            }
            if (this._notMatchedUpdate != null && this._notMatchedUpdate.Count > 0)
            {
                return new ExprMergeMatchedUpdate(this._notMatchedSourceAnd, this._notMatchedUpdate);
            }
            return null;
        }
    }

    public IMergeMatchedBuilder On(ExprBoolean on)
    {
        if (this._on != null)
        {
            throw new SqExpressException("Joining condition has already been set");
        }
        this._on = on;
        return this;
    }

    public IMergeMatchedThenBuilder WhenMatchedAnd(ExprBoolean filter)
    {
        if (this._matchedAnd != null)
        {
            throw new SqExpressException("Filtering condition has already been set");
        }
        this._matchedAnd = filter;
        return this;
    }

    public IMergeMatchedThenBuilder WhenMatched()
    {
        return this;
    }

    public IMergeNotMatchedByTargetThenBuilder WhenNotMatchedByTargetAnd(ExprBoolean filter)
    {
        if (this._notMatchedTargetAnd != null)
        {
            throw new SqExpressException("Filtering condition has already been set");
        }
        this._notMatchedTargetAnd = filter;
        return this;
    }

    public IMergeMatchedThenFirstUpdateBuilder ThenUpdate()
    {
        this._matchedDelete = false;
        return this;
    }

    public IMergeNotMatchedByTargetBuilder ThenDelete()
    {
        this._matchedDelete = true;
        return this;
    }

    public IMergeNotMatchedByTargetThenBuilder WhenNotMatchedByTarget()
    {
        return this;
    }

    public IMergeNotMatchedByTargetFirstInsertBuilder ThenInsert()
    {
        return this;
    }

    public IMergeNotMatchedBySourceBuilder ThenInsertDefaultValues()
    {
        this._notMatchedInsertDefaults = true;
        return this;
    }

    public IMergeMatchedBySourceThenBuilder WhenNotMatchedBySourceAnd(ExprBoolean filter)
    {
        if (this._notMatchedSourceAnd != null)
        {
            throw new SqExpressException("Filtering condition has already been set");
        }
        this._notMatchedSourceAnd = filter;
        return this;
    }

    public IMergeMatchedBySourceThenBuilder WhenNotMatchedBySource()
    {
        return this;
    }


    IMergeBuilderDone IMergeMatchedBySourceThenBuilder.ThenDelete()
    {
        this._notMatchedDelete = true;
        return this;
    }

    IMergeMatchedBySourceThenFirstUpdateBuilder IMergeMatchedBySourceThenBuilder.ThenUpdate()
    {
        return this;
    }

    private MergeBuilder GenericInsert(ref (List<ExprColumnName>, List<IExprAssigning>)? list, ExprColumnName column, IExprAssigning value)
    {
        list ??= new (new List<ExprColumnName>(), new List<IExprAssigning>());
        list.Value.Item1.Add(column);
        list.Value.Item2.Add(value);
        return this;
    }

    private MergeBuilder GenericSet(ref List<ExprColumnSetClause>? list, ExprColumn column, IExprAssigning value)
    {
        list ??= new List<ExprColumnSetClause>();
        list.Add(new ExprColumnSetClause(column, value));
        return this;
    }

    IMergeMatchedThenUpdateBuilder IUpdateSetter<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, IExprAssigning value) => this.GenericSet(ref this._matchedUpdate, col, value);
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, int? value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, int value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, string value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, Guid? value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, Guid value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, DateTime? value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, DateTime value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, DateTimeOffset? value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, DateTimeOffset value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, bool? value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, bool value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, byte? value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, byte value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, short? value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, short value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, long? value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, long value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, decimal? value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, decimal value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, double? value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, double value) => this.GenericSet(ref this._matchedUpdate, col, SqQueryBuilder.Literal(value));

    IMergeNotMatchedByTargetInsertBuilder IUpdateSetter<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, IExprAssigning value) => this.GenericInsert(ref this._notMatchedInsert, col, value);
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, int? value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, int value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, string value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, Guid? value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, Guid value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, DateTime? value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, DateTime value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, DateTimeOffset? value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, DateTimeOffset value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, bool? value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, bool value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, byte? value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, byte value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, short? value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, short value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, long? value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, long value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, decimal? value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, decimal value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, double? value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));
    IMergeNotMatchedByTargetInsertBuilder IUpdateSetterLiteral<IMergeNotMatchedByTargetInsertBuilder, ExprColumnName>.Set(ExprColumnName col, double value) => this.GenericInsert(ref this._notMatchedInsert, col, SqQueryBuilder.Literal(value));

    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetter<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, IExprAssigning value) => this.GenericSet(ref this._notMatchedUpdate, col, value);
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, int? value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, int value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, string value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, Guid? value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, Guid value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, DateTime? value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, DateTime value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, DateTimeOffset? value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, DateTimeOffset value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, bool? value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, bool value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, byte? value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, byte value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, short? value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, short value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, long? value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, long value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, decimal? value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, decimal value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, double? value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));
    IMergeMatchedBySourceThenUpdateBuilder IUpdateSetterLiteral<IMergeMatchedBySourceThenUpdateBuilder, ExprColumn>.Set(ExprColumn col, double value) => this.GenericSet(ref this._notMatchedUpdate, col, SqQueryBuilder.Literal(value));

    public IOutputDoneFirst Output()
    {
        return this;
    }

    public IOutputDone Inserted(ExprColumn column)
    {
        this._output ??= new List<IExprOutputColumn>();
        this._output.Add(new ExprOutputColumnInserted(column));
        return this;
    }

    public IOutputDone Inserted(ExprAliasedColumn column)
    {
        this._output ??= new List<IExprOutputColumn>();
        this._output.Add(new ExprOutputColumnInserted(new ExprAliasedColumnName(column.Column, column.Alias)));
        return this;
    }

    public IOutputDone Deleted(ExprColumn column)
    {
        this._output ??= new List<IExprOutputColumn>();
        this._output.Add(new ExprOutputColumnDeleted(column));
        return this;
    }

    public IOutputDone Deleted(ExprAliasedColumn column)
    {
        this._output ??= new List<IExprOutputColumn>();
        this._output.Add(new ExprOutputColumnDeleted(new ExprAliasedColumnName(column.Column, column.Alias)));
        return this;
    }

    public IOutputDone Column(ExprColumn column)
    {
        this._output ??= new List<IExprOutputColumn>();
        this._output.Add(new ExprOutputColumn(column));
        return this;
    }

    public IOutputDone Column(ExprAliasedColumn column)
    {
        this._output ??= new List<IExprOutputColumn>();
        this._output.Add(new ExprOutputColumn(column));
        return this;
    }

    public IOutputDone Action(ExprColumnAlias? alias = null)
    {
        this._output ??= new List<IExprOutputColumn>();
        this._output.Add(new ExprOutputAction(alias));
        return this;
    }

    IExprExec IExprExecFinal.Done() => this.Done();

    ExprMergeOutput IOutputDone.Done()
    {
        if (this._output == null || this._output.Count < 1)
        {
            throw new SqExpressException("At least one output field is expected");
        }
        return ExprMergeOutput.FromMerge(this.Done(), new ExprOutput(this._output));
    }

    IExprQuery IExprQueryFinal.Done() => ((IOutputDone)this).Done();
}