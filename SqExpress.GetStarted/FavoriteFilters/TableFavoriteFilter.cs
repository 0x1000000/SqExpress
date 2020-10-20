namespace SqExpress.GetStarted.FavoriteFilters
{
    public class TableFavoriteFilter : TableBase
    {
        public TableFavoriteFilter() : this(default) { }
        public TableFavoriteFilter(Alias alias) : base("dbo", "FavoriteFilter", alias)
        {
            this.FavoriteFilterId = CreateInt32Column(nameof(this.FavoriteFilterId), ColumnMeta.PrimaryKey().Identity());
            this.Name = CreateStringColumn(nameof(this.Name), 255);
        }

        public readonly Int32TableColumn FavoriteFilterId;
        public readonly StringTableColumn Name;
    }

    public class TableFavoriteFilterItem : TableBase
    {
        public TableFavoriteFilterItem(Alias alias = default) : base("dbo", "FavoriteFilterItem", alias)
        {
            this.FavoriteFilterId = CreateInt32Column(nameof(this.FavoriteFilterId), ColumnMeta.ForeignKey<TableFavoriteFilter>(t=>t.FavoriteFilterId));
            this.Id = CreateInt32Column(nameof(Id));
            this.ParentId = CreateInt32Column(nameof(ParentId));
            this.ArrayIndex = CreateNullableInt32Column(nameof(ArrayIndex));
            this.IsTypeTag = CreateBooleanColumn(nameof(IsTypeTag));
            this.Tag = CreateStringColumn(nameof(Tag), 255);
            this.Value = CreateNullableStringColumn(nameof(Value), null);
        }

        public readonly Int32TableColumn FavoriteFilterId;

        public Int32TableColumn Id { get; }
        public Int32TableColumn ParentId { get; }
        public NullableInt32TableColumn ArrayIndex { get; }
        public BooleanTableColumn IsTypeTag { get; }
        public StringTableColumn Tag { get; }
        public NullableStringTableColumn Value { get; }

    }
}