using System.Collections.Generic;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Functions;

namespace SqExpress.QueryBuilders.Case
{
    public readonly struct CaseWhen
    {
        public CaseThen When(ExprBoolean condition) => new CaseThen(new List<ExprCaseWhenThen>(), condition);
    }
}