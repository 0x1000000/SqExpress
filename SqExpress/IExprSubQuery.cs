using System.Collections.Generic;

namespace SqExpress
{
    public interface IExprSubQuery : IExprQuery
    {
        IReadOnlyList<string?> GetOutputColumnNames();
    }
}