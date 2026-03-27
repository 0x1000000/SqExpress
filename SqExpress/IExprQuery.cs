using System.Collections.Generic;
using SqExpress.Syntax;
using SqExpress.Syntax.Select;

namespace SqExpress
{
    public interface IExprQuery : IExprComplete, IExprSelectingSource
    {
        IReadOnlyList<string?> GetOutputColumnNames();
    }

    public interface IExprSelectingSource: IExpr
    {
        public IReadOnlyList<IExprSelecting> ExtractSelecting();
    }
}