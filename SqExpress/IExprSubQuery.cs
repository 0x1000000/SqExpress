using System.Collections.Generic;
using SqExpress.Syntax.Select;

namespace SqExpress
{
    public interface IExprSubQuery : IExprReadOnlyQuery, ISelectingSource
    {
        IReadOnlyList<string?> GetOutputColumnNames();
    }
}
