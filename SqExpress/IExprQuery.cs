using System.Collections.Generic;

namespace SqExpress
{
    public interface IExprQuery : IExprComplete
    {
        IReadOnlyList<string?> GetOutputColumnNames();
    }
}