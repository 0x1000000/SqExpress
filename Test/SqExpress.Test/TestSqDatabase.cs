using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.StatementSyntax;
#nullable enable
namespace SqExpress.Test
{
    public class TestSqDatabase : ISqDatabase
    {
        public TestSqDatabase(QueryDelegate<object>? queryImplementation = null)
        {
            this.QueryImplementation = queryImplementation;
        }

        public void Dispose()
        {
            
        }

        public ISqTransaction BeginTransaction()
        {
            throw new NotImplementedException();
        }

        public ISqTransaction BeginTransactionOrUseExisting(out bool isNewTransaction)
        {
            throw new NotImplementedException();
        }

        public ISqTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            throw new NotImplementedException();
        }

        public ISqTransaction BeginTransactionOrUseExisting(IsolationLevel isolationLevel, out bool isNewTransaction)
        {
            throw new NotImplementedException();
        }

        public delegate Task<TAgg> QueryDelegate<TAgg>(IExprQuery query, TAgg seed, Func<TAgg, ISqDataRecordReader, TAgg> aggregator);

        public readonly QueryDelegate<object>? QueryImplementation = null;

        public async Task<TAgg> Query<TAgg>(IExprQuery query, TAgg seed, Func<TAgg, ISqDataRecordReader, TAgg> aggregator, CancellationToken cancellationToken = default)
        {
            var qi = this.QueryImplementation;
            if (qi == null)
            {
                throw new NotImplementedException($"Could not find implementation of \"{nameof(this.Query)}\" method");
            }

            var res = await qi(query, seed!, (acc, r) => aggregator((TAgg) acc, r)!);
            return (TAgg)res;
        }

        public Task<TAgg> Query<TAgg>(IExprQuery query, TAgg seed, Func<TAgg, ISqDataRecordReader, Task<TAgg>> aggregator, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<object> QueryScalar(IExprQuery query, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task Exec(IExprExec statement, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task Statement(IStatement statement, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }


    public class TestSqDataRecordReader : ISqDataRecordReader
    {

        private readonly Func<string, object>? _getByColName;

        public TestSqDataRecordReader(Func<string, object>? getByColName = null)
        {
            this._getByColName = getByColName;
        }

        private T GetByColName<T>(string name)
        {
            if (this._getByColName == null)
            {
                throw new Exception("\"getByColName\" implementation was not specified");
            }

            return (T) this._getByColName(name);
        }

        public bool GetBoolean(string name)
        {
            return this.GetByColName<bool>(name);
        }

        public bool? GetNullableBoolean(string name)
        {
            return this.GetByColName<bool?>(name);
        }

        public byte GetByte(string name)
        {
            return this.GetByColName<byte>(name);
        }

        public byte? GetNullableByte(string name)
        {
            return this.GetByColName<byte?>(name);
        }

        public byte[] GetByteArray(string name)
        {
            return this.GetByColName<byte[]>(name);
        }

        public byte[]? GetNullableByteArray(string name)
        {
            return this.GetByColName<byte[]?>(name);
        }

        public Stream GetStream(string name)
        {
            return this.GetByColName<Stream>(name);
        }

        public Stream? GetNullableStream(string name)
        {
            return this.GetByColName<Stream?>(name);
        }

        public short GetInt16(string name)
        {
            return this.GetByColName<short>(name);
        }

        public short? GetNullableInt16(string name)
        {
            return this.GetByColName<short?>(name);
        }

        public int GetInt32(string name)
        {
            return this.GetByColName<int>(name);
        }

        public int? GetNullableInt32(string name)
        {
            return this.GetByColName<int?>(name);
        }

        public long GetInt64(string name)
        {
            return this.GetByColName<long>(name);
        }

        public long? GetNullableInt64(string name)
        {
            return this.GetByColName<long?>(name);
        }

        public decimal GetDecimal(string name)
        {
            return this.GetByColName<decimal>(name);
        }

        public decimal? GetNullableDecimal(string name)
        {
            return this.GetByColName<decimal?>(name);
        }

        public double GetDouble(string name)
        {
            return this.GetByColName<double>(name);
        }

        public double? GetNullableDouble(string name)
        {
            return this.GetByColName<double?>(name);
        }

        public DateTime GetDateTime(string name)
        {
            return this.GetByColName<DateTime>(name);
        }

        public DateTime? GetNullableDateTime(string name)
        {
            return this.GetByColName<DateTime?>(name);
        }

        public Guid GetGuid(string name)
        {
            return this.GetByColName<Guid>(name);
        }

        public Guid? GetNullableGuid(string name)
        {
            return this.GetByColName<Guid?>(name);
        }

        public string GetString(string name)
        {
            return this.GetByColName<string>(name);
        }

        public string? GetNullableString(string name)
        {
            return this.GetByColName<string?>(name);
        }

        public object? GetValue(string name)
        {
            return this.GetByColName<object?>(name);

        }


        public string GetName(int i)
        {
            throw new NotImplementedException();
        }

        public string GetDataTypeName(int i)
        {
            throw new NotImplementedException();
        }

        public Type GetFieldType(int i)
        {
            throw new NotImplementedException();
        }

        public object GetValue(int i)
        {
            throw new NotImplementedException();
        }

        public int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public int GetOrdinal(string name)
        {
            throw new NotImplementedException();
        }

        public bool GetBoolean(int i)
        {
            throw new NotImplementedException();
        }

        public byte GetByte(int i)
        {
            throw new NotImplementedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public char GetChar(int i)
        {
            throw new NotImplementedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotImplementedException();
        }

        public Guid GetGuid(int i)
        {
            throw new NotImplementedException();
        }

        public short GetInt16(int i)
        {
            throw new NotImplementedException();
        }

        public int GetInt32(int i)
        {
            throw new NotImplementedException();
        }

        public long GetInt64(int i)
        {
            throw new NotImplementedException();
        }

        public float GetFloat(int i)
        {
            throw new NotImplementedException();
        }

        public double GetDouble(int i)
        {
            throw new NotImplementedException();
        }

        public string GetString(int i)
        {
            throw new NotImplementedException();
        }

        public decimal GetDecimal(int i)
        {
            throw new NotImplementedException();
        }

        public DateTime GetDateTime(int i)
        {
            throw new NotImplementedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }

        public bool IsDBNull(int i)
        {
            throw new NotImplementedException();
        }

        public int FieldCount { get; }

        public object this[int i] => throw new NotImplementedException();

        public object this[string name] => throw new NotImplementedException();
    }
}