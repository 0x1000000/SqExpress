using SqExpress.CodeGenUtil.CodeGen;

namespace SqExpress.CodeGenUtil.Model.SqModel
{
    internal class SqModelMetaRaw
    {
        public SqModelMetaRaw(string modelName, string fieldName, string fieldTypeName, string? castTypeName, string tableNamespace, string tableName, string columnName, bool isPrimaryKey, bool isIdentity, BaseTypeKindTag baseTypeKindTag)
        {
            this.ModelName = modelName;
            this.FieldName = fieldName;
            this.FieldTypeName = fieldTypeName;
            this.CastTypeName = castTypeName;
            this.TableNamespace = tableNamespace;
            this.TableName = tableName;
            this.ColumnName = columnName;
            this.IsPrimaryKey = isPrimaryKey;
            this.IsIdentity = isIdentity;
            this.BaseTypeKindTag = baseTypeKindTag;
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