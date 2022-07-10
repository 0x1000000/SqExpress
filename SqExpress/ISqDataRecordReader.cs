using System;
using System.Data;
using System.IO;

namespace SqExpress
{
    public interface ISqDataRecordReader : IDataRecord
    {
        bool GetBoolean(string name);
        bool? GetNullableBoolean(string name);
        byte GetByte(string name);
        byte? GetNullableByte(string name);
        byte[] GetByteArray(string name);
        byte[]? GetNullableByteArray(string name);
        Stream GetStream(string name);
        Stream? GetNullableStream(string name);
        short GetInt16(string name);
        short? GetNullableInt16(string name);
        int GetInt32(string name);
        int? GetNullableInt32(string name);
        long GetInt64(string name);
        long? GetNullableInt64(string name);
        decimal GetDecimal(string name);
        decimal? GetNullableDecimal(string name);
        double GetDouble(string name);
        double? GetNullableDouble(string name);
        DateTime GetDateTime(string name);
        DateTime? GetNullableDateTime(string name);
        DateTimeOffset GetDateTimeOffset(string name);
        DateTimeOffset? GetNullableDateTimeOffset(string name);
        Guid GetGuid(string name);
        Guid? GetNullableGuid(string name);
        string GetString(string name);
        string? GetNullableString(string name);
        object? GetValue(string name);
    }
}