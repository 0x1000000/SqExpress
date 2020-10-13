using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Value;

namespace SqExpress.Syntax.Select
{
    public class ExprQuerySpecification : IExprQueryExpression
    {
        public IReadOnlyList<IExprSelecting> SelectList { get; }

        public ExprValue? Top { get; }

        public bool Distinct { get; }

        public IExprTableSource? From { get; }

        public ExprBoolean? Where { get; }

        public IReadOnlyList<ExprColumn>? GroupBy { get; }

        public ExprQuerySpecification(IReadOnlyList<IExprSelecting> selectList, ExprValue? top, bool distinct, IExprTableSource? from, ExprBoolean? where, IReadOnlyList<ExprColumn>? groupBy)
        {
            this.SelectList = selectList;
            this.Top = top;
            this.Distinct = distinct;
            this.From = from;
            this.Where = where;
            this.GroupBy = groupBy;
        }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprQuerySpecification(this);

        public IReadOnlyList<string?> GetOutputColumnNames()
        {
            string?[] result = new string?[this.SelectList.Count];
            for (int i = 0; i < this.SelectList.Count; i++)
            {
                if (this.SelectList[i] is IExprNamedSelecting item)
                {
                    result[i] = item.OutputName;
                }
            }

            return result;
        }
    }
}