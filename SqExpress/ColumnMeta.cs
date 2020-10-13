using System;

namespace SqExpress
{
    public class ColumnMeta
    {
        private readonly Lazy<TableColumn>? _foreignKey;

        internal bool IsPrimaryKey { get; }

        internal bool IsIdentity { get; }

        internal TableColumn? ForeignKeyColumn => this._foreignKey?.Value;

        public ColumnMeta(bool isPrimaryKey, bool isIdentity, Func<TableColumn>? foreignFactory)
        {
            this.IsPrimaryKey = isPrimaryKey;
            this.IsIdentity = isIdentity;

            if (foreignFactory != null)
            {
                this._foreignKey = new Lazy<TableColumn>(foreignFactory);
            }
        }

        public static ColumnMetaBuilder PrimaryKey() => ColumnMetaBuilder.Default.PrimaryKey();

        public static ColumnMetaBuilder Identity() => ColumnMetaBuilder.Default.Identity();

        public static ColumnMetaBuilder ForeignKey<TTable>(Func<TTable, TableColumn> fkFactory) where TTable : TableBase, new() => ColumnMetaBuilder.Default.ForeignKey(fkFactory);

        public readonly struct ColumnMetaBuilder
        {
            private readonly bool _isPrimaryKey;
            private readonly bool _isIdentity;
            private readonly Func<TableColumn>? _fkFactory;

            public static ColumnMetaBuilder Default => new ColumnMetaBuilder(false, false, null);

            public ColumnMetaBuilder(bool isPrimaryKey, bool isIdentity, Func<TableColumn>? fkFactory)
            {
                this._isPrimaryKey = isPrimaryKey;
                this._isIdentity = isIdentity;
                this._fkFactory = fkFactory;
            }

            public ColumnMetaBuilder PrimaryKey()
            {
                if (this._isPrimaryKey)
                {
                    throw new SqExpressException("Primary key has been already set");
                }
                return new ColumnMetaBuilder(true, this._isIdentity, this._fkFactory);
            }

            public ColumnMetaBuilder Identity()
            {
                if (this._isIdentity)
                {
                    throw new SqExpressException("Identity has been already set");
                }
                return new ColumnMetaBuilder(this._isPrimaryKey, true, this._fkFactory);
            }

            public ColumnMetaBuilder ForeignKey<TTable>(Func<TTable, TableColumn> fkFactory) where TTable : TableBase, new()
            {
                if (this._fkFactory != null)
                {
                    throw new SqExpressException("a foreign key has been already set");
                }
                return new ColumnMetaBuilder(this._isPrimaryKey, this._isIdentity, () => fkFactory(new TTable()));
            }

            public static implicit operator ColumnMeta(ColumnMetaBuilder builder)=> new ColumnMeta(builder._isPrimaryKey, builder._isIdentity, builder._fkFactory);
        }
    }
}