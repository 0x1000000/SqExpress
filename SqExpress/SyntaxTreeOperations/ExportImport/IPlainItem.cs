namespace SqExpress.SyntaxTreeOperations.ExportImport
{
    public interface IPlainItem
    {
        int Id { get; }
        int ParentId { get; }
        int? ArrayIndex { get; }
        bool IsTypeTag { get; }
        string Tag { get; }
        string? Value { get; }
    }
}