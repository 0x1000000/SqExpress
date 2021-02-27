#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Text.Json;
using SqExpress.SyntaxTreeOperations;

namespace SqExpress.Test.Syntax
{
    public class JsonReader : IExprReader<JsonElement>
    {
        public string GetNodeTypeTag(JsonElement node)
        {
            return node.GetProperty("$type").GetString();
        }

        public bool TryGetSubNode(JsonElement node, string propertyName, out JsonElement subNode)
        {
            return node.TryGetProperty(propertyName, out subNode);
        }

        public IEnumerable<JsonElement> EnumerateList(JsonElement node, string propertyName)
        {
            if (node.TryGetProperty(propertyName, out var arrayElement))
            {
                return arrayElement.EnumerateArray();
            }
            return null;
        }

        public bool TryGetGuid(JsonElement node, string propertyName, out Guid value)
        {
            value = default;
            return node.TryGetProperty(propertyName, out var valueNode) 
                   && valueNode.TryGetGuid(out value);
        }

        public bool TryGetBoolean(JsonElement node, string propertyName, out bool value)
        {
            value = default;
            if (node.TryGetProperty(propertyName, out var valueNode))
            {
                value = valueNode.GetBoolean();
                return true;
            }
            return false;
        }

        public bool TryGetByte(JsonElement node, string propertyName, out byte value)
        {
            value = default;
            return node.TryGetProperty(propertyName, out var valueNode)
                   && valueNode.TryGetByte(out value);
        }

        public bool TryGetInt16(JsonElement node, string propertyName, out short value)
        {
            value = default;
            return node.TryGetProperty(propertyName, out var valueNode)
                   && valueNode.TryGetInt16(out value);
        }

        public bool TryGetInt32(JsonElement node, string propertyName, out int value)
        {
            value = default;
            return node.TryGetProperty(propertyName, out var valueNode)
                   && valueNode.TryGetInt32(out value);
        }

        public bool TryGetInt64(JsonElement node, string propertyName, out long value)
        {
            value = default;
            return node.TryGetProperty(propertyName, out var valueNode)
                   && valueNode.TryGetInt64(out value);
        }

        public bool TryGetDecimal(JsonElement node, string propertyName, out decimal value)
        {
            value = default;
            return node.TryGetProperty(propertyName, out var valueNode)
                   && valueNode.TryGetDecimal(out value);
        }

        public bool TryGetDouble(JsonElement node, string propertyName, out double value)
        {
            value = default;
            return node.TryGetProperty(propertyName, out var valueNode)
                   && valueNode.TryGetDouble(out value);
        }

        public bool TryGetDateTime(JsonElement node, string propertyName, out DateTime value)
        {
            value = default;
            return node.TryGetProperty(propertyName, out var valueNode)
                   && valueNode.TryGetDateTime(out value);
        }

        public bool TryGetString(JsonElement node, string propertyName, out string value)
        {
            value = default;
            if (node.TryGetProperty(propertyName, out var valueNode))
            {
                value = valueNode.GetString();
                return true;
            }
            return false;
        }

        public bool TryGetByteArray(JsonElement node, string propertyName, out IReadOnlyList<byte> value)
        {
            value = default;
            if (node.TryGetProperty(propertyName, out var valueNode))
            {
                if (valueNode.TryGetBytesFromBase64(out var bytes))
                {
                    value = bytes;
                    return true;
                }
            }
            return false;
        }
    }
}
#endif
