using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Utils;

namespace SqExpress.Syntax.Update
{
    public class ExprDelete : IExprExec
    {
        public ExprDelete(ExprTable target, IExprTableSource? source, ExprBoolean? filter)
        {
            this.Target = target;
            this.Source = source;
            this.Filter = filter;
        }

        public ExprTable Target { get; }

        public IExprTableSource? Source { get; }

        public ExprBoolean? Filter { get; }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprDelete(this);
    }

    public class ExprDeleteOutput : IExprQuery
    {
        public ExprDeleteOutput(ExprDelete delete, IReadOnlyList<ExprAliasedColumn> outputColumns)
        {
            this.Delete = delete;
            this.OutputColumns = outputColumns;
        }

        public ExprDelete Delete { get; } 

        public IReadOnlyList<ExprAliasedColumn> OutputColumns { get; }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprDeleteOutput(this);

        public IReadOnlyList<string?> GetOutputColumnNames()
        {
            return this.OutputColumns.SelectToReadOnlyList(i => ((IExprNamedSelecting) i).OutputName);
        }
    }
}