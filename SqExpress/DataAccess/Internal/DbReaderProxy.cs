using System;
using System.Data;
using System.Data.Common;
using System.IO;

namespace SqExpress.DataAccess.Internal
{
    internal class DbReaderProxy : ISqDataRecordReader
    {
        private readonly DbDataReader _dataReader;

        public DbReaderProxy(DbDataReader dataReader)
        {
            this._dataReader = dataReader;
        }

        public bool GetBoolean(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                this.ThrowNull(name);
            }

            return this._dataReader.GetBoolean(ordinal);
        }

        public bool? GetNullableBoolean(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                return null;
            }

            return this._dataReader.GetBoolean(ordinal);
        }

        public byte GetByte(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                this.ThrowNull(name);
            }

            return this._dataReader.GetByte(ordinal);
        }

        public byte? GetNullableByte(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                return null;
            }
            return this._dataReader.GetByte(ordinal);
        }

        public byte[] GetByteArray(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                this.ThrowNull(name);
            }

            return (byte[])this._dataReader.GetValue(ordinal);
        }

        public byte[]? GetNullableByteArray(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                return null;
            }

            return (byte[])this._dataReader.GetValue(ordinal);
        }

        public Stream GetStream(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                this.ThrowNull(name);
            }

            return this._dataReader.GetStream(ordinal);
        }

        public Stream? GetNullableStream(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                return null;
            }

            return this._dataReader.GetStream(ordinal);
        }

        public short GetInt16(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                this.ThrowNull(name);
            }

            return this._dataReader.GetInt16(ordinal);
        }

        public short? GetNullableInt16(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                return null;
            }

            return this._dataReader.GetInt16(ordinal);
        }

        public int GetInt32(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                this.ThrowNull(name);
            }

            return this._dataReader.GetInt32(ordinal);
        }

        public int? GetNullableInt32(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                return null;
            }

            return this._dataReader.GetInt32(ordinal);
        }

        public long GetInt64(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                this.ThrowNull(name);
            }

            return this._dataReader.GetInt64(ordinal);
        }

        public long? GetNullableInt64(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                return null;
            }

            return this._dataReader.GetInt64(ordinal);
        }

        public decimal GetDecimal(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                this.ThrowNull(name);
            }

            return this._dataReader.GetDecimal(ordinal);
        }

        public decimal? GetNullableDecimal(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                return null;
            }

            return this._dataReader.GetDecimal(ordinal);
        }

        public double GetDouble(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                this.ThrowNull(name);
            }

            return this._dataReader.GetDouble(ordinal);
        }

        public double? GetNullableDouble(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                return null;
            }

            return this._dataReader.GetDouble(ordinal);
        }

        public DateTime GetDateTime(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                this.ThrowNull(name);
            }

            return this._dataReader.GetDateTime(ordinal);
        }

        public DateTime? GetNullableDateTime(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                return null;
            }

            return this._dataReader.GetDateTime(ordinal);
        }

        public DateTimeOffset GetDateTimeOffset(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                this.ThrowNull(name);
            }

            return this._dataReader.GetFieldValue<DateTimeOffset>(ordinal);
        }

        public DateTimeOffset? GetNullableDateTimeOffset(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                return null;
            }

            return this._dataReader.GetFieldValue<DateTimeOffset>(ordinal);
        }

        public Guid GetGuid(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                this.ThrowNull(name);
            }

            return this._dataReader.GetGuid(ordinal);
        }

        public Guid? GetNullableGuid(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                return null;
            }

            return this._dataReader.GetGuid(ordinal);
        }

        public string GetString(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                this.ThrowNull(name);
            }
            return this._dataReader.GetString(ordinal);
        }

        public string? GetNullableString(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                return null;
            }
            return this._dataReader.GetString(ordinal);
        }

        public object? GetValue(string name)
        {
            var ordinal = this._dataReader.GetOrdinal(name);
            if (this._dataReader.IsDBNull(ordinal))
            {
                return null;
            }
            return this._dataReader.GetValue(ordinal);
        }

        private void ThrowNull(string columnName)
        {
            throw new SqExpressException($"Null value was not expected for columnName \"{columnName}\"");
        }

        bool IDataRecord.GetBoolean(int i) => this._dataReader.GetBoolean(i);

        byte IDataRecord.GetByte(int i) => this._dataReader.GetByte(i);

        long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) 
            => this._dataReader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);

        char IDataRecord.GetChar(int i) => this._dataReader.GetChar(i);

        long IDataRecord.GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) 
            => this._dataReader.GetChars(i, fieldoffset, buffer, bufferoffset, length);

        IDataReader? IDataRecord.GetData(int i) => this._dataReader.GetData(i);

        string IDataRecord.GetDataTypeName(int i) => this._dataReader.GetDataTypeName(i);

        DateTime IDataRecord.GetDateTime(int i) => this._dataReader.GetDateTime(i);

        decimal IDataRecord.GetDecimal(int i) => this._dataReader.GetDecimal(i);

        double IDataRecord.GetDouble(int i) => this._dataReader.GetDouble(i);

        Type? IDataRecord.GetFieldType(int i) => this._dataReader.GetFieldType(i);

        float IDataRecord.GetFloat(int i) => this._dataReader.GetFloat(i);

        Guid IDataRecord.GetGuid(int i) => this._dataReader.GetGuid(i);

        short IDataRecord.GetInt16(int i) => this._dataReader.GetInt16(i);

        int IDataRecord.GetInt32(int i) => this._dataReader.GetInt32(i);

        long IDataRecord.GetInt64(int i) => this._dataReader.GetInt64(i);

        string IDataRecord.GetName(int i) => this._dataReader.GetName(i);

        int IDataRecord.GetOrdinal(string name) => this._dataReader.GetOrdinal(name);

        string IDataRecord.GetString(int i) => this._dataReader.GetString(i);

        object IDataRecord.GetValue(int i) => this._dataReader.GetValue(i);

        int IDataRecord.GetValues(object[] values) => this._dataReader.GetValues(values);

        bool IDataRecord.IsDBNull(int i) => this._dataReader.IsDBNull(i);

        int IDataRecord.FieldCount => this._dataReader.FieldCount;

        object IDataRecord.this[int i] => this._dataReader[i];

        object IDataRecord.this[string name] => this._dataReader[name];
    }
}