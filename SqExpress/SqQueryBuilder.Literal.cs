using System;
using System.Collections.Generic;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress
{
    public static partial class SqQueryBuilder
    {
        public static ExprInt32Literal Literal(int? value) => new ExprInt32Literal(value);
        public static ExprInt32Literal Literal(int value) => new ExprInt32Literal(value);
        public static ExprStringLiteral Literal(string? value) => new ExprStringLiteral(value);
        public static ExprGuidLiteral Literal(Guid? value) => new ExprGuidLiteral(value);
        public static ExprGuidLiteral Literal(Guid value) => new ExprGuidLiteral(value);
        public static ExprDateTimeLiteral Literal(DateTime? value) => new ExprDateTimeLiteral(value);
        public static ExprBoolLiteral Literal(bool? value) => new ExprBoolLiteral(value);
        public static ExprBoolLiteral Literal(bool value) => new ExprBoolLiteral(value);
        public static ExprByteLiteral Literal(byte? value) => new ExprByteLiteral(value);
        public static ExprByteLiteral Literal(byte value) => new ExprByteLiteral(value);
        public static ExprInt16Literal Literal(short? value) => new ExprInt16Literal(value);
        public static ExprInt16Literal Literal(short value) => new ExprInt16Literal(value);
        public static ExprInt64Literal Literal(long? value) => new ExprInt64Literal(value);
        public static ExprInt64Literal Literal(long value) => new ExprInt64Literal(value);
        public static ExprDecimalLiteral Literal(decimal? value) => new ExprDecimalLiteral(value);
        public static ExprDecimalLiteral Literal(decimal value) => new ExprDecimalLiteral(value);
        public static ExprDoubleLiteral Literal(double? value) => new ExprDoubleLiteral(value);
        public static ExprDoubleLiteral Literal(double value) => new ExprDoubleLiteral(value);
        public static ExprByteArrayLiteral Literal(IReadOnlyList<byte>? value) => new ExprByteArrayLiteral(value);
        public static ExprCast LiteralCast(Guid? value) => Cast(Literal(value), SqlType.Guid);
        public static ExprCast LiteralCast(Guid value) => Cast(Literal(value), SqlType.Guid);
        public static ExprCast LiteralCast(DateTime? value, bool isDate = false) => Cast(Literal(value), SqlType.DateTime(isDate));
        public static ExprCast LiteralCast(DateTime value, bool isDate = false) => Cast(Literal(value), SqlType.DateTime(isDate));
        public static ExprCast LiteralCast(bool? value) => Cast(Literal(value), SqlType.Boolean);
        public static ExprCast LiteralCast(bool value) => Cast(Literal(value), SqlType.Boolean);
        public static ExprCast LiteralCast(byte? value) => Cast(Literal(value), SqlType.Byte);
        public static ExprCast LiteralCast(byte value) => Cast(Literal(value), SqlType.Byte);
        public static ExprCast LiteralCast(short? value) => Cast(Literal(value), SqlType.Int16);
        public static ExprCast LiteralCast(short value) => Cast(Literal(value), SqlType.Int16);
        public static ExprCast LiteralCast(long? value) => Cast(Literal(value), SqlType.Int64);
        public static ExprCast LiteralCast(long value) => Cast(Literal(value), SqlType.Int64);
        public static ExprCast LiteralCast(decimal? value, DecimalPrecisionScale? precisionScale = null) => Cast(Literal(value), SqlType.Decimal(precisionScale));
        public static ExprCast LiteralCast(decimal value, DecimalPrecisionScale? precisionScale = null) => Cast(Literal(value), SqlType.Decimal(precisionScale));
        public static ExprCast LiteralCast(double? value) => Cast(Literal(value), SqlType.Double);
        public static ExprCast LiteralCast(double value) => Cast(Literal(value), SqlType.Double);
    }
}