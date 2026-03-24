using System.Collections.Immutable;
namespace SqExpress.CodeGen.Shared
{
    public enum CodeGenTableKind
    {
        Table,
        TempTable
    }

    public enum CodeGenColumnKind
    {
        Boolean,
        NullableBoolean,
        Byte,
        NullableByte,
        ByteArray,
        NullableByteArray,
        Int16,
        NullableInt16,
        Int32,
        NullableInt32,
        Int64,
        NullableInt64,
        Double,
        NullableDouble,
        Decimal,
        NullableDecimal,
        DateTime,
        NullableDateTime,
        DateTimeOffset,
        NullableDateTimeOffset,
        Guid,
        NullableGuid,
        String,
        NullableString,
        Xml,
        NullableXml
    }

    public enum CodeGenValidationIssueKind
    {
        DuplicateColumn,
        InvalidPropertyName,
        DuplicatePropertyName,
        UnknownIndexColumn,
        DescendingColumnMustBeIndexed,
        ForeignKeyTableNotFound,
        ForeignKeyColumnNotFound
    }

    public enum CodeGenDefaultValueKind
    {
        None = 0,
        RawSql = 1,
        Null = 2,
        Int32 = 3,
        Boolean = 4,
        String = 5,
        UtcNow = 6,
        Now = 7,
        Byte = 8,
        Int16 = 9,
        Int64 = 10,
        Decimal = 11,
        Double = 12,
        Guid = 13,
        DateTime = 14,
        DateTimeOffset = 15
    }

    public sealed class CodeGenTableModel
    {
        public CodeGenTableModel(
            CodeGenTableKind kind,
            string? databaseName,
            string? schemaName,
            string tableName,
            string className,
            string? @namespace,
            string fullyQualifiedTypeName,
            ImmutableArray<CodeGenColumnModel> columns,
            ImmutableArray<CodeGenIndexModel> indexes,
            string? sqModelName = null)
        {
            this.Kind = kind;
            this.DatabaseName = databaseName;
            this.SchemaName = schemaName;
            this.TableName = tableName;
            this.ClassName = className;
            this.Namespace = @namespace;
            this.FullyQualifiedTypeName = fullyQualifiedTypeName;
            this.Columns = columns;
            this.Indexes = indexes;
            this.SqModelName = sqModelName;
        }

        public CodeGenTableKind Kind { get; }

        public string? DatabaseName { get; }

        public string? SchemaName { get; }

        public string TableName { get; }

        public string ClassName { get; }

        public string? Namespace { get; }

        public string FullyQualifiedTypeName { get; }

        public ImmutableArray<CodeGenColumnModel> Columns { get; }

        public ImmutableArray<CodeGenIndexModel> Indexes { get; }

        public string? SqModelName { get; }

        public string TableKey => CodeGenTableDescriptorSupport.BuildTableKey(this.Kind, this.DatabaseName, this.SchemaName, this.TableName);

        public string TableDisplayName => this.Kind == CodeGenTableKind.TempTable ? $"temp table [{this.TableName}]" : this.TableKey;
    }

    public sealed class CodeGenColumnModel
    {
        public CodeGenColumnModel(
            CodeGenColumnKind kind,
            string sqlName,
            string? propertyName,
            bool isPrimaryKey,
            bool isIdentity,
            string? foreignKeyDatabase,
            string? foreignKeySchema,
            string? foreignKeyTable,
            string? foreignKeyColumn,
            CodeGenDefaultValueKind defaultValueKind,
            string? defaultValue,
            bool isUnicode,
            int? maxLength,
            bool isFixedLength,
            bool isText,
            int precision,
            int scale,
            bool isDate,
            string? sqModels = null,
            string? sqModelCastTypeName = null)
        {
            this.Kind = kind;
            this.SqlName = sqlName;
            this.PropertyName = propertyName;
            this.IsPrimaryKey = isPrimaryKey;
            this.IsIdentity = isIdentity;
            this.ForeignKeyDatabase = foreignKeyDatabase;
            this.ForeignKeySchema = foreignKeySchema;
            this.ForeignKeyTable = foreignKeyTable;
            this.ForeignKeyColumn = foreignKeyColumn;
            this.DefaultValueKind = defaultValueKind;
            this.DefaultValue = defaultValue;
            this.IsUnicode = isUnicode;
            this.MaxLength = maxLength;
            this.IsFixedLength = isFixedLength;
            this.IsText = isText;
            this.Precision = precision;
            this.Scale = scale;
            this.IsDate = isDate;
            this.SqModels = sqModels;
            this.SqModelCastTypeName = sqModelCastTypeName;
        }

        public CodeGenColumnKind Kind { get; }

        public string SqlName { get; }

        public string? PropertyName { get; }

        public bool IsPrimaryKey { get; }

        public bool IsIdentity { get; }

        public string? ForeignKeyDatabase { get; }

        public string? ForeignKeySchema { get; }

        public string? ForeignKeyTable { get; }

        public string? ForeignKeyColumn { get; }

        public CodeGenDefaultValueKind DefaultValueKind { get; }

        public string? DefaultValue { get; }

        public bool IsUnicode { get; }

        public int? MaxLength { get; }

        public bool IsFixedLength { get; }

        public bool IsText { get; }

        public int Precision { get; }

        public int Scale { get; }

        public bool IsDate { get; }

        public string? SqModels { get; }

        public string? SqModelCastTypeName { get; }
    }

    public sealed class CodeGenIndexModel
    {
        public CodeGenIndexModel(
            ImmutableArray<string> columns,
            ImmutableArray<string> descendingColumns,
            string? name,
            bool isUnique,
            bool isClustered)
        {
            this.Columns = columns;
            this.DescendingColumns = descendingColumns;
            this.Name = name;
            this.IsUnique = isUnique;
            this.IsClustered = isClustered;
        }

        public ImmutableArray<string> Columns { get; }

        public ImmutableArray<string> DescendingColumns { get; }

        public string? Name { get; }

        public bool IsUnique { get; }

        public bool IsClustered { get; }
    }

    public sealed class CodeGenValidationIssue
    {
        public CodeGenValidationIssue(CodeGenValidationIssueKind kind, string subject, string tableDisplayName, string? relatedValue = null)
        {
            this.Kind = kind;
            this.Subject = subject;
            this.TableDisplayName = tableDisplayName;
            this.RelatedValue = relatedValue;
        }

        public CodeGenValidationIssueKind Kind { get; }

        public string Subject { get; }

        public string TableDisplayName { get; }

        public string? RelatedValue { get; }
    }

    public sealed class CodeGenValidationResult
    {
        public CodeGenValidationResult(
            ImmutableDictionary<string, string> propertyNamesBySqlName,
            ImmutableArray<CodeGenValidationIssue> issues)
        {
            this.PropertyNamesBySqlName = propertyNamesBySqlName;
            this.Issues = issues;
        }

        public ImmutableDictionary<string, string> PropertyNamesBySqlName { get; }

        public ImmutableArray<CodeGenValidationIssue> Issues { get; }
    }

    public sealed class CodeGenTableDescriptorRenderOptions
    {
        public CodeGenTableDescriptorRenderOptions(
            bool isPublic,
            bool isPartial,
            bool isSealed = false,
            CodeGenTableDescriptorConstructorStyle constructorStyle = CodeGenTableDescriptorConstructorStyle.EmptyAndAlias,
            bool useCompactColumnFactoryArguments = false,
            bool includeAutoGeneratedHeader = true,
            bool includeNullableEnable = true)
        {
            this.IsPublic = isPublic;
            this.IsPartial = isPartial;
            this.IsSealed = isSealed;
            this.ConstructorStyle = constructorStyle;
            this.UseCompactColumnFactoryArguments = useCompactColumnFactoryArguments;
            this.IncludeAutoGeneratedHeader = includeAutoGeneratedHeader;
            this.IncludeNullableEnable = includeNullableEnable;
        }

        public bool IsPublic { get; }

        public bool IsPartial { get; }

        public bool IsSealed { get; }

        public CodeGenTableDescriptorConstructorStyle ConstructorStyle { get; }

        public bool UseCompactColumnFactoryArguments { get; }

        public bool IncludeAutoGeneratedHeader { get; }

        public bool IncludeNullableEnable { get; }

        public static CodeGenTableDescriptorRenderOptions Analyzer { get; } = new CodeGenTableDescriptorRenderOptions(
            isPublic: false,
            isPartial: true);

        public static CodeGenTableDescriptorRenderOptions PublicPartial { get; } = new CodeGenTableDescriptorRenderOptions(
            isPublic: true,
            isPartial: true);

        public static CodeGenTableDescriptorRenderOptions PublicSealedSingleAliasConstructor { get; } = new CodeGenTableDescriptorRenderOptions(
            isPublic: true,
            isPartial: false,
            isSealed: true,
            constructorStyle: CodeGenTableDescriptorConstructorStyle.OptionalAliasOnly,
            useCompactColumnFactoryArguments: true,
            includeAutoGeneratedHeader: false,
            includeNullableEnable: false);
    }

    public enum CodeGenTableDescriptorConstructorStyle
    {
        EmptyAndAlias,
        OptionalAliasOnly
    }
}
