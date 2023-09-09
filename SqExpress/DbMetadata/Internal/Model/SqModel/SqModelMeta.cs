using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.DbMetadata.Internal.Model.SqModel
{
    internal class SqModelMeta
    {
        private readonly List<SqModelPropertyMeta> _properties = new List<SqModelPropertyMeta>();

        public SqModelMeta(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public IReadOnlyList<SqModelPropertyMeta> Properties => _properties;

        public SqModelPropertyMeta AddPropertyCheckExistence(SqModelPropertyMeta candidate)
        {
            var result = _properties.Find(p => p.Name == candidate.Name);
            if (result != null)
            {
                if (result.Type != candidate.Type || result.CastType != candidate.CastType)
                {
                    throw new SqExpressException($"Property \"{Name}.{candidate.Name}\" was declared several times with different types.");
                }

                return result;
            }
            _properties.Add(candidate);
            return candidate;
        }

        public bool HasPk()
        {
            var pkCount = Properties.Count(i => i.IsPrimaryKey);
            return pkCount > 0 && pkCount < Properties.Count;
        }
    }

    internal class SqModelPropertyMeta
    {
        private readonly List<SqModelPropertyTableColMeta> _column = new List<SqModelPropertyTableColMeta>();

        public SqModelPropertyMeta(string name, string type, string? castType, bool isPrimaryKey, bool isIdentity)
        {
            Name = name;
            Type = type;
            CastType = castType;
            IsPrimaryKey = isPrimaryKey;
            IsIdentity = isIdentity;
        }

        public string Name { get; }

        public string Type { get; }

        public string? CastType { get; }

        public string FinalType => CastType ?? Type;

        public bool IsPrimaryKey { get; }

        public bool IsIdentity { get; }

        public IReadOnlyList<SqModelPropertyTableColMeta> Column => _column;

        public void AddColumnCheckExistence(string modelName, SqModelPropertyTableColMeta candidate)
        {
            foreach (var c in _column)
            {
                if (c.TableRef.Equals(candidate.TableRef))
                {
                    throw new SqExpressException($"Property \"{modelName}.{Name}\" was declared several times in one table descriptor.");
                }
            }
            _column.Add(candidate);
        }
    }

    internal readonly struct SqModelTableRef : IEquatable<SqModelTableRef>
    {
        public string TableTypeName { get; }

        public string TableTypeNameSpace { get; }

        public BaseTypeKindTag BaseTypeKindTag { get; }

        public SqModelTableRef(string tableTypeName, string tableTypeNameSpace, BaseTypeKindTag baseTypeKindTag)
        {
            TableTypeName = tableTypeName;
            TableTypeNameSpace = tableTypeNameSpace;
            BaseTypeKindTag = baseTypeKindTag;
        }

        public bool Equals(SqModelTableRef other)
        {
            return TableTypeName == other.TableTypeName && TableTypeNameSpace == other.TableTypeNameSpace;
        }

        public override bool Equals(object? obj)
        {
            return obj is SqModelTableRef other && Equals(other);
        }

        public override int GetHashCode()
        {
#if NETSTANDARD
            unchecked
            {
                return TableTypeName.GetHashCode() * 397 ^ TableTypeNameSpace.GetHashCode();
            }
#else

            return HashCode.Combine(TableTypeName, TableTypeNameSpace);
#endif
        }
    }

    internal class SqModelPropertyTableColMeta
    {
        public SqModelPropertyTableColMeta(SqModelTableRef tableRef, string columnName)
        {
            TableRef = tableRef;
            ColumnName = columnName;
        }

        public SqModelTableRef TableRef { get; }

        public string ColumnName { get; }
    }
}