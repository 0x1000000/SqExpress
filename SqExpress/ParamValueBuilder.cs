using System;

namespace SqExpress
{
#if NET8_0_OR_GREATER
    public static class ParamValueBuilder
    {
        public static ParamValue Create(ReadOnlySpan<SqExpress.Syntax.Value.ExprValue> values) => ParamValue.FromExprValueSpan(values);
    }
#endif
}
