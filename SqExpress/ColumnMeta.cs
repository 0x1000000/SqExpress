using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress;

public class ColumnMeta
{
    public bool IsPrimaryKey { get; }

    public bool IsIdentity { get; }

    public IReadOnlyList<TableColumn>? ForeignKeyColumns { get; }

    public IReadOnlyList<ColumnForeignKey>? ForeignKeys { get; }

    public ExprValue? ColumnDefaultValue { get; }

    internal ColumnMeta(bool isPrimaryKey, bool isIdentity, IReadOnlyList<TableColumn>? foreignFactory, ExprValue? defaultValue)
        : this(isPrimaryKey, isIdentity, foreignFactory?.Select(c => new ColumnForeignKey(c, ForeignKeyDeleteAction.NoAction)).ToArray(), defaultValue)
    {
    }

    internal ColumnMeta(bool isPrimaryKey, bool isIdentity, IReadOnlyList<ColumnForeignKey>? foreignKeys, ExprValue? defaultValue)
    {
        this.IsPrimaryKey = isPrimaryKey;
        this.IsIdentity = isIdentity;
        this.ColumnDefaultValue = defaultValue;
        this.ForeignKeys = foreignKeys;
        this.ForeignKeyColumns = foreignKeys?.Select(fk => fk.ReferencedColumn).ToArray();
    }

    public static ColumnMetaBuilder PrimaryKey() => ColumnMetaBuilder.Default.PrimaryKey();

    public static ColumnMetaBuilder Identity() => ColumnMetaBuilder.Default.Identity();

    public static ColumnMetaBuilder ForeignKey<TTable>(Func<TTable, TableColumn> fkFactory) where TTable : TableBase, new() => ColumnMetaBuilder.Default.ForeignKey(fkFactory);

    public static ColumnMetaBuilder ForeignKey<TTable>(Func<TTable, TableColumn> fkFactory, ForeignKeyDeleteAction onDelete) where TTable : TableBase, new() => ColumnMetaBuilder.Default.ForeignKey(fkFactory, onDelete);

    public static ColumnMetaBuilder ForeignKey(TableColumn column) => ColumnMetaBuilder.Default.ForeignKey(column);

    public static ColumnMetaBuilder ForeignKey(TableColumn column, ForeignKeyDeleteAction onDelete) => ColumnMetaBuilder.Default.ForeignKey(column, onDelete);

    public static ColumnMetaBuilder DefaultValue(ExprValue defaultValue) => ColumnMetaBuilder.Default.DefaultValue(defaultValue);

    public readonly struct ColumnMetaBuilder
    {
        //To Prevent Cycles in Foreign Keys
        private static readonly ConcurrentDictionary<object, byte> FkFactoriesCache = new ConcurrentDictionary<object, byte>();

        private readonly bool _isPrimaryKey;
        private readonly bool _isIdentity;
        private readonly ColumnForeignKey[]? _fks;
        private readonly ExprValue? _defaultValue;

        public static ColumnMetaBuilder Default => new ColumnMetaBuilder(false, false, null, null);

        internal ColumnMetaBuilder(bool isPrimaryKey, bool isIdentity, ColumnForeignKey[]? fks, ExprValue? defaultValue)
        {
            this._isPrimaryKey = isPrimaryKey;
            this._isIdentity = isIdentity;
            this._fks = fks;
            this._defaultValue = defaultValue;
        }

        public ColumnMetaBuilder PrimaryKey()
        {
            if (this._isPrimaryKey)
            {
                throw new SqExpressException("Primary key has been already set");
            }
            return new ColumnMetaBuilder(true, this._isIdentity, this._fks, this._defaultValue);
        }

        public ColumnMetaBuilder Identity()
        {
            if (this._isIdentity)
            {
                throw new SqExpressException("Identity has been already set");
            }
            return new ColumnMetaBuilder(this._isPrimaryKey, true, this._fks, this._defaultValue);
        }

        public ColumnMetaBuilder ForeignKey<TTable>(Func<TTable, TableColumn> fkFactory) where TTable : TableBase, new()
            => this.ForeignKey(fkFactory, ForeignKeyDeleteAction.NoAction);

        public ColumnMetaBuilder ForeignKey<TTable>(Func<TTable, TableColumn> fkFactory, ForeignKeyDeleteAction onDelete) where TTable : TableBase, new()
        {
            ValidateDeleteAction(onDelete);
            TableColumn? fkColumn;

            if(FkFactoriesCache.TryAdd(fkFactory, 0))
            {
                fkColumn = fkFactory(new TTable());
            }
            else
            {
                return this;
            }
            FkFactoriesCache.Clear();

            return this.ForeignKey(fkColumn!, onDelete);
        }

        public ColumnMetaBuilder ForeignKey(TableColumn column)
            => this.ForeignKey(column, ForeignKeyDeleteAction.NoAction);

        public ColumnMetaBuilder ForeignKey(TableColumn column, ForeignKeyDeleteAction onDelete)
        {
            ValidateDeleteAction(onDelete);
            var fk = new ColumnForeignKey(column, onDelete);
            var newFks = this._fks == null
                ? [fk]
                : Helpers.Combine(this._fks, fk);

            return new ColumnMetaBuilder(this._isPrimaryKey, this._isIdentity, newFks, this._defaultValue);
        }

        private static void ValidateDeleteAction(ForeignKeyDeleteAction onDelete)
        {
            if (onDelete != ForeignKeyDeleteAction.NoAction && onDelete != ForeignKeyDeleteAction.Cascade)
            {
                throw new SqExpressException($"Unsupported foreign key delete action: {onDelete}");
            }
        }

        public ColumnMetaBuilder DefaultValue(ExprValue defaultValue)
        {
            if (!ReferenceEquals(this._defaultValue, null))
            {
                throw new SqExpressException("Default Value has been already set");
            }
            return new ColumnMetaBuilder(this._isPrimaryKey, this._isIdentity, this._fks, defaultValue);
        }

        public static implicit operator ColumnMeta(ColumnMetaBuilder builder)
            => new ColumnMeta(builder._isPrimaryKey, builder._isIdentity, builder._fks, builder._defaultValue);
    }
}
