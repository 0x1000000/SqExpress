using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SqExpress.Syntax;
using SqExpress.SyntaxTreeOperations;

namespace SqExpress.Test.Syntax
{
    public class JsonWriter : IWalkerVisitor<Utf8JsonWriter>
    {
        public VisitorResult<Utf8JsonWriter> VisitExpr(IExpr expr, string typeTag, Utf8JsonWriter ctx)
        {
            ctx.WriteStartObject();
            ctx.WriteString("$type", typeTag);
            return VisitorResult<Utf8JsonWriter>.Continue(ctx);
        }

        public void EndVisitExpr(IExpr expr, Utf8JsonWriter ctx)
        {
            ctx.WriteEndObject();
            ctx.Flush();
        }

        public void VisitProperty(string name, bool isArray, bool isNull, Utf8JsonWriter ctx)
        {
            if (isNull)
            {
                return;
            }
            if (isArray)
            {
                ctx.WriteStartArray(name);
            }
            else
            {
                ctx.WritePropertyName(name);
            }
        }

        public void EndVisitProperty(string name, bool isArray, bool isNull, Utf8JsonWriter ctx)
        {
            if (isNull)
            {
                return;
            }
            if (isArray)
            {
                ctx.WriteEndArray();
            }
        }

        public void VisitArrayItem(string name, int arrayIndex, Utf8JsonWriter ctx)
        {
        }

        public void EndVisitArrayItem(string name, int arrayIndex, Utf8JsonWriter ctx)
        {
            
        }

        public void VisitPlainProperty(string name, string value, Utf8JsonWriter ctx)
        {
            if (value != null)
            {
                ctx.WriteString(name, value);
            }
        }

        public void VisitPlainProperty(string name, bool? value, Utf8JsonWriter ctx)
        {
            if (value.HasValue)
            {
                ctx.WriteBoolean(name, value.Value);
            }
        }

        public void VisitPlainProperty(string name, byte? value, Utf8JsonWriter ctx)
        {
            if (value.HasValue)
            {
                ctx.WriteNumber(name, value.Value);
            }
        }

        public void VisitPlainProperty(string name, short? value, Utf8JsonWriter ctx)
        {
            if (value.HasValue)
            {
                ctx.WriteNumber(name, value.Value);
            }
        }

        public void VisitPlainProperty(string name, int? value, Utf8JsonWriter ctx)
        {
            if (value.HasValue)
            {
                ctx.WriteNumber(name, value.Value);
            }
        }

        public void VisitPlainProperty(string name, long? value, Utf8JsonWriter ctx)
        {
            if (value.HasValue)
            {
                ctx.WriteNumber(name, value.Value);
            }
        }

        public void VisitPlainProperty(string name, decimal? value, Utf8JsonWriter ctx)
        {
            if (value.HasValue)
            {
                ctx.WriteNumber(name, value.Value);
            }
        }

        public void VisitPlainProperty(string name, double? value, Utf8JsonWriter ctx)
        {
            if (value.HasValue)
            {
                ctx.WriteNumber(name, value.Value);
            }
        }

        public void VisitPlainProperty(string name, DateTime? value, Utf8JsonWriter ctx)
        {
            if (value.HasValue)
            {
                ctx.WriteString(name, value.Value);
            }
        }

        public void VisitPlainProperty(string name, Guid? value, Utf8JsonWriter ctx)
        {
            if (value.HasValue)
            {
                ctx.WriteString(name, value.Value);
            }
        }

        public void VisitPlainProperty(string name, IReadOnlyList<byte> value, Utf8JsonWriter ctx)
        {
            if (value != null)
            {
                ctx.WriteBase64String(name, value.ToArray().AsSpan());
            }
        }
    }
}