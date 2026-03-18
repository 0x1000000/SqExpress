using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.SqlParser;
using SqExpress.Syntax;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios;

public class ScParserParamsExprValues : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        var boolValue = true;
        var byteValue = (byte)7;
        var int16Value = (short)1234;
        var int32Value = 567890;
        var int64Value = 1234567890123L;
        var decimalValue = 12345.678m;
        var doubleValue = 12345.25d;
        var stringValue = "Hello Ж";
        var guidValue = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var dateTimeValue = new DateTime(2024, 11, 12, 13, 14, 15);
        var bytesValue = new byte[] { 0, 1, 2, 3, 255 };
        var dateTimeOffsetValue = new DateTimeOffset(new DateTime(2024, 11, 12, 13, 14, 15), TimeSpan.FromHours(3));

        var sql = context.Dialect.IsMySqlFamily()
            ? "SELECT @pBool [BoolV],@pByte [ByteV],@pInt16 [Int16V],@pInt32 [Int32V],@pInt64 [Int64V],@pDecimal [DecimalV],@pDouble [DoubleV],@pString [StringV],@pGuid [GuidV],@pDateTime [DateTimeV],@pBytes [BytesV],@pNull [NullV]"
            : "SELECT @pBool [BoolV],@pByte [ByteV],@pInt16 [Int16V],@pInt32 [Int32V],@pInt64 [Int64V],@pDecimal [DecimalV],@pDouble [DoubleV],@pString [StringV],@pGuid [GuidV],@pDateTime [DateTimeV],@pDateTimeOffset [DateTimeOffsetV],@pBytes [BytesV],@pNull [NullV]";

        if (!SqTSqlParser.TryParse(sql, out IExpr? parsedExpr, out var error))
        {
            throw new Exception(error ?? "Could not parse sql");
        }

        IExpr boundExpr;
        if (context.Dialect.IsMySqlFamily())
        {
            boundExpr = parsedExpr!.WithParams(
                ("pBool", boolValue),
                ("pByte", byteValue),
                ("pInt16", int16Value),
                ("pInt32", int32Value),
                ("pInt64", int64Value),
                ("pDecimal", decimalValue),
                ("pDouble", doubleValue),
                ("pString", stringValue),
                ("pGuid", guidValue),
                ("pDateTime", dateTimeValue),
                ("pBytes", Literal(bytesValue)),
                ("pNull", Null)
            );
        }
        else
        {
            boundExpr = parsedExpr!.WithParams(
                ("pBool", boolValue),
                ("pByte", byteValue),
                ("pInt16", int16Value),
                ("pInt32", int32Value),
                ("pInt64", int64Value),
                ("pDecimal", decimalValue),
                ("pDouble", doubleValue),
                ("pString", stringValue),
                ("pGuid", guidValue),
                ("pDateTime", dateTimeValue),
                ("pDateTimeOffset", dateTimeOffsetValue),
                ("pBytes", Literal(bytesValue)),
                ("pNull", Null)
            );
        }

        if (boundExpr is not IExprQuery query)
        {
            throw new Exception($"Expected query expression, got {boundExpr.GetType().Name}");
        }

        var row = await query.Query(
            context.Database,
            new Dictionary<string, object?>(),
            (acc, r) =>
            {
                for (int i = 0; i < r.FieldCount; i++)
                {
                    var raw = r.GetValue(i);
                    acc[r.GetName(i)] = raw == DBNull.Value ? null : raw;
                }

                return acc;
            });

        AssertValue(row, "BoolV", boolValue, v => Convert.ToBoolean(v));
        AssertValue(row, "ByteV", byteValue, v => Convert.ToByte(v));
        AssertValue(row, "Int16V", int16Value, v => Convert.ToInt16(v));
        AssertValue(row, "Int32V", int32Value, v => Convert.ToInt32(v));
        AssertValue(row, "Int64V", int64Value, v => Convert.ToInt64(v));
        AssertValue(row, "DecimalV", decimalValue, v => Convert.ToDecimal(v));
        AssertValue(row, "DoubleV", doubleValue, v => Convert.ToDouble(v));
        AssertValue(row, "StringV", stringValue, v => Convert.ToString(v) ?? string.Empty);
        AssertValue(row, "GuidV", guidValue, ReadGuid);
        AssertValue(row, "DateTimeV", dateTimeValue, ReadDateTime);
        AssertValue(row, "BytesV", bytesValue, ReadBytes, ByteArrayComparer.Instance);

        if (!context.Dialect.IsMySqlFamily())
        {
            AssertDateTimeOffsetValue(row, "DateTimeOffsetV", dateTimeOffsetValue);
        }

        if (row["NullV"] != null)
        {
            throw new Exception($"NullV: expected null but was {row["NullV"]}");
        }
    }

    private static void AssertDateTimeOffsetValue(Dictionary<string, object?> row, string key, DateTimeOffset expected)
    {
        var raw = row[key];
        if (raw == null)
        {
            throw new Exception($"{key}: expected DateTimeOffset value but was null");
        }

        if (raw is DateTimeOffset dto)
        {
            if (dto.ToUniversalTime() != expected.ToUniversalTime())
            {
                throw new Exception($"{key}: expected UTC {expected.ToUniversalTime():O} but was {dto.ToUniversalTime():O}");
            }

            return;
        }

        if (raw is DateTime dt)
        {
            if (dt.ToUniversalTime() != expected.UtcDateTime.ToUniversalTime())
            {
                throw new Exception($"{key}: expected UTC {expected.UtcDateTime:O} but was {dt.ToUniversalTime():O}");
            }

            return;
        }

        if (raw is string text
            && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDto))
        {
            if (parsedDto.ToUniversalTime() != expected.ToUniversalTime())
            {
                throw new Exception($"{key}: expected UTC {expected.ToUniversalTime():O} but was {parsedDto.ToUniversalTime():O}");
            }

            return;
        }

        throw new Exception($"{key}: unsupported type {raw.GetType().Name}");
    }

    private static DateTime ReadDateTime(object? value)
    {
        if (value is DateTime dateTime)
        {
            return dateTime;
        }

        if (value is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.DateTime;
        }

        if (value is string text
            && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDateTime))
        {
            return parsedDateTime;
        }

        throw new Exception($"Could not convert {value?.GetType().Name ?? "null"} to DateTime");
    }

    private static Guid ReadGuid(object? value)
    {
        if (value is Guid guid)
        {
            return guid;
        }

        if (value is byte[] bytes && bytes.Length == 16)
        {
            return new Guid(bytes);
        }

        if (value is string text && Guid.TryParse(text, out var parsed))
        {
            return parsed;
        }

        throw new Exception($"Could not convert {value?.GetType().Name ?? "null"} to Guid");
    }

    private static byte[] ReadBytes(object? value)
    {
        if (value is byte[] bytes)
        {
            return bytes;
        }

        if (value is string text)
        {
            if (TryParseHexBytes(text, out var parsed))
            {
                return parsed;
            }
        }

        throw new Exception($"Could not convert {value?.GetType().Name ?? "null"} to byte[]");
    }

    private static bool TryParseHexBytes(string text, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var hex = text;
        if (hex.StartsWith("\\x", StringComparison.OrdinalIgnoreCase))
        {
            hex = hex.Substring(2);
        }
        else if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            hex = hex.Substring(2);
        }
        else
        {
            return false;
        }

        if ((hex.Length & 1) != 0)
        {
            return false;
        }

        var result = new byte[hex.Length / 2];
        for (int i = 0; i < result.Length; i++)
        {
            if (!byte.TryParse(
                    hex.Substring(i * 2, 2),
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture,
                    out result[i]))
            {
                return false;
            }
        }

        bytes = result;
        return true;
    }

    private static void AssertValue<T>(
        Dictionary<string, object?> row,
        string key,
        T expected,
        Func<object?, T> converter,
        IEqualityComparer<T>? comparer = null)
    {
        var actual = converter(row[key]);
        var eq = comparer ?? EqualityComparer<T>.Default;
        if (!eq.Equals(actual, expected))
        {
            throw new Exception($"{key}: expected {expected} but was {actual}");
        }
    }

    private class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public static readonly ByteArrayComparer Instance = new ByteArrayComparer();

        public bool Equals(byte[]? x, byte[]? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null || x.Length != y.Length)
            {
                return false;
            }

            return x.SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj)
        {
            if (obj.Length == 0)
            {
                return 0;
            }

            return HashCode.Combine(obj.Length, obj[0], obj[obj.Length - 1]);
        }
    }
}

