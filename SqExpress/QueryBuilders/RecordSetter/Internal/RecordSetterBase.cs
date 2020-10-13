using System;
using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Value;

namespace SqExpress.QueryBuilders.RecordSetter.Internal
{
    internal abstract class RecordSetterBase<TNext> : IRecordSetter<TNext>
    {
        protected abstract TNext SetGeneric(ExprColumnName column, ExprLiteral value);

        public TNext Set(BooleanTableColumn column, bool value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(ByteTableColumn column, byte value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(Int16TableColumn column, short value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(Int32TableColumn column, int value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(Int64TableColumn column, long value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(DecimalTableColumn column, decimal value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(DoubleTableColumn column, double value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(DateTimeTableColumn column, DateTime value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(GuidTableColumn column, Guid value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(StringTableColumn column, string value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableBooleanTableColumn column, bool? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableByteTableColumn column, byte? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableInt16TableColumn column, short? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableInt32TableColumn column, int? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableInt64TableColumn column, long? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableDecimalTableColumn column, decimal? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableDoubleTableColumn column, double? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableDateTimeTableColumn column, DateTime? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableGuidTableColumn column, Guid? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableStringTableColumn column, string? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(BooleanCustomColumn column, bool value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(ByteCustomColumn column, byte value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(Int16CustomColumn column, short value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(Int32CustomColumn column, int value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(Int64CustomColumn column, long value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(DecimalCustomColumn column, decimal value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(DoubleCustomColumn column, double value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(DateTimeCustomColumn column, DateTime value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(GuidCustomColumn column, Guid value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(StringCustomColumn column, string value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableBooleanCustomColumn column, bool? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableByteCustomColumn column, byte? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableInt16CustomColumn column, short? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableInt32CustomColumn column, int? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableInt64CustomColumn column, long? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableDecimalCustomColumn column, decimal? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableDoubleCustomColumn column, double? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableDateTimeCustomColumn column, DateTime? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableGuidCustomColumn column, Guid? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(NullableStringCustomColumn column, string? value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }

        public TNext Set(ExprColumnName column, IReadOnlyList<byte> value)
        {
            return this.SetGeneric(column, SqQueryBuilder.Literal(value));
        }
    }
}