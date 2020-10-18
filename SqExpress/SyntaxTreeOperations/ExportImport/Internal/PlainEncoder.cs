using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SqExpress.Syntax;

namespace SqExpress.SyntaxTreeOperations.ExportImport.Internal
{
    public delegate IPlainItem PlainItemFactory(int id, int parentId, int? arrayIndex, bool isTypeTag, string tag, string? value);

    public class PlainEncoder : IWalkerVisitor<int>
    {
        private readonly List<IPlainItem> _buffer = new List<IPlainItem>();

        private readonly PlainItemFactory _factory;

        private int _currentId;

        private int GetNewId() => ++this._currentId;

        public PlainEncoder(PlainItemFactory factory)
        {
            this._factory = factory;
        }

        public IReadOnlyList<IPlainItem> Result => this._buffer;

        public VisitorResult<int> VisitExpr(IExpr expr, string typeTag, int ctx)
        {
            var newId = this._currentId;
            this._buffer.Add(this._factory(newId, ctx, null, true, typeTag, null));
            return VisitorResult<int>.Continue(newId);
        }

        public void EndVisitExpr(IExpr expr, int ctx)
        {
        }

        public void VisitProperty(string name, bool isArray, bool isNull, int ctx)
        {
            if (!isNull && !isArray)
            {
                this._buffer.Add(this._factory(this.GetNewId(), ctx, null, false, name, null));
            }
        }

        public void EndVisitProperty(string name, bool isArray, bool isNull, int ctx)
        {
        }

        public void VisitArrayItem(string name, int arrayIndex, int ctx)
        {
            this._buffer.Add(this._factory(this.GetNewId(), ctx, arrayIndex, false, name, null));
        }

        public void EndVisitArrayItem(string name, int arrayIndex, int ctx)
        {
        }

        public void VisitPlainProperty(string name, string? value, int ctx)
        {
            this._buffer.Add(this._factory(ctx, ctx, null, false, name, value));
        }

        public void VisitPlainProperty(string name, bool? value, int ctx)
        {
            this._buffer.Add(this._factory(ctx, ctx, null, false, name, value?.ToString(CultureInfo.InvariantCulture)));
        }

        public void VisitPlainProperty(string name, byte? value, int ctx)
        {
            this._buffer.Add(this._factory(ctx, ctx, null, false, name, value?.ToString(CultureInfo.InvariantCulture)));
        }

        public void VisitPlainProperty(string name, short? value, int ctx)
        {
            this._buffer.Add(this._factory(ctx, ctx, null, false, name, value?.ToString(CultureInfo.InvariantCulture)));
        }

        public void VisitPlainProperty(string name, int? value, int ctx)
        {
            this._buffer.Add(this._factory(ctx, ctx, null, false, name, value?.ToString(CultureInfo.InvariantCulture)));
        }

        public void VisitPlainProperty(string name, long? value, int ctx)
        {
            this._buffer.Add(this._factory(ctx, ctx, null, false, name, value?.ToString(CultureInfo.InvariantCulture)));
        }

        public void VisitPlainProperty(string name, decimal? value, int ctx)
        {
            this._buffer.Add(this._factory(ctx, ctx, null, false, name, value?.ToString(CultureInfo.InvariantCulture)));
        }

        public void VisitPlainProperty(string name, double? value, int ctx)
        {
            this._buffer.Add(this._factory(ctx, ctx, null, false, name, value?.ToString(CultureInfo.InvariantCulture)));
        }

        public void VisitPlainProperty(string name, DateTime? value, int ctx)
        {
            if (value != null)
            {
                string ts = value.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff");
                this._buffer.Add(this._factory(ctx, ctx, null, false, name, ts));
            }
        }

        public void VisitPlainProperty(string name, Guid? value, int ctx)
        {
            this._buffer.Add(this._factory(ctx, ctx, null, false, name, value?.ToString("D")));
        }

        public void VisitPlainProperty(string name, IReadOnlyList<byte>? value, int ctx)
        {

            this._buffer.Add(this._factory(ctx, ctx, null, false, name, value != null ? Convert.ToBase64String(value.ToArray()) : null));
        }
    }
}