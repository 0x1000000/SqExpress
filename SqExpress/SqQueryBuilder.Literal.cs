using System;
using System.Collections.Generic;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress
{
    public static partial class SqQueryBuilder
    {
        /// <summary>Represents a nullable 32-bit integer as a typed value node for SQL generation or parametrization.</summary>
        /// <param name="value">The integer value, or <see langword="null"/> for SQL <c>NULL</c>.</param>
        /// <returns>An integer literal AST node.</returns>
        public static ExprInt32Literal Literal(int? value) => new ExprInt32Literal(value);
        /// <summary>Represents a 32-bit integer as a typed value node for SQL generation or parametrization.</summary>
        /// <param name="value">The integer value.</param>
        /// <returns>An integer literal AST node.</returns>
        public static ExprInt32Literal Literal(int value) => new ExprInt32Literal(value);
        /// <summary>Represents text as a typed value node so the exporter can escape or parameterize it safely.</summary>
        /// <param name="value">The text, or <see langword="null"/> for SQL <c>NULL</c>.</param>
        /// <returns>A string literal AST node.</returns>
        public static ExprStringLiteral Literal(string? value) => new ExprStringLiteral(value);
        /// <summary>Represents a nullable GUID as a typed value node using the target dialect's compatible SQL form.</summary>
        /// <param name="value">The GUID, or <see langword="null"/> for SQL <c>NULL</c>.</param>
        /// <returns>A GUID literal AST node.</returns>
        public static ExprGuidLiteral Literal(Guid? value) => new ExprGuidLiteral(value);
        /// <summary>Represents a GUID as a typed value node using the target dialect's compatible SQL form.</summary>
        /// <param name="value">The GUID value.</param>
        /// <returns>A GUID literal AST node.</returns>
        public static ExprGuidLiteral Literal(Guid value) => new ExprGuidLiteral(value);
        /// <summary>Represents a nullable date and time so the exporter can format or parameterize it for the target database.</summary>
        /// <param name="value">The temporal value, or <see langword="null"/> for SQL <c>NULL</c>.</param>
        /// <returns>A date-time literal AST node.</returns>
        public static ExprDateTimeLiteral Literal(DateTime? value) => new ExprDateTimeLiteral(value);
        /// <summary>Represents a nullable date, time, and offset using the target dialect's compatible SQL form.</summary>
        /// <param name="value">The offset-aware temporal value, or <see langword="null"/> for SQL <c>NULL</c>.</param>
        /// <returns>A date-time-offset literal AST node.</returns>
        public static ExprDateTimeOffsetLiteral Literal(DateTimeOffset? value) => new ExprDateTimeOffsetLiteral(value);
        /// <summary>Represents a nullable Boolean using the selected database's Boolean-compatible SQL form.</summary>
        /// <param name="value">The Boolean value, or <see langword="null"/> for SQL <c>NULL</c>.</param>
        /// <returns>A Boolean literal AST node.</returns>
        public static ExprBoolLiteral Literal(bool? value) => new ExprBoolLiteral(value);
        /// <summary>Represents a Boolean using the selected database's Boolean-compatible SQL form.</summary>
        /// <param name="value">The Boolean value.</param>
        /// <returns>A Boolean literal AST node.</returns>
        public static ExprBoolLiteral Literal(bool value) => new ExprBoolLiteral(value);
        /// <summary>Represents a nullable unsigned byte using the target dialect's compatible numeric form.</summary>
        /// <param name="value">The byte value, or <see langword="null"/> for SQL <c>NULL</c>.</param>
        /// <returns>A byte literal AST node.</returns>
        public static ExprByteLiteral Literal(byte? value) => new ExprByteLiteral(value);
        /// <summary>Represents an unsigned byte using the target dialect's compatible numeric form.</summary>
        /// <param name="value">The byte value.</param>
        /// <returns>A byte literal AST node.</returns>
        public static ExprByteLiteral Literal(byte value) => new ExprByteLiteral(value);
        /// <summary>Represents a nullable 16-bit integer as a typed SQL value node.</summary>
        /// <param name="value">The integer value, or <see langword="null"/> for SQL <c>NULL</c>.</param>
        /// <returns>A 16-bit integer literal AST node.</returns>
        public static ExprInt16Literal Literal(short? value) => new ExprInt16Literal(value);
        /// <summary>Represents a 16-bit integer as a typed SQL value node.</summary>
        /// <param name="value">The integer value.</param>
        /// <returns>A 16-bit integer literal AST node.</returns>
        public static ExprInt16Literal Literal(short value) => new ExprInt16Literal(value);
        /// <summary>Represents a nullable 64-bit integer as a typed SQL value node.</summary>
        /// <param name="value">The integer value, or <see langword="null"/> for SQL <c>NULL</c>.</param>
        /// <returns>A 64-bit integer literal AST node.</returns>
        public static ExprInt64Literal Literal(long? value) => new ExprInt64Literal(value);
        /// <summary>Represents a 64-bit integer as a typed SQL value node.</summary>
        /// <param name="value">The integer value.</param>
        /// <returns>A 64-bit integer literal AST node.</returns>
        public static ExprInt64Literal Literal(long value) => new ExprInt64Literal(value);
        /// <summary>Represents a nullable decimal as an exact numeric SQL value node.</summary>
        /// <param name="value">The decimal value, or <see langword="null"/> for SQL <c>NULL</c>.</param>
        /// <returns>An exact numeric literal AST node.</returns>
        public static ExprDecimalLiteral Literal(decimal? value) => new ExprDecimalLiteral(value);
        /// <summary>Represents a decimal as an exact numeric SQL value node.</summary>
        /// <param name="value">The decimal value.</param>
        /// <returns>An exact numeric literal AST node.</returns>
        public static ExprDecimalLiteral Literal(decimal value) => new ExprDecimalLiteral(value);
        /// <summary>Represents a nullable double as an approximate numeric SQL value node.</summary>
        /// <param name="value">The floating-point value, or <see langword="null"/> for SQL <c>NULL</c>.</param>
        /// <returns>A double-precision literal AST node.</returns>
        public static ExprDoubleLiteral Literal(double? value) => new ExprDoubleLiteral(value);
        /// <summary>Represents a double as an approximate numeric SQL value node.</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns>A double-precision literal AST node.</returns>
        public static ExprDoubleLiteral Literal(double value) => new ExprDoubleLiteral(value);
        /// <summary>Represents binary data so the exporter can format or parameterize it for the target database.</summary>
        /// <param name="value">The bytes, or <see langword="null"/> for SQL <c>NULL</c>.</param>
        /// <returns>A binary literal AST node.</returns>
        public static ExprByteArrayLiteral Literal(IReadOnlyList<byte>? value) => new ExprByteArrayLiteral(value);
        /// <summary>Wraps a nullable GUID literal in an explicit cast to the selected dialect's GUID/UUID type.</summary>
        /// <param name="value">The GUID, or <see langword="null"/>.</param>
        /// <returns>A cast expression that preserves the intended SQL type even when the value is null.</returns>
        public static ExprCast LiteralCast(Guid? value) => Cast(Literal(value), SqlType.Guid);
        /// <summary>Wraps a GUID literal in an explicit cast to the selected dialect's GUID/UUID type.</summary>
        /// <param name="value">The GUID value.</param>
        /// <returns>A GUID-typed cast expression.</returns>
        public static ExprCast LiteralCast(Guid value) => Cast(Literal(value), SqlType.Guid);
        /// <summary>Wraps a nullable temporal literal in an explicit date-only or date-time cast.</summary>
        /// <param name="value">The temporal value, or <see langword="null"/>.</param>
        /// <param name="isDate"><see langword="true"/> to cast to date-only; otherwise to date and time.</param>
        /// <returns>A dialect-rendered temporal cast that retains type information for null values.</returns>
        public static ExprCast LiteralCast(DateTime? value, bool isDate = false) => Cast(Literal(value), SqlType.DateTime(isDate));
        /// <summary>Wraps a temporal literal in an explicit date-only or date-time cast.</summary>
        /// <param name="value">The temporal value.</param>
        /// <param name="isDate"><see langword="true"/> to cast to date-only; otherwise to date and time.</param>
        /// <returns>A dialect-rendered temporal cast expression.</returns>
        public static ExprCast LiteralCast(DateTime value, bool isDate = false) => Cast(Literal(value), SqlType.DateTime(isDate));
        /// <summary>Wraps a nullable offset-aware temporal literal in the dialect's explicit date-time-offset cast.</summary>
        /// <param name="value">The value, or <see langword="null"/>.</param>
        /// <returns>A date-time-offset-typed cast expression.</returns>
        public static ExprCast LiteralCast(DateTimeOffset? value) => Cast(Literal(value), SqlType.DateTimeOffset);
        /// <summary>Wraps an offset-aware temporal literal in the dialect's explicit date-time-offset cast.</summary>
        /// <param name="value">The offset-aware temporal value.</param>
        /// <returns>A date-time-offset-typed cast expression.</returns>
        public static ExprCast LiteralCast(DateTimeOffset value) => Cast(Literal(value), SqlType.DateTimeOffset);
        /// <summary>Wraps a nullable Boolean literal in the selected database's explicit Boolean-compatible cast.</summary>
        /// <param name="value">The Boolean value, or <see langword="null"/>.</param>
        /// <returns>A Boolean-typed cast expression.</returns>
        public static ExprCast LiteralCast(bool? value) => Cast(Literal(value), SqlType.Boolean);
        /// <summary>Wraps a Boolean literal in the selected database's explicit Boolean-compatible cast.</summary>
        /// <param name="value">The Boolean value.</param>
        /// <returns>A Boolean-typed cast expression.</returns>
        public static ExprCast LiteralCast(bool value) => Cast(Literal(value), SqlType.Boolean);
        /// <summary>Wraps a nullable byte literal in the selected database's closest unsigned-byte cast.</summary>
        /// <param name="value">The byte value, or <see langword="null"/>.</param>
        /// <returns>A byte-typed cast expression.</returns>
        public static ExprCast LiteralCast(byte? value) => Cast(Literal(value), SqlType.Byte);
        /// <summary>Wraps a byte literal in the selected database's closest unsigned-byte cast.</summary>
        /// <param name="value">The byte value.</param>
        /// <returns>A byte-typed cast expression.</returns>
        public static ExprCast LiteralCast(byte value) => Cast(Literal(value), SqlType.Byte);
        /// <summary>Wraps a nullable integer literal in an explicit 16-bit SQL integer cast.</summary>
        /// <param name="value">The integer value, or <see langword="null"/>.</param>
        /// <returns>A 16-bit-integer-typed cast expression.</returns>
        public static ExprCast LiteralCast(short? value) => Cast(Literal(value), SqlType.Int16);
        /// <summary>Wraps an integer literal in an explicit 16-bit SQL integer cast.</summary>
        /// <param name="value">The integer value.</param>
        /// <returns>A 16-bit-integer-typed cast expression.</returns>
        public static ExprCast LiteralCast(short value) => Cast(Literal(value), SqlType.Int16);
        /// <summary>Wraps a nullable integer literal in an explicit 64-bit SQL integer cast.</summary>
        /// <param name="value">The integer value, or <see langword="null"/>.</param>
        /// <returns>A 64-bit-integer-typed cast expression.</returns>
        public static ExprCast LiteralCast(long? value) => Cast(Literal(value), SqlType.Int64);
        /// <summary>Wraps an integer literal in an explicit 64-bit SQL integer cast.</summary>
        /// <param name="value">The integer value.</param>
        /// <returns>A 64-bit-integer-typed cast expression.</returns>
        public static ExprCast LiteralCast(long value) => Cast(Literal(value), SqlType.Int64);
        /// <summary>Wraps a nullable decimal literal in an explicit exact-numeric cast.</summary>
        /// <param name="value">The decimal value, or <see langword="null"/>.</param>
        /// <param name="precisionScale">Optional total precision and fractional scale; <see langword="null"/> uses the dialect default.</param>
        /// <returns>A decimal-typed cast expression.</returns>
        public static ExprCast LiteralCast(decimal? value, DecimalPrecisionScale? precisionScale = null) => Cast(Literal(value), SqlType.Decimal(precisionScale));
        /// <summary>Wraps a decimal literal in an explicit exact-numeric cast.</summary>
        /// <param name="value">The decimal value.</param>
        /// <param name="precisionScale">Optional total precision and fractional scale; <see langword="null"/> uses the dialect default.</param>
        /// <returns>A decimal-typed cast expression.</returns>
        public static ExprCast LiteralCast(decimal value, DecimalPrecisionScale? precisionScale = null) => Cast(Literal(value), SqlType.Decimal(precisionScale));
        /// <summary>Wraps a nullable floating-point literal in the dialect's double-precision cast.</summary>
        /// <param name="value">The floating-point value, or <see langword="null"/>.</param>
        /// <returns>A double-precision-typed cast expression.</returns>
        public static ExprCast LiteralCast(double? value) => Cast(Literal(value), SqlType.Double);
        /// <summary>Wraps a floating-point literal in the dialect's double-precision cast.</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns>A double-precision-typed cast expression.</returns>
        public static ExprCast LiteralCast(double value) => Cast(Literal(value), SqlType.Double);
    }
}
