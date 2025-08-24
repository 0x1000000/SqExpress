using System;
using System.Collections.Generic;
using System.Data;
using SqExpress.Meta.Internal;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress.QueryBuilders.RecordSetter.Internal
{
    internal class DataMapSetter<TTable, TItem> : RecordSetterBase<IRecordSetterNext>, IDataMapSetter<TTable, TItem>, IRecordSetterNext
    {
        private readonly List<ExprColumn> _columns = new List<ExprColumn>();

        private int? _capacity;

        private List<ExprLiteral>? _record;

        public int Index { get; private set; } = -1;

        public TTable Target { get; }

        public TItem Source { get; private set; }

        public IReadOnlyList<ExprLiteral>? Record => this._record;

        public IReadOnlyList<ExprColumn> Columns => this._columns;

        public DataMapSetter(TTable target, TItem defaultItem)
        {
            this.Target = target;
            this.Source = defaultItem;
        }

        public void NextItem(TItem item, int? length)
        {
            this.Index++;
            this.Source = item;
            this._capacity = length;
            this._record = length.HasValue ? new List<ExprLiteral>(length.Value) : new List<ExprLiteral>();
        }

        public void EnsureRecordLength()
        {
            if (this._capacity.HasValue)
            {
                if (this._record == null || this._record.Count != this._capacity.Value)
                {
                    throw new SqExpressException($"Number of columns on {this.Index + 1} iteration is less than number of columns on the first one");
                }
            }
        }

        protected override IRecordSetterNext SetGeneric(ExprColumn column, ExprLiteral value)
        {
            var record = this._record.AssertFatalNotNull(nameof(this._record));
            if (this._capacity.HasValue && record.Count == this._capacity)
            {
                throw new SqExpressException($"Number of columns on {this.Index+1} iteration exceeds number of columns on the first one");
            }
            record.Add(value);
            if (!this._capacity.HasValue)
            {
                this._columns.Add(column);
            }
            return this;
        }
    }

    internal class DataTableFirstMapSetter<TTable, TItem> : IDataMapSetter<TTable, TItem>, IRecordSetterNext
    {
        public int Index => 0;
        public TTable Target { get; }
        public TItem Source { get; }

        private readonly DataTable _result;
        private readonly List<object?> _firstRow = new ();
        private readonly HashSet<string> _check = new (StringComparer.InvariantCultureIgnoreCase);

        public DataTableFirstMapSetter(TTable target, TItem defaultItem, string? tableName)
        {
            this.Target = target;
            this.Source = defaultItem;
            this._result = string.IsNullOrEmpty(tableName) ? new DataTable() : new DataTable(tableName);
        }

        private void CheckUniqueness(string columnName)
        {
            if (!this._check.Add(columnName))
            {
                throw new SqExpressException($"Column with name \"{columnName}\" was already added.");
            }
        }

        private IRecordSetterNext Generic(TableColumn column, object? value)
        {
            this.CheckUniqueness(column.ColumnName.Name);
            this._result.Columns.Add(column.Accept(DataColumnConverter.Instance));
            this._firstRow.Add(value);
            return this;
        }

        private IRecordSetterNext GenericCustom(ExprColumn column, bool isNullable, Type clrType, object? value)
        {
            this.CheckUniqueness(column.ColumnName.Name);

            var dataColumn = new DataColumn(column.ColumnName.Name, clrType);

            dataColumn.AllowDBNull = isNullable;
            this._result.Columns.Add(dataColumn);
            
            this._firstRow.Add(value);
            return this;
        }

        public IRecordSetterNext Set(BooleanTableColumn column, bool value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(ByteTableColumn column, byte value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(ByteArrayTableColumn column, IReadOnlyList<byte> value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(Int16TableColumn column, short value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(Int32TableColumn column, int value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(Int64TableColumn column, long value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(DecimalTableColumn column, decimal value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(DoubleTableColumn column, double value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(DateTimeTableColumn column, DateTime value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(DateTimeOffsetTableColumn column, DateTimeOffset value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(GuidTableColumn column, Guid value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(StringTableColumn column, string value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(NullableBooleanTableColumn column, bool? value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(NullableByteTableColumn column, byte? value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(NullableByteArrayTableColumn column, IReadOnlyList<byte>? value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(NullableInt16TableColumn column, short? value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(NullableInt32TableColumn column, int? value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(NullableInt64TableColumn column, long? value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(NullableDecimalTableColumn column, decimal? value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(NullableDoubleTableColumn column, double? value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(NullableDateTimeTableColumn column, DateTime? value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(NullableDateTimeOffsetTableColumn column, DateTimeOffset? value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(NullableGuidTableColumn column, Guid? value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(NullableStringTableColumn column, string? value)
        {
            return this.Generic(column, value);
        }

        public IRecordSetterNext Set(BooleanCustomColumn column, bool value)
        {
            return this.GenericCustom(column, false, typeof(bool), value);
        }

        public IRecordSetterNext Set(ByteCustomColumn column, byte value)
        {
            return this.GenericCustom(column, false, typeof(byte), value);
        }

        public IRecordSetterNext Set(ByteArrayCustomColumn column, IReadOnlyList<byte> value)
        {
            return this.GenericCustom(column, false, typeof(IReadOnlyList<byte>), value);
        }

        public IRecordSetterNext Set(Int16CustomColumn column, short value)
        {
            return this.GenericCustom(column, false, typeof(short), value);
        }

        public IRecordSetterNext Set(Int32CustomColumn column, int value)
        {
            return this.GenericCustom(column, false, typeof(int), value);
        }

        public IRecordSetterNext Set(Int64CustomColumn column, long value)
        {
            return this.GenericCustom(column, false, typeof(long), value);
        }

        public IRecordSetterNext Set(DecimalCustomColumn column, decimal value)
        {
            return this.GenericCustom(column, false, typeof(decimal), value);
        }

        public IRecordSetterNext Set(DoubleCustomColumn column, double value)
        {
            return this.GenericCustom(column, false, typeof(double), value);
        }

        public IRecordSetterNext Set(DateTimeCustomColumn column, DateTime value)
        {
            return this.GenericCustom(column, false, typeof(DateTime), value);
        }

        public IRecordSetterNext Set(DateTimeOffsetCustomColumn column, DateTimeOffset value)
        {
            return this.GenericCustom(column, false, typeof(DateTimeOffset), value);
        }

        public IRecordSetterNext Set(GuidCustomColumn column, Guid value)
        {
            return this.GenericCustom(column, false, typeof(Guid), value);
        }

        public IRecordSetterNext Set(StringCustomColumn column, string value)
        {
            return this.GenericCustom(column, false, typeof(string), value);
        }

        public IRecordSetterNext Set(NullableBooleanCustomColumn column, bool? value)
        {
            return this.GenericCustom(column, true, typeof(bool), value);
        }

        public IRecordSetterNext Set(NullableByteCustomColumn column, byte? value)
        {
            return this.GenericCustom(column, true, typeof(byte), value);
        }

        public IRecordSetterNext Set(NullableByteArrayCustomColumn column, IReadOnlyList<byte>? value)
        {
            return this.GenericCustom(column, true, typeof(byte[]), value);
        }

        public IRecordSetterNext Set(NullableInt16CustomColumn column, short? value)
        {
            return this.GenericCustom(column, true, typeof(short), value);
        }

        public IRecordSetterNext Set(NullableInt32CustomColumn column, int? value)
        {
            return this.GenericCustom(column, true, typeof(int), value);
        }

        public IRecordSetterNext Set(NullableInt64CustomColumn column, long? value)
        {
            return this.GenericCustom(column, true, typeof(long), value);
        }

        public IRecordSetterNext Set(NullableDecimalCustomColumn column, decimal? value)
        {
            return this.GenericCustom(column, true, typeof(decimal), value);
        }

        public IRecordSetterNext Set(NullableDoubleCustomColumn column, double? value)
        {
            return this.GenericCustom(column, true, typeof(double), value);
        }

        public IRecordSetterNext Set(NullableDateTimeCustomColumn column, DateTime? value)
        {
            return this.GenericCustom(column, true, typeof(DateTime), value);
        }

        public IRecordSetterNext Set(NullableDateTimeOffsetCustomColumn column, DateTimeOffset? value)
        {
            return this.GenericCustom(column, true, typeof(DateTimeOffset), value);
        }

        public IRecordSetterNext Set(NullableGuidCustomColumn column, Guid? value)
        {
            return this.GenericCustom(column, true, typeof(Guid), value);
        }

        public IRecordSetterNext Set(NullableStringCustomColumn column, string? value)
        {
            return this.GenericCustom(column, true, typeof(string), value);
        }
    }

    internal class DataTableMapSetter<TTable, TItem> : IDataMapSetter<TTable, TItem>, IRecordSetterNext
    {
        public int Index { get; private set; }

        public TTable Target { get; }
        public TItem Source { get; private set; }

        private readonly DataTable _table;
        private readonly int _capacity;

        private DataRow? _record;
        private int _addedCells;


        public DataTableMapSetter(TTable target, TItem defaultItem, DataTable table, int capacity)
        {
            this.Target = target;
            this.Source = defaultItem;
            this._table = table;
            this._capacity = capacity;
        }

        public void NextItem(TItem item)
        {
            this.Index++;
            this.Source = item;
            this._record = this._table.NewRow();
            this._addedCells = 0;
        }

        public void EnsureRecordLength()
        {
            if (this._addedCells != this._capacity)
            {
                throw new SqExpressException($"Number of columns on {this.Index + 1} iteration is less than number of columns on the first one");
            }
        }

        private IRecordSetterNext SetGeneric(ExprColumn column, object? value)
        {
            var dataRow = this._record;
            if (dataRow == null)
            {
                throw new SqExpressException("Next row was not added");
            }
            dataRow[column.ColumnName.Name] = value;
            this._addedCells++;
            return this;
        }

        public IRecordSetterNext Set(BooleanTableColumn column, bool value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(ByteTableColumn column, byte value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(ByteArrayTableColumn column, IReadOnlyList<byte> value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(Int16TableColumn column, short value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(Int32TableColumn column, int value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(Int64TableColumn column, long value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(DecimalTableColumn column, decimal value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(DoubleTableColumn column, double value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(DateTimeTableColumn column, DateTime value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(DateTimeOffsetTableColumn column, DateTimeOffset value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(GuidTableColumn column, Guid value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(StringTableColumn column, string value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableBooleanTableColumn column, bool? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableByteTableColumn column, byte? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableByteArrayTableColumn column, IReadOnlyList<byte>? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableInt16TableColumn column, short? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableInt32TableColumn column, int? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableInt64TableColumn column, long? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableDecimalTableColumn column, decimal? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableDoubleTableColumn column, double? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableDateTimeTableColumn column, DateTime? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableDateTimeOffsetTableColumn column, DateTimeOffset? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableGuidTableColumn column, Guid? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableStringTableColumn column, string? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(BooleanCustomColumn column, bool value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(ByteCustomColumn column, byte value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(ByteArrayCustomColumn column, IReadOnlyList<byte> value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(Int16CustomColumn column, short value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(Int32CustomColumn column, int value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(Int64CustomColumn column, long value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(DecimalCustomColumn column, decimal value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(DoubleCustomColumn column, double value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(DateTimeCustomColumn column, DateTime value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(DateTimeOffsetCustomColumn column, DateTimeOffset value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(GuidCustomColumn column, Guid value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(StringCustomColumn column, string value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableBooleanCustomColumn column, bool? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableByteCustomColumn column, byte? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableByteArrayCustomColumn column, IReadOnlyList<byte>? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableInt16CustomColumn column, short? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableInt32CustomColumn column, int? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableInt64CustomColumn column, long? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableDecimalCustomColumn column, decimal? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableDoubleCustomColumn column, double? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableDateTimeCustomColumn column, DateTime? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableDateTimeOffsetCustomColumn column, DateTimeOffset? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableGuidCustomColumn column, Guid? value)
        {
            return this.SetGeneric(column, value);
        }

        public IRecordSetterNext Set(NullableStringCustomColumn column, string? value)
        {
            return this.SetGeneric(column, value);
        }
    }
}