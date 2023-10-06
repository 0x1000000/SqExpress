using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SqExpress.Syntax;

namespace SqExpress.SyntaxTreeOperations.ExportImport.Internal
{
    internal class ExprXmlWriter : IWalkerVisitor<XmlWriter>
    {
        private IExpr? _root;

        public VisitorResult<XmlWriter> VisitExpr(IExpr expr, string typeTag, XmlWriter writer)
        {
            if (this._root == null)
            {
                writer.WriteStartElement("Expr");
                this._root = expr;
            }

            writer.WriteAttributeString("typeTag", typeTag);
            return VisitorResult<XmlWriter>.Continue(writer);
        }

        public void EndVisitExpr(IExpr expr, XmlWriter writer)
        {
            if (ReferenceEquals(expr, this._root))
            {
                writer.WriteEndElement();
            }

            writer.Flush();
        }

        public void VisitProperty(string name, bool isArray, bool isNull, XmlWriter writer)
        {
            if (isNull)
            {
                return;
            }

            writer.WriteStartElement(name);
        }

        public void EndVisitProperty(string name, bool isArray, bool isNull, XmlWriter writer)
        {
            if (isNull)
            {
                return;
            }

            writer.WriteEndElement();
        }

        public void VisitArrayItem(string name, int arrayIndex, XmlWriter writer)
        {
            writer.WriteStartElement(name + arrayIndex);
        }

        public void EndVisitArrayItem(string name, int arrayIndex, XmlWriter writer)
        {
            writer.WriteEndElement();
        }

        public void VisitPlainProperty(string name, string? value, XmlWriter writer)
        {
            if (value == null)
            {
                return;
            }

            writer.WriteStartElement(name);
            writer.WriteValue(value);
            writer.WriteEndElement();
        }

        public void VisitPlainProperty(string name, bool? value, XmlWriter writer)
        {
            if (value == null)
            {
                return;
            }

            writer.WriteStartElement(name);
            writer.WriteValue(value.Value);
            writer.WriteEndElement();
        }

        public void VisitPlainProperty(string name, byte? value, XmlWriter writer)
        {
            if (value == null)
            {
                return;
            }

            writer.WriteStartElement(name);
            writer.WriteValue(value.Value);
            writer.WriteEndElement();
        }

        public void VisitPlainProperty(string name, short? value, XmlWriter writer)
        {
            if (value == null)
            {
                return;
            }

            writer.WriteStartElement(name);
            writer.WriteValue(value.Value);
            writer.WriteEndElement();
        }

        public void VisitPlainProperty(string name, int? value, XmlWriter writer)
        {
            if (value == null)
            {
                return;
            }

            writer.WriteStartElement(name);
            writer.WriteValue(value.Value);
            writer.WriteEndElement();
        }

        public void VisitPlainProperty(string name, long? value, XmlWriter writer)
        {
            if (value == null)
            {
                return;
            }

            writer.WriteStartElement(name);
            writer.WriteValue(value.Value);
            writer.WriteEndElement();
        }

        public void VisitPlainProperty(string name, decimal? value, XmlWriter writer)
        {
            if (value == null)
            {
                return;
            }

            writer.WriteStartElement(name);
            writer.WriteValue(value.Value);
            writer.WriteEndElement();
        }

        public void VisitPlainProperty(string name, double? value, XmlWriter writer)
        {
            if (value == null)
            {
                return;
            }

            writer.WriteStartElement(name);
            writer.WriteValue(value.Value);
            writer.WriteEndElement();
        }

        public void VisitPlainProperty(string name, DateTime? value, XmlWriter writer)
        {
            if (value == null)
            {
                return;
            }

            writer.WriteStartElement(name);
            writer.WriteValue(value.Value);
            writer.WriteEndElement();
        }

        public void VisitPlainProperty(string name, DateTimeOffset? value, XmlWriter writer)
        {
            if (value == null)
            {
                return;
            }

            writer.WriteStartElement(name);
            writer.WriteValue(value.Value);
            writer.WriteEndElement();
        }

        public void VisitPlainProperty(string name, Guid? value, XmlWriter writer)
        {
            if (value == null)
            {
                return;
            }

            writer.WriteStartElement(name);
            writer.WriteValue(value.Value.ToString("D"));
            writer.WriteEndElement();
        }

        public void VisitPlainProperty(string name, IReadOnlyList<byte>? value, XmlWriter writer)
        {
            if (value == null)
            {
                return;
            }

            writer.WriteStartAttribute(name);
            var buffer = value as byte[] ?? value.ToArray();
            writer.WriteBase64(buffer, 0, buffer.Length);
            writer.WriteEndAttribute();
        }
    }
}