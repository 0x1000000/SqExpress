using System;

namespace SqExpress.TableDecalationAttributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class TableDescriptorAttribute : Attribute
    {
        public TableDescriptorAttribute(string name)
        {
            this.Name = name;
        }

        public TableDescriptorAttribute(string schema, string name)
        {
            this.Schema = schema;
            this.Name = name;
        }

        public TableDescriptorAttribute(string databaseName, string schema, string name)
        {
            this.DatabaseName = databaseName;
            this.Schema = schema;
            this.Name = name;
        }

        public string? DatabaseName { get; }
        public string? Schema { get; }
        public string Name { get; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public abstract class TableColumnAttributeBase : Attribute
    {
        protected TableColumnAttributeBase(string name)
        {
            this.Name = name;
        }

        public string Name { get; }

        public string? PropertyName { get; set; }

        public bool Pk { get; set; }

        public bool Identity { get; set; }

        public string? FkSchema { get; set; }

        public string? FkDatabase { get; set; }

        public string? FkTable { get; set; }

        public string? FkColumn { get; set; }

        public string? DefaultValue { get; set; }
    }

    public abstract class StringColumnAttributeBase : TableColumnAttributeBase
    {
        protected StringColumnAttributeBase(string name) : base(name)
        {
        }

        public bool Unicode { get; set; }

        public int MaxLength { get; set; } = -1;

        public bool FixedLength { get; set; }

        public bool Text { get; set; }
    }

    public abstract class ByteArrayColumnAttributeBase : TableColumnAttributeBase
    {
        protected ByteArrayColumnAttributeBase(string name) : base(name)
        {
        }

        public int MaxLength { get; set; } = -1;

        public bool FixedLength { get; set; }
    }

    public abstract class DecimalColumnAttributeBase : TableColumnAttributeBase
    {
        protected DecimalColumnAttributeBase(string name)
            : base(name)
        {
        }

        public int Precision { get; set; }

        public int Scale { get; set; }
    }

    public abstract class DateTimeColumnAttributeBase : TableColumnAttributeBase
    {
        protected DateTimeColumnAttributeBase(string name) : base(name)
        {
        }

        public bool IsDate { get; set; }
    }

    public sealed class BooleanColumnAttribute : TableColumnAttributeBase
    {
        public BooleanColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class NullableBooleanColumnAttribute : TableColumnAttributeBase
    {
        public NullableBooleanColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class ByteColumnAttribute : TableColumnAttributeBase
    {
        public ByteColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class NullableByteColumnAttribute : TableColumnAttributeBase
    {
        public NullableByteColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class ByteArrayColumnAttribute : ByteArrayColumnAttributeBase
    {
        public ByteArrayColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class NullableByteArrayColumnAttribute : ByteArrayColumnAttributeBase
    {
        public NullableByteArrayColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class Int16ColumnAttribute : TableColumnAttributeBase
    {
        public Int16ColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class NullableInt16ColumnAttribute : TableColumnAttributeBase
    {
        public NullableInt16ColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class Int32ColumnAttribute : TableColumnAttributeBase
    {
        public Int32ColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class NullableInt32ColumnAttribute : TableColumnAttributeBase
    {
        public NullableInt32ColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class Int64ColumnAttribute : TableColumnAttributeBase
    {
        public Int64ColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class NullableInt64ColumnAttribute : TableColumnAttributeBase
    {
        public NullableInt64ColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class DoubleColumnAttribute : TableColumnAttributeBase
    {
        public DoubleColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class NullableDoubleColumnAttribute : TableColumnAttributeBase
    {
        public NullableDoubleColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class DecimalColumnAttribute : DecimalColumnAttributeBase
    {
        public DecimalColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class NullableDecimalColumnAttribute : DecimalColumnAttributeBase
    {
        public NullableDecimalColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class DateTimeColumnAttribute : DateTimeColumnAttributeBase
    {
        public DateTimeColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class NullableDateTimeColumnAttribute : DateTimeColumnAttributeBase
    {
        public NullableDateTimeColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class DateTimeOffsetColumnAttribute : TableColumnAttributeBase
    {
        public DateTimeOffsetColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class NullableDateTimeOffsetColumnAttribute : TableColumnAttributeBase
    {
        public NullableDateTimeOffsetColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class GuidColumnAttribute : TableColumnAttributeBase
    {
        public GuidColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class NullableGuidColumnAttribute : TableColumnAttributeBase
    {
        public NullableGuidColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class StringColumnAttribute : StringColumnAttributeBase
    {
        public StringColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class NullableStringColumnAttribute : StringColumnAttributeBase
    {
        public NullableStringColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class XmlColumnAttribute : TableColumnAttributeBase
    {
        public XmlColumnAttribute(string name) : base(name)
        {
        }
    }

    public sealed class NullableXmlColumnAttribute : TableColumnAttributeBase
    {
        public NullableXmlColumnAttribute(string name) : base(name)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class IndexAttribute : Attribute
    {
        public IndexAttribute(string column, params string[] otherColumns)
        {
            this.Columns = new string[1 + otherColumns.Length];
            this.Columns[0] = column;
            Array.Copy(otherColumns, 0, this.Columns, 1, otherColumns.Length);
        }

        public string[] Columns { get; }

        public string? Name { get; set; }

        public bool Unique { get; set; }

        public bool Clustered { get; set; }

        public string[]? DescendingColumns { get; set; }
    }
}
