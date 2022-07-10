using System;
using System.Collections.Generic;
using System.Globalization;

namespace SqExpress.SyntaxTreeOperations.ExportImport.Internal
{
    internal class ExprPlainReader : IExprReader<IPlainItem>
    {
        private readonly IReadOnlyDictionary<PropKey, IPlainItem> _properties;
        private readonly IReadOnlyDictionary<int, IPlainItem> _types;

        public static ExprPlainReader Create(IEnumerable<IPlainItem> buffer,out IPlainItem root)
        {
            var properties = new Dictionary<PropKey, IPlainItem>();
            var types = new Dictionary<int, IPlainItem>();

            IPlainItem? rootFound = null;

            foreach (var plainItem in buffer)
            {
                if (plainItem.Id == 0)
                {
                    if (rootFound != null)
                    {
                        throw new SqExpressException("Root item (with Id == 0) should be unique");
                    }

                    rootFound = plainItem;
                }

                if (plainItem.IsTypeTag)
                {
                    types.Add(plainItem.Id, plainItem);
                }
                else
                {
                    properties.Add(new PropKey(plainItem.ParentId, plainItem.Tag, plainItem.ArrayIndex), plainItem);
                }
            }

            root = rootFound ?? throw new SqExpressException("Could not find a root item (with Id == 0)");

            return new ExprPlainReader(properties, types);
        }

        private ExprPlainReader(IReadOnlyDictionary<PropKey, IPlainItem> properties, IReadOnlyDictionary<int, IPlainItem> types)
        {
            this._properties = properties;
            this._types = types;
        }

        public string GetNodeTypeTag(IPlainItem node)
        {
            if (!node.IsTypeTag)
            {
                if (!this._types.TryGetValue(node.Id, out var subNode))
                {
                    throw new SqExpressException($"Inconsistent plain item list. Could not find type for the node: {node.Id} {node.Tag}");
                }
                return subNode.Tag;
            }

            return node.Tag;
        }

        public bool TryGetSubNode(IPlainItem node, string propertyName, out IPlainItem subNode)
        {
            return this.TryGetSubNode(node.Id, propertyName, null, out subNode);
        }

        public bool TryGetSubNode(int parentId, string propertyName, int? arrayIndex, out IPlainItem subNode)
        {
            var key = new PropKey(parentId, propertyName, arrayIndex);
            return this._properties.TryGetValue(key, out subNode!);
        }

        public IEnumerable<IPlainItem>? EnumerateList(IPlainItem node, string propertyName)
        {
            int nextIndex = 0;
            if (!this.TryGetSubNode(node.Id, propertyName, nextIndex, out var firstSubNode))
            {
                return null;
            }

            return Enumerate();

            IEnumerable<IPlainItem> Enumerate()
            {
                yield return firstSubNode;
                nextIndex++;

                while (this.TryGetSubNode(node.Id, propertyName, nextIndex, out var nextItem))
                {
                    yield return nextItem;
                    nextIndex++;
                }
            }
        }

        public bool TryGetGuid(IPlainItem node, string propertyName, out Guid value)
        {
            value = default;
            if (this.TryGetSubNode(node.Id, propertyName, null, out var prop) && prop.Value != null)
            {
                value = Guid.Parse(prop.Value);
                return true;
            }
            return false;
        }

        public bool TryGetBoolean(IPlainItem node, string propertyName, out bool value)
        {
            value = default;
            if (this.TryGetSubNode(node.Id, propertyName, null, out var prop) && prop.Value != null)
            {
                value = bool.Parse(prop.Value);
                return true;
            }
            return false;
        }

        public bool TryGetByte(IPlainItem node, string propertyName, out byte value)
        {
            value = default;
            if (this.TryGetSubNode(node.Id, propertyName, null, out var prop) && prop.Value != null)
            {
                value = byte.Parse(prop.Value, CultureInfo.InvariantCulture);
                return true;
            }
            return false;
        }

        public bool TryGetInt16(IPlainItem node, string propertyName, out short value)
        {
            value = default;
            if (this.TryGetSubNode(node.Id, propertyName, null, out var prop) && prop.Value != null)
            {
                value = short.Parse(prop.Value, CultureInfo.InvariantCulture);
                return true;
            }
            return false;
        }

        public bool TryGetInt32(IPlainItem node, string propertyName, out int value)
        {
            value = default;
            if (this.TryGetSubNode(node.Id, propertyName, null, out var prop) && prop.Value != null)
            {
                value = int.Parse(prop.Value, CultureInfo.InvariantCulture);
                return true;
            }
            return false;
        }

        public bool TryGetInt64(IPlainItem node, string propertyName, out long value)
        {
            value = default;
            if (this.TryGetSubNode(node.Id, propertyName, null, out var prop) && prop.Value != null)
            {
                value = long.Parse(prop.Value, CultureInfo.InvariantCulture);
                return true;
            }
            return false;
        }

        public bool TryGetDecimal(IPlainItem node, string propertyName, out decimal value)
        {
            value = default;
            if (this.TryGetSubNode(node.Id, propertyName, null, out var prop) && prop.Value != null)
            {
                value = decimal.Parse(prop.Value, CultureInfo.InvariantCulture);
                return true;
            }
            return false;
        }

        public bool TryGetDouble(IPlainItem node, string propertyName, out double value)
        {
            value = default;
            if (this.TryGetSubNode(node.Id, propertyName, null, out var prop) && prop.Value != null)
            {
                value = double.Parse(prop.Value, CultureInfo.InvariantCulture);
                return true;
            }
            return false;
        }

        public bool TryGetDateTime(IPlainItem node, string propertyName, out DateTime value)
        {
            value = default;
            if (this.TryGetSubNode(node.Id, propertyName, null, out var prop) && prop.Value != null)
            {
                value = DateTime.ParseExact(prop.Value, "yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
                return true;
            }
            return false;
        }

        public bool TryGetDateTimeOffset(IPlainItem node, string propertyName, out DateTimeOffset value)
        {
            value = default;
            if (this.TryGetSubNode(node.Id, propertyName, null, out var prop) && prop.Value != null)
            {
                value = DateTimeOffset.Parse(prop.Value);
                return true;
            }
            return false;
        }

        public bool TryGetString(IPlainItem node, string propertyName, out string? value)
        {
            value = default;
            if (this.TryGetSubNode(node.Id, propertyName, null, out var prop) && prop.Value != null)
            {
                value = prop.Value;
                return true;
            }
            return false;
        }

        public bool TryGetByteArray(IPlainItem node, string propertyName, out IReadOnlyList<byte>? value)
        {
            value = default;
            if (this.TryGetSubNode(node.Id, propertyName, null, out var prop))
            {
                value = prop.Value != null ? Convert.FromBase64String(prop.Value) : null;
                return true;
            }
            return false;
        }

        private readonly struct PropKey : IEquatable<PropKey>
        {
            public readonly int ParentId;

            public readonly string PropertyName;

            public readonly int? ArrayIndex;

            public PropKey(int parentId, string propertyName, int? arrayIndex)
            {
                this.ParentId = parentId;
                this.PropertyName = propertyName;
                this.ArrayIndex = arrayIndex;
            }

            public bool Equals(PropKey other)
            {
                return this.ParentId == other.ParentId && this.PropertyName == other.PropertyName && this.ArrayIndex == other.ArrayIndex;
            }

            public override bool Equals(object? obj)
            {
                return obj is PropKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = this.ParentId;
                    hashCode = (hashCode * 397) ^ this.PropertyName.GetHashCode();
                    hashCode = (hashCode * 397) ^ this.ArrayIndex.GetHashCode();
                    return hashCode;
                }
            }
        }
    }
}