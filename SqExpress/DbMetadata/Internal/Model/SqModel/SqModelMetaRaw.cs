using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.DbMetadata.Internal.Model.SqModel
{
    internal class SqModelMetaRaw
    {
        public SqModelMetaRaw(string modelName, string fieldName, string fieldTypeName, string? castTypeName, string tableNamespace, string tableName, string columnName, bool isPrimaryKey, bool isIdentity, BaseTypeKindTag baseTypeKindTag)
        {
            ModelName = modelName;
            FieldName = fieldName;
            FieldTypeName = fieldTypeName;
            CastTypeName = castTypeName;
            TableNamespace = tableNamespace;
            TableName = tableName;
            ColumnName = columnName;
            IsPrimaryKey = isPrimaryKey;
            IsIdentity = isIdentity;
            BaseTypeKindTag = baseTypeKindTag;
        }

        public string ModelName { get; }

        public string FieldName { get; }

        public string FieldTypeName { get; }

        public string? CastTypeName { get; }

        public string TableNamespace { get; }

        public string TableName { get; }

        public string ColumnName { get; }

        public bool IsPrimaryKey { get; }

        public bool IsIdentity { get; }

        public BaseTypeKindTag BaseTypeKindTag { get; }
    }
}