using System;

namespace SqExpress.Syntax.Type
{
    public readonly struct DecimalPrecisionScale
    {
        public DecimalPrecisionScale(int precision, int? scale)
        {
            this.Precision = precision;
            this.Scale = scale;
        }

        public readonly int Precision;

        public readonly int? Scale;

        public static implicit operator DecimalPrecisionScale(int precision)
            => new DecimalPrecisionScale(precision, null);

        public static implicit operator DecimalPrecisionScale(ValueTuple<int, int> precisionScale)
            => new DecimalPrecisionScale(precisionScale.Item1, precisionScale.Item2);

    }
}