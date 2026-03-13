using System.Collections.Generic;
using SqExpress.Syntax.Select;

namespace SqExpress
{
    public interface IExprSubQuery : IExprQuery, ISelectingSource
    {
        IReadOnlyList<string?> GetOutputColumnNames();
    }
}