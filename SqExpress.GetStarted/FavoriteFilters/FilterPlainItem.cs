using SqExpress.SyntaxTreeOperations.ExportImport;

namespace SqExpress.GetStarted.FavoriteFilters
{
    public class FilterPlainItem : IPlainItem
    {
        public static FilterPlainItem Create(int favoriteFilterId, int id, int parentId, int? arrayIndex, bool isTypeTag, string tag, string encodedValue)
            => new FilterPlainItem(favoriteFilterId, id, parentId, arrayIndex, isTypeTag, tag, encodedValue);

        public FilterPlainItem(int favoriteFilterId, int id, int parentId, int? arrayIndex, bool isTypeTag, string tag, string value)
        {
            this.FavoriteFilterId = favoriteFilterId;
            this.Id = id;
            this.ParentId = parentId;
            this.ArrayIndex = arrayIndex;
            this.IsTypeTag = isTypeTag;
            this.Tag = tag;
            this.Value = value;
        }

        public int FavoriteFilterId { get; }
        public int Id { get; }
        public int ParentId { get; }
        public int? ArrayIndex { get; }
        public bool IsTypeTag { get; }
        public string Tag { get; }
        public string Value { get; }
    }
}