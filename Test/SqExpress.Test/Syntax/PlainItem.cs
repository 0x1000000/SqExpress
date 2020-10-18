using SqExpress.SyntaxTreeOperations.ExportImport;

namespace SqExpress.Test.Syntax
{
    public class PlainItem : IPlainItem
    {
        public PlainItem(int id, int parentId, int? arrayIndex, bool isTypeTag, string tag, string encodedValue)
        {
            this.Id = id;
            this.ParentId = parentId;
            this.ArrayIndex = arrayIndex;
            this.IsTypeTag = isTypeTag;
            this.Tag = tag;
            this.Value = encodedValue;
        }

        public int Id { get; }
        public int ParentId { get; }
        public int? ArrayIndex { get; }
        public bool IsTypeTag { get; }
        public string Tag { get; }
        public string Value { get; }
    }
}