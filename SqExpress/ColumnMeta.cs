using System;
using System.Collections.Generic;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress
{
    public class ColumnMeta
    {
        public bool IsPrimaryKey { get; }

        public bool IsIdentity { get; }

        public IReadOnlyList<TableColumn>? ForeignKeyColumns { get; }

        public ExprValue? ColumnDefaultValue { get; }

        internal ColumnMeta(bool isPrimaryKey, bool isIdentity, IReadOnlyList<TableColumn>? foreignFactory, ExprValue? defaultValue)
        {
            this.IsPrimaryKey = isPrimaryKey;
            this.IsIdentity = isIdentity;
            this.ColumnDefaultValue = defaultValue;
            this.ForeignKeyColumns = foreignFactory;
        }

        public static ColumnMetaBuilder PrimaryKey() => ColumnMetaBuilder.Default.PrimaryKey();

        public static ColumnMetaBuilder Identity() => ColumnMetaBuilder.Default.Identity();

        public static ColumnMetaBuilder ForeignKey<TTable>(Func<TTable, TableColumn> fkFactory) where TTable : TableBase, new() => ColumnMetaBuilder.Default.ForeignKey(fkFactory);

        public static ColumnMetaBuilder DefaultValue(ExprValue defaultValue) => ColumnMetaBuilder.Default.DefaultValue(defaultValue);

        public readonly struct ColumnMetaBuilder
        {
            //To Prevent Cycles in Foreign Keys
            private static readonly HashSet<object> FkFactoriesCache = new HashSet<object>();

            private readonly bool _isPrimaryKey;
            private readonly bool _isIdentity;
            private readonly TableColumn[]? _fks;
            private readonly ExprValue? _defaultValue;

            public static ColumnMetaBuilder Default => new ColumnMetaBuilder(false, false, null, null);

            internal ColumnMetaBuilder(bool isPrimaryKey, bool isIdentity, TableColumn[]? fks, ExprValue? defaultValue)
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
            {
                TableColumn? fkColumn;

                if (!FkFactoriesCache.Contains(fkFactory))
                {
                    FkFactoriesCache.Add(fkFactory);

                    fkColumn = fkFactory(new TTable());
                }
                else
                {
                    return this;
                }
                FkFactoriesCache.Clear();

                var newFks = this._fks == null
                    ? new [] {fkColumn}
                    : Helpers.Combine(this._fks, fkColumn);

                return new ColumnMetaBuilder(this._isPrimaryKey, this._isIdentity, newFks, this._defaultValue);
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
}