using System;
using System.Collections.Generic;
using System.Linq;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.CodeGen.Shared
{
    internal enum CodeGenModelType
    {
        ImmutableClass = 1,
        Record = 2
    }

    internal sealed class CodeGenSqModelMeta
    {
        private readonly List<CodeGenSqModelPropertyMeta> _properties = new List<CodeGenSqModelPropertyMeta>();

        public CodeGenSqModelMeta(string name)
        {
            this.Name = name;
        }

        public string Name { get; }

        public IReadOnlyList<CodeGenSqModelPropertyMeta> Properties => this._properties;

        public CodeGenSqModelPropertyMeta AddPropertyCheckExistence(CodeGenSqModelPropertyMeta candidate)
        {
            var result = this._properties.Find(p => p.Name == candidate.Name);
            if (result != null)
            {
                if (result.Type != candidate.Type || result.CastType != candidate.CastType)
                {
                    throw new InvalidOperationException($"Property \"{this.Name}.{candidate.Name}\" was declared several times with different types.");
                }

                return result;
            }

            this._properties.Add(candidate);
            return candidate;
        }

        public bool HasPk()
        {
            var pkCount = this.Properties.Count(i => i.IsPrimaryKey);
            return pkCount > 0 && pkCount < this.Properties.Count;
        }
    }

    internal sealed class CodeGenSqModelPropertyMeta
    {
        private readonly List<CodeGenSqModelPropertyTableColMeta> _column = new List<CodeGenSqModelPropertyTableColMeta>();

        public CodeGenSqModelPropertyMeta(string name, string type, string? castType, bool isPrimaryKey, bool isIdentity)
        {
            this.Name = name;
            this.Type = type;
            this.CastType = castType;
            this.IsPrimaryKey = isPrimaryKey;
            this.IsIdentity = isIdentity;
        }

        public string Name { get; }

        public string Type { get; }

        public string? CastType { get; }

        public string FinalType => this.CastType ?? this.Type;

        public bool IsPrimaryKey { get; }

        public bool IsIdentity { get; }

        public IReadOnlyList<CodeGenSqModelPropertyTableColMeta> Column => this._column;

        public void AddColumnCheckExistence(string modelName, CodeGenSqModelPropertyTableColMeta candidate)
        {
            foreach (var c in this._column)
            {
                if (c.TableRef.Equals(candidate.TableRef))
                {
                    throw new InvalidOperationException($"Property \"{modelName}.{this.Name}\" was declared several times in one table descriptor.");
                }
            }

            this._column.Add(candidate);
        }
    }

    internal readonly struct CodeGenSqModelTableRef : IEquatable<CodeGenSqModelTableRef>
    {
        public CodeGenSqModelTableRef(string tableTypeName, string tableTypeNameSpace, BaseTypeKindTag baseTypeKindTag)
        {
            this.TableTypeName = tableTypeName;
            this.TableTypeNameSpace = tableTypeNameSpace;
            this.BaseTypeKindTag = baseTypeKindTag;
        }

        public string TableTypeName { get; }

        public string TableTypeNameSpace { get; }

        public BaseTypeKindTag BaseTypeKindTag { get; }

        public bool Equals(CodeGenSqModelTableRef other)
        {
            return this.TableTypeName == other.TableTypeName && this.TableTypeNameSpace == other.TableTypeNameSpace;
        }

        public override bool Equals(object? obj)
        {
            return obj is CodeGenSqModelTableRef other && this.Equals(other);
        }

        public override int GetHashCode()
        {
#if NETSTANDARD
            unchecked
            {
                return this.TableTypeName.GetHashCode() * 397 ^ this.TableTypeNameSpace.GetHashCode();
            }
#else
            return HashCode.Combine(this.TableTypeName, this.TableTypeNameSpace);
#endif
        }
    }

    internal sealed class CodeGenSqModelPropertyTableColMeta
    {
        public CodeGenSqModelPropertyTableColMeta(CodeGenSqModelTableRef tableRef, string columnName)
        {
            this.TableRef = tableRef;
            this.ColumnName = columnName;
        }

        public CodeGenSqModelTableRef TableRef { get; }

        public string ColumnName { get; }
    }
}
