#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SqExpress.Syntax.Value;

namespace SqExpress.Test
{
    [TestFixture]
    public class ParamValueTest
    {
        [Test]
        public void Scalar_Int32_CreatesSingleValue()
        {
            ParamValue paramValue = 42;

            Assert.That(paramValue.IsSingle, Is.True);
            Assert.That(paramValue.IsList, Is.False);
            Assert.That(paramValue.AsSingle, Is.TypeOf<ExprInt32Literal>());
            Assert.That(((ExprInt32Literal)paramValue.AsSingle).Value, Is.EqualTo(42));
        }

        [Test]
        public void Scalar_NullString_CreatesSingleValue()
        {
            ParamValue paramValue = (string?)null;

            Assert.That(paramValue.IsSingle, Is.True);
            Assert.That(paramValue.IsList, Is.False);
            Assert.That(paramValue.AsSingle, Is.TypeOf<ExprStringLiteral>());
            Assert.That(((ExprStringLiteral)paramValue.AsSingle).Value, Is.Null);
        }

        [Test]
        public void SupportedScalarTypes_AreAccepted()
        {
            DateTime dateTime = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            DateTimeOffset dateTimeOffset = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
            Guid guid = Guid.NewGuid();

            ParamValue[] values =
            {
                "A",
                true,
                (bool?)false,
                1,
                (int?)2,
                (byte)3,
                (byte?)4,
                (short)5,
                (short?)6,
                7L,
                (long?)8L,
                9.1m,
                (decimal?)10.2m,
                11.3d,
                (double?)12.4d,
                guid,
                (Guid?)guid,
                dateTime,
                (DateTime?)dateTime,
                dateTimeOffset,
                (DateTimeOffset?)dateTimeOffset
            };

            foreach (var value in values)
            {
                Assert.That(value.IsSingle, Is.True);
                Assert.That(value.IsList, Is.False);
                Assert.That(value.AsSingle, Is.Not.Null);
            }
        }

        [Test]
        public void Array_WithOneElement_CollapsesToSingleValue()
        {
            ParamValue paramValue = new[] { 5 };

            Assert.That(paramValue.IsSingle, Is.True);
            Assert.That(paramValue.IsList, Is.False);
            Assert.That(paramValue.AsSingle, Is.TypeOf<ExprInt32Literal>());
            Assert.That(((ExprInt32Literal)paramValue.AsSingle).Value, Is.EqualTo(5));
        }

        [Test]
        public void Array_WithMultipleElements_CreatesList()
        {
            ParamValue paramValue = new[] { 1, 2, 3 };

            Assert.That(paramValue.IsSingle, Is.False);
            Assert.That(paramValue.IsList, Is.True);
            Assert.That(paramValue.AsList.Count, Is.EqualTo(3));
            Assert.That(paramValue.AsList[0], Is.TypeOf<ExprInt32Literal>());
            Assert.That(((ExprInt32Literal)paramValue.AsList[0]).Value, Is.EqualTo(1));
            Assert.That(((ExprInt32Literal)paramValue.AsList[1]).Value, Is.EqualTo(2));
            Assert.That(((ExprInt32Literal)paramValue.AsList[2]).Value, Is.EqualTo(3));
        }

        [Test]
        public void NullableArray_PreservesNullValue()
        {
            ParamValue paramValue = new int?[] { 1, null };

            Assert.That(paramValue.IsList, Is.True);
            Assert.That(paramValue.AsList.Count, Is.EqualTo(2));
            Assert.That(((ExprInt32Literal)paramValue.AsList[0]).Value, Is.EqualTo(1));
            Assert.That(((ExprInt32Literal)paramValue.AsList[1]).Value, Is.Null);
        }

        [Test]
        public void EmptyArray_Throws()
        {
            var ex = Assert.Throws<SqExpressException>(() =>
            {
                ParamValue _ = Array.Empty<int>();
            });

            Assert.That(ex!.Message, Does.Contain("Cannot be empty"));
        }

        [Test]
        public void HashSet_CreatesList()
        {
            ParamValue paramValue = new HashSet<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            Assert.That(paramValue.IsSingle, Is.False);
            Assert.That(paramValue.IsList, Is.True);
            Assert.That(paramValue.AsList.Count, Is.EqualTo(2));
            Assert.That(paramValue.AsList[0], Is.TypeOf<ExprGuidLiteral>());
            Assert.That(paramValue.AsList[1], Is.TypeOf<ExprGuidLiteral>());
        }

#if NET8_0_OR_GREATER
        [Test]
        public void Span_CreatesList()
        {
            ParamValue paramValue = (ReadOnlySpan<DateTimeOffset>)new[]
            {
                new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
                new DateTimeOffset(2024, 6, 7, 8, 9, 10, TimeSpan.Zero)
            };

            Assert.That(paramValue.IsSingle, Is.False);
            Assert.That(paramValue.IsList, Is.True);
            Assert.That(paramValue.AsList.Count, Is.EqualTo(2));
            Assert.That(paramValue.AsList[0], Is.TypeOf<ExprDateTimeOffsetLiteral>());
            Assert.That(paramValue.AsList[1], Is.TypeOf<ExprDateTimeOffsetLiteral>());
        }

        [Test]
        public void CollectionExpression_CreatesList()
        {
            ParamValue paramValue = [1, 2, 3];

            Assert.That(paramValue.IsSingle, Is.False);
            Assert.That(paramValue.IsList, Is.True);
            Assert.That(paramValue.AsList.Count, Is.EqualTo(3));
            Assert.That(((ExprInt32Literal)paramValue.AsList[0]).Value, Is.EqualTo(1));
            Assert.That(((ExprInt32Literal)paramValue.AsList[1]).Value, Is.EqualTo(2));
            Assert.That(((ExprInt32Literal)paramValue.AsList[2]).Value, Is.EqualTo(3));
        }
#endif
    }
}
