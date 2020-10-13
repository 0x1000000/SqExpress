using System.Collections.Generic;
using SqExpress.Syntax;

namespace SqExpress
{
    public interface IExprQuery : IExpr
    {
        IReadOnlyList<string?> GetOutputColumnNames();
    }
}