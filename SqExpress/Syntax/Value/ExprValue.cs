using System;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Select;

namespace SqExpress.Syntax.Value
{
    public abstract class ExprValue : ExprSelecting, IExprAssigning
    {
        public bool Equals(ExprValue? other) 
            => base.Equals(other);

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExprValue) obj);
        }

        public override int GetHashCode() 
            => base.GetHashCode();

        //Types
        public static implicit operator ExprValue(string? value)
            => new ExprStringLiteral(value);

        public static implicit operator ExprValue(bool value)
            => new ExprBoolLiteral(value);

        public static implicit operator ExprValue(bool? value)
            => new ExprBoolLiteral(value);

        public static implicit operator ExprValue(int value)
            => new ExprInt32Literal(value);

        public static implicit operator ExprValue(int? value)
            => new ExprInt32Literal(value);

        public static implicit operator ExprValue(byte value)
            => new ExprByteLiteral(value);

        public static implicit operator ExprValue(byte? value)
            => new ExprByteLiteral(value);

        public static implicit operator ExprValue(short value)
            => new ExprInt16Literal(value);

        public static implicit operator ExprValue(short? value)
            => new ExprInt16Literal(value);

        public static implicit operator ExprValue(long value)
            => new ExprInt64Literal(value);

        public static implicit operator ExprValue(long? value)
            => new ExprInt64Literal(value);

        public static implicit operator ExprValue(decimal value)
            => new ExprDecimalLiteral(value);

        public static implicit operator ExprValue(decimal? value)
            => new ExprDecimalLiteral(value);

        public static implicit operator ExprValue(double value)
            => new ExprDoubleLiteral(value);

        public static implicit operator ExprValue(double? value)
            => new ExprDoubleLiteral(value);

        public static implicit operator ExprValue(Guid value)
            => new ExprGuidLiteral(value);

        public static implicit operator ExprValue(Guid? value)
            => new ExprGuidLiteral(value);

        public static implicit operator ExprValue(DateTime value)
            => new ExprDateTimeLiteral(value);

        public static implicit operator ExprValue(DateTime? value)
            => new ExprDateTimeLiteral(value);

        //Summary
        public static ExprSum operator +(ExprValue a, ExprValue b)
            => new ExprSum(a, b);

        //String
        public static ExprStringConcat operator +(ExprValue a, string b)
            => new ExprStringConcat(a, SqQueryBuilder.Literal(b));

        public static ExprStringConcat operator +(string a, ExprValue b)
            => new ExprStringConcat(SqQueryBuilder.Literal(a), b);

        //Int
        public static ExprSum operator +(ExprValue a, int b)
            => new ExprSum(a, SqQueryBuilder.Literal(b));

        public static ExprSum operator +(int a, ExprValue b)
            => new ExprSum(SqQueryBuilder.Literal(a), b);

        public static ExprSum operator +(ExprValue a, int? b)
            => new ExprSum(a, SqQueryBuilder.Literal(b));

        public static ExprSum operator +(int? a, ExprValue b)
            => new ExprSum(SqQueryBuilder.Literal(a), b);

        //Double
        public static ExprSum operator +(ExprValue a, double b)
            => new ExprSum(a, SqQueryBuilder.Literal(b));

        public static ExprSum operator +(double a, ExprValue b)
            => new ExprSum(SqQueryBuilder.Literal(a), b);

        public static ExprSum operator +(ExprValue a, double? b)
            => new ExprSum(a, SqQueryBuilder.Literal(b));

        public static ExprSum operator +(double? a, ExprValue b)
            => new ExprSum(SqQueryBuilder.Literal(a), b);

        //Subtraction

        public static ExprSub operator -(ExprValue a, ExprValue b)
            => new ExprSub(a, b);

        //Int
        public static ExprSub operator -(ExprValue a, int b)
            => new ExprSub(a, SqQueryBuilder.Literal(b));

        public static ExprSub operator -(int a, ExprValue b)
            => new ExprSub(SqQueryBuilder.Literal(a), b);

        public static ExprSub operator -(ExprValue a, int? b)
            => new ExprSub(a, SqQueryBuilder.Literal(b));

        public static ExprSub operator -(int? a, ExprValue b)
            => new ExprSub(SqQueryBuilder.Literal(a), b);

        //Double
        public static ExprSub operator -(ExprValue a, double b)
            => new ExprSub(a, SqQueryBuilder.Literal(b));

        public static ExprSub operator -(double a, ExprValue b)
            => new ExprSub(SqQueryBuilder.Literal(a), b);

        public static ExprSub operator -(ExprValue a, double? b)
            => new ExprSub(a, SqQueryBuilder.Literal(b));

        public static ExprSub operator -(double? a, ExprValue b)
            => new ExprSub(SqQueryBuilder.Literal(a), b);

        //Multiplication

        public static ExprMul operator *(ExprValue a, ExprValue b)
            => new ExprMul(a, b);

        //Int
        public static ExprMul operator *(ExprValue a, int b)
            => new ExprMul(a, SqQueryBuilder.Literal(b));

        public static ExprMul operator *(int a, ExprValue b)
            => new ExprMul(SqQueryBuilder.Literal(a), b);

        public static ExprMul operator *(ExprValue a, int? b)
            => new ExprMul(a, SqQueryBuilder.Literal(b));

        public static ExprMul operator *(int? a, ExprValue b)
            => new ExprMul(SqQueryBuilder.Literal(a), b);

        //Double
        public static ExprMul operator *(ExprValue a, double b)
            => new ExprMul(a, SqQueryBuilder.Literal(b));

        public static ExprMul operator *(double a, ExprValue b)
            => new ExprMul(SqQueryBuilder.Literal(a), b);

        public static ExprMul operator *(ExprValue a, double? b)
            => new ExprMul(a, SqQueryBuilder.Literal(b));

        public static ExprMul operator *(double? a, ExprValue b)
            => new ExprMul(SqQueryBuilder.Literal(a), b);

        //Division

        public static ExprDiv operator /(ExprValue a, ExprValue b)
            => new ExprDiv(a, b);

        //Int
        public static ExprDiv operator /(ExprValue a, int b)
            => new ExprDiv(a, SqQueryBuilder.Literal(b));

        public static ExprDiv operator /(int a, ExprValue b)
            => new ExprDiv(SqQueryBuilder.Literal(a), b);

        public static ExprDiv operator /(ExprValue a, int? b)
            => new ExprDiv(a, SqQueryBuilder.Literal(b));

        public static ExprDiv operator /(int? a, ExprValue b)
            => new ExprDiv(SqQueryBuilder.Literal(a), b);

        //Double
        public static ExprDiv operator /(ExprValue a, double b)
            => new ExprDiv(a, SqQueryBuilder.Literal(b));

        public static ExprDiv operator /(double a, ExprValue b)
            => new ExprDiv(SqQueryBuilder.Literal(a), b);

        public static ExprDiv operator /(ExprValue a, double? b)
            => new ExprDiv(a, SqQueryBuilder.Literal(b));

        public static ExprDiv operator /(double? a, ExprValue b)
            => new ExprDiv(SqQueryBuilder.Literal(a), b);

        //Remainder

        public static ExprModulo operator %(ExprValue a, ExprValue b)
            => new ExprModulo(a, b);

        //Int
        public static ExprModulo operator %(ExprValue a, int b)
            => new ExprModulo(a, SqQueryBuilder.Literal(b));

        public static ExprModulo operator %(int a, ExprValue b)
            => new ExprModulo(SqQueryBuilder.Literal(a), b);

        public static ExprModulo operator %(ExprValue a, int? b)
            => new ExprModulo(a, SqQueryBuilder.Literal(b));

        public static ExprModulo operator %(int? a, ExprValue b)
            => new ExprModulo(SqQueryBuilder.Literal(a), b);

        //Boolean

        //Another Value
        public static ExprPredicateLeftRight operator ==(ExprValue a, ExprValue b)
            => new ExprBooleanEq(a, b);

        public static ExprPredicateLeftRight operator !=(ExprValue a, ExprValue b)
            => new ExprBooleanNotEq(a, b);

        //Int
        public static ExprPredicateLeftRight operator ==(ExprValue a, int b)
            => new ExprBooleanEq(a, new ExprInt32Literal(b));

        public static ExprPredicateLeftRight operator ==(int a, ExprValue b)
            => new ExprBooleanEq(new ExprInt32Literal(a), b);

        public static ExprPredicateLeftRight operator !=(ExprValue a, int b)
            => new ExprBooleanNotEq(a, new ExprInt32Literal(b));

        public static ExprPredicateLeftRight operator !=(int a, ExprValue b)
            => new ExprBooleanNotEq(new ExprInt32Literal(a), b);

        public static ExprPredicateLeftRight operator >(ExprValue a, int b)
            => new ExprBooleanGt(a, new ExprInt32Literal(b));

        public static ExprPredicateLeftRight operator >(int a, ExprValue b)
            => new ExprBooleanGt(new ExprInt32Literal(a), b);

        public static ExprPredicateLeftRight operator <(ExprValue a, int b)
            => new ExprBooleanLt(a, new ExprInt32Literal(b));

        public static ExprPredicateLeftRight operator <(int a, ExprValue b)
            => new ExprBooleanLt(new ExprInt32Literal(a), b);

        public static ExprPredicateLeftRight operator >=(ExprValue a, int b)
            => new ExprBooleanGtEq(a, new ExprInt32Literal(b));

        public static ExprPredicateLeftRight operator >=(int a, ExprValue b)
            => new ExprBooleanGtEq(new ExprInt32Literal(a), b);

        public static ExprPredicateLeftRight operator <=(ExprValue a, int b)
            => new ExprBooleanLtEq(a, new ExprInt32Literal(b));

        public static ExprPredicateLeftRight operator <=(int a, ExprValue b)
            => new ExprBooleanLtEq(new ExprInt32Literal(a), b);

        //Short
        public static ExprPredicateLeftRight operator ==(ExprValue a, short b)
            => new ExprBooleanEq(a, new ExprInt16Literal(b));

        public static ExprPredicateLeftRight operator ==(short a, ExprValue b)
            => new ExprBooleanEq(new ExprInt16Literal(a), b);

        public static ExprPredicateLeftRight operator !=(ExprValue a, short b)
            => new ExprBooleanNotEq(a, new ExprInt16Literal(b));

        public static ExprPredicateLeftRight operator !=(short a, ExprValue b)
            => new ExprBooleanNotEq(new ExprInt16Literal(a), b);

        public static ExprPredicateLeftRight operator >(ExprValue a, short b)
            => new ExprBooleanGt(a, new ExprInt16Literal(b));

        public static ExprPredicateLeftRight operator >(short a, ExprValue b)
            => new ExprBooleanGt(new ExprInt16Literal(a), b);

        public static ExprPredicateLeftRight operator <(ExprValue a, short b)
            => new ExprBooleanLt(a, new ExprInt16Literal(b));

        public static ExprPredicateLeftRight operator <(short a, ExprValue b)
            => new ExprBooleanLt(new ExprInt16Literal(a), b);

        public static ExprPredicateLeftRight operator >=(ExprValue a, short b)
            => new ExprBooleanGtEq(a, new ExprInt16Literal(b));

        public static ExprPredicateLeftRight operator >=(short a, ExprValue b)
            => new ExprBooleanGtEq(new ExprInt16Literal(a), b);

        public static ExprPredicateLeftRight operator <=(ExprValue a, short b)
            => new ExprBooleanLtEq(a, new ExprInt16Literal(b));

        public static ExprPredicateLeftRight operator <=(short a, ExprValue b)
            => new ExprBooleanLtEq(new ExprInt16Literal(a), b);

        //Byte
        public static ExprPredicateLeftRight operator ==(ExprValue a, byte b)
            => new ExprBooleanEq(a, new ExprByteLiteral(b));

        public static ExprPredicateLeftRight operator ==(byte a, ExprValue b)
            => new ExprBooleanEq(new ExprByteLiteral(a), b);

        public static ExprPredicateLeftRight operator !=(ExprValue a, byte b)
            => new ExprBooleanNotEq(a, new ExprByteLiteral(b));

        public static ExprPredicateLeftRight operator !=(byte a, ExprValue b)
            => new ExprBooleanNotEq(new ExprByteLiteral(a), b);

        public static ExprPredicateLeftRight operator >(ExprValue a, byte b)
            => new ExprBooleanGt(a, new ExprByteLiteral(b));

        public static ExprPredicateLeftRight operator >(byte a, ExprValue b)
            => new ExprBooleanGt(new ExprByteLiteral(a), b);

        public static ExprPredicateLeftRight operator <(ExprValue a, byte b)
            => new ExprBooleanLt(a, new ExprByteLiteral(b));

        public static ExprPredicateLeftRight operator <(byte a, ExprValue b)
            => new ExprBooleanLt(new ExprByteLiteral(a), b);

        public static ExprPredicateLeftRight operator >=(ExprValue a, byte b)
            => new ExprBooleanGtEq(a, new ExprByteLiteral(b));

        public static ExprPredicateLeftRight operator >=(byte a, ExprValue b)
            => new ExprBooleanGtEq(new ExprByteLiteral(a), b);

        public static ExprPredicateLeftRight operator <=(ExprValue a, byte b)
            => new ExprBooleanLtEq(a, new ExprByteLiteral(b));

        public static ExprPredicateLeftRight operator <=(byte a, ExprValue b)
            => new ExprBooleanLtEq(new ExprByteLiteral(a), b);

        //Double
        public static ExprPredicateLeftRight operator ==(ExprValue a, double b)
            => new ExprBooleanEq(a, new ExprDoubleLiteral(b));

        public static ExprPredicateLeftRight operator ==(double a, ExprValue b)
            => new ExprBooleanEq(new ExprDoubleLiteral(a), b);

        public static ExprPredicateLeftRight operator !=(ExprValue a, double b)
            => new ExprBooleanNotEq(a, new ExprDoubleLiteral(b));

        public static ExprPredicateLeftRight operator !=(double a, ExprValue b)
            => new ExprBooleanNotEq(new ExprDoubleLiteral(a), b);

        public static ExprPredicateLeftRight operator >(ExprValue a, double b)
            => new ExprBooleanGt(a, new ExprDoubleLiteral(b));

        public static ExprPredicateLeftRight operator >(double a, ExprValue b)
            => new ExprBooleanGt(new ExprDoubleLiteral(a), b);

        public static ExprPredicateLeftRight operator <(ExprValue a, double b)
            => new ExprBooleanLt(a, new ExprDoubleLiteral(b));

        public static ExprPredicateLeftRight operator <(double a, ExprValue b)
            => new ExprBooleanLt(new ExprDoubleLiteral(a), b);

        public static ExprPredicateLeftRight operator >=(ExprValue a, double b)
            => new ExprBooleanGtEq(a, new ExprDoubleLiteral(b));

        public static ExprPredicateLeftRight operator >=(double a, ExprValue b)
            => new ExprBooleanGtEq(new ExprDoubleLiteral(a), b);

        public static ExprPredicateLeftRight operator <=(ExprValue a, double b)
            => new ExprBooleanLtEq(a, new ExprDoubleLiteral(b));

        public static ExprPredicateLeftRight operator <=(double a, ExprValue b)
            => new ExprBooleanLtEq(new ExprDoubleLiteral(a), b);

        //Decimal
        public static ExprPredicateLeftRight operator ==(ExprValue a, decimal b)
            => new ExprBooleanEq(a, new ExprDecimalLiteral(b));

        public static ExprPredicateLeftRight operator ==(decimal a, ExprValue b)
            => new ExprBooleanEq(new ExprDecimalLiteral(a), b);

        public static ExprPredicateLeftRight operator !=(ExprValue a, decimal b)
            => new ExprBooleanNotEq(a, new ExprDecimalLiteral(b));

        public static ExprPredicateLeftRight operator !=(decimal a, ExprValue b)
            => new ExprBooleanNotEq(new ExprDecimalLiteral(a), b);

        public static ExprPredicateLeftRight operator >(ExprValue a, decimal b)
            => new ExprBooleanGt(a, new ExprDecimalLiteral(b));

        public static ExprPredicateLeftRight operator >(decimal a, ExprValue b)
            => new ExprBooleanGt(new ExprDecimalLiteral(a), b);

        public static ExprPredicateLeftRight operator <(ExprValue a, decimal b)
            => new ExprBooleanLt(a, new ExprDecimalLiteral(b));

        public static ExprPredicateLeftRight operator <(decimal a, ExprValue b)
            => new ExprBooleanLt(new ExprDecimalLiteral(a), b);

        public static ExprPredicateLeftRight operator >=(ExprValue a, decimal b)
            => new ExprBooleanGtEq(a, new ExprDecimalLiteral(b));

        public static ExprPredicateLeftRight operator >=(decimal a, ExprValue b)
            => new ExprBooleanGtEq(new ExprDecimalLiteral(a), b);

        public static ExprPredicateLeftRight operator <=(ExprValue a, decimal b)
            => new ExprBooleanLtEq(a, new ExprDecimalLiteral(b));

        public static ExprPredicateLeftRight operator <=(decimal a, ExprValue b)
            => new ExprBooleanLtEq(new ExprDecimalLiteral(a), b);

        //Long
        public static ExprPredicateLeftRight operator ==(ExprValue a, long b)
            => new ExprBooleanEq(a, new ExprInt64Literal(b));

        public static ExprPredicateLeftRight operator ==(long a, ExprValue b)
            => new ExprBooleanEq(new ExprInt64Literal(a), b);

        public static ExprPredicateLeftRight operator !=(ExprValue a, long b)
            => new ExprBooleanNotEq(a, new ExprInt64Literal(b));

        public static ExprPredicateLeftRight operator !=(long a, ExprValue b)
            => new ExprBooleanNotEq(new ExprInt64Literal(a), b);

        public static ExprPredicateLeftRight operator >(ExprValue a, long b)
            => new ExprBooleanGt(a, new ExprInt64Literal(b));

        public static ExprPredicateLeftRight operator >(long a, ExprValue b)
            => new ExprBooleanGt(new ExprInt64Literal(a), b);

        public static ExprPredicateLeftRight operator <(ExprValue a, long b)
            => new ExprBooleanLt(a, new ExprInt64Literal(b));

        public static ExprPredicateLeftRight operator <(long a, ExprValue b)
            => new ExprBooleanLt(new ExprInt64Literal(a), b);

        public static ExprPredicateLeftRight operator >=(ExprValue a, long b)
            => new ExprBooleanGtEq(a, new ExprInt64Literal(b));

        public static ExprPredicateLeftRight operator >=(long a, ExprValue b)
            => new ExprBooleanGtEq(new ExprInt64Literal(a), b);

        public static ExprPredicateLeftRight operator <=(ExprValue a, long b)
            => new ExprBooleanLtEq(a, new ExprInt64Literal(b));

        public static ExprPredicateLeftRight operator <=(long a, ExprValue b)
            => new ExprBooleanLtEq(new ExprInt64Literal(a), b);


        //DateTime
        public static ExprPredicateLeftRight operator ==(ExprValue a, DateTime b)
            => new ExprBooleanEq(a, new ExprDateTimeLiteral(b));

        public static ExprPredicateLeftRight operator ==(DateTime a, ExprValue b)
            => new ExprBooleanEq(new ExprDateTimeLiteral(a), b);

        public static ExprPredicateLeftRight operator !=(ExprValue a, DateTime b)
            => new ExprBooleanNotEq(a, new ExprDateTimeLiteral(b));

        public static ExprPredicateLeftRight operator !=(DateTime a, ExprValue b)
            => new ExprBooleanNotEq(new ExprDateTimeLiteral(a), b);

        public static ExprPredicateLeftRight operator >(ExprValue a, DateTime b)
            => new ExprBooleanGt(a, new ExprDateTimeLiteral(b));

        public static ExprPredicateLeftRight operator >(DateTime a, ExprValue b)
            => new ExprBooleanGt(new ExprDateTimeLiteral(a), b);

        public static ExprPredicateLeftRight operator <(ExprValue a, DateTime b)
            => new ExprBooleanLt(a, new ExprDateTimeLiteral(b));

        public static ExprPredicateLeftRight operator <(DateTime a, ExprValue b)
            => new ExprBooleanLt(new ExprDateTimeLiteral(a), b);

        public static ExprPredicateLeftRight operator >=(ExprValue a, DateTime b)
            => new ExprBooleanGtEq(a, new ExprDateTimeLiteral(b));

        public static ExprPredicateLeftRight operator >=(DateTime a, ExprValue b)
            => new ExprBooleanGtEq(new ExprDateTimeLiteral(a), b);

        public static ExprPredicateLeftRight operator <=(ExprValue a, DateTime b)
            => new ExprBooleanLtEq(a, new ExprDateTimeLiteral(b));

        public static ExprPredicateLeftRight operator <=(DateTime a, ExprValue b)
            => new ExprBooleanLtEq(new ExprDateTimeLiteral(a), b);


        //Guid
        public static ExprPredicateLeftRight operator ==(ExprValue a, Guid b)
            => new ExprBooleanEq(a, new ExprGuidLiteral(b));

        public static ExprPredicateLeftRight operator ==(Guid a, ExprValue b)
            => new ExprBooleanEq(new ExprGuidLiteral(a), b);

        public static ExprPredicateLeftRight operator !=(ExprValue a, Guid b)
            => new ExprBooleanNotEq(a, new ExprGuidLiteral(b));

        public static ExprPredicateLeftRight operator !=(Guid a, ExprValue b)
            => new ExprBooleanNotEq(new ExprGuidLiteral(a), b);

        //String
        public static ExprPredicateLeftRight operator ==(ExprValue a, string b)
            => new ExprBooleanEq(a, new ExprStringLiteral(b));

        public static ExprPredicateLeftRight operator ==(string a, ExprValue b)
            => new ExprBooleanEq(new ExprStringLiteral(a), b);

        public static ExprPredicateLeftRight operator !=(ExprValue a, string b)
            => new ExprBooleanNotEq(a, new ExprStringLiteral(b));

        public static ExprPredicateLeftRight operator !=(string a, ExprValue b)
            => new ExprBooleanNotEq(new ExprStringLiteral(a), b);

        //Bool
        public static ExprPredicateLeftRight operator ==(ExprValue a, bool b)
            => new ExprBooleanEq(a, new ExprBoolLiteral(b));

        public static ExprPredicateLeftRight operator ==(bool a, ExprValue b)
            => new ExprBooleanEq(new ExprBoolLiteral(a), b);

        public static ExprPredicateLeftRight operator !=(ExprValue a, bool b)
            => new ExprBooleanNotEq(a, new ExprBoolLiteral(b));

        public static ExprPredicateLeftRight operator !=(bool a, ExprValue b)
            => new ExprBooleanNotEq(new ExprBoolLiteral(a), b);
    }
}