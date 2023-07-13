using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Output;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress.Syntax.Update
{
    public class ExprMerge : IExprExec
    {
        public ExprMerge(ExprTable targetTable, IExprTableSource source, ExprBoolean on, IExprMergeMatched? whenMatched, IExprMergeNotMatched? whenNotMatchedByTarget, IExprMergeMatched? whenNotMatchedBySource)
        {
            this.TargetTableBase = targetTable;
            this.Source = source;
            this.On = on;
            this.WhenMatched = whenMatched;
            this.WhenNotMatchedByTarget = whenNotMatchedByTarget;
            this.WhenNotMatchedBySource = whenNotMatchedBySource;
        }

        public ExprTable TargetTableBase { get; }

        public IExprTableSource Source { get; }

        public ExprBoolean On { get; }

        public IExprMergeMatched? WhenMatched { get; }

        public IExprMergeNotMatched? WhenNotMatchedByTarget { get; }

        public IExprMergeMatched? WhenNotMatchedBySource { get; }

        public virtual TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprMerge(this, arg);
    }

    public class ExprMergeOutput : ExprMerge, IExprQuery
    {
        public static ExprMergeOutput FromMerge(ExprMerge merge, ExprOutput output) => new ExprMergeOutput(merge.TargetTableBase, merge.Source, merge.On, merge.WhenMatched, merge.WhenNotMatchedByTarget, merge.WhenNotMatchedBySource, output);

        public ExprMergeOutput(ExprTable targetTable, IExprTableSource source, ExprBoolean on, IExprMergeMatched? whenMatched, IExprMergeNotMatched? whenNotMatchedByTarget, IExprMergeMatched? whenNotMatchedBySource, ExprOutput output) : base(targetTable, source, @on, whenMatched, whenNotMatchedByTarget, whenNotMatchedBySource)
        {
            this.Output = output;
        }

        public ExprOutput Output { get; }

        public override TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprMergeOutput(this, arg);

        public IReadOnlyList<string?> GetOutputColumnNames()
        {
            return this.Output.Columns.SelectToReadOnlyList(i => i.OutputName);
        }
    }

    public interface IExprMergeMatched : IExpr
    {
    }

    public class ExprMergeMatchedUpdate : IExprMergeMatched
    {
        public ExprMergeMatchedUpdate(ExprBoolean? and, IReadOnlyList<ExprColumnSetClause> set)
        {
            this.And = and;
            this.Set = set;
        }

        public ExprBoolean? And { get; }

        public IReadOnlyList<ExprColumnSetClause> Set { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprMergeMatchedUpdate(this, arg);
    }

    public class ExprMergeMatchedDelete : IExprMergeMatched
    {
        public ExprMergeMatchedDelete(ExprBoolean? and)
        {
            this.And = and;
        }

        public ExprBoolean? And { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprMergeMatchedDelete(this, arg);
    }


    public interface IExprMergeNotMatched : IExpr
    {
        ExprBoolean? And { get; }
    }

    public class ExprExprMergeNotMatchedInsert : IExprMergeNotMatched
    {
        public ExprExprMergeNotMatchedInsert(ExprBoolean? and, IReadOnlyList<ExprColumnName> columns, IReadOnlyList<IExprAssigning> values)
        {
            this.And = and;
            this.Columns = columns;
            this.Values = values;
        }

        public ExprBoolean? And { get; }

        public IReadOnlyList<ExprColumnName> Columns { get; }

        public IReadOnlyList<IExprAssigning> Values { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprExprMergeNotMatchedInsert(this, arg);

    }

    public class ExprExprMergeNotMatchedInsertDefault : IExprMergeNotMatched
    {
        public ExprExprMergeNotMatchedInsertDefault(ExprBoolean? and)
        {
            this.And = and;
        }

        public ExprBoolean? And { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprExprMergeNotMatchedInsertDefault(this, arg);
    }

}