using System;
using System.Collections.Generic;
using System.Xml;
using SqExpress.Utils;

namespace SqExpress.SyntaxTreeOperations.ExportImport.Internal
{
    internal class ExprXmlReader : IExprReader<XmlElement>
    {
        public static readonly ExprXmlReader Instance = new ExprXmlReader();

        private ExprXmlReader()
        {
        }

        public string GetNodeTypeTag(XmlElement node)
        {
            return node.GetAttribute("typeTag") ?? throw new SqExpressException("Incorrect XML format - Could not find 'typeTag' attribute");
        }

        private XmlElement? FindElement(XmlElement node, string propertyName)
        {
            return node.SelectSingleNode(propertyName) as XmlElement;
        }

        public bool TryGetSubNode(XmlElement node, string propertyName, out XmlElement subNode)
        {
            var result = this.FindElement(node, propertyName);
            subNode = result ?? node;
            return result != null;
        }

        public IEnumerable<XmlElement>? EnumerateList(XmlElement node, string propertyName)
        {
            var result = this.FindElement(node, propertyName);
            if (result != null)
            {
                List<IndexXElement> buffer = new List<IndexXElement>();
                foreach (var childNode in result.ChildNodes)
                {
                    if (childNode is XmlElement childElement)
                    {
                        string indexStr = childElement.Name.Substring(propertyName.Length);
                        if (int.TryParse(indexStr, out var index))
                        {
                            buffer.Add(new IndexXElement(index, childElement));
                        }
                    }
                }

                if (buffer.Count == 0)
                {
                    return null;
                }

                buffer.Sort((x, y) => x.Index - y.Index);
                return buffer.SelectToReadOnlyList(i => i.Element);
            }

            return null;
        }

        public bool TryGetGuid(XmlElement node, string propertyName, out Guid value)
        {
            var el = this.FindElement(node, propertyName);
            value = el != null ? XmlConvert.ToGuid(el.InnerText) : default;
            return el != null;
        }

        public bool TryGetBoolean(XmlElement node, string propertyName, out bool value)
        {
            var el = this.FindElement(node, propertyName);
            value = el != null ? XmlConvert.ToBoolean(el.InnerText) : default;
            return el != null;
        }

        public bool TryGetByte(XmlElement node, string propertyName, out byte value)
        {
            var el = this.FindElement(node, propertyName);
            value = el != null ? XmlConvert.ToByte(el.InnerText) : default;
            return el != null;
        }

        public bool TryGetInt16(XmlElement node, string propertyName, out short value)
        {
            var el = this.FindElement(node, propertyName);
            value = el != null ? XmlConvert.ToInt16(el.InnerText) : default;
            return el != null;
        }

        public bool TryGetInt32(XmlElement node, string propertyName, out int value)
        {
            var el = this.FindElement(node, propertyName);
            value = el != null ? XmlConvert.ToInt32(el.InnerText) : default;
            return el != null;
        }

        public bool TryGetInt64(XmlElement node, string propertyName, out long value)
        {
            var el = this.FindElement(node, propertyName);
            value = el != null ? XmlConvert.ToInt64(el.InnerText) : default;
            return el != null;
        }

        public bool TryGetDecimal(XmlElement node, string propertyName, out decimal value)
        {
            var el = this.FindElement(node, propertyName);
            value = el != null ? XmlConvert.ToDecimal(el.InnerText) : default;
            return el != null;
        }

        public bool TryGetDouble(XmlElement node, string propertyName, out double value)
        {
            var el = this.FindElement(node, propertyName);
            value = el != null ? XmlConvert.ToDouble(el.InnerText) : default;
            return el != null;
        }

        public bool TryGetDateTime(XmlElement node, string propertyName, out DateTime value)
        {
            var el = this.FindElement(node, propertyName);
            value = el != null ? XmlConvert.ToDateTime(el.InnerText, XmlDateTimeSerializationMode.Unspecified) : default;
            return el != null;
        }

        public bool TryGetDateTimeOffset(XmlElement node, string propertyName, out DateTimeOffset value)
        {
            var el = this.FindElement(node, propertyName);
            value = el != null ? XmlConvert.ToDateTimeOffset(el.InnerText) : default;
            return el != null;
        }

        public bool TryGetString(XmlElement node, string propertyName, out string? value)
        {
            var el = this.FindElement(node, propertyName);
            value = el?.InnerText;
            return el != null;
        }

        public bool TryGetByteArray(XmlElement node, string propertyName, out IReadOnlyList<byte>? value)
        {
            var el = this.FindElement(node, propertyName);
            if (el != null && !string.IsNullOrEmpty(el.InnerText))
            {
                value = Convert.FromBase64String(el.InnerText);
                return true;
            }

            value = null;
            return false;
        }

        private readonly struct IndexXElement : IEquatable<IndexXElement>
        {
            public readonly int Index;
            public readonly XmlElement Element;

            public IndexXElement(int index, XmlElement element)
            {
                this.Index = index;
                this.Element = element;
            }

            public bool Equals(IndexXElement other)
            {
                return this.Index == other.Index && this.Element.Equals(other.Element);
            }

            public override bool Equals(object? obj)
            {
                return obj is IndexXElement other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (this.Index * 397) ^ this.Element.GetHashCode();
                }
            }
        }
    }
}