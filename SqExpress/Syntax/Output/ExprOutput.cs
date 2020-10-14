using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;

namespace SqExpress.Syntax.Output
{
    public class ExprOutput : IExpr
    {
        public ExprOutput(IReadOnlyList<IExprOutputColumn> columns)
        {
            this.Columns = columns;
        }

        public IReadOnlyList<IExprOutputColumn> Columns { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprOutput(this, arg);
    }

    public interface IExprOutputColumn : IExpr
    {
        string? OutputName { get; }
    }

    public class  ExprOutputColumnInserted : IExprOutputColumn
    {
        public ExprOutputColumnInserted(ExprAliasedColumnName columnName)
        {
            this.ColumnName = columnName;
        }

        public ExprAliasedColumnName ColumnName { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprOutputColumnInserted(this, arg);

        public string? OutputName => ((IExprNamedSelecting) this.ColumnName).OutputName;
    }

    public class ExprOutputColumnDeleted : IExprOutputColumn
    {
        public ExprOutputColumnDeleted(ExprAliasedColumnName columnName)
        {
            this.ColumnName = columnName;
        }

        public ExprAliasedColumnName ColumnName { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprOutputColumnDeleted(this, arg);

        public string? OutputName => ((IExprNamedSelecting)this.ColumnName).OutputName;
    }

    public class ExprOutputColumn : IExprOutputColumn
    {
        public ExprOutputColumn(ExprAliasedColumn column)
        {
            this.Column = column;
        }

        public ExprAliasedColumn Column { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprOutputColumn(this, arg);

        public string? OutputName => ((IExprNamedSelecting)this.Column).OutputName;

    }

    public class ExprOutputAction : IExprOutputColumn
    {
        public ExprOutputAction(ExprColumnAlias? @alias)
        {
            this.Alias = alias;
        }

        public ExprColumnAlias? Alias { get; }

        public TRes Accept<TRes, TArg>(IExprVisitor<TRes, TArg> visitor, TArg arg)
            => visitor.VisitExprOutputAction(this, arg);

        public string? OutputName => this.Alias?.Name;
    }
}