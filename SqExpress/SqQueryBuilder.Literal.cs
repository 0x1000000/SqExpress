using System;
using System.Collections.Generic;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress
{
    public static partial class SqQueryBuilder
    {
        /// <summary>Creates a nullable 32-bit integer literal.</summary>
        public static ExprInt32Literal Literal(int? value) => new ExprInt32Literal(value);
        /// <summary>Creates a 32-bit integer literal.</summary>
        public static ExprInt32Literal Literal(int value) => new ExprInt32Literal(value);
        /// <summary>Creates a nullable string literal.</summary>
        public static ExprStringLiteral Literal(string? value) => new ExprStringLiteral(value);
        /// <summary>Creates a nullable GUID literal.</summary>
        public static ExprGuidLiteral Literal(Guid? value) => new ExprGuidLiteral(value);
        /// <summary>Creates a GUID literal.</summary>
        public static ExprGuidLiteral Literal(Guid value) => new ExprGuidLiteral(value);
        /// <summary>Creates a nullable date and time literal.</summary>
        public static ExprDateTimeLiteral Literal(DateTime? value) => new ExprDateTimeLiteral(value);
        /// <summary>Creates a nullable date, time, and offset literal.</summary>
        public static ExprDateTimeOffsetLiteral Literal(DateTimeOffset? value) => new ExprDateTimeOffsetLiteral(value);
        /// <summary>Creates a nullable Boolean literal.</summary>
        public static ExprBoolLiteral Literal(bool? value) => new ExprBoolLiteral(value);
        /// <summary>Creates a Boolean literal.</summary>
        public static ExprBoolLiteral Literal(bool value) => new ExprBoolLiteral(value);
        /// <summary>Creates a nullable byte literal.</summary>
        public static ExprByteLiteral Literal(byte? value) => new ExprByteLiteral(value);
        /// <summary>Creates a byte literal.</summary>
        public static ExprByteLiteral Literal(byte value) => new ExprByteLiteral(value);
        /// <summary>Creates a nullable 16-bit integer literal.</summary>
        public static ExprInt16Literal Literal(short? value) => new ExprInt16Literal(value);
        /// <summary>Creates a 16-bit integer literal.</summary>
        public static ExprInt16Literal Literal(short value) => new ExprInt16Literal(value);
        /// <summary>Creates a nullable 64-bit integer literal.</summary>
        public static ExprInt64Literal Literal(long? value) => new ExprInt64Literal(value);
        /// <summary>Creates a 64-bit integer literal.</summary>
        public static ExprInt64Literal Literal(long value) => new ExprInt64Literal(value);
        /// <summary>Creates a nullable decimal literal.</summary>
        public static ExprDecimalLiteral Literal(decimal? value) => new ExprDecimalLiteral(value);
        /// <summary>Creates a decimal literal.</summary>
        public static ExprDecimalLiteral Literal(decimal value) => new ExprDecimalLiteral(value);
        /// <summary>Creates a nullable double-precision literal.</summary>
        public static ExprDoubleLiteral Literal(double? value) => new ExprDoubleLiteral(value);
        /// <summary>Creates a double-precision literal.</summary>
        public static ExprDoubleLiteral Literal(double value) => new ExprDoubleLiteral(value);
        /// <summary>Creates a nullable byte-array literal.</summary>
        public static ExprByteArrayLiteral Literal(IReadOnlyList<byte>? value) => new ExprByteArrayLiteral(value);
        /// <summary>Creates a GUID literal with an explicit SQL GUID cast.</summary>
        public static ExprCast LiteralCast(Guid? value) => Cast(Literal(value), SqlType.Guid);
        /// <summary>Creates a GUID literal with an explicit SQL GUID cast.</summary>
        public static ExprCast LiteralCast(Guid value) => Cast(Literal(value), SqlType.Guid);
        /// <summary>Creates a date/time literal with an explicit SQL date or date-time cast.</summary>
        public static ExprCast LiteralCast(DateTime? value, bool isDate = false) => Cast(Literal(value), SqlType.DateTime(isDate));
        /// <summary>Creates a date/time literal with an explicit SQL date or date-time cast.</summary>
        public static ExprCast LiteralCast(DateTime value, bool isDate = false) => Cast(Literal(value), SqlType.DateTime(isDate));
        /// <summary>Creates a date-time-offset literal with an explicit SQL cast.</summary>
        public static ExprCast LiteralCast(DateTimeOffset? value) => Cast(Literal(value), SqlType.DateTimeOffset);
        /// <summary>Creates a date-time-offset literal with an explicit SQL cast.</summary>
        public static ExprCast LiteralCast(DateTimeOffset value) => Cast(Literal(value), SqlType.DateTimeOffset);
        /// <summary>Creates a Boolean literal with an explicit SQL Boolean cast.</summary>
        public static ExprCast LiteralCast(bool? value) => Cast(Literal(value), SqlType.Boolean);
        /// <summary>Creates a Boolean literal with an explicit SQL Boolean cast.</summary>
        public static ExprCast LiteralCast(bool value) => Cast(Literal(value), SqlType.Boolean);
        /// <summary>Creates a byte literal with an explicit SQL byte cast.</summary>
        public static ExprCast LiteralCast(byte? value) => Cast(Literal(value), SqlType.Byte);
        /// <summary>Creates a byte literal with an explicit SQL byte cast.</summary>
        public static ExprCast LiteralCast(byte value) => Cast(Literal(value), SqlType.Byte);
        /// <summary>Creates a 16-bit integer literal with an explicit SQL cast.</summary>
        public static ExprCast LiteralCast(short? value) => Cast(Literal(value), SqlType.Int16);
        /// <summary>Creates a 16-bit integer literal with an explicit SQL cast.</summary>
        public static ExprCast LiteralCast(short value) => Cast(Literal(value), SqlType.Int16);
        /// <summary>Creates a 64-bit integer literal with an explicit SQL cast.</summary>
        public static ExprCast LiteralCast(long? value) => Cast(Literal(value), SqlType.Int64);
        /// <summary>Creates a 64-bit integer literal with an explicit SQL cast.</summary>
        public static ExprCast LiteralCast(long value) => Cast(Literal(value), SqlType.Int64);
        /// <summary>Creates a decimal literal with an explicit SQL decimal cast and optional precision and scale.</summary>
        public static ExprCast LiteralCast(decimal? value, DecimalPrecisionScale? precisionScale = null) => Cast(Literal(value), SqlType.Decimal(precisionScale));
        /// <summary>Creates a decimal literal with an explicit SQL decimal cast and optional precision and scale.</summary>
        public static ExprCast LiteralCast(decimal value, DecimalPrecisionScale? precisionScale = null) => Cast(Literal(value), SqlType.Decimal(precisionScale));
        /// <summary>Creates a double-precision literal with an explicit SQL cast.</summary>
        public static ExprCast LiteralCast(double? value) => Cast(Literal(value), SqlType.Double);
        /// <summary>Creates a double-precision literal with an explicit SQL cast.</summary>
        public static ExprCast LiteralCast(double value) => Cast(Literal(value), SqlType.Double);
    }
}
