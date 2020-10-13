using System.Collections.Generic;
using SqExpress.Syntax.Boolean;

namespace SqExpress.Syntax.Select
{
    public interface IExprTableSource : IExpr
    {
        public (IReadOnlyList<IExprTableSource> Tables, ExprBoolean? On) ToTableMultiplication();
    }
}